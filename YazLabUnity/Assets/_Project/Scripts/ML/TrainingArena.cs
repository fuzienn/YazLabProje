using UnityEngine;

namespace DungeonEscape.ML
{
    /// <summary>
    /// Tek bir egitim arenasi: duvarlarla cevrili oda, her bolumde yeri degisen sutunlar, muhafiz ve fare.
    /// Bir sahnede birden fazla arena paralel calisarak egitimi hizlandirir.
    /// </summary>
    public class TrainingArena : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(10f, 7f);
        [SerializeField] private Transform[] pillars;
        [SerializeField] private TrainingGuard guard;
        [SerializeField] private float minStartDistance = 4f;

        private const float Margin = 0.8f;
        private const int MaxAttempts = 30;

        public TrainingGuard Guard => guard;
        public float Diagonal => size.magnitude;

        public void Configure(Vector2 arenaSize, Transform[] pillarTransforms, TrainingGuard arenaGuard)
        {
            size = arenaSize;
            pillars = pillarTransforms;
            guard = arenaGuard;
        }

        /// <summary>Sutunlari ve muhafizi yerlestirir, fare icin uygun baslangic noktasini (yerel) dondurur.</summary>
        public Vector2 ResetArena()
        {
            foreach (var pillar in pillars)
                pillar.localPosition = RandomPoint(1.4f);
            Physics2D.SyncTransforms();

            guard.ResetGuard(FreePoint(Vector2.zero, 0f, 0.5f));
            Physics2D.SyncTransforms();
            return FreePoint(guard.transform.localPosition, minStartDistance, 0.3f);
        }

        private Vector2 FreePoint(Vector2 awayFrom, float minDistance, float clearance)
        {
            var mask = LayerMask.GetMask("Walls", "Guards");
            for (int i = 0; i < MaxAttempts; i++)
            {
                var local = RandomPoint(Margin);
                if (minDistance > 0f && Vector2.Distance(local, awayFrom) < minDistance) continue;
                if (Physics2D.OverlapCircle(transform.TransformPoint(local), clearance, mask) != null) continue;
                return local;
            }
            return awayFrom + new Vector2(minDistance, 0f);
        }

        private Vector2 RandomPoint(float margin) => new(
            Random.Range(-size.x / 2f + margin, size.x / 2f - margin),
            Random.Range(-size.y / 2f + margin, size.y / 2f - margin));
    }
}
