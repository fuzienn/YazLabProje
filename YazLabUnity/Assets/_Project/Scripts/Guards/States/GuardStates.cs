using DungeonEscape.Core;
using UnityEngine;

namespace DungeonEscape.Guards.States
{
    public abstract class GuardState : IState
    {
        protected readonly Guard Guard;

        protected GuardState(Guard guard) => Guard = guard;

        public abstract string Name { get; }
        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }

        protected float Range(float min, float max) => min + (float)Guard.Random.NextDouble() * (max - min);
    }

    /// <summary>Evinin cevresinde rastgele, ulasilabilir bir noktaya yurur.</summary>
    public class GuardPatrolState : GuardState
    {
        private const int MaxAttempts = 8;
        private float _timeout;

        public GuardPatrolState(Guard guard) : base(guard) { }

        public override string Name => "Patrol";

        public override void Enter()
        {
            _timeout = 6f;
            for (int i = 0; i < MaxAttempts; i++)
            {
                var offset = new Vector2(Range(-1f, 1f), Range(-1f, 1f)) * Guard.PatrolRadius;
                if (!Guard.Navigator.TrySnap(Guard.Home + offset, 0.5f, out var point)) continue;
                if (Guard.Navigator.SetDestination(point)) return;
            }
            Guard.ChangeState(Guard.LookAround);
        }

        public override void Tick(float deltaTime)
        {
            _timeout -= deltaTime;
            if (Guard.Navigator.HasArrived(0.1f) || _timeout <= 0f)
                Guard.ChangeState(Guard.LookAround);
        }
    }

    /// <summary>Durup birkac kez etrafa bakinir; fare bu anlarda saklanmak zorunda kalir.</summary>
    public class GuardLookAroundState : GuardState
    {
        private static readonly Vector2[] Directions = { Vector2.down, Vector2.left, Vector2.up, Vector2.right };

        private float _turnTimer;
        private int _turnsLeft;

        public GuardLookAroundState(Guard guard) : base(guard) { }

        public override string Name => "LookAround";

        public override void Enter()
        {
            Guard.Navigator.Stop();
            _turnsLeft = Guard.Random.Next(2, 4);
            _turnTimer = Range(0.8f, 1.6f);
        }

        public override void Tick(float deltaTime)
        {
            _turnTimer -= deltaTime;
            if (_turnTimer > 0f) return;

            if (_turnsLeft-- <= 0)
            {
                Guard.ChangeState(Guard.Patrol);
                return;
            }
            Guard.Look(Directions[Guard.Random.Next(Directions.Length)]);
            _turnTimer = Range(0.8f, 1.6f);
        }
    }

    /// <summary>Yerde, hareketsiz. Artik gormez ve fare onu hedef olarak secmez.</summary>
    public class GuardNeutralizedState : GuardState
    {
        public GuardNeutralizedState(Guard guard) : base(guard) { }

        public override string Name => "Neutralized";

        public override void Enter() => Guard.OnNeutralized();
    }
}
