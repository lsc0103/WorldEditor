using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;
using WorldEditor.Environment;

namespace WorldEditor.Optimization
{
    /// <summary>
    /// 世界优化器 - 性能优化和大世界支持
    /// </summary>
    public class WorldOptimizer : MonoBehaviour
    {
        [Header("优化设置")]
        [SerializeField] private bool enableOptimization = true;
        [SerializeField] private bool enableLODSystem = true;
        [SerializeField] private bool enableOcclusionCulling = true;
        [SerializeField] private bool enableStreamingLOD = true;
        
        [Header("LOD设置")]
        [SerializeField] private float[] lodDistances = { 50f, 150f, 500f, 1500f };
        [SerializeField] private int maxActiveChunks = 25;
        [SerializeField] private float chunkSize = 100f;
        
        [Header("性能监控")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float performanceCheckInterval = 1f;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private Camera mainCamera;
        private float lastPerformanceCheck;
        private Queue<float> frameTimeHistory = new Queue<float>();
        private const int FRAME_HISTORY_SIZE = 60;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            SetupOptimization();
        }
        
        void SetupOptimization()
        {
            if (enableLODSystem)
            {
                SetupLODSystem();
            }
            
            if (enableOcclusionCulling)
            {
                SetupOcclusionCulling();
            }
        }
        
        void SetupLODSystem()
        {
            // 设置LOD系统
            Debug.Log("[Optimizer] 设置LOD系统...");
            
            // 配置流式LOD
            if (enableStreamingLOD)
            {
                Debug.Log("[Optimizer] 启用流式LOD系统");
                SetupStreamingLOD();
            }
            else
            {
                Debug.Log("[Optimizer] 使用静态LOD系统");
            }
        }
        
        void SetupStreamingLOD()
        {
            // 设置动态LOD流式加载
            QualitySettings.lodBias = 1.0f;
            QualitySettings.maximumLODLevel = 0;
            
            // 启用LOD淡入淡出
            QualitySettings.enableLODCrossFade = true;
            
            Debug.Log("[Optimizer] 流式LOD配置完成");
        }
        
        void SetupOcclusionCulling()
        {
            // 设置遮挡剔除
            if (mainCamera != null)
            {
                mainCamera.useOcclusionCulling = true;
            }
        }
        
        /// <summary>
        /// 更新优化系统
        /// </summary>
        public void UpdateOptimization()
        {
            if (!enableOptimization) return;
            
            // 性能监控
            if (enablePerformanceMonitoring)
            {
                MonitorPerformance();
            }
            
            // 动态优化调整
            if (Time.time - lastPerformanceCheck > performanceCheckInterval)
            {
                PerformOptimizationAdjustments();
                lastPerformanceCheck = Time.time;
            }
        }
        
        void MonitorPerformance()
        {
            float frameTime = Time.unscaledDeltaTime;
            frameTimeHistory.Enqueue(frameTime);
            
            if (frameTimeHistory.Count > FRAME_HISTORY_SIZE)
            {
                frameTimeHistory.Dequeue();
            }
        }
        
        void PerformOptimizationAdjustments()
        {
            float averageFrameTime = CalculateAverageFrameTime();
            float currentFPS = 1f / averageFrameTime;
            
            if (currentFPS < targetFrameRate * 0.8f)
            {
                // 性能低于目标，增加优化
                IncreaseOptimization();
            }
            else if (currentFPS > targetFrameRate * 1.2f)
            {
                // 性能良好，可以减少优化提升质量
                DecreaseOptimization();
            }
        }
        
        float CalculateAverageFrameTime()
        {
            if (frameTimeHistory.Count == 0) return 0.016f; // 默认60FPS
            
            float total = 0f;
            foreach (float frameTime in frameTimeHistory)
            {
                total += frameTime;
            }
            
            return total / frameTimeHistory.Count;
        }
        
        void IncreaseOptimization()
        {
            Debug.Log("[Optimizer] 增加优化级别...");
            
            // 减少LOD距离
            for (int i = 0; i < lodDistances.Length; i++)
            {
                lodDistances[i] *= 0.9f;
            }
            
            // 减少活动区块数量
            maxActiveChunks = Mathf.Max(9, maxActiveChunks - 2);
        }
        
        void DecreaseOptimization()
        {
            Debug.Log("[Optimizer] 减少优化级别...");
            
            // 增加LOD距离
            for (int i = 0; i < lodDistances.Length; i++)
            {
                lodDistances[i] *= 1.1f;
            }
            
            // 增加活动区块数量
            maxActiveChunks = Mathf.Min(49, maxActiveChunks + 2);
        }
        
        /// <summary>
        /// 获取优化统计信息
        /// </summary>
        public string GetOptimizationStats()
        {
            float averageFrameTime = CalculateAverageFrameTime();
            float currentFPS = 1f / averageFrameTime;
            
            return $"优化统计:\n" +
                   $"当前FPS: {currentFPS:F1}\n" +
                   $"目标FPS: {targetFrameRate}\n" +
                   $"活动区块: {maxActiveChunks}\n" +
                   $"区块大小: {chunkSize}m\n" +
                   $"LOD距离: {string.Join(", ", lodDistances)}\n" +
                   $"优化启用: {enableOptimization}";
        }
    }
}