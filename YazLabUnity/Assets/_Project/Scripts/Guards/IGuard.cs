using DungeonEscape.Interaction;
using UnityEngine;

namespace DungeonEscape.Guards
{
    /// <summary>
    /// Fare ve oyuncunun muhafiz hakkinda bilmesi gerekenler. Fare buradan hedef secer,
    /// gorus konisine yakalanmamak icin CanSee'yi sorar.
    /// </summary>
    public interface IGuard
    {
        Transform Transform { get; }
        IKeyHolder Keys { get; }
        Vector2 LookDirection { get; }

        /// <summary>Etkisiz hale getirilmemis ve gorevde.</summary>
        bool IsActive { get; }

        /// <summary>Nokta gorus konisi ve menzil icinde, arada duvar yok.</summary>
        bool CanSee(Vector2 point);
    }
}
