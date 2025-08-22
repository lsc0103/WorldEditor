using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 天气控制器 - 管理所有天气效果和转换
    /// </summary>
    public class WeatherController : MonoBehaviour
    {
        [Header("天气效果组件")]
        [SerializeField] private ParticleSystem rainParticleSystem;
        [SerializeField] private ParticleSystem snowParticleSystem;
        [SerializeField] private ParticleSystem fogParticleSystem;
        [SerializeField] private VisualEffect lightningVFX;
        [SerializeField] private WindZone windZone;
        
        [Header("天气音效")]
        [SerializeField] private AudioSource weatherAudioSource;
        [SerializeField] private AudioClip rainSound;
        [SerializeField] private AudioClip thunderSound;
        [SerializeField] private AudioClip windSound;
        [SerializeField] private AudioClip snowSound;
        
        [Header("天气设置")]
        [SerializeField] private float weatherTransitionSpeed = 1f;
        [SerializeField] private bool enableWeatherEffects = true;
        [SerializeField] private bool enableWeatherSounds = true;
        [SerializeField] private float maxParticleDistance = 200f;
        
        [Header("降水设置")]
        [SerializeField] private float rainIntensityMultiplier = 1f;
        [SerializeField] private float snowIntensityMultiplier = 1f;
        [SerializeField] private Vector3 precipitationArea = new Vector3(100f, 50f, 100f);
        [SerializeField] private int maxRainParticles = 5000;
        [SerializeField] private int maxSnowParticles = 3000;
        
        [Header("闪电设置")]
        [SerializeField] private float lightningFrequency = 10f;
        [SerializeField] private float lightningDuration = 0.2f;
        [SerializeField] private Color lightningColor = Color.white;
        [SerializeField] private float lightningIntensity = 5f;
        
        [Header("风效设置")]
        [SerializeField] private float windStrengthMultiplier = 1f;
        [SerializeField] private bool enableWindParticles = true;
        [SerializeField] private ParticleSystem windParticles;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private WeatherType currentWeather = WeatherType.Clear;
        private WeatherType targetWeather = WeatherType.Clear;
        private float weatherTransitionProgress = 0f;
        
        // 天气状态
        private WeatherState clearState;
        private WeatherState cloudyState;
        private WeatherState rainyState;
        private WeatherState stormyState;
        private WeatherState foggyState;
        private WeatherState snowyState;
        
        // 闪电系统
        private Coroutine lightningCoroutine;
        private Light lightningLight;
        private Camera mainCamera;
        
        // 性能优化
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            InitializeWeatherStates();
            SetupComponents();
            SetupLightning();
        }
        
        void InitializeWeatherStates()
        {
            // 初始化各种天气状态
            clearState = new WeatherState
            {
                weatherType = WeatherType.Clear,
                precipitationIntensity = 0f,
                cloudCoverage = 0.1f,
                windStrength = 0.2f,
                fogDensity = 0f,
                lightningChance = 0f,
                ambientVolume = 0.3f
            };
            
            cloudyState = new WeatherState
            {
                weatherType = WeatherType.Cloudy,
                precipitationIntensity = 0f,
                cloudCoverage = 0.7f,
                windStrength = 0.4f,
                fogDensity = 0.1f,
                lightningChance = 0f,
                ambientVolume = 0.5f
            };
            
            rainyState = new WeatherState
            {
                weatherType = WeatherType.Rainy,
                precipitationIntensity = 0.6f,
                cloudCoverage = 0.9f,
                windStrength = 0.5f,
                fogDensity = 0.3f,
                lightningChance = 0.1f,
                ambientVolume = 0.8f
            };
            
            stormyState = new WeatherState
            {
                weatherType = WeatherType.Stormy,
                precipitationIntensity = 0.9f,
                cloudCoverage = 1f,
                windStrength = 0.8f,
                fogDensity = 0.2f,
                lightningChance = 0.4f,
                ambientVolume = 1f
            };
            
            foggyState = new WeatherState
            {
                weatherType = WeatherType.Foggy,
                precipitationIntensity = 0.1f,
                cloudCoverage = 0.8f,
                windStrength = 0.1f,
                fogDensity = 0.8f,
                lightningChance = 0f,
                ambientVolume = 0.4f
            };
            
            snowyState = new WeatherState
            {
                weatherType = WeatherType.Snowy,
                precipitationIntensity = 0.5f,
                cloudCoverage = 0.9f,
                windStrength = 0.3f,
                fogDensity = 0.2f,
                lightningChance = 0f,
                ambientVolume = 0.6f
            };
        }
        
        void SetupComponents()
        {
            // 设置主摄像机
            mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            
            // 设置音频源
            if (weatherAudioSource == null)
            {
                weatherAudioSource = gameObject.AddComponent<AudioSource>();
                weatherAudioSource.loop = true;
                weatherAudioSource.playOnAwake = false;
                weatherAudioSource.spatialBlend = 0f; // 2D音频
            }
            
            // 设置风区
            if (windZone == null)
            {
                GameObject windObj = new GameObject("Weather Wind Zone");
                windObj.transform.SetParent(transform);
                windZone = windObj.AddComponent<WindZone>();
                windZone.mode = WindZoneMode.Directional;
            }
            
            // 设置粒子系统
            SetupParticleSystems();
        }
        
        void SetupParticleSystems()
        {
            // 设置雨粒子系统
            if (rainParticleSystem == null)
            {
                rainParticleSystem = CreateWeatherParticleSystem("Rain Particles", maxRainParticles);
                ConfigureRainParticles();
            }
            
            // 设置雪粒子系统
            if (snowParticleSystem == null)
            {
                snowParticleSystem = CreateWeatherParticleSystem("Snow Particles", maxSnowParticles);
                ConfigureSnowParticles();
            }
            
            // 设置雾粒子系统
            if (fogParticleSystem == null)
            {
                fogParticleSystem = CreateWeatherParticleSystem("Fog Particles", 1000);
                ConfigureFogParticles();
            }
            
            // 设置风粒子系统
            if (windParticles == null && enableWindParticles)
            {
                windParticles = CreateWeatherParticleSystem("Wind Particles", 500);
                ConfigureWindParticles();
            }
        }
        
        ParticleSystem CreateWeatherParticleSystem(string name, int maxParticles)
        {
            GameObject particleObj = new GameObject(name);
            particleObj.transform.SetParent(transform);
            
            ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
            
            var main = particles.main;
            main.maxParticles = maxParticles;
            main.startLifetime = 2f;
            main.startSpeed = 10f;
            main.startSize = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = particles.emission;
            emission.enabled = false;
            
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = precipitationArea;
            
            return particles;
        }
        
        void ConfigureRainParticles()
        {
            var main = rainParticleSystem.main;
            main.startColor = new Color(0.7f, 0.8f, 1f, 0.8f);
            main.startLifetime = 1.5f;
            main.startSpeed = 15f;
            main.startSize = 0.05f;
            main.gravityModifier = 2f;
            
            var emission = rainParticleSystem.emission;
            emission.rateOverTime = 1000f;
            
            var velocityOverLifetime = rainParticleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-20f);
        }
        
        void ConfigureSnowParticles()
        {
            var main = snowParticleSystem.main;
            main.startColor = Color.white;
            main.startLifetime = 5f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.gravityModifier = 0.1f;
            
            var emission = snowParticleSystem.emission;
            emission.rateOverTime = 500f;
            
            var velocityOverLifetime = snowParticleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-1f, -3f);
            
            var noise = snowParticleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.1f;
        }
        
        void ConfigureFogParticles()
        {
            var main = fogParticleSystem.main;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            main.startLifetime = 10f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 8f);
            main.gravityModifier = 0f;
            
            var emission = fogParticleSystem.emission;
            emission.rateOverTime = 50f;
            
            var shape = fogParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 50f;
        }
        
        void ConfigureWindParticles()
        {
            var main = windParticles.main;
            main.startColor = new Color(1f, 1f, 1f, 0.1f);
            main.startLifetime = 3f;
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            
            var emission = windParticles.emission;
            emission.rateOverTime = 100f;
        }
        
        void SetupLightning()
        {
            // 创建闪电光源
            GameObject lightningObj = new GameObject("Lightning Light");
            lightningObj.transform.SetParent(transform);
            lightningLight = lightningObj.AddComponent<Light>();
            
            lightningLight.type = LightType.Directional;
            lightningLight.color = lightningColor;
            lightningLight.intensity = 0f;
            lightningLight.enabled = false;
            lightningLight.shadows = LightShadows.Soft;
        }
        
        /// <summary>
        /// 更新天气
        /// </summary>
        public void UpdateWeather(float deltaTime, AtmosphericData atmosphericData)
        {
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL)
                return;
            
            lastUpdateTime = Time.time;
            
            // 更新天气转换
            UpdateWeatherTransition(deltaTime);
            
            // 更新天气效果
            UpdateWeatherEffects(deltaTime, atmosphericData);
            
            // 更新音效
            UpdateWeatherAudio(deltaTime);
            
            // 更新闪电
            UpdateLightning(deltaTime);
        }
        
        void UpdateWeatherTransition(float deltaTime)
        {
            if (currentWeather != targetWeather)
            {
                weatherTransitionProgress += deltaTime * weatherTransitionSpeed;
                
                if (weatherTransitionProgress >= 1f)
                {
                    currentWeather = targetWeather;
                    weatherTransitionProgress = 0f;
                }
            }
        }
        
        void UpdateWeatherEffects(float deltaTime, AtmosphericData atmosphericData)
        {
            if (!enableWeatherEffects) return;
            
            WeatherState currentState = GetCurrentWeatherState();
            WeatherState targetState = GetTargetWeatherState();
            
            // 插值计算当前天气参数
            float lerpFactor = weatherTransitionProgress;
            
            float precipitationIntensity = Mathf.Lerp(currentState.precipitationIntensity, targetState.precipitationIntensity, lerpFactor);
            float windStrength = Mathf.Lerp(currentState.windStrength, targetState.windStrength, lerpFactor);
            float fogDensity = Mathf.Lerp(currentState.fogDensity, targetState.fogDensity, lerpFactor);
            
            // 更新降水效果
            UpdatePrecipitation(precipitationIntensity);
            
            // 更新风效果
            UpdateWind(windStrength, atmosphericData);
            
            // 更新雾效果
            UpdateFog(fogDensity);
            
            // 更新粒子系统位置（跟随摄像机）
            UpdateParticlePositions();
        }
        
        void UpdatePrecipitation(float intensity)
        {
            switch (targetWeather)
            {
                case WeatherType.Rainy:
                case WeatherType.Stormy:
                    UpdateRain(intensity);
                    break;
                    
                case WeatherType.Snowy:
                    UpdateSnow(intensity);
                    break;
                    
                default:
                    StopPrecipitation();
                    break;
            }
        }
        
        void UpdateRain(float intensity)
        {
            if (rainParticleSystem != null)
            {
                var emission = rainParticleSystem.emission;
                emission.enabled = intensity > 0f;
                emission.rateOverTime = intensity * 1000f * rainIntensityMultiplier;
                
                var velocityOverLifetime = rainParticleSystem.velocityOverLifetime;
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-15f * (1f + intensity));
            }
            
            // 停止雪效果
            if (snowParticleSystem != null)
            {
                var snowEmission = snowParticleSystem.emission;
                snowEmission.enabled = false;
            }
        }
        
        void UpdateSnow(float intensity)
        {
            if (snowParticleSystem != null)
            {
                var emission = snowParticleSystem.emission;
                emission.enabled = intensity > 0f;
                emission.rateOverTime = intensity * 500f * snowIntensityMultiplier;
                
                var velocityOverLifetime = snowParticleSystem.velocityOverLifetime;
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-1f * (1f + intensity), -3f * (1f + intensity));
            }
            
            // 停止雨效果
            if (rainParticleSystem != null)
            {
                var rainEmission = rainParticleSystem.emission;
                rainEmission.enabled = false;
            }
        }
        
        void StopPrecipitation()
        {
            if (rainParticleSystem != null)
            {
                var rainEmission = rainParticleSystem.emission;
                rainEmission.enabled = false;
            }
            
            if (snowParticleSystem != null)
            {
                var snowEmission = snowParticleSystem.emission;
                snowEmission.enabled = false;
            }
        }
        
        void UpdateWind(float windStrength, AtmosphericData atmosphericData)
        {
            if (windZone != null)
            {
                windZone.windMain = windStrength * windStrengthMultiplier * 20f;
                windZone.windTurbulence = windStrength * 5f;
                windZone.windPulseMagnitude = windStrength * 2f;
                windZone.windPulseFrequency = 0.5f + windStrength;
                
                // 设置风向
                Vector3 windDirection = atmosphericData.GetWindAtHeight(10f).normalized;
                if (windDirection != Vector3.zero)
                {
                    windZone.transform.rotation = Quaternion.LookRotation(windDirection);
                }
            }
            
            // 更新风粒子效果
            if (windParticles != null && enableWindParticles)
            {
                var emission = windParticles.emission;
                emission.enabled = windStrength > 0.3f;
                emission.rateOverTime = windStrength * 200f;
                
                var velocityOverLifetime = windParticles.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                Vector3 windVelocity = atmosphericData.GetWindAtHeight(10f) * windStrength;
                velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(windVelocity.x);
                velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(windVelocity.z);
            }
        }
        
        void UpdateFog(float fogDensity)
        {
            if (fogParticleSystem != null)
            {
                var emission = fogParticleSystem.emission;
                emission.enabled = fogDensity > 0f;
                emission.rateOverTime = fogDensity * 100f;
                
                var main = fogParticleSystem.main;
                Color fogColor = new Color(0.8f, 0.8f, 0.8f, fogDensity * 0.5f);
                main.startColor = fogColor;
            }
            
            // 更新Unity雾效
            RenderSettings.fog = fogDensity > 0.1f;
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = new Color(0.8f, 0.8f, 0.8f);
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = fogDensity * 0.01f;
            }
        }
        
        void UpdateParticlePositions()
        {
            if (mainCamera == null) return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            // 更新粒子系统位置以跟随摄像机，并应用最大距离限制
            if (rainParticleSystem != null)
            {
                Vector3 targetPos = cameraPos + Vector3.up * precipitationArea.y * 0.5f;
                rainParticleSystem.transform.position = targetPos;
                
                // 设置粒子系统的渲染距离
                var shape = rainParticleSystem.shape;
                shape.radius = Mathf.Min(shape.radius, maxParticleDistance * 0.5f);
            }
            
            if (snowParticleSystem != null)
            {
                Vector3 targetPos = cameraPos + Vector3.up * precipitationArea.y * 0.5f;
                snowParticleSystem.transform.position = targetPos;
                
                // 设置粒子系统的渲染距离
                var shape = snowParticleSystem.shape;
                shape.radius = Mathf.Min(shape.radius, maxParticleDistance * 0.5f);
            }
            
            if (fogParticleSystem != null)
            {
                fogParticleSystem.transform.position = cameraPos;
                
                // 设置雾效的渲染距离
                var shape = fogParticleSystem.shape;
                shape.radius = Mathf.Min(shape.radius, maxParticleDistance);
            }
        }
        
        void UpdateWeatherAudio(float deltaTime)
        {
            if (!enableWeatherSounds || weatherAudioSource == null) return;
            
            WeatherState targetState = GetTargetWeatherState();
            
            // 选择合适的音效
            AudioClip targetClip = GetWeatherAudioClip(targetWeather);
            
            if (targetClip != weatherAudioSource.clip)
            {
                if (weatherAudioSource.isPlaying)
                {
                    weatherAudioSource.volume = Mathf.Lerp(weatherAudioSource.volume, 0f, deltaTime * 2f);
                    
                    if (weatherAudioSource.volume < 0.01f)
                    {
                        weatherAudioSource.clip = targetClip;
                        weatherAudioSource.Play();
                    }
                }
                else
                {
                    weatherAudioSource.clip = targetClip;
                    if (targetClip != null)
                    {
                        weatherAudioSource.Play();
                    }
                }
            }
            
            // 调整音量
            float targetVolume = targetClip != null ? targetState.ambientVolume : 0f;
            weatherAudioSource.volume = Mathf.Lerp(weatherAudioSource.volume, targetVolume, deltaTime);
        }
        
        AudioClip GetWeatherAudioClip(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rainy:
                case WeatherType.Stormy:
                    return rainSound;
                case WeatherType.Snowy:
                    return snowSound;
                case WeatherType.Foggy:
                case WeatherType.Cloudy:
                    return windSound;
                default:
                    return null;
            }
        }
        
        void UpdateLightning(float deltaTime)
        {
            WeatherState targetState = GetTargetWeatherState();
            
            if (targetState.lightningChance > 0f)
            {
                // 使用闪电频率来控制触发概率
                float adjustedChance = targetState.lightningChance * deltaTime * lightningFrequency;
                
                if (lightningCoroutine == null && Random.value < adjustedChance)
                {
                    lightningCoroutine = StartCoroutine(TriggerLightning());
                }
            }
        }
        
        IEnumerator TriggerLightning()
        {
            // 闪电闪烁效果
            lightningLight.enabled = true;
            lightningLight.intensity = lightningIntensity;
            
            // 播放雷声
            if (thunderSound != null && weatherAudioSource != null)
            {
                weatherAudioSource.PlayOneShot(thunderSound);
            }
            
            // 基于lightningDuration计算闪烁次数和间隔
            int flickerCount = Mathf.Max(1, Mathf.RoundToInt(lightningDuration * 10f)); // 基于持续时间计算闪烁次数
            float flickerInterval = lightningDuration / (flickerCount * 2f); // 计算闪烁间隔
            
            // 闪烁效果
            for (int i = 0; i < flickerCount; i++)
            {
                lightningLight.intensity = lightningIntensity * Random.Range(0.5f, 1f);
                yield return new WaitForSeconds(flickerInterval);
                lightningLight.intensity = 0f;
                yield return new WaitForSeconds(flickerInterval * 0.3f);
            }
            
            lightningLight.enabled = false;
            lightningCoroutine = null;
        }
        
        WeatherState GetCurrentWeatherState()
        {
            return GetWeatherStateByType(currentWeather);
        }
        
        WeatherState GetTargetWeatherState()
        {
            return GetWeatherStateByType(targetWeather);
        }
        
        WeatherState GetWeatherStateByType(WeatherType weatherType)
        {
            switch (weatherType)
            {
                case WeatherType.Clear: return clearState;
                case WeatherType.Cloudy: return cloudyState;
                case WeatherType.Rainy: return rainyState;
                case WeatherType.Stormy: return stormyState;
                case WeatherType.Foggy: return foggyState;
                case WeatherType.Snowy: return snowyState;
                default: return clearState;
            }
        }
        
        /// <summary>
        /// 设置目标天气
        /// </summary>
        public void SetTargetWeather(WeatherType weather)
        {
            targetWeather = weather;
            weatherTransitionProgress = 0f;
        }
        
        /// <summary>
        /// 获取当前天气
        /// </summary>
        public WeatherType GetCurrentWeather()
        {
            return currentWeather;
        }
        
        /// <summary>
        /// 获取天气转换进度
        /// </summary>
        public float GetWeatherTransitionProgress()
        {
            return weatherTransitionProgress;
        }
    }
    
    /// <summary>
    /// 天气状态数据
    /// </summary>
    [System.Serializable]
    public class WeatherState
    {
        public WeatherType weatherType;
        public float precipitationIntensity;
        public float cloudCoverage;
        public float windStrength;
        public float fogDensity;
        public float lightningChance;
        public float ambientVolume;
    }
}