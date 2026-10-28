using DungeonEscape.Dialogue;
using NUnit.Framework;

namespace DungeonEscape.Tests
{
    public class DialogueRulesTests
    {
        private static GuardDecision Decision(GuardAction action, int fear = 50) =>
            new("...", fear, GuardMood.Nervous, action);

        [Test]
        public void FearRise_IsLimitedPerTurn()
        {
            var meter = new FearMeter(initialLevel: 15, maxRisePerTurn: 35);
            Assert.AreEqual(50, meter.Apply(100));
            Assert.AreEqual(85, meter.Apply(100));
            Assert.AreEqual(100, meter.Apply(100));
        }

        [Test]
        public void FearDrop_IsLimitedPerTurn()
        {
            var meter = new FearMeter(initialLevel: 80, maxDropPerTurn: 15);
            Assert.AreEqual(65, meter.Apply(0));
        }

        [Test]
        public void OpenDoor_BelowThreshold_IsRejected()
        {
            var meter = new FearMeter(initialLevel: 30);
            var outcome = DialogueRules.Resolve(Decision(GuardAction.OpenDoor), meter, out var note);
            Assert.AreEqual(DialogueOutcome.Continue, outcome);
            Assert.IsNotNull(note);
        }

        [Test]
        public void OpenDoor_AboveThreshold_OpensDoor()
        {
            var meter = new FearMeter(initialLevel: FearMeter.OpenThreshold);
            Assert.AreEqual(DialogueOutcome.DoorOpens, DialogueRules.Resolve(Decision(GuardAction.OpenDoor), meter, out _));
        }

        [Test]
        public void Attack_WhenBrave_EndsGame()
        {
            var meter = new FearMeter(initialLevel: 10);
            Assert.AreEqual(DialogueOutcome.GuardAttacks, DialogueRules.Resolve(Decision(GuardAction.Attack), meter, out _));
        }

        [Test]
        public void Attack_WhenScared_IsRejected()
        {
            var meter = new FearMeter(initialLevel: 60);
            Assert.AreEqual(DialogueOutcome.Continue, DialogueRules.Resolve(Decision(GuardAction.Attack), meter, out var note));
            Assert.IsNotNull(note);
        }

        [Test]
        public void HighFearWithoutModelAction_DoesNotOpenDoor()
        {
            var meter = new FearMeter(initialLevel: 95);
            Assert.AreEqual(DialogueOutcome.Continue, DialogueRules.Resolve(Decision(GuardAction.None), meter, out _));
        }
    }
}
