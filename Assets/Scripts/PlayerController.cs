using TDS.Camera;
using UnityEngine;

namespace TDS
{
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
        [Tooltip("留空则自动查找场景中的 SphereCameraController")]
        [SerializeField] SphereCameraController sphereCamera;
        [Tooltip("按住哪个键才旋转；选 Always 则鼠标移动即旋转")]
        [SerializeField] RotateButton rotateButton = RotateButton.Always;
        [Tooltip("鼠标移动一个单位对应的旋转角度")]
        [SerializeField, Min(0f)] float sensitivity = 3f;
        [Tooltip("勾选后鼠标右移改为向左环绕")]
        [SerializeField] bool invertX;
        [Tooltip("勾选后鼠标上移改为镜头下移")]
        [SerializeField] bool invertY;
        [SerializeField] bool zoomByScroll = true;
        [Tooltip("滚轮缩放的灵敏度，每格滚动按指数缩放距离")]
        [SerializeField, Min(0f)] float zoomSpeed = 2f;

        public bool IsMouseLocked => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            if (sphereCamera == null)
                sphereCamera = FindFirstObjectByType<SphereCameraController>();
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

        void HandleCameraInput()
        {
            if (sphereCamera == null) return;

            if (IsRotateButtonHeld())
            {
                float invertXFactor = invertX ? -1f : 1f;
                float invertYFactor = invertY ? -1f : 1f;
                float yawDelta = Input.GetAxis("Mouse X") * sensitivity * invertXFactor;
                float pitchDelta = Input.GetAxis("Mouse Y") * sensitivity * invertYFactor;

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
