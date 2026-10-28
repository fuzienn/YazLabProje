using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonEscape.Dialogue
{
    /// <summary>Bir oyuncu mesajinin islenmis sonucu. Hata varsa Decision bos ve oyun durumu degismez.</summary>
    public class TurnResult
    {
        public string PlayerText;
        public string RawOutput;
        public GuardDecision Decision;
        public DialogueOutcome Outcome;
        public int FearLevel;
        public float LatencySeconds;
        public LLMErrorKind ErrorKind;
        public string UserFacingError;
        public string RuleNote;
        public List<string> Warnings = new();
        public bool Repaired;

        public bool Success => Decision != null;
    }

    /// <summary>
    /// Gardiyanla tek bir konusma: gecmisi tutar, modeli cagirir, ciktiyi ayristirir, korku ve kurallari uygular.
    /// Bozuk ciktida bir kez onarim ister; yine olmazsa turu hatali sayar ama oyunu devam ettirir.
    /// </summary>
    public class DialogueSession
    {
        private const int MaxHistoryMessages = 12;

        private readonly ILLMClient _client;
        private readonly IDecisionParser _parser;
        private readonly List<ChatMessage> _history = new();
        private readonly List<TurnResult> _turns = new();

        public FearMeter Fear { get; }
        public IReadOnlyList<TurnResult> Turns => _turns;
        public bool IsBusy { get; private set; }

        public DialogueSession(ILLMClient client, IDecisionParser parser, FearMeter fear = null)
        {
            _client = client;
            _parser = parser;
            Fear = fear ?? new FearMeter();
            _history.Add(ChatMessage.Assistant(WardenPrompt.Opening));
        }

        public async Task<TurnResult> SendAsync(string playerText, CancellationToken cancellation)
        {
            var turn = new TurnResult { PlayerText = playerText, FearLevel = Fear.Level };
            if (string.IsNullOrWhiteSpace(playerText))
            {
                turn.UserFacingError = "Bir sey soylemelisin.";
                return turn;
            }
            if (IsBusy)
            {
                turn.UserFacingError = "Gardiyan hala dusunuyor...";
                return turn;
            }

            IsBusy = true;
            try
            {
                var messages = BuildMessages(ChatMessage.User(playerText));
                var result = await _client.ChatAsync(messages, GuardDecision.JsonSchema, cancellation);
                turn.LatencySeconds = result.LatencySeconds;

                if (!result.Success)
                {
                    turn.ErrorKind = result.ErrorKind;
                    turn.UserFacingError = result.ErrorMessage;
                    return Record(turn);
                }

                turn.RawOutput = result.Content;
                var parsed = _parser.Parse(result.Content);
                if (!parsed.Success)
                {
                    parsed = await RepairAsync(messages, result.Content, turn, cancellation);
                    if (parsed == null) return Record(turn);
                }

                Apply(turn, parsed);
                _history.Add(ChatMessage.User(playerText));
                _history.Add(ChatMessage.Assistant(turn.RawOutput));
                return Record(turn);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<ParseResult> RepairAsync(List<ChatMessage> messages, string badOutput, TurnResult turn,
            CancellationToken cancellation)
        {
            messages.Add(ChatMessage.Assistant(badOutput));
            messages.Add(ChatMessage.User(WardenPrompt.RepairRequest));
            var retry = await _client.ChatAsync(messages, GuardDecision.JsonSchema, cancellation);
            turn.LatencySeconds += retry.LatencySeconds;

            var parsed = retry.Success ? _parser.Parse(retry.Content) : null;
            if (parsed is { Success: true })
            {
                turn.RawOutput = retry.Content;
                turn.Repaired = true;
                return parsed;
            }

            turn.UserFacingError = "Gardiyan anlasilmaz bir seyler mirildandi. (Model gecersiz yanit verdi, tekrar dene.)";
            turn.ErrorKind = retry.Success ? LLMErrorKind.None : retry.ErrorKind;
            return null;
        }

        private void Apply(TurnResult turn, ParseResult parsed)
        {
            turn.Decision = parsed.Decision;
            turn.Warnings.AddRange(parsed.Warnings);
            turn.FearLevel = Fear.Apply(parsed.Decision.Fear);
            turn.Outcome = DialogueRules.Resolve(parsed.Decision, Fear, out turn.RuleNote);
        }

        private List<ChatMessage> BuildMessages(ChatMessage latest)
        {
            var messages = new List<ChatMessage> { ChatMessage.System(WardenPrompt.Build(Fear.Level)) };
            messages.AddRange(_history.Skip(System.Math.Max(0, _history.Count - MaxHistoryMessages)));
            messages.Add(latest);
            return messages;
        }

        private TurnResult Record(TurnResult turn)
        {
            _turns.Add(turn);
            return turn;
        }
    }
}
