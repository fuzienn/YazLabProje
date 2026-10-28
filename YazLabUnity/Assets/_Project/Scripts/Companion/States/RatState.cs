using DungeonEscape.Core;
using UnityEngine;

namespace DungeonEscape.Companion.States
{
    /// <summary>Fare durumlarinin ortak tabani: ajana erisim ve periyodik yeniden yol hesaplama.</summary>
    public abstract class RatState : IState
    {
        private const float RepathInterval = 0.3f;

        protected readonly RatAgent Rat;
        private float _repathTimer;

        protected RatState(RatAgent rat) => Rat = rat;

        public abstract string Name { get; }

        public virtual void Enter() => _repathTimer = 0f;
        public abstract void Tick(float deltaTime);
        public virtual void Exit() { }

        /// <summary>Hareketli hedefler icin yolu belli araliklarla yeniler.</summary>
        protected void MoveTowards(Vector2 destination, float deltaTime)
        {
            _repathTimer -= deltaTime;
            if (_repathTimer > 0f) return;
            Rat.Navigator.SetDestination(destination);
            _repathTimer = RepathInterval;
        }

        protected float DistanceTo(Vector2 point) => Vector2.Distance(Rat.Position, point);
    }
}
