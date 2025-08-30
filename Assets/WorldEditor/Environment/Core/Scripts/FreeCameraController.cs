using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WorldEditor.Environment
{
    /// <summary>
    /// 自由摄像机控制器 - 用于测试天空系统
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMoveSpeed = 50f;
        [SerializeField] private float mouseSensitivity = 2f;
        
        [Header("旋转设置")]
        [SerializeField] private float maxLookAngle = 90f;
        
        [Header("控制键设置")]
        [SerializeField] private KeyCode enableKey = KeyCode.F1;
        [SerializeField] private KeyCode fastMoveKey = KeyCode.LeftShift;
        
        [Header("启用设置")]
        [SerializeField] private bool autoEnableOnStart = false; // 改为默认不自动启用
        [SerializeField] private bool enableByMouseClick = true; // 鼠标点击启用
        
        private bool isEnabled = false;
        private float verticalRotation = 0;
        private float horizontalRotation = 0;
        private bool wasMouseLocked = false;
        
        void Start()
        {
            // 保存原始鼠标状态
            wasMouseLocked = Cursor.lockState == CursorLockMode.Locked;
            
            // 自动启用（如果设置了）
            if (autoEnableOnStart)
            {
                EnableFreeCamera();
            }
            
            Debug.Log("[FreeCameraController] 自由摄像机控制器已就绪");
        }
        
        void Update()
        {
            HandleInput();
            
            if (isEnabled)
            {
                HandleMouseLook();
                HandleMovement();
            }
        }
        
        void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            // 鼠标点击启用
            if (enableByMouseClick && !isEnabled && Mouse.current.leftButton.wasPressedThisFrame)
            {
                EnableFreeCamera();
            }
            
            // 键盘控制
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ToggleFreeCamera();
            }
            
            if (Keyboard.current.escapeKey.wasPressedThisFrame && isEnabled)
            {
                DisableFreeCamera(); // ESC直接退出
            }
#else
            // 鼠标点击启用（旧Input系统）
            if (enableByMouseClick && !isEnabled && Input.GetMouseButtonDown(0))
            {
                EnableFreeCamera();
            }
            
            // 键盘控制
            if (Input.GetKeyDown(enableKey))
            {
                ToggleFreeCamera();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) && isEnabled)
            {
                DisableFreeCamera(); // ESC直接退出
            }
#endif
        }
        
        void ToggleFreeCamera()
        {
            if (isEnabled)
            {
                DisableFreeCamera();
            }
            else
            {
                EnableFreeCamera();
            }
        }
        
        void EnableFreeCamera()
        {
            isEnabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[FreeCameraController] 自由摄像机已启用 - 使用WASD移动，鼠标查看，Shift加速，ESC退出");
        }
        
        void DisableFreeCamera()
        {
            isEnabled = false;
            Cursor.lockState = wasMouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !wasMouseLocked;
            
            Debug.Log("[FreeCameraController] 自由摄像机已禁用");
        }
        
        void HandleMouseLook()
        {
            // 获取鼠标输入
            float mouseX = 0f;
            float mouseY = 0f;
            
#if ENABLE_INPUT_SYSTEM
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity * 0.02f; // 调整灵敏度
            mouseY = mouseDelta.y * mouseSensitivity * 0.02f;
#else
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
#endif
            
            // 累积旋转值
            horizontalRotation += mouseX;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            
            // 应用完整的旋转
            transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
        }
        
        void HandleMovement()
        {
            Vector3 moveDirection = Vector3.zero;
            
#if ENABLE_INPUT_SYSTEM
            // 新的Input System
            Keyboard keyboard = Keyboard.current;
            
            // WASD移动
            if (keyboard.wKey.isPressed)
                moveDirection += transform.forward;
            if (keyboard.sKey.isPressed)
                moveDirection -= transform.forward;
            if (keyboard.aKey.isPressed)
                moveDirection -= transform.right;
            if (keyboard.dKey.isPressed)
                moveDirection += transform.right;
            
            // 上下移动
            if (keyboard.qKey.isPressed)
                moveDirection -= transform.up;
            if (keyboard.eKey.isPressed)
                moveDirection += transform.up;
            
            // 检查是否按住加速键
            float currentSpeed = keyboard.leftShiftKey.isPressed ? fastMoveSpeed : moveSpeed;
#else
            // 旧的Input Manager
            // WASD移动
            if (Input.GetKey(KeyCode.W))
                moveDirection += transform.forward;
            if (Input.GetKey(KeyCode.S))
                moveDirection -= transform.forward;
            if (Input.GetKey(KeyCode.A))
                moveDirection -= transform.right;
            if (Input.GetKey(KeyCode.D))
                moveDirection += transform.right;
            
            // 上下移动
            if (Input.GetKey(KeyCode.Q))
                moveDirection -= transform.up;
            if (Input.GetKey(KeyCode.E))
                moveDirection += transform.up;
            
            // 检查是否按住加速键
            float currentSpeed = Input.GetKey(fastMoveKey) ? fastMoveSpeed : moveSpeed;
#endif
            
            // 应用移动
            if (moveDirection != Vector3.zero)
            {
                transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
            }
        }
        
        void OnGUI()
        {
            if (isEnabled)
            {
                // 显示控制提示
                GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
                GUILayout.Box("自由摄像机控制");
                GUILayout.Label("WASD: 前后左右移动");
                GUILayout.Label("QE: 上下移动");
                GUILayout.Label("鼠标: 查看方向");
                GUILayout.Label("Shift: 加速移动");
                GUILayout.Label("ESC: 退出自由摄像机");
                GUILayout.EndArea();
            }
            else
            {
                // 显示启用提示
                GUILayout.BeginArea(new Rect(10, Screen.height - 60, 250, 50));
                GUILayout.Box("点击鼠标左键启用自由摄像机");
                GUILayout.EndArea();
            }
        }
        
        void OnDisable()
        {
            if (isEnabled)
            {
                DisableFreeCamera();
            }
        }
    }
}