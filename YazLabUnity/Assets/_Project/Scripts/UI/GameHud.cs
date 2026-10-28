using System.Linq;
using DungeonEscape.Companion;
using DungeonEscape.Core;
using DungeonEscape.Guards;
using DungeonEscape.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonEscape.UI
{
    /// <summary>
    /// Oyun ici ust bilgi: gorev metni, fare durumu, anahtar, baslik ekrani, duraklatma ve oyun sonu ekrani.
    /// Yalnizca gosterir; oyun kararlarini GameFlow ve diger bilesenler verir.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private const float RefreshInterval = 0.2f;

        [SerializeField] private PlayerController player;
        [SerializeField] private RatAgent rat;
        [SerializeField] private Text objective;
        [SerializeField] private Text ratStatus;
        [SerializeField] private Text keyStatus;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text endTitle;
        [SerializeField] private Text endBody;

        private IPlayerInput _input;
        private bool _exitOpen;
        private float _refreshTimer;

        public void Bind(PlayerController owner, RatAgent companion, Text objectiveText, Text ratText, Text keyText,
            GameObject title, GameObject pause, GameObject end, Text endTitleText, Text endBodyText)
        {
            player = owner;
            rat = companion;
            objective = objectiveText;
            ratStatus = ratText;
            keyStatus = keyText;
            titlePanel = title;
            pausePanel = pause;
            endPanel = end;
            endTitle = endTitleText;
            endBody = endBodyText;
        }

        private void OnEnable() => GameEvents.DoorOpened += OnDoorOpened;
        private void OnDisable() => GameEvents.DoorOpened -= OnDoorOpened;

        private void OnDestroy()
        {
            if (GameFlow.Instance != null) GameFlow.Instance.PhaseChanged -= OnPhaseChanged;
        }

        private void Start()
        {
            _input = player.GetComponent<IPlayerInput>();
            if (GameFlow.Instance != null) GameFlow.Instance.PhaseChanged += OnPhaseChanged;
            pausePanel.SetActive(false);
            endPanel.SetActive(false);
            titlePanel.SetActive(true);
            _input?.SetGameplayEnabled(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null) HandleKeys(keyboard);

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshInterval;
            Refresh();
        }

        private void HandleKeys(Keyboard keyboard)
        {
            if (titlePanel.activeSelf)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) DismissTitle();
                return;
            }

            var flow = GameFlow.Instance;
            bool canPause = flow != null && !flow.IsOver && flow.Phase != GamePhase.Dialogue;
            if (keyboard.escapeKey.wasPressedThisFrame && canPause) SetPaused(!pausePanel.activeSelf);
            if (!pausePanel.activeSelf) return;
            if (keyboard.rKey.wasPressedThisFrame) flow.Restart();
            if (keyboard.qKey.wasPressedThisFrame) Application.Quit();
        }

        public void DismissTitle()
        {
            titlePanel.SetActive(false);
            _input?.SetGameplayEnabled(true);
        }

        private void SetPaused(bool paused)
        {
            pausePanel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            _input?.SetGameplayEnabled(!paused);
        }

        private void Refresh()
        {
            var flow = GameFlow.Instance;
            if (flow == null) return;

            var snapshot = new ObjectiveText.Snapshot(flow.Phase, player.Keys.HasKey(rat.KeyId),
                rat.IsOnMission, GuardRegistry.Active.Count(), _exitOpen);
            objective.text = ObjectiveText.For(snapshot);
            ratStatus.text = rat.Status;
            keyStatus.text = snapshot.PlayerHasKey ? "Anahtar: sende" : "Anahtar: yok";
        }

        private void OnDoorOpened(DoorId door)
        {
            if (door == DoorId.Exit) _exitOpen = true;
            Refresh();
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase is not (GamePhase.Won or GamePhase.Lost)) return;
            var (title, body) = ObjectiveText.EndScreen(phase, GameFlow.Instance.LastLoseReason, GameFlow.Instance.ElapsedTime);
            endTitle.text = title;
            endBody.text = body;
            endPanel.SetActive(true);
        }
    }
}
