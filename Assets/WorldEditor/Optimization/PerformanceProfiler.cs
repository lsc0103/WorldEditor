using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using WorldEditor.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldEditor.Optimization
{
    /// <summary>
    /// 性能分析器 - 监控和优化WorldEditor系统性能
    /// 提供实时性能分析、瓶颈检测、自动优化建议
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        [Header("分析设置")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool enableRealtimeAnalysis = false;
        [SerializeField] private float samplingInterval = 1f;
        [SerializeField] private int maxSamples = 1000;
        
        [Header("性能阈值")]
        [SerializeField] private float frameTimeWarningThreshold = 33.33f; // 30 FPS
        [SerializeField] private float frameTimeCriticalThreshold = 50f;    // 20 FPS
        [SerializeField] private long memoryWarningThreshold = 1024 * 1024 * 512; // 512MB
        [SerializeField] private long memoryCriticalThreshold = 1024 * 1024 * 1024; // 1GB
        
        [Header("优化控制")]
        [SerializeField] private bool enableAutoOptimization = false;
        [SerializeField] private PerformanceMode currentPerformanceMode = PerformanceMode.Balanced;
        
        // 性能数据
        private List<PerformanceSample> performanceSamples = new List<PerformanceSample>();
        private PerformanceMetrics currentMetrics = new PerformanceMetrics();
        private PerformanceState currentState = PerformanceState.Good;
        
        // 系统监控
        private Dictionary<string, SystemProfiler> systemProfilers = new Dictionary<string, SystemProfiler>();
        private float lastSampleTime = 0f;
        private Stopwatch frameTimer = new Stopwatch();
        
        // 事件
        public System.Action<PerformanceState> OnPerformanceStateChanged;
        public System.Action<PerformanceMetrics> OnMetricsUpdated;
        public System.Action<string> OnOptimizationApplied;

        void Awake()
        {
            InitializeProfiler();
        }

        void Start()
        {
            RegisterSystemProfilers();
            
            if (enableRealtimeAnalysis)
            {
                InvokeRepeating(nameof(UpdatePerformanceAnalysis), samplingInterval, samplingInterval);
            }
        }

        void Update()
        {
            if (!enableProfiling) return;
            
            // 帧时间监控
            frameTimer.Restart();
        }

        void LateUpdate()
        {
            if (!enableProfiling) return;
            
            frameTimer.Stop();
            float frameTime = (float)frameTimer.Elapsed.TotalMilliseconds;
            
            UpdateCurrentMetrics(frameTime);
            
            if (enableRealtimeAnalysis && Time.time - lastSampleTime >= samplingInterval)
            {
                RecordPerformanceSample();
                lastSampleTime = Time.time;
            }
        }

        /// <summary>
        /// 初始化性能分析器
        /// </summary>
        void InitializeProfiler()
        {
            UnityEngine.Debug.Log("[Performance] 性能分析器初始化");
            
            // 设置QualitySettings监控
            QualitySettings.vSyncCount = 0; // 禁用垂直同步以获得准确的帧率测量
            Application.targetFrameRate = -1; // 不限制帧率
            
            // 初始化性能计时器
            frameTimer = new Stopwatch();
            
            // 预分配样本列表
            performanceSamples.Capacity = maxSamples;
        }

        /// <summary>
        /// 注册系统性能分析器
        /// </summary>
        void RegisterSystemProfilers()
        {
            // 地形系统分析器
            systemProfilers["TerrainSystem"] = new SystemProfiler("地形系统", 
                () => Object.FindFirstObjectByType<WorldEditor.TerrainSystem.AdvancedTerrainGenerator>()?.IsGenerating() ?? false);
            
            // 放置系统分析器
            systemProfilers["PlacementSystem"] = new SystemProfiler("放置系统",
                () => Object.FindFirstObjectByType<WorldEditor.Placement.SmartPlacementSystem>()?.IsPlacementActive() ?? false);
            
            // 环境系统分析器
            systemProfilers["EnvironmentSystem"] = new SystemProfiler("环境系统",
                () => Object.FindFirstObjectByType<WorldEditor.Environment.EnvironmentManager>() != null);
            
            UnityEngine.Debug.Log($"[Performance] 已注册 {systemProfilers.Count} 个系统分析器");
        }

        /// <summary>
        /// 更新性能分析
        /// </summary>
        void UpdatePerformanceAnalysis()
        {
            if (!enableProfiling) return;
            
            // 分析系统性能
            AnalyzeSystemPerformance();
            
            // 检查性能状态
            CheckPerformanceState();
            
            // 自动优化（如果启用）
            if (enableAutoOptimization)
            {
                ApplyAutoOptimizations();
            }
            
            OnMetricsUpdated?.Invoke(currentMetrics);
        }

        /// <summary>
        /// 更新当前性能指标
        /// </summary>
        void UpdateCurrentMetrics(float frameTime)
        {
            currentMetrics.frameTime = frameTime;
            currentMetrics.fps = 1000f / frameTime;
            currentMetrics.memoryUsage = System.GC.GetTotalMemory(false);
            currentMetrics.timestamp = Time.realtimeSinceStartup;
            
            // Unity性能统计（使用Profiler API获取）
            #if UNITY_EDITOR
            currentMetrics.drawCalls = UnityEditor.UnityStats.drawCalls;
            currentMetrics.batches = UnityEditor.UnityStats.batches; 
            currentMetrics.triangles = UnityEditor.UnityStats.triangles;
            currentMetrics.vertices = UnityEditor.UnityStats.vertices;
            #else
            // 运行时无法获取详细统计，使用默认值
            currentMetrics.drawCalls = 0;
            currentMetrics.batches = 0;
            currentMetrics.triangles = 0;
            currentMetrics.vertices = 0;
            #endif
        }

        /// <summary>
        /// 记录性能样本
        /// </summary>
        void RecordPerformanceSample()
        {
            var sample = new PerformanceSample
            {
                timestamp = currentMetrics.timestamp,
                frameTime = currentMetrics.frameTime,
                fps = currentMetrics.fps,
                memoryUsage = currentMetrics.memoryUsage,
                drawCalls = currentMetrics.drawCalls,
                triangles = currentMetrics.triangles
            };
            
            performanceSamples.Add(sample);
            
            // 限制样本数量
            if (performanceSamples.Count > maxSamples)
            {
                performanceSamples.RemoveAt(0);
            }
        }

        /// <summary>
        /// 分析系统性能
        /// </summary>
        void AnalyzeSystemPerformance()
        {
            foreach (var profiler in systemProfilers.Values)
            {
                profiler.Update();
            }
        }

        /// <summary>
        /// 检查性能状态
        /// </summary>
        void CheckPerformanceState()
        {
            PerformanceState newState = PerformanceState.Good;
            
            // 检查帧时间
            if (currentMetrics.frameTime > frameTimeCriticalThreshold)
            {
                newState = PerformanceState.Critical;
            }
            else if (currentMetrics.frameTime > frameTimeWarningThreshold)
            {
                newState = PerformanceState.Warning;
            }
            
            // 检查内存使用
            if (currentMetrics.memoryUsage > memoryCriticalThreshold)
            {
                newState = PerformanceState.Critical;
            }
            else if (currentMetrics.memoryUsage > memoryWarningThreshold && newState == PerformanceState.Good)
            {
                newState = PerformanceState.Warning;
            }
            
            // 状态改变事件
            if (newState != currentState)
            {
                currentState = newState;
                OnPerformanceStateChanged?.Invoke(currentState);
                
                UnityEngine.Debug.Log($"[Performance] 性能状态变更: {currentState}");
            }
        }

        /// <summary>
        /// 应用自动优化
        /// </summary>
        void ApplyAutoOptimizations()
        {
            switch (currentState)
            {
                case PerformanceState.Warning:
                    ApplyPerformanceMode(PerformanceMode.Performance);
                    break;
                    
                case PerformanceState.Critical:
                    ApplyPerformanceMode(PerformanceMode.Minimal);
                    break;
                    
                case PerformanceState.Good:
                    if (currentPerformanceMode != PerformanceMode.Quality)
                    {
                        ApplyPerformanceMode(PerformanceMode.Balanced);
                    }
                    break;
            }
        }

        /// <summary>
        /// 应用性能模式
        /// </summary>
        public void ApplyPerformanceMode(PerformanceMode mode)
        {
            if (currentPerformanceMode == mode) return;
            
            currentPerformanceMode = mode;
            
            switch (mode)
            {
                case PerformanceMode.Quality:
                    ApplyQualitySettings();
                    break;
                    
                case PerformanceMode.Balanced:
                    ApplyBalancedSettings();
                    break;
                    
                case PerformanceMode.Performance:
                    ApplyPerformanceSettings();
                    break;
                    
                case PerformanceMode.Minimal:
                    ApplyMinimalSettings();
                    break;
            }
            
            OnOptimizationApplied?.Invoke($"应用性能模式: {mode}");
            UnityEngine.Debug.Log($"[Performance] 已应用性能模式: {mode}");
        }

        /// <summary>
        /// 应用质量设置
        /// </summary>
        void ApplyQualitySettings()
        {
            QualitySettings.shadowDistance = 150f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.lodBias = 2f;
            QualitySettings.maximumLODLevel = 0;
            
            // 通知其他系统
            NotifySystemsPerformanceChange(PerformanceLevel.High);
        }

        /// <summary>
        /// 应用平衡设置
        /// </summary>
        void ApplyBalancedSettings()
        {
            QualitySettings.shadowDistance = 100f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.lodBias = 1.5f;
            QualitySettings.maximumLODLevel = 0;
            
            NotifySystemsPerformanceChange(PerformanceLevel.Medium);
        }

        /// <summary>
        /// 应用性能设置
        /// </summary>
        void ApplyPerformanceSettings()
        {
            QualitySettings.shadowDistance = 50f;
            QualitySettings.shadowCascades = 1;
            QualitySettings.lodBias = 1f;
            QualitySettings.maximumLODLevel = 1;
            
            NotifySystemsPerformanceChange(PerformanceLevel.Low);
        }

        /// <summary>
        /// 应用最小设置
        /// </summary>
        void ApplyMinimalSettings()
        {
            QualitySettings.shadowDistance = 0f;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.lodBias = 0.7f;
            QualitySettings.maximumLODLevel = 2;
            
            NotifySystemsPerformanceChange(PerformanceLevel.Minimal);
        }

        /// <summary>
        /// 通知系统性能变化
        /// </summary>
        void NotifySystemsPerformanceChange(PerformanceLevel level)
        {
            // 通知地形系统
            var terrainGen = Object.FindFirstObjectByType<WorldEditor.TerrainSystem.AdvancedTerrainGenerator>();
            if (terrainGen != null)
            {
                // 可以添加性能级别设置方法
            }
            
            // 通知环境系统
            var envManager = Object.FindFirstObjectByType<WorldEditor.Environment.EnvironmentManager>();
            if (envManager != null)
            {
                // 新的环境系统会根据性能自动调整
                UnityEngine.Debug.Log($"[Performance] 环境系统已通知性能级别变化: {level}");
            }
        }


        /// <summary>
        /// 获取性能报告
        /// </summary>
        public PerformanceReport GeneratePerformanceReport()
        {
            var report = new PerformanceReport();
            
            if (performanceSamples.Count > 0)
            {
                // 计算统计数据
                float totalFrameTime = 0f;
                float minFrameTime = float.MaxValue;
                float maxFrameTime = float.MinValue;
                long totalMemory = 0L;
                
                foreach (var sample in performanceSamples)
                {
                    totalFrameTime += sample.frameTime;
                    minFrameTime = Mathf.Min(minFrameTime, sample.frameTime);
                    maxFrameTime = Mathf.Max(maxFrameTime, sample.frameTime);
                    totalMemory += sample.memoryUsage;
                }
                
                report.averageFrameTime = totalFrameTime / performanceSamples.Count;
                report.averageFPS = 1000f / report.averageFrameTime;
                report.minFrameTime = minFrameTime;
                report.maxFrameTime = maxFrameTime;
                report.averageMemoryUsage = totalMemory / performanceSamples.Count;
                report.sampleCount = performanceSamples.Count;
                report.currentState = currentState;
                report.currentMode = currentPerformanceMode;
            }
            
            return report;
        }

        /// <summary>
        /// 清理性能数据
        /// </summary>
        public void ClearPerformanceData()
        {
            performanceSamples.Clear();
            UnityEngine.Debug.Log("[Performance] 性能数据已清理");
        }

        /// <summary>
        /// 强制垃圾回收
        /// </summary>
        public void ForceGarbageCollection()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            UnityEngine.Debug.Log("[Performance] 已执行垃圾回收");
        }

        /// <summary>
        /// 获取系统性能状态
        /// </summary>
        public Dictionary<string, SystemPerformanceInfo> GetSystemPerformanceInfo()
        {
            var info = new Dictionary<string, SystemPerformanceInfo>();
            
            foreach (var kvp in systemProfilers)
            {
                info[kvp.Key] = new SystemPerformanceInfo
                {
                    systemName = kvp.Value.SystemName,
                    isActive = kvp.Value.IsActive,
                    lastUpdateTime = kvp.Value.LastUpdateTime,
                    performanceImpact = kvp.Value.CalculatePerformanceImpact()
                };
            }
            
            return info;
        }

        void OnDestroy()
        {
            CancelInvoke();
        }
    }

    /// <summary>
    /// 性能样本数据
    /// </summary>
    [System.Serializable]
    public class PerformanceSample
    {
        public float timestamp;
        public float frameTime;
        public float fps;
        public long memoryUsage;
        public int drawCalls;
        public int triangles;
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float timestamp;
        public float frameTime;
        public float fps;
        public long memoryUsage;
        public int drawCalls;
        public int batches;
        public int triangles;
        public int vertices;
    }

    /// <summary>
    /// 系统性能分析器
    /// </summary>
    public class SystemProfiler
    {
        public string SystemName { get; private set; }
        public bool IsActive { get; private set; }
        public float LastUpdateTime { get; private set; }
        
        private System.Func<bool> activeChecker;
        private List<float> performanceHistory = new List<float>();
        
        public SystemProfiler(string systemName, System.Func<bool> activeChecker)
        {
            SystemName = systemName;
            this.activeChecker = activeChecker;
        }
        
        public void Update()
        {
            IsActive = activeChecker?.Invoke() ?? false;
            LastUpdateTime = Time.realtimeSinceStartup;
            
            // 记录性能历史
            float currentPerformance = IsActive ? Time.unscaledDeltaTime : 0f;
            performanceHistory.Add(currentPerformance);
            
            // 限制历史记录长度
            if (performanceHistory.Count > 60) // 保留60帧历史
            {
                performanceHistory.RemoveAt(0);
            }
        }
        
        public float CalculatePerformanceImpact()
        {
            if (performanceHistory.Count == 0) return 0f;
            
            float total = 0f;
            foreach (float value in performanceHistory)
            {
                total += value;
            }
            
            return total / performanceHistory.Count;
        }
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    [System.Serializable]
    public class PerformanceReport
    {
        public float averageFrameTime;
        public float averageFPS;
        public float minFrameTime;
        public float maxFrameTime;
        public long averageMemoryUsage;
        public int sampleCount;
        public PerformanceState currentState;
        public PerformanceMode currentMode;
    }

    /// <summary>
    /// 系统性能信息
    /// </summary>
    [System.Serializable]
    public class SystemPerformanceInfo
    {
        public string systemName;
        public bool isActive;
        public float lastUpdateTime;
        public float performanceImpact;
    }

    /// <summary>
    /// 性能状态
    /// </summary>
    public enum PerformanceState
    {
        Good,     // 良好
        Warning,  // 警告
        Critical  // 严重
    }

    /// <summary>
    /// 性能模式
    /// </summary>
    public enum PerformanceMode
    {
        Quality,     // 质量优先
        Balanced,    // 平衡模式
        Performance, // 性能优先
        Minimal      // 最小设置
    }

    /// <summary>
    /// 性能级别
    /// </summary>
    public enum PerformanceLevel
    {
        Minimal,
        Low,
        Medium,
        High
    }
}