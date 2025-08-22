using UnityEngine;
using UnityEngine.Rendering;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 光照控制器 - 管理动态光照、阴影和曝光
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        [Header("光照设置")]
        [SerializeField] private bool enableDynamicLighting = true;
        [SerializeField] private bool enableAutoExposure = true;
        [SerializeField] private float exposureSpeed = 2f;
        [SerializeField] private Vector2 exposureRange = new Vector2(0.5f, 2f);
        
        [Header("阴影设置")]
        [SerializeField] private bool enableDynamicShadows = true;
        [SerializeField] private UnityEngine.ShadowQuality shadowQuality = UnityEngine.ShadowQuality.All;
        [SerializeField] private float shadowDistance = 150f;
        [SerializeField] private int shadowCascades = 4;
        
        [Header("环境反射")]
        [SerializeField] private bool enableDynamicReflections = true;
        [SerializeField] private ReflectionProbe environmentProbe;
        [SerializeField] private float probeUpdateInterval = 1f;
        
        [Header("光照探针")]
        [SerializeField] private bool enableLightProbes = true;
        [SerializeField] private LightProbeGroup lightProbeGroup;
        [SerializeField] private int probeResolution = 32;
        
        [Header("HDRI天空")]
        [SerializeField] private bool useHDRISky = false;
        [SerializeField] private Cubemap hdriSkybox;
        [SerializeField] private float hdriExposure = 1f;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private EnvironmentQuality currentQuality = EnvironmentQuality.High;
        private Light[] sceneLights;
        private ReflectionProbe[] reflectionProbes;
        
        // 光照状态
        private float currentExposure = 1f;
        private float targetExposure = 1f;
        private float lastProbeUpdateTime;
        
        // 后处理效果（替代URP组件）
        private bool enableShadows = true;
        private float shadowIntensity = 1f;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            SetupLightingComponents();
            SetupPostProcessing();
            SetupReflectionProbes();
        }
        
        void SetupLightingComponents()
        {
            // 设置渲染设置
            SetupRenderSettings();
            
            // 设置光照探针
            if (enableLightProbes && lightProbeGroup == null)
            {
                SetupLightProbes();
            }
        }
        
        void SetupRenderSettings()
        {
            // 设置环境光模式
            RenderSettings.ambientMode = useHDRISky ? AmbientMode.Skybox : AmbientMode.Trilight;
            
            // 设置天空盒
            if (useHDRISky && hdriSkybox != null)
            {
                Material skyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));
                skyboxMaterial.SetTexture("_Tex", hdriSkybox);
                skyboxMaterial.SetFloat("_Exposure", hdriExposure);
                RenderSettings.skybox = skyboxMaterial;
                
                Debug.Log("[LightingController] 启用HDRI天空盒");
            }
            
            // 设置反射设置
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.defaultReflectionResolution = 256;
            
            // 设置雾效
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }
        
        void SetupPostProcessing()
        {
            // 查找场景中的所有光源
            sceneLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            
            // 设置基础光照参数
            if (sceneLights.Length > 0)
            {
                foreach (var light in sceneLights)
                {
                    if (light.type == LightType.Directional)
                    {
                        // 配置主光源
                        light.shadows = enableShadows ? LightShadows.Soft : LightShadows.None;
                        light.shadowStrength = shadowIntensity;
                    }
                }
            }
        }
        
        void SetupReflectionProbes()
        {
            if (!enableDynamicReflections) return;
            
            if (environmentProbe == null)
            {
                // 创建环境反射探针
                GameObject probeObj = new GameObject("Environment Reflection Probe");
                probeObj.transform.SetParent(transform);
                environmentProbe = probeObj.AddComponent<ReflectionProbe>();
                
                environmentProbe.mode = ReflectionProbeMode.Realtime;
                environmentProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                environmentProbe.size = new Vector3(1000f, 1000f, 1000f);
                environmentProbe.resolution = 256;
                environmentProbe.hdr = true;
                environmentProbe.shadowDistance = shadowDistance;
            }
        }
        
        void SetupLightProbes()
        {
            GameObject lightProbeObj = new GameObject("Light Probe Group");
            lightProbeObj.transform.SetParent(transform);
            lightProbeGroup = lightProbeObj.AddComponent<LightProbeGroup>();
            
            // 生成光照探针网格
            GenerateLightProbeGrid();
        }
        
        void GenerateLightProbeGrid()
        {
            if (lightProbeGroup == null) return;
            
            Vector3[] probePositions = new Vector3[probeResolution * probeResolution];
            int index = 0;
            
            float spacing = 50f; // 探针间距
            float halfRes = probeResolution * 0.5f;
            
            for (int x = 0; x < probeResolution; x++)
            {
                for (int z = 0; z < probeResolution; z++)
                {
                    Vector3 position = new Vector3(
                        (x - halfRes) * spacing,
                        10f, // 固定高度
                        (z - halfRes) * spacing
                    );
                    
                    probePositions[index] = position;
                    index++;
                }
            }
            
            lightProbeGroup.probePositions = probePositions;
        }
        
        public void UpdateLighting(float deltaTime, EnvironmentState environmentState)
        {
            if (!enableDynamicLighting) return;
            
            // 更新曝光
            UpdateExposure(deltaTime, environmentState);
            
            // 更新色彩调整
            UpdateColorAdjustments(environmentState);
            
            // 更新白平衡
            UpdateWhiteBalance(environmentState);
            
            // 更新反射探针
            UpdateReflectionProbes(deltaTime);
            
            // 更新阴影设置
            UpdateShadowSettings(environmentState);
        }
        
        void UpdateExposure(float deltaTime, EnvironmentState environmentState)
        {
            if (!enableAutoExposure) return;
            
            // 计算目标曝光值
            float timeBasedExposure = CalculateTimeBasedExposure(environmentState.timeOfDay);
            float weatherBasedExposure = CalculateWeatherBasedExposure(environmentState.currentWeather);
            
            targetExposure = timeBasedExposure * weatherBasedExposure;
            targetExposure = Mathf.Clamp(targetExposure, exposureRange.x, exposureRange.y);
            
            // 平滑过渡到目标曝光
            currentExposure = Mathf.Lerp(currentExposure, targetExposure, deltaTime * exposureSpeed);
            
            // 应用曝光到场景光源
            ApplyExposureToLights(currentExposure);
        }
        
        float CalculateTimeBasedExposure(float timeOfDay)
        {
            // 基于时间计算曝光
            // 0.5 = 正午（最亮），0 或 1 = 午夜（最暗）
            float normalizedTime = Mathf.Abs(timeOfDay - 0.5f) * 2f; // 0-1，0为正午，1为午夜
            
            // 使用曲线来模拟真实的曝光变化
            AnimationCurve exposureCurve = AnimationCurve.EaseInOut(0f, 1.5f, 1f, 0.3f);
            return exposureCurve.Evaluate(1f - normalizedTime);
        }
        
        float CalculateWeatherBasedExposure(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear: return 1f;
                case WeatherType.Cloudy: return 0.8f;
                case WeatherType.Rainy: return 0.6f;
                case WeatherType.Stormy: return 0.4f;
                case WeatherType.Foggy: return 0.7f;
                case WeatherType.Snowy: return 0.9f; // 雪反射更多光
                default: return 1f;
            }
        }
        
        void UpdateColorAdjustments(EnvironmentState environmentState)
        {
            // 基于时间调整饱和度
            float timeSaturation = CalculateTimeSaturation(environmentState.timeOfDay);
            
            // 基于天气调整饱和度
            float weatherSaturation = CalculateWeatherSaturation(environmentState.currentWeather);
            
            float finalSaturation = timeSaturation * weatherSaturation;
            
            // 调整对比度
            float timeContrast = CalculateTimeContrast(environmentState.timeOfDay);
            
            // 应用到场景渲染设置
            ApplyColorAdjustmentsToScene(finalSaturation, timeContrast);
        }
        
        float CalculateTimeSaturation(float timeOfDay)
        {
            // 日出日落时饱和度较高，夜晚较低
            float hourAngle = timeOfDay * 2f * Mathf.PI;
            float saturationCurve = (Mathf.Sin(hourAngle) + 1f) * 0.5f; // 0-1
            
            return Mathf.Lerp(-20f, 10f, saturationCurve); // 饱和度范围
        }
        
        float CalculateWeatherSaturation(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear: return 1f;
                case WeatherType.Cloudy: return 0.9f;
                case WeatherType.Rainy: return 0.7f;
                case WeatherType.Stormy: return 0.6f;
                case WeatherType.Foggy: return 0.8f;
                case WeatherType.Snowy: return 0.85f;
                default: return 1f;
            }
        }
        
        float CalculateTimeContrast(float timeOfDay)
        {
            // 正午对比度高，夜晚对比度低
            float normalizedTime = Mathf.Abs(timeOfDay - 0.5f) * 2f;
            return Mathf.Lerp(15f, -5f, normalizedTime);
        }
        
        void UpdateWhiteBalance(EnvironmentState environmentState)
        {
            // 基于时间调整色温
            float timeTemperature = CalculateTimeTemperature(environmentState.timeOfDay);
            
            // 基于天气调整色温
            float weatherTemperature = CalculateWeatherTemperature(environmentState.currentWeather);
            
            float finalTemperature = timeTemperature + weatherTemperature;
            
            // 应用白平衡到光源
            ApplyWhiteBalanceToLights(finalTemperature);
        }
        
        float CalculateTimeTemperature(float timeOfDay)
        {
            // 日出日落偏暖，正午偏中性，夜晚偏冷
            if (timeOfDay < 0.25f || timeOfDay > 0.75f) // 夜晚
                return -10f; // 偏冷
            else if (timeOfDay >= 0.3f && timeOfDay <= 0.7f) // 白天
                return 5f; // 偏暖
            else // 日出日落
                return 20f; // 很暖
        }
        
        float CalculateWeatherTemperature(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear: return 0f;
                case WeatherType.Cloudy: return -5f;
                case WeatherType.Rainy: return -10f;
                case WeatherType.Stormy: return -15f;
                case WeatherType.Foggy: return -8f;
                case WeatherType.Snowy: return -12f;
                default: return 0f;
            }
        }
        
        void UpdateReflectionProbes(float deltaTime)
        {
            if (!enableDynamicReflections || environmentProbe == null) return;
            
            // 定期更新反射探针
            if (Time.time - lastProbeUpdateTime > probeUpdateInterval)
            {
                environmentProbe.RenderProbe();
                lastProbeUpdateTime = Time.time;
            }
        }
        
        void UpdateShadowSettings(EnvironmentState environmentState)
        {
            if (!enableDynamicShadows) return;
            
            // 根据光照强度调整阴影质量
            float lightIntensity = environmentState.sunIntensity;
            
            if (lightIntensity > 0.1f)
            {
                QualitySettings.shadows = shadowQuality;
                QualitySettings.shadowDistance = shadowDistance;
                QualitySettings.shadowCascades = shadowCascades;
            }
            else
            {
                // 夜晚或低光照时禁用阴影以提高性能
                QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            }
        }
        
        public void SetSunIntensity(float intensity)
        {
            // 该方法由日夜循环控制器调用
            // 这里可以添加额外的光照调整逻辑
        }
        
        public void SetSunColor(Color color)
        {
            // 该方法由日夜循环控制器调用
            // 这里可以添加额外的颜色调整逻辑
        }
        
        public void SetQuality(EnvironmentQuality quality)
        {
            currentQuality = quality;
            
            // 根据质量调整设置
            switch (quality)
            {
                case EnvironmentQuality.Low:
                    shadowDistance = 50f;
                    shadowCascades = 2;
                    if (environmentProbe != null)
                        environmentProbe.resolution = 128;
                    probeResolution = 16;
                    break;
                    
                case EnvironmentQuality.Medium:
                    shadowDistance = 100f;
                    shadowCascades = 3;
                    if (environmentProbe != null)
                        environmentProbe.resolution = 256;
                    probeResolution = 24;
                    break;
                    
                case EnvironmentQuality.High:
                    shadowDistance = 150f;
                    shadowCascades = 4;
                    if (environmentProbe != null)
                        environmentProbe.resolution = 512;
                    probeResolution = 32;
                    break;
                    
                case EnvironmentQuality.Ultra:
                    shadowDistance = 250f;
                    shadowCascades = 4;
                    if (environmentProbe != null)
                        environmentProbe.resolution = 1024;
                    probeResolution = 48;
                    break;
            }
            
            // 重新生成光照探针
            if (enableLightProbes)
            {
                GenerateLightProbeGrid();
            }
        }
        
        public float GetCurrentExposure()
        {
            return currentExposure;
        }
        
        public void ForceUpdateReflectionProbe()
        {
            if (environmentProbe != null)
            {
                environmentProbe.RenderProbe();
            }
        }
        
        /// <summary>
        /// 应用曝光到场景光源
        /// </summary>
        void ApplyExposureToLights(float exposure)
        {
            if (sceneLights == null) return;
            
            foreach (var light in sceneLights)
            {
                if (light != null && light.type == LightType.Directional)
                {
                    // 调整主光源强度
                    float baseIntensity = 1f;
                    light.intensity = baseIntensity * exposure;
                }
            }
        }
        
        /// <summary>
        /// 应用色彩调整到场景
        /// </summary>
        void ApplyColorAdjustmentsToScene(float saturation, float contrast)
        {
            // 设置全局着色器参数
            Shader.SetGlobalFloat("_Saturation", saturation);
            Shader.SetGlobalFloat("_Contrast", contrast);
        }
        
        /// <summary>
        /// 应用白平衡到光源
        /// </summary>
        void ApplyWhiteBalanceToLights(float temperature)
        {
            if (sceneLights == null) return;
            
            // 将温度转换为颜色
            Color temperatureColor = CalculateColorFromTemperature(temperature);
            
            foreach (var light in sceneLights)
            {
                if (light != null && light.type == LightType.Directional)
                {
                    light.color = temperatureColor;
                }
            }
        }
        
        /// <summary>
        /// 根据温度计算颜色
        /// </summary>
        Color CalculateColorFromTemperature(float temperature)
        {
            // 简化的色温到RGB转换
            float normalizedTemp = (temperature + 50f) / 100f; // 归一化到0-1
            
            // 暖色调（偏红）到冷色调（偏蓝）
            Color warmColor = new Color(1f, 0.8f, 0.6f, 1f);
            Color coolColor = new Color(0.6f, 0.8f, 1f, 1f);
            Color neutralColor = Color.white;
            
            if (normalizedTemp < 0.5f)
            {
                return Color.Lerp(coolColor, neutralColor, normalizedTemp * 2f);
            }
            else
            {
                return Color.Lerp(neutralColor, warmColor, (normalizedTemp - 0.5f) * 2f);
            }
        }
    }
}