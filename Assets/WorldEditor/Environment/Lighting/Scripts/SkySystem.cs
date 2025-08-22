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
        private Material skyMaterialInstance;
        private Material cloudMaterialInstance;
        private Material starMaterialInstance;
        private EnvironmentState linkedEnvironmentState;

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

        #endregion

        #region 事件系统

        /// <summary>天空状态变化事件</summary>
        public event Action<float> OnSkyStateChanged;

        #endregion

        #region 公共属性

        /// <summary>天空系统是否激活</summary>
        public bool IsActive => isActive && isInitialized;
        
        /// <summary>当前云层覆盖度</summary>
        public float CurrentCloudCoverage => cloudCoverage;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化天空系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[SkySystem] 天空系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[SkySystem] 开始初始化天空系统...");

            // 链接环境状态
            linkedEnvironmentState = environmentState;

            // 初始化材质实例
            InitializeMaterials();

            // 初始化默认梯度
            InitializeDefaultGradients();

            // 设置默认天空盒
            SetupDefaultSkybox();

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
                // 使用Unity内置程序化天空盒
                skyMaterialInstance = new Material(Shader.Find("Skybox/Procedural"));
            }

            // 创建云层材质实例
            if (cloudMaterial != null && enableClouds)
            {
                cloudMaterialInstance = new Material(cloudMaterial);
            }

            // 创建星空材质实例
            if (starMaterial != null && enableStars)
            {
                starMaterialInstance = new Material(starMaterial);
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

        #region 调试信息

        void OnGUI()
        {
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            GUILayout.BeginArea(new Rect(740, 10, 200, 120));
            GUILayout.Box("天空系统调试");
            
            GUILayout.Label($"时间: {currentTimeOfDay:F2}");
            GUILayout.Label($"云层覆盖: {cloudCoverage:F2}");
            GUILayout.Label($"大气厚度: {atmosphereThickness:F2}");
            GUILayout.Label($"星空强度: {starIntensity:F2}");
            
            GUILayout.EndArea();
        }

        #endregion
    }
}