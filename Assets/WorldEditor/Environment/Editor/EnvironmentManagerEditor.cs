using UnityEngine;
using UnityEditor;
using WorldEditor.Environment;

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
            
            // 运行时测试控制
            if (Application.isPlaying)
            {
                DrawRuntimeControls();
            }
            else
            {
                DrawEditorControls();
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
                
                // 天气控制
                EditorGUILayout.LabelField("天气控制", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("晴天"))
                    environmentManager.SetWeather(WeatherType.Clear);
                if (GUILayout.Button("多云"))
                    environmentManager.SetWeather(WeatherType.Cloudy);
                if (GUILayout.Button("雨天"))
                    environmentManager.SetWeather(WeatherType.Rainy);
                if (GUILayout.Button("雪天"))
                    environmentManager.SetWeather(WeatherType.Snowy);
                EditorGUILayout.EndHorizontal();
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
    }
}