using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 动态环境系统 - 超越Enviro 3的综合环境控制解决方案
    /// 支持真实物理天气模拟、体积大气渲染、动态光照、音频环境等
    /// </summary>
    public class DynamicEnvironmentSystem : MonoBehaviour
    {
        [Header("环境系统核心")]
        [SerializeField] private bool enableDynamicEnvironment = true;
        [SerializeField] private bool enableRealTimeUpdates = true;
        [SerializeField] private float updateFrequency = 0.1f;
        
        [Header("子系统组件")]
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private DayNightCycleController dayNightController;
        [SerializeField] private AtmosphereRenderer atmosphereRenderer;
        [SerializeField] private VolumetricCloudSystem cloudSystem;
        [SerializeField] private LightingController lightingController;
        [SerializeField] private EnvironmentAudioController audioController;
        [SerializeField] private SeasonController seasonController;
        
        [Header("全局环境设置")]
        [SerializeField] private EnvironmentProfile currentProfile;
        [SerializeField] private bool useGlobalProfile = true;
        [SerializeField] private float environmentTransitionSpeed = 1f;
        
        [Header("物理模拟")]
        [SerializeField] private bool enablePhysicsBasedWeather = true;
        [SerializeField] private bool enableAtmosphericPressure = true;
        [SerializeField] private bool enableWindSimulation = true;
        [SerializeField] private bool enableTemperatureGradients = true;
        
        [Header("性能设置")]
        [SerializeField] private EnvironmentQuality quality = EnvironmentQuality.High;
        [SerializeField] private bool enableLODSystem = true;
        [SerializeField] private float maxUpdateDistance = 1000f;
        
        // 事件
        public System.Action<WeatherType> OnWeatherChanged;
        public System.Action<float> OnTimeOfDayChanged;
        public System.Action<Season> OnSeasonChanged;
        public System.Action<EnvironmentState> OnEnvironmentStateChanged;
        
        // 私有变量
        private EnvironmentState currentState;
        private EnvironmentState targetState;
        private float lastUpdateTime;
        private Coroutine environmentUpdateCoroutine;
        private Camera mainCamera;
        
        // 环境数据
        private EnvironmentData environmentData;
        private WeatherSystem weatherSystem;
        private AtmosphericData atmosphericData;
        
        void Awake()
        {
            InitializeEnvironmentSystem();
        }
        
        void Start()
        {
            SetupEnvironmentComponents();
            StartEnvironmentUpdates();
        }
        
        void InitializeEnvironmentSystem()
        {
            // 初始化主摄像机引用
            mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            
            // 初始化环境数据
            environmentData = new EnvironmentData();
            weatherSystem = new WeatherSystem();
            atmosphericData = new AtmosphericData();
            
            // 初始化当前状态
            currentState = new EnvironmentState();
            targetState = new EnvironmentState();
            
            // 加载默认配置
            if (currentProfile == null)
            {
                LoadDefaultProfile();
            }
            
            // 如果启用全局配置文件，应用其设置
            if (useGlobalProfile && currentProfile != null)
            {
                ApplyGlobalProfileSettings();
            }
        }
        
        void SetupEnvironmentComponents()
        {
            // 初始化子系统组件
            if (weatherController == null)
                weatherController = GetComponent<WeatherController>() ?? gameObject.AddComponent<WeatherController>();
                
            if (dayNightController == null)
                dayNightController = GetComponent<DayNightCycleController>() ?? gameObject.AddComponent<DayNightCycleController>();
                
            if (atmosphereRenderer == null)
                atmosphereRenderer = GetComponent<AtmosphereRenderer>() ?? gameObject.AddComponent<AtmosphereRenderer>();
                
            if (cloudSystem == null)
                cloudSystem = GetComponent<VolumetricCloudSystem>() ?? gameObject.AddComponent<VolumetricCloudSystem>();
                
            if (lightingController == null)
                lightingController = GetComponent<LightingController>() ?? gameObject.AddComponent<LightingController>();
                
            if (audioController == null)
                audioController = GetComponent<EnvironmentAudioController>() ?? gameObject.AddComponent<EnvironmentAudioController>();
                
            if (seasonController == null)
                seasonController = GetComponent<SeasonController>() ?? gameObject.AddComponent<SeasonController>();
            
            // 初始化各个子系统
            InitializeSubSystems();
        }
        
        void InitializeSubSystems()
        {
            weatherController.Initialize(this);
            dayNightController.Initialize(this);
            atmosphereRenderer.Initialize(this);
            cloudSystem.Initialize(this);
            lightingController.Initialize(this);
            audioController.Initialize(this);
            seasonController.Initialize(this);
        }
        
        void StartEnvironmentUpdates()
        {
            if (enableRealTimeUpdates)
            {
                environmentUpdateCoroutine = StartCoroutine(EnvironmentUpdateLoop());
            }
        }
        
        /// <summary>
        /// 环境更新循环
        /// </summary>
        IEnumerator EnvironmentUpdateLoop()
        {
            while (enableDynamicEnvironment)
            {
                float deltaTime = Time.time - lastUpdateTime;
                
                if (deltaTime >= updateFrequency)
                {
                    UpdateEnvironment(deltaTime);
                    lastUpdateTime = Time.time;
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 更新环境状态
        /// </summary>
        void UpdateEnvironment(float deltaTime)
        {
            if (!enableDynamicEnvironment) return;
            
            // 更新大气数据
            atmosphericData.Update(deltaTime);
            
            // 更新大气压力（如果启用）
            if (enableAtmosphericPressure)
            {
                UpdateAtmosphericPressure(deltaTime);
            }
            
            // 更新温度梯度（如果启用）
            if (enableTemperatureGradients)
            {
                UpdateTemperatureGradients(deltaTime);
            }
            
            // 更新风模拟（如果启用）
            if (enableWindSimulation)
            {
                UpdateWindSimulation(deltaTime);
            }
            
            // 更新天气系统
            weatherSystem.Update(deltaTime, atmosphericData);
            
            // 更新各个子系统
            UpdateSubSystems(deltaTime);
            
            // 插值到目标状态
            InterpolateToTargetState(deltaTime);
            
            // 触发环境状态改变事件
            OnEnvironmentStateChanged?.Invoke(currentState);
        }
        
        /// <summary>
        /// 更新温度梯度效应
        /// </summary>
        void UpdateTemperatureGradients(float deltaTime)
        {
            if (mainCamera == null) return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            // 基于高度计算温度梯度
            float heightBasedTemperature = CalculateHeightBasedTemperature(cameraPos.y);
            
            // 基于纬度计算温度变化（假设Z轴为南北方向）
            float latitudeBasedTemperature = CalculateLatitudeBasedTemperature(cameraPos.z);
            
            // 应用温度梯度到当前环境状态
            float gradientTemperature = (heightBasedTemperature + latitudeBasedTemperature) * 0.5f;
            
            // 平滑过渡到梯度温度
            currentState.temperature = Mathf.Lerp(currentState.temperature, gradientTemperature, deltaTime * 0.5f);
            
            // 设置全局着色器参数
            Shader.SetGlobalFloat("_TemperatureGradient", gradientTemperature);
            Shader.SetGlobalFloat("_HeightTemperature", heightBasedTemperature);
            Shader.SetGlobalFloat("_LatitudeTemperature", latitudeBasedTemperature);
        }
        
        /// <summary>
        /// 计算基于高度的温度
        /// </summary>
        float CalculateHeightBasedTemperature(float height)
        {
            // 标准大气层递减率：每100米高度下降0.65度
            float temperatureLapseRate = 0.0065f;
            float seaLevelTemperature = 15f; // 海平面基准温度
            
            float temperatureAtHeight = seaLevelTemperature - (height * temperatureLapseRate);
            
            return Mathf.Clamp(temperatureAtHeight, -40f, 50f); // 限制在合理范围内
        }
        
        /// <summary>
        /// 计算基于纬度的温度
        /// </summary>
        float CalculateLatitudeBasedTemperature(float worldZ)
        {
            // 假设世界Z轴范围为-5000到5000，映射到纬度-90到90度
            float normalizedLatitude = Mathf.Clamp(worldZ / 5000f, -1f, 1f);
            float latitude = normalizedLatitude * 90f; // -90到90度
            
            // 基于纬度的温度变化（简化模型）
            float latitudeTemperature = Mathf.Cos(latitude * Mathf.Deg2Rad) * 20f + 10f;
            
            return latitudeTemperature;
        }
        
        /// <summary>
        /// 更新大气压力系统
        /// </summary>
        void UpdateAtmosphericPressure(float deltaTime)
        {
            if (mainCamera == null) return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            // 基于高度计算大气压力（海拔越高压力越低）
            float altitude = cameraPos.y;
            float standardPressure = 1013.25f; // 海平面标准大气压（毫巴）
            
            // 使用大气压力高度公式的简化版本
            float pressureAtAltitude = standardPressure * Mathf.Pow(1f - (altitude * 0.0065f) / 288.15f, 5.255f);
            
            // 转换为标准化值（0-1范围）
            float normalizedPressure = Mathf.Clamp01(pressureAtAltitude / standardPressure);
            
            // 平滑过渡到新的大气压力值
            currentState.atmosphericPressure = Mathf.Lerp(currentState.atmosphericPressure, normalizedPressure, deltaTime * 0.5f);
            
            // 大气压力影响天气系统
            if (weatherSystem != null)
            {
                weatherSystem.SetAtmosphericPressure(normalizedPressure);
            }
            
            // 设置全局着色器参数
            Shader.SetGlobalFloat("_AtmosphericPressure", normalizedPressure);
            Shader.SetGlobalFloat("_Altitude", altitude);
        }
        
        /// <summary>
        /// 更新风模拟系统
        /// </summary>
        void UpdateWindSimulation(float deltaTime)
        {
            if (mainCamera == null) return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            // 基于大气压力差计算风向和风力
            float currentPressure = currentState.atmosphericPressure;
            
            // 模拟压力梯度力产生的风
            Vector3 pressureGradient = CalculatePressureGradient(cameraPos);
            
            // 科里奥利力影响（地球自转效应）
            Vector3 coriolisEffect = CalculateCoriolisEffect(currentState.windDirection, cameraPos);
            
            // 地形影响
            Vector3 terrainEffect = CalculateTerrainWindEffect(cameraPos);
            
            // 计算最终风向和风力
            Vector3 newWindDirection = (pressureGradient + coriolisEffect + terrainEffect).normalized;
            float newWindStrength = Mathf.Clamp01(pressureGradient.magnitude + terrainEffect.magnitude * 0.5f);
            
            // 平滑过渡
            currentState.windDirection = Vector3.Slerp(currentState.windDirection, newWindDirection, deltaTime * 0.3f);
            currentState.windStrength = Mathf.Lerp(currentState.windStrength, newWindStrength, deltaTime * 0.2f);
            
            // 设置全局风参数供着色器使用
            Shader.SetGlobalVector("_WindDirection", currentState.windDirection);
            Shader.SetGlobalFloat("_WindStrength", currentState.windStrength);
        }
        
        /// <summary>
        /// 计算压力梯度
        /// </summary>
        Vector3 CalculatePressureGradient(Vector3 position)
        {
            // 简化的压力梯度计算
            float pressureDifference = 1013.25f - (currentState.atmosphericPressure * 1013.25f);
            
            // 假设压力梯度指向低压区
            Vector3 gradient = Vector3.zero;
            if (Mathf.Abs(pressureDifference) > 5f) // 5毫巴的压力差
            {
                gradient = new Vector3(
                    Mathf.Sin(Time.time * 0.1f) * pressureDifference * 0.001f,
                    0f,
                    Mathf.Cos(Time.time * 0.1f) * pressureDifference * 0.001f
                );
            }
            
            return gradient;
        }
        
        /// <summary>
        /// 计算科里奥利效应
        /// </summary>
        Vector3 CalculateCoriolisEffect(Vector3 currentWind, Vector3 position)
        {
            // 基于纬度的科里奥利参数
            float latitude = position.z / 5000f * 90f; // 假设纬度范围
            float coriolisParameter = 2f * 7.2921e-5f * Mathf.Sin(latitude * Mathf.Deg2Rad);
            
            // 科里奥利力垂直于风向
            Vector3 coriolisForce = new Vector3(-currentWind.z, 0f, currentWind.x) * coriolisParameter;
            
            return coriolisForce;
        }
        
        /// <summary>
        /// 计算地形对风的影响
        /// </summary>
        Vector3 CalculateTerrainWindEffect(Vector3 position)
        {
            // 简化的地形风效应
            // 在实际应用中，这里会分析地形高度图来计算风向偏转
            
            float terrainRoughness = 0.1f; // 地表粗糙度
            float terrainHeight = position.y;
            
            // 山谷风效应（简化）
            Vector3 valleyWind = Vector3.zero;
            if (terrainHeight < 100f) // 低地区域
            {
                valleyWind = new Vector3(0.1f, 0f, 0f); // 假设沿河谷方向
            }
            
            return valleyWind * terrainRoughness;
        }
        
        /// <summary>
        /// 更新子系统
        /// </summary>
        void UpdateSubSystems(float deltaTime)
        {
            // 根据性能设置决定更新频率
            float qualityMultiplier = GetQualityMultiplier();
            
            // 更新日夜循环
            dayNightController.UpdateCycle(deltaTime * qualityMultiplier);
            
            // 更新天气系统
            if (enablePhysicsBasedWeather)
            {
                // 启用基于物理的天气模拟
                weatherController.UpdateWeather(deltaTime * qualityMultiplier, atmosphericData);
            }
            else
            {
                // 简化的天气更新
                weatherController.UpdateWeather(deltaTime * qualityMultiplier * 0.5f, atmosphericData);
            }
            
            // 更新大气渲染
            if (ShouldUpdateAtmosphere())
            {
                atmosphereRenderer.UpdateAtmosphere(deltaTime, currentState);
            }
            
            // 更新云系统
            if (ShouldUpdateClouds())
            {
                cloudSystem.UpdateClouds(deltaTime, currentState, atmosphericData);
            }
            
            // 更新光照
            lightingController.UpdateLighting(deltaTime, currentState);
            
            // 更新音频
            audioController.UpdateAudio(deltaTime, currentState);
            
            // 更新季节
            seasonController.UpdateSeason(deltaTime);
        }
        
        /// <summary>
        /// 插值到目标状态
        /// </summary>
        void InterpolateToTargetState(float deltaTime)
        {
            float lerpSpeed = environmentTransitionSpeed * deltaTime;
            
            // 温度插值
            currentState.temperature = Mathf.Lerp(currentState.temperature, targetState.temperature, lerpSpeed);
            
            // 湿度插值
            currentState.humidity = Mathf.Lerp(currentState.humidity, targetState.humidity, lerpSpeed);
            
            // 风力插值
            currentState.windStrength = Mathf.Lerp(currentState.windStrength, targetState.windStrength, lerpSpeed);
            currentState.windDirection = Vector3.Slerp(currentState.windDirection, targetState.windDirection, lerpSpeed);
            
            // 云覆盖插值
            currentState.cloudCoverage = Mathf.Lerp(currentState.cloudCoverage, targetState.cloudCoverage, lerpSpeed);
            
            // 降水插值
            currentState.precipitationIntensity = Mathf.Lerp(currentState.precipitationIntensity, targetState.precipitationIntensity, lerpSpeed);
            
            // 雾密度插值
            currentState.fogDensity = Mathf.Lerp(currentState.fogDensity, targetState.fogDensity, lerpSpeed);
            
            // 大气压力插值
            currentState.atmosphericPressure = Mathf.Lerp(currentState.atmosphericPressure, targetState.atmosphericPressure, lerpSpeed);
        }
        
        /// <summary>
        /// 更新环境参数 - 主要入口点
        /// </summary>
        public void UpdateEnvironment(WorldGenerationParameters parameters)
        {
            if (!enableDynamicEnvironment) return;
            
            Debug.Log("[Environment] 更新环境参数...");
            
            var envParams = parameters.environmentParams;
            
            // 设置目标天气
            SetTargetWeather(envParams.weather);
            
            // 设置时间
            SetTimeOfDay(envParams.timeOfDay);
            
            // 设置环境参数
            SetEnvironmentParameters(envParams);
            
            Debug.Log("[Environment] 环境参数更新完成");
        }
        
        /// <summary>
        /// 设置目标天气
        /// </summary>
        public void SetTargetWeather(WeatherType weatherType)
        {
            if (weatherController != null)
            {
                weatherController.SetTargetWeather(weatherType);
                OnWeatherChanged?.Invoke(weatherType);
            }
        }
        
        /// <summary>
        /// 设置时间
        /// </summary>
        public void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            if (dayNightController != null)
            {
                dayNightController.SetTimeOfDay(timeOfDay);
                float normalizedTime = GetNormalizedTimeOfDay(timeOfDay);
                OnTimeOfDayChanged?.Invoke(normalizedTime);
            }
        }
        
        /// <summary>
        /// 设置环境参数
        /// </summary>
        void SetEnvironmentParameters(EnvironmentGenerationParams envParams)
        {
            // 设置目标状态
            targetState.temperature = envParams.temperature / 100f; // 标准化
            targetState.humidity = envParams.humidity;
            targetState.windStrength = envParams.windStrength;
            
            // 设置光照参数
            if (lightingController != null)
            {
                lightingController.SetSunIntensity(envParams.sunIntensity);
                lightingController.SetSunColor(envParams.sunColor);
            }
            
            // 设置大气效果
            if (atmosphereRenderer != null)
            {
                atmosphereRenderer.SetFogEnabled(envParams.enableFog);
                atmosphereRenderer.SetVolumetricLightingEnabled(envParams.enableVolumetricLighting);
            }
            
            // 设置云系统
            if (cloudSystem != null)
            {
                cloudSystem.SetCloudsEnabled(envParams.enableClouds);
            }
            
            // 设置音频
            if (audioController != null && envParams.generateAmbientSounds)
            {
                audioController.SetAmbientProfile(envParams.soundProfile);
            }
        }
        
        /// <summary>
        /// 获取标准化时间
        /// </summary>
        float GetNormalizedTimeOfDay(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Dawn: return 0.25f;
                case TimeOfDay.Morning: return 0.3f;
                case TimeOfDay.Noon: return 0.5f;
                case TimeOfDay.Afternoon: return 0.7f;
                case TimeOfDay.Dusk: return 0.75f;
                case TimeOfDay.Night: return 0f;
                default: return 0.5f;
            }
        }
        
        /// <summary>
        /// 检查是否应该更新大气
        /// </summary>
        bool ShouldUpdateAtmosphere()
        {
            if (mainCamera == null) return true;
            
            return enableLODSystem ? 
                   Vector3.Distance(transform.position, mainCamera.transform.position) <= maxUpdateDistance : 
                   true;
        }
        
        /// <summary>
        /// 检查是否应该更新云系统
        /// </summary>
        bool ShouldUpdateClouds()
        {
            if (quality == EnvironmentQuality.Low) return false;
            return ShouldUpdateAtmosphere();
        }
        
        /// <summary>
        /// 获取质量乘数
        /// </summary>
        float GetQualityMultiplier()
        {
            switch (quality)
            {
                case EnvironmentQuality.Low: return 0.5f;
                case EnvironmentQuality.Medium: return 0.8f;
                case EnvironmentQuality.High: return 1f;
                case EnvironmentQuality.Ultra: return 1.2f;
                default: return 1f;
            }
        }
        
        /// <summary>
        /// 加载默认配置
        /// </summary>
        void LoadDefaultProfile()
        {
            currentProfile = ScriptableObject.CreateInstance<EnvironmentProfile>();
            currentProfile.Initialize();
            Debug.Log("[Environment] 加载默认环境配置");
        }
        
        /// <summary>
        /// 应用全局配置文件设置
        /// </summary>
        void ApplyGlobalProfileSettings()
        {
            if (currentProfile == null) return;
            
            Debug.Log("[Environment] 应用全局环境配置文件设置");
            
            // 应用配置文件中的更新频率设置
            updateFrequency = currentProfile.GetUpdateFrequency();
            
            // 应用质量设置
            quality = currentProfile.GetQualityLevel();
            
            // 应用物理模拟设置
            enablePhysicsBasedWeather = currentProfile.IsPhysicsBasedWeatherEnabled();
            enableTemperatureGradients = currentProfile.IsTemperatureGradientsEnabled();
            enableAtmosphericPressure = currentProfile.IsAtmosphericPressureEnabled();
            
            // 应用环境过渡速度
            environmentTransitionSpeed = currentProfile.GetTransitionSpeed();
            
            // 应用LOD设置
            enableLODSystem = currentProfile.IsLODSystemEnabled();
            maxUpdateDistance = currentProfile.GetMaxUpdateDistance();
            
            Debug.Log($"[Environment] 全局配置应用完成 - 质量等级: {quality}, 更新频率: {updateFrequency}");
        }
        
        /// <summary>
        /// 设置环境质量
        /// </summary>
        public void SetEnvironmentQuality(EnvironmentQuality newQuality)
        {
            quality = newQuality;
            
            // 通知子系统质量变化
            atmosphereRenderer?.SetQuality(quality);
            cloudSystem?.SetQuality(quality);
            lightingController?.SetQuality(quality);
        }
        
        /// <summary>
        /// 获取当前环境状态
        /// </summary>
        public EnvironmentState GetCurrentEnvironmentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// 获取大气数据
        /// </summary>
        public AtmosphericData GetAtmosphericData()
        {
            return atmosphericData;
        }
        
        /// <summary>
        /// 获取天气系统
        /// </summary>
        public WeatherSystem GetWeatherSystem()
        {
            return weatherSystem;
        }
        
        /// <summary>
        /// 强制更新环境
        /// </summary>
        public void ForceEnvironmentUpdate()
        {
            UpdateEnvironment(Time.unscaledDeltaTime);
        }
        
        /// <summary>
        /// 停止环境更新
        /// </summary>
        public void StopEnvironmentUpdates()
        {
            if (environmentUpdateCoroutine != null)
            {
                StopCoroutine(environmentUpdateCoroutine);
                environmentUpdateCoroutine = null;
            }
        }
        
        /// <summary>
        /// 重新开始环境更新
        /// </summary>
        public void RestartEnvironmentUpdates()
        {
            StopEnvironmentUpdates();
            StartEnvironmentUpdates();
        }
        
        /// <summary>
        /// 获取环境统计信息
        /// </summary>
        public string GetEnvironmentStats()
        {
            return $"环境系统统计:\n" +
                   $"当前天气: {weatherController?.GetCurrentWeather()}\n" +
                   $"时间: {dayNightController?.GetCurrentTimeOfDay()}\n" +
                   $"温度: {currentState.temperature * 100f:F1}°C\n" +
                   $"湿度: {currentState.humidity * 100f:F1}%\n" +
                   $"风力: {currentState.windStrength:F2}\n" +
                   $"云覆盖: {currentState.cloudCoverage * 100f:F1}%\n" +
                   $"大气压力: {currentState.atmosphericPressure:F2} atm\n" +
                   $"质量等级: {quality}\n" +
                   $"实时更新: {enableRealTimeUpdates}";
        }
        
        void OnDestroy()
        {
            StopEnvironmentUpdates();
        }
    }
    
    /// <summary>
    /// 环境质量等级
    /// </summary>
    public enum EnvironmentQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }
}