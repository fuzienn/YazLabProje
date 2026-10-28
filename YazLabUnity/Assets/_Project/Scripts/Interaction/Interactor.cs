using System;
using UnityEngine;

namespace DungeonEscape.Interaction
{
    /// <summary>
    /// Yakindaki en uygun IInteractable'i secer. Girdiyi bilmez; PlayerController Interact cagirir.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private float radius = 1.1f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private readonly Collider2D[] _hits = new Collider2D[16];
        private ContactFilter2D _filter;

        public IKeyHolder Keys { get; private set; }
        public IInteractable Current { get; private set; }

        /// <summary>Hedef degisince (null dahil) tetiklenir; HUD istem metni icin.</summary>
        public event Action<IInteractable> TargetChanged;

        public void Bind(IKeyHolder keys) => Keys = keys;

        private void Awake()
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(interactableMask);
        }

        private void Update()
        {
            var best = FindBest();
            if (ReferenceEquals(best, Current)) return;
            Current = best;
            TargetChanged?.Invoke(Current);
        }

        public bool TryInteract()
        {
            if (Current == null || !Current.CanInteract(this)) return false;
            Current.Interact(this);
            return true;
        }

        private IInteractable FindBest()
        {
            int count = Physics2D.OverlapCircle(transform.position, radius, _filter, _hits);
            IInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].TryGetComponent(out IInteractable candidate)) continue;
                if (!candidate.CanInteract(this)) continue;

                float distance = (candidate.Transform.position - transform.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
