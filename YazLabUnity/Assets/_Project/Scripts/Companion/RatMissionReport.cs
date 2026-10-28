namespace DungeonEscape.Companion
{
    /// <summary>Bir anahtar calma gorevinin sonucu. Testler ve rapor tablolari bunu kullanir.</summary>
    public readonly struct RatMissionReport
    {
        public readonly bool Success;
        public readonly float Duration;
        public readonly float TargetPathLength;
        public readonly int HideCount;
        public readonly string FailReason;

        public RatMissionReport(bool success, float duration, float targetPathLength, int hideCount, string failReason)
        {
            Success = success;
            Duration = duration;
            TargetPathLength = targetPathLength;
            HideCount = hideCount;
            FailReason = failReason;
        }
    }
}
