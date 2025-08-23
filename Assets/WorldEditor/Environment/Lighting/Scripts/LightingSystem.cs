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
        private EnvironmentState linkedEnvironmentState;
        private TimeSystem linkedTimeSystem;
        private SeasonSystem linkedSeasonSystem;

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
        public void Initialize(EnvironmentState environmentState = null, TimeSystem timeSystem = null, SeasonSystem seasonSystem = null)
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
            UpdateLighting();

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
        /// 更新太阳光照
        /// </summary>
        private void UpdateSunLighting(float timeOfDay)
        {
            if (sunLight == null) return;

            // 计算太阳强度
            float intensity = sunIntensityCurve.Evaluate(timeOfDay) * maxSunIntensity;

            // 计算太阳颜色
            Color color = sunColorGradient.Evaluate(timeOfDay);

            // 应用季节调整
            ApplySeasonalAdjustment(ref intensity, ref color);

            sunLight.intensity = intensity;
            sunLight.color = color;

            // 计算太阳角度（-90度到90度）
            float sunAngle = (timeOfDay - 0.5f) * 180f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0f);

            // 触发事件
            OnLightIntensityChanged?.Invoke(intensity);
            OnLightColorChanged?.Invoke(color);
        }

        /// <summary>
        /// 更新环境光照
        /// </summary>
        private void UpdateAmbientLighting(float timeOfDay)
        {
            // 环境光强度
            float ambientIntensity = ambientIntensityCurve.Evaluate(timeOfDay) * maxAmbientIntensity;
            RenderSettings.ambientIntensity = ambientIntensity;

            // 环境光颜色
            Color ambientColor = ambientColorGradient.Evaluate(timeOfDay);
            RenderSettings.ambientSkyColor = ambientColor;
            RenderSettings.ambientEquatorColor = ambientColor * 0.8f;
            RenderSettings.ambientGroundColor = ambientColor * 0.6f;
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
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            GUILayout.BeginArea(new Rect(530, 10, 200, 120));
            GUILayout.Box("光照系统调试");
            
            GUILayout.Label($"时间: {currentTimeOfDay:F2}");
            GUILayout.Label($"太阳强度: {(sunLight ? sunLight.intensity : 0):F2}");
            GUILayout.Label($"环境光强度: {RenderSettings.ambientIntensity:F2}");
            GUILayout.Label($"月亮强度: {(moonLight ? moonLight.intensity : 0):F2}");
            
            GUILayout.EndArea();
        }

        /// <summary>
        /// 应用季节性光照调整
        /// </summary>
        private void ApplySeasonalAdjustment(ref float intensity, ref Color color)
        {
            switch (currentSeason)
            {
                case SeasonType.Spring:
                    color = Color.Lerp(color, new Color(1f, 1f, 0.95f), 0.1f);
                    break;
                case SeasonType.Summer:
                    intensity *= 1.05f;
                    color = Color.Lerp(color, new Color(1f, 0.99f, 0.88f), 0.1f);
                    break;
                case SeasonType.Autumn:
                    color = Color.Lerp(color, new Color(1f, 0.85f, 0.65f), 0.15f);
                    break;
                case SeasonType.Winter:
                    intensity *= 0.9f;
                    color = Color.Lerp(color, new Color(0.95f, 0.95f, 1f), 0.1f);
                    break;
            }
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
        }

        #endregion
    }
}