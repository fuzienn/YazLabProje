using System;
using System.Collections;
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
                ? "E: Gardiyanla konuş"
                : "Gardiyan: \"Önce arkadaşlarımı atlatman lazım, evlat.\"";

        public void Interact(Interactor interactor)
        {
            if (!AllGuardsDown || IsTalking) return;
            _animator.Face((Vector2)interactor.transform.position - (Vector2)transform.position);
            TalkRequested?.Invoke();
        }

        /// <summary>Diyalog sistemi konusma basladiginda/bittiginde bildirir.</summary>
        public void SetTalking(bool talking) => IsTalking = talking;

        /// <summary>Kapiyi actiktan sonra yolu acmak icin yana cekilir.</summary>
        public void StepAside(Vector2 offset, float duration = 0.6f) => StartCoroutine(Slide(offset, duration));

        private IEnumerator Slide(Vector2 offset, float duration)
        {
            Vector2 start = transform.position;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(start, start + offset, t / duration);
                _animator.SetMotion(offset, Time.deltaTime);
                transform.position = position;
                yield return null;
            }
            transform.position = start + offset;
            _animator.Face(Vector2.down);
        }
    }
}
