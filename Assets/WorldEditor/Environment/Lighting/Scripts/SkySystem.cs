using UnityEngine;
using System;
using System.Collections;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 天空系统 - 管理程序化天空渲染和大气散射效果
    /// 
    /// 核心功能：
    /// - 程序化天空盒生成和渲染
    /// - 基于物理的大气散射计算
    /// - 动态云层生成和动画
    /// - 星空和天体渲染
    /// - 与光照系统的深度集成
    /// </summary>
    public class SkySystem : MonoBehaviour
    {
        #region 天空配置参数

        [Header("大气散射配置")]
        [Tooltip("大气厚度 (影响散射强度)")]
        [Range(0f, 5f)]
        public float atmosphereThickness = 1f;
        
        [Tooltip("瑞利散射系数")]
        [Range(0f, 8f)]
        public float rayleighCoefficient = 2f;
        
        [Tooltip("米散射系数")]
        [Range(0f, 5f)]
        public float mieCoefficient = 1f;
        
        [Tooltip("散射指向性")]
        [Range(0.75f, 0.999f)]
        public float mieDirectionalG = 0.85f;

        [Header("天空颜色配置")]
        [Tooltip("天空色调")]
        public Color skyTint = Color.white;
        
        [Tooltip("地平线颜色梯度")]
        public Gradient horizonColorGradient;
        
        [Tooltip("天顶颜色梯度")]
        public Gradient zenithColorGradient;

        #endregion

        #region 云层配置

        [Header("云层系统配置")]
        [Tooltip("是否启用云层渲染")]
        public bool enableClouds = true;
        
        [Tooltip("云层覆盖度 (0=无云, 1=完全覆盖)")]
        [Range(0f, 1f)]
        public float cloudCoverage = 0.3f;
        
        [Tooltip("云层密度")]
        [Range(0f, 1f)]
        public float cloudDensity = 0.5f;
        
        [Tooltip("云层高度")]
        [Range(1000f, 10000f)]
        public float cloudHeight = 3000f;
        
        [Tooltip("云层移动速度")]
        [Range(0f, 10f)]
        public float cloudSpeed = 1f;

        #endregion

        #region 星空配置

        [Header("星空配置")]
        [Tooltip("是否启用星空渲染")]
        public bool enableStars = true;
        
        [Tooltip("星空亮度")]
        [Range(0f, 2f)]
        public float starIntensity = 1f;
        
        [Tooltip("星空密度")]
        [Range(0f, 1f)]
        public float starDensity = 0.5f;
        
        [Tooltip("星空闪烁强度")]
        [Range(0f, 1f)]
        public float starTwinkle = 0.5f;

        #endregion

        #region 材质和着色器引用

        [Header("渲染材质")]
        [Tooltip("天空材质")]
        public Material skyMaterial;
        
        [Tooltip("云层材质")]
        public Material cloudMaterial;
        
        [Tooltip("星空材质")]
        public Material starMaterial;

        #endregion

        #region 运行时状态

        private bool isInitialized = false;
        private bool isActive = false;
        private float currentTimeOfDay = 0.5f;
        private WorldEditor.Environment.WeatherType currentWeather = WorldEditor.Environment.WeatherType.Clear;
        private float currentWeatherIntensity = 1f;
        private Material skyMaterialInstance;
        private Material cloudMaterialInstance;
        private Material starMaterialInstance;
        private EnvironmentState linkedEnvironmentState;
        private TimeSystem linkedTimeSystem;
        private WeatherSystem linkedWeatherSystem;

        #endregion

        #region 着色器属性ID

        private static readonly int AtmosphereThickness = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int RayleighCoefficient = Shader.PropertyToID("_RayleighCoefficient");
        private static readonly int MieCoefficient = Shader.PropertyToID("_MieCoefficient");
        private static readonly int MieDirectionalG = Shader.PropertyToID("_MieDirectionalG");
        private static readonly int SkyTint = Shader.PropertyToID("_SkyTint");
        private static readonly int SunPosition = Shader.PropertyToID("_SunPosition");
        private static readonly int CloudCoverage = Shader.PropertyToID("_CloudCoverage");
        private static readonly int CloudDensity = Shader.PropertyToID("_CloudDensity");
        private static readonly int CloudOffset = Shader.PropertyToID("_CloudOffset");
        private static readonly int StarIntensity = Shader.PropertyToID("_StarIntensity");
        private static readonly int StarTwinkle = Shader.PropertyToID("_StarTwinkle");
        
        // 天气相关着色器属性
        private static readonly int WeatherInfluence = Shader.PropertyToID("_WeatherInfluence");
        private static readonly int WeatherType = Shader.PropertyToID("_WeatherType");
        private static readonly int FogDensity = Shader.PropertyToID("_FogDensity");
        private static readonly int FogColor = Shader.PropertyToID("_FogColor");

        #endregion

        #region 事件系统

        /// <summary>天空状态变化事件</summary>
        public event Action<float> OnSkyStateChanged;

        #endregion

        #region 公共属性

        /// <summary>天空系统是否激活</summary>
        public bool IsActive => isActive && isInitialized;
        
        /// <summary>当前时间（只读）</summary>
        public float CurrentTimeOfDay => currentTimeOfDay;
        
        /// <summary>当前天气（只读）</summary>
        public WorldEditor.Environment.WeatherType CurrentWeather => currentWeather;
        
        /// <summary>是否已初始化（只读）</summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>当前云层覆盖度</summary>
        public float CurrentCloudCoverage => cloudCoverage;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化天空系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null, TimeSystem timeSystem = null, WeatherSystem weatherSystem = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[SkySystem] 天空系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[SkySystem] 开始初始化天空系统...");

            // 链接系统引用
            linkedEnvironmentState = environmentState;
            linkedTimeSystem = timeSystem;
            linkedWeatherSystem = weatherSystem;

            // 初始化材质实例
            InitializeMaterials();

            // 初始化默认梯度
            InitializeDefaultGradients();

            // 设置默认天空盒
            SetupDefaultSkybox();

            // 订阅系统事件
            SubscribeToSystemEvents();

            // 同步环境状态
            if (linkedEnvironmentState != null)
            {
                SyncFromEnvironmentState();
            }

            isActive = true;
            isInitialized = true;

            Debug.Log("[SkySystem] 天空系统初始化完成");
        }

        /// <summary>
        /// 初始化材质实例
        /// </summary>
        private void InitializeMaterials()
        {
            // 创建天空材质实例
            if (skyMaterial != null)
            {
                skyMaterialInstance = new Material(skyMaterial);
            }
            else
            {
                // 如果没有指定材质，创建程序化天空材质
                CreateProceduralSkyMaterial();
            }

            // 创建云层材质实例
            if (cloudMaterial != null && enableClouds)
            {
                cloudMaterialInstance = new Material(cloudMaterial);
            }
            else if (enableClouds)
            {
                // 创建默认云层材质
                CreateProceduralCloudMaterial();
            }

            // 创建星空材质实例
            if (starMaterial != null && enableStars)
            {
                starMaterialInstance = new Material(starMaterial);
            }
            else if (enableStars)
            {
                // 创建默认星空材质
                CreateProceduralStarMaterial();
            }
        }

        /// <summary>
        /// 创建《原神》风格的专业天空材质
        /// </summary>
        private void CreateProceduralSkyMaterial()
        {
            // 查找自定义的《原神》风格天空着色器
            Shader physicallyBasedSkyShader = Shader.Find("WorldEditor/PhysicallyBasedSky");
            
            if (physicallyBasedSkyShader != null)
            {
                // 创建基于物理的天空材质
                skyMaterialInstance = new Material(physicallyBasedSkyShader);
                skyMaterialInstance.name = "Physically Based Sky Material (Generated)";
                
                // 启用关键字
                skyMaterialInstance.EnableKeyword("STARS_ENABLED");
                
                // 设置基本的天空属性
                SetupPhysicallyBasedSkyProperties();
                
                // 将生成的材质赋值给原始字段，这样Inspector中也会显示
                skyMaterial = skyMaterialInstance;
                
                // 应用到天空盒渲染设置
                RenderSettings.skybox = skyMaterialInstance;
                
                // 强制刷新环境光照
                DynamicGI.UpdateEnvironment();
                
                Debug.Log("[SkySystem] 已创建《原神》风格天空材质并应用到渲染设置");
            }
            else
            {
                Debug.LogError("[SkySystem] 无法找到 WorldEditor/PhysicallyBasedSky 着色器，请检查着色器是否存在");
                
                // 备选方案：使用Unity内置的Procedural着色器
                Shader proceduralSkyShader = Shader.Find("Skybox/Procedural");
                if (proceduralSkyShader != null)
                {
                    skyMaterialInstance = new Material(proceduralSkyShader);
                    skyMaterialInstance.name = "Fallback Procedural Sky (Generated)";
                    skyMaterial = skyMaterialInstance;
                    RenderSettings.skybox = skyMaterialInstance;
                    Debug.Log("[SkySystem] 使用内置Procedural天空作为备选方案");
                }
            }
        }
        
        /// <summary>
        /// 设置基于物理的天空属性
        /// </summary>
        private void SetupPhysicallyBasedSkyProperties()
        {
            if (skyMaterialInstance == null) return;
            
            // 大气参数 - 基于真实地球数据
            skyMaterialInstance.SetFloat("_PlanetRadius", 6371000f);           // 地球半径（米）
            skyMaterialInstance.SetFloat("_AtmosphereHeight", 80000f);         // 大气层高度（米）
            skyMaterialInstance.SetFloat("_RayleighScaleHeight", 8000f);       // 瑞利散射标高
            skyMaterialInstance.SetFloat("_MieScaleHeight", 1200f);            // 米氏散射标高
            
            // 散射系数 - 基于物理测量值
            skyMaterialInstance.SetVector("_RayleighScattering", new Vector4(0.0000058f, 0.0000135f, 0.0000331f, 0f));
            skyMaterialInstance.SetFloat("_MieScattering", 0.00002f);
            skyMaterialInstance.SetFloat("_MieAbsorption", 0.0000044f);
            skyMaterialInstance.SetFloat("_MieG", 0.8f);
            
            // 臭氧层参数
            skyMaterialInstance.SetVector("_OzoneAbsorption", new Vector4(0.00000065f, 0.000001881f, 0.000000085f, 0f));
            skyMaterialInstance.SetFloat("_OzoneHeight", 25000f);
            skyMaterialInstance.SetFloat("_OzoneThickness", 15000f);
            
            // 天体属性
            skyMaterialInstance.SetFloat("_SunIntensity", 20f);                // 太阳强度
            skyMaterialInstance.SetColor("_SunColor", Color.white);
            skyMaterialInstance.SetFloat("_SunSize", 0.0045f);                 // 真实太阳角度大小
            skyMaterialInstance.SetFloat("_MoonIntensity", 0.4f);
            skyMaterialInstance.SetColor("_MoonColor", Color.white);
            skyMaterialInstance.SetFloat("_MoonSize", 0.0087f);                // 真实月亮角度大小
            
            // 夜空参数
            skyMaterialInstance.SetFloat("_StarIntensity", starIntensity);
            skyMaterialInstance.SetFloat("_MilkyWayIntensity", 1f);
            
            // 海洋和地平线参数
            skyMaterialInstance.SetColor("_OceanColor", new Color(0.1f, 0.3f, 0.6f, 1f));
            skyMaterialInstance.SetFloat("_HorizonSmoothness", 0.15f);
            skyMaterialInstance.SetFloat("_WaterReflection", 0.3f);
            
            // 曝光
            skyMaterialInstance.SetFloat("_Exposure", 1f);
            
            // 设置太阳和月亮方向
            UpdateCelestialDirections();
        }
        
        /// <summary>
        /// 更新天体方向（太阳和月亮）- 专业级实现
        /// </summary>
        private void UpdateCelestialDirections()
        {
            if (skyMaterialInstance == null) return;
            
            // 使用专业级太阳位置计算
            Vector3 sunDirection = GetSunPosition();
            
            // 专业级月亮位置计算 - 确保月亮与太阳永不同时出现
            Vector3 moonDirection = GetMoonPosition();
            
            skyMaterialInstance.SetVector("_SunDirection", sunDirection);
            skyMaterialInstance.SetVector("_MoonDirection", moonDirection);
            
            // 输出调试信息（每秒一次）- 增强版本
            if (Time.time % 1f < 0.1f)
            {
                Debug.Log($"[SkySystem] 当前着色器: {skyMaterialInstance.shader.name}");
                Debug.Log($"[SkySystem] 时间: {currentTimeOfDay:F3}, 太阳高度: {sunDirection.y:F2}, 月亮高度: {moonDirection.y:F2}");
                Debug.Log($"[SkySystem] 太阳方向: {sunDirection}");
            }
            
            // 确保有主方向光（太阳光）
            Light mainLight = GetOrCreateSunLight();
            if (mainLight != null)
            {
                // 更新太阳光方向
                mainLight.transform.rotation = Quaternion.LookRotation(-sunDirection);
                
                // 根据太阳高度调整光照强度和颜色
                UpdateSunLightProperties(mainLight, sunDirection);
                
                // 调试信息 - 每5秒输出一次
                if (Time.time % 5f < 0.1f)
                {
                    Debug.Log($"[SkySystem] 时间: {currentTimeOfDay:F2}, 太阳高度: {sunDirection.y:F2}, 光照强度: {mainLight.intensity:F2}");
                }
            }
        }
        
        /// <summary>
        /// 获取或创建太阳光（主方向光）
        /// </summary>
        private Light GetOrCreateSunLight()
        {
            Light mainLight = RenderSettings.sun;
            
            // 如果没有设置主方向光，查找场景中的方向光
            if (mainLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        mainLight = light;
                        RenderSettings.sun = mainLight;
                        Debug.Log($"[SkySystem] 找到方向光并设为太阳光: {light.name}");
                        break;
                    }
                }
            }
            
            // 如果还是没有，自动创建一个
            if (mainLight == null)
            {
                GameObject sunLightGO = new GameObject("Sun Light (Auto Generated)");
                mainLight = sunLightGO.AddComponent<Light>();
                mainLight.type = LightType.Directional;
                mainLight.color = new Color(1f, 0.95f, 0.8f); // 温暖的阳光色
                mainLight.intensity = 1.5f;
                mainLight.shadows = LightShadows.Soft;
                
                // 设置为主太阳光
                RenderSettings.sun = mainLight;
                
                Debug.Log("[SkySystem] 自动创建了太阳光（方向光）");
            }
            
            return mainLight;
        }
        
        /// <summary>
        /// 根据太阳位置更新光照属性 - 基于真实世界数据的专业级光照系统
        /// </summary>
        private void UpdateSunLightProperties(Light sunLight, Vector3 sunDirection)
        {
            UpdateRealisticLighting(sunLight, sunDirection);
        }
        
        /// <summary>
        /// 基于真实世界数据的光照更新
        /// 使用真实的勒克斯值和色温数据，消除"模拟感"
        /// </summary>
        private void UpdateRealisticLighting(Light sunLight, Vector3 sunDirection)
        {
            // 计算太阳高度角（-1到1，-1是地平线下，0是地平线，1是天顶）
            float sunElevation = sunDirection.y;
            
            // 基于真实世界的光照强度计算
            float realWorldLux = GetRealWorldLuxValue(sunElevation);
            float unityIntensity = ConvertLuxToUnityIntensity(realWorldLux);
            
            // 应用光照强度
            sunLight.intensity = unityIntensity;
            
            // 基于真实色温的颜色计算
            float colorTemperature = GetRealWorldColorTemperature(sunElevation);
            Color realisticSunColor = ColorTemperatureToRGB(colorTemperature);
            
            // 应用光照颜色
            sunLight.color = realisticSunColor;
            
            // 更新环境光照以匹配真实世界条件
            UpdateAmbientLighting(sunElevation, realisticSunColor);
            
            // 调试输出（仅在开发环境）
            if (Application.isEditor && Time.time % 3f < 0.1f)
            {
                Debug.Log($"[真实光照] 太阳高度: {sunElevation:F2}, 勒克斯: {realWorldLux:F0}, Unity强度: {unityIntensity:F2}, 色温: {colorTemperature:F0}K");
            }
        }
        
        /// <summary>
        /// 获取基于真实世界的勒克斯值
        /// 数据来源：国际照明委员会(CIE)标准和气象学数据
        /// </summary>
        private float GetRealWorldLuxValue(float sunElevation)
        {
            if (sunElevation <= -0.1f) // 夜晚（太阳在地平线下6度以上）
            {
                return 0.0002f; // 满月夜晚：0.2 lux
            }
            else if (sunElevation <= 0f) // 天文薄暮/黎明
            {
                return Mathf.Lerp(0.0002f, 1f, (sunElevation + 0.1f) / 0.1f); // 0.2-1 lux
            }
            else if (sunElevation <= 0.1f) // 民用薄暮/黎明
            {
                return Mathf.Lerp(1f, 400f, sunElevation / 0.1f); // 1-400 lux
            }
            else if (sunElevation <= 0.3f) // 日出后/日落前
            {
                return Mathf.Lerp(400f, 10000f, (sunElevation - 0.1f) / 0.2f); // 400-10,000 lux
            }
            else if (sunElevation <= 0.7f) // 上午/下午
            {
                return Mathf.Lerp(10000f, 100000f, (sunElevation - 0.3f) / 0.4f); // 10,000-100,000 lux
            }
            else // 正午附近（太阳高度角>40度）
            {
                return Mathf.Lerp(100000f, 120000f, (sunElevation - 0.7f) / 0.3f); // 100,000-120,000 lux (直射阳光)
            }
        }
        
        /// <summary>
        /// 将勒克斯值转换为Unity光照强度 - 优化版（降低正午过亮问题）
        /// Unity的强度单位与真实世界不完全一致，需要转换
        /// </summary>
        private float ConvertLuxToUnityIntensity(float luxValue)
        {
            // Unity光照强度转换公式（经验值，针对游戏渲染优化）
            if (luxValue <= 0.01f)
            {
                return 0f; // 完全黑暗
            }
            else if (luxValue <= 1f)
            {
                return luxValue * 0.005f; // 夜晚：0-0.005 （轻微降低）
            }
            else if (luxValue <= 100f)
            {
                return Mathf.Lerp(0.005f, 0.08f, (luxValue - 1f) / 99f); // 黄昏：0.005-0.08
            }
            else if (luxValue <= 1000f)
            {
                return Mathf.Lerp(0.08f, 0.4f, (luxValue - 100f) / 900f); // 阴天：0.08-0.4
            }
            else if (luxValue <= 10000f)
            {
                return Mathf.Lerp(0.4f, 1.2f, (luxValue - 1000f) / 9000f); // 早晨/傍晚：0.4-1.2
            }
            else if (luxValue <= 50000f)
            {
                return Mathf.Lerp(1.2f, 1.8f, (luxValue - 10000f) / 40000f); // 阴天户外：1.2-1.8 （主要优化）
            }
            else // 直射阳光：>50,000 lux
            {
                return Mathf.Lerp(1.8f, 2.2f, Mathf.Min((luxValue - 50000f) / 70000f, 1f)); // 晴天：1.8-2.2 （显著降低）
            }
        }
        
        /// <summary>
        /// 获取基于真实世界的色温值（开尔文）
        /// 数据来源：天体物理学和摄影学标准
        /// </summary>
        private float GetRealWorldColorTemperature(float sunElevation)
        {
            if (sunElevation <= -0.1f) // 夜晚
            {
                return 4100f; // 月光色温：约4100K（偏蓝）
            }
            else if (sunElevation <= 0f) // 天文薄暮
            {
                return Mathf.Lerp(4100f, 2000f, (sunElevation + 0.1f) / 0.1f); // 4100K-2000K
            }
            else if (sunElevation <= 0.05f) // 深度日出/日落
            {
                return Mathf.Lerp(2000f, 2500f, sunElevation / 0.05f); // 2000K-2500K（深橙红）
            }
            else if (sunElevation <= 0.15f) // 日出/日落黄金时刻
            {
                return Mathf.Lerp(2500f, 3200f, (sunElevation - 0.05f) / 0.1f); // 2500K-3200K（温暖橙黄）
            }
            else if (sunElevation <= 0.3f) // 早晨/傍晚
            {
                return Mathf.Lerp(3200f, 4500f, (sunElevation - 0.15f) / 0.15f); // 3200K-4500K（暖白）
            }
            else if (sunElevation <= 0.6f) // 上午/下午
            {
                return Mathf.Lerp(4500f, 5600f, (sunElevation - 0.3f) / 0.3f); // 4500K-5600K（接近自然白）
            }
            else // 正午时分
            {
                return Mathf.Lerp(5600f, 6500f, (sunElevation - 0.6f) / 0.4f); // 5600K-6500K（日光白）
            }
        }
        
        /// <summary>
        /// 基于色温将开尔文值转换为RGB颜色
        /// 使用普朗克黑体辐射定律的近似算法
        /// </summary>
        private Color ColorTemperatureToRGB(float kelvin)
        {
            // 限制色温范围以避免极端值
            kelvin = Mathf.Clamp(kelvin, 1000f, 12000f);
            
            float temp = kelvin / 100f;
            float red, green, blue;
            
            // 红色分量计算
            if (temp <= 66f)
            {
                red = 255f;
            }
            else
            {
                red = temp - 60f;
                red = 329.698727446f * Mathf.Pow(red, -0.1332047592f);
                red = Mathf.Clamp(red, 0f, 255f);
            }
            
            // 绿色分量计算
            if (temp <= 66f)
            {
                green = temp;
                green = 99.4708025861f * Mathf.Log(green) - 161.1195681661f;
                green = Mathf.Clamp(green, 0f, 255f);
            }
            else
            {
                green = temp - 60f;
                green = 288.1221695283f * Mathf.Pow(green, -0.0755148492f);
                green = Mathf.Clamp(green, 0f, 255f);
            }
            
            // 蓝色分量计算
            if (temp >= 66f)
            {
                blue = 255f;
            }
            else if (temp <= 19f)
            {
                blue = 0f;
            }
            else
            {
                blue = temp - 10f;
                blue = 138.5177312231f * Mathf.Log(blue) - 305.0447927307f;
                blue = Mathf.Clamp(blue, 0f, 255f);
            }
            
            return new Color(red / 255f, green / 255f, blue / 255f, 1f);
        }
        
        /// <summary>
        /// 更新环境光照以模拟真实世界的天空散射
        /// </summary>
        private void UpdateAmbientLighting(float sunElevation, Color sunColor)
        {
            // 计算环境光强度（基于天空散射理论）
            float ambientIntensity;
            Color ambientColor;
            
            if (sunElevation <= -0.1f) // 夜晚
            {
                ambientIntensity = 0.02f; // 非常暗的环境光
                ambientColor = new Color(0.1f, 0.15f, 0.3f); // 冷蓝色夜空
            }
            else if (sunElevation <= 0f) // 薄暮
            {
                ambientIntensity = Mathf.Lerp(0.02f, 0.1f, (sunElevation + 0.1f) / 0.1f);
                ambientColor = Color.Lerp(new Color(0.1f, 0.15f, 0.3f), new Color(0.3f, 0.2f, 0.4f), (sunElevation + 0.1f) / 0.1f);
            }
            else if (sunElevation <= 0.1f) // 日出/日落
            {
                ambientIntensity = Mathf.Lerp(0.1f, 0.3f, sunElevation / 0.1f);
                ambientColor = Color.Lerp(new Color(0.3f, 0.2f, 0.4f), new Color(0.8f, 0.5f, 0.3f), sunElevation / 0.1f);
            }
            else // 白天
            {
                ambientIntensity = Mathf.Lerp(0.3f, 0.8f, Mathf.Min(sunElevation / 0.7f, 1f));
                // 环境光颜色受太阳光影响，但稍偏蓝（瑞利散射效应）
                ambientColor = Color.Lerp(sunColor, new Color(sunColor.r * 0.8f, sunColor.g * 0.9f, sunColor.b * 1.2f), 0.3f);
            }
            
            // 应用到Unity的环境光设置
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor * ambientIntensity;
            
            // 如果使用天空盒环境光，也更新相关参数
            RenderSettings.ambientIntensity = ambientIntensity;
        }

        /// <summary>
        /// 创建程序化云层材质
        /// </summary>
        private void CreateProceduralCloudMaterial()
        {
            // 查找适合的云层着色器（可以是Unlit或Standard）
            Shader cloudShader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            
            if (cloudShader != null)
            {
                cloudMaterialInstance = new Material(cloudShader);
                cloudMaterialInstance.name = "Procedural Cloud Material (Generated)";
                
                // 设置基本的云层属性
                if (cloudMaterialInstance.HasProperty("_Color"))
                {
                    cloudMaterialInstance.SetColor("_Color", new Color(1f, 1f, 1f, 0.8f));
                }
                
                // 如果是Standard着色器，设置为透明模式
                if (cloudShader.name == "Standard")
                {
                    cloudMaterialInstance.SetFloat("_Mode", 2); // Fade
                    cloudMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    cloudMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    cloudMaterialInstance.SetInt("_ZWrite", 0);
                    cloudMaterialInstance.DisableKeyword("_ALPHATEST_ON");
                    cloudMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
                    cloudMaterialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    cloudMaterialInstance.renderQueue = 3000;
                }
                
                Debug.Log("[SkySystem] 已创建程序化云层材质");
            }
            else
            {
                Debug.LogError("[SkySystem] 无法找到适合的云层着色器");
            }
        }

        /// <summary>
        /// 创建程序化星空材质
        /// </summary>
        private void CreateProceduralStarMaterial()
        {
            // 查找适合的星空着色器
            Shader starShader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            
            if (starShader != null)
            {
                starMaterialInstance = new Material(starShader);
                starMaterialInstance.name = "Procedural Star Material (Generated)";
                
                // 设置基本的星空属性
                if (starMaterialInstance.HasProperty("_Color"))
                {
                    starMaterialInstance.SetColor("_Color", Color.white);
                }
                
                Debug.Log("[SkySystem] 已创建程序化星空材质");
            }
            else
            {
                Debug.LogError("[SkySystem] 无法找到适合的星空着色器");
            }
        }

        /// <summary>
        /// 初始化默认梯度
        /// </summary>
        private void InitializeDefaultGradients()
        {
            // 地平线颜色梯度
            if (horizonColorGradient == null)
            {
                horizonColorGradient = new Gradient();
                GradientColorKey[] horizonKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.9f, 0.7f, 0.5f), 0.0f),  // 夜晚地平线
                    new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0.25f),   // 日出地平线
                    new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0.5f),    // 正午地平线
                    new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0.75f),   // 日落地平线
                    new GradientColorKey(new Color(0.9f, 0.7f, 0.5f), 1.0f)   // 夜晚地平线
                };
                
                GradientAlphaKey[] horizonAlphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                
                horizonColorGradient.SetKeys(horizonKeys, horizonAlphaKeys);
            }

            // 天顶颜色梯度
            if (zenithColorGradient == null)
            {
                zenithColorGradient = new Gradient();
                GradientColorKey[] zenithKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0.0f),  // 夜晚天顶
                    new GradientColorKey(new Color(0.4f, 0.6f, 0.9f), 0.25f), // 日出天顶
                    new GradientColorKey(new Color(0.3f, 0.7f, 1f), 0.5f),    // 正午天顶
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.8f), 0.75f), // 日落天顶
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1.0f)   // 夜晚天顶
                };
                
                GradientAlphaKey[] zenithAlphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                
                zenithColorGradient.SetKeys(zenithKeys, zenithAlphaKeys);
            }
        }

        /// <summary>
        /// 设置默认天空盒
        /// </summary>
        private void SetupDefaultSkybox()
        {
            if (skyMaterialInstance != null)
            {
                RenderSettings.skybox = skyMaterialInstance;
                DynamicGI.UpdateEnvironment();
            }
        }

        #endregion

        #region 天空控制方法

        /// <summary>
        /// 设置一天中的时间
        /// </summary>
        public void SetTimeOfDay(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            currentTimeOfDay = normalizedTime;
            
            UpdateSkyColors(normalizedTime);
            UpdateAtmosphereScattering(normalizedTime);
            UpdateCloudProperties(normalizedTime);
            UpdateStarProperties(normalizedTime);
            
            // 同步到环境状态
            SyncToEnvironmentState();
            
            // 触发事件
            OnSkyStateChanged?.Invoke(normalizedTime);
        }

        /// <summary>
        /// 更新天空颜色
        /// </summary>
        private void UpdateSkyColors(float timeOfDay)
        {
            if (skyMaterialInstance == null) return;

            // 计算地平线和天顶颜色
            Color horizonColor = horizonColorGradient.Evaluate(timeOfDay);
            Color zenithColor = zenithColorGradient.Evaluate(timeOfDay);

            // 应用到天空材质
            skyMaterialInstance.SetColor("_SkyTint", skyTint);
            skyMaterialInstance.SetColor("_GroundColor", horizonColor);
            
            // 如果使用程序化天空盒
            if (skyMaterialInstance.shader.name.Contains("Procedural"))
            {
                skyMaterialInstance.SetFloat("_SunSize", 0.04f);
                skyMaterialInstance.SetFloat("_SunSizeConvergence", 5f);
                skyMaterialInstance.SetFloat("_AtmosphereThickness", atmosphereThickness);
                skyMaterialInstance.SetColor("_SkyTint", skyTint);
                skyMaterialInstance.SetColor("_GroundColor", horizonColor);
            }
        }

        /// <summary>
        /// 更新大气散射参数
        /// </summary>
        private void UpdateAtmosphereScattering(float timeOfDay)
        {
            if (skyMaterialInstance == null) return;

            // 根据时间调整散射强度
            float scatteringMultiplier = 1f;
            if (timeOfDay < 0.25f || timeOfDay > 0.75f)
            {
                // 夜晚减少散射
                scatteringMultiplier = 0.3f;
            }
            else if (timeOfDay > 0.2f && timeOfDay < 0.3f || timeOfDay > 0.7f && timeOfDay < 0.8f)
            {
                // 日出日落增强散射
                scatteringMultiplier = 1.5f;
            }

            // 更新散射参数
            if (skyMaterialInstance.HasProperty(AtmosphereThickness))
            {
                skyMaterialInstance.SetFloat(AtmosphereThickness, atmosphereThickness * scatteringMultiplier);
            }
            
            if (skyMaterialInstance.HasProperty(RayleighCoefficient))
            {
                skyMaterialInstance.SetFloat(RayleighCoefficient, rayleighCoefficient);
            }
            
            if (skyMaterialInstance.HasProperty(MieCoefficient))
            {
                skyMaterialInstance.SetFloat(MieCoefficient, mieCoefficient);
            }
            
            if (skyMaterialInstance.HasProperty(MieDirectionalG))
            {
                skyMaterialInstance.SetFloat(MieDirectionalG, mieDirectionalG);
            }
        }

        /// <summary>
        /// 更新云层属性
        /// </summary>
        private void UpdateCloudProperties(float timeOfDay)
        {
            if (!enableClouds) return;

            // 计算云层偏移（模拟云层移动）
            Vector2 cloudOffset = new Vector2(
                Time.time * cloudSpeed * 0.01f,
                Time.time * cloudSpeed * 0.005f
            );

            // 更新天空材质中的云层参数
            if (skyMaterialInstance != null)
            {
                if (skyMaterialInstance.HasProperty(CloudCoverage))
                {
                    skyMaterialInstance.SetFloat(CloudCoverage, cloudCoverage);
                }
                
                if (skyMaterialInstance.HasProperty(CloudDensity))
                {
                    skyMaterialInstance.SetFloat(CloudDensity, cloudDensity);
                }
                
                if (skyMaterialInstance.HasProperty(CloudOffset))
                {
                    skyMaterialInstance.SetVector(CloudOffset, cloudOffset);
                }
            }

            // 更新独立云层材质
            if (cloudMaterialInstance != null)
            {
                cloudMaterialInstance.SetFloat("_Coverage", cloudCoverage);
                cloudMaterialInstance.SetFloat("_Density", cloudDensity);
                cloudMaterialInstance.SetVector("_CloudOffset", cloudOffset);
                cloudMaterialInstance.SetFloat("_Height", cloudHeight);
            }
        }

        /// <summary>
        /// 更新星空属性
        /// </summary>
        private void UpdateStarProperties(float timeOfDay)
        {
            if (!enableStars) return;

            // 计算星空可见度（夜晚时可见）
            float starVisibility = 0f;
            if (timeOfDay < 0.25f || timeOfDay > 0.75f)
            {
                starVisibility = (timeOfDay < 0.25f) ? (0.25f - timeOfDay) * 4f : (timeOfDay - 0.75f) * 4f;
            }

            // 更新天空材质中的星空参数
            if (skyMaterialInstance != null)
            {
                if (skyMaterialInstance.HasProperty(StarIntensity))
                {
                    skyMaterialInstance.SetFloat(StarIntensity, starIntensity * starVisibility);
                }
                
                if (skyMaterialInstance.HasProperty(StarTwinkle))
                {
                    skyMaterialInstance.SetFloat(StarTwinkle, starTwinkle);
                }
            }

            // 更新独立星空材质
            if (starMaterialInstance != null)
            {
                starMaterialInstance.SetFloat("_Intensity", starIntensity * starVisibility);
                starMaterialInstance.SetFloat("_Twinkle", starTwinkle);
                starMaterialInstance.SetFloat("_Time", Time.time);
            }
        }

        /// <summary>
        /// 设置云层覆盖度
        /// </summary>
        public void SetCloudCoverage(float coverage)
        {
            cloudCoverage = Mathf.Clamp01(coverage);
            UpdateCloudProperties(currentTimeOfDay);
        }

        /// <summary>
        /// 设置大气厚度
        /// </summary>
        public void SetAtmosphereThickness(float thickness)
        {
            atmosphereThickness = Mathf.Clamp(thickness, 0f, 5f);
            UpdateAtmosphereScattering(currentTimeOfDay);
        }

        #endregion

        #region 系统更新

        /// <summary>
        /// 更新天空系统 (由EnvironmentManager调用)
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
                
                if (Mathf.Abs(cloudCoverage - linkedEnvironmentState.cloudCoverage) > 0.01f)
                {
                    SetCloudCoverage(linkedEnvironmentState.cloudCoverage);
                }
                
                if (Mathf.Abs(atmosphereThickness - linkedEnvironmentState.atmosphereThickness) > 0.01f)
                {
                    SetAtmosphereThickness(linkedEnvironmentState.atmosphereThickness);
                }
            }

            // 持续更新云层动画
            if (enableClouds)
            {
                UpdateCloudProperties(currentTimeOfDay);
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
            SetCloudCoverage(linkedEnvironmentState.cloudCoverage);
            SetAtmosphereThickness(linkedEnvironmentState.atmosphereThickness);
            skyTint = linkedEnvironmentState.skyTint;
        }

        /// <summary>
        /// 同步到环境状态
        /// </summary>
        private void SyncToEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;

            linkedEnvironmentState.skyTint = skyTint;
            linkedEnvironmentState.atmosphereThickness = atmosphereThickness;
            linkedEnvironmentState.cloudCoverage = cloudCoverage;
            linkedEnvironmentState.cloudDensity = cloudDensity;
        }

        #endregion

        #region 清理资源

        void OnDestroy()
        {
            // 取消订阅事件
            if (linkedTimeSystem != null)
            {
                linkedTimeSystem.OnTimeChanged -= HandleTimeChanged;
            }
            
            if (linkedWeatherSystem != null)
            {
                linkedWeatherSystem.OnWeatherChanged -= HandleWeatherChanged;
                linkedWeatherSystem.OnWeatherIntensityChanged -= HandleWeatherIntensityChanged;
            }
            
            // 清理材质实例
            if (skyMaterialInstance != null)
            {
                DestroyImmediate(skyMaterialInstance);
            }
            
            if (cloudMaterialInstance != null)
            {
                DestroyImmediate(cloudMaterialInstance);
            }
            
            if (starMaterialInstance != null)
            {
                DestroyImmediate(starMaterialInstance);
            }
        }

        #endregion

        #region 公共调试方法

        /// <summary>
        /// 强制重新初始化天空材质（调试用）
        /// </summary>
        [ContextMenu("Force Reinitialize Sky Materials")]
        public void ForceReinitializeSkyMaterials()
        {
            Debug.Log("[SkySystem] 强制重新初始化天空材质...");
            
            // 检查可用的着色器
            Shader[] availableShaders = new Shader[]
            {
                Shader.Find("WorldEditor/PhysicallyBasedSky"),
                Shader.Find("WorldEditor/ProfessionalSky"),
                Shader.Find("WorldEditor/GenshinStyleSky")
            };
            
            for (int i = 0; i < availableShaders.Length; i++)
            {
                Debug.Log($"[SkySystem] 着色器检查 {i}: {(availableShaders[i] != null ? availableShaders[i].name : "未找到")}");
            }
            
            // 清理现有材质
            if (skyMaterialInstance != null)
            {
                Debug.Log($"[SkySystem] 清理旧材质: {skyMaterialInstance.shader.name}");
                DestroyImmediate(skyMaterialInstance);
                skyMaterialInstance = null;
            }
            
            // 重新初始化
            InitializeMaterials();
            
            // 检查结果
            if (skyMaterialInstance != null)
            {
                Debug.Log($"[SkySystem] 新材质已创建: {skyMaterialInstance.shader.name}");
                Debug.Log($"[SkySystem] 当前RenderSettings.skybox: {(RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "null")}");
            }
            else
            {
                Debug.LogError("[SkySystem] 材质初始化失败!");
            }
            
            // 如果成功创建了材质，更新参数
            if (skyMaterialInstance != null)
            {
                UpdateSkyMaterialParameters();
                Debug.Log("[SkySystem] 天空材质重新初始化完成");
            }
            else
            {
                Debug.LogError("[SkySystem] 天空材质重新初始化失败");
            }
        }

        #endregion

        #region 天气响应系统

        /// <summary>
        /// 订阅系统事件
        /// </summary>
        private void SubscribeToSystemEvents()
        {
            // 订阅时间系统事件
            if (linkedTimeSystem != null)
            {
                linkedTimeSystem.OnTimeChanged += HandleTimeChanged;
                Debug.Log("[SkySystem] 已订阅时间系统事件");
            }

            // 订阅天气系统事件
            if (linkedWeatherSystem != null)
            {
                linkedWeatherSystem.OnWeatherChanged += HandleWeatherChanged;
                linkedWeatherSystem.OnWeatherIntensityChanged += HandleWeatherIntensityChanged;
                Debug.Log("[SkySystem] 已订阅天气系统事件");
            }
        }

        /// <summary>
        /// 处理时间变化事件
        /// </summary>
        private void HandleTimeChanged(float normalizedTime)
        {
            currentTimeOfDay = normalizedTime;
            UpdateSkyParameters();
        }

        /// <summary>
        /// 处理天气变化事件
        /// </summary>
        private void HandleWeatherChanged(WorldEditor.Environment.WeatherType newWeather, WorldEditor.Environment.WeatherType oldWeather)
        {
            currentWeather = newWeather;
            ApplyWeatherToSky();
            Debug.Log($"[SkySystem] 天空响应天气变化: {oldWeather} → {newWeather}");
        }

        /// <summary>
        /// 处理天气强度变化事件
        /// </summary>
        private void HandleWeatherIntensityChanged(float intensity)
        {
            currentWeatherIntensity = intensity;
            ApplyWeatherToSky();
        }

        /// <summary>
        /// 应用天气对天空的影响
        /// </summary>
        private void ApplyWeatherToSky()
        {
            if (linkedEnvironmentState == null) return;

            // 从环境状态同步天气信息
            currentWeather = linkedEnvironmentState.currentWeather;
            currentWeatherIntensity = linkedEnvironmentState.weatherIntensity;

            // 根据天气类型调整天空参数
            switch (currentWeather)
            {
                case WorldEditor.Environment.WeatherType.Clear:
                    // 晴天：标准天空效果
                    ApplyClearSkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Cloudy:
                    // 多云：增加云层覆盖
                    ApplyCloudySkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Overcast:
                    // 阴天：厚重云层，暗淡天空
                    ApplyOvercastSkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Rainy:
                    // 雨天：雨云效果
                    ApplyRainySkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Storm:
                    // 暴风雨：乌云密布
                    ApplyStormSkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Snowy:
                    // 雪天：雪云效果
                    ApplySnowySkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Foggy:
                    // 雾天：雾霾效果
                    ApplyFoggySkySettings();
                    break;
                    
                case WorldEditor.Environment.WeatherType.Windy:
                    // 大风：快速移动的云层
                    ApplyWindySkySettings();
                    break;
            }

            // 更新天空材质参数
            UpdateSkyMaterialParameters();
        }

        /// <summary>
        /// 晴天天空设置
        /// </summary>
        private void ApplyClearSkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.1f, 0.3f, currentWeatherIntensity);
            cloudDensity = 0.3f;
            atmosphereThickness = 1f;
            skyTint = Color.white;
            starIntensity = currentTimeOfDay < 0.3f || currentTimeOfDay > 0.7f ? 1f : 0f;
        }

        /// <summary>
        /// 多云天空设置
        /// </summary>
        private void ApplyCloudySkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.3f, 0.6f, currentWeatherIntensity);
            cloudDensity = Mathf.Lerp(0.4f, 0.6f, currentWeatherIntensity);
            atmosphereThickness = Mathf.Lerp(1f, 1.2f, currentWeatherIntensity);
            skyTint = Color.Lerp(Color.white, new Color(0.9f, 0.9f, 0.95f), currentWeatherIntensity * 0.3f);
            starIntensity = (currentTimeOfDay < 0.3f || currentTimeOfDay > 0.7f) ? (1f - currentWeatherIntensity * 0.3f) : 0f;
        }

        /// <summary>
        /// 阴天天空设置
        /// </summary>
        private void ApplyOvercastSkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.6f, 0.9f, currentWeatherIntensity);
            cloudDensity = Mathf.Lerp(0.6f, 0.8f, currentWeatherIntensity);
            atmosphereThickness = Mathf.Lerp(1.2f, 1.5f, currentWeatherIntensity);
            skyTint = Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.85f), currentWeatherIntensity * 0.5f);
            starIntensity = (currentTimeOfDay < 0.3f || currentTimeOfDay > 0.7f) ? (1f - currentWeatherIntensity * 0.6f) : 0f;
        }

        /// <summary>
        /// 雨天天空设置
        /// </summary>
        private void ApplyRainySkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.7f, 1f, currentWeatherIntensity);
            cloudDensity = Mathf.Lerp(0.7f, 0.9f, currentWeatherIntensity);
            atmosphereThickness = Mathf.Lerp(1.3f, 1.8f, currentWeatherIntensity);
            skyTint = Color.Lerp(Color.white, new Color(0.7f, 0.75f, 0.8f), currentWeatherIntensity * 0.6f);
            starIntensity = 0f; // 雨天看不到星星
        }

        /// <summary>
        /// 暴风雨天空设置
        /// </summary>
        private void ApplyStormSkySettings()
        {
            cloudCoverage = 1f;
            cloudDensity = Mathf.Lerp(0.8f, 1f, currentWeatherIntensity);
            atmosphereThickness = Mathf.Lerp(1.5f, 2f, currentWeatherIntensity);
            skyTint = Color.Lerp(Color.white, new Color(0.5f, 0.55f, 0.6f), currentWeatherIntensity * 0.8f);
            starIntensity = 0f;
            
            // 暴风雨特效：云层快速移动
            cloudSpeed = Mathf.Lerp(2f, 5f, currentWeatherIntensity);
        }

        /// <summary>
        /// 雪天天空设置
        /// </summary>
        private void ApplySnowySkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.6f, 0.8f, currentWeatherIntensity);
            cloudDensity = Mathf.Lerp(0.5f, 0.7f, currentWeatherIntensity);
            atmosphereThickness = Mathf.Lerp(1f, 1.3f, currentWeatherIntensity);
            skyTint = Color.Lerp(Color.white, new Color(0.95f, 0.95f, 1f), currentWeatherIntensity * 0.4f);
            starIntensity = (currentTimeOfDay < 0.3f || currentTimeOfDay > 0.7f) ? (1f - currentWeatherIntensity * 0.4f) : 0f;
        }

        /// <summary>
        /// 雾天天空设置
        /// </summary>
        private void ApplyFoggySkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.4f, 0.7f, currentWeatherIntensity);
            cloudDensity = Mathf.Lerp(0.8f, 1f, currentWeatherIntensity);
            atmosphereThickness = Mathf.Lerp(1.5f, 2.5f, currentWeatherIntensity);
            skyTint = Color.Lerp(Color.white, new Color(0.85f, 0.85f, 0.9f), currentWeatherIntensity * 0.5f);
            starIntensity = (currentTimeOfDay < 0.3f || currentTimeOfDay > 0.7f) ? (1f - currentWeatherIntensity * 0.8f) : 0f;
        }

        /// <summary>
        /// 大风天空设置
        /// </summary>
        private void ApplyWindySkySettings()
        {
            cloudCoverage = Mathf.Lerp(0.2f, 0.5f, currentWeatherIntensity);
            cloudDensity = 0.4f;
            atmosphereThickness = 1f;
            skyTint = Color.white;
            starIntensity = currentTimeOfDay < 0.3f || currentTimeOfDay > 0.7f ? 1f : 0f;
            
            // 大风特效：云层快速移动
            cloudSpeed = Mathf.Lerp(2f, 4f, currentWeatherIntensity);
        }

        /// <summary>
        /// 更新天空参数
        /// </summary>
        private void UpdateSkyParameters()
        {
            UpdateSkyMaterialParameters();
            OnSkyStateChanged?.Invoke(currentTimeOfDay);
        }

        /// <summary>
        /// 更新天空材质参数
        /// </summary>
        private void UpdateSkyMaterialParameters()
        {
            if (skyMaterialInstance == null) return;

            // 检查是否为《原神》风格着色器
            bool isPhysicallyBasedSky = skyMaterialInstance.shader.name == "WorldEditor/PhysicallyBasedSky";
            
            if (isPhysicallyBasedSky)
            {
                // 基于物理的天空参数更新
                UpdatePhysicallyBasedSkyParameters();
            }
            else
            {
                // 传统天空参数更新（兼容性）
                UpdateLegacySkyParameters();
            }

            // 应用材质到天空盒
            RenderSettings.skybox = skyMaterialInstance;
            
            // 强制刷新天空盒
            DynamicGI.UpdateEnvironment();
        }
        
        /// <summary>
        /// 更新《原神》风格天空参数
        /// </summary>
        private void UpdatePhysicallyBasedSkyParameters()
        {
            // 时间和天体
            skyMaterialInstance.SetFloat("_TimeOfDay", currentTimeOfDay);
            UpdateCelestialDirections();
            
            // 大气参数
            skyMaterialInstance.SetFloat("_AtmosphereThickness", atmosphereThickness);
            skyMaterialInstance.SetColor("_SkyTint", skyTint);
            
            // 云层参数
            skyMaterialInstance.SetFloat("_CloudCoverage", cloudCoverage);
            skyMaterialInstance.SetFloat("_CloudDensity", cloudDensity);
            skyMaterialInstance.SetFloat("_CloudSpeed", cloudSpeed);
            
            // 星空参数
            skyMaterialInstance.SetFloat("_StarIntensity", starIntensity);
            skyMaterialInstance.SetFloat("_StarTwinkle", starTwinkle);
            
            // 天气效果
            skyMaterialInstance.SetFloat("_WeatherIntensity", currentWeatherIntensity);
            
            // 根据天气类型调整雨效参数
            float rainEffect = (currentWeather == WorldEditor.Environment.WeatherType.Rainy || 
                              currentWeather == WorldEditor.Environment.WeatherType.Storm) ? 
                              currentWeatherIntensity : 0f;
            skyMaterialInstance.SetFloat("_RainEffect", rainEffect);
        }
        
        /// <summary>
        /// 更新传统天空参数（向后兼容）
        /// </summary>
        private void UpdateLegacySkyParameters()
        {
            // 获取太阳位置
            Vector3 sunPosition = GetSunPosition();
            
            // 更新基础参数（安全地设置属性）
            SetMaterialPropertySafe(skyMaterialInstance, AtmosphereThickness, atmosphereThickness);
            SetMaterialPropertySafe(skyMaterialInstance, "_AtmosphereThickness", atmosphereThickness);
            SetMaterialPropertySafe(skyMaterialInstance, RayleighCoefficient, rayleighCoefficient);
            SetMaterialPropertySafe(skyMaterialInstance, MieCoefficient, mieCoefficient);
            SetMaterialPropertySafe(skyMaterialInstance, MieDirectionalG, mieDirectionalG);
            SetMaterialPropertySafe(skyMaterialInstance, SkyTint, skyTint);
            SetMaterialPropertySafe(skyMaterialInstance, "_SkyTint", skyTint);
            SetMaterialPropertySafe(skyMaterialInstance, SunPosition, sunPosition);
            SetMaterialPropertySafe(skyMaterialInstance, "_SunDirection", sunPosition);

            // 更新云层参数
            SetMaterialPropertySafe(skyMaterialInstance, CloudCoverage, cloudCoverage);
            SetMaterialPropertySafe(skyMaterialInstance, CloudDensity, cloudDensity);
            SetMaterialPropertySafe(skyMaterialInstance, CloudOffset, Time.time * cloudSpeed);

            // 更新星空参数
            SetMaterialPropertySafe(skyMaterialInstance, StarIntensity, starIntensity);
            SetMaterialPropertySafe(skyMaterialInstance, StarTwinkle, starTwinkle);

            // 更新天气参数
            SetMaterialPropertySafe(skyMaterialInstance, WeatherInfluence, currentWeatherIntensity);
            SetMaterialPropertySafe(skyMaterialInstance, WeatherType, (float)currentWeather);
            
            // 更新雾效参数
            if (linkedEnvironmentState != null)
            {
                SetMaterialPropertySafe(skyMaterialInstance, FogDensity, linkedEnvironmentState.fogDensity);
                SetMaterialPropertySafe(skyMaterialInstance, FogColor, linkedEnvironmentState.fogColor);
            }
        }

        /// <summary>
        /// 安全地设置材质属性（检查属性是否存在）
        /// </summary>
        private void SetMaterialPropertySafe(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private void SetMaterialPropertySafe(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private void SetMaterialPropertySafe(Material material, string propertyName, Vector4 value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetVector(propertyName, value);
            }
        }

        // 重载方法：接受shader property ID (int)
        private void SetMaterialPropertySafe(Material material, int propertyID, float value)
        {
            if (material.HasProperty(propertyID))
            {
                material.SetFloat(propertyID, value);
            }
        }

        private void SetMaterialPropertySafe(Material material, int propertyID, Color value)
        {
            if (material.HasProperty(propertyID))
            {
                material.SetColor(propertyID, value);
            }
        }

        private void SetMaterialPropertySafe(Material material, int propertyID, Vector4 value)
        {
            if (material.HasProperty(propertyID))
            {
                material.SetVector(propertyID, value);
            }
        }

        private void SetMaterialPropertySafe(Material material, int propertyID, Vector3 value)
        {
            if (material.HasProperty(propertyID))
            {
                material.SetVector(propertyID, value);
            }
        }

        /// <summary>
        /// 获取太阳位置
        /// </summary>
        /// <summary>
        /// 基于真实天体力学的太阳位置计算
        /// 实现精确的日出日落时间控制
        /// </summary>
        private Vector3 GetSunPosition()
        {
            // 超级简单直接的时间映射 - 确保100%正确
            // 0.0 = 午夜, 0.25 = 日出, 0.5 = 正午, 0.75 = 日落, 1.0 = 午夜
            
            // 直接计算：正午时Y=1，午夜时Y=-1
            float normalizedTime = (currentTimeOfDay - 0.5f) * 2f; // -1 to 1
            float sunHeight = Mathf.Cos(normalizedTime * Mathf.PI); // cos(0)=1在正午，cos(π)=-1在午夜
            
            // 简单的东西方向：早上在东边，晚上在西边
            float eastWest = Mathf.Sin(currentTimeOfDay * 2f * Mathf.PI);
            float northSouth = Mathf.Cos(currentTimeOfDay * 2f * Mathf.PI);
            
            Vector3 sunDirection = new Vector3(eastWest, sunHeight, northSouth).normalized;
            
            // 调试输出
            string timeStr = "";
            if (currentTimeOfDay < 0.1f || currentTimeOfDay > 0.9f) timeStr = "午夜";
            else if (currentTimeOfDay < 0.4f) timeStr = "日出";
            else if (currentTimeOfDay < 0.6f) timeStr = "正午";
            else timeStr = "日落";
            
            return sunDirection;
        }
        
        /// <summary>
        /// 专业级月亮位置计算
        /// 确保月亮与太阳相位相反，永不同时出现
        /// </summary>
        private Vector3 GetMoonPosition()
        {
            // 月亮相位：当太阳在白天时，月亮在夜晚（相位差180度）
            float moonTimeOfDay = (currentTimeOfDay + 0.5f) % 1f; // 月亮时间偏移12小时
            float moonTimeAngle = (moonTimeOfDay - 0.25f) * 2f * Mathf.PI;
            
            // 月亮高度角
            float moonElevation = Mathf.Sin(moonTimeAngle);
            
            // 月亮方位角
            float moonAzimuth = moonTimeAngle;
            
            // 转换为笛卡尔坐标
            float cosElevation = Mathf.Cos(Mathf.Asin(moonElevation));
            Vector3 moonDirection = new Vector3(
                cosElevation * Mathf.Sin(moonAzimuth),    // X: 东西方向
                moonElevation,                            // Y: 高度（可为负）
                cosElevation * Mathf.Cos(moonAzimuth)     // Z: 南北方向
            );
            
            return moonDirection.normalized;
        }

        #endregion

        #region 调试信息

        void OnGUI()
        {
            // 调试面板已禁用
            /*
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            GUILayout.BeginArea(new Rect(740, 10, 200, 150));
            GUILayout.Box("天空系统调试");
            
            GUILayout.Label($"时间: {currentTimeOfDay:F2}");
            GUILayout.Label($"天气: {currentWeather}");
            GUILayout.Label($"天气强度: {currentWeatherIntensity:F2}");
            GUILayout.Label($"云层覆盖: {cloudCoverage:F2}");
            GUILayout.Label($"大气厚度: {atmosphereThickness:F2}");
            GUILayout.Label($"星空强度: {starIntensity:F2}");
            
            GUILayout.EndArea();
            */
        }

        #endregion
    }
}