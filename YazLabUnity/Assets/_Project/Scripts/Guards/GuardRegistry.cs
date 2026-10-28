using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonEscape.Guards
{
    /// <summary>Sahnedeki muhafizlarin listesi. Muhafizlar kendini OnEnable/OnDisable'da kaydeder.</summary>
    public static class GuardRegistry
    {
        private static readonly List<IGuard> Guards = new();

        public static IReadOnlyList<IGuard> All => Guards;

        public static IEnumerable<IGuard> Active => Guards.Where(g => g.IsActive);

        public static void Register(IGuard guard)
        {
            if (!Guards.Contains(guard)) Guards.Add(guard);
        }

        public static void Unregister(IGuard guard) => Guards.Remove(guard);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => Guards.Clear();
    }
}
