using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDS
{
    /// <summary>
    /// 挂在武器碰撞盒上，把碰到的受击盒转成命中。
    /// 跨阵营过滤交给物理碰撞矩阵，同一轮判定里同一目标只命中一次。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Hitbox : MonoBehaviour
    {
        /// <summary>武器命中受击盒时触发，命中点取受击盒上离武器最近的位置。</summary>
        public event Action<Hurtbox, Vector3> HitLanded;

        readonly HashSet<MeleeFighter> hitTargets = new HashSet<MeleeFighter>();
        Collider hitCollider;
        Rigidbody hitRigidbody;

        public bool IsActive => hitCollider != null && hitCollider.enabled;

        void Awake()
        {
            hitCollider = GetComponent<Collider>();
            hitRigidbody = GetComponent<Rigidbody>();

            if (hitCollider == null)
            {
                Debug.LogError("Couldn't find Collider on the hitbox. To avoid this error, please add a collider to it.");
                return;
            }

            if (!hitCollider.isTrigger)
                Debug.LogError("The hitbox collider is not a trigger. To avoid this error, please enable Is Trigger on it.");

            if (!hitRigidbody.isKinematic)
                Debug.LogError("The hitbox Rigidbody is not kinematic. To avoid this error, please enable Is Kinematic on it.");

            hitCollider.enabled = false;
        }

        /// <summary>开启判定，并清空上一轮的目标名单。</summary>
        public void Activate()
        {
            hitTargets.Clear();
            hitCollider.enabled = true;
        }

        public void Deactivate()
        {
            hitCollider.enabled = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out Hurtbox hurtbox)) return;
            if (hurtbox.Owner == null) return;
            if (!hitTargets.Add(hurtbox.Owner)) return;

            HitLanded?.Invoke(hurtbox, other.ClosestPoint(transform.position));
        }
    }
}
