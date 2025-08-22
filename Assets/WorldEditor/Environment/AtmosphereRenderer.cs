using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 大气渲染器 - 渲染大气散射、体积光等效果
    /// </summary>
    public class AtmosphereRenderer : MonoBehaviour
    {
        [Header("大气散射")]
        [SerializeField] private bool enableAtmosphericScattering = true;
        [SerializeField] private float rayleighScattering = 1f;
        [SerializeField] private float mieScattering = 1f;
        [SerializeField] private float atmosphereThickness = 1f;
        
        [Header("体积光")]
        [SerializeField] private bool enableVolumetricLighting = true;
        [SerializeField] private float volumetricIntensity = 1f;
        [SerializeField] private int volumetricSteps = 32;
        
        [Header("雾效果")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = Color.gray;
        [SerializeField] private float fogDensity = 0.01f;
        
        private DynamicEnvironmentSystem environmentSystem;
        private EnvironmentQuality currentQuality = EnvironmentQuality.High;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            SetupAtmosphereRendering();
        }
        
        void SetupAtmosphereRendering()
        {
            // 设置大气渲染组件
            SetupScattering();
            SetupVolumetricLighting();
        }
        
        void SetupScattering()
        {
            if (!enableAtmosphericScattering) return;
            
            // 设置Shader全局参数
            Shader.SetGlobalFloat("_RayleighScattering", rayleighScattering);
            Shader.SetGlobalFloat("_MieScattering", mieScattering);
            Shader.SetGlobalFloat("_AtmosphereThickness", atmosphereThickness);
        }
        
        void SetupVolumetricLighting()
        {
            if (!enableVolumetricLighting) return;
            
            // 设置体积光参数
            Shader.SetGlobalFloat("_VolumetricIntensity", volumetricIntensity);
            Shader.SetGlobalInt("_VolumetricSteps", volumetricSteps);
        }
        
        public void UpdateAtmosphere(float deltaTime, EnvironmentState environmentState)
        {
            if (!enableAtmosphericScattering) return;
            
            // 更新大气参数
            UpdateScatteringParameters(environmentState);
            UpdateVolumetricParameters(environmentState);
            UpdateFogParameters(environmentState);
        }
        
        void UpdateScatteringParameters(EnvironmentState environmentState)
        {
            float actualRayleigh = rayleighScattering * environmentState.rayleighScattering;
            float actualMie = mieScattering * environmentState.mieScattering;
            
            Shader.SetGlobalFloat("_RayleighScattering", actualRayleigh);
            Shader.SetGlobalFloat("_MieScattering", actualMie);
            Shader.SetGlobalFloat("_AtmosphereThickness", environmentState.atmosphereThickness);
        }
        
        void UpdateVolumetricParameters(EnvironmentState environmentState)
        {
            if (!enableVolumetricLighting) return;
            
            float actualIntensity = volumetricIntensity * environmentState.sunIntensity;
            Shader.SetGlobalFloat("_VolumetricIntensity", actualIntensity);
        }
        
        void UpdateFogParameters(EnvironmentState environmentState)
        {
            if (!enableFog) return;
            
            Color actualFogColor = Color.Lerp(fogColor, environmentState.fogColor, 0.5f);
            float actualFogDensity = fogDensity * environmentState.fogDensity;
            
            RenderSettings.fog = actualFogDensity > 0.001f;
            RenderSettings.fogColor = actualFogColor;
            RenderSettings.fogDensity = actualFogDensity;
        }
        
        public void SetFogEnabled(bool enabled)
        {
            enableFog = enabled;
        }
        
        public void SetVolumetricLightingEnabled(bool enabled)
        {
            enableVolumetricLighting = enabled;
        }
        
        public void SetQuality(EnvironmentQuality quality)
        {
            currentQuality = quality;
            
            // 根据质量调整参数
            switch (quality)
            {
                case EnvironmentQuality.Low:
                    volumetricSteps = 16;
                    break;
                case EnvironmentQuality.Medium:
                    volumetricSteps = 24;
                    break;
                case EnvironmentQuality.High:
                    volumetricSteps = 32;
                    break;
                case EnvironmentQuality.Ultra:
                    volumetricSteps = 48;
                    break;
            }
        }
    }
}