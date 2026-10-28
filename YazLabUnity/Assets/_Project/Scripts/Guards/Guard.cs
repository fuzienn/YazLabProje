using DungeonEscape.Characters;
using DungeonEscape.Core;
using DungeonEscape.Guards.States;
using DungeonEscape.Interaction;
using DungeonEscape.Navigation;
using UnityEngine;

namespace DungeonEscape.Guards
{
    /// <summary>
    /// Devriye gezen, anahtar tasiyan muhafiz. Oyuncuyu gorus konisinde bir sure tutarsa yakalar.
    /// Arkasindan yaklasan oyuncu E ile etkisiz hale getirebilir.
    /// </summary>
    [RequireComponent(typeof(NavMeshNavigator), typeof(CharacterAnimator))]
    public class Guard : MonoBehaviour, IGuard, IInteractable
    {
        [SerializeField] private float patrolRadius = 3f;
        [SerializeField] private float catchTime = 0.6f;
        [SerializeField] private float takedownRange = 1.1f;
        [Tooltip("Oyuncu ile muhafizin bakis yonu arasindaki bu acidan buyukse 'arkada' sayilir.")]
        [SerializeField] private float behindAngle = 110f;

        private readonly StateMachine _machine = new();
        private CharacterAnimator _animator;
        private GuardVision _vision;
        private Transform _player;
        private float _suspicion;

        public KeyRing KeyRing { get; } = new();
        public IKeyHolder Keys => KeyRing;
        public Transform Transform => transform;
        public Vector2 LookDirection => _vision.Direction;
        public bool IsActive { get; private set; } = true;
        public string StateName => _machine.Current?.Name ?? "-";

        public INavigator Navigator { get; private set; }
        public Vector2 Home { get; private set; }
        public float PatrolRadius => patrolRadius;
        public System.Random Random { get; private set; } = new();

        internal GuardPatrolState Patrol { get; private set; }
        internal GuardLookAroundState LookAround { get; private set; }
        internal GuardNeutralizedState Neutralized { get; private set; }

        /// <summary>Spawner'dan cagrilir: devriye merkezi ve tekrarlanabilir rastgelelik.</summary>
        public void Init(Vector2 home, int seed)
        {
            Home = home;
            Random = new System.Random(seed);
        }

        private void Awake()
        {
            Navigator = GetComponent<NavMeshNavigator>();
            _animator = GetComponent<CharacterAnimator>();
            _vision = GetComponentInChildren<GuardVision>();
            Home = transform.position;
            KeyRing.AddKey(KeyIds.Cell);

            Patrol = new GuardPatrolState(this);
            LookAround = new GuardLookAroundState(this);
            Neutralized = new GuardNeutralizedState(this);
        }

        private void OnEnable() => GuardRegistry.Register(this);
        private void OnDisable() => GuardRegistry.Unregister(this);

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) _player = player.transform;
            _machine.ChangeState(LookAround);
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
            var velocity = Navigator.Velocity;
            if (velocity.sqrMagnitude > 0.01f) Look(velocity);
            _animator.SetMotion(velocity, Time.deltaTime);
            if (IsActive) WatchForPlayer(Time.deltaTime);
        }

        internal void ChangeState(IState next) => _machine.ChangeState(next);

        /// <summary>Hem sprite yonunu hem gorus konisini cevirir.</summary>
        internal void Look(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            _vision.Direction = direction.normalized;
            _animator.Face(direction);
        }

        public bool CanSee(Vector2 point) => IsActive && _vision.CanSee(point);

        private void WatchForPlayer(float deltaTime)
        {
            var flow = GameFlow.Instance;
            if (_player == null || flow == null || flow.IsOver) return;

            bool seen = flow.Phase == GamePhase.Escaping && CanSee(_player.position);
            _suspicion = seen ? _suspicion + deltaTime : Mathf.Max(0f, _suspicion - deltaTime);
            _vision.SetAlert(_suspicion > 0f);

            if (seen) Look((Vector2)_player.position - (Vector2)transform.position);
            if (_suspicion >= catchTime) GameEvents.RaiseGameLost(LoseReason.Caught);
        }

        public bool IsBehind(Vector2 point)
        {
            Vector2 toPoint = point - (Vector2)transform.position;
            return toPoint.magnitude <= takedownRange && Vector2.Angle(LookDirection, toPoint) >= behindAngle;
        }

        public bool CanInteract(Interactor interactor) => IsActive;

        public string GetPrompt(Interactor interactor) =>
            IsBehind(interactor.transform.position)
                ? "E: Muhafızı etkisiz hâle getir"
                : "Arkasından yaklaş!";

        public void Interact(Interactor interactor)
        {
            if (!IsActive || !IsBehind(interactor.transform.position)) return;
            ChangeState(Neutralized);
        }

        /// <summary>Etkisiz hale gelir: gorus kapanir, collider devre disi, bir daha yakalayamaz.</summary>
        internal void OnNeutralized()
        {
            IsActive = false;
            _suspicion = 0f;
            _vision.SetVisible(false);
            Navigator.Stop();
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            var sprite = GetComponent<SpriteRenderer>();
            sprite.color = new Color(0.55f, 0.55f, 0.6f);
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            GameEvents.RaiseGuardNeutralized();
        }
    }
}
