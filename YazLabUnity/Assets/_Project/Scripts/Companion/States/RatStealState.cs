using DungeonEscape.Interaction;

namespace DungeonEscape.Companion.States
{
    /// <summary>Kisa bir sure muhafizin dibinde bekleyip anahtari kendi anahtarligina aktarir.</summary>
    public class RatStealState : RatState
    {
        private const float StealDuration = 0.5f;
        private float _timer;

        public RatStealState(RatAgent rat) : base(rat) { }

        public override string Name => "Steal";

        public override void Enter()
        {
            base.Enter();
            _timer = 0f;
            Rat.Navigator.Stop();
            Rat.SetStatus("Fare anahtari kemiriyor...");
        }

        public override void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < StealDuration) return;

            if (Rat.Target != null && KeyRing.Transfer(Rat.Target.Keys, Rat.Keys, Rat.KeyId))
            {
                Rat.SetStatus("Fare anahtari kapti, geri donuyor!");
                Rat.ChangeState(Rat.Return);
            }
            else
            {
                Rat.ChangeState(Rat.SelectTarget);
            }
        }
    }
}
