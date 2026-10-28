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
                Rat.FailMission("Ulaşılabilir, anahtarlı muhafız yok");
                return;
            }

            Rat.Target = choice.Guard;
            Rat.TargetPathLength = choice.PathLength;
            Rat.SetStatus($"Fare en yakın muhafıza gidiyor ({choice.PathLength:F1} m)");
            Rat.ChangeState(Rat.Approach);
        }

        public override void Tick(float deltaTime) { }
    }
}
