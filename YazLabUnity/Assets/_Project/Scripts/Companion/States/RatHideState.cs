namespace DungeonEscape.Companion.States
{
    /// <summary>Muhafiz fareyi gorebiliyorken durur; bir sure gorus disinda kalinca yaklasmaya doner.</summary>
    public class RatHideState : RatState
    {
        private const float SafeTimeToResume = 0.4f;
        private float _safeTime;

        public RatHideState(RatAgent rat) : base(rat) { }

        public override string Name => "Hide";

        public override void Enter()
        {
            base.Enter();
            _safeTime = 0f;
            Rat.HideCount++;
            Rat.Navigator.Stop();
            Rat.SetStatus("Fare saklanıyor, muhafız bakıyor...");
        }

        public override void Tick(float deltaTime)
        {
            var target = Rat.Target;
            if (target == null || !target.IsActive)
            {
                Rat.ChangeState(Rat.SelectTarget);
                return;
            }

            _safeTime = target.CanSee(Rat.Position) ? 0f : _safeTime + deltaTime;
            if (_safeTime >= SafeTimeToResume)
            {
                Rat.SetStatus("Fare sessizce yaklaşıyor");
                Rat.ChangeState(Rat.Approach);
            }
        }
    }
}
