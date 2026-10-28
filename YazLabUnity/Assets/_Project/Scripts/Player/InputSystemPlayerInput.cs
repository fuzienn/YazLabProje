using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonEscape.Player
{
    /// <summary>InputSystem_Actions varligindaki "Player" haritasini IPlayerInput'a baglar.</summary>
    public class InputSystemPlayerInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private InputActionAsset actions;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _interact;
        private InputAction _command;
        private InputAction _restart;

        public Vector2 Move => _map.enabled ? _move.ReadValue<Vector2>() : Vector2.zero;
        public bool InteractPressed => _interact.WasPressedThisFrame();
        public bool CommandPressed => _command.WasPressedThisFrame();
        public bool RestartPressed => _restart.WasPressedThisFrame();

        public void SetActions(InputActionAsset asset) => actions = asset;

        private void Awake()
        {
            _map = actions.FindActionMap("Player", true);
            _move = _map.FindAction("Move", true);
            _interact = _map.FindAction("Interact", true);
            _command = _map.FindAction("Command", true);

            // Yeniden baslatma diyalog sirasinda da calismali; ayri tutulur
            _restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
        }

        private void OnEnable()
        {
            _map.Enable();
            _restart.Enable();
        }

        private void OnDisable()
        {
            _map.Disable();
            _restart.Disable();
        }

        private void OnDestroy() => _restart.Dispose();

        public void SetGameplayEnabled(bool enabled)
        {
            if (enabled) _map.Enable();
            else _map.Disable();
        }
    }
}
