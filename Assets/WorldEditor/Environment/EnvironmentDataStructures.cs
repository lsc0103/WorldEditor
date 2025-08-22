using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境状态 - 存储当前环境的所有状态信息
    /// </summary>
    [System.Serializable]
    public class EnvironmentState
    {
        [Header("基本环境参数")]
        public float temperature = 20f;           // 温度（摄氏度）
        public float humidity = 0.5f;             // 湿度（0-1）
        public float pressure = 1013.25f;        // 大气压力（hPa）
        public float visibility = 10000f;        // 能见度（米）
        
        [Header("风力系统")]
        public Vector3 windDirection = Vector3.forward;  // 风向
        public float windStrength = 0.3f;               // 风力强度（0-1）
        public float windTurbulence = 0.1f;             // 风力湍流
        public float windGusts = 0f;                    // 阵风强度
        
        [Header("云系统")]
        public float cloudCoverage = 0.5f;        // 云覆盖率（0-1）
        public float cloudDensity = 0.7f;         // 云密度
        public float cloudHeight = 2000f;        // 云层高度（米）
        public float cloudSpeed = 5f;            // 云移动速度
        
        [Header("降水系统")]
        public WeatherType currentWeather = WeatherType.Clear;
        public float precipitationIntensity = 0f;     // 降水强度（0-1）
        public PrecipitationType precipitationType = PrecipitationType.Rain;
        public float precipitationAccumulation = 0f;  // 降水累积量
        
        [Header("光照参数")]
        public float sunIntensity = 1f;          // 太阳光强度
        public Color sunColor = Color.white;     // 太阳光颜色
        public float sunAzimuth = 180f;          // 太阳方位角
        public float sunElevation = 45f;         // 太阳高度角
        public float moonIntensity = 0.1f;       // 月光强度
        public Color moonColor = new Color(0.8f, 0.9f, 1f, 1f); // 月光颜色
        
        [Header("大气参数")]
        public float atmosphericPressure = 1f;   // 大气压力（标准化）
        public float fogDensity = 0f;            // 雾密度
        public Color fogColor = Color.gray;      // 雾颜色
        public float atmosphereThickness = 1f;   // 大气厚度
        public float rayleighScattering = 1f;    // 瑞利散射强度
        public float mieScattering = 1f;         // 米氏散射强度
        
        [Header("时间参数")]
        public float timeOfDay = 0.5f;           // 时间（0-1，0为午夜，0.5为正午）
        public float dayLength = 1440f;          // 一天的长度（秒）
        public Season currentSeason = Season.Spring;
        public float seasonProgress = 0f;        // 季节进度（0-1）
        
        [Header("环境效果")]
        public bool lightningActive = false;     // 闪电激活
        public float lightningIntensity = 0f;    // 闪电强度
        public bool auroraActive = false;        // 极光激活
        public float auroraIntensity = 0f;       // 极光强度
        public float dustAmount = 0f;            // 灰尘/沙尘量
        public float pollutionLevel = 0f;        // 污染程度
        
        public EnvironmentState()
        {
            // 设置默认值
            windDirection = Vector3.forward;
            sunColor = Color.white;
            moonColor = new Color(0.8f, 0.9f, 1f, 1f);
            fogColor = Color.gray;
        }
        
        /// <summary>
        /// 复制环境状态
        /// </summary>
        public EnvironmentState Clone()
        {
            return new EnvironmentState
            {
                temperature = this.temperature,
                humidity = this.humidity,
                pressure = this.pressure,
                visibility = this.visibility,
                windDirection = this.windDirection,
                windStrength = this.windStrength,
                windTurbulence = this.windTurbulence,
                windGusts = this.windGusts,
                cloudCoverage = this.cloudCoverage,
                cloudDensity = this.cloudDensity,
                cloudHeight = this.cloudHeight,
                cloudSpeed = this.cloudSpeed,
                currentWeather = this.currentWeather,
                precipitationIntensity = this.precipitationIntensity,
                precipitationType = this.precipitationType,
                precipitationAccumulation = this.precipitationAccumulation,
                sunIntensity = this.sunIntensity,
                sunColor = this.sunColor,
                sunAzimuth = this.sunAzimuth,
                sunElevation = this.sunElevation,
                moonIntensity = this.moonIntensity,
                moonColor = this.moonColor,
                atmosphericPressure = this.atmosphericPressure,
                fogDensity = this.fogDensity,
                fogColor = this.fogColor,
                atmosphereThickness = this.atmosphereThickness,
                rayleighScattering = this.rayleighScattering,
                mieScattering = this.mieScattering,
                timeOfDay = this.timeOfDay,
                dayLength = this.dayLength,
                currentSeason = this.currentSeason,
                seasonProgress = this.seasonProgress,
                lightningActive = this.lightningActive,
                lightningIntensity = this.lightningIntensity,
                auroraActive = this.auroraActive,
                auroraIntensity = this.auroraIntensity,
                dustAmount = this.dustAmount,
                pollutionLevel = this.pollutionLevel
            };
        }
    }
    
    /// <summary>
    /// 大气数据 - 物理模拟的大气参数
    /// </summary>
    [System.Serializable]
    public class AtmosphericData
    {
        [Header("大气物理")]
        public float seaLevelPressure = 1013.25f;    // 海平面气压（hPa）
        public float temperatureLapseRate = 6.5f;    // 温度递减率（°C/km）
        public float tropopauseHeight = 11000f;      // 对流层顶高度（m）
        public float stratosphereTemp = -56.5f;      // 平流层温度（°C）
        
        [Header("湿度和云")]
        public float relativeHumidity = 50f;         // 相对湿度（%）
        public float dewPoint = 10f;                 // 露点温度（°C）
        public float condensationLevel = 1000f;      // 凝结高度（m）
        public float precipitableWater = 20f;        // 可降水量（mm）
        
        [Header("风场数据")]
        public Vector3[] windLayers = new Vector3[10]; // 不同高度的风场
        public float[] windHeights = new float[10];     // 风场对应高度
        public float jetStreamStrength = 30f;           // 急流强度（m/s）
        public float jetStreamHeight = 9000f;           // 急流高度（m）
        
        [Header("辐射参数")]
        public float solarRadiation = 1361f;         // 太阳辐射常数（W/m²）
        public float earthAlbedo = 0.3f;             // 地球反照率
        public float ozoneDensity = 300f;            // 臭氧密度（DU）
        public float aerosolOpticalDepth = 0.1f;     // 气溶胶光学厚度
        
        [Header("化学成分")]
        public float co2Concentration = 410f;        // CO2浓度（ppm）
        public float waterVaporDensity = 10f;        // 水汽密度（g/m³）
        public float aerosolDensity = 50f;           // 气溶胶密度（μg/m³）
        public float pollutantIndex = 0f;            // 污染物指数
        
        public AtmosphericData()
        {
            // 初始化风场数据
            InitializeWindLayers();
        }
        
        void InitializeWindLayers()
        {
            windLayers = new Vector3[10];
            windHeights = new float[10];
            
            for (int i = 0; i < 10; i++)
            {
                windHeights[i] = i * 1000f; // 每1000米一层
                windLayers[i] = new Vector3(5f, 0f, 2f); // 默认风向
            }
        }
        
        /// <summary>
        /// 获取指定高度的风向量
        /// </summary>
        public Vector3 GetWindAtHeight(float height)
        {
            if (windLayers == null || windLayers.Length == 0)
                return Vector3.zero;
            
            // 在风场层之间插值
            for (int i = 0; i < windHeights.Length - 1; i++)
            {
                if (height >= windHeights[i] && height <= windHeights[i + 1])
                {
                    float t = (height - windHeights[i]) / (windHeights[i + 1] - windHeights[i]);
                    return Vector3.Lerp(windLayers[i], windLayers[i + 1], t);
                }
            }
            
            // 如果超出范围，返回最接近的层
            if (height < windHeights[0])
                return windLayers[0];
            else
                return windLayers[windLayers.Length - 1];
        }
        
        /// <summary>
        /// 计算指定高度的温度
        /// </summary>
        public float GetTemperatureAtHeight(float height, float surfaceTemp)
        {
            if (height <= tropopauseHeight)
            {
                // 对流层：线性递减
                return surfaceTemp - (height / 1000f) * temperatureLapseRate;
            }
            else
            {
                // 平流层：恒温
                return stratosphereTemp;
            }
        }
        
        /// <summary>
        /// 计算指定高度的气压
        /// </summary>
        public float GetPressureAtHeight(float height)
        {
            // 使用气压高度公式（简化版）
            float scale = height / 8400f; // 8400m是标准大气的尺度高度
            return seaLevelPressure * Mathf.Exp(-scale);
        }
        
        /// <summary>
        /// 更新大气数据
        /// </summary>
        public void Update(float deltaTime)
        {
            // 更新风场
            UpdateWindField(deltaTime);
            
            // 更新湿度
            UpdateMoisture(deltaTime);
            
            // 更新污染物扩散
            UpdatePollutants(deltaTime);
        }
        
        void UpdateWindField(float deltaTime)
        {
            // 简单的风场变化模拟
            for (int i = 0; i < windLayers.Length; i++)
            {
                float noiseX = Time.time * 0.1f + i * 0.5f;
                float noiseY = Time.time * 0.08f + i * 0.3f;
                
                Vector3 windVariation = new Vector3(
                    Mathf.PerlinNoise(noiseX, 0f) - 0.5f,
                    0f,
                    Mathf.PerlinNoise(0f, noiseY) - 0.5f
                ) * 2f * deltaTime;
                
                windLayers[i] += windVariation;
                
                // 限制风速范围
                windLayers[i] = Vector3.ClampMagnitude(windLayers[i], 50f);
            }
        }
        
        void UpdateMoisture(float deltaTime)
        {
            // 简单的湿度变化
            float moistureVariation = Mathf.PerlinNoise(Time.time * 0.02f, 0.5f) - 0.5f;
            relativeHumidity += moistureVariation * 5f * deltaTime;
            relativeHumidity = Mathf.Clamp(relativeHumidity, 0f, 100f);
        }
        
        void UpdatePollutants(float deltaTime)
        {
            // 污染物扩散和沉降
            if (pollutantIndex > 0f)
            {
                float dispersionRate = GetWindAtHeight(100f).magnitude * 0.1f + 0.01f;
                pollutantIndex -= dispersionRate * deltaTime;
                pollutantIndex = Mathf.Max(0f, pollutantIndex);
            }
        }
    }
    
    /// <summary>
    /// 天气系统 - 天气模拟和预测
    /// </summary>
    [System.Serializable]
    public class WeatherSystem
    {
        [Header("天气状态")]
        public WeatherType currentWeather = WeatherType.Clear;
        public WeatherType targetWeather = WeatherType.Clear;
        public float weatherTransitionProgress = 0f;
        public float weatherStability = 0.8f;
        
        [Header("天气前锋")]
        public List<WeatherFront> weatherFronts = new List<WeatherFront>();
        public float frontSpeed = 10f; // km/h
        
        [Header("预报系统")]
        public WeatherForecast[] forecast = new WeatherForecast[7]; // 7天预报
        public float forecastAccuracy = 0.8f;
        
        [Header("极端天气")]
        public bool thunderstormRisk = false;
        public float tornadoRisk = 0f;
        public float hurricaneRisk = 0f;
        public bool blizzardRisk = false;
        
        private System.Random weatherRandom;
        
        public WeatherSystem()
        {
            weatherRandom = new System.Random();
            InitializeForecast();
        }
        
        void InitializeForecast()
        {
            forecast = new WeatherForecast[7];
            for (int i = 0; i < 7; i++)
            {
                forecast[i] = new WeatherForecast
                {
                    day = i,
                    weather = WeatherType.Clear,
                    minTemperature = 15f,
                    maxTemperature = 25f,
                    precipitationChance = 0.1f,
                    windSpeed = 5f
                };
            }
        }
        
        /// <summary>
        /// 更新天气系统
        /// </summary>
        public void Update(float deltaTime, AtmosphericData atmosphericData)
        {
            // 更新天气转换
            UpdateWeatherTransition(deltaTime);
            
            // 更新天气前锋
            UpdateWeatherFronts(deltaTime);
            
            // 评估极端天气风险
            EvaluateExtremeWeatherRisks(atmosphericData);
            
            // 更新天气预报
            UpdateForecast(deltaTime);
        }
        
        void UpdateWeatherTransition(float deltaTime)
        {
            if (currentWeather != targetWeather)
            {
                weatherTransitionProgress += deltaTime * weatherStability;
                
                if (weatherTransitionProgress >= 1f)
                {
                    currentWeather = targetWeather;
                    weatherTransitionProgress = 0f;
                }
            }
        }
        
        void UpdateWeatherFronts(float deltaTime)
        {
            for (int i = weatherFronts.Count - 1; i >= 0; i--)
            {
                var front = weatherFronts[i];
                front.position += front.direction * frontSpeed * deltaTime / 3600f; // 转换为km/s
                
                // 移除远离的天气前锋
                if (Vector3.Distance(front.position, Vector3.zero) > 1000f)
                {
                    weatherFronts.RemoveAt(i);
                }
            }
        }
        
        void EvaluateExtremeWeatherRisks(AtmosphericData atmosphericData)
        {
            // 评估雷暴风险
            thunderstormRisk = atmosphericData.relativeHumidity > 70f && 
                              atmosphericData.GetTemperatureAtHeight(0f, 25f) > 20f;
            
            // 评估龙卷风风险
            float windShear = CalculateWindShear(atmosphericData);
            tornadoRisk = windShear > 20f && thunderstormRisk ? 0.3f : 0f;
            
            // 评估暴风雪风险
            blizzardRisk = atmosphericData.GetTemperatureAtHeight(0f, 25f) < 0f && 
                          atmosphericData.relativeHumidity > 80f;
        }
        
        float CalculateWindShear(AtmosphericData atmosphericData)
        {
            Vector3 surfaceWind = atmosphericData.GetWindAtHeight(0f);
            Vector3 upperWind = atmosphericData.GetWindAtHeight(3000f);
            return Vector3.Distance(upperWind, surfaceWind);
        }
        
        void UpdateForecast(float deltaTime)
        {
            // 简单的天气预报更新逻辑
            // 实际应用中这里会有复杂的数值天气预报模型
            
            for (int i = 0; i < forecast.Length; i++)
            {
                var dailyForecast = forecast[i];
                
                // 根据当前大气条件调整预报
                dailyForecast.precipitationChance = Mathf.Lerp(
                    dailyForecast.precipitationChance,
                    CalculatePrecipitationProbability(),
                    deltaTime * 0.1f
                );
            }
        }
        
        float CalculatePrecipitationProbability()
        {
            // 基于多个因素计算降水概率
            float humidity = 0.6f; // 简化，实际应从大气数据获取
            float pressure = 1000f; // 简化
            float temperature = 20f; // 简化
            
            float baseProb = humidity;
            if (pressure < 1010f) baseProb += 0.2f; // 低压增加降水概率
            if (temperature > 25f) baseProb += 0.1f; // 高温增加对流降水概率
            
            return Mathf.Clamp01(baseProb);
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
        /// 获取天气转换进度
        /// </summary>
        public float GetWeatherTransitionProgress()
        {
            return weatherTransitionProgress;
        }
        
        /// <summary>
        /// 设置大气压力
        /// </summary>
        public void SetAtmosphericPressure(float pressure)
        {
            // 大气压力影响天气稳定性和降水概率
            weatherStability = Mathf.Lerp(weatherStability, pressure, 0.1f);
            
            // 低压系统通常带来不稳定天气
            if (pressure < 0.95f && currentWeather == WeatherType.Clear)
            {
                // 可能转向多云或雨天
                if (UnityEngine.Random.value < 0.3f)
                {
                    SetTargetWeather(WeatherType.Cloudy);
                }
            }
        }
    }
    
    /// <summary>
    /// 环境数据 - 综合环境信息
    /// </summary>
    [System.Serializable]
    public class EnvironmentData
    {
        public EnvironmentState currentState;
        public AtmosphericData atmosphericData;
        public WeatherSystem weatherSystem;
        
        public EnvironmentData()
        {
            currentState = new EnvironmentState();
            atmosphericData = new AtmosphericData();
            weatherSystem = new WeatherSystem();
        }
    }
    
    /// <summary>
    /// 天气前锋
    /// </summary>
    [System.Serializable]
    public class WeatherFront
    {
        public Vector3 position;
        public Vector3 direction;
        public WeatherFrontType frontType;
        public float intensity;
        public float size;
        public WeatherType associatedWeather;
    }
    
    /// <summary>
    /// 天气预报
    /// </summary>
    [System.Serializable]
    public class WeatherForecast
    {
        public int day;
        public WeatherType weather;
        public float minTemperature;
        public float maxTemperature;
        public float precipitationChance;
        public float windSpeed;
        public Vector3 windDirection;
        public float humidity;
        public float pressure;
    }
    
    /// <summary>
    /// 环境配置文件
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentProfile", menuName = "WorldEditor/Environment Profile")]
    public class EnvironmentProfile : ScriptableObject
    {
        [Header("基础设置")]
        public string profileName = "Default Environment";
        public string description = "Default environment settings";
        
        [Header("默认状态")]
        public EnvironmentState defaultState;
        
        [Header("天气设置")]
        public WeatherTransitionSettings weatherTransitions;
        
        [Header("光照设置")]
        public LightingSettings lightingSettings;
        
        [Header("音频设置")]
        public AudioSettings audioSettings;
        
        public void Initialize()
        {
            if (defaultState == null)
                defaultState = new EnvironmentState();
            
            if (weatherTransitions == null)
                weatherTransitions = new WeatherTransitionSettings();
            
            if (lightingSettings == null)
                lightingSettings = new LightingSettings();
            
            if (audioSettings == null)
                audioSettings = new AudioSettings();
        }
        
        // 环境配置文件访问方法
        public float GetUpdateFrequency() => weatherTransitions?.transitionSpeed ?? 0.1f;
        public EnvironmentQuality GetQualityLevel() => EnvironmentQuality.High; // 默认高质量
        public bool IsPhysicsBasedWeatherEnabled() => weatherTransitions?.enableRandomWeatherChanges ?? true;
        public bool IsTemperatureGradientsEnabled() => true; // 默认启用
        public bool IsAtmosphericPressureEnabled() => true; // 默认启用
        public float GetTransitionSpeed() => weatherTransitions?.transitionSpeed ?? 1f;
        public bool IsLODSystemEnabled() => true; // 默认启用LOD
        public float GetMaxUpdateDistance() => 1000f; // 默认最大更新距离
    }
    
    // 支持类和设置
    [System.Serializable]
    public class WeatherTransitionSettings
    {
        public float transitionSpeed = 1f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool enableRandomWeatherChanges = true;
        public float weatherChangeInterval = 300f; // 5分钟
    }
    
    [System.Serializable]
    public class LightingSettings
    {
        public Gradient sunColorGradient;
        public AnimationCurve sunIntensityCurve;
        public Gradient moonColorGradient;
        public AnimationCurve moonIntensityCurve;
        public bool enableDynamicExposure = true;
        public float exposureSpeed = 1f;
    }
    
    [System.Serializable]
    public class AudioSettings
    {
        public AudioClip[] ambientSounds;
        public AudioClip[] weatherSounds;
        public float masterVolume = 1f;
        public float fadeSpeed = 1f;
        public bool enable3DAudio = true;
    }
    
    // 枚举定义
    public enum PrecipitationType
    {
        None,
        Rain,
        Snow,
        Sleet,
        Hail,
        Drizzle
    }
    
    public enum WeatherFrontType
    {
        Cold,
        Warm,
        Occluded,
        Stationary
    }
}