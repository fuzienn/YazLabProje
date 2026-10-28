using UnityEngine;

namespace DungeonEscape.Interaction
{
    /// <summary>Oyuncunun E ile etkilesebildigi her sey (kapi, muhafiz, gardiyan).</summary>
    public interface IInteractable
    {
        Transform Transform { get; }
        string GetPrompt(Interactor interactor);
        bool CanInteract(Interactor interactor);
        void Interact(Interactor interactor);
    }
}
