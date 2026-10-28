using UnityEngine;

namespace DungeonEscape.Guards
{
    /// <summary>
    /// Muhafizin gorus konisi: menzil, aci ve duvar engeli. Koni ayni zamanda ekranda yari saydam
    /// bir mesh olarak cizilir; kenar isinlari duvarlarda kirpilir ki oyuncu nereye bakildigini gorsun.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GuardVision : MonoBehaviour
    {
        [SerializeField] private float range = 4f;
        [SerializeField] private float angle = 90f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private int meshRays = 24;
        [SerializeField] private Color idleColor = new(1f, 0.85f, 0.4f, 0.12f);
        [SerializeField] private Color alertColor = new(1f, 0.2f, 0.2f, 0.3f);

        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private MeshRenderer _renderer;

        public Vector2 Direction { get; set; } = Vector2.down;
        public float Range => range;
        public float Angle => angle;

        public void Configure(float viewRange, float viewAngle, LayerMask obstacles)
        {
            range = viewRange;
            angle = viewAngle;
            obstacleMask = obstacles;
        }

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _mesh = new Mesh { name = "VisionCone" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _vertices = new Vector3[meshRays + 2];
            _triangles = new int[meshRays * 3];
            for (int i = 0; i < meshRays; i++)
            {
                _triangles[i * 3] = 0;
                _triangles[i * 3 + 1] = i + 2;
                _triangles[i * 3 + 2] = i + 1;
            }
        }

        public bool CanSee(Vector2 point)
        {
            if (!enabled) return false;
            Vector2 origin = transform.position;
            Vector2 toPoint = point - origin;
            float distance = toPoint.magnitude;
            if (distance > range) return false;
            if (distance > 0.01f && Vector2.Angle(Direction, toPoint) > angle * 0.5f) return false;
            return !Physics2D.Linecast(origin, point, obstacleMask);
        }

        public void SetAlert(bool alert)
        {
            if (_renderer != null) _renderer.material.color = alert ? alertColor : idleColor;
        }

        public void SetVisible(bool visible)
        {
            if (_renderer != null) _renderer.enabled = visible;
        }

        private void LateUpdate()
        {
            if (_mesh == null) return;
            Vector2 origin = transform.position;
            float start = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg - angle * 0.5f;
            _vertices[0] = Vector3.zero;

            for (int i = 0; i <= meshRays; i++)
            {
                float a = (start + angle * i / meshRays) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var hit = Physics2D.Raycast(origin, dir, range, obstacleMask);
                float length = hit.collider != null ? hit.distance : range;
                _vertices[i + 1] = transform.InverseTransformDirection(dir * length);
            }

            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            float half = angle * 0.5f * Mathf.Deg2Rad;
            float baseAngle = Mathf.Atan2(Direction.y, Direction.x);
            var left = new Vector3(Mathf.Cos(baseAngle + half), Mathf.Sin(baseAngle + half)) * range;
            var right = new Vector3(Mathf.Cos(baseAngle - half), Mathf.Sin(baseAngle - half)) * range;
            Gizmos.DrawLine(transform.position, transform.position + left);
            Gizmos.DrawLine(transform.position, transform.position + right);
        }
    }
}
