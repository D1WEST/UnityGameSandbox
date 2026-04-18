namespace Assets.Modules.PlayerModule
{
    using UnityEngine;
    public class PlayerAnimationService : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
        private static readonly int VerticalHash = Animator.StringToHash("Vertical");
        private static readonly int IsDuckingHash = Animator.StringToHash("isDucking");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsInteractingHash = Animator.StringToHash("isInteracting");

        public void UpdateAnimatorValues(float horizontalMovement, float verticalMovement, bool isSprinting, bool isDucking, bool isGrounded)
        {
            float modifier = isSprinting ? 2f : 1f;

            _animator.SetFloat(HorizontalHash, RoundValue(horizontalMovement, modifier), 0.1f, Time.deltaTime);
            _animator.SetFloat(VerticalHash, RoundValue(verticalMovement, modifier), 0.1f, Time.deltaTime);
            _animator.SetBool(IsDuckingHash, isDucking);
            _animator.SetBool(IsGroundedHash, isGrounded);
        }

        public void PlayTargetAnimation(string targetAnimation, bool isInteracting)
        {
            _animator.SetBool(IsInteractingHash, isInteracting);
            _animator.CrossFade(targetAnimation, 0.2f);
        }

        private float RoundValue(float value, float modifier)
        {
            float result = 0f;
            if (value > 0.05f && value < 0.55f) result = 0.5f;
            else if (value >= 0.55f) result = 1f;
            else if (value < -0.05f && value > -0.55f) result = -0.5f;
            else if (value <= -0.55f) result = -1f;

            return result * modifier;
        }
    }
}
