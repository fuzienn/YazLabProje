using UnityEngine;

namespace DungeonEscape.Companion.States
{
    /// <summary>
    /// Hedef muhafizin arkasina dogru ilerler. Sensor hedefi calma menzilinde gorurse calar;
    /// muhafiz fareyi gorebilecek durumdaysa saklanmaya gecer.
    /// </summary>
    public class RatApproachState : RatState
    {
        private const float BehindOffset = 0.5f;

        public RatApproachState(RatAgent rat) : base(rat) { }

        public override string Name => "Approach";

        public override void Tick(float deltaTime)
        {
            var target = Rat.Target;
            if (target == null || !target.IsActive || !target.Keys.HasKey(Rat.KeyId))
            {
                Rat.ChangeState(Rat.SelectTarget);
                return;
            }

            Vector2 guardPosition = target.Transform.position;
            float distance = DistanceTo(guardPosition);

            if (IsInStealRange(target.Transform, distance))
            {
                Rat.ChangeState(Rat.Steal);
                return;
            }

            if (distance < Rat.DangerRange && target.CanSee(Rat.Position))
            {
                Rat.ChangeState(Rat.Hide);
                return;
            }

            MoveTowards(BehindPoint(guardPosition, target.LookDirection), deltaTime);
        }

        /// <summary>Sensor isinlarindan biri hedefe menzil icinde carpiyorsa veya dogrudan gorus hatti varsa.</summary>
        private bool IsInStealRange(Transform guard, float distance)
        {
            if (Rat.Sensor.Detects(guard, Rat.StealRange, out _)) return true;
            return distance <= Rat.StealRange && Rat.Sensor.HasLineOfSight(guard, Rat.StealRange);
        }

        /// <summary>Muhafizin baktigi yonun tersi; ulasilamazsa muhafizin kendisi.</summary>
        private Vector2 BehindPoint(Vector2 guardPosition, Vector2 look)
        {
            var behind = guardPosition - look.normalized * BehindOffset;
            return Rat.Navigator.TrySnap(behind, 0.6f, out var snapped) ? snapped : guardPosition;
        }
    }
}
