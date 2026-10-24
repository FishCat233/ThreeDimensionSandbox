using UnityEngine;

namespace TDS
{
    [RequireComponent(typeof(Animator))]
    public class MeleeFighter : MonoBehaviour
    {
        [Header("动画")]
        [SerializeField] string attackStateName = "Slash";
        [Tooltip("Slash 在 Override Layer 上，不在 Base Layer")]
        [SerializeField, Min(0)] int attackLayerIndex = 1;
        [SerializeField, Min(0f)] float fadeDuration = 0.05f;

        Animator animator;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public bool IsAttacking
        {
            get
            {
                if (animator.GetCurrentAnimatorStateInfo(attackLayerIndex).IsName(attackStateName)) return true;

                return animator.IsInTransition(attackLayerIndex)
                    && animator.GetNextAnimatorStateInfo(attackLayerIndex).IsName(attackStateName);
            }
        }

        public void Attack()
        {
            if (animator.layerCount <= attackLayerIndex)
            {
                Debug.LogWarning($"Attack layer index {attackLayerIndex} is out of range (layerCount = {animator.layerCount}).");
                return;
            }

            if (IsAttacking) return;

            animator.CrossFadeInFixedTime(attackStateName, fadeDuration, attackLayerIndex);
        }
    }
}
