using System.Collections;
using DungeonEscape.Core;
using UnityEngine;

namespace DungeonEscape.Interaction
{
    /// <summary>
    /// Anahtarla (hucre kapisi) veya disaridan Open cagrisiyla (cikis kapisi, gardiyan karari) acilan kapi.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class LockedDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private DoorId doorId;
        [Tooltip("Bos ise oyuncu anahtarla acamaz; yalnizca Open() ile acilir.")]
        [SerializeField] private string requiredKeyId;
        [SerializeField] private Sprite[] openingFrames;
        [SerializeField] private float frameDuration = 0.08f;

        private SpriteRenderer _renderer;
        private BoxCollider2D _blocker;

        public DoorId Id => doorId;
        public bool IsOpen { get; private set; }
        public Transform Transform => transform;

        public void Configure(DoorId id, string keyId, Sprite[] frames)
        {
            doorId = id;
            requiredKeyId = keyId;
            openingFrames = frames;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _blocker = GetComponent<BoxCollider2D>();
        }

        private bool PlayerCanOpen => !string.IsNullOrEmpty(requiredKeyId);

        public bool CanInteract(Interactor interactor) => !IsOpen && PlayerCanOpen;

        public string GetPrompt(Interactor interactor) =>
            interactor.Keys != null && interactor.Keys.HasKey(requiredKeyId)
                ? "E: Kapıyı aç"
                : "Kilitli. Anahtar gerekli.";

        public void Interact(Interactor interactor)
        {
            if (interactor.Keys == null || !interactor.Keys.RemoveKey(requiredKeyId)) return;
            Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _blocker.enabled = false;
            StartCoroutine(PlayOpening());
            GameEvents.RaiseDoorOpened(doorId);
        }

        private IEnumerator PlayOpening()
        {
            if (openingFrames == null) yield break;
            var wait = new WaitForSeconds(frameDuration);
            foreach (var frame in openingFrames)
            {
                _renderer.sprite = frame;
                yield return wait;
            }
        }
    }
}
