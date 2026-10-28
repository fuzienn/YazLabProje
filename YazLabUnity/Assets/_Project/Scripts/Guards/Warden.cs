using System;
using System.Linq;
using DungeonEscape.Characters;
using DungeonEscape.Interaction;
using UnityEngine;

namespace DungeonEscape.Guards
{
    /// <summary>
    /// Cikis kapisindaki son muhafiz. Savasmaz; diger muhafizlar etkisiz kalinca oyuncuyla konusur.
    /// Konusmanin kendisi (LLM) Dialogue tarafindadir; Warden yalnizca konusma istegini yayinlar.
    /// </summary>
    [RequireComponent(typeof(CharacterAnimator))]
    public class Warden : MonoBehaviour, IInteractable
    {
        private CharacterAnimator _animator;

        public Transform Transform => transform;
        public bool IsTalking { get; private set; }

        /// <summary>Oyuncu konusmayi baslatmak istedi.</summary>
        public event Action TalkRequested;

        public static bool AllGuardsDown => !GuardRegistry.Active.Any();

        private void Awake() => _animator = GetComponent<CharacterAnimator>();

        private void Start() => _animator.Face(Vector2.down);

        public bool CanInteract(Interactor interactor) => !IsTalking;

        public string GetPrompt(Interactor interactor) =>
            AllGuardsDown
                ? "E: Gardiyanla konus"
                : "Gardiyan: \"Once arkadaslarimi atlatman lazim, evlat.\"";

        public void Interact(Interactor interactor)
        {
            if (!AllGuardsDown || IsTalking) return;
            _animator.Face((Vector2)interactor.transform.position - (Vector2)transform.position);
            TalkRequested?.Invoke();
        }

        /// <summary>Diyalog sistemi konusma basladiginda/bittiginde bildirir.</summary>
        public void SetTalking(bool talking) => IsTalking = talking;
    }
}
