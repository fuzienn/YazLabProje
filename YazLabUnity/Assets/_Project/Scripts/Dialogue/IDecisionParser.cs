using System.Collections.Generic;

namespace DungeonEscape.Dialogue
{
    public class ParseResult
    {
        public GuardDecision Decision { get; }
        public IReadOnlyList<string> Warnings { get; }
        public string Error { get; }

        private ParseResult(GuardDecision decision, IReadOnlyList<string> warnings, string error)
        {
            Decision = decision;
            Warnings = warnings;
            Error = error;
        }

        public bool Success => Decision != null;

        public static ParseResult Ok(GuardDecision decision, IReadOnlyList<string> warnings) =>
            new(decision, warnings, null);

        public static ParseResult Fail(string error) => new(null, new List<string>(), error);
    }

    /// <summary>Ham model ciktisini GuardDecision'a cevirir. Bozuk ciktida Fail doner, istisna firlatmaz.</summary>
    public interface IDecisionParser
    {
        ParseResult Parse(string raw);
    }
}
