using NavMeshPlus.Components;
using UnityEngine;

namespace DungeonEscape.Navigation
{
    /// <summary>
    /// Sahne acilirken tum NavMeshSurface'leri pisirir. Harita kucuk oldugu icin bu birkac ms surer
    /// ve editorde NavMesh varligi tutma geregini ortadan kaldirir.
    /// Awake'te degil Start'ta pisirilir: yuzeyler etkinlesmeden pisirilen veri NavMesh'e eklenmiyor.
    /// Erken calisma sirasi sayesinde ajanlarin Start'indan once biter.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        private void Start()
        {
            foreach (var surface in GetComponentsInChildren<NavMeshSurface>())
                surface.BuildNavMesh();
        }
    }
}
