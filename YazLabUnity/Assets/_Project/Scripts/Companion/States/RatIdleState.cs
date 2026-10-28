namespace DungeonEscape.Companion.States
{
    /// <summary>Gorev yokken oyuncunun yaninda bekler; uzaklasirsa pesinden gider.</summary>
    public class RatIdleState : RatState
    {
        public RatIdleState(RatAgent rat) : base(rat) { }

        public override string Name => "Idle";

        public override void Tick(float deltaTime)
        {
            if (DistanceTo(Rat.OwnerPosition) > Rat.FollowDistance)
                MoveTowards(Rat.OwnerPosition, deltaTime);
            else if (Rat.Navigator.HasPath)
                Rat.Navigator.Stop();
        }
    }
}
