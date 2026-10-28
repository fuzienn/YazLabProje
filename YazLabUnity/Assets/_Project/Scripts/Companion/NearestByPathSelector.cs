using System.Collections.Generic;
using DungeonEscape.Guards;
using DungeonEscape.Navigation;

namespace DungeonEscape.Companion
{
    /// <summary>
    /// Anahtari olan ve farenin ulasabildigi muhafizlardan NavMesh yol uzunlugu en kisa olani secer.
    /// Kus ucusu mesafe degil yol uzunlugu kullanilir: duvarin ardindaki muhafiz yakin gorunse de uzak olabilir.
    /// </summary>
    public class NearestByPathSelector : ITargetSelector
    {
        private readonly INavigator _navigator;

        public NearestByPathSelector(INavigator navigator) => _navigator = navigator;

        public TargetChoice Select(IEnumerable<IGuard> candidates, string keyId)
        {
            IGuard best = null;
            float bestLength = float.MaxValue;

            foreach (var guard in candidates)
            {
                if (!guard.IsActive || !guard.Keys.HasKey(keyId)) continue;
                if (!_navigator.TryGetPathLength(guard.Transform.position, out float length)) continue;
                if (length >= bestLength) continue;
                best = guard;
                bestLength = length;
            }
            return new TargetChoice(best, bestLength);
        }
    }
}
