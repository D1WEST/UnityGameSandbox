using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Modules.PlayerModule
{
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.InputSystem;

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAnimation))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("DoNotTouch")]
        public Vector2 MovementVector { get; set; } = Vector2.zero;
        public Vector2 LookVectorDelta {get;set;} = Vector2.zero;
        private float _xRotation = 0f;
        private float _yRotation = 0f;
        private CancellationTokenSource _crouchCTS;

        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;


        [Header("Movement")]
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _crouchSpeed = 2f;
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
        private bool isCrouching = false;
        private bool _isInDuckPosition = false;

        [Header("Look")]
        [SerializeField] private float _maxLookAngle = 80f;
        [SerializeField] private float _minLookAngle = -80f;
        [SerializeField] private float _sensetivity = 1f;
        [SerializeField] private float _lookSmoothTime = 0.01f;

        [Header("CameraPhysics")]
        [SerializeField] private float _headSize = 0.25f;
        [SerializeField] private float _bodySize = 1.75f;
        [Range(0,1)]
        [SerializeField] private float _crouchToStandRatio = 0.4f;

        [SerializeField] private float _crouchSmoothTime = 0.05f;

        [Header("Animation")] 
        [SerializeField] private PlayerAnimation _playerAnimation;

        private void Start()
        {
            _selectedSpeed = _walkSpeed;
            if (_controller == null) _controller = GetComponent<CharacterController>();
            if (_camera == null) _camera = GetComponent<Camera>();
            if(_playerAnimation == null) _playerAnimation = GetComponent<PlayerAnimation>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Look();
            Move();
        }

        /// <summary>
        /// Updates camera y position smoothly.
        /// </summary>
        /// <param name="endingCrouch">Is crouch ending?</param>
        /// <param name="targetY">Target position.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns></returns>
        private async UniTask SmoothCameraCrouch(bool endingCrouch,float targetY, CancellationToken ct)
        {
            // »спользуем Transform.localPosition напр€мую дл€ скорости
            Transform camTransform = _camera.transform;
            float currentVelocity = 0f;

            // ÷икл работает, пока не достигнет цели или не будет отменен
            while (Mathf.Abs(camTransform.localPosition.y - targetY) > 0.001f)
            {
                if (!endingCrouch)
                {
                    float newY = Mathf.SmoothDamp(camTransform.localPosition.y, targetY,
                        ref currentVelocity, _crouchSmoothTime);

                    camTransform.localPosition =
                        new Vector3(camTransform.localPosition.x, newY, camTransform.localPosition.z);
                    _isInDuckPosition = true;
                    _controller.height = (_bodySize + _headSize) * _crouchToStandRatio;
                    _controller.center = new Vector3(0, -0.5f* (_bodySize + _headSize) * _crouchToStandRatio,0);
                }
                else
                {
                    RaycastHit hit;
                    Vector3 pointToActivate = transform.position;
                    if (!Physics.SphereCast(pointToActivate, _headSize / 2f, Vector3.up, out hit, 1,
                            LayerMask.GetMask("Environment")))
                    {
                        float newY = Mathf.SmoothDamp(camTransform.localPosition.y, targetY,
                            ref currentVelocity, _crouchSmoothTime);

                        camTransform.localPosition =
                            new Vector3(camTransform.localPosition.x, newY, camTransform.localPosition.z);
                        _isInDuckPosition = false;
                        _controller.height = _bodySize + _headSize;
                        _controller.center = Vector3.zero;
                    }
                }

                // await UniTask.Yield делает паузу до следующего кадра, не блокиру€ поток
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        /// <summary>
        /// Triggers crouch camera animation.
        /// </summary>
        /// <param name="endingCrouch">Is crouch ending?</param>
        /// <param name="targetY">Target camera position.</param>
        private void TriggerCrouchAnimation(bool endingCrouch, float targetY)
        {
            _crouchCTS?.Cancel();
            _crouchCTS = new CancellationTokenSource();

            SmoothCameraCrouch(endingCrouch, targetY, _crouchCTS.Token).Forget();
        }

        /// <summary>
        /// Jump action.
        /// </summary>
        /// <param name="obj">Callback.</param>
        public void DoJump(InputAction.CallbackContext obj)
        {
            if (_controller.isGrounded && !isCrouching)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
        }

        /// <summary>
        /// Crouch action.
        /// </summary>
        /// <param name="obj">Callback.</param>
        public void DoCrouch(InputAction.CallbackContext obj)
        {
            isCrouching = true;
            _selectedSpeed = _crouchSpeed;
            TriggerCrouchAnimation(false,(-0.5f * _bodySize) + ((_bodySize - _headSize / 2) * _crouchToStandRatio));
        }

        /// <summary>
        /// Crouch stop action.
        /// </summary>
        /// <param name="obj">Callback.</param>
        public void StopCrouch(InputAction.CallbackContext obj)
        {
            isCrouching = false;
            _selectedSpeed = _walkSpeed;
            TriggerCrouchAnimation(true, (-0.5f * _bodySize) + (_bodySize - (_headSize / 2)));
        }

        /// <summary>
        /// Sprint action.
        /// </summary>
        /// <param name="obj">Callback.</param>
        public void DoSprint(InputAction.CallbackContext obj)
        {
            if (!isCrouching)
            {
                if (obj.performed) _selectedSpeed = _runSpeed;
                else if (obj.canceled) _selectedSpeed = _walkSpeed;
            }
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
            _playerAnimation.UpdateAnimatorValues(MovementVector.x, MovementVector.y, _selectedSpeed == _runSpeed, _isInDuckPosition, _controller.isGrounded);
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
