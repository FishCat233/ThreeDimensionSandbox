using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TDS
{
    [RequireComponent(typeof(Animator))]
    public class MeleeFighter : MonoBehaviour
    {
        [Header("动画")]
        [SerializeField] AttackSO attack;
        [SerializeField] string hurtStateName = "Sword Impact";
        [FormerlySerializedAs("fadeDuration")]
        [Tooltip("进入受击状态的淡入时长（秒）")]
        [SerializeField, Min(0f)] float hurtFadeDuration = 0.05f;
        [Tooltip("Slash 和 Sword Impact 都在 Override Layer 上")]
        [FormerlySerializedAs("attackLayerIndex")]
        [SerializeField, Min(0)] int overrideLayerIndex = 1;

        [Header("判定")]
        [Tooltip("武器上的命中盒，留空则只播攻击动画")]
        [SerializeField] Hitbox hitbox;

        /// <summary>自己的挥砍命中别人时触发，此时受击方已经进入受击状态。</summary>
        public event Action<HitInfo> HitLanded;

        /// <summary>受击开始时触发，此时 <see cref="IsHurt"/> 已经为 true。</summary>
        public event Action HurtStarted;

        /// <summary>受击动画退出时触发，此时 <see cref="IsHurt"/> 已经为 false。</summary>
        public event Action HurtEnded;

        Animator animator;
        bool hitboxOpen;

        void Awake()
        {
            animator = GetComponent<Animator>();

            if (attack == null)
                Debug.LogError("Couldn't find attack by explicit binding. To avoid this error, please bind attack explicitly.");
        }

        void OnEnable()
        {
            if (hitbox != null) hitbox.HitLanded += OnHitboxHitLanded;
        }

        void OnDisable()
        {
            if (hitbox != null) hitbox.HitLanded -= OnHitboxHitLanded;
        }

        void Update()
        {
            if (IsHurt && !IsStateCurrentOrNext(hurtStateName)) EndHurt();

            UpdateHitboxWindow();
        }

        void UpdateHitboxWindow()
        {
            if (hitbox == null) return;

            bool shouldOpen = attack != null
                && TryGetAttackNormalizedTime(out float normalizedTime)
                && attack.IsHitboxOpen(normalizedTime);

            if (shouldOpen == hitboxOpen) return;

            if (shouldOpen) hitbox.Activate();
            else hitbox.Deactivate();

            hitboxOpen = shouldOpen;
        }

        void OnHitboxHitLanded(Hurtbox hurtbox, Vector3 point)
        {
            MeleeFighter victim = hurtbox.Owner;
            if (victim == this) return;

            victim.Hurt();
            HitLanded?.Invoke(new HitInfo(this, victim, point));
        }

        public bool IsAttacking
        {
            get
            {
                if (attack == null) return false;
                if (animator.GetCurrentAnimatorStateInfo(overrideLayerIndex).IsName(attack.StateName)) return true;

                return animator.IsInTransition(overrideLayerIndex)
                    && animator.GetNextAnimatorStateInfo(overrideLayerIndex).IsName(attack.StateName);
            }
        }

        public bool IsHurt { get; private set; }

        public void Attack()
        {
            if (attack == null)
            {
                Debug.LogError("Couldn't find attack by explicit binding. To avoid this error, please bind attack explicitly.");
                return;
            }

            if (!ValidateLayer()) return;
            if (IsHurt) return;
            if (IsAttacking) return;

            animator.CrossFadeInFixedTime(attack.StateName, attack.FadeDuration, overrideLayerIndex);
        }

        /// <summary>播放受击动画。受击期间的重复调用会被忽略。</summary>
        public void Hurt()
        {
            if (!ValidateLayer()) return;
            if (IsHurt) return;

            IsHurt = true;
            animator.CrossFadeInFixedTime(hurtStateName, hurtFadeDuration, overrideLayerIndex);
            HurtStarted?.Invoke();
        }

        void EndHurt()
        {
            IsHurt = false;
            HurtEnded?.Invoke();
        }

        bool TryGetAttackNormalizedTime(out float normalizedTime)
        {
            AnimatorStateInfo activeState = animator.IsInTransition(overrideLayerIndex)
                ? animator.GetNextAnimatorStateInfo(overrideLayerIndex)
                : animator.GetCurrentAnimatorStateInfo(overrideLayerIndex);

            if (!activeState.IsName(attack.StateName))
            {
                normalizedTime = 0f;
                return false;
            }

            normalizedTime = StateNormalizedTime(activeState);
            return true;
        }

        static float StateNormalizedTime(AnimatorStateInfo state)
        {
            return state.loop ? Mathf.Repeat(state.normalizedTime, 1f) : Mathf.Clamp01(state.normalizedTime);
        }

        /// <summary>stateName 是否为当前状态或下一个状态。</summary>
        bool IsStateCurrentOrNext(string stateName)
        {
            if (animator.IsInTransition(overrideLayerIndex))
                return animator.GetNextAnimatorStateInfo(overrideLayerIndex).IsName(stateName);

            return animator.GetCurrentAnimatorStateInfo(overrideLayerIndex).IsName(stateName);
        }

        bool ValidateLayer()
        {
            if (animator.layerCount > overrideLayerIndex) return true;

            Debug.LogError($"Animator layer index {overrideLayerIndex} is out of range (layerCount = {animator.layerCount}). To avoid this error, please set overrideLayerIndex within the layer count.");
            return false;
        }
    }
}
