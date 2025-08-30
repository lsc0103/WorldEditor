using UnityEngine;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 天空系统调试器 - 帮助诊断天空渲染问题
    /// </summary>
    public class SkySystemDebugger : MonoBehaviour
    {
        [Header("调试设置")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool debugSunPosition = true;
        [SerializeField] private bool debugMaterialProperties = true;
        
        [Header("实时信息")]
        [SerializeField] private Material currentSkyboxMaterial;
        [SerializeField] private Vector3 currentSunDirection;
        [SerializeField] private float currentTimeOfDay;
        
        void Update()
        {
            if (showDebugInfo)
            {
                UpdateDebugInfo();
            }
        }
        
        void UpdateDebugInfo()
        {
            // 获取当前天空盒材质
            currentSkyboxMaterial = RenderSettings.skybox;
            
            // 获取太阳方向
            Light sunLight = RenderSettings.sun;
            if (sunLight != null)
            {
                currentSunDirection = sunLight.transform.forward;
            }
            
            // 获取当前时间 - 优先从SkySystem获取，备用从EnvironmentManager获取
            SkySystem skySystem = FindFirstObjectByType<SkySystem>();
            if (skySystem != null && skySystem.IsInitialized)
            {
                currentTimeOfDay = skySystem.CurrentTimeOfDay;
            }
            else
            {
                EnvironmentManager envManager = FindFirstObjectByType<EnvironmentManager>();
                if (envManager != null && envManager.CurrentState != null)
                {
                    currentTimeOfDay = envManager.CurrentState.timeOfDay;
                }
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, Screen.height - 250, 400, 240));
            GUILayout.Box("天空系统调试信息");
            
            // 基础信息
            GUILayout.Label($"当前时间: {currentTimeOfDay:F3}");
            GUILayout.Label($"太阳方向: {currentSunDirection}");
            
            // 天空盒材质信息
            if (currentSkyboxMaterial != null)
            {
                GUILayout.Label($"天空材质: {currentSkyboxMaterial.name}");
                GUILayout.Label($"着色器: {currentSkyboxMaterial.shader.name}");
                
                if (debugMaterialProperties && currentSkyboxMaterial.shader.name == "WorldEditor/GenshinStyleSky")
                {
                    GUILayout.Space(5);
                    GUILayout.Label("着色器属性:");
                    
                    if (currentSkyboxMaterial.HasProperty("_SunIntensity"))
                        GUILayout.Label($"太阳强度: {currentSkyboxMaterial.GetFloat("_SunIntensity"):F2}");
                    
                    if (currentSkyboxMaterial.HasProperty("_SunSize"))
                        GUILayout.Label($"太阳尺寸: {currentSkyboxMaterial.GetFloat("_SunSize"):F3}");
                    
                    if (currentSkyboxMaterial.HasProperty("_SunDirection"))
                    {
                        Vector4 sunDir = currentSkyboxMaterial.GetVector("_SunDirection");
                        GUILayout.Label($"着色器太阳方向: {sunDir}");
                    }
                    
                    if (currentSkyboxMaterial.HasProperty("_TimeOfDay"))
                        GUILayout.Label($"着色器时间: {currentSkyboxMaterial.GetFloat("_TimeOfDay"):F3}");
                }
            }
            else
            {
                GUILayout.Label("天空材质: 无");
            }
            
            // 操作按钮
            GUILayout.Space(10);
            if (GUILayout.Button("强制刷新天空"))
            {
                SkySystem skySystem = FindFirstObjectByType<SkySystem>();
                if (skySystem != null)
                {
                    skySystem.ForceReinitializeSkyMaterials();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}