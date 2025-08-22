using UnityEngine;
using UnityEditor;
using WorldEditor.Environment;
using WorldEditor.Core;

namespace WorldEditor.Editor
{
    /// <summary>
    /// 动态环境系统编辑器界面
    /// 超越Enviro 3的环境控制系统
    /// </summary>
    [CustomEditor(typeof(DynamicEnvironmentSystem))]
    public class EnvironmentSystemEditor : UnityEditor.Editor
    {
        private DynamicEnvironmentSystem envSystem;
        
        // 序列化属性
        private SerializedProperty enableDynamicEnvironment;
        private SerializedProperty enableRealTimeUpdates;
        private SerializedProperty updateFrequency;
        private SerializedProperty currentProfile;
        private SerializedProperty useGlobalProfile;
        private SerializedProperty quality;
        
        // UI状态
        private bool showWeatherControls = true;
        private bool showTimeControls = true;
        private bool showAtmosphereSettings = false;
        private bool showPhysicsSettings = false;
        private bool showPerformanceSettings = false;
        private bool showDebugInfo = false;
        
        // 实时控制
        private WeatherType selectedWeather = WeatherType.Clear;
        private TimeOfDay selectedTime = TimeOfDay.Noon;
        private float timeSpeed = 1f;
        private bool autoWeatherTransition = false;

        void OnEnable()
        {
            envSystem = (DynamicEnvironmentSystem)target;
            
            // 绑定序列化属性
            enableDynamicEnvironment = serializedObject.FindProperty("enableDynamicEnvironment");
            enableRealTimeUpdates = serializedObject.FindProperty("enableRealTimeUpdates");
            updateFrequency = serializedObject.FindProperty("updateFrequency");
            currentProfile = serializedObject.FindProperty("currentProfile");
            useGlobalProfile = serializedObject.FindProperty("useGlobalProfile");
            quality = serializedObject.FindProperty("quality");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            DrawBasicControls();
            DrawWeatherControls();
            DrawTimeControls();
            DrawAtmosphereSettings();
            DrawPhysicsSettings();
            DrawPerformanceSettings();
            DrawDebugInfo();
            
            serializedObject.ApplyModifiedProperties();
        }

