using System;
using UnityEngine;

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
        [SerializeField] private Vector3 _ttpOffset = new Vector3(0f, 10f, -5f); // Настрой под себя (Высота и отдаление)
        [SerializeField] private Vector3 _ttpRotation = new Vector3(60f, 0f, 0); // Смотрит вниз под углом

        [Header("Look type")]
        [SerializeField] public LookType _lookType = LookType.FPP;

        private Action<Vector2> _lookAction;
        private float _xRotation = 0f;
        private float _yRotation = 0f;
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;

        // Переменная для плавного поворота персонажа в TTP
        private float _turnSmoothVelocity;

        public void Instantiate()
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
                // Камера просто висит со смещением от глобальной позиции игрока
                _camera.transform.position = transform.position + _ttpOffset;
            }
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
            Vector2 resultMovementVector = new Vector3(MovementVector.y, -MovementVector.x);

            // Вычисляем угол поворота на основе WASD (Вектор движения)
            float targetAngle = Mathf.Atan2(resultMovementVector.x, resultMovementVector.y) * Mathf.Rad2Deg;

            // Плавно поворачиваем персонажа в сторону движения
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}