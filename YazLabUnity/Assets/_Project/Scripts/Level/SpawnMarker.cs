using UnityEngine;

namespace DungeonEscape.Level
{
    /// <summary>Haritadaki dogma ve yerlesim noktalarini isaretler. Spawner'lar bunlari okur.</summary>
    public class SpawnMarker : MonoBehaviour
    {
        [SerializeField] private MarkerKind kind;

        public MarkerKind Kind => kind;

        public void Init(MarkerKind markerKind) => kind = markerKind;

        private void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                MarkerKind.PlayerSpawn => Color.green,
                MarkerKind.RatSpawn => Color.cyan,
                MarkerKind.GuardSpawn => Color.red,
                MarkerKind.Warden => Color.magenta,
                _ => Color.yellow
            };
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
