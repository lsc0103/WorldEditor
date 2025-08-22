using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 季节控制器 - 管理季节变化和相关效果
    /// </summary>
    public class SeasonController : MonoBehaviour
    {
        [Header("季节设置")]
        [SerializeField] private bool enableSeasonalChanges = true;
        [SerializeField] private Season currentSeason = Season.Spring;
        [SerializeField] private float seasonDuration = 3600f; // 60分钟 = 1个季节
        [SerializeField] private float seasonTransitionSpeed = 1f;
        
        [Header("季节进度")]
        [SerializeField] private float seasonProgress = 0f; // 0-1，当前季节的进度
        [SerializeField] private bool autoProgressSeasons = true;
        
        [Header("温度变化")]
        [SerializeField] private float springBaseTemp = 15f;
        [SerializeField] private float summerBaseTemp = 25f;
        [SerializeField] private float autumnBaseTemp = 10f;
        [SerializeField] private float winterBaseTemp = 0f;
        [SerializeField] private float temperatureVariation = 5f;
        
        [Header("植被变化")]
        [SerializeField] private bool enableVegetationSeasons = true;
        [SerializeField] private Color springVegetationColor = new Color(0.4f, 0.8f, 0.2f);
        [SerializeField] private Color summerVegetationColor = new Color(0.2f, 0.7f, 0.1f);
        [SerializeField] private Color autumnVegetationColor = new Color(0.8f, 0.6f, 0.1f);
        [SerializeField] private Color winterVegetationColor = new Color(0.3f, 0.4f, 0.2f);
        
        [Header("天气影响")]
        [SerializeField] private bool seasonalWeatherChanges = true;
        [SerializeField] private float springRainChance = 0.3f;
        [SerializeField] private float summerStormChance = 0.2f;
        [SerializeField] private float autumnFogChance = 0.4f;
        [SerializeField] private float winterSnowChance = 0.6f;
        
        [Header("光照变化")]
        [SerializeField] private bool seasonalLightingChanges = true;
        [SerializeField] private float springDayLength = 12f; // 小时
        [SerializeField] private float summerDayLength = 16f;
        [SerializeField] private float autumnDayLength = 10f;
        [SerializeField] private float winterDayLength = 8f;
        
        // 事件
        public System.Action<Season> OnSeasonChanged;
        public System.Action<float> OnSeasonProgressChanged;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private Season previousSeason;
        private float seasonTimer = 0f;
        
        // 季节状态
        private SeasonState springState;
        private SeasonState summerState;
        private SeasonState autumnState;
        private SeasonState winterState;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            previousSeason = currentSeason;
            InitializeSeasonStates();
        }
        
        void InitializeSeasonStates()
        {
            // 初始化各季节状态
            springState = new SeasonState
            {
                season = Season.Spring,
                baseTemperature = springBaseTemp,
                vegetationColor = springVegetationColor,
                dayLength = springDayLength,
                rainChance = springRainChance,
                snowChance = 0f,
                fogChance = 0.1f,
                stormChance = 0.1f
            };
            
            summerState = new SeasonState
            {
                season = Season.Summer,
                baseTemperature = summerBaseTemp,
                vegetationColor = summerVegetationColor,
                dayLength = summerDayLength,
                rainChance = 0.1f,
                snowChance = 0f,
                fogChance = 0.05f,
                stormChance = summerStormChance
            };
            
            autumnState = new SeasonState
            {
                season = Season.Autumn,
                baseTemperature = autumnBaseTemp,
                vegetationColor = autumnVegetationColor,
                dayLength = autumnDayLength,
                rainChance = 0.25f,
                snowChance = 0.1f,
                fogChance = autumnFogChance,
                stormChance = 0.15f
            };
            
            winterState = new SeasonState
            {
                season = Season.Winter,
                baseTemperature = winterBaseTemp,
                vegetationColor = winterVegetationColor,
                dayLength = winterDayLength,
                rainChance = 0.05f,
                snowChance = winterSnowChance,
                fogChance = 0.2f,
                stormChance = 0.1f
            };
        }
        
        public void UpdateSeason(float deltaTime)
        {
            if (!enableSeasonalChanges) return;
            
            // 更新季节进度
            UpdateSeasonProgress(deltaTime);
            
            // 检查季节变化
            CheckSeasonTransition();
            
            // 应用季节效果
            ApplySeasonalEffects();
        }
        
        void UpdateSeasonProgress(float deltaTime)
        {
            if (autoProgressSeasons)
            {
                seasonTimer += deltaTime;
                seasonProgress = (seasonTimer % seasonDuration) / seasonDuration;
                
                OnSeasonProgressChanged?.Invoke(seasonProgress);
                
                // 检查是否需要切换季节
                if (seasonTimer >= seasonDuration)
                {
                    seasonTimer = 0f;
                    AdvanceToNextSeason();
                }
            }
        }
        
        void AdvanceToNextSeason()
        {
            previousSeason = currentSeason;
            
            switch (currentSeason)
            {
                case Season.Spring:
                    currentSeason = Season.Summer;
                    break;
                case Season.Summer:
                    currentSeason = Season.Autumn;
                    break;
                case Season.Autumn:
                    currentSeason = Season.Winter;
                    break;
                case Season.Winter:
                    currentSeason = Season.Spring;
                    break;
            }
            
            OnSeasonChanged?.Invoke(currentSeason);
        }
        
        void CheckSeasonTransition()
        {
            if (currentSeason != previousSeason)
            {
                previousSeason = currentSeason;
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }
        
        void ApplySeasonalEffects()
        {
            if (environmentSystem == null) return;
            
            var environmentState = environmentSystem.GetCurrentEnvironmentState();
            if (environmentState == null) return;
            
            // 应用季节温度
            ApplySeasonalTemperature(environmentState);
            
            // 应用季节天气影响
            if (seasonalWeatherChanges)
            {
                ApplySeasonalWeatherInfluence(environmentState);
            }
            
            // 应用季节光照变化
            if (seasonalLightingChanges)
            {
                ApplySeasonalLightingChanges(environmentState);
            }
            
            // 应用植被颜色变化
            if (enableVegetationSeasons)
            {
                ApplySeasonalVegetationChanges();
            }
        }
        
        void ApplySeasonalTemperature(EnvironmentState environmentState)
        {
            SeasonState currentSeasonState = GetCurrentSeasonState();
            SeasonState nextSeasonState = GetNextSeasonState();
            
            // 在当前季节和下一季节之间插值，应用过渡速度
            float transitionProgress = Mathf.Pow(seasonProgress, seasonTransitionSpeed);
            float baseTemp = Mathf.Lerp(currentSeasonState.baseTemperature, nextSeasonState.baseTemperature, transitionProgress);
            
            // 添加随机变化
            float tempVariation = Random.Range(-temperatureVariation, temperatureVariation);
            float finalTemperature = baseTemp + tempVariation;
            
            // 应用到环境状态
            environmentState.temperature = finalTemperature;
        }
        
        void ApplySeasonalWeatherInfluence(EnvironmentState environmentState)
        {
            SeasonState seasonState = GetCurrentSeasonState();
            
            // 根据季节调整天气概率
            // 这里可以影响天气系统的天气选择逻辑
            
            // 例如，冬季增加雪的可能性
            if (currentSeason == Season.Winter && Random.value < seasonState.snowChance)
            {
                // 可以通过事件或直接调用天气系统来影响天气
                // environmentSystem.GetWeatherSystem().SetTargetWeather(WeatherType.Snowy);
            }
        }
        
        void ApplySeasonalLightingChanges(EnvironmentState environmentState)
        {
            SeasonState seasonState = GetCurrentSeasonState();
            
            // 调整日照长度
            environmentState.dayLength = seasonState.dayLength * 60f; // 转换为分钟
            
            // 季节性光照强度调整
            float seasonalLightMultiplier = CalculateSeasonalLightMultiplier();
            environmentState.sunIntensity *= seasonalLightMultiplier;
        }
        
        float CalculateSeasonalLightMultiplier()
        {
            switch (currentSeason)
            {
                case Season.Spring: return 0.9f;
                case Season.Summer: return 1.1f;
                case Season.Autumn: return 0.8f;
                case Season.Winter: return 0.7f;
                default: return 1f;
            }
        }
        
        void ApplySeasonalVegetationChanges()
        {
            SeasonState currentSeasonState = GetCurrentSeasonState();
            SeasonState nextSeasonState = GetNextSeasonState();
            
            // 在当前季节和下一季节的植被颜色之间插值，应用过渡速度
            float transitionProgress = Mathf.Pow(seasonProgress, seasonTransitionSpeed);
            Color vegetationColor = Color.Lerp(currentSeasonState.vegetationColor, nextSeasonState.vegetationColor, transitionProgress);
            
            // 将颜色应用到全局Shader参数
            Shader.SetGlobalColor("_SeasonalVegetationColor", vegetationColor);
            Shader.SetGlobalFloat("_SeasonalColorInfluence", enableVegetationSeasons ? 1f : 0f);
        }
        
        SeasonState GetCurrentSeasonState()
        {
            switch (currentSeason)
            {
                case Season.Spring: return springState;
                case Season.Summer: return summerState;
                case Season.Autumn: return autumnState;
                case Season.Winter: return winterState;
                default: return springState;
            }
        }
        
        SeasonState GetNextSeasonState()
        {
            switch (currentSeason)
            {
                case Season.Spring: return summerState;
                case Season.Summer: return autumnState;
                case Season.Autumn: return winterState;
                case Season.Winter: return springState;
                default: return summerState;
            }
        }
        
        /// <summary>
        /// 手动设置季节
        /// </summary>
        public void SetSeason(Season season)
        {
            if (currentSeason != season)
            {
                previousSeason = currentSeason;
                currentSeason = season;
                seasonProgress = 0f;
                seasonTimer = 0f;
                
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }
        
        /// <summary>
        /// 设置季节进度
        /// </summary>
        public void SetSeasonProgress(float progress)
        {
            seasonProgress = Mathf.Clamp01(progress);
            seasonTimer = seasonProgress * seasonDuration;
            
            OnSeasonProgressChanged?.Invoke(seasonProgress);
        }
        
        /// <summary>
        /// 获取当前季节
        /// </summary>
        public Season GetCurrentSeason()
        {
            return currentSeason;
        }
        
        /// <summary>
        /// 获取季节进度
        /// </summary>
        public float GetSeasonProgress()
        {
            return seasonProgress;
        }
        
        /// <summary>
        /// 获取当前季节的基础温度
        /// </summary>
        public float GetSeasonalBaseTemperature()
        {
            return GetCurrentSeasonState().baseTemperature;
        }
        
        /// <summary>
        /// 获取当前季节的植被颜色
        /// </summary>
        public Color GetSeasonalVegetationColor()
        {
            SeasonState currentSeasonState = GetCurrentSeasonState();
            SeasonState nextSeasonState = GetNextSeasonState();
            
            return Color.Lerp(currentSeasonState.vegetationColor, nextSeasonState.vegetationColor, seasonProgress);
        }
        
        /// <summary>
        /// 设置是否启用自动季节进度
        /// </summary>
        public void SetAutoProgressSeasons(bool enable)
        {
            autoProgressSeasons = enable;
        }
        
        /// <summary>
        /// 强制推进到下一个季节
        /// </summary>
        public void ForceNextSeason()
        {
            AdvanceToNextSeason();
            seasonProgress = 0f;
            seasonTimer = 0f;
        }
    }
    
    /// <summary>
    /// 季节状态数据
    /// </summary>
    [System.Serializable]
    public class SeasonState
    {
        public Season season;
        public float baseTemperature;
        public Color vegetationColor;
        public float dayLength;
        public float rainChance;
        public float snowChance;
        public float fogChance;
        public float stormChance;
    }
}