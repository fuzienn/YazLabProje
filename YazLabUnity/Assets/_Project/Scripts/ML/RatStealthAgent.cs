using System;
using DungeonEscape.Characters;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DungeonEscape.ML
{
    public enum StealthOutcome
    {
        Success,
        Caught,
        Timeout
    }

    /// <summary>
    /// Pekistirmeli ogrenmeyle egitilen fare: muhafizin gorus konisine yakalanmadan arkasina ulasmayi ogrenir.
    /// Gozlem: RayPerceptionSensor2D (duvar/muhafiz isinlari) + 9 degerlik vektor. Eylem: surekli 2B hareket.
    ///
    /// Odul fonksiyonu:
    ///   +1     muhafizin arkasina (>= 110 derece, <= 0.75 m) gorulmeden ulasmak
    ///   -1     0.6 sn kesintisiz gorulup yakalanmak
    ///   -0.01  gorulen her adim
    ///   +0.5 * (onceki - simdiki mesafe)  hedef noktaya yaklasma (potansiyel tabanli yonlendirme)
    ///   bolum sonunda +0.5 * kalan mesafe: terminal potansiyel sifirlanir (Ng 1999); boylece
    ///   yonlendirme odulunun toplami sonuctan bagimsizdir ve "kosup yakalanmak" odul getirmez
    ///   -1/MaxStep  her adim (oyalanma cezasi)
    ///   -0.002 duvara surtunen her adim
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RatStealthAgent : Agent
    {
        public const int VectorObservationSize = 9;

        [SerializeField] private TrainingArena arena;
        [SerializeField] private float speed = 2.6f;
        [SerializeField] private float stealRange = 0.75f;
        [SerializeField] private float behindAngle = 110f;
        [SerializeField] private float catchTime = 0.6f;
        [SerializeField] private float behindOffset = 0.6f;
        [Tooltip("Ilk denemede 0.1 idi; yakalanma cezasinin yaninda cok zayif kaldigi icin artirildi.")]
        [SerializeField] private float approachRewardScale = 0.5f;

        private Rigidbody2D _body;
        private CharacterAnimator _animator;
        private float _seenTime;
        private float _previousDistance;
        private bool _episodeOpen;

        public event Action<StealthOutcome, int> EpisodeFinished;

        public void SetArena(TrainingArena trainingArena) => arena = trainingArena;

        private TrainingGuard Guard => arena.Guard;
        private Vector2 Position => _body.position;

        private Vector2 BehindPoint =>
            (Vector2)Guard.transform.position - Guard.LookDirection * behindOffset;

        public override void Initialize()
        {
            _body = GetComponent<Rigidbody2D>();
            _animator = GetComponent<CharacterAnimator>();
        }

        public override void OnEpisodeBegin()
        {
            // MaxStep'e ulasilip yeni bolum basladiysa onceki bolum zaman asimidir
            if (_episodeOpen)
            {
                RecordStats(StealthOutcome.Timeout);
                EpisodeFinished?.Invoke(StealthOutcome.Timeout, StepCount);
            }

            var start = arena.ResetArena();
            transform.localPosition = start;
            _body.position = transform.position;
            _body.linearVelocity = Vector2.zero;
            _seenTime = 0f;
            _previousDistance = Vector2.Distance(Position, BehindPoint);
            _episodeOpen = true;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            float scale = arena.Diagonal;
            sensor.AddObservation((BehindPoint - Position) / scale);
            sensor.AddObservation(((Vector2)Guard.transform.position - Position) / scale);
            sensor.AddObservation(Guard.LookDirection);
            sensor.AddObservation(_body.linearVelocity / speed);
            sensor.AddObservation(Guard.CanSee(Position) ? 1f : 0f);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var move = Vector2.ClampMagnitude(new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]), 1f);
            _body.linearVelocity = move * speed;

            AddReward(-1f / Mathf.Max(MaxStep, 1));

            float distance = Vector2.Distance(Position, BehindPoint);
            AddReward(approachRewardScale * (_previousDistance - distance));
            _previousDistance = distance;

            bool seen = Guard.CanSee(Position);
            Guard.Watch(Position);
            if (seen)
            {
                _seenTime += Time.fixedDeltaTime;
                AddReward(-0.01f);
            }
            else
            {
                _seenTime = Mathf.Max(0f, _seenTime - Time.fixedDeltaTime);
            }

            if (_seenTime >= catchTime)
            {
                Finish(StealthOutcome.Caught, -1f);
                return;
            }

            float guardDistance = Vector2.Distance(Position, Guard.transform.position);
            if (!seen && guardDistance <= stealRange && Guard.IsBehind(Position, behindAngle))
                Finish(StealthOutcome.Success, 1f);
        }

        /// <summary>Karsilastirma tabani: gizlilik gozetmeden dogrudan hedef noktaya kosar.</summary>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var direction = (BehindPoint - Position).normalized;
            var actions = actionsOut.ContinuousActions;
            actions[0] = direction.x;
            actions[1] = direction.y;
        }

        /// <summary>TensorBoard'a gercek sonuc oranlarini yazar; toplam odul tek basina yaniltici olabilir.</summary>
        private static void RecordStats(StealthOutcome outcome)
        {
            var stats = Academy.Instance.StatsRecorder;
            stats.Add("Stealth/Success", outcome == StealthOutcome.Success ? 1f : 0f);
            stats.Add("Stealth/Caught", outcome == StealthOutcome.Caught ? 1f : 0f);
            stats.Add("Stealth/Timeout", outcome == StealthOutcome.Timeout ? 1f : 0f);
        }

        private void Update()
        {
            if (_animator != null && _body != null) _animator.SetMotion(_body.linearVelocity, Time.deltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Wall")) AddReward(-0.002f);
        }

        private void Finish(StealthOutcome outcome, float reward)
        {
            AddReward(reward + approachRewardScale * _previousDistance);
            RecordStats(outcome);
            _episodeOpen = false;
            EpisodeFinished?.Invoke(outcome, StepCount);
            EndEpisode();
        }
    }
}
