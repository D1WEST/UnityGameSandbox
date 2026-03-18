namespace Assets.Modules.PlayerModule
{
    using UnityEngine;
    public class PlayerAnimation : MonoBehaviour
    {
        public Animator _animator;

        int _horizontal;
        int _vertical;
        int _modifier; 

        private void Start()
        {
            _horizontal = Animator.StringToHash("Horizontal");
            _vertical = Animator.StringToHash("Vertical");
        }

        public void UpdateAnimatorValues(float horizontalMovement, float verticalMovement, bool isSprinting,
            bool isDucking, bool isGrounded)
        {
            if (isSprinting)
            {
                _modifier = 2;
            }
            else
            {
                _modifier = 1;
            }

            _animator.SetFloat(_horizontal, RoundValue(horizontalMovement, _modifier), 0.1f, Time.deltaTime);
            _animator.SetFloat(_vertical, RoundValue(verticalMovement, _modifier), 0.1f, Time.deltaTime);
            _animator.SetBool("isDucking", isDucking);
            _animator.SetBool("isGrounded", isGrounded);
        }

        public void PlayTargetAnimation(string targetAnimation, bool isInterracting)
        {
            _animator.SetBool("isInterracting", isInterracting);
            _animator.CrossFade(targetAnimation, 0.2f);
        }

        private float RoundValue(float value, float modifier)
        {
            float result;
            if (value > 0.05f && value < 0.55f)
            {
                result = 0.5f;
            }
            else if (value >= 0.55f)
            {
                result = 1f;
            }
            else if (value < -0.05f && value > -0.55f)
            {
                result = -0.5f;
            }
            else if (value <= -0.55f)
            {
                result = -1f;
            }
            else
            {
                result = 0;
            }

            return result * modifier;
        }
    }
}
