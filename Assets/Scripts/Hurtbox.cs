using UnityEngine;

namespace TDS
{
    /// <summary>挂在角色身上的受击盒，声明这块身体归哪个 <see cref="MeleeFighter"/>。</summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] MeleeFighter owner;

        public MeleeFighter Owner => owner;

        void Awake()
        {
            Collider hurtCollider = GetComponent<Collider>();

            if (hurtCollider == null)
                Debug.LogError("Couldn't find Collider on the hurtbox. To avoid this error, please add a collider to it.");
            else if (!hurtCollider.isTrigger)
                Debug.LogError("The hurtbox collider is not a trigger. To avoid this error, please enable Is Trigger on it.");

            if (owner == null)
                Debug.LogError("Couldn't find owner by explicit binding. To avoid this error, please bind owner explicitly.");
        }
    }
}
