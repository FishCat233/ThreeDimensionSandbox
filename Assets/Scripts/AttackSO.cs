using UnityEngine;

namespace TDS
{
    [CreateAssetMenu(menuName = "TDS/Attack", fileName = "Attack")]
    public class AttackSO : ScriptableObject
    {
        [Header("动画")]
        [Tooltip("Animator 中的状态名")]
        [SerializeField] string stateName = "Slash";
        [Tooltip("进入攻击状态的淡入时长（秒）")]
        [SerializeField, Min(0f)] float fadeDuration = 0.05f;

        [Header("命中判定")]
        [Tooltip("命中盒开始启用的归一化时间，0 为动画开头")]
        [SerializeField, Range(0f, 1f)] float hitboxStartNormalized = 0.2f;
        [Tooltip("命中盒关闭的归一化时间，1 为动画结尾；与起点构成左闭右开区间")]
        [SerializeField, Range(0f, 1f)] float hitboxEndNormalized = 0.5f;

        public string StateName => stateName;

        public float FadeDuration => fadeDuration;

        public bool IsHitboxOpen(float normalizedTime)
        {
            return normalizedTime >= hitboxStartNormalized && normalizedTime < hitboxEndNormalized;
        }

        void OnValidate()
        {
            hitboxStartNormalized = Mathf.Clamp01(hitboxStartNormalized);
            hitboxEndNormalized = Mathf.Clamp(hitboxEndNormalized, hitboxStartNormalized, 1f);
        }
    }
}
