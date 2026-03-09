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
        private float VerticalMovement { get; set; }

        [Header("Movement")]
        [SerializeField] private float _gravity = -9.8f;
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _runSpeed = 12f;
        [SerializeField] private CharacterController _controller;
        private float _selectedSpeed = 6f;

        private void Start()
        {
            gameObject.GetComponent<CharacterController>();
        }

        private void Update()
        {
            Move();
        }

        /// <summary>
        /// Jump action.
        /// </summary>
        /// <param name="obj">Callback - do not use.</param>
        public void DoJump(InputAction.CallbackContext obj)
        {
            Debug.Log("Jumped");
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
            if (obj.performed)
            {
                _selectedSpeed = _runSpeed;
            }

            if (obj.canceled)
            {
                _selectedSpeed = _walkSpeed;
            }
        }

        /// <summary>
        /// Move action.
        /// </summary>
        public void Move()
        {
            Vector3 movement = new Vector3(MovementVector.x * _selectedSpeed, 0, MovementVector.y * _selectedSpeed);
            movement = Vector3.ClampMagnitude(movement, _selectedSpeed);
            movement.y = _gravity + VerticalMovement;
            movement *= Time.deltaTime;
            movement = transform.TransformDirection(movement);
            _controller.Move(movement);
        }
    }
}
