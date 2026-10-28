using System;
using DungeonEscape.Characters;
using DungeonEscape.Core;
using DungeonEscape.Interaction;
using UnityEngine;

namespace DungeonEscape.Player
{
    /// <summary>
    /// Oyuncu hareketi ve eylem yonlendirmesi. Fizik Rigidbody2D ile; etkilesim (kapi, muhafiz) Interactor'a,
    /// fare komutu ise abonelere olay olarak iletilir.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterAnimator), typeof(Interactor))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.5f;

        private Rigidbody2D _body;
        private CharacterAnimator _animator;
        private Interactor _interactor;
        private IPlayerInput _input;
        private Vector2 _moveInput;
        private bool _frozen;

        public KeyRing Keys { get; } = new();
        public Vector2 LookDirection => _animator.LookDirection;

        /// <summary>F tusu: fareye gorev ver.</summary>
        public event Action CommandIssued;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _animator = GetComponent<CharacterAnimator>();
            _interactor = GetComponent<Interactor>();
            _input = GetComponent<IPlayerInput>();
            _interactor.Bind(Keys);
        }

        /// <summary>Girdi kaynagini degistirir (testlerde sahte girdi).</summary>
        public void SetInput(IPlayerInput input) => _input = input;

        private void OnEnable()
        {
            GameEvents.DialogueStarted += Freeze;
            GameEvents.DialogueEnded += Unfreeze;
            GameEvents.GameWon += Freeze;
            GameEvents.GameLost += OnGameLost;
        }

        private void OnDisable()
        {
            GameEvents.DialogueStarted -= Freeze;
            GameEvents.DialogueEnded -= Unfreeze;
            GameEvents.GameWon -= Freeze;
            GameEvents.GameLost -= OnGameLost;
        }

        private void Update()
        {
            if (_input == null) return;

            if (_input.RestartPressed && GameFlow.Instance != null && GameFlow.Instance.IsOver)
            {
                GameFlow.Instance.Restart();
                return;
            }

            _moveInput = _frozen ? Vector2.zero : Vector2.ClampMagnitude(_input.Move, 1f);
            _animator.SetMotion(_moveInput, Time.deltaTime);
            if (_frozen) return;

            if (_input.InteractPressed) _interactor.TryInteract();
            if (_input.CommandPressed) CommandIssued?.Invoke();
        }

        private void FixedUpdate() => _body.linearVelocity = _moveInput * moveSpeed;

        private void Freeze()
        {
            _frozen = true;
            _input?.SetGameplayEnabled(false);
        }

        private void Unfreeze()
        {
            _frozen = false;
            _input?.SetGameplayEnabled(true);
        }

        private void OnGameLost(LoseReason reason) => Freeze();
    }
}