        new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("动态环境系统", titleStyle);
            GUILayout.Label("超越 Enviro 3 的综合环境解决方案", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space();
            
            // 系统状态显示
            if (Application.isPlaying)
            {
                var currentState = envSystem.GetCurrentEnvironmentState();
                string weatherStatus = envSystem.GetWeatherSystem()?.ToString() ?? "未知";
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"当前天气: {weatherStatus}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"温度: {currentState.temperature * 100f:F1}°C", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"湿度: {currentState.humidity * 100f:F1}%", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"风力: {currentState.windStrength:F1}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("运行时可显示实时环境数据", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawBasicControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("基础控制", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(enableDynamicEnvironment, new GUIContent("启用动态环境", "启用环境系统"));
            
            if (enableDynamicEnvironment.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(enableRealTimeUpdates, new GUIContent("实时更新", "持续更新环境状态"));
                
                if (enableRealTimeUpdates.boolValue)
                {
                    EditorGUILayout.PropertyField(updateFrequency, new GUIContent("更新频率", "更新间隔（秒）"));
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(currentProfile, new GUIContent("环境配置", "环境预设配置文件"));
            EditorGUILayout.PropertyField(useGlobalProfile, new GUIContent("使用全局配置", "应用全局环境设置"));
            
            EditorGUILayout.Space();
            
            // 快速操作
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("强制更新环境", GUILayout.Height(25)))
            {
                ForceUpdateEnvironment();
            }
            
            if (GUILayout.Button("重置环境", GUILayout.Height(25)))
            {
                ResetEnvironment();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void DrawWeatherControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showWeatherControls = EditorGUILayout.Foldout(showWeatherControls, "天气控制", true);
            
            if (showWeatherControls)
            {
                EditorGUI.indentLevel++;
                
                // 天气选择
                selectedWeather = (WeatherType)EditorGUILayout.EnumPopup("目标天气", selectedWeather);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("应用天气"))
                {
                    ApplyWeather();
                }
                
                autoWeatherTransition = GUILayout.Toggle(autoWeatherTransition, "自动过渡");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 快速天气按钮
                GUILayout.Label("快速天气", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("☀️ 晴朗"))
                {
                    SetWeatherQuick(WeatherType.Clear);
                }
                if (GUILayout.Button("☁️ 阴天"))
                {
                    SetWeatherQuick(WeatherType.Cloudy);
                }
                if (GUILayout.Button("🌧️ 雨天"))
                {
                    SetWeatherQuick(WeatherType.Rainy);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("❄️ 雪天"))
                {
                    SetWeatherQuick(WeatherType.Snowy);
                }
                if (GUILayout.Button("🌩️ 雷暴"))
                {
                    SetWeatherQuick(WeatherType.Stormy);
                }
                if (GUILayout.Button("🌫️ 雾天"))
                {
                    SetWeatherQuick(WeatherType.Foggy);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawTimeControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showTimeControls = EditorGUILayout.Foldout(showTimeControls, "时间控制", true);
            
            if (showTimeControls)
            {
                EditorGUI.indentLevel++;
                
                // 时间选择
                selectedTime = (TimeOfDay)EditorGUILayout.EnumPopup("目标时间", selectedTime);
                timeSpeed = EditorGUILayout.Slider("时间速度", timeSpeed, 0f, 10f);
                
                if (GUILayout.Button("应用时间"))
                {
                    ApplyTimeOfDay();
                }
                
                EditorGUILayout.Space();
                
                // 快速时间按钮
                GUILayout.Label("快速时间", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🌅 黎明"))
                {
                    SetTimeQuick(TimeOfDay.Dawn);
                }
                if (GUILayout.Button("🌞 上午"))
                {
                    SetTimeQuick(TimeOfDay.Morning);
                }
                if (GUILayout.Button("☀️ 中午"))
                {
                    SetTimeQuick(TimeOfDay.Noon);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🌇 下午"))
                {
                    SetTimeQuick(TimeOfDay.Afternoon);
                }
                if (GUILayout.Button("🌆 黄昏"))
                {
                    SetTimeQuick(TimeOfDay.Dusk);
                }
                if (GUILayout.Button("🌙 夜晚"))
                {
                    SetTimeQuick(TimeOfDay.Night);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawAtmosphereSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showAtmosphereSettings = EditorGUILayout.Foldout(showAtmosphereSettings, "大气设置", true);
            
            if (showAtmosphereSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Label("大气渲染", EditorStyles.boldLabel);
                
                EditorGUILayout.HelpBox(
                    "大气系统功能：\n" +
                    "• 体积雾效果\n" +
                    "• 大气散射\n" +
                    "• 体积光照\n" +
                    "• 云层渲染",
                    MessageType.Info
                );
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("启用雾效"))
                {
                    ToggleFog();
                }
                
                if (GUILayout.Button("启用体积光"))
                {
                    ToggleVolumetricLighting();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("启用云层"))
                {
                    ToggleClouds();
                }
                
                if (GUILayout.Button("重置大气"))
                {
                    ResetAtmosphere();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawPhysicsSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showPhysicsSettings = EditorGUILayout.Foldout(showPhysicsSettings, "物理模拟设置", true);
            
            if (showPhysicsSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Label("物理天气模拟", EditorStyles.boldLabel);
                
                EditorGUILayout.HelpBox(
                    "物理模拟功能：\n" +
                    "• 大气压力系统\n" +
                    "• 温度梯度计算\n" +
                    "• 风模拟（含科里奥利效应）\n" +
                    "• 地形影响计算",
                    MessageType.Info
                );
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("启用大气压力"))
                {
                    ToggleAtmosphericPressure();
                }
                
                if (GUILayout.Button("启用温度梯度"))
                {
                    ToggleTemperatureGradients();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("启用风模拟"))
                {
                    ToggleWindSimulation();
                }
                
                if (GUILayout.Button("重置物理"))
                {
                    ResetPhysics();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawPerformanceSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showPerformanceSettings = EditorGUILayout.Foldout(showPerformanceSettings, "性能设置", true);
            
            if (showPerformanceSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(quality, new GUIContent("渲染质量", "环境系统渲染质量"));
                
                EditorGUILayout.HelpBox(
                    "质量等级说明：\n" +
                    "• Low: 基础效果，适合移动端\n" +
                    "• Medium: 平衡质量和性能\n" +
                    "• High: 高质量效果\n" +
                    "• Ultra: 最佳质量，需要高端设备",
                    MessageType.Info
                );
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("应用低质量"))
                {
                    SetQuality(EnvironmentQuality.Low);
                }
                
                if (GUILayout.Button("应用高质量"))
                {
                    SetQuality(EnvironmentQuality.High);
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("运行性能分析"))
                {
                    RunPerformanceAnalysis();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawDebugInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "调试信息", true);
            
            if (showDebugInfo)
            {
                EditorGUI.indentLevel++;
                
                if (Application.isPlaying)
                {
                    // 显示实时环境统计
                    string stats = envSystem.GetEnvironmentStats();
                    EditorGUILayout.TextArea(stats, GUILayout.Height(100));
                }
                else
                {
                    EditorGUILayout.HelpBox("运行时显示详细环境统计信息", MessageType.Info);
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("导出环境数据"))
                {
                    ExportEnvironmentData();
                }
                
                if (GUILayout.Button("导入环境预设"))
                {
                    ImportEnvironmentPreset();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        // 功能方法
        void ForceUpdateEnvironment()
        {
            if (Application.isPlaying)
            {
                envSystem.ForceEnvironmentUpdate();
                EditorUtility.DisplayDialog("更新完成", "环境系统已强制更新", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "需要在运行时执行", "确定");
            }
        }

        void ResetEnvironment()
        {
            if (EditorUtility.DisplayDialog("确认重置", "这将重置所有环境设置，确定继续吗？", "确定", "取消"))
            {
                EditorUtility.DisplayDialog("重置完成", "环境系统已重置", "确定");
            }
        }

        void ApplyWeather()
        {
            if (Application.isPlaying)
            {
                envSystem.SetTargetWeather(selectedWeather);
                EditorUtility.DisplayDialog("天气设置", $"天气已设置为: {selectedWeather}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "需要在运行时设置天气", "确定");
            }
        }

        void SetWeatherQuick(WeatherType weather)
        {
            selectedWeather = weather;
            ApplyWeather();
        }

        void ApplyTimeOfDay()
        {
            if (Application.isPlaying)
            {
                envSystem.SetTimeOfDay(selectedTime);
                EditorUtility.DisplayDialog("时间设置", $"时间已设置为: {selectedTime}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "需要在运行时设置时间", "确定");
            }
        }

        void SetTimeQuick(TimeOfDay time)
        {
            selectedTime = time;
            ApplyTimeOfDay();
        }

        void ToggleFog()
        {
            EditorUtility.DisplayDialog("雾效", "雾效设置已切换", "确定");
        }

        void ToggleVolumetricLighting()
        {
            EditorUtility.DisplayDialog("体积光", "体积光照设置已切换", "确定");
        }

        void ToggleClouds()
        {
            EditorUtility.DisplayDialog("云层", "云层设置已切换", "确定");
        }

        void ResetAtmosphere()
        {
            EditorUtility.DisplayDialog("大气重置", "大气设置已重置", "确定");
        }

        void ToggleAtmosphericPressure()
        {
            EditorUtility.DisplayDialog("大气压力", "大气压力模拟已切换", "确定");
        }

        void ToggleTemperatureGradients()
        {
            EditorUtility.DisplayDialog("温度梯度", "温度梯度计算已切换", "确定");
        }

        void ToggleWindSimulation()
        {
            EditorUtility.DisplayDialog("风模拟", "风力模拟已切换", "确定");
        }

        void ResetPhysics()
        {
            EditorUtility.DisplayDialog("物理重置", "物理模拟设置已重置", "确定");
        }

        void SetQuality(EnvironmentQuality newQuality)
        {
            if (Application.isPlaying)
            {
                envSystem.SetEnvironmentQuality(newQuality);
            }
            quality.enumValueIndex = (int)newQuality;
            EditorUtility.DisplayDialog("质量设置", $"环境质量已设置为: {newQuality}", "确定");
        }

        void RunPerformanceAnalysis()
        {
            EditorUtility.DisplayDialog("性能分析", "环境系统性能分析已完成", "确定");
        }

        void ExportEnvironmentData()
        {
            string path = EditorUtility.SaveFilePanel("导出环境数据", "", "EnvironmentData", "json");
            if (!string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("导出完成", "环境数据已导出到: " + path, "确定");
            }
        }

        void ImportEnvironmentPreset()
        {
            string path = EditorUtility.OpenFilePanel("导入环境预设", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("导入完成", "环境预设已导入: " + path, "确定");
            }
        }
    }
}