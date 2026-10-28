namespace DungeonEscape.Companion.States
{
    /// <summary>Anahtarla oyuncuya doner (ayni yoldan, tunelden).</summary>
    public class RatReturnState : RatState
    {
        public RatReturnState(RatAgent rat) : base(rat) { }

        public override string Name => "Return";

        public override void Tick(float deltaTime)
        {
            if (DistanceTo(Rat.OwnerPosition) <= Rat.DeliverRange)
            {
                Rat.ChangeState(Rat.Deliver);
                return;
            }
            MoveTowards(Rat.OwnerPosition, deltaTime);
        }
    }
}
