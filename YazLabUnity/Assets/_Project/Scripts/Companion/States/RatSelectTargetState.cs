using DungeonEscape.Guards;

namespace DungeonEscape.Companion.States
{
    /// <summary>Anahtari olan, ulasilabilir ve yol uzunlugu en kisa muhafizi secer.</summary>
    public class RatSelectTargetState : RatState
    {
        public RatSelectTargetState(RatAgent rat) : base(rat) { }

        public override string Name => "SelectTarget";

        public override void Enter()
        {
            base.Enter();
            var choice = Rat.Selector.Select(GuardRegistry.Active, Rat.KeyId);
            if (!choice.IsValid)
            {
                Rat.FailMission("Ulasilabilir anahtarli muhafiz yok");
                return;
            }

            Rat.Target = choice.Guard;
            Rat.TargetPathLength = choice.PathLength;
            Rat.SetStatus($"Fare en yakin muhafiza gidiyor ({choice.PathLength:F1} m)");
            Rat.ChangeState(Rat.Approach);
        }

        public override void Tick(float deltaTime) { }
    }
}
