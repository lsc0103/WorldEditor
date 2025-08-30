using UnityEngine;
using System;
using System.Collections;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 天气系统 - 管理动态天气变化和天气效果
    /// 
    /// 核心功能：
    /// - 动态天气类型切换和过渡
    /// - 天气强度和持续时间控制
    /// - 天气粒子效果管理（雨、雪、雾等）
    /// - 天气对环境参数的影响计算
    /// - 天气音效和物理效果集成
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        #region 天气配置参数

        [Header("天气系统配置")]
        [Tooltip("是否启用自动天气变化")]
        public bool enableAutoWeatherChange = true;
        
        [Tooltip("天气变化间隔 (分钟)")]
        [Range(5f, 60f)]
        public float weatherChangeInterval = 15f;
        
        [Tooltip("天气过渡时间 (分钟)")]
        [Range(1f, 10f)]
        public float weatherTransitionTime = 3f;
        
        [Tooltip("当前天气类型")]
        public WeatherType currentWeatherType = WeatherType.Clear;
        
        [Tooltip("当前天气强度 (0-1)")]
        [Range(0f, 1f)]
        public float currentWeatherIntensity = 1f;

        #endregion

        #region 各种天气配置

        [Header("雨天配置")]
        [Tooltip("雨粒子系统")]
        public ParticleSystem rainParticleSystem;
        
        [Tooltip("雨声音效")]
        public AudioClip rainAudioClip;
        
        [Tooltip("雨天温度降低幅度")]
        [Range(0f, 10f)]
        public float rainTemperatureReduction = 5f;

        [Header("雪天配置")]
        [Tooltip("雪花粒子系统")]
        public ParticleSystem snowParticleSystem;
        
        [Tooltip("雪花音效")]
        public AudioClip snowAudioClip;
        
        [Tooltip("雪天温度降低幅度")]
        [Range(0f, 20f)]
        public float snowTemperatureReduction = 15f;

        [Header("雾天配置")]
        [Tooltip("雾效果强度")]
        [Range(0f, 1f)]
        public float fogIntensity = 0.5f;
        
        [Tooltip("雾颜色")]
        public Color fogColor = Color.gray;
        
        [Tooltip("雾渲染距离")]
        [Range(10f, 500f)]
        public float fogDistance = 100f;

        [Header("风暴配置")]
        [Tooltip("风暴粒子系统")]
        public ParticleSystem stormParticleSystem;
        
        [Tooltip("闪电效果")]
        public Light lightningLight;
        
        [Tooltip("雷声音效")]
        public AudioClip thunderAudioClip;
        
        [Tooltip("闪电频率 (秒)")]
        [Range(5f, 30f)]
        public float lightningInterval = 10f;

        #endregion

        #region 天气影响参数

        [Header("天气环境影响")]
        [Tooltip("天气对光照的影响强度")]
        [Range(0f, 1f)]
        public float lightingInfluence = 0.7f;
        
        [Tooltip("天气对风力的影响强度")]
        [Range(0f, 2f)]
        public float windInfluence = 1.5f;
        
        [Tooltip("天气对湿度的影响强度")]
        [Range(0f, 1f)]
        public float humidityInfluence = 0.8f;

        #endregion

        #region 运行时状态

        private bool isInitialized = false;
        private bool isActive = false;
        private WeatherType targetWeatherType;
        private float weatherTransitionProgress = 0f;
        private float lastWeatherChangeTime = 0f;
        private float nextLightningTime = 0f;
        private EnvironmentState linkedEnvironmentState;
        private AudioSource weatherAudioSource;
        private Coroutine weatherTransitionCoroutine;
        private Coroutine lightningCoroutine;

        #endregion

        #region 天气数据结构

        [System.Serializable]
        public class WeatherData
        {
            public WeatherType weatherType;
            public float temperatureModifier;
            public float humidityModifier;
            public float windStrengthModifier;
            public float lightIntensityModifier;
            public Color lightColorModifier = Color.white;
            public float cloudCoverageModifier;
            public float fogDensityModifier;
        }

        [Header("天气数据配置")]
        public WeatherData[] weatherDataArray = new WeatherData[]
        {
            new WeatherData { weatherType = WeatherType.Clear, temperatureModifier = 0f, humidityModifier = 0f, 
                windStrengthModifier = 1f, lightIntensityModifier = 1f, cloudCoverageModifier = 0.2f, fogDensityModifier = 0f },
            new WeatherData { weatherType = WeatherType.Cloudy, temperatureModifier = -2f, humidityModifier = 0.2f, 
                windStrengthModifier = 1.2f, lightIntensityModifier = 0.8f, cloudCoverageModifier = 0.6f, fogDensityModifier = 0f },
            new WeatherData { weatherType = WeatherType.Overcast, temperatureModifier = -3f, humidityModifier = 0.3f, 
                windStrengthModifier = 1.1f, lightIntensityModifier = 0.6f, cloudCoverageModifier = 0.9f, fogDensityModifier = 0.1f },
            new WeatherData { weatherType = WeatherType.Rainy, temperatureModifier = -5f, humidityModifier = 0.8f, 
                windStrengthModifier = 1.5f, lightIntensityModifier = 0.4f, cloudCoverageModifier = 0.8f, fogDensityModifier = 0.2f },
            new WeatherData { weatherType = WeatherType.Storm, temperatureModifier = -8f, humidityModifier = 0.9f, 
                windStrengthModifier = 2.5f, lightIntensityModifier = 0.3f, cloudCoverageModifier = 1f, fogDensityModifier = 0.3f },
            new WeatherData { weatherType = WeatherType.Snowy, temperatureModifier = -15f, humidityModifier = 0.6f, 
                windStrengthModifier = 1.3f, lightIntensityModifier = 0.7f, cloudCoverageModifier = 0.7f, fogDensityModifier = 0.1f },
            new WeatherData { weatherType = WeatherType.Foggy, temperatureModifier = -1f, humidityModifier = 1f, 
                windStrengthModifier = 0.5f, lightIntensityModifier = 0.5f, cloudCoverageModifier = 0.4f, fogDensityModifier = 0.8f },
            new WeatherData { weatherType = WeatherType.Windy, temperatureModifier = 1f, humidityModifier = -0.2f, 
                windStrengthModifier = 3f, lightIntensityModifier = 0.9f, cloudCoverageModifier = 0.3f, fogDensityModifier = 0f }
        };

        #endregion

        #region 事件系统

        /// <summary>天气变化事件</summary>
        public event Action<WeatherType, WeatherType> OnWeatherChanged;
        
        /// <summary>天气强度变化事件</summary>
        public event Action<float> OnWeatherIntensityChanged;
        
        /// <summary>闪电事件</summary>
        public event Action OnLightning;

        #endregion

        #region 公共属性

        /// <summary>天气系统是否激活</summary>
        public bool IsActive => isActive && isInitialized;
        
        /// <summary>当前天气类型</summary>
        public WeatherType CurrentWeatherType => currentWeatherType;
        
        /// <summary>当前天气强度</summary>
        public float CurrentWeatherIntensity => currentWeatherIntensity;
        
        /// <summary>是否正在进行天气过渡</summary>
        public bool IsTransitioning => weatherTransitionProgress > 0f && weatherTransitionProgress < 1f;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化天气系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[WeatherSystem] 天气系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[WeatherSystem] 开始初始化天气系统...");

            // 链接环境状态
            linkedEnvironmentState = environmentState;

            // 初始化音频源
            InitializeAudioSource();

            // 初始化粒子系统
            InitializeParticleSystems();

            // 设置初始天气
            targetWeatherType = currentWeatherType;
            weatherTransitionProgress = 1f;

            // 同步环境状态
            if (linkedEnvironmentState != null)
            {
                SyncFromEnvironmentState();
            }

            // 启动自动天气变化
            if (enableAutoWeatherChange)
            {
                lastWeatherChangeTime = Time.time;
            }

            isActive = true;
            isInitialized = true;

            Debug.Log($"[WeatherSystem] 天气系统初始化完成 - 初始天气: {currentWeatherType}");
        }

        /// <summary>
        /// 初始化音频源
        /// </summary>
        private void InitializeAudioSource()
        {
            weatherAudioSource = GetComponent<AudioSource>();
            if (weatherAudioSource == null)
            {
                weatherAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            weatherAudioSource.loop = true;
            weatherAudioSource.playOnAwake = false;
            weatherAudioSource.volume = 0.5f;
        }

        /// <summary>
        /// 初始化粒子系统
        /// </summary>
        private void InitializeParticleSystems()
        {
            // 确保所有粒子系统开始时是停止状态
            if (rainParticleSystem != null && rainParticleSystem.isPlaying)
                rainParticleSystem.Stop();
                
            if (snowParticleSystem != null && snowParticleSystem.isPlaying)
                snowParticleSystem.Stop();
                
            if (stormParticleSystem != null && stormParticleSystem.isPlaying)
                stormParticleSystem.Stop();

            // 初始化闪电光源
            if (lightningLight != null)
            {
                lightningLight.enabled = false;
                lightningLight.type = LightType.Directional;
                lightningLight.intensity = 2f;
                lightningLight.color = Color.white;
            }
        }

        #endregion

        #region 天气控制方法

        /// <summary>
        /// 设置天气类型
        /// </summary>
        public void SetWeather(WeatherType weatherType, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            
            if (currentWeatherType == weatherType && Mathf.Abs(currentWeatherIntensity - intensity) < 0.01f)
                return;

            WeatherType previousWeather = currentWeatherType;
            
            // 开始天气过渡
            targetWeatherType = weatherType;
            currentWeatherIntensity = intensity;
            
            if (weatherTransitionCoroutine != null)
            {
                StopCoroutine(weatherTransitionCoroutine);
            }
            
            weatherTransitionCoroutine = StartCoroutine(TransitionWeather(previousWeather, weatherType, intensity));
            
            Debug.Log($"[WeatherSystem] 开始天气过渡: {previousWeather} → {weatherType} (强度: {intensity:F2})");
        }

        /// <summary>
        /// 天气过渡协程
        /// </summary>
        private IEnumerator TransitionWeather(WeatherType fromWeather, WeatherType toWeather, float targetIntensity)
        {
            float transitionDuration = weatherTransitionTime * 60f; // 转换为秒
            float elapsedTime = 0f;
            weatherTransitionProgress = 0f;

            WeatherData fromData = GetWeatherData(fromWeather);
            WeatherData toData = GetWeatherData(toWeather);

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                weatherTransitionProgress = elapsedTime / transitionDuration;
                
                // 应用渐变天气效果
                ApplyWeatherTransition(fromData, toData, weatherTransitionProgress, targetIntensity);
                
                yield return null;
            }

            // 完成过渡
            weatherTransitionProgress = 1f;
            currentWeatherType = toWeather;
            currentWeatherIntensity = targetIntensity;
            
            // 应用最终天气效果
            ApplyWeatherEffects(toData, targetIntensity);
            
            // 触发天气变化事件
            OnWeatherChanged?.Invoke(toWeather, fromWeather);
            OnWeatherIntensityChanged?.Invoke(targetIntensity);
            
            // 同步到环境状态
            SyncToEnvironmentState();
            
            Debug.Log($"[WeatherSystem] 天气过渡完成: {toWeather} (强度: {targetIntensity:F2})");
        }

        /// <summary>
        /// 应用天气过渡效果
        /// </summary>
        private void ApplyWeatherTransition(WeatherData fromData, WeatherData toData, float progress, float intensity)
        {
            // 插值计算天气参数
            float tempModifier = Mathf.Lerp(fromData.temperatureModifier, toData.temperatureModifier, progress);
            float humidityModifier = Mathf.Lerp(fromData.humidityModifier, toData.humidityModifier, progress);
            float windModifier = Mathf.Lerp(fromData.windStrengthModifier, toData.windStrengthModifier, progress);
            float lightModifier = Mathf.Lerp(fromData.lightIntensityModifier, toData.lightIntensityModifier, progress);
            float cloudModifier = Mathf.Lerp(fromData.cloudCoverageModifier, toData.cloudCoverageModifier, progress);
            float fogModifier = Mathf.Lerp(fromData.fogDensityModifier, toData.fogDensityModifier, progress);

            // 应用到环境状态
            if (linkedEnvironmentState != null)
            {
                linkedEnvironmentState.weatherTransition = progress;
                linkedEnvironmentState.weatherIntensity = intensity;
                
                // 应用天气影响
                ApplyWeatherInfluence(tempModifier, humidityModifier, windModifier, lightModifier, cloudModifier, fogModifier, intensity);
            }

            // 更新粒子系统和音效
            UpdateWeatherEffects(progress, intensity);
        }

        /// <summary>
        /// 应用天气效果
        /// </summary>
        private void ApplyWeatherEffects(WeatherData weatherData, float intensity)
        {
            if (linkedEnvironmentState == null) return;

            // 应用天气影响
            ApplyWeatherInfluence(
                weatherData.temperatureModifier,
                weatherData.humidityModifier,
                weatherData.windStrengthModifier,
                weatherData.lightIntensityModifier,
                weatherData.cloudCoverageModifier,
                weatherData.fogDensityModifier,
                intensity
            );

            // 管理粒子系统
            ManageParticleSystems(weatherData.weatherType, intensity);
            
            // 管理音效
            ManageWeatherAudio(weatherData.weatherType, intensity);
            
            // 管理特殊效果
            ManageSpecialEffects(weatherData.weatherType, intensity);
        }

        /// <summary>
        /// 应用天气对环境的影响
        /// </summary>
        private void ApplyWeatherInfluence(float tempMod, float humidityMod, float windMod, float lightMod, float cloudMod, float fogMod, float intensity)
        {
            if (linkedEnvironmentState == null) return;

            // 应用温度影响
            linkedEnvironmentState.temperature += tempMod * intensity;
            
            // 应用湿度影响
            linkedEnvironmentState.humidity = Mathf.Clamp01(linkedEnvironmentState.humidity + humidityMod * humidityInfluence * intensity);
            
            // 应用风力影响
            linkedEnvironmentState.windStrength = Mathf.Clamp01(linkedEnvironmentState.windStrength * windMod * windInfluence);
            
            // 应用光照影响
            linkedEnvironmentState.sunIntensity *= lightMod * Mathf.Lerp(1f, lightingInfluence, intensity);
            
            // 应用云层影响
            linkedEnvironmentState.cloudCoverage = Mathf.Clamp01(cloudMod);
            
            // 应用雾效影响
            linkedEnvironmentState.fogDensity = Mathf.Clamp01(fogMod * intensity);
            linkedEnvironmentState.fogColor = fogColor;
        }

        /// <summary>
        /// 更新天气效果
        /// </summary>
        private void UpdateWeatherEffects(float transitionProgress, float intensity)
        {
            // 根据过渡进度更新效果强度
            float effectIntensity = intensity * transitionProgress;
            
            // 更新粒子系统
            if (targetWeatherType == WeatherType.Rainy && rainParticleSystem != null)
            {
                UpdateParticleSystemIntensity(rainParticleSystem, effectIntensity);
            }
            else if (targetWeatherType == WeatherType.Snowy && snowParticleSystem != null)
            {
                UpdateParticleSystemIntensity(snowParticleSystem, effectIntensity);
            }
            else if (targetWeatherType == WeatherType.Storm && stormParticleSystem != null)
            {
                UpdateParticleSystemIntensity(stormParticleSystem, effectIntensity);
            }
        }

        /// <summary>
        /// 获取天气数据
        /// </summary>
        private WeatherData GetWeatherData(WeatherType weatherType)
        {
            foreach (var data in weatherDataArray)
            {
                if (data.weatherType == weatherType)
                    return data;
            }
            return weatherDataArray[0]; // 默认返回晴天数据
        }

        #endregion

        #region 粒子系统管理

        /// <summary>
        /// 管理粒子系统
        /// </summary>
        private void ManageParticleSystems(WeatherType weatherType, float intensity)
        {
            // 停止所有粒子系统
            StopAllParticleSystems();

            // 根据天气类型启动对应粒子系统
            switch (weatherType)
            {
                case WeatherType.Rainy:
                    if (rainParticleSystem != null)
                    {
                        rainParticleSystem.Play();
                        UpdateParticleSystemIntensity(rainParticleSystem, intensity);
                    }
                    break;
                    
                case WeatherType.Snowy:
                    if (snowParticleSystem != null)
                    {
                        snowParticleSystem.Play();
                        UpdateParticleSystemIntensity(snowParticleSystem, intensity);
                    }
                    break;
                    
                case WeatherType.Storm:
                    if (stormParticleSystem != null)
                    {
                        stormParticleSystem.Play();
                        UpdateParticleSystemIntensity(stormParticleSystem, intensity);
                    }
                    // 启动闪电效果
                    if (lightningCoroutine == null)
                    {
                        lightningCoroutine = StartCoroutine(LightningEffect());
                    }
                    break;
            }
        }

        /// <summary>
        /// 停止所有粒子系统
        /// </summary>
        private void StopAllParticleSystems()
        {
            if (rainParticleSystem != null && rainParticleSystem.isPlaying)
                rainParticleSystem.Stop();
                
            if (snowParticleSystem != null && snowParticleSystem.isPlaying)
                snowParticleSystem.Stop();
                
            if (stormParticleSystem != null && stormParticleSystem.isPlaying)
                stormParticleSystem.Stop();
                
            // 停止闪电效果
            if (lightningCoroutine != null)
            {
                StopCoroutine(lightningCoroutine);
                lightningCoroutine = null;
            }
        }

        /// <summary>
        /// 更新粒子系统强度
        /// </summary>
        private void UpdateParticleSystemIntensity(ParticleSystem particleSystem, float intensity)
        {
            var emission = particleSystem.emission;
            var rateOverTime = emission.rateOverTime;
            rateOverTime.constantMax = rateOverTime.constantMax * intensity;
            emission.rateOverTime = rateOverTime;
        }

        #endregion

        #region 音效管理

        /// <summary>
        /// 管理天气音效
        /// </summary>
        private void ManageWeatherAudio(WeatherType weatherType, float intensity)
        {
            if (weatherAudioSource == null) return;

            AudioClip targetClip = null;
            
            switch (weatherType)
            {
                case WeatherType.Rainy:
                case WeatherType.Storm:
                    targetClip = rainAudioClip;
                    break;
                case WeatherType.Snowy:
                    targetClip = snowAudioClip;
                    break;
            }

            if (targetClip != null)
            {
                if (weatherAudioSource.clip != targetClip)
                {
                    weatherAudioSource.clip = targetClip;
                    weatherAudioSource.Play();
                }
                weatherAudioSource.volume = 0.5f * intensity;
            }
            else
            {
                weatherAudioSource.Stop();
            }
        }

        #endregion

        #region 特殊效果

        /// <summary>
        /// 管理特殊效果
        /// </summary>
        private void ManageSpecialEffects(WeatherType weatherType, float intensity)
        {
            // 雾效处理
            if (weatherType == WeatherType.Foggy)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = fogIntensity * intensity;
            }
            else
            {
                RenderSettings.fog = false;
            }
        }

        /// <summary>
        /// 闪电效果协程
        /// </summary>
        private IEnumerator LightningEffect()
        {
            while (currentWeatherType == WeatherType.Storm)
            {
                yield return new WaitForSeconds(lightningInterval);
                
                if (lightningLight != null)
                {
                    // 闪电闪烁效果
                    lightningLight.enabled = true;
                    yield return new WaitForSeconds(0.1f);
                    lightningLight.enabled = false;
                    yield return new WaitForSeconds(0.1f);
                    lightningLight.enabled = true;
                    yield return new WaitForSeconds(0.05f);
                    lightningLight.enabled = false;
                    
                    // 播放雷声
                    if (thunderAudioClip != null && weatherAudioSource != null)
                    {
                        weatherAudioSource.PlayOneShot(thunderAudioClip);
                    }
                    
                    // 触发闪电事件
                    OnLightning?.Invoke();
                }
            }
        }

        #endregion

        #region 系统更新

        /// <summary>
        /// 更新天气系统 (由EnvironmentManager调用)
        /// </summary>
        public void UpdateSystem()
        {
            if (!isActive) return;

            // 自动天气变化
            if (enableAutoWeatherChange && Time.time - lastWeatherChangeTime > weatherChangeInterval * 60f)
            {
                GenerateRandomWeather();
                lastWeatherChangeTime = Time.time;
            }

            // 从环境状态同步
            if (linkedEnvironmentState != null)
            {
                if (linkedEnvironmentState.currentWeather != currentWeatherType)
                {
                    SetWeather(linkedEnvironmentState.currentWeather, linkedEnvironmentState.weatherIntensity);
                }
            }
        }

        /// <summary>
        /// 生成随机天气
        /// </summary>
        private void GenerateRandomWeather()
        {
            WeatherType[] availableWeather = (WeatherType[])Enum.GetValues(typeof(WeatherType));
            WeatherType newWeather = availableWeather[UnityEngine.Random.Range(0, availableWeather.Length)];
            
            // 避免连续相同天气
            while (newWeather == currentWeatherType)
            {
                newWeather = availableWeather[UnityEngine.Random.Range(0, availableWeather.Length)];
            }
            
            float intensity = UnityEngine.Random.Range(0.3f, 1f);
            SetWeather(newWeather, intensity);
        }

        #endregion

        #region 环境状态同步

        /// <summary>
        /// 从环境状态同步
        /// </summary>
        private void SyncFromEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;
            
            SetWeather(linkedEnvironmentState.currentWeather, linkedEnvironmentState.weatherIntensity);
        }

        /// <summary>
        /// 同步到环境状态
        /// </summary>
        private void SyncToEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;

            linkedEnvironmentState.currentWeather = currentWeatherType;
            linkedEnvironmentState.targetWeather = targetWeatherType;
            linkedEnvironmentState.weatherIntensity = currentWeatherIntensity;
            linkedEnvironmentState.weatherTransition = weatherTransitionProgress;
        }

        #endregion

        #region 调试信息

        void OnGUI()
        {
            // 调试面板已禁用
            /*
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            GUILayout.BeginArea(new Rect(950, 10, 200, 150));
            GUILayout.Box("天气系统调试");
            
            GUILayout.Label($"当前天气: {currentWeatherType}");
            GUILayout.Label($"目标天气: {targetWeatherType}");
            GUILayout.Label($"天气强度: {currentWeatherIntensity:F2}");
            GUILayout.Label($"过渡进度: {weatherTransitionProgress:F2}");
            GUILayout.Label($"自动变化: {(enableAutoWeatherChange ? "开启" : "关闭")}");
            
            if (enableAutoWeatherChange)
            {
                float timeUntilNext = weatherChangeInterval * 60f - (Time.time - lastWeatherChangeTime);
                GUILayout.Label($"下次变化: {timeUntilNext:F0}秒");
            }
            
            GUILayout.EndArea();
            */
        }

        #endregion
    }
}