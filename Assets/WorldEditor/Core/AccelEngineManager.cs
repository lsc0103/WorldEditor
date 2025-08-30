using UnityEngine;

namespace WorldEditor.Core
{
    /// <summary>
    /// AccelEngine管理器 - 确保引擎在场景中正确初始化
    /// </summary>
    public class AccelEngineManager : MonoBehaviour
    {
        [Header("AccelEngine配置")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool enableDebugInfo = true;
        [SerializeField] private AccelEngine.AccelProfile forceProfile = AccelEngine.AccelProfile.Auto;
        
        [Header("调试信息")]
        [SerializeField, TextArea(5, 10)] private string engineStatus = "未初始化";
        
        void Start()
        {
            if (autoInitialize)
            {
                InitializeAccelEngine();
            }
        }
        
        /// <summary>
        /// 初始化AccelEngine
        /// </summary>
        public void InitializeAccelEngine()
        {
            Debug.Log("[AccelEngineManager] 开始初始化AccelEngine...");
            
            // 确保AccelEngine实例存在
            var engine = AccelEngine.Instance;
            
            if (engine != null)
            {
                Debug.Log($"[AccelEngineManager] AccelEngine已初始化: {engine.gameObject.name}");
                
                // 设置强制配置（如果需要）
                if (forceProfile != AccelEngine.AccelProfile.Auto)
                {
                    var profileField = typeof(AccelEngine).GetField("currentProfile", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (profileField != null)
                    {
                        profileField.SetValue(engine, forceProfile);
                        Debug.Log($"[AccelEngineManager] 强制设置配置: {forceProfile}");
                    }
                }
                
                UpdateDebugInfo();
            }
            else
            {
                Debug.LogError("[AccelEngineManager] AccelEngine初始化失败!");
                engineStatus = "初始化失败!";
            }
        }
        
        /// <summary>
        /// 更新调试信息
        /// </summary>
        void UpdateDebugInfo()
        {
            if (!enableDebugInfo) return;
            
            var engine = AccelEngine.Instance;
            if (engine != null)
            {
                engineStatus = engine.GetEngineStatus();
            }
        }
        
        /// <summary>
        /// 测试GPU加速性能
        /// </summary>
        [ContextMenu("测试GPU性能")]
        public void TestGPUPerformance()
        {
            Debug.Log("[AccelEngineManager] 开始GPU性能测试...");
            
            var engine = AccelEngine.Instance;
            if (engine == null)
            {
                Debug.LogError("[AccelEngineManager] AccelEngine未初始化!");
                return;
            }
            
            // 提交测试任务
            for (int i = 0; i < 5; i++)
            {
                string taskId = engine.SubmitTask(
                    AccelEngine.ComputeTaskType.NoiseGeneration,
                    $"性能测试任务 {i + 1}",
                    (success) => {
                        Debug.Log($"[AccelEngineManager] 测试任务完成 - 成功: {success}");
                        UpdateDebugInfo();
                    },
                    null,
                    priority: i
                );
                
                Debug.Log($"[AccelEngineManager] 提交测试任务: {taskId}");
            }
        }
        
        /// <summary>
        /// 强制重新初始化引擎
        /// </summary>
        [ContextMenu("重新初始化引擎")]
        public void ForceReinitialize()
        {
            Debug.Log("[AccelEngineManager] 强制重新初始化AccelEngine...");
            
            // 销毁现有实例（如果存在）
            var existingEngine = FindFirstObjectByType<AccelEngine>();
            if (existingEngine != null)
            {
                DestroyImmediate(existingEngine.gameObject);
                Debug.Log("[AccelEngineManager] 已销毁现有AccelEngine实例");
            }
            
            // 重新初始化
            InitializeAccelEngine();
        }
        
        /// <summary>
        /// 获取引擎状态报告
        /// </summary>
        [ContextMenu("显示引擎状态")]
        public void ShowEngineStatus()
        {
            var engine = AccelEngine.Instance;
            if (engine != null)
            {
                string status = engine.GetEngineStatus();
                Debug.Log($"[AccelEngineManager] 引擎状态报告:\n{status}");
                engineStatus = status;
            }
            else
            {
                Debug.LogWarning("[AccelEngineManager] AccelEngine未初始化");
                engineStatus = "引擎未初始化";
            }
        }
        
        void Update()
        {
            // 定期更新调试信息
            if (enableDebugInfo && Time.frameCount % 60 == 0) // 每秒更新一次
            {
                UpdateDebugInfo();
            }
        }
        
        void OnGUI()
        {
            // 调试面板已禁用
            /*
            if (!enableDebugInfo) return;
            
            // 在屏幕上显示简单的状态信息
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label("AccelEngine状态:", GUI.skin.box);
            
            var engine = AccelEngine.Instance;
            if (engine != null)
            {
                GUILayout.Label($"队列任务: {engine.GetQueuedTaskCount()}");
                GUILayout.Label($"已完成: {engine.GetCompletedTaskCount()}");
                
                if (GUILayout.Button("性能测试"))
                {
                    TestGPUPerformance();
                }
                
                if (GUILayout.Button("显示状态"))
                {
                    ShowEngineStatus();
                }
            }
            else
            {
                GUILayout.Label("引擎未初始化");
                
                if (GUILayout.Button("初始化引擎"))
                {
                    InitializeAccelEngine();
                }
            }
            
            GUILayout.EndArea();
            */
        }
    }
}