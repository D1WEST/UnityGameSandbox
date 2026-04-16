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
        [SerializeField] private Vector3 _fppOffset  = new Vector3(0, 0.592f, 0);
        [SerializeField] private Vector3 _ttpOffset = new Vector3(-3f, 1.5f, 0);
        [SerializeField] private Vector3 _ttpRotation = new Vector3(0f, 90f, 0);


        [Header("Parenting dot")]
        [SerializeField] private Transform _ttpParent;

        [Header("Look type")]
        [SerializeField] private LookType _lookType = LookType.FPP;

        private Action<Vector2> _lookAction;

        private float _xRotation = 0f;
        private float _yRotation = 0f;
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;


        public void Instantiate()
        {
            ChangeLookPerspective(_lookType);
            SetLookDefaults(_lookType);
        }


        

        /// <summary>
        /// Unified look action.
        /// </summary>
        /// <param name="lookVectorDelta"></param>
        public void Look(Vector2 lookVectorDelta)
        {
            _lookAction.Invoke(lookVectorDelta);
        }

        /// <summary>
        /// Changes eefaults of camera placement and mask/
        /// </summary>
        /// <param name="lookType">Looktype to change.</param>
        private void SetLookDefaults(LookType lookType)
        {
            switch (lookType)
            {
                case LookType.FPP:
                    SetCameraParent(transform, _fppOffset, Vector3.zero);
                    gameObject.layer = 6;
                    foreach (var child in gameObject.GetComponentsInChildren<Transform>())
                    {
                        child.gameObject.layer = 6;
                    }
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case LookType.TTP:
                    SetCameraParent(transform, _ttpOffset, _ttpRotation);
                    gameObject.layer = 7;
                    foreach (var child in gameObject.GetComponentsInChildren<Transform>())
                    {
                        child.gameObject.layer = 7;
                    }
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    break;
                default:
                    break;


            }
        }

        private void ChangeLookPerspective(LookType lookType)
        {

            switch (lookType)
            {
                case LookType.FPP:
                    _lookType = lookType;
                    _lookAction = LookFPP;
                    break;
                case LookType.TTP:
                    _lookType = lookType;
                    _lookAction = LookTTP;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sets parent to camera.
        /// </summary>
        /// <param name="parent">New parent.</param>
        /// <param name="offset">Local camera offset.</param>
        /// <param name="rotation">Local camera rot.</param>
        private void SetCameraParent(Transform parent, Vector3 offset, Vector3 rotation)
        {
            _camera.transform.SetParent(parent);
            _camera.transform.localPosition = offset;
            _camera.transform.localRotation = Quaternion.Euler(rotation);
        }

        /// <summary>
        /// First person look action.
        /// </summary>
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

        /// <summary>
        /// Third top person look action.
        /// </summary>
        private void LookTTP(Vector2 LookVectorDelta)
        {
            
        }
    }
}
