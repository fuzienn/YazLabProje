using System;

namespace DungeonEscape.Dialogue
{
    /// <summary>
    /// Gardiyanin korku seviyesi. Modelin her turda bildirdigi korku dogrudan alinmaz; tur basina
    /// artis ve azalis sinirlanir. Boylece tek bir cumleyle 0'dan 100'e sicrama olmaz.
    /// </summary>
    public class FearMeter
    {
        public const int OpenThreshold = 70;
        public const int AttackCeiling = 30;

        private readonly int _maxRisePerTurn;
        private readonly int _maxDropPerTurn;

        public int Level { get; private set; }
        public bool CanOpenDoor => Level >= OpenThreshold;
        public bool BraveEnoughToAttack => Level < AttackCeiling;

        public FearMeter(int initialLevel = 15, int maxRisePerTurn = 35, int maxDropPerTurn = 15)
        {
            Level = Math.Clamp(initialLevel, 0, 100);
            _maxRisePerTurn = maxRisePerTurn;
            _maxDropPerTurn = maxDropPerTurn;
        }

        /// <summary>
        /// Modelin bildirdigi korkuya dogru sinirli adimla ilerler ve yeni seviyeyi dondurur.
        /// allowRise false ise (manipulasyon girisimi) korku yalnizca azalabilir.
        /// </summary>
        public int Apply(int reportedFear, bool allowRise = true)
        {
            int delta = Math.Clamp(reportedFear - Level, -_maxDropPerTurn, allowRise ? _maxRisePerTurn : 0);
            Level = Math.Clamp(Level + delta, 0, 100);
            return Level;
        }
    }
}
