using UnityEngine;
using UnityEditor;
using WorldEditor.Core;

namespace WorldEditor.Editor
{
    /// <summary>
    /// WorldEditor设置编辑器
    /// 管理全局设置和配置
    /// </summary>
    [CustomEditor(typeof(WorldEditorSettings))]
    public class WorldEditorSettingsEditor : UnityEditor.Editor
    {
        private WorldEditorSettings settings;
        
        // UI状态
        private bool showGeneralSettings = true;
        private bool showPerformanceSettings = false;
        private bool showDebugSettings = false;
        private bool showAdvancedSettings = false;

        void OnEnable()
        {
            settings = (WorldEditorSettings)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            DrawGeneralSettings();
            DrawPerformanceSettings();
            DrawDebugSettings();
            DrawAdvancedSettings();
            DrawActionButtons();
            
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
            
            GUILayout.Label("世界编辑器设置", titleStyle);
            GUILayout.Label("全局配置和性能优化", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        void DrawGeneralSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "常规设置", true);
            
            if (showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                
                // 项目设置
                GUILayout.Label("项目设置", EditorStyles.boldLabel);
                settings.projectName = EditorGUILayout.TextField("项目名称", settings.projectName);
                settings.version = EditorGUILayout.TextField("版本号", settings.version);
                
                EditorGUILayout.Space();
                
                // 默认参数
                GUILayout.Label("默认世界参数", EditorStyles.boldLabel);
                settings.defaultWorldSize = EditorGUILayout.Vector2Field("默认世界尺寸", settings.defaultWorldSize);
                settings.defaultTerrainHeight = EditorGUILayout.FloatField("默认地形高度", settings.defaultTerrainHeight);
                
                EditorGUILayout.Space();
                
                // 自动保存
                settings.enableAutoSave = EditorGUILayout.Toggle("启用自动保存", settings.enableAutoSave);
                if (settings.enableAutoSave)
                {
                    EditorGUI.indentLevel++;
                    settings.autoSaveInterval = EditorGUILayout.IntSlider("自动保存间隔（分钟）", settings.autoSaveInterval, 1, 60);
                    EditorGUI.indentLevel--;
                }
                
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
                
                // 多线程设置
                GUILayout.Label("多线程处理", EditorStyles.boldLabel);
                settings.enableMultithreading = EditorGUILayout.Toggle("启用多线程", settings.enableMultithreading);
                if (settings.enableMultithreading)
                {
                    EditorGUI.indentLevel++;
                    settings.maxWorkerThreads = EditorGUILayout.IntSlider("最大工作线程", settings.maxWorkerThreads, 1, System.Environment.ProcessorCount);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // 内存设置
                GUILayout.Label("内存管理", EditorStyles.boldLabel);
                settings.maxMemoryUsage = EditorGUILayout.IntSlider("最大内存使用（MB）", settings.maxMemoryUsage, 512, 8192);
                settings.enableMemoryOptimization = EditorGUILayout.Toggle("启用内存优化", settings.enableMemoryOptimization);
                
                EditorGUILayout.Space();
                
                // 渲染设置
                GUILayout.Label("渲染优化", EditorStyles.boldLabel);
                settings.enableLODSystem = EditorGUILayout.Toggle("启用LOD系统", settings.enableLODSystem);
                settings.enableOcclusion = EditorGUILayout.Toggle("启用遮挡剔除", settings.enableOcclusion);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "调试设置", true);
            
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                
                // 日志设置
                GUILayout.Label("日志系统", EditorStyles.boldLabel);
                settings.enableLogging = EditorGUILayout.Toggle("启用日志", settings.enableLogging);
                if (settings.enableLogging)
                {
                    EditorGUI.indentLevel++;
                    settings.logLevel = (LogLevel)EditorGUILayout.EnumPopup("日志级别", settings.logLevel);
                    settings.logToFile = EditorGUILayout.Toggle("保存到文件", settings.logToFile);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // 可视化调试
                GUILayout.Label("可视化调试", EditorStyles.boldLabel);
                settings.showDebugGizmos = EditorGUILayout.Toggle("显示调试Gizmos", settings.showDebugGizmos);
                settings.showPerformanceStats = EditorGUILayout.Toggle("显示性能统计", settings.showPerformanceStats);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawAdvancedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", true);
            
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                // 实验性功能
                GUILayout.Label("实验性功能", EditorStyles.boldLabel);
                settings.enableExperimentalFeatures = EditorGUILayout.Toggle("启用实验性功能", settings.enableExperimentalFeatures);
                
                if (settings.enableExperimentalFeatures)
                {
                    EditorGUILayout.HelpBox("警告：实验性功能可能不稳定，请谨慎使用", MessageType.Warning);
                }
                
                EditorGUILayout.Space();
                
                // 缓存设置
                GUILayout.Label("缓存管理", EditorStyles.boldLabel);
                settings.enableCaching = EditorGUILayout.Toggle("启用缓存", settings.enableCaching);
                if (settings.enableCaching)
                {
                    EditorGUI.indentLevel++;
                    settings.cacheSize = EditorGUILayout.IntSlider("缓存大小（MB）", settings.cacheSize, 128, 2048);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("重置为默认"))
            {
                ResetToDefaults();
            }
            
            if (GUILayout.Button("导出设置"))
            {
                ExportSettings();
            }
            
            if (GUILayout.Button("导入设置"))
            {
                ImportSettings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("验证设置"))
            {
                ValidateSettings();
            }
            
            if (GUILayout.Button("应用设置"))
            {
                ApplySettings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void ResetToDefaults()
        {
            if (EditorUtility.DisplayDialog("确认重置", "这将重置所有设置为默认值，确定继续吗？", "确定", "取消"))
            {
                settings.ResetToDefaults();
                EditorUtility.SetDirty(settings);
                EditorUtility.DisplayDialog("重置完成", "设置已重置为默认值", "确定");
            }
        }

        void ExportSettings()
        {
            string path = EditorUtility.SaveFilePanel("导出设置", "", "WorldEditorSettings", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = JsonUtility.ToJson(settings, true);
                    System.IO.File.WriteAllText(path, json);
                    EditorUtility.DisplayDialog("导出成功", "设置已导出到: " + path, "确定");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("导出失败", "导出设置时发生错误: " + e.Message, "确定");
                }
            }
        }

        void ImportSettings()
        {
            string path = EditorUtility.OpenFilePanel("导入设置", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    JsonUtility.FromJsonOverwrite(json, settings);
                    EditorUtility.SetDirty(settings);
                    EditorUtility.DisplayDialog("导入成功", "设置已从文件导入: " + path, "确定");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("导入失败", "导入设置时发生错误: " + e.Message, "确定");
                }
            }
        }

        void ValidateSettings()
        {
            bool isValid = true;
            string validationMessage = "设置验证结果:\n";
            
            // 验证世界尺寸
            if (settings.defaultWorldSize.x <= 0 || settings.defaultWorldSize.y <= 0)
            {
                validationMessage += "• 世界尺寸必须大于0\n";
                isValid = false;
            }
            
            // 验证地形高度
            if (settings.defaultTerrainHeight <= 0)
            {
                validationMessage += "• 地形高度必须大于0\n";
                isValid = false;
            }
            
            // 验证线程数
            if (settings.enableMultithreading && settings.maxWorkerThreads <= 0)
            {
                validationMessage += "• 工作线程数必须大于0\n";
                isValid = false;
            }
            
            // 验证内存设置
            if (settings.maxMemoryUsage < 512)
            {
                validationMessage += "• 最大内存使用量建议不少于512MB\n";
                isValid = false;
            }
            
            if (isValid)
            {
                validationMessage += "所有设置验证通过！";
                EditorUtility.DisplayDialog("验证通过", validationMessage, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("验证失败", validationMessage, "确定");
            }
        }

        void ApplySettings()
        {
            // 应用设置到运行时系统
            EditorUtility.SetDirty(settings);
            
            // 如果在运行时，可以立即应用某些设置
            if (Application.isPlaying)
            {
                // 应用运行时设置
                ApplyRuntimeSettings();
            }
            
            EditorUtility.DisplayDialog("应用完成", "设置已应用", "确定");
        }

        void ApplyRuntimeSettings()
        {
            // 应用日志设置
            if (settings.enableLogging)
            {
                Debug.Log($"[WorldEditor] 日志系统已启用，级别: {settings.logLevel}");
            }
            
            // 应用性能设置
            if (settings.enableMultithreading)
            {
                Debug.Log($"[WorldEditor] 多线程已启用，最大线程数: {settings.maxWorkerThreads}");
            }
        }
    }
}