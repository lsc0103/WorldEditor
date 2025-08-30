using UnityEngine;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 自动为主摄像机添加自由摄像机控制器
    /// </summary>
    [System.Serializable]
    public class AutoAddFreeCameraController : MonoBehaviour
    {
        [Header("自动设置")]
        [SerializeField] private bool autoAddToMainCamera = true;
        [SerializeField] private bool showStartupMessage = true;
        
        void Start()
        {
            if (autoAddToMainCamera)
            {
                AddFreeCameraControllerToMainCamera();
            }
        }
        
        void AddFreeCameraControllerToMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[AutoAddFreeCameraController] 未找到主摄像机");
                return;
            }
            
            // 检查是否已经有FreeCameraController组件
            FreeCameraController existingController = mainCamera.GetComponent<FreeCameraController>();
            if (existingController != null)
            {
                if (showStartupMessage)
                {
                    Debug.Log("[AutoAddFreeCameraController] 主摄像机已有自由摄像机控制器");
                }
                return;
            }
            
            // 添加FreeCameraController组件
            FreeCameraController controller = mainCamera.gameObject.AddComponent<FreeCameraController>();
            
            if (showStartupMessage)
            {
                Debug.Log("=== 自由摄像机控制器已添加 ===");
                Debug.Log("按 F1 键启用/禁用自由摄像机");
                Debug.Log("WASD: 移动  QE: 上下  Shift: 加速  鼠标: 查看方向");
                Debug.Log("ESC: 退出自由摄像机模式");
                Debug.Log("===============================");
            }
        }
    }
}