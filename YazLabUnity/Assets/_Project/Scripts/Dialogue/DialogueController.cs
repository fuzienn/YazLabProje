using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonEscape.Core;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using UnityEngine;

namespace DungeonEscape.Dialogue
{
    /// <summary>
    /// Gardiyan konusmasini oyuna baglar: paneli acar, sunucu sagligini kontrol eder, oyuncu mesajlarini
    /// DialogueSession'a iletir ve sonucu oyun durumuna uygular (kapi acilir / oyun kaybedilir).
    /// LLM istegi beklenirken oyun donmaz; panel "Dusunuyor..." gosterir.
    /// </summary>
    public class DialogueController : MonoBehaviour
    {
        [SerializeField] private Warden warden;
        [SerializeField] private LockedDoor exitDoor;
        [SerializeField] private DialogueView view;
        [SerializeField] private OllamaSettings settings;
        [SerializeField] private float closeDelayAfterDecision = 2.5f;

        private IDialogueView _view;
        private ILLMClient _client;
        private DialogueSession _session;
        private CancellationTokenSource _cancellation;
        private bool _serverReady;

        public DialogueSession Session => _session;

        public void Bind(Warden target, LockedDoor door, DialogueView dialogueView, OllamaSettings ollama)
        {
            warden = target;
            exitDoor = door;
            view = dialogueView;
            settings = ollama;
        }

        /// <summary>Testlerde sahte istemci ve gorunum enjekte etmek icin.</summary>
        public void Inject(ILLMClient client, IDialogueView dialogueView)
        {
            if (isActiveAndEnabled) Unsubscribe();
            _client = client;
            _view = dialogueView ?? _view;
            _session = null;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake()
        {
            _client ??= new OllamaClient(settings);
            _view ??= view;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            _cancellation?.Cancel();
        }

        private void Subscribe()
        {
            warden.TalkRequested += OnTalkRequested;
            _view.Submitted += OnSubmitted;
            _view.CloseRequested += EndConversation;
        }

        private void Unsubscribe()
        {
            warden.TalkRequested -= OnTalkRequested;
            _view.Submitted -= OnSubmitted;
            _view.CloseRequested -= EndConversation;
        }

        private void OnTalkRequested()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();
            _session ??= new DialogueSession(_client, new DecisionParser());

            warden.SetTalking(true);
            GameEvents.RaiseDialogueStarted();
            _view.Open();
            _view.SetFear(_session.Fear.Level, FearMeter.OpenThreshold);
            _view.AddLine(Speaker.Warden, WardenPrompt.Opening);
            _ = CheckServerAsync(_cancellation.Token);
        }

        private async Task CheckServerAsync(CancellationToken cancellation)
        {
            _view.SetInputEnabled(false);
            _view.SetThinking(true);
            var health = await _client.CheckHealthAsync(cancellation);
            if (cancellation.IsCancellationRequested) return;

            _serverReady = health.Healthy;
            _view.SetThinking(false);
            _view.SetInputEnabled(true);
            if (!_serverReady)
                _view.ShowError($"{health.Message}\nSunucuyu baslatip tekrar yazabilirsin; oyun beklemeye devam eder.");
        }

        private async void OnSubmitted(string text)
        {
            // async void yalnizca bu UI olay sinirinda; tum hatalar burada yakalanir
            try
            {
                await HandleTurnAsync(text, _cancellation.Token);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _view.ShowError("Beklenmeyen bir hata oldu, tekrar dene.");
                _view.SetThinking(false);
                _view.SetInputEnabled(true);
            }
        }

        private async Task HandleTurnAsync(string text, CancellationToken cancellation)
        {
            _view.AddLine(Speaker.Player, text);
            _view.SetInputEnabled(false);
            _view.SetThinking(true);

            if (!_serverReady)
            {
                var health = await _client.CheckHealthAsync(cancellation);
                _serverReady = health.Healthy;
                if (!_serverReady)
                {
                    FinishTurnWithError(health.Message);
                    return;
                }
            }

            var turn = await _session.SendAsync(text, cancellation);
            if (cancellation.IsCancellationRequested) return;
            _view.SetThinking(false);

            if (!turn.Success)
            {
                if (turn.ErrorKind is LLMErrorKind.ServerUnreachable or LLMErrorKind.ModelMissing) _serverReady = false;
                FinishTurnWithError(turn.UserFacingError);
                return;
            }

            LogTurn(turn);
            _view.AddLine(Speaker.Warden, turn.Decision.Reply);
            _view.SetFear(turn.FearLevel, FearMeter.OpenThreshold);
            ApplyOutcome(turn.Outcome);
        }

        private void ApplyOutcome(DialogueOutcome outcome)
        {
            switch (outcome)
            {
                case DialogueOutcome.DoorOpens:
                    _view.AddLine(Speaker.System, "Gardiyan titreyen elleriyle kapıyı açıyor...");
                    exitDoor.Open();
                    warden.StepAside(new Vector2(2f, 0f));
                    Invoke(nameof(EndConversation), closeDelayAfterDecision);
                    break;
                case DialogueOutcome.GuardAttacks:
                    _view.AddLine(Speaker.System, "Gardiyan kılıcını çekip üzerine atılıyor!");
                    GameEvents.RaiseGameLost(LoseReason.WardenAttacked);
                    break;
                default:
                    _view.SetInputEnabled(true);
                    break;
            }
        }

        private void FinishTurnWithError(string message)
        {
            _view.SetThinking(false);
            _view.ShowError(message);
            _view.SetInputEnabled(true);
        }

        private void EndConversation()
        {
            _cancellation?.Cancel();
            _view.Close();
            warden.SetTalking(false);
            if (GameFlow.Instance == null || !GameFlow.Instance.IsOver) GameEvents.RaiseDialogueEnded();
        }

        private static void LogTurn(TurnResult turn)
        {
            var d = turn.Decision;
            Debug.Log($"[Diyalog] '{turn.PlayerText}' -> fear {d.Fear} (seviye {turn.FearLevel}), {d.Mood}, {d.Action} " +
                      $"=> {turn.Outcome}, {turn.LatencySeconds:F1} sn" +
                      (turn.Repaired ? ", onarildi" : "") +
                      (turn.RuleNote != null ? $", {turn.RuleNote}" : "") +
                      (turn.Warnings.Count > 0 ? $", uyari: {string.Join("; ", turn.Warnings)}" : ""));
        }
    }
}
