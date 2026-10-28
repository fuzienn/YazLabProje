using UnityEngine;

namespace DungeonEscape.Navigation
{
    /// <summary>Yol bulma soyutlamasi. Yapay zeka durumlari NavMeshAgent'i dogrudan tanimaz.</summary>
    public interface INavigator
    {
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        bool IsReady { get; }
        bool HasPath { get; }

        bool SetDestination(Vector2 target);
        bool HasArrived(float tolerance);
        void Stop();

        /// <summary>Ajani yol hesaplamadan verilen NavMesh noktasina tasir (test/olcum baslangici).</summary>
        bool Teleport(Vector2 point);

        /// <summary>Hedefe NavMesh uzerindeki yolun uzunlugu. Ulasilamiyorsa false.</summary>
        bool TryGetPathLength(Vector2 target, out float length);

        /// <summary>Noktayi bu ajanin gidebilecegi en yakin NavMesh noktasina tasir.</summary>
        bool TrySnap(Vector2 point, float maxDistance, out Vector2 snapped);
    }
}
