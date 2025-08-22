using UnityEngine;
using UnityEditor;
using WorldEditor.Core;
using WorldEditor.TerrainSystem;
using WorldEditor.Placement;
using WorldEditor.Environment;

namespace WorldEditor.Editor
{
    /// <summary>
    /// WorldEditor菜单项和工具栏
    /// 提供便捷的菜单访问
    /// </summary>
    public static class WorldEditorMenus
    {
        #region 主菜单项

        [MenuItem("世界编辑器/主窗口", priority = 1)]
        public static void OpenMainWindow()
        {
            WorldEditorWindow.ShowWindow();
        }

        [MenuItem("世界编辑器/创建新世界项目", priority = 2)]
        public static void CreateNewWorldProject()
        {
            CreateWorldProjectWizard();
        }



        [MenuItem("世界编辑器/文档和帮助", priority = 100)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://worldeditor-docs.com");
        }

        #endregion

        #region 系统创建菜单

        [MenuItem("世界编辑器/系统/创建世界管理器", priority = 10)]
        public static void CreateWorldManager()
        {
            GameObject managerGO = new GameObject("WorldEditorManager");
            managerGO.AddComponent<WorldEditorManager>();
            
            // 添加基础组件
            managerGO.AddComponent<AdvancedTerrainGenerator>();
            managerGO.AddComponent<SmartPlacementSystem>();
            managerGO.AddComponent<EnvironmentManager>();
            
            Selection.activeGameObject = managerGO;
            EditorGUIUtility.PingObject(managerGO);
            
            EditorUtility.DisplayDialog("创建完成", "世界编辑器管理器已创建完成", "确定");
        }

        [MenuItem("世界编辑器/系统/创建地形生成器", priority = 11)]
        public static void CreateTerrainGenerator()
        {
            GameObject terrainGO = new GameObject("TerrainGenerator");
            var generator = terrainGO.AddComponent<AdvancedTerrainGenerator>();
            
            // 自动创建地形组件
            var terrainData = new UnityEngine.TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000, 600, 1000);
            var terrain = Terrain.CreateTerrainGameObject(terrainData);
            terrain.name = "Generated Terrain";
            terrain.transform.SetParent(terrainGO.transform);
            
            Selection.activeGameObject = terrainGO;
            EditorGUIUtility.PingObject(terrainGO);
            
            EditorUtility.DisplayDialog("创建完成", "地形生成器已创建完成", "确定");
        }

        [MenuItem("世界编辑器/系统/创建智能放置系统", priority = 12)]
        public static void CreatePlacementSystem()
        {
            GameObject placementGO = new GameObject("SmartPlacementSystem");
            var placement = placementGO.AddComponent<SmartPlacementSystem>();
            
            Selection.activeGameObject = placementGO;
            EditorGUIUtility.PingObject(placementGO);
            
            EditorUtility.DisplayDialog("创建完成", "智能放置系统已创建完成", "确定");
        }

        [MenuItem("世界编辑器/系统/创建环境系统", priority = 13)]
        public static void CreateEnvironmentSystem()
        {
            GameObject envGO = new GameObject("EnvironmentSystem");
            var environment = envGO.AddComponent<EnvironmentManager>();
            
            Selection.activeGameObject = envGO;
            EditorGUIUtility.PingObject(envGO);
            
            EditorUtility.DisplayDialog("创建完成", "动态环境系统已创建完成", "确定");
        }

        #endregion

        #region 快速操作菜单

