using UnityEngine;
using System;
using System.Collections;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 光照系统 - 管理环境光照、太阳光照和动态光照效果
    /// 
    /// 核心功能：
    /// - 动态太阳光照控制（基于时间系统）
    /// - 环境光照管理和大气散射
    /// - 月亮光照和夜间照明
    /// - 实时阴影和光照质量管理
    /// - 与天气系统联动的光照变化
    /// </summary>
    public class LightingSystem : MonoBehaviour
    {
        #region 光照配置参数

        [Header("太阳光照配置")]
        [Tooltip("太阳光照强度曲线（基于一天中的时间）")]
        public AnimationCurve sunIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Tooltip("太阳光照颜色梯度（基于一天中的时间）")]
        public Gradient sunColorGradient;
        
        [Tooltip("太阳光照最大强度")]
        [Range(0f, 8f)]
        public float maxSunIntensity = 1.2f;

        [Header("环境光照配置")]
        [Tooltip("环境光强度曲线（基于一天中的时间）")]
        public AnimationCurve ambientIntensityCurve = AnimationCurve.EaseInOut(0, 0.3f, 1, 0.3f);
        
        [Tooltip("环境光颜色梯度（基于一天中的时间）")]
        public Gradient ambientColorGradient;
        
        [Tooltip("环境光最大强度")]
        [Range(0f, 2f)]
        public float maxAmbientIntensity = 0.4f;

        [Header("月亮光照配置")]
        [Tooltip("月亮光照颜色")]
        public Color moonColor = new Color(0.8f, 0.9f, 1f, 1f);
        
        [Tooltip("月亮光照强度")]
        [Range(0f, 1f)]
        public float moonIntensity = 0.15f;
        
        [Tooltip("是否启用月亮光照")]
        public bool enableMoonLight = true;

        #endregion

        #region 光照对象引用

        [Header("光照对象")]
        [Tooltip("太阳光照对象（主方向光）")]
        public Light sunLight;
        
        [Tooltip("月亮光照对象（次方向光）")]
        public Light moonLight;
        
        [Tooltip("是否自动查找光照对象")]
        public bool autoFindLights = true;

        #endregion

        #region 阴影配置

        [Header("阴影设置")]
        [Tooltip("阴影距离")]
        [Range(50f, 500f)]
        public float shadowDistance = 150f;
        
        [Tooltip("阴影质量")]
        public ShadowQuality shadowQuality = ShadowQuality.HardOnly;
        
        [Tooltip("阴影分辨率")]
        public ShadowResolution shadowResolution = ShadowResolution.Medium;

        #endregion

        #region 运行时状态

        private bool isInitialized = false;
        private bool isActive = false;
        private float currentTimeOfDay = 0.5f;
        private SeasonType currentSeason = SeasonType.Spring;
        private WeatherType currentWeather = WeatherType.Clear;
        private float currentWeatherIntensity = 1f;
        private EnvironmentState linkedEnvironmentState;
        private TimeSystem linkedTimeSystem;
        private SeasonSystem linkedSeasonSystem;
        private WeatherSystem linkedWeatherSystem;

        #endregion

        #region 事件系统

        /// <summary>光照强度变化事件</summary>
        public event Action<float> OnLightIntensityChanged;
        
        /// <summary>光照颜色变化事件</summary>
        public event Action<Color> OnLightColorChanged;

        #endregion

        #region 公共属性

        /// <summary>光照系统是否激活</summary>
        public bool IsActive => isActive && isInitialized;
        
        /// <summary>当前太阳光强度</summary>
        public float CurrentSunIntensity => sunLight ? sunLight.intensity : 0f;
        
        /// <summary>当前环境光强度</summary>
        public float CurrentAmbientIntensity => RenderSettings.ambientIntensity;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化光照系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null, TimeSystem timeSystem = null, SeasonSystem seasonSystem = null, WeatherSystem weatherSystem = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[LightingSystem] 光照系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[LightingSystem] 开始初始化光照系统...");

            // 链接系统引用
            linkedEnvironmentState = environmentState;
            linkedTimeSystem = timeSystem;
            linkedSeasonSystem = seasonSystem;
            linkedWeatherSystem = weatherSystem;

            // 自动查找光照对象
            if (autoFindLights)
            {
                FindLightObjects();
            }

            // 初始化默认梯度
            InitializeDefaultGradients();

            // 配置阴影设置
            ConfigureShadowSettings();

            // 订阅系统事件
            SubscribeToSystemEvents();

            // 同步环境状态
            if (linkedEnvironmentState != null)
            {
                SyncFromEnvironmentState();
            }

            // 执行初始光照更新
            if (linkedEnvironmentState != null)
            {
                SetTimeOfDay(linkedEnvironmentState.timeOfDay);
            }
            else
            {
                SetTimeOfDay(currentTimeOfDay);
            }

            isActive = true;
            isInitialized = true;

            Debug.Log("[LightingSystem] 光照系统初始化完成");
        }

        /// <summary>
        /// 订阅系统事件
        /// </summary>
        private void SubscribeToSystemEvents()
        {
            // 订阅时间系统事件
            if (linkedTimeSystem != null)
            {
                linkedTimeSystem.OnTimeChanged += HandleTimeChanged;
                Debug.Log("[LightingSystem] 已订阅时间系统事件");
            }

            // 订阅季节系统事件
            if (linkedSeasonSystem != null)
            {
                SeasonSystem.OnSeasonChanged += HandleSeasonChanged;
                Debug.Log("[LightingSystem] 已订阅季节系统事件");
            }

            // 订阅天气系统事件
            if (linkedWeatherSystem != null)
            {
                linkedWeatherSystem.OnWeatherChanged += HandleWeatherChanged;
                linkedWeatherSystem.OnWeatherIntensityChanged += HandleWeatherIntensityChanged;
                Debug.Log("[LightingSystem] 已订阅天气系统事件");
            }
        }

        /// <summary>
        /// 自动查找光照对象
        /// </summary>
        private void FindLightObjects()
        {
            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional && light.gameObject.name.ToLower().Contains("sun"))
                    {
                        sunLight = light;
                        break;
                    }
                }
                
                // 如果没找到，创建一个
                if (sunLight == null)
                {
                    GameObject sunLightGO = new GameObject("Sun Light");
                    sunLight = sunLightGO.AddComponent<Light>();
                    sunLight.type = LightType.Directional;
                    sunLight.shadows = LightShadows.Soft;
                }
            }

            if (moonLight == null && enableMoonLight)
            {
                GameObject moonLightGO = new GameObject("Moon Light");
                moonLight = moonLightGO.AddComponent<Light>();
                moonLight.type = LightType.Directional;
                moonLight.shadows = LightShadows.Soft;
                moonLight.color = moonColor;
                moonLight.intensity = 0f; // 白天时关闭
            }
        }

        /// <summary>
        /// 初始化默认梯度
        /// </summary>
        private void InitializeDefaultGradients()
        {
            // 太阳光颜色梯度（模拟一天的色温变化）
            if (sunColorGradient == null)
            {
                sunColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.0f),   // 深夜/黄昏
                    new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0.25f),  // 日出
                    new GradientColorKey(Color.white, 0.5f),                  // 正午
                    new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0.75f),  // 日落
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1.0f)    // 深夜
                };
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                
                sunColorGradient.SetKeys(colorKeys, alphaKeys);
            }

            // 环境光颜色梯度
            if (ambientColorGradient == null)
            {
                ambientColorGradient = new Gradient();
                GradientColorKey[] ambientColorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.2f, 0.3f, 0.5f), 0.0f),  // 深夜
                    new GradientColorKey(new Color(0.8f, 0.7f, 0.6f), 0.25f), // 日出
                    new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f),    // 正午
                    new GradientColorKey(new Color(0.8f, 0.6f, 0.4f), 0.75f), // 日落
                    new GradientColorKey(new Color(0.2f, 0.3f, 0.5f), 1.0f)   // 深夜
                };
                
                GradientAlphaKey[] ambientAlphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                
                ambientColorGradient.SetKeys(ambientColorKeys, ambientAlphaKeys);
            }
        }

        /// <summary>
        /// 配置阴影设置
        /// </summary>
        private void ConfigureShadowSettings()
        {
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadows = shadowQuality;
            QualitySettings.shadowResolution = shadowResolution;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理时间变化事件
        /// </summary>
        private void HandleTimeChanged(float normalizedTime)
        {
            currentTimeOfDay = normalizedTime;
            SetTimeOfDay(normalizedTime);
        }

        /// <summary>
        /// 处理季节变化事件
        /// </summary>
        private void HandleSeasonChanged(SeasonType newSeason, SeasonType oldSeason)
        {
            currentSeason = newSeason;
            // 季节变化时重新计算光照
            SetTimeOfDay(currentTimeOfDay);
            Debug.Log($"[LightingSystem] 光照适配季节变化: {oldSeason} → {newSeason}");
        }

        /// <summary>
        /// 处理天气变化事件
        /// </summary>
        private void HandleWeatherChanged(WeatherType newWeather, WeatherType oldWeather)
        {
            currentWeather = newWeather;
            // 天气变化时重新计算光照
            SetTimeOfDay(currentTimeOfDay);
            Debug.Log($"[LightingSystem] 光照适配天气变化: {oldWeather} → {newWeather}");
        }

        /// <summary>
        /// 处理天气强度变化事件
        /// </summary>
        private void HandleWeatherIntensityChanged(float intensity)
        {
            currentWeatherIntensity = intensity;
            // 天气强度变化时重新计算光照
            SetTimeOfDay(currentTimeOfDay);
        }

        #endregion

        #region 光照控制方法

        /// <summary>
        /// 设置一天中的时间
        /// </summary>
        public void SetTimeOfDay(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            currentTimeOfDay = normalizedTime;
            
            UpdateSunLighting(normalizedTime);
            UpdateAmbientLighting(normalizedTime);
            UpdateMoonLighting(normalizedTime);
            
            // 同步到环境状态
            SyncToEnvironmentState();
        }

        /// <summary>
        /// 更新太阳光照 - 真实物理光照模拟
        /// </summary>
        private void UpdateSunLighting(float timeOfDay)
        {
            if (sunLight == null) return;

            // 1. 计算真实的太阳角度 (基于物理日照模型)
            float sunElevation = CalculateRealisticSunElevation(timeOfDay);
            float sunAzimuth = CalculateRealisticSunAzimuth(timeOfDay);
            
            // 2. 计算大气散射影响的光照强度
            float atmosphericIntensity = CalculateAtmosphericIntensity(sunElevation);
            float intensity = atmosphericIntensity * maxSunIntensity;
            
            // 3. 计算色温和大气散射颜色
            Color atmosphericColor = CalculateAtmosphericColor(sunElevation, timeOfDay);
            
            // 4. 应用季节调整
            ApplySeasonalAdjustment(ref intensity, ref atmosphericColor);
            
            // 4.5. 应用天气影响
            ApplyWeatherInfluence(ref intensity, ref atmosphericColor, sunElevation);
            
            // 5. 设置太阳光照参数
            sunLight.intensity = intensity;
            sunLight.color = atmosphericColor;
            
            // 6. 设置真实的太阳位置
            sunLight.transform.rotation = Quaternion.Euler(sunElevation, sunAzimuth, 0f);
            
            // 7. 动态调整阴影设置
            UpdateDynamicShadows(sunElevation, intensity);
            
            // 8. 触发事件
            OnLightIntensityChanged?.Invoke(intensity);
            OnLightColorChanged?.Invoke(atmosphericColor);
        }

        /// <summary>
        /// 更新环境光照 - 真实大气光照模拟
        /// </summary>
        private void UpdateAmbientLighting(float timeOfDay)
        {
            // 获取太阳角度信息
            float sunElevation = CalculateRealisticSunElevation(timeOfDay);
            
            // 1. 计算基础环境光强度 (考虑太阳角度)
            float baseAmbientIntensity = CalculateAmbientIntensity(sunElevation, timeOfDay);
            RenderSettings.ambientIntensity = baseAmbientIntensity * maxAmbientIntensity;

            // 2. 计算天空光颜色 (基于瑞利散射)
            Color skyColor = CalculateSkyAmbientColor(sunElevation, timeOfDay);
            Color equatorColor = CalculateEquatorAmbientColor(sunElevation, timeOfDay);
            Color groundColor = CalculateGroundAmbientColor(sunElevation, timeOfDay);
            
            // 3. 应用天气对环境光的影响
            ApplyWeatherToAmbientLight(ref skyColor, ref equatorColor, ref groundColor);
            
            // 4. 设置分层环境光
            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = equatorColor;
            RenderSettings.ambientGroundColor = groundColor;
            
            // 5. 设置雾效颜色匹配环境光
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor = Color.Lerp(groundColor, skyColor, 0.5f);
            }
        }
        
        /// <summary>
        /// 计算环境光强度
        /// </summary>
        private float CalculateAmbientIntensity(float sunElevation, float timeOfDay)
        {
            // 白天的环境光主要来自天空散射
            if (sunElevation > 0f)
            {
                return Mathf.Clamp01(sunElevation / 90f) * 0.8f + 0.2f; // 最小20%，最大100%
            }
            // 夜间环境光来自月光和星光
            else
            {
                float nightFactor = Mathf.Clamp01((-sunElevation) / 18f); // 天文暮光角度
                return Mathf.Lerp(0.5f, 0.05f, nightFactor); // 从50%衰减到5%
            }
        }
        
        /// <summary>
        /// 计算天空环境光颜色
        /// </summary>
        private Color CalculateSkyAmbientColor(float sunElevation, float timeOfDay)
        {
            if (sunElevation > 0f)
            {
                // 白天：蓝色天空散射
                Color dayColor = new Color(0.5f, 0.7f, 1f, 1f);
                
                // 日出日落时偏向暖色
                if (sunElevation < 20f)
                {
                    Color warmColor = new Color(1f, 0.8f, 0.6f, 1f);
                    float warmFactor = 1f - (sunElevation / 20f);
                    dayColor = Color.Lerp(dayColor, warmColor, warmFactor * 0.5f);
                }
                
                return dayColor;
            }
            else
            {
                // 夜间：深蓝紫色
                return new Color(0.1f, 0.15f, 0.3f, 1f);
            }
        }
        
        /// <summary>
        /// 计算地平线环境光颜色
        /// </summary>
        private Color CalculateEquatorAmbientColor(float sunElevation, float timeOfDay)
        {
            Color skyColor = CalculateSkyAmbientColor(sunElevation, timeOfDay);
            Color groundColor = CalculateGroundAmbientColor(sunElevation, timeOfDay);
            
            // 地平线是天空和地面的混合
            return Color.Lerp(groundColor, skyColor, 0.7f);
        }
        
        /// <summary>
        /// 计算地面环境光颜色
        /// </summary>
        private Color CalculateGroundAmbientColor(float sunElevation, float timeOfDay)
        {
            if (sunElevation > 0f)
            {
                // 白天：温暖的地面反射光
                Color dayGroundColor = new Color(0.4f, 0.4f, 0.3f, 1f);
                
                // 日出日落时更暖
                if (sunElevation < 20f)
                {
                    Color warmGroundColor = new Color(0.6f, 0.4f, 0.2f, 1f);
                    float warmFactor = 1f - (sunElevation / 20f);
                    dayGroundColor = Color.Lerp(dayGroundColor, warmGroundColor, warmFactor * 0.6f);
                }
                
                return dayGroundColor;
            }
            else
            {
                // 夜间：很暗的地面
                return new Color(0.05f, 0.05f, 0.08f, 1f);
            }
        }

        /// <summary>
        /// 更新月亮光照
        /// </summary>
        private void UpdateMoonLighting(float timeOfDay)
        {
            if (!enableMoonLight || moonLight == null) return;

            // 夜晚时启用月亮光照
            bool isNighttime = timeOfDay < 0.25f || timeOfDay > 0.75f;
            
            if (isNighttime)
            {
                // 计算月亮强度（与太阳强度相反）
                float nightIntensity = (timeOfDay < 0.25f) ? (0.25f - timeOfDay) * 4f : (timeOfDay - 0.75f) * 4f;
                moonLight.intensity = nightIntensity * moonIntensity;
                
                // 月亮角度（与太阳相对）
                float moonAngle = sunLight.transform.eulerAngles.x + 180f;
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, -30f, 0f);
            }
            else
            {
                moonLight.intensity = 0f;
            }
        }

        /// <summary>
        /// 设置光照强度倍数
        /// </summary>
        public void SetLightIntensityMultiplier(float multiplier)
        {
            multiplier = Mathf.Clamp(multiplier, 0f, 5f);
            maxSunIntensity = 1.2f * multiplier;
            maxAmbientIntensity = 0.4f * multiplier;
            
            // 重新计算光照
            SetTimeOfDay(currentTimeOfDay);
        }

        #endregion

        #region 系统更新

        /// <summary>
        /// 更新光照系统 (由EnvironmentManager调用)
        /// </summary>
        public void UpdateSystem()
        {
            if (!isActive) return;

            // 从环境状态同步
            if (linkedEnvironmentState != null)
            {
                if (Mathf.Abs(currentTimeOfDay - linkedEnvironmentState.timeOfDay) > 0.001f)
                {
                    SetTimeOfDay(linkedEnvironmentState.timeOfDay);
                }
            }
        }

        #endregion

        #region 环境状态同步

        /// <summary>
        /// 从环境状态同步
        /// </summary>
        private void SyncFromEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;
            
            SetTimeOfDay(linkedEnvironmentState.timeOfDay);
        }

        /// <summary>
        /// 同步到环境状态
        /// </summary>
        private void SyncToEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;

            if (sunLight != null)
            {
                linkedEnvironmentState.sunColor = sunLight.color;
                linkedEnvironmentState.sunIntensity = sunLight.intensity;
            }
            
            linkedEnvironmentState.ambientColor = RenderSettings.ambientSkyColor;
            linkedEnvironmentState.ambientIntensity = RenderSettings.ambientIntensity;
            
            if (moonLight != null)
            {
                linkedEnvironmentState.moonColor = moonLight.color;
                linkedEnvironmentState.moonIntensity = moonLight.intensity;
            }
        }

        #endregion

        #region 调试信息

        void OnGUI()
        {
            // 调试面板已禁用
            /*
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            GUILayout.BeginArea(new Rect(530, 10, 200, 120));
            GUILayout.Box("光照系统调试");
            
            GUILayout.Label($"时间: {currentTimeOfDay:F2}");
            GUILayout.Label($"太阳强度: {(sunLight ? sunLight.intensity : 0):F2}");
            GUILayout.Label($"环境光强度: {RenderSettings.ambientIntensity:F2}");
            GUILayout.Label($"月亮强度: {(moonLight ? moonLight.intensity : 0):F2}");
            
            GUILayout.EndArea();
            */
        }

        /// <summary>
        /// 应用季节性光照调整 - 考虑季节进度的渐变效果
        /// </summary>
        private void ApplySeasonalAdjustment(ref float intensity, ref Color color)
        {
            // 获取季节进度
            float seasonProgress = linkedEnvironmentState?.seasonProgress ?? 0.5f;
            
            // 根据当前季节和进度计算光照调整
            Color targetSeasonColor;
            float intensityMultiplier = 1f;
            
            switch (currentSeason)
            {
                case SeasonType.Spring:
                    // 春季：从冬季冷色调渐变到温暖清新
                    Color earlySpring = new Color(0.98f, 0.98f, 1f);     // 初春仍有冷感
                    Color lateSpring = new Color(1f, 1f, 0.95f);         // 晚春温暖清新
                    targetSeasonColor = Color.Lerp(earlySpring, lateSpring, seasonProgress);
                    intensityMultiplier = Mathf.Lerp(0.95f, 1.02f, seasonProgress); // 光照逐渐增强
                    break;
                    
                case SeasonType.Summer:
                    // 夏季：从温暖到炽热
                    Color earlySummer = new Color(1f, 0.99f, 0.92f);     // 初夏温暖
                    Color lateSummer = new Color(1f, 0.98f, 0.85f);      // 盛夏炽热
                    targetSeasonColor = Color.Lerp(earlySummer, lateSummer, seasonProgress);
                    intensityMultiplier = Mathf.Lerp(1.02f, 1.08f, seasonProgress); // 光照最强
                    break;
                    
                case SeasonType.Autumn:
                    // 秋季：从温暖逐渐转向金黄橘红
                    Color earlyAutumn = new Color(1f, 0.95f, 0.8f);      // 初秋温和
                    Color lateAutumn = new Color(1f, 0.8f, 0.6f);        // 深秋金黄
                    targetSeasonColor = Color.Lerp(earlyAutumn, lateAutumn, seasonProgress);
                    intensityMultiplier = Mathf.Lerp(1f, 0.95f, seasonProgress); // 光照逐渐减弱
                    break;
                    
                case SeasonType.Winter:
                    // 冬季：逐渐变得更冷更蓝
                    Color earlyWinter = new Color(0.98f, 0.98f, 1f);     // 初冬微冷
                    Color lateWinter = new Color(0.9f, 0.9f, 1f);        // 深冬严寒
                    targetSeasonColor = Color.Lerp(earlyWinter, lateWinter, seasonProgress);
                    intensityMultiplier = Mathf.Lerp(0.95f, 0.85f, seasonProgress); // 光照最弱
                    break;
                    
                default:
                    targetSeasonColor = Color.white;
                    break;
            }
            
            // 应用季节调整，使用进度来控制影响强度
            float seasonInfluence = 0.1f + (seasonProgress * 0.1f); // 进度越高，季节特征越明显
            color = Color.Lerp(color, targetSeasonColor, seasonInfluence);
            intensity *= intensityMultiplier;
            
            // 调试输出 (可选)
            #if UNITY_EDITOR
            if (seasonProgress != 0.5f) // 避免默认值时的大量日志
            {
                //Debug.Log($"[LightingSystem] 季节调整 - {currentSeason} 进度:{seasonProgress:F2} 强度倍率:{intensityMultiplier:F2} 颜色:{targetSeasonColor}");
            }
            #endif
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        void OnDestroy()
        {
            if (linkedTimeSystem != null)
            {
                linkedTimeSystem.OnTimeChanged -= HandleTimeChanged;
            }
            
            SeasonSystem.OnSeasonChanged -= HandleSeasonChanged;
            
            if (linkedWeatherSystem != null)
            {
                linkedWeatherSystem.OnWeatherChanged -= HandleWeatherChanged;
                linkedWeatherSystem.OnWeatherIntensityChanged -= HandleWeatherIntensityChanged;
            }
        }

        #endregion

        #region 天气光照影响计算

        /// <summary>
        /// 应用天气对太阳光的影响
        /// </summary>
        private void ApplyWeatherInfluence(ref float intensity, ref Color color, float sunElevation)
        {
            if (linkedEnvironmentState == null) return;

            // 从环境状态同步天气信息
            currentWeather = linkedEnvironmentState.currentWeather;
            currentWeatherIntensity = linkedEnvironmentState.weatherIntensity;

            // 根据天气类型计算光照修正参数
            float intensityMultiplier = 1f;
            Color weatherTint = Color.white;
            
            switch (currentWeather)
            {
                case WeatherType.Clear:
                    // 晴天：最佳光照条件
                    intensityMultiplier = 1f;
                    weatherTint = Color.white;
                    break;
                    
                case WeatherType.Cloudy:
                    // 多云：光照轻微减弱，色调偏冷
                    intensityMultiplier = Mathf.Lerp(1f, 0.8f, currentWeatherIntensity);
                    weatherTint = Color.Lerp(Color.white, new Color(0.9f, 0.95f, 1f), currentWeatherIntensity * 0.3f);
                    break;
                    
                case WeatherType.Overcast:
                    // 阴天：光照明显减弱，灰蒙蒙的效果
                    intensityMultiplier = Mathf.Lerp(1f, 0.6f, currentWeatherIntensity);
                    weatherTint = Color.Lerp(Color.white, new Color(0.8f, 0.85f, 0.9f), currentWeatherIntensity * 0.5f);
                    break;
                    
                case WeatherType.Rainy:
                    // 雨天：光照大幅减弱，冷色调
                    intensityMultiplier = Mathf.Lerp(1f, 0.4f, currentWeatherIntensity);
                    weatherTint = Color.Lerp(Color.white, new Color(0.7f, 0.8f, 0.9f), currentWeatherIntensity * 0.6f);
                    
                    // 雨天特殊效果：增加动态光照变化模拟云层遮挡
                    if (currentWeatherIntensity > 0.5f)
                    {
                        float cloudFlicker = Mathf.PerlinNoise(Time.time * 0.5f, 0f) * 0.3f;
                        intensityMultiplier *= (1f + cloudFlicker);
                    }
                    break;
                    
                case WeatherType.Storm:
                    // 暴风雨：光照剧烈减弱，深冷色调
                    intensityMultiplier = Mathf.Lerp(1f, 0.2f, currentWeatherIntensity);
                    weatherTint = Color.Lerp(Color.white, new Color(0.5f, 0.6f, 0.7f), currentWeatherIntensity * 0.8f);
                    
                    // 暴风雨特殊效果：剧烈的光照变化
                    float stormFlicker = Mathf.PerlinNoise(Time.time * 2f, 0f) * 0.5f;
                    intensityMultiplier *= (1f + stormFlicker);
                    break;
                    
                case WeatherType.Snowy:
                    // 雪天：光照减弱但反射增加，冷白色调
                    intensityMultiplier = Mathf.Lerp(1f, 0.7f, currentWeatherIntensity);
                    weatherTint = Color.Lerp(Color.white, new Color(0.95f, 0.98f, 1f), currentWeatherIntensity * 0.4f);
                    
                    // 雪地反射增强环境光
                    if (sunElevation > 10f)
                    {
                        float snowReflection = currentWeatherIntensity * 0.2f;
                        intensityMultiplier += snowReflection;
                    }
                    break;
                    
                case WeatherType.Foggy:
                    // 雾天：光照散射，柔和但昏暗
                    intensityMultiplier = Mathf.Lerp(1f, 0.5f, currentWeatherIntensity);
                    weatherTint = Color.Lerp(Color.white, new Color(0.85f, 0.9f, 0.95f), currentWeatherIntensity * 0.4f);
                    
                    // 雾的散射效果：降低对比度
                    float fogScatter = currentWeatherIntensity * 0.3f;
                    weatherTint = Color.Lerp(weatherTint, Color.gray, fogScatter);
                    break;
                    
                case WeatherType.Windy:
                    // 大风：光照轻微波动，模拟云层快速移动
                    intensityMultiplier = 1f;
                    weatherTint = Color.white;
                    
                    // 风的效果：快速的光照变化
                    float windFlicker = Mathf.PerlinNoise(Time.time * 3f, 0f) * 0.2f * currentWeatherIntensity;
                    intensityMultiplier *= (1f + windFlicker);
                    break;
            }
            
            // 应用天气影响
            intensity *= intensityMultiplier;
            color = Color.Lerp(color, color * weatherTint, 0.8f); // 温和地混合颜色
            
            // 调试输出
            #if UNITY_EDITOR
            if (currentWeatherIntensity > 0.1f && Time.frameCount % 300 == 0) // 每5秒输出一次日志
            {
                Debug.Log($"[LightingSystem] 天气光照影响 - {currentWeather} (强度:{currentWeatherIntensity:F2}) 强度倍率:{intensityMultiplier:F2}");
            }
            #endif
        }
        
        /// <summary>
        /// 应用天气对环境光的影响
        /// </summary>
        private void ApplyWeatherToAmbientLight(ref Color skyColor, ref Color equatorColor, ref Color groundColor)
        {
            if (linkedEnvironmentState == null) return;

            // 从环境状态同步天气信息
            currentWeather = linkedEnvironmentState.currentWeather;
            currentWeatherIntensity = linkedEnvironmentState.weatherIntensity;

            Color weatherSkyTint = Color.white;
            Color weatherGroundTint = Color.white;
            float ambientReduction = 1f;
            
            switch (currentWeather)
            {
                case WeatherType.Clear:
                    // 晴天：保持原有颜色
                    break;
                    
                case WeatherType.Cloudy:
                case WeatherType.Overcast:
                    // 多云/阴天：降低饱和度，偏灰
                    ambientReduction = Mathf.Lerp(1f, 0.8f, currentWeatherIntensity);
                    weatherSkyTint = Color.Lerp(Color.white, new Color(0.9f, 0.9f, 0.9f), currentWeatherIntensity * 0.4f);
                    weatherGroundTint = Color.Lerp(Color.white, new Color(0.85f, 0.85f, 0.85f), currentWeatherIntensity * 0.3f);
                    break;
                    
                case WeatherType.Rainy:
                    // 雨天：冷色调，降低亮度
                    ambientReduction = Mathf.Lerp(1f, 0.6f, currentWeatherIntensity);
                    weatherSkyTint = Color.Lerp(Color.white, new Color(0.8f, 0.85f, 0.95f), currentWeatherIntensity * 0.5f);
                    weatherGroundTint = Color.Lerp(Color.white, new Color(0.7f, 0.75f, 0.8f), currentWeatherIntensity * 0.4f);
                    break;
                    
                case WeatherType.Storm:
                    // 暴风雨：深冷色调，大幅降低亮度
                    ambientReduction = Mathf.Lerp(1f, 0.4f, currentWeatherIntensity);
                    weatherSkyTint = Color.Lerp(Color.white, new Color(0.6f, 0.7f, 0.8f), currentWeatherIntensity * 0.7f);
                    weatherGroundTint = Color.Lerp(Color.white, new Color(0.5f, 0.55f, 0.6f), currentWeatherIntensity * 0.6f);
                    break;
                    
                case WeatherType.Snowy:
                    // 雪天：冷白色调，增加地面反射
                    ambientReduction = Mathf.Lerp(1f, 1.1f, currentWeatherIntensity); // 雪地反射实际上会增加环境光
                    weatherSkyTint = Color.Lerp(Color.white, new Color(0.95f, 0.97f, 1f), currentWeatherIntensity * 0.3f);
                    weatherGroundTint = Color.Lerp(Color.white, new Color(0.9f, 0.95f, 1f), currentWeatherIntensity * 0.5f);
                    break;
                    
                case WeatherType.Foggy:
                    // 雾天：降低对比度，偏灰白
                    ambientReduction = Mathf.Lerp(1f, 0.7f, currentWeatherIntensity);
                    Color fogTint = Color.Lerp(Color.white, Color.gray, currentWeatherIntensity * 0.3f);
                    weatherSkyTint = fogTint;
                    weatherGroundTint = fogTint;
                    break;
                    
                case WeatherType.Windy:
                    // 大风：轻微的环境光波动
                    float windAmbientFlicker = Mathf.PerlinNoise(Time.time * 2f, 1f) * 0.1f * currentWeatherIntensity;
                    ambientReduction = 1f + windAmbientFlicker;
                    break;
            }
            
            // 应用天气影响到环境光
            skyColor = Color.Lerp(skyColor, skyColor * weatherSkyTint, 0.6f) * ambientReduction;
            groundColor = Color.Lerp(groundColor, groundColor * weatherGroundTint, 0.6f) * ambientReduction;
            equatorColor = Color.Lerp(skyColor, groundColor, 0.5f); // 地平线颜色保持为天空和地面的混合
        }

        #endregion

        #region 真实光照计算方法

        /// <summary>
        /// 计算真实的太阳仰角 (考虑地球倾斜和纬度)
        /// </summary>
        private float CalculateRealisticSunElevation(float timeOfDay)
        {
            // 将时间转换为小时角
            float hourAngle = (timeOfDay - 0.5f) * 180f; // -90°到+90°
            
            // 模拟地球纬度影响 (假设中纬度地区)
            float latitude = 40f; // 可以做成参数
            
            // 模拟季节影响 (太阳赤纬角)
            float dayOfYear = (linkedEnvironmentState?.daysPassed ?? 0) % 365;
            float declination = 23.45f * Mathf.Sin(Mathf.Deg2Rad * (360f * (284f + dayOfYear) / 365f));
            
            // 计算太阳仰角 (简化的天文公式)
            float sunElevation = Mathf.Asin(
                Mathf.Sin(Mathf.Deg2Rad * declination) * Mathf.Sin(Mathf.Deg2Rad * latitude) +
                Mathf.Cos(Mathf.Deg2Rad * declination) * Mathf.Cos(Mathf.Deg2Rad * latitude) * Mathf.Cos(Mathf.Deg2Rad * hourAngle)
            ) * Mathf.Rad2Deg;
            
            return Mathf.Clamp(sunElevation, -90f, 90f);
        }
        
        /// <summary>
        /// 计算真实的太阳方位角
        /// </summary>
        private float CalculateRealisticSunAzimuth(float timeOfDay)
        {
            // 基础方位角计算 (简化版)
            float hourAngle = (timeOfDay - 0.5f) * 180f;
            
            // 上午太阳在东侧，下午在西侧
            float azimuth = 180f + hourAngle * 0.8f; // 调整系数让变化更平滑
            
            return azimuth;
        }
        
        /// <summary>
        /// 计算大气散射影响的光照强度
        /// </summary>
        private float CalculateAtmosphericIntensity(float sunElevation)
        {
            if (sunElevation < -10f) return 0f; // 太阳在地平线下，没有直射光
            
            // 大气质量系数 (Air Mass)
            float airMass = 1f / Mathf.Max(0.1f, Mathf.Sin(Mathf.Deg2Rad * Mathf.Max(5f, sunElevation)));
            
            // 大气透射率 (简化的Beer-Lambert定律)
            float atmosphericTransmission = Mathf.Exp(-0.1f * airMass);
            
            // 地平线附近的额外散射衰减
            if (sunElevation < 20f)
            {
                float horizonFactor = Mathf.Clamp01(sunElevation / 20f);
                atmosphericTransmission *= horizonFactor;
            }
            
            return atmosphericTransmission;
        }
        
        /// <summary>
        /// 计算大气散射颜色效果
        /// </summary>
        private Color CalculateAtmosphericColor(float sunElevation, float timeOfDay)
        {
            // 基础色温 (太阳光谱)
            Color baseColor = Color.white;
            
            // 大气散射效应
            if (sunElevation < 30f) // 低角度时散射更明显
            {
                float scatteringFactor = Mathf.Clamp01((30f - sunElevation) / 30f);
                
                // Rayleigh散射 (蓝光散射更多，留下红橙色)
                float redShift = 1f + scatteringFactor * 0.5f;
                float blueReduction = 1f - scatteringFactor * 0.6f;
                
                baseColor = new Color(
                    Mathf.Min(1f, baseColor.r * redShift),
                    Mathf.Lerp(baseColor.g, baseColor.g * 0.9f, scatteringFactor),
                    baseColor.b * blueReduction,
                    1f
                );
            }
            
            // 日出日落特殊颜色
            bool isSunriseOrSunset = (timeOfDay < 0.3f || timeOfDay > 0.7f) && sunElevation > -5f && sunElevation < 15f;
            if (isSunriseOrSunset)
            {
                Color goldenHour = new Color(1f, 0.7f, 0.3f, 1f);
                float goldenFactor = 1f - Mathf.Clamp01(sunElevation / 15f);
                baseColor = Color.Lerp(baseColor, goldenHour, goldenFactor * 0.7f);
            }
            
            return baseColor;
        }
        
        /// <summary>
        /// 动态调整阴影设置
        /// </summary>
        private void UpdateDynamicShadows(float sunElevation, float intensity)
        {
            if (sunLight == null) return;
            
            // 太阳角度太低时禁用阴影以提高性能
            if (sunElevation < 5f || intensity < 0.1f)
            {
                sunLight.shadows = LightShadows.None;
            }
            else
            {
                sunLight.shadows = LightShadows.Soft;
                
                // 根据太阳角度调整阴影强度
                float shadowStrength = Mathf.Clamp01((sunElevation - 5f) / 40f);
                sunLight.shadowStrength = shadowStrength * 0.8f;
                
                // 根据强度调整阴影距离
                if (shadowDistance > 0)
                {
                    float dynamicShadowDistance = shadowDistance * Mathf.Clamp01(intensity / (maxSunIntensity * 0.5f));
                    QualitySettings.shadowDistance = dynamicShadowDistance;
                }
            }
        }

        #endregion
    }
}