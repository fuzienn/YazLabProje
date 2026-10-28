using UnityEngine;

namespace DungeonEscape.Characters
{
    /// <summary>
    /// Hareket yonune gore dogru sprite'i secer. Hareketin kaynagini bilmez (oyuncu girdisi, NavMesh ajani...);
    /// sahibi her kare SetMotion cagirir.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterAnimator : MonoBehaviour
    {
        // Durus, adim, durus, diger adim
        private static readonly int[] WalkCycle = { 1, 0, 2, 0 };

        [SerializeField] private CharacterSpriteSet sprites;
        [SerializeField] private float walkFramesPerSecond = 8f;
        [SerializeField] private float moveThreshold = 0.05f;

        private SpriteRenderer _renderer;
        private float _walkTime;

        public Facing Facing { get; private set; } = Facing.Down;
        public Vector2 LookDirection { get; private set; } = Vector2.down;

        public void SetSprites(CharacterSpriteSet set) => sprites = set;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        public void SetMotion(Vector2 velocity, float deltaTime)
        {
            bool moving = velocity.sqrMagnitude > moveThreshold * moveThreshold;
            if (moving) Face(velocity);

            _walkTime = moving ? _walkTime + deltaTime : 0f;
            int frame = moving ? WalkCycle[(int)(_walkTime * walkFramesPerSecond) % WalkCycle.Length] : 0;
            Apply(frame);
        }

        /// <summary>Hareket etmeden bakis yonunu degistirir (muhafiz etrafa bakinca).</summary>
        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            LookDirection = direction.normalized;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                Facing = Facing.Side;
                _renderer.flipX = direction.x < 0f;
            }
            else
            {
                Facing = direction.y > 0f ? Facing.Up : Facing.Down;
                _renderer.flipX = false;
            }
            Apply(0);
        }

        private void Apply(int frame)
        {
            if (sprites == null) return;
            var frames = sprites.Get(Facing);
            if (frames != null && frame < frames.Length && frames[frame] != null)
                _renderer.sprite = frames[frame];
        }
    }
}
