using DungeonEscape.Core;

namespace DungeonEscape.UI
{
    /// <summary>Oyun durumundan oyuncuya gosterilecek gorev metnini uretir. Unity'den bagimsiz, test edilebilir.</summary>
    public static class ObjectiveText
    {
        public readonly struct Snapshot
        {
            public readonly GamePhase Phase;
            public readonly bool PlayerHasKey;
            public readonly bool RatOnMission;
            public readonly int ActiveGuards;
            public readonly bool ExitOpen;

            public Snapshot(GamePhase phase, bool playerHasKey, bool ratOnMission, int activeGuards, bool exitOpen)
            {
                Phase = phase;
                PlayerHasKey = playerHasKey;
                RatOnMission = ratOnMission;
                ActiveGuards = activeGuards;
                ExitOpen = exitOpen;
            }
        }

        public static string For(Snapshot s) => s.Phase switch
        {
            GamePhase.InCell when s.PlayerHasKey => "Anahtar sende! Hücre kapısına git ve E ile aç.",
            GamePhase.InCell when s.RatOnMission => "Fare görevde. Muhafızlara yakalanmadan anahtarı getirmesini bekle.",
            GamePhase.InCell => "Hücredesin. F tuşuyla fareye emir ver: en yakın muhafızın anahtarını getirsin.",
            GamePhase.Escaping when s.ExitOpen => "Kapı açık! Kapıdan çık ve özgürlüğüne kavuş.",
            GamePhase.Escaping when s.ActiveGuards > 0 =>
                $"Muhafızlara arkadan yaklaşıp E ile etkisiz hâle getir. Kalan: {s.ActiveGuards}. Görüş konilerinden uzak dur!",
            GamePhase.Escaping => "Tüm muhafızlar yerde. Çıkıştaki gardiyanla konuş (E) ve onu korkut.",
            _ => string.Empty
        };

        public static (string title, string body) EndScreen(GamePhase phase, LoseReason? reason, float seconds)
        {
            string time = $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
            if (phase == GamePhase.Won)
                return ("ÖZGÜRSÜN!", $"Zindandan {time} sürede kaçtın.\nR: yeniden oyna");
            return reason == LoseReason.WardenAttacked
                ? ("GARDİYAN SALDIRDI!", "Bertan'ı korkutacağına kızdırdın.\nR: yeniden dene")
                : ("YAKALANDIN!", "Bir muhafız seni gördü.\nR: yeniden dene");
        }
    }
}
