using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Modules.PlayerModule
{
    using UnityEngine;
    using UnityEngine.InputSystem;

    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("DoNotTouch")]
        public Vector2 MovementVector { get; set; } = Vector2.zero;
        public Vector2 LookVectorDelta {get;set;} = Vector2.zero;
        private float _xRotation = 0f;
        private float _yRotation = 0f;

        private float VerticalMovement { get; set; }
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;

        [Header("Movement")]
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _runSpeed = 7f;
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Camera _camera;

        [Header("Movement Physics")]
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _deceleration = 10f;
        [Range(0f, 1f)]
        [SerializeField] private float _airControlMultiplier = 0.2f;

        private float _selectedSpeed = 0f;
        private Vector3 _velocity;
        private Vector3 _currentHorizontalVelocity;

        [Header("Look")]
        [SerializeField] private float _maxLookAngle = 80f;
        [SerializeField] private float _minLookAngle = -80f;
        [SerializeField] private float _sensetivity = 1f;
        [SerializeField] private float _lookSmoothTime = 0.01f;

        private void Start()
        {
            _selectedSpeed = _walkSpeed;
            if (_controller == null) _controller = GetComponent<CharacterController>();
            if (_camera == null) _camera = GetComponent<Camera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Look();
            Move();
        }

        /// <summary>
        /// Jump action.
        /// </summary>
        /// <param name="obj">Callback - do not use.</param>
        public void DoJump(InputAction.CallbackContext obj)
        {
            if (_controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
        }

        /// <summary>
        /// Crouch action.
        /// </summary>
        /// <param name="obj">Callback - do not use.</param>
        public void DoCrouch(InputAction.CallbackContext obj)
        {
            Debug.Log("Crouched");
        }

        /// <summary>
        /// Sprint action.
        /// </summary>
        /// <param name="obj">Callback - do not use.</param>
        public void DoSprint(InputAction.CallbackContext obj)
        {
            if (obj.performed) _selectedSpeed = _runSpeed;
            if (obj.canceled) _selectedSpeed = _walkSpeed;
        }

        /// <summary>
        /// Move action.
        /// </summary>
        public void Move()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
            Vector3 inputDirection = transform.right * MovementVector.x + transform.forward * MovementVector.y;
            if (inputDirection.magnitude > 1f)
            {
                inputDirection.Normalize();
            }

            Vector3 targetVelocity = inputDirection * _selectedSpeed;
            float speedChangeRate = MovementVector.magnitude > 0.1f ? _acceleration : _deceleration;

            if (!_controller.isGrounded)
            {
                speedChangeRate *= _airControlMultiplier;
            }
            _currentHorizontalVelocity = Vector3.Lerp(_currentHorizontalVelocity, targetVelocity, speedChangeRate * Time.deltaTime);

            _velocity.y += _gravity * Time.deltaTime;
            Vector3 finalVelocity = _currentHorizontalVelocity + new Vector3(0, _velocity.y, 0);
            _controller.Move(finalVelocity * Time.deltaTime);
        }

        /// <summary>
        /// Look action.
        /// </summary>
        public void Look()
        {
            _currentMouseDelta = Vector2.SmoothDamp(_currentMouseDelta, LookVectorDelta, ref _currentMouseDeltaVelocity, _lookSmoothTime);

            float mouseX = _currentMouseDelta.x * _sensetivity/4;
            float mouseY = _currentMouseDelta.y * _sensetivity/4;

            _yRotation += mouseX;
            _xRotation -= mouseY;

            _xRotation = Mathf.Clamp(_xRotation, _minLookAngle, _maxLookAngle);

            _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            transform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
        }

    }
}
