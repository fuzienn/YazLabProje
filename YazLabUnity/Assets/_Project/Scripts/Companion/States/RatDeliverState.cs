using DungeonEscape.Core;
using DungeonEscape.Interaction;

namespace DungeonEscape.Companion.States
{
    /// <summary>Anahtari oyuncuya verir ve gorevi tamamlar.</summary>
    public class RatDeliverState : RatState
    {
        public RatDeliverState(RatAgent rat) : base(rat) { }

        public override string Name => "Deliver";

        public override void Enter()
        {
            base.Enter();
            Rat.Navigator.Stop();
            if (KeyRing.Transfer(Rat.Keys, Rat.OwnerKeys, Rat.KeyId))
            {
                GameEvents.RaiseKeyDelivered(Rat.KeyId);
                Rat.CompleteMission();
            }
            else
            {
                Rat.ChangeState(Rat.Idle);
            }
        }

        public override void Tick(float deltaTime) { }
    }
}
