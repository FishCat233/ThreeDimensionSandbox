using UnityEngine;

namespace TDS.Cameras
{
    /// <summary>球面轨道相机：每帧按焦点与距离摆放镜头的位置与朝向，旋转和缩放由外部通过 Rotate/Zoom 驱动。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class SphereCameraController : MonoBehaviour
    {
        [Header("焦点")]
        [Tooltip("环绕的焦点；留空时以初始视线上的点为中心")]
        [SerializeField] Transform focusOn;
        [Tooltip("在焦点物体本地空间中的偏移，常用于让镜头看向物体头顶而不是轴心")]
        [SerializeField] Vector3 focusOffset;

        [Header("距离")]
        [Tooltip("与焦点的距离，可通过 Zoom 或 Distance 属性修改")]
        [SerializeField, Min(0.01f)] float distance = 6f;
        [SerializeField, Min(0.01f)] float minDistance = 0.5f;
        [SerializeField, Min(0.01f)] float maxDistance = 50f;

        [Header("旋转")]
        [Tooltip("俯仰角的上下限，避免翻转到物体另一侧")]
        [SerializeField] Vector2 pitchRange = new Vector2(-80f, 80f);

        [Header("跟随")]
        [Tooltip("焦点与距离的跟随平滑速度，0 表示硬跟随")]
        [SerializeField, Min(0f)] float followDamping = 12f;

        float yaw;
        float pitch;
        float currentDistance;
        Vector3 anchor;
        Vector3 pivot;

        /// <summary>当前环绕的焦点物体。运行时替换请用 SetFocus。</summary>
        public Transform FocusOn => focusOn;

        /// <summary>目标距离。</summary>
        public float Distance
        {
            get => distance;
            set => distance = ClampDistance(value);
        }

        /// <summary>当前镜头环绕的世界坐标点。</summary>
        public Vector3 Pivot => pivot;

        /// <summary>当前环绕的实际距离。</summary>
        public float CurrentDistance => currentDistance;

        void OnEnable()
        {
            Initialize();
        }

        void LateUpdate()
        {
            float t = DampingFactor(Time.unscaledDeltaTime);
            pivot = Vector3.Lerp(pivot, SolveTargetPivot(), t);
            currentDistance = Mathf.Lerp(currentDistance, distance, t);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(
                pivot - rotation * Vector3.forward * currentDistance,
                rotation);
        }

        void OnValidate()
        {
            if (maxDistance < minDistance) maxDistance = minDistance;
            distance = ClampDistance(distance);
            if (pitchRange.x > pitchRange.y)
                pitchRange = new Vector2(pitchRange.y, pitchRange.x);
        }

        /// <summary>切换环绕焦点；snap 为 true 时同帧吸附，不做平滑过渡。</summary>
        public void SetFocus(Transform target, bool snap = false)
        {
            focusOn = target;
            if (snap) SnapToFocus();
        }

        /// <summary>立即吸附到当前焦点的目标位置与目标距离。</summary>
        public void SnapToFocus()
        {
            pivot = SolveTargetPivot();
            currentDistance = distance;
        }

        /// <summary>在当前朝向基础上追加旋转角度（度）：yawDelta 绕世界 Y 轴，pitchDelta 为正表示镜头向下看；俯仰角会被 pitchRange 限制。</summary>
        public void Rotate(float yawDelta, float pitchDelta)
        {
            yaw += yawDelta;
            pitch = Mathf.Clamp(pitch + pitchDelta, pitchRange.x, pitchRange.y);
        }

        /// <summary>按倍数缩放距离，factor 小于 1 拉近、大于 1 拉远。</summary>
        public void Zoom(float factor)
        {
            if (factor <= 0f) return;
            distance = ClampDistance(distance * factor);
        }

        void Initialize()
        {
            Vector3 forward = transform.forward;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);

            distance = ClampDistance(distance);
            currentDistance = distance;
            // 无焦点时把初始视线上距离为 distance 的点当作注视点，镜头仍可原地旋转、缩放。
            anchor = transform.position + forward * distance;
            pivot = SolveTargetPivot();
        }

        Vector3 SolveTargetPivot()
        {
            return focusOn != null ? focusOn.TransformPoint(focusOffset) : anchor;
        }

        float ClampDistance(float value)
        {
            return Mathf.Clamp(value, minDistance, Mathf.Max(minDistance, maxDistance));
        }

        float DampingFactor(float deltaTime)
        {
            return followDamping <= 0f ? 1f : 1f - Mathf.Exp(-followDamping * deltaTime);
        }
    }
}
