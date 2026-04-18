using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Modules.PlayerModule
{
    public enum LookType
    {
        FPP,
        TTP
    }

    public class PlayerCameraService : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Look")]
        [SerializeField] private float _maxLookAngle = 80f;
        [SerializeField] private float _minLookAngle = -80f;
        [SerializeField] private float _sensetivity = 1f;
        [SerializeField] private float _lookSmoothTime = 0.01f;

        [Header("FPP Settings")]
        [SerializeField] private Vector3 _fppOffset = new Vector3(0, 0.592f, 0);

        [Header("TTP Settings")]
        [SerializeField] private Vector3 _ttpOffsetDir = new Vector3(-1f, 0.5f, 0f);
        [SerializeField] private Vector3 _ttpRotation = new Vector3(30f, 90f, 0);

        [Header("TTP Smooth Follow")]
        [SerializeField] private float _followSmoothTime = 0.1f;
        private Vector3 _cameraVelocity;

        [Header("TTP Zoom")]
        [SerializeField] private float _minZoom = 1f;
        [SerializeField] private float _maxZoom = 15f;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _currentZoom = 7f;

        [Header("TTP Collision")]
        [SerializeField] private bool _enableCollision = true;
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField] private float _cameraRadius = 0.3f;

        [Header("Look type")]
        [SerializeField] public LookType _lookType = LookType.FPP;

        private Action<Vector2> _lookAction;
        private float _xRotation = 0f;
        private float _yRotation = 0f;
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;

        // Переменная для плавного поворота персонажа в TTP
        private float _turnSmoothVelocity;

        /// <summary>
        /// Changes view perspective.
        /// </summary>
        /// <param name="obj">Callback.</param>
        public void TriggerViewTypeChange(InputAction.CallbackContext obj)
        {
            _lookType = _lookType == LookType.FPP ? LookType.TTP : LookType.FPP;
            Initialize();
        }

        public void Initialize()
        {
            ChangeLookPerspective(_lookType);
            SetLookDefaults(_lookType);
        }

        public void Look(Vector2 lookVectorDelta)
        {
            _lookAction?.Invoke(lookVectorDelta);
        }

        // В LateUpdate мы двигаем камеру ЗА игроком, но НЕ крутим её
        private void LateUpdate()
        {
            if (_lookType == LookType.TTP)
            {
                HandleTTPCamera();
            }
        }
        public void Zoom(float scrollValue)
        {
            if (_lookType != LookType.TTP) return;
            float scrollSign = Mathf.Sign(scrollValue);
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                _currentZoom -= scrollSign * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            }
        }

        /// <summary>
        /// Handles zoom and collision actions.
        /// </summary>
        private CharacterController _cc;

        private void Start()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void HandleTTPCamera()
        {
            Vector3 desiredOffset = _ttpOffsetDir * _currentZoom;

            float focusHeight = _cc != null ? _cc.height * 0.8f : 1.5f;
            Vector3 playerFocusPoint = transform.position + Vector3.up * focusHeight;

            Vector3 targetCameraPosition = playerFocusPoint + desiredOffset;

            if (_enableCollision)
            {
                Vector3 direction = targetCameraPosition - playerFocusPoint;
                float distance = direction.magnitude;
                if (Physics.SphereCast(playerFocusPoint, _cameraRadius, direction.normalized, out RaycastHit hit, distance, _collisionMask))
                {
                    targetCameraPosition = playerFocusPoint + direction.normalized * hit.distance;
                }
            }

            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                targetCameraPosition,
                ref _cameraVelocity,
                _followSmoothTime
            );
        }

        private void SetLookDefaults(LookType lookType)
        {
            switch (lookType)
            {
                case LookType.FPP:
                    // В первом лице камера жестко привязана к игроку
                    _camera.transform.SetParent(transform);
                    _camera.transform.localPosition = _fppOffset;
                    _camera.transform.localRotation = Quaternion.identity;

                    gameObject.layer = 6;
                    SetLayerRecursively(gameObject, 6);

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                case LookType.TTP:
                    // В третьем лице мы ОТВЯЗЫВАЕМ камеру от игрока, чтобы она не крутилась вместе с ним
                    _camera.transform.SetParent(null);
                    _camera.transform.rotation = Quaternion.Euler(_ttpRotation);

                    gameObject.layer = 7;
                    SetLayerRecursively(gameObject, 7);

                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    break;
            }
        }

        /// <summary>
        /// Changes gameobject level.
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = newLayer;
            }
        }

        private void ChangeLookPerspective(LookType lookType)
        {
            _lookType = lookType;
            switch (lookType)
            {
                case LookType.FPP:
                    _lookAction = LookFPP;
                    break;
                case LookType.TTP:
                    _lookAction = LookTTP;
                    break;
            }
        }

        public Vector2 SelectOperatingVector(Vector2 lookVector, Vector2 moveVector)
        {
            return _lookType == LookType.FPP ? lookVector : moveVector;
        }

        private void LookFPP(Vector2 LookVectorDelta)
        {
            _currentMouseDelta = Vector2.SmoothDamp(_currentMouseDelta, LookVectorDelta, ref _currentMouseDeltaVelocity, _lookSmoothTime);

            float mouseX = _currentMouseDelta.x * _sensetivity / 4;
            float mouseY = _currentMouseDelta.y * _sensetivity / 4;

            _yRotation += mouseX;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, _minLookAngle, _maxLookAngle);

            _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
        }

        private void LookTTP(Vector2 MovementVector)
        {
            if (MovementVector.sqrMagnitude < 0.01f)
                return;

            Vector2 resultMovementVector = new Vector2(MovementVector.y, -MovementVector.x);

            float targetAngle = Mathf.Atan2(resultMovementVector.x, resultMovementVector.y) * Mathf.Rad2Deg;

            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}