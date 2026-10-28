using UnityEngine;

namespace DungeonEscape.Companion
{
    /// <summary>Tek bir isinin sonucu: aci (derece, dunya +x eksenine gore), mesafe ve carpilan nesne.</summary>
    public struct RayReading
    {
        public float Angle;
        public float Distance;
        public Collider2D Hit;
    }

    /// <summary>
    /// Ajanin cevresine esit aralikli isin demeti gonderir. Her isin engele (duvar) veya muhafiza olan
    /// mesafeyi ve aciyi olcer. Fare bu olcumlerle hedefin menzilde olup olmadigini, en yakin engeli
    /// ve sikisip sikismadigini anlar.
    /// </summary>
    public class RaySensor2D : MonoBehaviour
    {
        [SerializeField] private int rayCount = 16;
        [SerializeField] private float range = 3f;
        [SerializeField] private LayerMask detectionMask = ~0;

        private RayReading[] _readings;

        public int RayCount => rayCount;
        public float Range => range;
        public RayReading[] Readings => _readings;

        public void Configure(int rays, float maxRange, LayerMask mask)
        {
            rayCount = rays;
            range = maxRange;
            detectionMask = mask;
            _readings = null;
        }

        /// <summary>Tum isinlari gonderir ve olcumleri gunceller.</summary>
        public void Sense()
        {
            if (_readings == null || _readings.Length != rayCount) _readings = new RayReading[rayCount];

            Vector2 origin = transform.position;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = 360f * i / rayCount;
                var direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var hit = Physics2D.Raycast(origin, direction, range, detectionMask);
                _readings[i] = new RayReading
                {
                    Angle = angle,
                    Distance = hit.collider != null ? hit.distance : range,
                    Hit = hit.collider
                };
            }
        }

        /// <summary>Herhangi bir isin verilen nesneye menzil icinde carpiyor mu (arada engel yok demektir).</summary>
        public bool Detects(Transform target, float maxDistance, out RayReading reading)
        {
            reading = default;
            if (_readings == null || target == null) return false;
            foreach (var r in _readings)
            {
                if (r.Hit == null || r.Distance > maxDistance) continue;
                if (r.Hit.transform != target && !r.Hit.transform.IsChildOf(target)) continue;
                reading = r;
                return true;
            }
            return false;
        }

        /// <summary>Hedefe dogrudan tek isinla gorus hatti kontrolu (demetin arasina dusen hedefler icin).</summary>
        public bool HasLineOfSight(Transform target, float maxDistance)
        {
            Vector2 origin = transform.position;
            Vector2 toTarget = (Vector2)target.position - origin;
            if (toTarget.magnitude > maxDistance) return false;
            var hit = Physics2D.Raycast(origin, toTarget.normalized, toTarget.magnitude + 0.1f, detectionMask);
            return hit.collider == null || hit.transform == target || hit.transform.IsChildOf(target);
        }

        /// <summary>En kisa olcum (en yakin engel). Hic carpma yoksa false.</summary>
        public bool NearestObstacle(out RayReading nearest)
        {
            nearest = default;
            if (_readings == null) return false;
            nearest.Distance = float.MaxValue;
            foreach (var r in _readings)
                if (r.Hit != null && r.Distance < nearest.Distance) nearest = r;
            return nearest.Hit != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (_readings == null) return;
            foreach (var r in _readings)
            {
                var direction = new Vector3(Mathf.Cos(r.Angle * Mathf.Deg2Rad), Mathf.Sin(r.Angle * Mathf.Deg2Rad));
                Gizmos.color = r.Hit != null ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, transform.position + direction * r.Distance);
            }
        }
    }
}
