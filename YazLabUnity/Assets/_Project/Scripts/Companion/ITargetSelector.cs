using System.Collections.Generic;
using DungeonEscape.Guards;

namespace DungeonEscape.Companion
{
    public readonly struct TargetChoice
    {
        public readonly IGuard Guard;
        public readonly float PathLength;

        public TargetChoice(IGuard guard, float pathLength)
        {
            Guard = guard;
            PathLength = pathLength;
        }

        public bool IsValid => Guard != null;
    }

    /// <summary>Farenin hangi muhafizdan anahtar calacagina karar veren strateji.</summary>
    public interface ITargetSelector
    {
        TargetChoice Select(IEnumerable<IGuard> candidates, string keyId);
    }
}
