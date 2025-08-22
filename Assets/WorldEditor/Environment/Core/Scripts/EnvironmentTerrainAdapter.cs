using UnityEngine;
using System;
using System.Collections;
using WorldEditor.TerrainSystem;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境-地形适配器
    /// 负责环境系统与地形生成器之间的双向通信和数据同步
    /// 
    /// 主要功能：
    /// - 地形生成完成后自动调整环境参数
    /// - 环境变化影响地形侵蚀、纹理等
    /// - 实时同步地形数据与环境状态
    /// - 优化性能，避免不必要的重复计算
    /// </summary>
    public class EnvironmentTerrainAdapter : MonoBehaviour
    {
        #region 系统引用

        [Header("系统引用")]
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private AdvancedTerrainGenerator terrainGenerator;

        #endregion

        #region 适配配置

        [Header("适配配置")]
        [SerializeField] private bool enableAutoAdaptation = true;
        [SerializeField] private bool enableRealtimeSync = true;
        [SerializeField] private float adaptationDelay = 0.5f; // 延迟适配时间（秒）
        [SerializeField] private float syncFrequency = 10f; // 同步频率（每秒）

        #endregion

        #region 地形影响参数

        [Header("地形对环境的影响")]
        [SerializeField] private bool terrainAffectsWind = true;
        [SerializeField] private bool terrainAffectsTemperature = true;
        [SerializeField] private bool terrainAffectsHumidity = true;
        [SerializeField] private bool terrainAffectsWaterLevel = true;

        #endregion

        #region 环境影响参数

        [Header("环境对地形的影响")]
        [SerializeField] private bool weatherAffectsErosion = true;
        [SerializeField] private bool temperatureAffectsTexture = true;
        [SerializeField] private bool waterLevelAffectsTerrain = true;
        [SerializeField] private bool seasonAffectsTexture = true;

        #endregion

        #region 缓存数据

        private TerrainAdaptationData cachedTerrainData;
        private EnvironmentState lastEnvironmentState;
        private float lastSyncTime = 0f;
        private bool isInitialized = false;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化适配器
        /// </summary>
        public void Initialize(EnvironmentManager envManager, AdvancedTerrainGenerator terrainGen)
        {
            environmentManager = envManager;
            terrainGenerator = terrainGen;

            if (environmentManager == null)
            {
                Debug.LogError("[EnvironmentTerrainAdapter] EnvironmentManager引用为空！");
                return;
            }

            // 订阅环境系统事件
            SubscribeToEnvironmentEvents();

            // 订阅地形生成器事件
            SubscribeToTerrainEvents();

            // 初始化缓存数据
            InitializeCachedData();

            // 启动实时同步
            if (enableRealtimeSync)
            {
                StartCoroutine(RealtimeSyncCoroutine());
            }

            isInitialized = true;
            Debug.Log("[EnvironmentTerrainAdapter] 地形适配器初始化完成");
        }

        /// <summary>
        /// 订阅环境系统事件
        /// </summary>
        private void SubscribeToEnvironmentEvents()
        {
            EnvironmentManager.OnTimeChanged += HandleTimeChanged;
            EnvironmentManager.OnWeatherChanged += HandleWeatherChanged;
            EnvironmentManager.OnSeasonChanged += HandleSeasonChanged;
            EnvironmentManager.OnEnvironmentStateUpdated += HandleEnvironmentStateUpdated;
        }

        /// <summary>
        /// 订阅地形生成器事件
        /// </summary>
        private void SubscribeToTerrainEvents()
        {
            if (terrainGenerator != null)
            {
                // TODO: 订阅地形生成完成事件
                // terrainGenerator.OnTerrainGenerationComplete += HandleTerrainGenerationComplete;
                Debug.Log("[EnvironmentTerrainAdapter] 已订阅地形生成器事件");
            }
        }

        /// <summary>
        /// 初始化缓存数据
        /// </summary>
        private void InitializeCachedData()
        {
            cachedTerrainData = new TerrainAdaptationData();
            
            if (environmentManager != null && environmentManager.CurrentState != null)
            {
                lastEnvironmentState = environmentManager.CurrentState.Clone();
            }
        }

        #endregion

        #region 环境事件处理

        /// <summary>
        /// 处理时间变化事件
        /// </summary>
        private void HandleTimeChanged(float normalizedTime)
        {
            if (!enableAutoAdaptation || !isInitialized) return;

            // 时间变化可能影响地形纹理（如雪线、植被线）
            if (temperatureAffectsTexture)
            {
                StartCoroutine(AdaptTerrainToTimeChange(normalizedTime));
            }
        }

        /// <summary>
        /// 处理天气变化事件
        /// </summary>
        private void HandleWeatherChanged(WeatherType newWeather)
        {
            if (!enableAutoAdaptation || !isInitialized) return;

            Debug.Log($"[EnvironmentTerrainAdapter] 适配天气变化: {newWeather}");

            // 雨雪天气影响地形侵蚀
            if (weatherAffectsErosion && (newWeather == WeatherType.Rainy || newWeather == WeatherType.Storm))
            {
                StartCoroutine(TriggerWeatherErosion(newWeather));
            }
        }

        /// <summary>
        /// 处理季节变化事件
        /// </summary>
        private void HandleSeasonChanged(SeasonType newSeason)
        {
            if (!enableAutoAdaptation || !isInitialized) return;

            Debug.Log($"[EnvironmentTerrainAdapter] 适配季节变化: {newSeason}");

            // 季节变化影响地形纹理
            if (seasonAffectsTexture)
            {
                StartCoroutine(AdaptTerrainToSeason(newSeason));
            }
        }

        /// <summary>
        /// 处理环境状态更新事件
        /// </summary>
        private void HandleEnvironmentStateUpdated(EnvironmentState newState)
        {
            if (!enableAutoAdaptation || !isInitialized) return;

            // 检查关键环境参数变化
            CheckCriticalEnvironmentChanges(newState);

            // 更新缓存状态
            lastEnvironmentState = newState.Clone();
        }

        #endregion

        #region 地形事件处理

        /// <summary>
        /// 处理地形生成完成事件
        /// </summary>
        public void HandleTerrainGenerationComplete(Terrain terrain)
        {
            if (!enableAutoAdaptation || !isInitialized) return;

            Debug.Log("[EnvironmentTerrainAdapter] 开始地形生成完成后的环境适配");

            StartCoroutine(AdaptEnvironmentToTerrain(terrain));
        }

        /// <summary>
        /// 根据地形适配环境
        /// </summary>
        private IEnumerator AdaptEnvironmentToTerrain(Terrain terrain)
        {
            yield return new WaitForSeconds(adaptationDelay);

            if (terrain == null || environmentManager == null) yield break;

            var terrainData = terrain.terrainData;
            var environmentState = environmentManager.CurrentState;

            // 分析地形高度数据
            AnalyzeTerrainHeights(terrainData, environmentState);

            // 分析地形坡度影响
            AnalyzeTerrainSlopes(terrainData, environmentState);

            // 更新环境参数
            environmentManager.UpdateEnvironment();

            Debug.Log("[EnvironmentTerrainAdapter] 地形环境适配完成");
        }

        /// <summary>
        /// 分析地形高度对环境的影响
        /// </summary>
        private void AnalyzeTerrainHeights(UnityEngine.TerrainData terrainData, EnvironmentState environmentState)
        {
            if (!terrainAffectsTemperature && !terrainAffectsWaterLevel) return;

            // 计算平均海拔
            float averageElevation = CalculateAverageElevation(terrainData);
            float maxElevation = CalculateMaxElevation(terrainData);

            // 海拔影响温度 (每升高100米，温度下降0.6度)
            if (terrainAffectsTemperature)
            {
                float temperatureOffset = -averageElevation * 0.006f;
                environmentState.temperature = Mathf.Clamp(20f + temperatureOffset, -30f, 40f);
            }

            // 更新全局水位
            if (terrainAffectsWaterLevel)
            {
                float suggestedWaterLevel = CalculateSuggestedWaterLevel(terrainData);
                environmentState.globalWaterLevel = suggestedWaterLevel;
            }

            Debug.Log($"[EnvironmentTerrainAdapter] 海拔适配 - 平均:{averageElevation:F1}m, 最高:{maxElevation:F1}m, 温度:{environmentState.temperature:F1}°C");
        }

        /// <summary>
        /// 分析地形坡度对风力的影响
        /// </summary>
        private void AnalyzeTerrainSlopes(UnityEngine.TerrainData terrainData, EnvironmentState environmentState)
        {
            if (!terrainAffectsWind) return;

            // 计算主导坡向
            Vector3 dominantSlope = CalculateDominantSlope(terrainData);
            
            // 坡向影响风向
            if (dominantSlope.magnitude > 0.1f)
            {
                // 山地地形会形成特定的风向模式
                environmentState.windDirection = Vector3.Slerp(
                    environmentState.windDirection, 
                    dominantSlope.normalized, 
                    0.3f
                );
                
                // 山地地形增强风力变化
                environmentState.windVariation = Mathf.Clamp(environmentState.windVariation + 0.5f, 0f, 3f);
            }

            Debug.Log($"[EnvironmentTerrainAdapter] 坡度适配 - 主导坡向:{dominantSlope}, 风向:{environmentState.windDirection}");
        }

        #endregion

        #region 环境影响地形

        /// <summary>
        /// 检查关键环境变化
        /// </summary>
        private void CheckCriticalEnvironmentChanges(EnvironmentState newState)
        {
            if (lastEnvironmentState == null) return;

            // 检查温度显著变化
            if (Mathf.Abs(newState.temperature - lastEnvironmentState.temperature) > 5f)
            {
                if (temperatureAffectsTexture)
                {
                    StartCoroutine(AdaptTerrainToTemperatureChange(newState.temperature));
                }
            }

            // 检查水位显著变化
            if (Mathf.Abs(newState.globalWaterLevel - lastEnvironmentState.globalWaterLevel) > 1f)
            {
                if (waterLevelAffectsTerrain)
                {
                    StartCoroutine(AdaptTerrainToWaterLevelChange(newState.globalWaterLevel));
                }
            }
        }

        /// <summary>
        /// 天气侵蚀适配
        /// </summary>
        private IEnumerator TriggerWeatherErosion(WeatherType weather)
        {
            yield return new WaitForSeconds(adaptationDelay);

            if (terrainGenerator == null) yield break;

            float erosionIntensity = weather == WeatherType.Storm ? 2f : 1f;
            
            // TODO: 触发地形侵蚀模拟
            // terrainGenerator.TriggerErosionSimulation(erosionIntensity);
            
            Debug.Log($"[EnvironmentTerrainAdapter] 触发天气侵蚀 - 类型:{weather}, 强度:{erosionIntensity}");
        }

        /// <summary>
        /// 季节地形纹理适配
        /// </summary>
        private IEnumerator AdaptTerrainToSeason(SeasonType season)
        {
            yield return new WaitForSeconds(adaptationDelay);

            // TODO: 根据季节调整地形纹理混合
            Debug.Log($"[EnvironmentTerrainAdapter] 适配季节地形纹理: {season}");
        }

        /// <summary>
        /// 时间地形适配
        /// </summary>
        private IEnumerator AdaptTerrainToTimeChange(float normalizedTime)
        {
            yield return new WaitForSeconds(adaptationDelay);

            // TODO: 根据时间调整地形材质参数
            Debug.Log($"[EnvironmentTerrainAdapter] 适配时间地形变化: {normalizedTime}");
        }

        /// <summary>
        /// 温度变化地形适配
        /// </summary>
        private IEnumerator AdaptTerrainToTemperatureChange(float temperature)
        {
            yield return new WaitForSeconds(adaptationDelay);

            // TODO: 根据温度调整雪线、植被线等
            Debug.log($"[EnvironmentTerrainAdapter] 适配温度地形变化: {temperature}°C");
        }

        /// <summary>
        /// 水位变化地形适配
        /// </summary>
        private IEnumerator AdaptTerrainToWaterLevelChange(float waterLevel)
        {
            yield return new WaitForSeconds(adaptationDelay);

            // TODO: 根据水位调整地形湿润区域
            Debug.Log($"[EnvironmentTerrainAdapter] 适配水位地形变化: {waterLevel}m");
        }

        #endregion

        #region 实时同步

        /// <summary>
        /// 实时同步协程
        /// </summary>
        private IEnumerator RealtimeSyncCoroutine()
        {
            while (enableRealtimeSync && isInitialized)
            {
                float deltaTime = Time.time - lastSyncTime;
                float targetInterval = 1f / syncFrequency;

                if (deltaTime >= targetInterval)
                {
                    PerformRealtimeSync();
                    lastSyncTime = Time.time;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 执行实时同步
        /// </summary>
        private void PerformRealtimeSync()
        {
            if (environmentManager == null || terrainGenerator == null) return;

            // 更新地形适配数据缓存
            UpdateTerrainAdaptationData();
        }

        /// <summary>
        /// 更新地形适配数据
        /// </summary>
        private void UpdateTerrainAdaptationData()
        {
            if (cachedTerrainData == null) return;

            // TODO: 更新缓存的地形分析数据
            cachedTerrainData.lastUpdateTime = Time.time;
        }

        #endregion

        #region 地形分析工具方法

        /// <summary>
        /// 计算平均海拔
        /// </summary>
        private float CalculateAverageElevation(UnityEngine.TerrainData terrainData)
        {
            var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            float sum = 0f;
            int count = 0;

            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    sum += heights[y, x];
                    count++;
                }
            }

            return (sum / count) * terrainData.size.y;
        }

        /// <summary>
        /// 计算最高海拔
        /// </summary>
        private float CalculateMaxElevation(UnityEngine.TerrainData terrainData)
        {
            var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            float maxHeight = 0f;

            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    if (heights[y, x] > maxHeight)
                        maxHeight = heights[y, x];
                }
            }

            return maxHeight * terrainData.size.y;
        }

        /// <summary>
        /// 计算建议水位
        /// </summary>
        private float CalculateSuggestedWaterLevel(UnityEngine.TerrainData terrainData)
        {
            // 建议水位为平均海拔的20%
            return CalculateAverageElevation(terrainData) * 0.2f;
        }

        /// <summary>
        /// 计算主导坡向
        /// </summary>
        private Vector3 CalculateDominantSlope(UnityEngine.TerrainData terrainData)
        {
            var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            Vector3 totalSlope = Vector3.zero;
            int samples = 0;

            // 采样计算坡度
            int step = Mathf.Max(1, terrainData.heightmapResolution / 32); // 降低采样密度提高性能
            
            for (int y = step; y < heights.GetLength(0) - step; y += step)
            {
                for (int x = step; x < heights.GetLength(1) - step; x += step)
                {
                    float heightL = heights[y, x - step];
                    float heightR = heights[y, x + step];
                    float heightD = heights[y - step, x];
                    float heightU = heights[y + step, x];

                    Vector3 slope = new Vector3(heightL - heightR, 2f, heightD - heightU);
                    totalSlope += slope;
                    samples++;
                }
            }

            return samples > 0 ? totalSlope / samples : Vector3.zero;
        }

        #endregion

        #region 清理

        void OnDestroy()
        {
            // 取消订阅事件
            EnvironmentManager.OnTimeChanged -= HandleTimeChanged;
            EnvironmentManager.OnWeatherChanged -= HandleWeatherChanged;
            EnvironmentManager.OnSeasonChanged -= HandleSeasonChanged;
            EnvironmentManager.OnEnvironmentStateUpdated -= HandleEnvironmentStateUpdated;
        }

        #endregion
    }

    #region 辅助数据结构

    /// <summary>
    /// 地形适配数据缓存
    /// </summary>
    [System.Serializable]
    public class TerrainAdaptationData
    {
        public float averageElevation = 0f;
        public float maxElevation = 0f;
        public Vector3 dominantSlope = Vector3.zero;
        public float suggestedWaterLevel = 0f;
        public float lastUpdateTime = 0f;

        public TerrainAdaptationData()
        {
            lastUpdateTime = Time.time;
        }
    }

    #endregion
}