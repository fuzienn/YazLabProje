using DungeonEscape.Core;
using DungeonEscape.UI;
using NUnit.Framework;

namespace DungeonEscape.Tests
{
    public class ObjectiveTextTests
    {
        private static string For(GamePhase phase, bool key = false, bool ratBusy = false, int guards = 0, bool exitOpen = false) =>
            ObjectiveText.For(new ObjectiveText.Snapshot(phase, key, ratBusy, guards, exitOpen));

        [Test]
        public void InCell_GuidesTheStory()
        {
            StringAssert.Contains("F tuşuyla", For(GamePhase.InCell));
            StringAssert.Contains("bekle", For(GamePhase.InCell, ratBusy: true));
            StringAssert.Contains("Hücre kapısına", For(GamePhase.InCell, key: true));
        }

        [Test]
        public void Escaping_CountsGuards_ThenPointsToWarden()
        {
            StringAssert.Contains("Kalan: 2", For(GamePhase.Escaping, guards: 2));
            StringAssert.Contains("gardiyanla konuş", For(GamePhase.Escaping, guards: 0));
            StringAssert.Contains("Kapı açık", For(GamePhase.Escaping, exitOpen: true));
        }

        [Test]
        public void EndScreens_ExplainOutcome()
        {
            Assert.AreEqual("ÖZGÜRSÜN!", ObjectiveText.EndScreen(GamePhase.Won, null, 95f).title);
            StringAssert.Contains("01:35", ObjectiveText.EndScreen(GamePhase.Won, null, 95f).body);
            Assert.AreEqual("YAKALANDIN!", ObjectiveText.EndScreen(GamePhase.Lost, LoseReason.Caught, 0f).title);
            Assert.AreEqual("GARDİYAN SALDIRDI!", ObjectiveText.EndScreen(GamePhase.Lost, LoseReason.WardenAttacked, 0f).title);
        }
    }
}
