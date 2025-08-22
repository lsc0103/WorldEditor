using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WorldEditor.Core
{
    /// <summary>
    /// 实时GPU性能监控器
    /// 在游戏运行时显示GPU加速状态和性能指标
    /// </summary>
    public class GPUMonitor : MonoBehaviour
    {
        [Header("监控设置")]
        [SerializeField] private bool showOnScreen = true;
        [SerializeField] private bool showInConsole = false;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int maxHistorySize = 60;
        
        [Header("显示位置")]
        [SerializeField] private Vector2 screenPosition = new Vector2(10, 100);
        [SerializeField] private Vector2 panelSize = new Vector2(400, 300);
        
        // 性能监控数据
        private List<float> frameTimeHistory = new List<float>();
        private List<int> gpuTaskHistory = new List<int>();
        private List<int> cpuTaskHistory = new List<int>();
        
        private float lastUpdateTime;
        private int lastGPUTasks = 0;
        private int lastCPUTasks = 0;
        private bool isGPUActive = false;
        
        // GUI样式
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        
        void Start()
        {
            lastUpdateTime = Time.time;
            InitializeGUIStyles();
        }
        
        void Update()
        {
            UpdatePerformanceData();
        }
        
        /// <summary>
        /// 更新性能监控数据
        /// </summary>
        void UpdatePerformanceData()
        {
            // 记录帧时间
            frameTimeHistory.Add(Time.deltaTime * 1000f); // 转换为毫秒
            if (frameTimeHistory.Count > maxHistorySize)
            {
                frameTimeHistory.RemoveAt(0);
            }
            
            // 定期更新GPU任务统计
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateGPUTaskStats();
                lastUpdateTime = Time.time;
                
                if (showInConsole)
                {
                    LogPerformanceStats();
                }
            }
        }
        
        /// <summary>
        /// 更新GPU任务统计
        /// </summary>
        void UpdateGPUTaskStats()
        {
            var accelEngine = AccelEngine.Instance;
            if (accelEngine != null)
            {
                int currentCompleted = accelEngine.GetCompletedTaskCount();
                int currentQueued = accelEngine.GetQueuedTaskCount();
                
                // 计算新增的GPU任务
                int newGPUTasks = currentCompleted - lastGPUTasks;
                gpuTaskHistory.Add(newGPUTasks);
                
                lastGPUTasks = currentCompleted;
                isGPUActive = currentQueued > 0 || newGPUTasks > 0;
                
                // 限制历史记录大小
                if (gpuTaskHistory.Count > maxHistorySize)
                {
                    gpuTaskHistory.RemoveAt(0);
                }
            }
        }
        
        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        void InitializeGUIStyles()
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(1, 1, new Color(0, 0, 0, 0.8f)) }
            };
            
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                wordWrap = true
            };
            
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.yellow },
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }
        
        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        Texture2D MakeTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 在屏幕上显示GPU监控信息
        /// </summary>
        void OnGUI()
        {
            if (!showOnScreen) return;
            
            if (boxStyle == null) InitializeGUIStyles();
            
            Rect panelRect = new Rect(screenPosition.x, screenPosition.y, panelSize.x, panelSize.y);
            GUI.Box(panelRect, "", boxStyle);
            
            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + 10, panelRect.width - 20, panelRect.height - 20));
            
            // 标题
            GUILayout.Label("🚀 WorldEditor GPU Monitor", headerStyle);
            GUILayout.Space(5);
            
            // GPU基础信息
            GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName}", labelStyle);
            GUILayout.Label($"显存: {SystemInfo.graphicsMemorySize} MB", labelStyle);
            
            // 实时状态
            string gpuStatus = isGPUActive ? "🟢 GPU 活跃" : "⚪ GPU 待机";
            GUILayout.Label($"状态: {gpuStatus}", labelStyle);
            
            GUILayout.Space(5);
            
            // AccelEngine状态
            var accelEngine = AccelEngine.Instance;
            if (accelEngine != null)
            {
                GUILayout.Label("📊 AccelEngine 状态:", headerStyle);
                GUILayout.Label($"队列任务: {accelEngine.GetQueuedTaskCount()}", labelStyle);
                GUILayout.Label($"完成任务: {accelEngine.GetCompletedTaskCount()}", labelStyle);
                
                // 任务处理速度
                if (gpuTaskHistory.Count > 0)
                {
                    float avgGPUTasks = (float)gpuTaskHistory.Average();
                    GUILayout.Label($"GPU任务/秒: {avgGPUTasks:F1}", labelStyle);
                }
                
                GUILayout.Space(5);
            }
            
            // 帧率信息
            if (frameTimeHistory.Count > 0)
            {
                float avgFrameTime = (float)frameTimeHistory.Average();
                float fps = 1000f / avgFrameTime;
                
                GUILayout.Label("📈 性能指标:", headerStyle);
                GUILayout.Label($"FPS: {fps:F1}", labelStyle);
                GUILayout.Label($"帧时间: {avgFrameTime:F2} ms", labelStyle);
                
                // 性能等级指示
                string perfLevel;
                Color perfColor;
                
                if (fps >= 60)
                {
                    perfLevel = "🔥 极佳";
                    perfColor = Color.green;
                }
                else if (fps >= 30)
                {
                    perfLevel = "✅ 良好";
                    perfColor = Color.yellow;
                }
                else
                {
                    perfLevel = "⚠️ 需优化";
                    perfColor = Color.red;
                }
                
                var oldColor = labelStyle.normal.textColor;
                labelStyle.normal.textColor = perfColor;
                GUILayout.Label($"等级: {perfLevel}", labelStyle);
                labelStyle.normal.textColor = oldColor;
            }
            
            GUILayout.Space(5);
            
            // RTX特殊标识
            if (SystemInfo.graphicsDeviceName.Contains("RTX"))
            {
                string rtxInfo = "🎯 RTX 加速就绪";
                if (SystemInfo.graphicsDeviceName.Contains("4070"))
                {
                    rtxInfo = "🔥 RTX 4070 顶级性能";
                }
                
                var oldColor = labelStyle.normal.textColor;
                labelStyle.normal.textColor = Color.cyan;
                GUILayout.Label(rtxInfo, labelStyle);
                labelStyle.normal.textColor = oldColor;
            }
            
            // 操作按钮
            GUILayout.Space(10);
            
            if (GUILayout.Button("🧪 运行GPU基准测试"))
            {
                var benchmark = FindFirstObjectByType<GPUBenchmark>();
                if (benchmark != null)
                {
                    benchmark.RunPerformanceBenchmark();
                }
                else
                {
                    Debug.LogWarning("[GPUMonitor] 未找到GPUBenchmark组件");
                }
            }
            
            if (GUILayout.Button("📊 显示AccelEngine状态"))
            {
                if (accelEngine != null)
                {
                    Debug.Log($"[GPUMonitor] {accelEngine.GetEngineStatus()}");
                }
            }
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 在控制台输出性能统计
        /// </summary>
        void LogPerformanceStats()
        {
            if (frameTimeHistory.Count == 0) return;
            
            float avgFrameTime = (float)frameTimeHistory.Average();
            float fps = 1000f / avgFrameTime;
            
            var accelEngine = AccelEngine.Instance;
            string gpuStats = accelEngine != null ? 
                $"GPU任务队列: {accelEngine.GetQueuedTaskCount()}, 已完成: {accelEngine.GetCompletedTaskCount()}" : 
                "AccelEngine未初始化";
            
            Debug.Log($"[GPUMonitor] FPS: {fps:F1} | 帧时间: {avgFrameTime:F2}ms | {gpuStats} | GPU状态: {(isGPUActive ? "活跃" : "待机")}");
        }
        
        /// <summary>
        /// 手动触发性能测试
        /// </summary>
        [ContextMenu("触发GPU负载测试")]
        public void TriggerGPULoadTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GPUMonitor] GPU负载测试需要在Play模式下运行");
                return;
            }
            
            StartCoroutine(ExecuteGPULoadTest());
        }
        
        /// <summary>
        /// 执行GPU负载测试
        /// </summary>
        System.Collections.IEnumerator ExecuteGPULoadTest()
        {
            Debug.Log("[GPUMonitor] 开始GPU负载测试...");
            
            var accelEngine = AccelEngine.Instance;
            if (accelEngine == null)
            {
                Debug.LogError("[GPUMonitor] AccelEngine未初始化");
                yield break;
            }
            
            // 提交多个GPU任务来测试负载
            for (int i = 0; i < 10; i++)
            {
                accelEngine.SubmitTask(
                    AccelEngine.ComputeTaskType.NoiseGeneration,
                    $"GPU负载测试任务 {i+1}",
                    (success) => {
                        Debug.Log($"[GPUMonitor] GPU测试任务完成 - 成功: {success}");
                    },
                    priority: i
                );
                
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("[GPUMonitor] GPU负载测试任务已提交，观察GPU Monitor面板查看实时状态");
        }
        
        /// <summary>
        /// 获取当前GPU利用率估算
        /// </summary>
        public float GetGPUUtilizationEstimate()
        {
            if (gpuTaskHistory.Count == 0) return 0f;
            
            float recentActivity = (float)gpuTaskHistory.TakeLast(10).Sum();
            return Mathf.Clamp01(recentActivity / 10f); // 简单估算
        }
        
        /// <summary>
        /// 检查是否为高端GPU
        /// </summary>
        public bool IsHighEndGPU()
        {
            return SystemInfo.graphicsMemorySize >= 8000 && // 8GB+显存
                   (SystemInfo.graphicsDeviceName.Contains("RTX") || 
                    SystemInfo.graphicsDeviceName.Contains("Radeon RX"));
        }
    }
}