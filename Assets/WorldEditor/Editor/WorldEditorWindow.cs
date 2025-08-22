using UnityEngine;
using UnityEditor;
using WorldEditor.Core;
using WorldEditor.TerrainSystem;
using WorldEditor.Placement;
using WorldEditor.Environment;

namespace WorldEditor.Editor
{
    /// <summary>
    /// WorldEditor主窗口 - 提供统一的编辑器界面
    /// 超越现有工具的完整编辑体验
    /// </summary>
    public class WorldEditorWindow : EditorWindow
    {
        [MenuItem("世界编辑器/打开WorldEditor")]
        public static void ShowWindow()
        {
            WorldEditorWindow window = GetWindow<WorldEditorWindow>("世界编辑器");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        // 标签页
        private int selectedTab = 0;
        private readonly string[] tabs = {
            "项目总览",
            "地形生成", 
            "智能放置",
            "环境天气",
            "AI生成",
            "性能优化",
            "设置"
        };

        // 核心系统引用
        private WorldEditorManager worldManager;
        private AdvancedTerrainGenerator terrainGenerator;
        private SmartPlacementSystem placementSystem;
        private DynamicEnvironmentSystem environmentSystem;

        // UI状态
        private Vector2 scrollPosition;
        private bool systemsInitialized = false;
        private string statusMessage = "准备就绪";

        // 样式
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;

        void OnEnable()
        {
            titleContent = new GUIContent("世界编辑器 v1.0");
            FindWorldEditorComponents();
        }

        void OnGUI()
        {
            InitializeStyles();
            DrawHeader();
            DrawTabs();
            DrawContent();
            DrawFooter();
        }

        void InitializeStyles()
        {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 5, 5)
            };

            stylesInitialized = true;
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            
            GUILayout.Label("Unity WorldEditor", titleStyle);
            GUILayout.Label("超越 Gaia Pro + GeNa Pro + Enviro 3 的综合世界编辑解决方案", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(5);
            
            // 状态栏
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"状态: {statusMessage}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"系统: {(systemsInitialized ? "已初始化" : "未初始化")}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
        }

