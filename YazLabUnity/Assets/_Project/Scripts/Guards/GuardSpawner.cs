using System.Collections.Generic;
using System.Linq;
using DungeonEscape.Level;
using DungeonEscape.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonEscape.Guards
{
    /// <summary>
    /// Muhafizlari aday noktalar arasindan seed'li rastgelelikle secer ve noktanin cevresinde
    /// ulasilabilir bir yere yerlestirir. Ayni seed her zaman ayni yerlesimi verir (testler icin).
    /// </summary>
    public class GuardSpawner : MonoBehaviour
    {
        [SerializeField] private Guard guardPrefab;
        [SerializeField] private int guardCount = 3;
        [SerializeField] private float jitterRadius = 1.2f;
        [SerializeField] private float minSpacing = 2.5f;
        [Tooltip("0 ise her oyunda farkli rastgele yerlesim.")]
        [SerializeField] private int seed;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<Guard> _spawned = new();

        public int LastSeed { get; private set; }
        public IReadOnlyList<Guard> Spawned => _spawned;

        public void Configure(Guard prefab, int count)
        {
            guardPrefab = prefab;
            guardCount = count;
        }

        private void Start()
        {
            if (spawnOnStart) Spawn(seed != 0 ? seed : System.Environment.TickCount);
        }

        public void Spawn(int spawnSeed)
        {
            Clear();
            LastSeed = spawnSeed;
            var random = new System.Random(spawnSeed);
            var candidates = FindObjectsByType<SpawnMarker>()
                .Where(m => m.Kind == MarkerKind.GuardSpawn)
                .OrderBy(m => m.name)
                .Select(m => (Vector2)m.transform.position)
                .OrderBy(_ => random.Next())
                .ToList();

            var filter = new NavMeshQueryFilter
            {
                agentTypeID = NavAreas.AgentTypeId(NavAreas.GuardAgent),
                areaMask = NavAreas.GuardAreaMask
            };

            foreach (var anchor in candidates)
            {
                if (_spawned.Count >= guardCount) break;
                if (!TryPickPoint(anchor, random, filter, out var point)) continue;

                var guard = Instantiate(guardPrefab, point, Quaternion.identity, transform);
                guard.name = $"Guard_{_spawned.Count}";
                guard.Init(point, random.Next());
                _spawned.Add(guard);
            }
            Debug.Log($"[Spawner] seed={spawnSeed}, {_spawned.Count} muhafiz: " +
                      string.Join(", ", _spawned.Select(g => ((Vector2)g.transform.position).ToString("F1"))));
        }

        private bool TryPickPoint(Vector2 anchor, System.Random random, NavMeshQueryFilter filter, out Vector2 point)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = (float)random.NextDouble() * jitterRadius;
                var candidate = anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                if (!NavMesh.SamplePosition(candidate, out var hit, 0.5f, filter)) continue;
                point = hit.position;
                var p = point;
                if (_spawned.Any(g => Vector2.Distance(g.transform.position, p) < minSpacing)) continue;
                return true;
            }
            point = default;
            return false;
        }

        public void Clear()
        {
            foreach (var guard in _spawned)
                if (guard != null) Destroy(guard.gameObject);
            _spawned.Clear();
        }
    }
}
