using TDS.Cameras;
using UnityEngine;
using UnityEngine.Serialization;

namespace TDS
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>允许触发相机旋转的鼠标按键。</summary>
        public enum RotateButton
        {
            /// <summary>不需要按键，鼠标一动就旋转。</summary>
            Always = -1,
            Left = 0,
            Right = 1,
            Middle = 2,
        }

        [Header("鼠标")]
        [Tooltip("启动时立刻锁定并隐藏鼠标")]
        [SerializeField] bool lockMouseOnStart = true;
        [Tooltip("鼠标释放后，点击左键重新锁定")]
        [SerializeField] bool relockOnClick = true;

        [Header("相机")]
        [Tooltip("提供移动参考朝向与旋转缩放的球面相机")]
        [SerializeField] SphereCameraController sphereCamera;
        [Tooltip("按住哪个键才旋转；选 Always 则鼠标移动即旋转")]
        [SerializeField] RotateButton rotateButton = RotateButton.Always;
        [Tooltip("鼠标移动一个单位对应的旋转角度")]
        [SerializeField, Min(0f)] float sensitivity = 3f;
        [Tooltip("勾选后鼠标右移改为向左环绕")]
        [SerializeField] bool invertX;
        [Tooltip("勾选后鼠标上移改为压低视线，镜头随之升高")]
        [SerializeField] bool invertY;
        [SerializeField] bool zoomByScroll = true;
        [Tooltip("滚轮缩放的灵敏度，每格滚动按指数缩放距离")]
        [SerializeField, Min(0f)] float zoomSpeed = 2f;

        [Header("移动")]
        [Tooltip("不按 Shift 时的移动速度（米/秒）")]
        [SerializeField, Min(0f)] float walkSpeed = 5f;
        [Tooltip("按住 Shift 时的移动速度（米/秒）")]
        [SerializeField, Min(0f)] float sprintSpeed = 8f;

        [Header("跳跃")]
        [Tooltip("跳跃能到达的最大高度（米）")]
        [SerializeField, Min(0f)] float jumpHeight = 1.5f;
        [Tooltip("重力加速度，越大下落越快、跳跃越干脆")]
        [SerializeField, Min(0f)] float gravity = 20f;

        [Header("进阶")]
        [Tooltip("黏着地面的速度。通常和下坡、平台边缘有关")]
        [FormerlySerializedAs("GroundStickSpeed")]
        [SerializeField, Min(0.01f)] float groundStickSpeed = 2f;

        CharacterController controller;
        float verticalVelocity;


        public bool IsMouseLocked => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (sphereCamera == null)
                Debug.LogError("Couldn't find sphereCamera by explicit binding. For avoid this error, please bind sphereCamera explicit.");
        }

        void Start()
        {
            if (lockMouseOnStart) LockMouse();
            else UnlockMouse();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockMouse();
                return;
            }

            if (relockOnClick && !IsMouseLocked && Input.GetMouseButtonDown(0))
            {
                LockMouse();
                return;
            }

            if (IsMouseLocked) HandleCameraInput();
            HandleMoveInput();
        }

        /// <summary>锁定并隐藏光标，相机开始接收输入。</summary>
        public void LockMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>解锁并显示光标，相机停止接收输入。</summary>
        public void UnlockMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void HandleMoveInput()
        {
            bool grounded = controller.isGrounded;

            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -groundStickSpeed;

            if (grounded && Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            verticalVelocity -= gravity * Time.deltaTime;

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();

            Vector3 move = SolveMoveDirection(input) * (IsSprinting() ? sprintSpeed : walkSpeed);
            controller.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        Vector3 SolveMoveDirection(Vector2 input)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            // 以相机的水平朝向作为移动参考：镜头转到哪里，WASD 就朝哪里。
            Vector3 cameraForward = Vector3.ProjectOnPlane(sphereCamera.transform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                forward = cameraForward.normalized;
                right = Vector3.Cross(Vector3.up, forward);
            }

            return right * input.x + forward * input.y;
        }

        bool IsSprinting()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        void HandleCameraInput()
        {
            if (IsRotateButtonHeld())
            {
                float invertXFactor = invertX ? -1f : 1f;
                float invertYFactor = invertY ? -1f : 1f;
                float yawDelta = Input.GetAxis("Mouse X") * sensitivity * invertXFactor;
                float pitchDelta = -Input.GetAxis("Mouse Y") * sensitivity * invertYFactor;

                if (yawDelta != 0f || pitchDelta != 0f)
                    sphereCamera.Rotate(yawDelta, pitchDelta);
            }

            if (zoomByScroll)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (!Mathf.Approximately(scroll, 0f))
                    sphereCamera.Zoom(Mathf.Exp(-scroll * zoomSpeed));
            }
        }

        bool IsRotateButtonHeld()
        {
            return rotateButton == RotateButton.Always || Input.GetMouseButton((int)rotateButton);
        }
    }
}
