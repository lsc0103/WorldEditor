using UnityEngine;
using System;
using System.Collections;
using WorldEditor.Core;
using WorldEditor.TerrainSystem;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境系统主管理器
    /// 统一管理所有环境子系统，与地形生成器和GPU加速引擎深度集成
    /// 
    /// 核心职责：
    /// - 管理时间、光照、天气、水体等所有环境子系统
    /// - 与AccelEngine集成，调度GPU环境计算任务
    /// - 与AdvancedTerrainGenerator双向通信，实现环境-地形联动
    /// - 提供统一的环境状态管理和事件通知
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        #region 单例模式
        
        private static EnvironmentManager _instance;
        public static EnvironmentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EnvironmentManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EnvironmentManager");
                        _instance = go.AddComponent<EnvironmentManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 系统集成引用

        [Header("核心系统集成")]
        [SerializeField] private AccelEngine accelEngine;
        [SerializeField] private AdvancedTerrainGenerator terrainGenerator;
        [SerializeField] private EnvironmentTerrainAdapter terrainAdapter;

        #endregion

        #region 环境子系统

        [Header("环境子系统")]
        [SerializeField] private TimeSystem timeSystem;
        [SerializeField] private LightingSystem lightingSystem;
        [SerializeField] private SkySystem skySystem;
        [SerializeField] private WeatherSystem weatherSystem;
        [SerializeField] private WaterSystem waterSystem;

        #endregion

        #region 环境状态

        [Header("环境状态")]
        [SerializeField] private EnvironmentState currentState;
        
        /// <summary>
        /// 当前环境状态（只读访问）
        /// </summary>
        public EnvironmentState CurrentState => currentState;

        #endregion

        #region 配置参数

        [Header("系统配置")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool enableGPUAcceleration = true;
        [SerializeField] private bool enableRealtimeUpdates = true;
        [SerializeField] private float updateFrequency = 60f; // 更新频率（FPS）

        #endregion

        #region 事件系统

        /// <summary>时间变化事件 (参数：标准化时间0-1)</summary>
        public static event Action<float> OnTimeChanged;
        
        /// <summary>天气变化事件 (参数：天气类型)</summary>
        public static event Action<WeatherType> OnWeatherChanged;
        
        /// <summary>季节变化事件 (参数：季节类型)</summary>
        public static event Action<SeasonType> OnSeasonChanged;
        
        /// <summary>环境状态更新事件 (参数：环境状态)</summary>
        public static event Action<EnvironmentState> OnEnvironmentStateUpdated;

        #endregion

        #region 初始化系统

        private bool isInitialized = false;
        private float lastUpdateTime = 0f;

        void Awake()
        {
            // 确保单例唯一性
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[EnvironmentManager] 检测到重复的EnvironmentManager实例，销毁当前实例");
                Destroy(gameObject);
                return;
            }

            // 初始化环境状态
            if (currentState == null)
            {
                currentState = new EnvironmentState();
            }
            
            // 在Awake中就设置组件引用，这样Edit模式也能看到
            SetupComponentReferences();
        }

        /// <summary>
        /// 设置组件引用（Edit模式和Play模式都可用）
        /// </summary>
        private void SetupComponentReferences()
        {
            // 设置时间系统引用
            if (timeSystem == null)
            {
                timeSystem = GetComponent<TimeSystem>();
                if (timeSystem == null)
                {
                    timeSystem = gameObject.AddComponent<TimeSystem>();
                }
            }

            // 设置地形适配器引用
            if (terrainAdapter == null)
            {
                terrainAdapter = GetComponent<EnvironmentTerrainAdapter>();
                if (terrainAdapter == null)
                {
                    terrainAdapter = gameObject.AddComponent<EnvironmentTerrainAdapter>();
                }
            }

            // 查找其他子系统（如果存在）
            if (lightingSystem == null)
                lightingSystem = GetComponent<LightingSystem>();
            if (skySystem == null)
                skySystem = GetComponent<SkySystem>();
            if (weatherSystem == null)
                weatherSystem = GetComponent<WeatherSystem>();
            if (waterSystem == null)
                waterSystem = GetComponent<WaterSystem>();
        }

        void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Unity Reset方法 - 在Inspector中添加组件时自动调用
        /// </summary>
        void Reset()
        {
            // 创建默认环境状态
            if (currentState == null)
            {
                currentState = new EnvironmentState();
            }
            
            // 设置组件引用
            SetupComponentReferences();
            
            Debug.Log("[EnvironmentManager] Reset - 组件引用已自动设置");
        }

        /// <summary>
        /// 初始化环境系统
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[EnvironmentManager] 环境系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[EnvironmentManager] 开始初始化环境系统...");

            try
            {
                // 1. 验证核心系统引用
                ValidateSystemReferences();

                // 2. 初始化AccelEngine集成
                InitializeAccelEngineIntegration();

                // 3. 初始化地形适配器
                InitializeTerrainAdapter();

                // 4. 初始化环境子系统
                InitializeSubSystems();

                // 5. 建立系统间通信
                EstablishSystemCommunication();

                // 6. 启动环境更新循环
                StartEnvironmentUpdate();

                isInitialized = true;
                Debug.Log("[EnvironmentManager] 环境系统初始化完成！");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnvironmentManager] 环境系统初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 验证系统引用完整性
        /// </summary>
        private void ValidateSystemReferences()
        {
            // 验证AccelEngine
            if (accelEngine == null)
            {
                accelEngine = FindFirstObjectByType<AccelEngine>();
                if (accelEngine == null)
                {
                    Debug.LogWarning("[EnvironmentManager] 未找到AccelEngine，GPU加速功能将被禁用");
                    enableGPUAcceleration = false;
                }
            }

            // 验证地形生成器
            if (terrainGenerator == null)
            {
                terrainGenerator = FindFirstObjectByType<AdvancedTerrainGenerator>();
                if (terrainGenerator == null)
                {
                    Debug.LogWarning("[EnvironmentManager] 未找到AdvancedTerrainGenerator，地形集成功能受限");
                }
            }

            Debug.Log($"[EnvironmentManager] 系统引用验证完成 - AccelEngine: {accelEngine != null}, TerrainGenerator: {terrainGenerator != null}");
        }

        /// <summary>
        /// 初始化AccelEngine集成
        /// </summary>
        private void InitializeAccelEngineIntegration()
        {
            if (!enableGPUAcceleration || accelEngine == null)
            {
                Debug.Log("[EnvironmentManager] GPU加速功能已禁用");
                return;
            }

            // 注册环境系统到AccelEngine
            // TODO: 实现环境GPU任务注册
            Debug.Log("[EnvironmentManager] AccelEngine集成初始化完成");
        }

        /// <summary>
        /// 初始化地形适配器
        /// </summary>
        private void InitializeTerrainAdapter()
        {
            if (terrainAdapter == null)
            {
                terrainAdapter = GetComponent<EnvironmentTerrainAdapter>();
                if (terrainAdapter == null)
                {
                    terrainAdapter = gameObject.AddComponent<EnvironmentTerrainAdapter>();
                }
            }

            // 配置适配器
            terrainAdapter.Initialize(this, terrainGenerator);
            Debug.Log("[EnvironmentManager] 地形适配器初始化完成");
        }

        /// <summary>
        /// 初始化环境子系统
        /// </summary>
        private void InitializeSubSystems()
        {
            // 初始化时间系统
            if (timeSystem == null)
            {
                timeSystem = GetComponent<TimeSystem>() ?? gameObject.AddComponent<TimeSystem>();
            }
            timeSystem.Initialize(currentState);

            // 初始化其他子系统（如果存在）
            if (lightingSystem == null)
            {
                lightingSystem = GetComponent<LightingSystem>();
            }
            
            if (skySystem == null)
            {
                skySystem = GetComponent<SkySystem>();
            }
            
            if (weatherSystem == null)
            {
                weatherSystem = GetComponent<WeatherSystem>();
            }
            
            if (waterSystem == null)
            {
                waterSystem = GetComponent<WaterSystem>();
            }

            Debug.Log($"[EnvironmentManager] 环境子系统初始化完成 - TimeSystem: {timeSystem != null}");
        }

        /// <summary>
        /// 建立系统间通信
        /// </summary>
        private void EstablishSystemCommunication()
        {
            // 订阅时间系统事件
            if (timeSystem != null)
            {
                timeSystem.OnTimeChanged += HandleTimeChanged;
            }

            Debug.Log("[EnvironmentManager] 系统间通信建立完成");
        }

        /// <summary>
        /// 启动环境更新循环
        /// </summary>
        private void StartEnvironmentUpdate()
        {
            if (enableRealtimeUpdates)
            {
                StartCoroutine(EnvironmentUpdateCoroutine());
                Debug.Log("[EnvironmentManager] 环境实时更新已启动");
            }
        }

        #endregion

        #region 核心接口方法

        /// <summary>
        /// 设置一天中的时间
        /// </summary>
        /// <param name="normalizedTime">标准化时间 0-1 (0=午夜, 0.5=正午, 1=午夜)</param>
        public void SetTimeOfDay(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            
            if (timeSystem != null)
            {
                timeSystem.SetTimeOfDay(normalizedTime);
            }
            
            currentState.timeOfDay = normalizedTime;
            OnTimeChanged?.Invoke(normalizedTime);
            
            Debug.Log($"[EnvironmentManager] 时间设置为: {normalizedTime:F2} ({GetTimeString(normalizedTime)})");
        }

        /// <summary>
        /// 设置季节
        /// </summary>
        /// <param name="season">季节类型</param>
        public void SetSeason(SeasonType season)
        {
            var previousSeason = currentState.currentSeason;
            currentState.currentSeason = season;
            
            if (previousSeason != season)
            {
                OnSeasonChanged?.Invoke(season);
                Debug.Log($"[EnvironmentManager] 季节从 {previousSeason} 变更为 {season}");
            }
        }

        /// <summary>
        /// 设置天气
        /// </summary>
        /// <param name="weather">天气类型</param>
        /// <param name="intensity">天气强度 0-1</param>
        public void SetWeather(WeatherType weather, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            
            var previousWeather = currentState.currentWeather;
            currentState.currentWeather = weather;
            currentState.weatherIntensity = intensity;
            
            if (weatherSystem != null)
            {
                weatherSystem.SetWeather(weather, intensity);
            }
            
            if (previousWeather != weather)
            {
                OnWeatherChanged?.Invoke(weather);
                Debug.Log($"[EnvironmentManager] 天气从 {previousWeather} 变更为 {weather} (强度: {intensity:F2})");
            }
        }

        /// <summary>
        /// 手动触发环境更新
        /// </summary>
        public void UpdateEnvironment()
        {
            if (!isInitialized) return;

            // 更新所有子系统
            UpdateSubSystems();
            
            // 触发环境状态更新事件
            OnEnvironmentStateUpdated?.Invoke(currentState);
        }

        #endregion

        #region 环境更新循环

        /// <summary>
        /// 环境更新协程
        /// </summary>
        private IEnumerator EnvironmentUpdateCoroutine()
        {
            while (enableRealtimeUpdates && isInitialized)
            {
                float deltaTime = Time.time - lastUpdateTime;
                float targetInterval = 1f / updateFrequency;
                
                if (deltaTime >= targetInterval)
                {
                    UpdateEnvironment();
                    lastUpdateTime = Time.time;
                }
                
                yield return null;
            }
        }

        /// <summary>
        /// 更新所有环境子系统
        /// </summary>
        private void UpdateSubSystems()
        {
            // 更新时间系统
            if (timeSystem != null && timeSystem.IsActive)
            {
                timeSystem.UpdateSystem();
            }

            // TODO: 更新其他子系统
            // if (lightingSystem != null) lightingSystem.UpdateSystem();
            // if (weatherSystem != null) weatherSystem.UpdateSystem();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理时间变化事件
        /// </summary>
        private void HandleTimeChanged(float normalizedTime)
        {
            currentState.timeOfDay = normalizedTime;
            
            // 通知其他系统时间变化
            OnTimeChanged?.Invoke(normalizedTime);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 将标准化时间转换为时间字符串
        /// </summary>
        private string GetTimeString(float normalizedTime)
        {
            float hours = normalizedTime * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            return $"{hour:D2}:{minute:D2}";
        }

        #endregion

        #region 调试信息

        void OnGUI()
        {
            if (!isInitialized || !Debug.isDebugBuild) return;

            // 显示调试信息
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("环境系统状态");
            
            GUILayout.Label($"时间: {GetTimeString(currentState.timeOfDay)}");
            GUILayout.Label($"季节: {currentState.currentSeason}");
            GUILayout.Label($"天气: {currentState.currentWeather}");
            GUILayout.Label($"温度: {currentState.temperature:F1}°C");
            
            GUILayout.EndArea();
        }

        #endregion
    }
}