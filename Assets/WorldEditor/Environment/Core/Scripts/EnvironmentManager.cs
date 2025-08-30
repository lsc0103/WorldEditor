using UnityEngine;
using System;
using System.Collections;
using System.Reflection;
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
        [SerializeField] private SeasonSystem seasonSystem;
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

            // 设置季节系统引用
            if (seasonSystem == null)
            {
                seasonSystem = GetComponent<SeasonSystem>();
                if (seasonSystem == null)
                {
                    seasonSystem = gameObject.AddComponent<SeasonSystem>();
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

            // 查找或创建其他子系统
            if (lightingSystem == null)
            {
                lightingSystem = GetComponent<LightingSystem>();
                if (lightingSystem == null)
                {
                    lightingSystem = gameObject.AddComponent<LightingSystem>();
                    Debug.Log("[EnvironmentManager] 自动创建LightingSystem组件");
                }
            }
            
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

            // 初始化季节系统
            if (seasonSystem == null)
            {
                seasonSystem = GetComponent<SeasonSystem>() ?? gameObject.AddComponent<SeasonSystem>();
            }
            seasonSystem.Initialize(currentState, timeSystem);

            // 初始化光照系统
            if (lightingSystem == null)
            {
                lightingSystem = GetComponent<LightingSystem>();
            }
            if (lightingSystem != null)
            {
                lightingSystem.Initialize(currentState, timeSystem, seasonSystem, weatherSystem);
            }
            
            // 初始化天气系统
            if (weatherSystem == null)
            {
                weatherSystem = GetComponent<WeatherSystem>() ?? gameObject.AddComponent<WeatherSystem>();
            }
            weatherSystem.Initialize(currentState);
            
            // 初始化天空系统
            if (skySystem == null)
            {
                skySystem = GetComponent<SkySystem>() ?? gameObject.AddComponent<SkySystem>();
            }
            skySystem.Initialize(currentState, timeSystem, weatherSystem);
            
            if (waterSystem == null)
            {
                waterSystem = GetComponent<WaterSystem>();
            }

            Debug.Log($"[EnvironmentManager] 环境子系统初始化完成 - TimeSystem: {timeSystem != null}, SeasonSystem: {seasonSystem != null}, LightingSystem: {lightingSystem != null}");
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
            if (seasonSystem != null)
            {
                seasonSystem.SetSeason(season);
            }
            
            // 同步到当前状态
            currentState.currentSeason = season;
            OnSeasonChanged?.Invoke(season);
            Debug.Log($"[EnvironmentManager] 季节设置为 {season}");
        }
        
        /// <summary>
        /// 设置季节进度
        /// </summary>
        /// <param name="progress">进度值 (0-1)</param>
        public void SetSeasonProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            if (seasonSystem != null)
            {
                seasonSystem.SetSeasonProgress(progress);
            }
            
            // 同步到当前状态
            currentState.seasonProgress = progress;
            
            Debug.Log($"[EnvironmentManager] 季节进度设置为 {progress:F2} ({GetSeasonProgressDescription(progress)})");
        }
        
        /// <summary>
        /// 获取季节进度描述
        /// </summary>
        private string GetSeasonProgressDescription(float progress)
        {
            int days = Mathf.FloorToInt(progress * 30);
            string phase = progress < 0.25f ? "初期" : progress < 0.5f ? "早期" : progress < 0.75f ? "中期" : "晚期";
            return $"{days}/30天, {phase}";
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

            // 更新天气系统
            if (weatherSystem != null && weatherSystem.IsActive)
            {
                weatherSystem.UpdateSystem();
            }

            // 更新光照系统
            if (lightingSystem != null && lightingSystem.IsActive)
            {
                lightingSystem.UpdateSystem();
            }

            // 更新天空系统
            if (skySystem != null && skySystem.IsActive)
            {
                // SkySystem通过事件响应，不需要主动更新
                // skySystem.UpdateSystem();
            }
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

        #region 诊断和调试方法

        /// <summary>
        /// 强制刷新组件引用
        /// </summary>
        [ContextMenu("刷新组件引用")]
        public void RefreshComponentReferences()
        {
            Debug.Log("==========================================");
            Debug.Log("[EnvironmentManager] 强制刷新组件引用...");
            
            SetupComponentReferences();
            
            // 显示当前状态
            Debug.Log($"TimeSystem: {(timeSystem != null ? "✓" : "❌")}");
            Debug.Log($"LightingSystem: {(lightingSystem != null ? "✓" : "❌")}");
            Debug.Log($"SeasonSystem: {(seasonSystem != null ? "✓" : "❌")}");
            
            // 如果运行时，重新初始化
            if (Application.isPlaying && !isInitialized)
            {
                Initialize();
            }
            
            Debug.Log("组件引用刷新完成！");
            Debug.Log("==========================================");
        }

        /// <summary>
        /// 诊断时间系统连接状态
        /// </summary>
        [ContextMenu("诊断时间系统连接")]
        public void DiagnoseTimeSystemConnection()
        {
            Debug.Log("==========================================");
            Debug.Log("[EnvironmentManager] 诊断时间系统连接状态...");
            
            // 检查EnvironmentManager初始化状态
            Debug.Log($"EnvironmentManager初始化状态: {isInitialized}");
            
            // 检查TimeSystem组件
            if (timeSystem == null)
            {
                Debug.LogError("❌ TimeSystem组件缺失！");
            }
            else
            {
                Debug.Log($"✓ TimeSystem组件存在");
                Debug.Log($"  - TimeSystem初始化状态: {timeSystem.IsActive}");
                Debug.Log($"  - 当前时间: {timeSystem.CurrentTime:F3}");
                Debug.Log($"  - 暂停状态: {timeSystem.IsPaused}");
                Debug.Log($"  - 时间倍率: {timeSystem.timeScale}");
            }
            
            // 检查LightingSystem连接
            if (lightingSystem == null)
            {
                Debug.LogError("❌ LightingSystem组件缺失！");
            }
            else
            {
                Debug.Log($"✓ LightingSystem组件存在");
                // 通过反射检查是否订阅了事件
                var eventInfo = typeof(TimeSystem).GetEvent("OnTimeChanged");
                Debug.Log($"  - LightingSystem连接状态检查...");
            }
            
            // 检查当前环境状态
            if (currentState != null)
            {
                Debug.Log($"✓ 环境状态存在");
                Debug.Log($"  - 环境状态中的时间: {currentState.timeOfDay:F3}");
            }
            else
            {
                Debug.LogError("❌ 环境状态缺失！");
            }
            
            Debug.Log("==========================================");
        }

        /// <summary>
        /// 强制重新连接时间系统
        /// </summary>
        [ContextMenu("强制重新连接时间系统")]
        public void ForceReconnectTimeSystem()
        {
            Debug.Log("==========================================");
            Debug.Log("[EnvironmentManager] 强制重新连接时间系统...");
            
            try
            {
                // 1. 重新设置组件引用
                SetupComponentReferences();
                
                // 2. 如果没有初始化，执行完整初始化
                if (!isInitialized)
                {
                    Initialize();
                }
                else
                {
                    // 3. 重新建立系统间通信
                    EstablishSystemCommunication();
                    
                    // 4. 强制初始化子系统
                    if (timeSystem != null && !timeSystem.IsActive)
                    {
                        timeSystem.Initialize(currentState);
                    }
                    
                    if (lightingSystem != null)
                    {
                        // 强制光照系统重新连接
                        var initMethod = lightingSystem.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (initMethod != null)
                        {
                            initMethod.Invoke(lightingSystem, new object[] { currentState, timeSystem });
                        }
                    }
                }
                
                // 5. 测试时间变化
                if (timeSystem != null)
                {
                    float testTime = timeSystem.CurrentTime;
                    testTime = (testTime + 0.1f) % 1f; // 稍微改变时间
                    timeSystem.SetTimeOfDay(testTime);
                    Debug.Log($"测试时间变化: 设置时间为 {testTime:F3}");
                }
                
                Debug.Log("✓ 时间系统重新连接完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 时间系统重新连接失败: {ex.Message}");
            }
            
            Debug.Log("==========================================");
        }

        /// <summary>
        /// 手动测试时间变化
        /// </summary>
        [ContextMenu("测试时间变化")]
        public void TestTimeChange()
        {
            if (timeSystem == null)
            {
                Debug.LogError("[EnvironmentManager] TimeSystem未找到，无法测试时间变化");
                return;
            }
            
            Debug.Log("[EnvironmentManager] 开始测试时间变化...");
            
            // 测试几个不同的时间点
            float[] testTimes = { 0.0f, 0.25f, 0.5f, 0.75f };
            string[] timeNames = { "午夜", "早晨", "正午", "傍晚" };
            
            for (int i = 0; i < testTimes.Length; i++)
            {
                timeSystem.SetTimeOfDay(testTimes[i]);
                Debug.Log($"设置时间为 {timeNames[i]} ({testTimes[i]:F2})");
                
                // 等待一帧让其他系统响应
                if (Application.isPlaying)
                {
                    StartCoroutine(WaitAndLogResult(testTimes[i], timeNames[i]));
                }
            }
        }
        
        private System.Collections.IEnumerator WaitAndLogResult(float time, string name)
        {
            yield return null;
            Debug.Log($"  -> {name} 时间设置完成，当前环境状态时间: {currentState?.timeOfDay:F3}");
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
            // 调试面板已禁用
            /*
            if (!isInitialized || !Debug.isDebugBuild) return;

            // 显示调试信息
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("环境系统状态");
            
            GUILayout.Label($"时间: {GetTimeString(currentState.timeOfDay)}");
            GUILayout.Label($"季节: {currentState.currentSeason}");
            GUILayout.Label($"天气: {currentState.currentWeather}");
            GUILayout.Label($"温度: {currentState.temperature:F1}°C");
            
            GUILayout.EndArea();
            */
        }

        #endregion
    }
}