        [MenuItem("世界编辑器/快速操作/生成新世界", priority = 20)]
        public static void QuickGenerateWorld()
        {
            var worldManager = Object.FindFirstObjectByType<WorldEditorManager>();
            if (worldManager != null)
            {
                worldManager.GenerateWorld();
                EditorUtility.DisplayDialog("生成开始", "新世界生成已开始", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到世界管理器", "确定");
            }
        }

        [MenuItem("世界编辑器/快速操作/生成地形", priority = 21)]
        public static void QuickGenerateTerrain()
        {
            var terrainGen = Object.FindFirstObjectByType<AdvancedTerrainGenerator>();
            if (terrainGen != null)
            {
                var worldManager = Object.FindFirstObjectByType<WorldEditorManager>();
                if (worldManager != null)
                {
                    terrainGen.GenerateTerrain(worldManager.GetGenerationParameters());
                    EditorUtility.DisplayDialog("生成开始", "地形生成已开始", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "需要世界管理器提供参数", "确定");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到地形生成器", "确定");
            }
        }

        [MenuItem("世界编辑器/快速操作/放置植被", priority = 22)]
        public static void QuickPlaceVegetation()
        {
            var placementSystem = Object.FindFirstObjectByType<SmartPlacementSystem>();
            if (placementSystem != null)
            {
                var worldManager = Object.FindFirstObjectByType<WorldEditorManager>();
                if (worldManager != null)
                {
                    placementSystem.PlaceVegetation(worldManager.GetGenerationParameters());
                    EditorUtility.DisplayDialog("放置开始", "植被放置已开始", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "需要世界管理器提供参数", "确定");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到智能放置系统", "确定");
            }
        }

        [MenuItem("世界编辑器/快速操作/清理场景", priority = 23)]
        public static void QuickClearScene()
        {
            if (EditorUtility.DisplayDialog("确认清理", "这将清除所有生成的内容，确定继续吗？", "确定", "取消"))
            {
                ClearGeneratedContent();
                EditorUtility.DisplayDialog("清理完成", "场景已清理", "确定");
            }
        }

        #endregion

        #region 工具菜单

        [MenuItem("世界编辑器/工具/性能分析器", priority = 30)]
        public static void OpenPerformanceAnalyzer()
        {
            EditorUtility.DisplayDialog("功能开发中", "性能分析器正在开发中", "确定");
        }

        [MenuItem("世界编辑器/工具/预设管理器", priority = 31)]
        public static void OpenPresetManager()
        {
            EditorUtility.DisplayDialog("功能开发中", "预设管理器正在开发中", "确定");
        }

        [MenuItem("世界编辑器/工具/批处理工具", priority = 32)]
        public static void OpenBatchProcessor()
        {
            EditorUtility.DisplayDialog("功能开发中", "批处理工具正在开发中", "确定");
        }

        [MenuItem("世界编辑器/工具/统计报告", priority = 33)]
        public static void GenerateStatsReport()
        {
            GenerateSystemStatsReport();
        }

        #endregion

        #region 验证菜单项

        [MenuItem("世界编辑器/快速操作/生成新世界", true)]
        public static bool ValidateQuickGenerateWorld()
        {
            return Object.FindFirstObjectByType<WorldEditorManager>() != null;
        }

        [MenuItem("世界编辑器/快速操作/生成地形", true)]
        public static bool ValidateQuickGenerateTerrain()
        {
            return Object.FindFirstObjectByType<AdvancedTerrainGenerator>() != null;
        }

        [MenuItem("世界编辑器/快速操作/放置植被", true)]
        public static bool ValidateQuickPlaceVegetation()
        {
            return Object.FindFirstObjectByType<SmartPlacementSystem>() != null;
        }

        #endregion

        #region 私有方法

        static void CreateWorldProjectWizard()
        {
            if (EditorUtility.DisplayDialog(
                "创建新世界项目",
                "这将创建一个完整的世界编辑器项目结构，包括：\n" +
                "• 世界管理器\n" +
                "• 地形生成系统\n" +
                "• 智能放置系统\n" +
                "• 环境系统\n" +
                "\n确定创建吗？",
                "创建",
                "取消"))
            {
                CreateCompleteWorldProject();
            }
        }

        static void CreateCompleteWorldProject()
        {
            // 创建主容器
            GameObject projectRoot = new GameObject("WorldEditorProject");
            
            // 添加世界管理器
            var worldManager = projectRoot.AddComponent<WorldEditorManager>();
            
            // 创建地形系统
            GameObject terrainSystemGO = new GameObject("TerrainSystem");
            terrainSystemGO.transform.SetParent(projectRoot.transform);
            var terrainGen = terrainSystemGO.AddComponent<AdvancedTerrainGenerator>();
            
            // 创建Unity地形
            var terrainData = new UnityEngine.TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000, 600, 1000);
            var terrain = Terrain.CreateTerrainGameObject(terrainData);
            terrain.name = "MainTerrain";
            terrain.transform.SetParent(terrainSystemGO.transform);
            
            // 创建放置系统
            GameObject placementSystemGO = new GameObject("PlacementSystem");
            placementSystemGO.transform.SetParent(projectRoot.transform);
            var placementSystem = placementSystemGO.AddComponent<SmartPlacementSystem>();
            
            // 创建环境系统
            GameObject environmentSystemGO = new GameObject("EnvironmentSystem");
            environmentSystemGO.transform.SetParent(projectRoot.transform);
            var environmentSystem = environmentSystemGO.AddComponent<EnvironmentManager>();
            
            // 创建光照
            GameObject lightGO = new GameObject("Main Light");
            lightGO.transform.SetParent(environmentSystemGO.transform);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            
            // 选中项目根对象
            Selection.activeGameObject = projectRoot;
            EditorGUIUtility.PingObject(projectRoot);
            
            EditorUtility.DisplayDialog("创建完成", "世界编辑器项目已创建完成！", "确定");
        }

        static void ClearGeneratedContent()
        {
            // 清理逻辑
            var allTerrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in allTerrains)
            {
                if (terrain.terrainData != null)
                {
                    // 重置地形
                    int resolution = terrain.terrainData.heightmapResolution;
                    float[,] heights = new float[resolution, resolution];
                    terrain.terrainData.SetHeights(0, 0, heights);
                }
            }
            
            // 清理放置的对象
            // 这里应该实现清理放置对象的逻辑
        }

        static void GenerateSystemStatsReport()
        {
            string report = "世界编辑器系统统计报告\n";
            report += "====================================\n\n";
            
            // 系统状态
            var worldManager = Object.FindFirstObjectByType<WorldEditorManager>();
            var terrainGen = Object.FindFirstObjectByType<AdvancedTerrainGenerator>();
            var placementSystem = Object.FindFirstObjectByType<SmartPlacementSystem>();
            var environmentSystem = Object.FindFirstObjectByType<EnvironmentManager>();
            
            report += $"世界管理器: {(worldManager != null ? "已安装" : "未找到")}\n";
            report += $"地形生成器: {(terrainGen != null ? "已安装" : "未找到")}\n";
            report += $"智能放置系统: {(placementSystem != null ? "已安装" : "未找到")}\n";
            report += $"环境系统: {(environmentSystem != null ? "已安装" : "未找到")}\n\n";
            
            // 场景统计
            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            report += $"地形数量: {terrains.Length}\n";
            
            report += $"\n生成时间: {System.DateTime.Now}\n";
            
            // 保存报告
            string path = EditorUtility.SaveFilePanel("保存统计报告", "", "WorldEditor_Stats", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                EditorUtility.DisplayDialog("报告生成", "统计报告已保存到: " + path, "确定");
            }
        }

        static T FindObjectOfType<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }

        static T[] FindObjectsOfType<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        #endregion

        #region 上下文菜单

        [MenuItem("GameObject/世界编辑器/添加到世界管理器", false, 0)]
        static void AddToWorldManager(MenuCommand menuCommand)
        {
            GameObject go = menuCommand.context as GameObject;
            var worldManager = Object.FindFirstObjectByType<WorldEditorManager>();
            
            if (worldManager != null && go != null)
            {
                go.transform.SetParent(worldManager.transform);
                EditorUtility.DisplayDialog("添加完成", $"{go.name} 已添加到世界管理器", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到世界管理器或选中对象", "确定");
            }
        }

        [MenuItem("GameObject/世界编辑器/添加到世界管理器", true)]
        static bool ValidateAddToWorldManager()
        {
            return Selection.activeGameObject != null && Object.FindFirstObjectByType<WorldEditorManager>() != null;
        }

        #endregion
    }
}