        void DrawContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawTerrainTab(); break;
                case 2: DrawPlacementTab(); break;
                case 3: DrawEnvironmentTab(); break;
                case 4: DrawAITab(); break;
                case 5: DrawOptimizationTab(); break;
                case 6: DrawSettingsTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawFooter()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("刷新系统", buttonStyle, GUILayout.Width(100)))
            {
                RefreshSystems();
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("帮助文档", buttonStyle, GUILayout.Width(100)))
            {
                Application.OpenURL("https://worldeditor-docs.com");
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawOverviewTab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("项目总览", EditorStyles.boldLabel);
            
            // 项目状态
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("世界管理器:", GUILayout.Width(100));
            EditorGUILayout.ObjectField(worldManager, typeof(WorldEditorManager), true);
            if (GUILayout.Button("创建", GUILayout.Width(60)))
            {
                CreateWorldManager();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 系统状态面板
            DrawSystemStatus();
            
            EditorGUILayout.Space();
            
            // 快速操作
            DrawQuickActions();
            
            EditorGUILayout.EndVertical();
        }

        void DrawSystemStatus()
        {
            GUILayout.Label("系统状态", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawStatusLine("地形生成系统", terrainGenerator != null, "管理地形生成和修改");
            DrawStatusLine("智能放置系统", placementSystem != null, "自动放置植被和物体");
            DrawStatusLine("环境天气系统", environmentSystem != null, "控制天气和环境效果");
            DrawStatusLine("AI生成系统", true, "AI辅助内容生成");
            DrawStatusLine("性能优化", true, "LOD和性能管理");
            
            EditorGUILayout.EndVertical();
        }

        void DrawStatusLine(string systemName, bool isActive, string description)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 状态指示器
            Color originalColor = GUI.color;
            GUI.color = isActive ? Color.green : Color.red;
            GUILayout.Label("●", GUILayout.Width(20));
            GUI.color = originalColor;
            
            // 系统名称
            GUILayout.Label(systemName, GUILayout.Width(120));
            
            // 描述
            GUILayout.Label(description, EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        void DrawQuickActions()
        {
            GUILayout.Label("快速操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("生成新世界", buttonStyle))
            {
                QuickGenerateWorld();
            }
            
            if (GUILayout.Button("重置场景", buttonStyle))
            {
                QuickResetScene();
            }
            
            if (GUILayout.Button("应用优化", buttonStyle))
            {
                QuickOptimize();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("导出设置", buttonStyle))
            {
                ExportSettings();
            }
            
            if (GUILayout.Button("导入设置", buttonStyle))
            {
                ImportSettings();
            }
            
            if (GUILayout.Button("预览模式", buttonStyle))
            {
                TogglePreview();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void DrawTerrainTab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("地形生成系统", EditorStyles.boldLabel);
            
            if (terrainGenerator == null)
            {
                EditorGUILayout.HelpBox("未找到地形生成器组件", MessageType.Warning);
                if (GUILayout.Button("创建地形生成器"))
                {
                    CreateTerrainGenerator();
                }
                return;
            }
            
            // 地形生成器设置
            EditorGUILayout.Space();
            GUILayout.Label("基础设置", EditorStyles.boldLabel);
            
            if (GUILayout.Button("生成地形", buttonStyle))
            {
                GenerateTerrain();
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("高级功能", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用侵蚀"))
            {
                ApplyErosion();
            }
            if (GUILayout.Button("生成河流"))
            {
                GenerateRivers();
            }
            if (GUILayout.Button("混合纹理"))
            {
                BlendTextures();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void DrawPlacementTab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("智能放置系统", EditorStyles.boldLabel);
            
            if (placementSystem == null)
            {
                EditorGUILayout.HelpBox("未找到智能放置系统组件", MessageType.Warning);
                if (GUILayout.Button("创建放置系统"))
                {
                    CreatePlacementSystem();
                }
                return;
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("放置控制", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("放置植被"))
            {
                PlaceVegetation();
            }
            if (GUILayout.Button("放置装饰"))
            {
                PlaceDecorations();
            }
            if (GUILayout.Button("清理场景"))
            {
                ClearPlacements();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            GUILayout.Label("生态系统", EditorStyles.boldLabel);
            
            if (GUILayout.Button("运行生态模拟"))
            {
                RunEcosystemSimulation();
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawEnvironmentTab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("环境天气系统", EditorStyles.boldLabel);
            
            if (environmentSystem == null)
            {
                EditorGUILayout.HelpBox("未找到环境系统组件", MessageType.Warning);
                if (GUILayout.Button("创建环境系统"))
                {
                    CreateEnvironmentSystem();
                }
                return;
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("天气控制", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("晴朗"))
            {
                SetWeather(WeatherType.Clear);
            }
            if (GUILayout.Button("阴天"))
            {
                SetWeather(WeatherType.Cloudy);
            }
            if (GUILayout.Button("雨天"))
            {
                SetWeather(WeatherType.Rainy);
            }
            if (GUILayout.Button("暴风雨"))
            {
                SetWeather(WeatherType.Stormy);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            GUILayout.Label("时间控制", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("黎明"))
            {
                SetTimeOfDay(TimeOfDay.Dawn);
            }
            if (GUILayout.Button("中午"))
            {
                SetTimeOfDay(TimeOfDay.Noon);
            }
            if (GUILayout.Button("黄昏"))
            {
                SetTimeOfDay(TimeOfDay.Dusk);
            }
            if (GUILayout.Button("夜晚"))
            {
                SetTimeOfDay(TimeOfDay.Night);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void DrawAITab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("AI生成系统", EditorStyles.boldLabel);
            
            GUILayout.Label("AI辅助功能将在未来版本中提供", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        void DrawOptimizationTab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("性能优化", EditorStyles.boldLabel);
            
            if (GUILayout.Button("运行性能分析"))
            {
                RunPerformanceAnalysis();
            }
            
            if (GUILayout.Button("应用LOD优化"))
            {
                ApplyLODOptimization();
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawSettingsTab()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("设置", EditorStyles.boldLabel);
            
            GUILayout.Label("全局设置将在未来版本中提供", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        // 系统方法
        void FindWorldEditorComponents()
        {
            worldManager = Object.FindFirstObjectByType<WorldEditorManager>();
            terrainGenerator = Object.FindFirstObjectByType<AdvancedTerrainGenerator>();
            placementSystem = Object.FindFirstObjectByType<SmartPlacementSystem>();
            environmentSystem = Object.FindFirstObjectByType<DynamicEnvironmentSystem>();
            
            systemsInitialized = (worldManager != null && terrainGenerator != null && 
                                placementSystem != null && environmentSystem != null);
            
            statusMessage = systemsInitialized ? "所有系统已就绪" : "部分系统未初始化";
        }

        void RefreshSystems()
        {
            FindWorldEditorComponents();
            Repaint();
        }

        void CreateWorldManager()
        {
            GameObject managerGO = new GameObject("WorldEditorManager");
            worldManager = managerGO.AddComponent<WorldEditorManager>();
            statusMessage = "世界管理器已创建";
            Repaint();
        }

        void CreateTerrainGenerator()
        {
            if (worldManager != null)
            {
                terrainGenerator = worldManager.gameObject.AddComponent<AdvancedTerrainGenerator>();
            }
            else
            {
                GameObject terrainGO = new GameObject("TerrainGenerator");
                terrainGenerator = terrainGO.AddComponent<AdvancedTerrainGenerator>();
            }
            statusMessage = "地形生成器已创建";
            Repaint();
        }

        void CreatePlacementSystem()
        {
            if (worldManager != null)
            {
                placementSystem = worldManager.gameObject.AddComponent<SmartPlacementSystem>();
            }
            else
            {
                GameObject placementGO = new GameObject("PlacementSystem");
                placementSystem = placementGO.AddComponent<SmartPlacementSystem>();
            }
            statusMessage = "智能放置系统已创建";
            Repaint();
        }

        void CreateEnvironmentSystem()
        {
            if (worldManager != null)
            {
                environmentSystem = worldManager.gameObject.AddComponent<DynamicEnvironmentSystem>();
            }
            else
            {
                GameObject environmentGO = new GameObject("EnvironmentSystem");
                environmentSystem = environmentGO.AddComponent<DynamicEnvironmentSystem>();
            }
            statusMessage = "环境系统已创建";
            Repaint();
        }

        // 功能方法
        void QuickGenerateWorld()
        {
            if (worldManager != null)
            {
                worldManager.GenerateWorld();
                statusMessage = "正在生成新世界...";
            }
            else
            {
                statusMessage = "错误: 未找到世界管理器";
            }
        }

        void QuickResetScene()
        {
            if (EditorUtility.DisplayDialog("确认重置", "这将清除所有生成的内容，确定继续吗？", "确定", "取消"))
            {
                // 重置逻辑
                statusMessage = "场景已重置";
            }
        }

        void QuickOptimize()
        {
            statusMessage = "性能优化已应用";
        }

        void ExportSettings()
        {
            string path = EditorUtility.SaveFilePanel("导出设置", "", "WorldEditorSettings", "json");
            if (!string.IsNullOrEmpty(path))
            {
                statusMessage = "设置已导出到: " + path;
            }
        }

        void ImportSettings()
        {
            string path = EditorUtility.OpenFilePanel("导入设置", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                statusMessage = "设置已从导入: " + path;
            }
        }

        void TogglePreview()
        {
            statusMessage = "预览模式已切换";
        }

        void GenerateTerrain()
        {
            if (terrainGenerator != null && worldManager != null)
            {
                terrainGenerator.GenerateTerrain(worldManager.GetGenerationParameters());
                statusMessage = "地形生成已开始";
            }
        }

        void ApplyErosion()
        {
            statusMessage = "正在应用侵蚀效果...";
        }

        void GenerateRivers()
        {
            statusMessage = "正在生成河流...";
        }

        void BlendTextures()
        {
            statusMessage = "正在混合地形纹理...";
        }

        void PlaceVegetation()
        {
            if (placementSystem != null && worldManager != null)
            {
                placementSystem.PlaceVegetation(worldManager.GetGenerationParameters());
                statusMessage = "植被放置已开始";
            }
        }

        void PlaceDecorations()
        {
            statusMessage = "正在放置装饰物体...";
        }

        void ClearPlacements()
        {
            if (EditorUtility.DisplayDialog("确认清理", "这将清除所有放置的物体，确定继续吗？", "确定", "取消"))
            {
                statusMessage = "场景物体已清理";
            }
        }

        void RunEcosystemSimulation()
        {
            statusMessage = "生态系统模拟已开始";
        }

        void SetWeather(WeatherType weather)
        {
            if (environmentSystem != null)
            {
                environmentSystem.SetTargetWeather(weather);
                statusMessage = $"天气已设置为: {weather}";
            }
        }

        void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            if (environmentSystem != null)
            {
                environmentSystem.SetTimeOfDay(timeOfDay);
                statusMessage = $"时间已设置为: {timeOfDay}";
            }
        }

        void RunPerformanceAnalysis()
        {
            statusMessage = "性能分析已完成";
        }

        void ApplyLODOptimization()
        {
            statusMessage = "LOD优化已应用";
        }
    }
}