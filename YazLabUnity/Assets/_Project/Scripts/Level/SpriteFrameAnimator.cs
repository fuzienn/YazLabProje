using UnityEngine;

namespace DungeonEscape.Level
{
    /// <summary>Mesale gibi dekorlar icin sabit hizda donen sprite animasyonu.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 8f;

        private SpriteRenderer _renderer;
        private float _offset;

        public void SetFrames(Sprite[] newFrames, float fps)
        {
            frames = newFrames;
            framesPerSecond = fps;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            // Ayni anda yanan mesaleler senkron gorunmesin
            _offset = Random.value * 10f;
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)((Time.time + _offset) * framesPerSecond) % frames.Length;
            _renderer.sprite = frames[index];
        }
    }
}
