using DungeonEscape.Characters;
using DungeonEscape.Guards;
using UnityEngine;

namespace DungeonEscape.ML
{
    /// <summary>
    /// Egitim arenasindaki muhafiz: yerinde durur, belli araliklarla rastgele yone bakar (oyundaki
    /// LookAround gibi), fareyi gorurse ona doner. Gorus hesabi oyundaki GuardVision ile aynidir.
    /// </summary>
    public class TrainingGuard : MonoBehaviour
    {
        private static readonly Vector2[] Directions = { Vector2.down, Vector2.left, Vector2.up, Vector2.right };

        [SerializeField] private Vector2 turnInterval = new(1f, 2.5f);

        private GuardVision _vision;
        private CharacterAnimator _animator;
        private float _turnTimer;

        public Vector2 LookDirection => Vision.Direction;

        // Ajanin ilk bolumu, muhafizin Awake'inden once baslayabilir; bilesenler ilk kullanimda alinir
        private GuardVision Vision => _vision ??= GetComponentInChildren<GuardVision>();
        private CharacterAnimator Animator => _animator ??= GetComponent<CharacterAnimator>();

        public void ResetGuard(Vector2 position)
        {
            transform.localPosition = position;
            Look(Directions[Random.Range(0, Directions.Length)]);
            _turnTimer = Random.Range(turnInterval.x, turnInterval.y);
        }

        public bool CanSee(Vector2 point) => Vision.CanSee(point);

        /// <summary>Arkada mi: muhafizin bakis yonuyle noktaya giden yon arasindaki aci.</summary>
        public bool IsBehind(Vector2 point, float minAngle) =>
            Vector2.Angle(LookDirection, point - (Vector2)transform.position) >= minAngle;

        public void Watch(Vector2 ratPosition)
        {
            if (CanSee(ratPosition)) Look(ratPosition - (Vector2)transform.position);
        }

        private void FixedUpdate()
        {
            _turnTimer -= Time.fixedDeltaTime;
            if (_turnTimer > 0f) return;
            Look(Directions[Random.Range(0, Directions.Length)]);
            _turnTimer = Random.Range(turnInterval.x, turnInterval.y);
        }

        private void Look(Vector2 direction)
        {
            Vision.Direction = direction.normalized;
            if (Animator != null) Animator.Face(direction);
        }
    }
}
