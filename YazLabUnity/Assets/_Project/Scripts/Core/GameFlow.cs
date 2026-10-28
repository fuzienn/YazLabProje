using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonEscape.Core
{
    public enum GamePhase
    {
        InCell,
        Escaping,
        Dialogue,
        Won,
        Lost
    }

    /// <summary>
    /// Oyunun ust duzey asamalarini olaylara gore ilerletir.
    /// Hucre kapisi acilinca kacis, gardiyanla konusma baslayinca diyalog, sonunda kazanma/kaybetme.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.InCell;
        public LoseReason? LastLoseReason { get; private set; }
        public bool IsOver => Phase is GamePhase.Won or GamePhase.Lost;

        public float ElapsedTime => Time.timeSinceLevelLoad;

        public event Action<GamePhase> PhaseChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Domain reload kapaliyken Play modundan kalan abonelikleri temizler
            GameEvents.Reset();
            Instance = null;
        }

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            GameEvents.DoorOpened += OnDoorOpened;
            GameEvents.DialogueStarted += OnDialogueStarted;
            GameEvents.DialogueEnded += OnDialogueEnded;
            GameEvents.GameWon += OnGameWon;
            GameEvents.GameLost += OnGameLost;
        }

        private void OnDisable()
        {
            GameEvents.DoorOpened -= OnDoorOpened;
            GameEvents.DialogueStarted -= OnDialogueStarted;
            GameEvents.DialogueEnded -= OnDialogueEnded;
            GameEvents.GameWon -= OnGameWon;
            GameEvents.GameLost -= OnGameLost;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            GameEvents.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnDoorOpened(DoorId door)
        {
            if (door == DoorId.Cell && Phase == GamePhase.InCell) SetPhase(GamePhase.Escaping);
        }

        private void OnDialogueStarted()
        {
            if (!IsOver) SetPhase(GamePhase.Dialogue);
        }

        private void OnDialogueEnded()
        {
            if (Phase == GamePhase.Dialogue) SetPhase(GamePhase.Escaping);
        }

        private void OnGameWon() => SetPhase(GamePhase.Won);

        private void OnGameLost(LoseReason reason)
        {
            if (IsOver) return;
            LastLoseReason = reason;
            SetPhase(GamePhase.Lost);
        }

        private void SetPhase(GamePhase next)
        {
            if (Phase == next || IsOver) return;
            Phase = next;
            Debug.Log($"[GameFlow] Asama: {next}");
            PhaseChanged?.Invoke(next);
        }
    }
}
