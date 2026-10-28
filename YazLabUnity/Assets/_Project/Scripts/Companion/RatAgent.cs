using System;
using DungeonEscape.Characters;
using DungeonEscape.Companion.States;
using DungeonEscape.Core;
using DungeonEscape.Guards;
using DungeonEscape.Interaction;
using DungeonEscape.Navigation;
using DungeonEscape.Player;
using UnityEngine;

namespace DungeonEscape.Companion
{
    /// <summary>
    /// Otonom fare yoldas. Oyuncunun F komutuyla en yakin muhafizi NavMesh yol uzunluguna gore secer,
    /// isin sensoruyle gorulmeden yaklasip anahtari calar ve oyuncuya getirir.
    /// Karar mantigi durum makinesindedir (bkz. States klasoru).
    /// </summary>
    [RequireComponent(typeof(NavMeshNavigator), typeof(RaySensor2D), typeof(CharacterAnimator))]
    public class RatAgent : MonoBehaviour, ICommandable
    {
        [SerializeField] private PlayerController owner;
        [SerializeField] private string keyId = KeyIds.Cell;

        [Header("Mesafeler")]
        [SerializeField] private float stealRange = 0.75f;
        [SerializeField] private float deliverRange = 0.9f;
        [SerializeField] private float followDistance = 1.2f;
        [SerializeField] private float dangerRange = 4f;

        [Header("Zamanlama")]
        [SerializeField] private float sensorInterval = 0.1f;
        [SerializeField] private float missionTimeout = 60f;

        private readonly StateMachine _machine = new();
        private CharacterAnimator _animator;
        private float _sensorTimer;
        private float _missionStart;
        private bool _onMission;

        public INavigator Navigator { get; private set; }
        public RaySensor2D Sensor { get; private set; }
        public ITargetSelector Selector { get; set; }
        public KeyRing Keys { get; } = new();
        public IKeyHolder OwnerKeys => owner.Keys;
        public Vector2 OwnerPosition => owner.transform.position;
        public Vector2 Position => transform.position;
        public string KeyId => keyId;
        public IGuard Target { get; set; }
        public float TargetPathLength { get; set; }
        public int HideCount { get; set; }

        public float StealRange => stealRange;
        public float DeliverRange => deliverRange;
        public float FollowDistance => followDistance;
        public float DangerRange => dangerRange;

        internal RatIdleState Idle { get; private set; }
        internal RatSelectTargetState SelectTarget { get; private set; }
        internal RatApproachState Approach { get; private set; }
        internal RatHideState Hide { get; private set; }
        internal RatStealState Steal { get; private set; }
        internal RatReturnState Return { get; private set; }
        internal RatDeliverState Deliver { get; private set; }

        public string StateName => _machine.Current?.Name ?? "-";
        public bool IsOnMission => _onMission;
        public string Status { get; private set; } = "Fare hazır. F: anahtarı getir";

        public event Action<RatMissionReport> MissionFinished;
        public event Action<string> StatusChanged;
        public event Action<string, string> StateChanged;

        public bool CanAcceptCommand =>
            !_onMission && !OwnerKeys.HasKey(keyId) && !Keys.HasKey(keyId) && _machine.Current == Idle;

        public void SetOwner(PlayerController player) => owner = player;

        private void Awake()
        {
            Navigator = GetComponent<NavMeshNavigator>();
            Sensor = GetComponent<RaySensor2D>();
            _animator = GetComponent<CharacterAnimator>();
            Selector = new NearestByPathSelector(Navigator);

            Idle = new RatIdleState(this);
            SelectTarget = new RatSelectTargetState(this);
            Approach = new RatApproachState(this);
            Hide = new RatHideState(this);
            Steal = new RatStealState(this);
            Return = new RatReturnState(this);
            Deliver = new RatDeliverState(this);

            _machine.StateChanged += (from, to) => StateChanged?.Invoke(from?.Name, to.Name);
        }

        private void OnEnable()
        {
            if (owner != null) owner.CommandIssued += Command;
        }

        private void OnDisable()
        {
            if (owner != null) owner.CommandIssued -= Command;
        }

        private void Start() => _machine.ChangeState(Idle);

        public void Command()
        {
            if (!CanAcceptCommand)
            {
                SetStatus(_onMission ? "Fare zaten görevde." : "Anahtar zaten elinde.");
                return;
            }
            _onMission = true;
            _missionStart = Time.time;
            HideCount = 0;
            TargetPathLength = 0f;
            ChangeState(SelectTarget);
        }

        private void Update()
        {
            _sensorTimer -= Time.deltaTime;
            if (_sensorTimer <= 0f)
            {
                Sensor.Sense();
                _sensorTimer = sensorInterval;
            }

            if (_onMission && Time.time - _missionStart > missionTimeout)
                FailMission("Zaman aşımı");

            _machine.Tick(Time.deltaTime);
            _animator.SetMotion(Navigator.Velocity, Time.deltaTime);
        }

        internal void ChangeState(IState next) => _machine.ChangeState(next);

        internal void SetStatus(string status)
        {
            if (status == Status) return;
            Status = status;
            StatusChanged?.Invoke(status);
        }

        internal void CompleteMission()
        {
            FinishMission(true, null);
            SetStatus("Fare anahtarı getirdi!");
            ChangeState(Idle);
        }

        internal void FailMission(string reason)
        {
            FinishMission(false, reason);
            SetStatus($"Fare başaramadı: {reason}");
            Target = null;
            // Anahtar fareyle kaldiysa yine de oyuncuya goturur
            ChangeState(Keys.HasKey(keyId) ? Return : Idle);
        }

        private void FinishMission(bool success, string reason)
        {
            if (!_onMission) return;
            _onMission = false;
            var report = new RatMissionReport(success, Time.time - _missionStart, TargetPathLength, HideCount, reason);
            Debug.Log($"[Fare] Gorev {(success ? "basarili" : "basarisiz")}: {report.Duration:F1} sn, yol {report.TargetPathLength:F1} m, saklanma {report.HideCount}{(reason != null ? ", " + reason : "")}");
            MissionFinished?.Invoke(report);
        }
    }
}
