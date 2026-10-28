namespace DungeonEscape.Dialogue
{
    public enum GuardMood
    {
        Calm,
        Nervous,
        Scared,
        Angry
    }

    public enum GuardAction
    {
        None,
        OpenDoor,
        Attack
    }

    /// <summary>Modelin her yanitinin ayristirilmis, dogrulanmis hali.</summary>
    public class GuardDecision
    {
        public string Reply { get; }
        public int Fear { get; }
        public GuardMood Mood { get; }
        public GuardAction Action { get; }

        public GuardDecision(string reply, int fear, GuardMood mood, GuardAction action)
        {
            Reply = reply;
            Fear = fear;
            Mood = mood;
            Action = action;
        }

        /// <summary>Modelden istenen cikti semasi. Ollama 'format' alanina dogrudan verilir.</summary>
        public const string JsonSchema = @"{
  ""type"": ""object"",
  ""properties"": {
    ""reply"": { ""type"": ""string"" },
    ""fear"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 100 },
    ""mood"": { ""type"": ""string"", ""enum"": [""calm"", ""nervous"", ""scared"", ""angry""] },
    ""action"": { ""type"": ""string"", ""enum"": [""NONE"", ""OPEN_DOOR"", ""ATTACK""] }
  },
  ""required"": [""reply"", ""fear"", ""mood"", ""action""]
}";
    }
}
