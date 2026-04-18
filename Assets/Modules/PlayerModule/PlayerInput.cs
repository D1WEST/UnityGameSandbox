namespace Assets.Modules.PlayerModule
{
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.InputSystem;

    [RequireComponent(typeof(PlayerLocomotion))]
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputActions _playerInputActions;
        [SerializeField] private PlayerLocomotion _playerLocomotion;
        [SerializeField] private PlayerCameraService _playerCamera;

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
            _playerInputActions.PlayerCameraActions.Enable();
            _playerInputActions.PlayerMovementActions.Jump.performed += _playerLocomotion.DoJump;
            _playerInputActions.PlayerMovementActions.Crouch.performed += _playerLocomotion.DoCrouch;
            _playerInputActions.PlayerMovementActions.Crouch.canceled += _playerLocomotion.StopCrouch;
            _playerInputActions.PlayerMovementActions.Sprint.performed += _playerLocomotion.DoSprint;
            _playerInputActions.PlayerMovementActions.Sprint.canceled += _playerLocomotion.DoSprint;
            _playerInputActions.PlayerCameraActions.ChangeView.started += _playerCamera.TriggerViewTypeChange;
            _playerInputActions.PlayerCameraActions.ChangeLookDistance.performed += OnScroll;
        }

        /// <summary>
        /// On input interaction disabled.
        /// </summary>
        private void OnDisable()
        {
            _playerInputActions.PlayerMovementActions.Disable();
            _playerInputActions.PlayerCameraActions.Disable();
            _playerInputActions.PlayerMovementActions.Jump.performed -= _playerLocomotion.DoJump;
            _playerInputActions.PlayerMovementActions.Crouch.performed -= _playerLocomotion.DoCrouch;
            _playerInputActions.PlayerMovementActions.Crouch.canceled -= _playerLocomotion.StopCrouch;
            _playerInputActions.PlayerMovementActions.Sprint.performed -= _playerLocomotion.DoSprint;
            _playerInputActions.PlayerMovementActions.Sprint.canceled -= _playerLocomotion.DoSprint;
            _playerInputActions.PlayerCameraActions.ChangeView.started -= _playerCamera.TriggerViewTypeChange;
            _playerInputActions.PlayerCameraActions.ChangeLookDistance.performed -= OnScroll;
        }

        private void Update()
        {
            _playerLocomotion.MovementVector = _playerInputActions.PlayerMovementActions.Move.ReadValue<Vector2>();
            _playerLocomotion.LookVectorDelta = _playerInputActions.PlayerMovementActions.Look.ReadValue<Vector2>();
        }

        private void Start()
        {
            if (_playerLocomotion == null) 
            { 
                _playerLocomotion = GetComponent<PlayerLocomotion>();
            }

            if (_playerInputActions == null) 
            { 
                _playerInputActions = new PlayerInputActions();
            }
        }

        private void OnScroll(InputAction.CallbackContext context)
        {
            float scrollValue = context.ReadValue<float>();
            _playerCamera.Zoom(scrollValue);
        }
    }
}
