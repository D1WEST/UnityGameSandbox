using UnityEngine;

namespace Assets.Modules.PlayerModule
{
    public class PlayerCameraService : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("Look")]
        [SerializeField] private float _maxLookAngle = 80f;
        [SerializeField] private float _minLookAngle = -80f;
        [SerializeField] private float _sensetivity = 1f;
        [SerializeField] private float _lookSmoothTime = 0.01f;

        private float _xRotation = 0f;
        private float _yRotation = 0f;
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;

        /// <summary>
        /// Look action.
        /// </summary>
        public void Look(Vector2 LookVectorDelta)
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
    }
}
