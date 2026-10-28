using DungeonEscape.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonEscape.Player
{
    /// <summary>Interactor'in sectigi hedefin istem metnini ekranda gosterir ("E: Kapiyi ac").</summary>
    public class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private Interactor interactor;
        [SerializeField] private Text label;

        public void Bind(Interactor source, Text text)
        {
            interactor = source;
            label = text;
        }

        private void OnEnable()
        {
            if (interactor != null) interactor.TargetChanged += Refresh;
            Refresh(interactor != null ? interactor.Current : null);
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.TargetChanged -= Refresh;
        }

        private void Update()
        {
            // Anahtar alininca ayni hedefin metni degisebilir
            if (interactor != null && interactor.Current != null) Refresh(interactor.Current);
        }

        private void Refresh(IInteractable target)
        {
            if (label == null) return;
            label.text = target != null ? target.GetPrompt(interactor) : string.Empty;
        }
    }
}
