using UnityEngine;
using UnityEngine.Rendering;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 日夜循环控制器 - 管理时间流逝、太阳月亮位置、光照变化
    /// </summary>
    public class DayNightCycleController : MonoBehaviour
    {
        [Header("时间设置")]
        [SerializeField] private bool enableDayNightCycle = true;
        [SerializeField] private float dayDuration = 1440f; // 24分钟 = 24小时
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private bool pauseTime = false;
        
        [Header("当前时间")]
        [SerializeField] private float currentTimeOfDay = 0.5f; // 0-1，0.5为正午
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int currentMonth = 6; // 六月
        [SerializeField] private int currentYear = 2024;
        
        [Header("太阳设置")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Transform sunTransform;
        [SerializeField] private Gradient sunColorGradient;
        [SerializeField] private AnimationCurve sunIntensityCurve;
        [SerializeField] private float maxSunIntensity = 3f;
        [SerializeField] private bool enableSunShadows = true;
        
        [Header("月亮设置")]
        [SerializeField] private Light moonLight;
        [SerializeField] private Transform moonTransform;
        [SerializeField] private Gradient moonColorGradient;
        [SerializeField] private AnimationCurve moonIntensityCurve;
        [SerializeField] private float maxMoonIntensity = 0.5f;
        [SerializeField] private bool enableMoonShadows = false;
        
        [Header("天空盒设置")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Gradient skyColorGradient;
        [SerializeField] private Gradient horizonColorGradient;
        [SerializeField] private AnimationCurve skyboxExposure;
        
        [Header("环境光设置")]
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private AnimationCurve ambientIntensityCurve;
        [SerializeField] private bool useGradientAmbient = true;
        
        [Header("雾效设置")]
        [SerializeField] private bool enableTimeBasedFog = true;
        [SerializeField] private Gradient fogColorGradient;
        [SerializeField] private AnimationCurve fogDensityCurve;
        
        [Header("地理设置")]
        [SerializeField] private float latitude = 45f; // 纬度（影响太阳轨迹）
        [SerializeField] private float longitude = 0f;  // 经度（时区）
        [SerializeField] private bool enableSeasonalVariation = true;
        
        [Header("性能设置")]
        [SerializeField] private float updateFrequency = 0.1f;
        [SerializeField] private bool enableLODOptimization = true;
        [SerializeField] private float maxUpdateDistance = 500f;
        
        // 事件
        public System.Action<float> OnTimeChanged;
        public System.Action<TimeOfDay> OnTimeOfDayChanged;
        public System.Action OnDayChanged;
        public System.Action<Season> OnSeasonChanged;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private Camera mainCamera;
        private Light mainLight;
        
        // 时间相关
        private float lastUpdateTime;
        private TimeOfDay currentTimeOfDayEnum = TimeOfDay.Noon;
        private TimeOfDay previousTimeOfDayEnum = TimeOfDay.Noon;
        
        // 太阳月亮位置计算
        private Vector3 sunDirection;
        private Vector3 moonDirection;
        private float seasonalAngleOffset;
        
        // 缓存的梯度采样
        private Color currentSunColor;
        private Color currentMoonColor;
        private Color currentSkyColor;
        private Color currentAmbientColor;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            SetupComponents();
            InitializeGradients();
            CalculateSeasonalOffset();
        }
        
        void SetupComponents()
        {
            mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            
            // 设置太阳光
            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("Sun Light");
                sunObj.transform.SetParent(transform);
                sunLight = sunObj.AddComponent<Light>();
                sunTransform = sunObj.transform;
                
                sunLight.type = LightType.Directional;
                sunLight.shadows = enableSunShadows ? LightShadows.Soft : LightShadows.None;
                sunLight.cullingMask = ~0; // 照亮所有层
            }
            
            // 设置月亮光
            if (moonLight == null)
            {
                GameObject moonObj = new GameObject("Moon Light");
                moonObj.transform.SetParent(transform);
                moonLight = moonObj.AddComponent<Light>();
                moonTransform = moonObj.transform;
                
                moonLight.type = LightType.Directional;
                moonLight.shadows = enableMoonShadows ? LightShadows.Soft : LightShadows.None;
                moonLight.cullingMask = ~0;
                moonLight.intensity = 0f;
            }
            
            // 查找主光源
            mainLight = RenderSettings.sun ?? Object.FindFirstObjectByType<Light>();
            
            // 设置天空盒材质
            if (skyboxMaterial == null)
            {
                skyboxMaterial = RenderSettings.skybox;
            }
        }
        
        void InitializeGradients()
        {
            // 初始化默认梯度
            if (sunColorGradient == null)
            {
                sunColorGradient = CreateDefaultSunGradient();
            }
            
            if (moonColorGradient == null)
            {
                moonColorGradient = CreateDefaultMoonGradient();
            }
            
            if (skyColorGradient == null)
            {
                skyColorGradient = CreateDefaultSkyGradient();
            }
            
            if (ambientColorGradient == null)
            {
                ambientColorGradient = CreateDefaultAmbientGradient();
            }
            
            if (fogColorGradient == null)
            {
                fogColorGradient = CreateDefaultFogGradient();
            }
            
            // 初始化动画曲线
            if (sunIntensityCurve == null)
            {
                sunIntensityCurve = CreateDefaultSunIntensityCurve();
            }
            
            if (moonIntensityCurve == null)
            {
                moonIntensityCurve = CreateDefaultMoonIntensityCurve();
            }
            
            if (ambientIntensityCurve == null)
            {
                ambientIntensityCurve = CreateDefaultAmbientIntensityCurve();
            }
            
            if (skyboxExposure == null)
            {
                skyboxExposure = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1.3f);
            }
            
            if (fogDensityCurve == null)
            {
                fogDensityCurve = AnimationCurve.EaseInOut(0f, 0.02f, 1f, 0.005f);
            }
        }
        
        /// <summary>
        /// 更新日夜循环
        /// </summary>
        public void UpdateCycle(float deltaTime)
        {
            if (!enableDayNightCycle || pauseTime) return;
            
            if (Time.time - lastUpdateTime < updateFrequency)
                return;
            
            lastUpdateTime = Time.time;
            
            // 更新时间
            UpdateTime(deltaTime);
            
            // 计算太阳月亮位置
            CalculateCelestialPositions();
            
            // 更新光照
            UpdateLighting();
            
            // 更新环境设置
            UpdateEnvironmentSettings();
            
            // 检查时段变化
            CheckTimeOfDayChanges();
            
            // 性能优化检查
            if (enableLODOptimization && !ShouldUpdateLighting())
                return;
        }
        
        void UpdateTime(float deltaTime)
        {
            float timeIncrement = (deltaTime * timeScale) / dayDuration;
            currentTimeOfDay += timeIncrement;
            
            // 处理日期变化
            if (currentTimeOfDay >= 1f)
            {
                currentTimeOfDay -= 1f;
                currentDay++;
                OnDayChanged?.Invoke();
                
                // 检查月份变化
                CheckMonthChange();
            }
            else if (currentTimeOfDay < 0f)
            {
                currentTimeOfDay += 1f;
                currentDay--;
            }
            
            OnTimeChanged?.Invoke(currentTimeOfDay);
        }
        
        void CheckMonthChange()
        {
            int daysInMonth = GetDaysInMonth(currentMonth, currentYear);
            
            if (currentDay > daysInMonth)
            {
                currentDay = 1;
                currentMonth++;
                
                if (currentMonth > 12)
                {
                    currentMonth = 1;
                    currentYear++;
                }
                
                // 重新计算季节偏移
                CalculateSeasonalOffset();
            }
        }
        
        void CalculateSeasonalOffset()
        {
            if (!enableSeasonalVariation) return;
            
            // 计算一年中的天数
            int dayOfYear = GetDayOfYear(currentDay, currentMonth, currentYear);
            float yearProgress = (float)dayOfYear / (IsLeapYear(currentYear) ? 366f : 365f);
            
            // 计算季节性角度偏移（地球轴倾斜）
            seasonalAngleOffset = Mathf.Sin(yearProgress * 2f * Mathf.PI) * 23.5f; // 地球轴倾斜角
        }
        
        void CalculateCelestialPositions()
        {
            // 基于经度调整时间（时区效应）
            float adjustedTimeOfDay = currentTimeOfDay + (longitude / 360f);
            adjustedTimeOfDay = adjustedTimeOfDay - Mathf.Floor(adjustedTimeOfDay); // 保持在0-1范围内
            
            // 计算太阳位置
            float sunAngle = (adjustedTimeOfDay - 0.25f) * 2f * Mathf.PI; // -0.25调整为日出在6点
            float sunElevation = Mathf.Sin(sunAngle) * 90f + seasonalAngleOffset;
            float sunAzimuth = Mathf.Cos(sunAngle) * 180f + longitude; // 经度影响方位角
            
            // 考虑纬度影响
            sunElevation = AdjustForLatitude(sunElevation, latitude);
            
            // 转换为Unity坐标系
            sunDirection = SphericalToCartesian(sunElevation, sunAzimuth);
            
            if (sunTransform != null)
            {
                sunTransform.rotation = Quaternion.LookRotation(sunDirection);
            }
            
            // 计算月亮位置（与太阳相对180度）
            float moonAngle = sunAngle + Mathf.PI;
            float moonElevation = Mathf.Sin(moonAngle) * 90f + seasonalAngleOffset;
            float moonAzimuth = Mathf.Cos(moonAngle) * 180f;
            
            moonElevation = AdjustForLatitude(moonElevation, latitude);
            moonDirection = SphericalToCartesian(moonElevation, moonAzimuth);
            
            if (moonTransform != null)
            {
                moonTransform.rotation = Quaternion.LookRotation(moonDirection);
            }
        }
        
        Vector3 SphericalToCartesian(float elevation, float azimuth)
        {
            float elevRad = elevation * Mathf.Deg2Rad;
            float azimRad = azimuth * Mathf.Deg2Rad;
            
            return new Vector3(
                Mathf.Sin(azimRad) * Mathf.Cos(elevRad),
                Mathf.Sin(elevRad),
                Mathf.Cos(azimRad) * Mathf.Cos(elevRad)
            );
        }
        
        float AdjustForLatitude(float elevation, float lat)
        {
            // 简化的纬度调整
            float latitudeEffect = Mathf.Cos(lat * Mathf.Deg2Rad);
            return elevation * latitudeEffect;
        }
        
        void UpdateLighting()
        {
            // 获取太阳高度角
            float sunElevation = Mathf.Asin(sunDirection.y) * Mathf.Rad2Deg;
            float moonElevation = Mathf.Asin(moonDirection.y) * Mathf.Rad2Deg;
            
            // 更新太阳光
            UpdateSunLight(sunElevation);
            
            // 更新月亮光
            UpdateMoonLight(moonElevation);
            
            // 缓存颜色采样
            CacheColorSamples();
        }
        
        void UpdateSunLight(float sunElevation)
        {
            if (sunLight == null) return;
            
            bool sunVisible = sunElevation > -5f; // 太阳在地平线上5度以内仍有光
            sunLight.enabled = sunVisible;
            
            if (sunVisible)
            {
                // 计算太阳强度
                float sunIntensityFactor = Mathf.Clamp01((sunElevation + 5f) / 95f);
                float sunIntensity = sunIntensityCurve.Evaluate(sunIntensityFactor) * maxSunIntensity;
                
                // 应用环境系统的强度修改
                if (environmentSystem != null)
                {
                    var envState = environmentSystem.GetCurrentEnvironmentState();
                    sunIntensity *= envState.sunIntensity;
                    currentSunColor = Color.Lerp(sunColorGradient.Evaluate(currentTimeOfDay), envState.sunColor, 0.3f);
                }
                else
                {
                    currentSunColor = sunColorGradient.Evaluate(currentTimeOfDay);
                }
                
                sunLight.intensity = sunIntensity;
                sunLight.color = currentSunColor;
                
                // 设置阴影
                sunLight.shadows = enableSunShadows && sunIntensity > 0.1f ? LightShadows.Soft : LightShadows.None;
            }
        }
        
        void UpdateMoonLight(float moonElevation)
        {
            if (moonLight == null) return;
            
            bool moonVisible = moonElevation > -5f;
            moonLight.enabled = moonVisible;
            
            if (moonVisible)
            {
                float moonIntensityFactor = Mathf.Clamp01((moonElevation + 5f) / 95f);
                float moonIntensity = moonIntensityCurve.Evaluate(moonIntensityFactor) * maxMoonIntensity;
                
                // 月相效果（简化）
                float moonPhase = Mathf.Sin(currentDay * 0.2f) * 0.5f + 0.5f; // 简化的月相计算
                moonIntensity *= moonPhase;
                
                if (environmentSystem != null)
                {
                    var envState = environmentSystem.GetCurrentEnvironmentState();
                    moonIntensity *= envState.moonIntensity;
                    currentMoonColor = Color.Lerp(moonColorGradient.Evaluate(currentTimeOfDay), envState.moonColor, 0.3f);
                }
                else
                {
                    currentMoonColor = moonColorGradient.Evaluate(currentTimeOfDay);
                }
                
                moonLight.intensity = moonIntensity;
                moonLight.color = currentMoonColor;
                
                moonLight.shadows = enableMoonShadows && moonIntensity > 0.05f ? LightShadows.Soft : LightShadows.None;
            }
        }
        
        void CacheColorSamples()
        {
            currentSkyColor = skyColorGradient.Evaluate(currentTimeOfDay);
            currentAmbientColor = ambientColorGradient.Evaluate(currentTimeOfDay);
        }
        
        void UpdateEnvironmentSettings()
        {
            // 更新环境光
            UpdateAmbientLighting();
            
            // 更新天空盒
            UpdateSkybox();
            
            // 更新雾效
            if (enableTimeBasedFog)
            {
                UpdateTimeBasedFog();
            }
            
            // 更新后处理
            UpdatePostProcessing();
        }
        
        void UpdateAmbientLighting()
        {
            if (useGradientAmbient)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = currentAmbientColor;
                RenderSettings.ambientEquatorColor = currentAmbientColor * 0.7f;
                RenderSettings.ambientGroundColor = currentAmbientColor * 0.3f;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = currentAmbientColor;
            }
            
            float ambientIntensity = ambientIntensityCurve.Evaluate(currentTimeOfDay);
            RenderSettings.ambientIntensity = ambientIntensity;
        }
        
        void UpdateSkybox()
        {
            if (skyboxMaterial != null)
            {
                // 更新天空盒材质属性
                if (skyboxMaterial.HasProperty("_Tint"))
                {
                    skyboxMaterial.SetColor("_Tint", currentSkyColor);
                }
                
                if (skyboxMaterial.HasProperty("_Exposure"))
                {
                    float exposure = skyboxExposure.Evaluate(currentTimeOfDay);
                    skyboxMaterial.SetFloat("_Exposure", exposure);
                }
                
                // 设置太阳方向
                if (skyboxMaterial.HasProperty("_SunDirection"))
                {
                    skyboxMaterial.SetVector("_SunDirection", sunDirection);
                }
                
                RenderSettings.skybox = skyboxMaterial;
            }
        }
        
        void UpdateTimeBasedFog()
        {
            Color fogColor = fogColorGradient.Evaluate(currentTimeOfDay);
            float fogDensity = fogDensityCurve.Evaluate(currentTimeOfDay);
            
            RenderSettings.fog = fogDensity > 0.001f;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }
        
        void UpdatePostProcessing()
        {
            if (mainLight != null)
            {
                // 基于时间调整主光源属性
                float timeBasedIntensity = Mathf.Lerp(0.1f, 1.5f, currentTimeOfDay);
                mainLight.intensity = timeBasedIntensity;
            }
        }
        
        void CheckTimeOfDayChanges()
        {
            TimeOfDay newTimeOfDay = GetTimeOfDayEnum(currentTimeOfDay);
            
            if (newTimeOfDay != currentTimeOfDayEnum)
            {
                previousTimeOfDayEnum = currentTimeOfDayEnum;
                currentTimeOfDayEnum = newTimeOfDay;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDayEnum);
            }
        }
        
        TimeOfDay GetTimeOfDayEnum(float normalizedTime)
        {
            float hours = normalizedTime * 24f;
            
            if (hours >= 5f && hours < 7f)
                return TimeOfDay.Dawn;
            else if (hours >= 7f && hours < 11f)
                return TimeOfDay.Morning;
            else if (hours >= 11f && hours < 14f)
                return TimeOfDay.Noon;
            else if (hours >= 14f && hours < 18f)
                return TimeOfDay.Afternoon;
            else if (hours >= 18f && hours < 20f)
                return TimeOfDay.Dusk;
            else
                return TimeOfDay.Night;
        }
        
        bool ShouldUpdateLighting()
        {
            if (mainCamera == null) return true;
            
            float distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
            return distanceToCamera <= maxUpdateDistance;
        }
        
        // 创建默认梯度的辅助方法
        Gradient CreateDefaultSunGradient()
        {
            Gradient gradient = new Gradient();
            var colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f);    // 夜晚
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f);    // 日出
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.5f);    // 正午
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0.75f);    // 日落
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f);     // 夜晚
            gradient.colorKeys = colorKeys;
            return gradient;
        }
        
        Gradient CreateDefaultMoonGradient()
        {
            Gradient gradient = new Gradient();
            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f);
            gradient.colorKeys = colorKeys;
            return gradient;
        }
        
        Gradient CreateDefaultSkyGradient()
        {
            Gradient gradient = new Gradient();
            var colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(0.5f, 0.8f, 1f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.7f, 0.5f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);
            gradient.colorKeys = colorKeys;
            return gradient;
        }
        
        Gradient CreateDefaultAmbientGradient()
        {
            Gradient gradient = new Gradient();
            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.8f, 1f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f);
            gradient.colorKeys = colorKeys;
            return gradient;
        }
        
        Gradient CreateDefaultFogGradient()
        {
            Gradient gradient = new Gradient();
            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.8f, 1f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.3f, 0.3f, 0.5f), 1f);
            gradient.colorKeys = colorKeys;
            return gradient;
        }
        
        AnimationCurve CreateDefaultSunIntensityCurve()
        {
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);      // 夜晚
            curve.AddKey(0.25f, 0.8f); // 日出
            curve.AddKey(0.5f, 1f);    // 正午
            curve.AddKey(0.75f, 0.8f); // 日落
            curve.AddKey(1f, 0f);      // 夜晚
            return curve;
        }
        
        AnimationCurve CreateDefaultMoonIntensityCurve()
        {
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);     // 夜晚
            curve.AddKey(0.25f, 0f);  // 日出
            curve.AddKey(0.5f, 0f);   // 正午
            curve.AddKey(0.75f, 0f);  // 日落
            curve.AddKey(1f, 1f);     // 夜晚
            return curve;
        }
        
        AnimationCurve CreateDefaultAmbientIntensityCurve()
        {
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0.3f);   // 夜晚
            curve.AddKey(0.25f, 0.7f); // 日出
            curve.AddKey(0.5f, 1f);   // 正午
            curve.AddKey(0.75f, 0.7f); // 日落
            curve.AddKey(1f, 0.3f);   // 夜晚
            return curve;
        }
        
        // 日期计算辅助方法
        int GetDaysInMonth(int month, int year)
        {
            switch (month)
            {
                case 2: return IsLeapYear(year) ? 29 : 28;
                case 4: case 6: case 9: case 11: return 30;
                default: return 31;
            }
        }
        
        bool IsLeapYear(int year)
        {
            return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
        }
        
        int GetDayOfYear(int day, int month, int year)
        {
            int dayOfYear = day;
            for (int m = 1; m < month; m++)
            {
                dayOfYear += GetDaysInMonth(m, year);
            }
            return dayOfYear;
        }
        
        /// <summary>
        /// 设置时间
        /// </summary>
        public void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Dawn:
                    currentTimeOfDay = 0.25f;
                    break;
                case TimeOfDay.Morning:
                    currentTimeOfDay = 0.33f;
                    break;
                case TimeOfDay.Noon:
                    currentTimeOfDay = 0.5f;
                    break;
                case TimeOfDay.Afternoon:
                    currentTimeOfDay = 0.67f;
                    break;
                case TimeOfDay.Dusk:
                    currentTimeOfDay = 0.75f;
                    break;
                case TimeOfDay.Night:
                    currentTimeOfDay = 0f;
                    break;
            }
        }
        
        /// <summary>
        /// 设置具体时间
        /// </summary>
        public void SetTime(float normalizedTime)
        {
            currentTimeOfDay = Mathf.Clamp01(normalizedTime);
        }
        
        /// <summary>
        /// 获取当前时间段
        /// </summary>
        public TimeOfDay GetCurrentTimeOfDay()
        {
            return currentTimeOfDayEnum;
        }
        
        /// <summary>
        /// 获取当前时间字符串
        /// </summary>
        public string GetTimeString()
        {
            float hours = currentTimeOfDay * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            
            return $"{hour:00}:{minute:00}";
        }
        
        /// <summary>
        /// 获取当前日期字符串
        /// </summary>
        public string GetDateString()
        {
            return $"{currentYear}/{currentMonth:00}/{currentDay:00}";
        }
    }
}