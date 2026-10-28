using System;

namespace DungeonEscape.Core
{
    public enum DoorId
    {
        Cell,
        Exit
    }

    public enum LoseReason
    {
        Caught,
        WardenAttacked
    }

    /// <summary>
    /// Bilesenler arasi olay yolu. Gonderen, dinleyeni tanimaz (fare, kapi, gardiyan birbirinden bagimsiz kalir).
    /// Sahne yeniden yuklenirken Reset ile tum abonelikler temizlenir.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> KeyDelivered;
        public static event Action<DoorId> DoorOpened;
        public static event Action GuardNeutralized;
        public static event Action DialogueStarted;
        public static event Action DialogueEnded;
        public static event Action GameWon;
        public static event Action<LoseReason> GameLost;

        public static void RaiseKeyDelivered(string keyId) => KeyDelivered?.Invoke(keyId);
        public static void RaiseDoorOpened(DoorId door) => DoorOpened?.Invoke(door);
        public static void RaiseGuardNeutralized() => GuardNeutralized?.Invoke();
        public static void RaiseDialogueStarted() => DialogueStarted?.Invoke();
        public static void RaiseDialogueEnded() => DialogueEnded?.Invoke();
        public static void RaiseGameWon() => GameWon?.Invoke();
        public static void RaiseGameLost(LoseReason reason) => GameLost?.Invoke(reason);

        public static void Reset()
        {
            KeyDelivered = null;
            DoorOpened = null;
            GuardNeutralized = null;
            DialogueStarted = null;
            DialogueEnded = null;
            GameWon = null;
            GameLost = null;
        }
    }
}
