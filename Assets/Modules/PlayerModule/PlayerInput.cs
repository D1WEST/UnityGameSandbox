namespace Assets.Modules.PlayerModule
{
    using UnityEngine;

    [RequireComponent(typeof(PlayerLocomotion))]
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputActions _playerInputActions;
        [SerializeField] private PlayerLocomotion _playerLocomotion;

        /// <summary>
        /// On input interaction enabled.
        /// </summary>
        private void OnEnable()
        {
            if (_playerInputActions == null)
            {
                _playerInputActions = new();
            }
            _playerInputActions.PlayerMovementActions.Enable();
            _playerInputActions.PlayerMovementActions.Jump.performed += _playerLocomotion.DoJump;
            _playerInputActions.PlayerMovementActions.Crouch.performed += _playerLocomotion.DoCrouch;
            _playerInputActions.PlayerMovementActions.Sprint.performed += _playerLocomotion.DoSprint;
            _playerInputActions.PlayerMovementActions.Sprint.canceled += _playerLocomotion.DoSprint;
        }

        /// <summary>
        /// On input interaction disabled.
        /// </summary>
        private void OnDisable()
        {
            _playerInputActions.PlayerMovementActions.Disable();
            _playerInputActions.PlayerMovementActions.Jump.performed -= _playerLocomotion.DoJump;
            _playerInputActions.PlayerMovementActions.Crouch.performed -= _playerLocomotion.DoCrouch;
            _playerInputActions.PlayerMovementActions.Sprint.performed -= _playerLocomotion.DoSprint;
            _playerInputActions.PlayerMovementActions.Sprint.canceled -= _playerLocomotion.DoSprint;
        }

        private void FixedUpdate()
        {
            _playerLocomotion.MovementVector = _playerInputActions.PlayerMovementActions.Move.ReadValue<Vector2>().normalized;
        }

        private void Start()
        {
            gameObject.GetComponent<PlayerLocomotion>();
        }
    }
}
