using UnityEngine;

namespace DungeonEscape.Player
{
    /// <summary>Oyuncu girdisi soyutlamasi. Testlerde sahte girdi verilebilir.</summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool InteractPressed { get; }
        bool CommandPressed { get; }
        bool RestartPressed { get; }

        /// <summary>Diyalogda metin yazarken oyun girdisi kapatilir.</summary>
        void SetGameplayEnabled(bool enabled);
    }
}
