using UnityEngine;

namespace DungeonEscape.Player
{
    /// <summary>Kamerayi hedefe yumusak takip ettirir ve harita sinirlari icinde tutar.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Rect bounds = new(0, 0, 24, 22);

        private Camera _camera;
        private Vector3 _velocity;

        public void Configure(Transform followTarget, Rect worldBounds)
        {
            target = followTarget;
            bounds = worldBounds;
        }

        private void Awake() => _camera = GetComponent<Camera>();

        private void Start()
        {
            if (target != null) transform.position = Clamp(target.position);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Clamp(target.position), ref _velocity, smoothTime);
        }

        private Vector3 Clamp(Vector3 point)
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float x = bounds.width > halfWidth * 2f ? Mathf.Clamp(point.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth) : bounds.center.x;
            float y = bounds.height > halfHeight * 2f ? Mathf.Clamp(point.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight) : bounds.center.y;
            return new Vector3(x, y, transform.position.z);
        }
    }
}
