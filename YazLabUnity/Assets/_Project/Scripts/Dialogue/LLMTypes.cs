using System;

namespace DungeonEscape.Dialogue
{
    public enum LLMErrorKind
    {
        None,
        ServerUnreachable,
        ModelMissing,
        Timeout,
        HttpError,
        EmptyResponse,
        Cancelled
    }

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public static ChatMessage System(string content) => new("system", content);
        public static ChatMessage User(string content) => new("user", content);
        public static ChatMessage Assistant(string content) => new("assistant", content);
    }

    /// <summary>Model cagrisinin sonucu. Hata durumunda Content bostur, ErrorKind nedeni soyler.</summary>
    public readonly struct LLMResult
    {
        public readonly string Content;
        public readonly LLMErrorKind ErrorKind;
        public readonly string ErrorMessage;
        public readonly float LatencySeconds;

        private LLMResult(string content, LLMErrorKind kind, string error, float latency)
        {
            Content = content;
            ErrorKind = kind;
            ErrorMessage = error;
            LatencySeconds = latency;
        }

        public bool Success => ErrorKind == LLMErrorKind.None;

        public static LLMResult Ok(string content, float latency) => new(content, LLMErrorKind.None, null, latency);

        public static LLMResult Fail(LLMErrorKind kind, string message, float latency = 0f) =>
            new(null, kind, message, latency);
    }

    public readonly struct HealthStatus
    {
        public readonly bool ServerUp;
        public readonly bool ModelAvailable;
        public readonly string Message;

        public HealthStatus(bool serverUp, bool modelAvailable, string message)
        {
            ServerUp = serverUp;
            ModelAvailable = modelAvailable;
            Message = message;
        }

        public bool Healthy => ServerUp && ModelAvailable;
    }
}
