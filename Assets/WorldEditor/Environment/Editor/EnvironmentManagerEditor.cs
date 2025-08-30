using UnityEngine;
using UnityEditor;

namespace WorldEditor.Environment.Editor
{
    /// <summary>
    /// 环境管理器自定义编辑器
    /// 提供更好的Inspector界面和实时测试功能
    /// </summary>
    [CustomEditor(typeof(EnvironmentManager))]
    public class EnvironmentManagerEditor : UnityEditor.Editor
    {
        private EnvironmentManager environmentManager;

        void OnEnable()
        {
            environmentManager = (EnvironmentManager)target;
        }

        public override void OnInspectorGUI()
        {
            // 绘制默认Inspector
            DrawDefaultInspector();
            
            // 添加分隔线
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // 调试信息
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("调试信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"运行状态: {Application.isPlaying}");
            if (environmentManager != null)
            {
                EditorGUILayout.LabelField($"环境管理器: 存在");
                EditorGUILayout.LabelField($"当前状态: {(environmentManager.CurrentState != null ? "存在" : "空")}");
            }
            EditorGUILayout.EndVertical();
            
            // 运行时测试控制
            if (Application.isPlaying)
            {
                DrawRuntimeControls();
            }
            else
            {
                DrawEditorControls();
            }
            
            // 强制重绘
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        /// <summary>
        /// 绘制运行时控制面板
        /// </summary>
        private void DrawRuntimeControls()
        {
            EditorGUILayout.LabelField("运行时测试控制", EditorStyles.boldLabel);
            
            if (environmentManager != null && environmentManager.CurrentState != null)
            {
                var state = environmentManager.CurrentState;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"当前时间: {state.GetTimeString()}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"季节: {state.currentSeason}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"天气: {state.currentWeather}");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 时间控制
                EditorGUILayout.LabelField("时间控制", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("日出 (6:00)"))
                    environmentManager.SetTimeOfDay(0.25f);
                if (GUILayout.Button("正午 (12:00)"))
                    environmentManager.SetTimeOfDay(0.5f);
                if (GUILayout.Button("日落 (18:00)"))
                    environmentManager.SetTimeOfDay(0.75f);
                if (GUILayout.Button("午夜 (0:00)"))
                    environmentManager.SetTimeOfDay(0f);
                EditorGUILayout.EndHorizontal();
                
                // 季节控制
                EditorGUILayout.LabelField("季节控制", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("春天"))
                    environmentManager.SetSeason(SeasonType.Spring);
                if (GUILayout.Button("夏天"))
                    environmentManager.SetSeason(SeasonType.Summer);
                if (GUILayout.Button("秋天"))
                    environmentManager.SetSeason(SeasonType.Autumn);
                if (GUILayout.Button("冬天"))
                    environmentManager.SetSeason(SeasonType.Winter);
                EditorGUILayout.EndHorizontal();
                
                // 季节进度控制
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("季节进度:", GUILayout.Width(60));
                float currentProgress = state.seasonProgress;
                float newProgress = EditorGUILayout.Slider(currentProgress, 0f, 1f);
                if (Mathf.Abs(newProgress - currentProgress) > 0.001f)
                {
                    environmentManager.SetSeasonProgress(newProgress);
                }
                EditorGUILayout.LabelField($"{newProgress:F2}", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                
                // 显示季节信息
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"季节天数: {Mathf.FloorToInt(newProgress * 30)}/30", GUILayout.Width(100));
                string seasonPhase = GetSeasonPhase(newProgress);
                EditorGUILayout.LabelField($"阶段: {seasonPhase}");
                EditorGUILayout.EndHorizontal();
                
                // 天气控制
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("天气控制", EditorStyles.boldLabel);
                
                // 第一行天气按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("晴天"))
                    environmentManager.SetWeather(WeatherType.Clear, 1f);
                if (GUILayout.Button("多云"))
                    environmentManager.SetWeather(WeatherType.Cloudy, 0.7f);
                if (GUILayout.Button("阴天"))
                    environmentManager.SetWeather(WeatherType.Overcast, 0.8f);
                if (GUILayout.Button("雨天"))
                    environmentManager.SetWeather(WeatherType.Rainy, 0.8f);
                EditorGUILayout.EndHorizontal();
                
                // 第二行天气按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("暴雨"))
                    environmentManager.SetWeather(WeatherType.Storm, 1f);
                if (GUILayout.Button("雪天"))
                    environmentManager.SetWeather(WeatherType.Snowy, 0.9f);
                if (GUILayout.Button("雾天"))
                    environmentManager.SetWeather(WeatherType.Foggy, 0.8f);
                if (GUILayout.Button("大风"))
                    environmentManager.SetWeather(WeatherType.Windy, 0.7f);
                EditorGUILayout.EndHorizontal();
                
                // 天气强度控制
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("天气强度:", GUILayout.Width(60));
                float currentIntensity = state.weatherIntensity;
                float newIntensity = EditorGUILayout.Slider(currentIntensity, 0f, 1f);
                if (Mathf.Abs(newIntensity - currentIntensity) > 0.001f)
                {
                    environmentManager.SetWeather(state.currentWeather, newIntensity);
                }
                EditorGUILayout.LabelField($"{newIntensity:P0}", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                
                // 天气信息显示
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"当前天气强度: {state.weatherIntensity:P0}", GUILayout.Width(120));
                if (state.weatherTransition > 0f && state.weatherTransition < 1f)
                {
                    EditorGUILayout.LabelField($"过渡中: {state.weatherTransition:P0}");
                }
                EditorGUILayout.EndHorizontal();
                
                // 光照状态显示
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("光照状态", EditorStyles.boldLabel);
                
                // 获取主方向光
                Light mainLight = RenderSettings.sun;
                if (mainLight == null)
                {
                    Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                    foreach (Light light in lights)
                    {
                        if (light.type == LightType.Directional)
                        {
                            mainLight = light;
                            break;
                        }
                    }
                }
                
                EditorGUILayout.BeginVertical("box");
                if (mainLight != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"太阳光强度: {mainLight.intensity:F2}", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"颜色: R{mainLight.color.r:F2} G{mainLight.color.g:F2} B{mainLight.color.b:F2}");
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("未找到主方向光");
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"环境光强度: {RenderSettings.ambientIntensity:F2}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"雾效: {(RenderSettings.fog ? "开启" : "关闭")}");
                EditorGUILayout.EndHorizontal();
                
                // 环境温度和湿度
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"温度: {state.temperature:F1}°C", GUILayout.Width(120));
                EditorGUILayout.LabelField($"湿度: {state.humidity:P0}");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                // 天空系统状态显示
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("天空系统状态", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"云层覆盖度: {state.cloudCoverage:P0}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"大气厚度: {state.atmosphereThickness:F2}");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"天空色调: R{state.skyTint.r:F2} G{state.skyTint.g:F2} B{state.skyTint.b:F2}");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("环境管理器未初始化或当前状态为空", MessageType.Warning);
            }
        }

        /// <summary>
        /// 绘制编辑器控制面板
        /// </summary>
        private void DrawEditorControls()
        {
            EditorGUILayout.LabelField("编辑器工具", EditorStyles.boldLabel);
            
            if (GUILayout.Button("强制初始化组件引用"))
            {
                // 强制设置组件引用
                var timeSystem = environmentManager.GetComponent<TimeSystem>();
                if (timeSystem == null)
                {
                    timeSystem = environmentManager.gameObject.AddComponent<TimeSystem>();
                }
                
                // 使用反射设置私有字段
                var field = typeof(EnvironmentManager).GetField("timeSystem", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(environmentManager, timeSystem);
                
                EditorUtility.SetDirty(environmentManager);
                Debug.Log("[EnvironmentManagerEditor] 强制初始化完成");
            }
            
            if (GUILayout.Button("创建默认环境状态"))
            {
                // 创建默认环境状态
                var stateField = typeof(EnvironmentManager).GetField("currentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var newState = new EnvironmentState();
                stateField?.SetValue(environmentManager, newState);
                
                EditorUtility.SetDirty(environmentManager);
                Debug.Log("[EnvironmentManagerEditor] 创建默认环境状态完成");
            }
            
            EditorGUILayout.HelpBox("请先运行游戏来测试环境系统功能", MessageType.Info);
        }
        
        /// <summary>
        /// 获取季节阶段描述
        /// </summary>
        private string GetSeasonPhase(float progress)
        {
            if (progress < 0.25f) return "初期";
            else if (progress < 0.5f) return "早期";
            else if (progress < 0.75f) return "中期";
            else return "晚期";
        }
    }
}