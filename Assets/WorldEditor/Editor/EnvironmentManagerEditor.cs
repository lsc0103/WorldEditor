using UnityEngine;
using UnityEditor;
using WorldEditor.Environment;

namespace WorldEditor.Editor
{
    /// <summary>
    /// 环境管理器编辑器界面
    /// 为新的环境系统提供编辑器支持
    /// </summary>
    [CustomEditor(typeof(EnvironmentManager))]
    public class EnvironmentManagerEditor : UnityEditor.Editor
    {
        private EnvironmentManager envManager;
        
        // UI状态
        private bool showTimeControls = true;
        private bool showWeatherControls = true;
        private bool showSeasonControls = true;
        private bool showSystemStatus = true;
        
        void OnEnable()
        {
            envManager = (EnvironmentManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (envManager == null)
                return;
                
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("环境系统控制", EditorStyles.boldLabel);
            
            // 时间控制
            showTimeControls = EditorGUILayout.Foldout(showTimeControls, "时间控制");
            if (showTimeControls)
            {
                EditorGUI.indentLevel++;
                DrawTimeControls();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 天气控制
            showWeatherControls = EditorGUILayout.Foldout(showWeatherControls, "天气控制");
            if (showWeatherControls)
            {
                EditorGUI.indentLevel++;
                DrawWeatherControls();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 季节控制
            showSeasonControls = EditorGUILayout.Foldout(showSeasonControls, "季节控制");
            if (showSeasonControls)
            {
                EditorGUI.indentLevel++;
                DrawSeasonControls();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 系统状态
            showSystemStatus = EditorGUILayout.Foldout(showSystemStatus, "系统状态");
            if (showSystemStatus)
            {
                EditorGUI.indentLevel++;
                DrawSystemStatus();
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawTimeControls()
        {
            if (envManager.CurrentState == null)
                return;
                
            EditorGUILayout.BeginVertical("box");
            
            // 当前时间显示
            EditorGUILayout.LabelField($"当前时间: {envManager.CurrentState.GetTimeString()}");
            EditorGUILayout.LabelField($"已过天数: {envManager.CurrentState.daysPassed}");
            
            // 时间滑动条
            float currentTime = envManager.CurrentState.timeOfDay;
            float newTime = EditorGUILayout.Slider("时间 (0=午夜, 0.5=正午)", currentTime, 0f, 1f);
            if (Mathf.Abs(newTime - currentTime) > 0.01f)
            {
                envManager.SetTimeOfDay(newTime);
            }
            
            // 快捷时间按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("午夜")) envManager.SetTimeOfDay(0f);
            if (GUILayout.Button("日出")) envManager.SetTimeOfDay(0.25f);
            if (GUILayout.Button("正午")) envManager.SetTimeOfDay(0.5f);
            if (GUILayout.Button("日落")) envManager.SetTimeOfDay(0.75f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawWeatherControls()
        {
            if (envManager.CurrentState == null)
                return;
                
            EditorGUILayout.BeginVertical("box");
            
            // 当前天气显示
            EditorGUILayout.LabelField($"当前天气: {envManager.CurrentState.currentWeather}");
            EditorGUILayout.LabelField($"天气强度: {envManager.CurrentState.weatherIntensity:F2}");
            
            // 天气选择
            WeatherType newWeather = (WeatherType)EditorGUILayout.EnumPopup("设置天气", envManager.CurrentState.currentWeather);
            if (newWeather != envManager.CurrentState.currentWeather)
            {
                envManager.SetWeather(newWeather);
            }
            
            // 天气强度滑动条
            float newIntensity = EditorGUILayout.Slider("天气强度", envManager.CurrentState.weatherIntensity, 0f, 1f);
            if (Mathf.Abs(newIntensity - envManager.CurrentState.weatherIntensity) > 0.01f)
            {
                envManager.SetWeather(envManager.CurrentState.currentWeather, newIntensity);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSeasonControls()
        {
            if (envManager.CurrentState == null)
                return;
                
            EditorGUILayout.BeginVertical("box");
            
            // 当前季节显示
            EditorGUILayout.LabelField($"当前季节: {envManager.CurrentState.currentSeason}");
            EditorGUILayout.LabelField($"季节进度: {envManager.CurrentState.seasonProgress * 100:F0}%");
            
            // 季节选择
            SeasonType newSeason = (SeasonType)EditorGUILayout.EnumPopup("设置季节", envManager.CurrentState.currentSeason);
            if (newSeason != envManager.CurrentState.currentSeason)
            {
                envManager.SetSeason(newSeason);
            }
            
            // 季节进度滑条控制 - 新增功能
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("季节进度控制", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("进度:", GUILayout.Width(40));
            float currentProgress = envManager.CurrentState.seasonProgress;
            float newProgress = EditorGUILayout.Slider(currentProgress, 0f, 1f);
            if (Mathf.Abs(newProgress - currentProgress) > 0.001f)
            {
                envManager.SetSeasonProgress(newProgress);
            }
            EditorGUILayout.LabelField($"{newProgress:F2}", GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();
            
            // 季节阶段信息
            EditorGUILayout.BeginHorizontal();
            int seasonDays = Mathf.FloorToInt(newProgress * 30);
            string seasonPhase = GetSeasonPhase(newProgress);
            EditorGUILayout.LabelField($"天数: {seasonDays}/30", GUILayout.Width(80));
            EditorGUILayout.LabelField($"阶段: {seasonPhase}");
            EditorGUILayout.EndHorizontal();
            
            // 快速设置按钮
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("快速设置:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("初期", GUILayout.Height(18)))
                envManager.SetSeasonProgress(0.125f);
            if (GUILayout.Button("早期", GUILayout.Height(18)))
                envManager.SetSeasonProgress(0.375f);
            if (GUILayout.Button("中期", GUILayout.Height(18)))
                envManager.SetSeasonProgress(0.625f);
            if (GUILayout.Button("晚期", GUILayout.Height(18)))
                envManager.SetSeasonProgress(0.875f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
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
        
        private void DrawSystemStatus()
        {
            if (envManager.CurrentState == null)
                return;
                
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"温度: {envManager.CurrentState.temperature:F1}°C");
            EditorGUILayout.LabelField($"湿度: {envManager.CurrentState.humidity * 100:F0}%");
            EditorGUILayout.LabelField($"风速: {envManager.CurrentState.windStrength * 100:F0}%");
            EditorGUILayout.LabelField($"云层覆盖: {envManager.CurrentState.cloudCoverage * 100:F0}%");
            
            EditorGUILayout.Space();
            
            // 手动更新按钮
            if (GUILayout.Button("强制更新环境"))
            {
                envManager.UpdateEnvironment();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}