using DungeonEscape.Player;
using UnityEngine;

namespace DungeonEscape.Tests
{
    /// <summary>Testlerde klavye yerine kullanilan, elle ayarlanan girdi.</summary>
    public class FakePlayerInput : IPlayerInput
    {
        public Vector2 Move { get; set; }
        public bool InteractPressed { get; set; }
        public bool CommandPressed { get; set; }
        public bool RestartPressed { get; set; }
        public bool GameplayEnabled { get; private set; } = true;

        public void SetGameplayEnabled(bool enabled) => GameplayEnabled = enabled;
    }
}
