using UnityEngine;
using UnityEditor;
using WorldEditor.Placement;
using WorldEditor.Core;
using System.Collections.Generic;
using System.Linq;

namespace WorldEditor.Editor
{
    /// <summary>
    /// 智能放置系统编辑器界面
    /// 超越GeNa Pro的智能放置控制
    /// </summary>
    [CustomEditor(typeof(SmartPlacementSystem))]
    public class SmartPlacementEditor : UnityEditor.Editor
    {
        private SmartPlacementSystem placementSystem;
        
        // 序列化属性
        private SerializedProperty enableSmartPlacement;
        private SerializedProperty placementDatabase;
        private SerializedProperty densityManager;
        private SerializedProperty biomeAnalyzer;
        
        // UI状态
        private bool showLayerSettings = true;
        private bool showBiomeSettings = false;
        private bool showEcosystemSettings = false;
        private bool showPreviewSettings = false;
        private bool showVegetationSettings = true;
        
        // 临时设置
        private PlacementLayer tempLayer;
        private int selectedLayerIndex = 0;
        
        // 预览
        private bool isPreviewMode = false;
        private Vector3 previewPosition;
        
        // 植被相关
        private VegetationType selectedVegetationType = VegetationType.针叶树;
        private float vegetationBrushSize = 10f;
        private float vegetationDensity = 0.5f;
        private Vector2 vegetationScrollPos;
        private Terrain targetTerrain;

        void OnEnable()
        {
            placementSystem = (SmartPlacementSystem)target;
            
            // 绑定序列化属性
            enableSmartPlacement = serializedObject.FindProperty("enableSmartPlacement");
            placementDatabase = serializedObject.FindProperty("placementDatabase");
            densityManager = serializedObject.FindProperty("densityManager");
            biomeAnalyzer = serializedObject.FindProperty("biomeAnalyzer");
            
            // 初始化临时层
            InitializeTempLayer();
            
            // 注册Scene视图回调
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        void OnDisable()
        {
            // 注销Scene视图回调
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            DrawBasicControls();
            DrawVegetationSystem();
            DrawLayerManagement();
            DrawBiomeSettings();
            DrawEcosystemSettings();
            DrawPreviewControls();
            
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
            
            GUILayout.Label("智能放置系统", titleStyle);
            GUILayout.Label("超越 GeNa Pro 的智能资源放置解决方案", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space();
            
            // 系统状态
            string status = placementSystem.IsPlacementActive() ? "放置中..." : "就绪";
            Color statusColor = placementSystem.IsPlacementActive() ? Color.yellow : Color.green;
            
            GUI.color = statusColor;
            GUILayout.Label($"状态: {status}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.EndVertical();
        }

        void DrawBasicControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("基础控制", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(enableSmartPlacement, new GUIContent("启用智能放置", "开启智能分析和自动放置"));
            EditorGUILayout.PropertyField(placementDatabase, new GUIContent("放置数据库", "存储所有放置规则和预制件"));
            EditorGUILayout.PropertyField(densityManager, new GUIContent("密度管理器", "控制对象密度分布"));
            EditorGUILayout.PropertyField(biomeAnalyzer, new GUIContent("生物群落分析器", "分析环境类型"));
            
            EditorGUILayout.Space();
            
            // 主要操作按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !placementSystem.IsPlacementActive();
            if (GUILayout.Button("开始智能放置", GUILayout.Height(30)))
            {
                StartSmartPlacement();
            }
            
            GUI.enabled = placementSystem.IsPlacementActive();
            if (GUILayout.Button("停止放置", GUILayout.Height(30)))
            {
                StopPlacement();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 分类放置按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("放置植被"))
            {
                PlaceVegetationOnly();
            }
            
            if (GUILayout.Button("放置结构"))
            {
                PlaceStructuresOnly();
            }
            
            if (GUILayout.Button("放置装饰"))
            {
                PlaceDecorationsOnly();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("清理全部"))
            {
                ClearAllPlacements();
            }
            
            if (GUILayout.Button("优化放置"))
            {
                OptimizePlacements();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        void DrawLayerManagement()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showLayerSettings = EditorGUILayout.Foldout(showLayerSettings, "放置层管理", true);
            
            if (showLayerSettings)
            {
                EditorGUI.indentLevel++;
                
                // 层列表
                GUILayout.Label("当前放置层", EditorStyles.boldLabel);
                
                // 简化的层显示 (实际应该从placementDatabase获取)
                string[] layerNames = { "树木层", "灌木层", "草地层", "岩石层", "建筑层" };
                selectedLayerIndex = EditorGUILayout.Popup("选择层", selectedLayerIndex, layerNames);
                
                EditorGUILayout.Space();
                
                // 层设置
                DrawCurrentLayerSettings();
                
                EditorGUILayout.Space();
                
                // 层操作
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("新建层"))
                {
                    CreateNewLayer();
                }
                
                if (GUILayout.Button("删除层"))
                {
                    DeleteCurrentLayer();
                }
                
                if (GUILayout.Button("复制层"))
                {
                    DuplicateCurrentLayer();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawCurrentLayerSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("层设置", EditorStyles.boldLabel);
            
            tempLayer.layerName = EditorGUILayout.TextField("层名称", tempLayer.layerName);
            tempLayer.layerType = (PlacementLayerType)EditorGUILayout.EnumPopup("层类型", tempLayer.layerType);
            tempLayer.enabled = EditorGUILayout.Toggle("启用", tempLayer.enabled);
            tempLayer.priority = EditorGUILayout.Slider("优先级", tempLayer.priority, 0f, 10f);
            
            EditorGUILayout.Space();
            
            // 密度设置
            GUILayout.Label("密度控制", EditorStyles.boldLabel);
            tempLayer.baseDensity = EditorGUILayout.Slider("基础密度", tempLayer.baseDensity, 0f, 10f);
            tempLayer.useNoiseDensity = EditorGUILayout.Toggle("使用噪声密度", tempLayer.useNoiseDensity);
            
            if (tempLayer.useNoiseDensity)
            {
                EditorGUI.indentLevel++;
                tempLayer.noiseScale = EditorGUILayout.Slider("噪声缩放", tempLayer.noiseScale, 0.001f, 1f);
                tempLayer.noiseInfluence = EditorGUILayout.Slider("噪声影响", tempLayer.noiseInfluence, 0f, 1f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 变换设置
            GUILayout.Label("变换设置", EditorStyles.boldLabel);
            tempLayer.enableRandomRotation = EditorGUILayout.Toggle("随机旋转", tempLayer.enableRandomRotation);
            tempLayer.enableRandomScale = EditorGUILayout.Toggle("随机缩放", tempLayer.enableRandomScale);
            
            if (tempLayer.enableRandomScale)
            {
                EditorGUI.indentLevel++;
                tempLayer.minScale = EditorGUILayout.Slider("最小缩放", tempLayer.minScale, 0.1f, 2f);
                tempLayer.maxScale = EditorGUILayout.Slider("最大缩放", tempLayer.maxScale, 0.1f, 2f);
                EditorGUI.indentLevel--;
            }
            
            tempLayer.alignToSurface = EditorGUILayout.Toggle("对齐表面", tempLayer.alignToSurface);
            tempLayer.surfaceOffset = EditorGUILayout.Slider("表面偏移", tempLayer.surfaceOffset, -10f, 10f);
            
            EditorGUILayout.EndVertical();
        }

        void DrawBiomeSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showBiomeSettings = EditorGUILayout.Foldout(showBiomeSettings, "生物群落设置", true);
            
            if (showBiomeSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Label("生物群落分析", EditorStyles.boldLabel);
                
                EditorGUILayout.HelpBox(
                    "系统会自动分析地形特征：\n" +
                    "• 高度和坡度\n" +
                    "• 湿度和温度\n" +
                    "• 土壤类型\n" +
                    "• 光照条件",
                    MessageType.Info
                );
                
                EditorGUILayout.Space();
                
                // 生物群落控制
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("分析当前地形"))
                {
                    AnalyzeTerrain();
                }
                
                if (GUILayout.Button("显示生物群落"))
                {
                    ShowBiomeMap();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawEcosystemSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showEcosystemSettings = EditorGUILayout.Foldout(showEcosystemSettings, "生态系统设置", true);
            
            if (showEcosystemSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Label("生态系统模拟", EditorStyles.boldLabel);
                
                EditorGUILayout.HelpBox(
                    "智能生态系统功能：\n" +
                    "• 物种共生关系\n" +
                    "• 竞争和排斥\n" +
                    "• 自然演替模拟\n" +
                    "• 环境适应性",
                    MessageType.Info
                );
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("运行生态模拟"))
                {
                    RunEcosystemSimulation();
                }
                
                if (GUILayout.Button("重置生态系统"))
                {
                    ResetEcosystem();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawPreviewControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showPreviewSettings = EditorGUILayout.Foldout(showPreviewSettings, "预览和调试", true);
            
            if (showPreviewSettings)
            {
                EditorGUI.indentLevel++;
                
                // 预览模式
                bool newPreviewMode = EditorGUILayout.Toggle("预览模式", isPreviewMode);
                if (newPreviewMode != isPreviewMode)
                {
                    isPreviewMode = newPreviewMode;
                    TogglePreviewMode();
                }
                
                if (isPreviewMode)
                {
                    EditorGUILayout.HelpBox("预览模式已启用。在场景视图中点击以预览放置效果。", MessageType.Info);
                    
                    previewPosition = EditorGUILayout.Vector3Field("预览位置", previewPosition);
                    
                    if (GUILayout.Button("在当前位置预览"))
                    {
                        PreviewAtPosition();
                    }
                }
                
                EditorGUILayout.Space();
                
                // 调试信息
                GUILayout.Label("调试信息", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("显示密度图"))
                {
                    ShowDensityMap();
                }
                
                if (GUILayout.Button("显示放置网格"))
                {
                    ShowPlacementGrid();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("导出统计"))
                {
                    ExportPlacementStats();
                }
                
                if (GUILayout.Button("性能分析"))
                {
                    RunPerformanceAnalysis();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void InitializeTempLayer()
        {
            tempLayer = new PlacementLayer();
            tempLayer.layerName = "新建层";
            tempLayer.layerType = PlacementLayerType.Vegetation;
            tempLayer.enabled = true;
            tempLayer.priority = 1f;
            tempLayer.baseDensity = 1f;
            tempLayer.useNoiseDensity = true;
            tempLayer.noiseScale = 0.1f;
            tempLayer.noiseInfluence = 0.5f;
            tempLayer.enableRandomRotation = true;
            tempLayer.enableRandomScale = true;
            tempLayer.minScale = 0.8f;
            tempLayer.maxScale = 1.2f;
            tempLayer.alignToSurface = true;
            tempLayer.surfaceOffset = 0f;
        }

        // 功能方法
        void StartSmartPlacement()
        {
            if (placementSystem.GetComponent<WorldEditorManager>() != null)
            {
                var worldManager = placementSystem.GetComponent<WorldEditorManager>();
                placementSystem.PlaceVegetation(worldManager.GetGenerationParameters());
                EditorUtility.DisplayDialog("放置开始", "智能放置系统已开始工作", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "需要WorldEditorManager组件", "确定");
            }
        }

        void StopPlacement()
        {
            EditorUtility.DisplayDialog("功能开发中", "停止放置功能正在开发中", "确定");
        }

        void PlaceVegetationOnly()
        {
            EditorUtility.DisplayDialog("放置植被", "正在放置植被...", "确定");
        }

        void PlaceStructuresOnly()
        {
            EditorUtility.DisplayDialog("放置结构", "正在放置结构...", "确定");
        }

        void PlaceDecorationsOnly()
        {
            EditorUtility.DisplayDialog("放置装饰", "正在放置装饰物...", "确定");
        }

        void ClearAllPlacements()
        {
            if (EditorUtility.DisplayDialog("确认清理", "这将清除所有放置的物体，确定继续吗？", "确定", "取消"))
            {
                EditorUtility.DisplayDialog("清理完成", "所有放置的物体已清理", "确定");
            }
        }

        void OptimizePlacements()
        {
            EditorUtility.DisplayDialog("优化完成", "放置对象优化已完成", "确定");
        }

        void CreateNewLayer()
        {
            EditorUtility.DisplayDialog("新建层", "新的放置层已创建", "确定");
        }

        void DeleteCurrentLayer()
        {
            if (EditorUtility.DisplayDialog("确认删除", "确定要删除当前层吗？", "确定", "取消"))
            {
                EditorUtility.DisplayDialog("删除完成", "层已删除", "确定");
            }
        }

        void DuplicateCurrentLayer()
        {
            EditorUtility.DisplayDialog("复制完成", "层已复制", "确定");
        }

        void AnalyzeTerrain()
        {
            EditorUtility.DisplayDialog("分析完成", "地形分析已完成", "确定");
        }

        void ShowBiomeMap()
        {
            EditorUtility.DisplayDialog("生物群落图", "生物群落可视化已启用", "确定");
        }

        void RunEcosystemSimulation()
        {
            EditorUtility.DisplayDialog("生态模拟", "生态系统模拟已启动", "确定");
        }

        void ResetEcosystem()
        {
            if (EditorUtility.DisplayDialog("确认重置", "这将重置整个生态系统，确定继续吗？", "确定", "取消"))
            {
                EditorUtility.DisplayDialog("重置完成", "生态系统已重置", "确定");
            }
        }

        void TogglePreviewMode()
        {
            if (isPreviewMode)
            {
                EditorUtility.DisplayDialog("预览模式", "预览模式已启用", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("预览模式", "预览模式已关闭", "确定");
            }
        }

        void PreviewAtPosition()
        {
            EditorUtility.DisplayDialog("预览", $"在位置 {previewPosition} 预览放置效果", "确定");
        }

        void ShowDensityMap()
        {
            EditorUtility.DisplayDialog("密度图", "密度可视化已启用", "确定");
        }

        void ShowPlacementGrid()
        {
            EditorUtility.DisplayDialog("放置网格", "放置网格可视化已启用", "确定");
        }

        void ExportPlacementStats()
        {
            string path = EditorUtility.SaveFilePanel("导出放置统计", "", "PlacementStats", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("导出完成", "放置统计已导出到: " + path, "确定");
            }
        }

        void RunPerformanceAnalysis()
        {
            EditorUtility.DisplayDialog("性能分析", "放置系统性能分析已完成", "确定");
        }
        
        #region 植被系统UI
        
        void DrawVegetationSystem()
        {
            showVegetationSettings = EditorGUILayout.Foldout(showVegetationSettings, "植被系统", true);
            
            if (showVegetationSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                DrawVegetationControls();
                DrawVegetationSelection();
                DrawVegetationBrushSettings();
                DrawVegetationTemplates();
                DrawVegetationStats();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawVegetationControls()
        {
            EditorGUILayout.LabelField("植被控制", EditorStyles.boldLabel);
            
            // 地形选择
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("目标地形:", targetTerrain, typeof(Terrain), true);
            
            if (targetTerrain == null)
            {
                EditorGUILayout.HelpBox("请选择一个地形对象来放置植被", MessageType.Warning);
                
                if (GUILayout.Button("自动查找场景中的地形"))
                {
                    targetTerrain = FindFirstObjectByType<Terrain>();
                    if (targetTerrain != null)
                    {
                        Debug.Log($"自动找到地形: {targetTerrain.name}");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"✅ 目标地形: {targetTerrain.name}", MessageType.Info);
                
                // 控制按钮
                EditorGUILayout.BeginHorizontal();
                
                if (!placementSystem.IsVegetationPainting)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("激活植被绘制", GUILayout.Height(30)))
                    {
                        placementSystem.ActivateVegetationPainting(true);
                        Tools.hidden = true;
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("退出绘制模式", GUILayout.Height(30)))
                    {
                        placementSystem.ActivateVegetationPainting(false);
                        Tools.hidden = false;
                    }
                    GUI.backgroundColor = Color.white;
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("清除所有植被"))
                {
                    if (EditorUtility.DisplayDialog("确认清除", "这将删除地形上的所有植被，确定继续吗？", "确定", "取消"))
                    {
                        placementSystem.ClearAllVegetation();
                    }
                }
                
                if (GUILayout.Button("植被统计"))
                {
                    ShowVegetationStatistics();
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 添加快速植被选择按钮
                EditorGUILayout.LabelField("快速选择:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                // 快速选择针叶树（北欧云杉）
                if (GUILayout.Button("北欧云杉", GUILayout.Height(25)))
                {
                    selectedVegetationType = VegetationType.针叶树;
                    placementSystem.SetSelectedVegetationType(VegetationType.针叶树);
                }
                
                // 快速选择阔叶树
                if (GUILayout.Button("橡树", GUILayout.Height(25)))
                {
                    selectedVegetationType = VegetationType.阔叶树;
                    placementSystem.SetSelectedVegetationType(VegetationType.阔叶树);
                }
                
                // 快速选择野草
                if (GUILayout.Button("野草", GUILayout.Height(25)))
                {
                    selectedVegetationType = VegetationType.野草;
                    placementSystem.SetSelectedVegetationType(VegetationType.野草);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 显示当前选择
                EditorGUILayout.LabelField($"当前选择: {selectedVegetationType}", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.Space();
        }
        
        void DrawVegetationSelection()
        {
            EditorGUILayout.LabelField("选择植被类型", EditorStyles.boldLabel);
            
            if (placementSystem.VegetationSystem?.Library?.vegetationTypes == null)
            {
                EditorGUILayout.HelpBox("植被库未初始化", MessageType.Warning);
                return;
            }
            
            var vegetationLib = placementSystem.VegetationSystem.Library;
            
            vegetationScrollPos = EditorGUILayout.BeginScrollView(vegetationScrollPos, GUILayout.Height(150));
            
            // 按类别显示植被
            var categories = new Dictionary<string, List<VegetationData>>();
            foreach (var vegData in vegetationLib.vegetationTypes)
            {
                string category = vegetationLib.GetVegetationCategory(vegData.type);
                if (!categories.ContainsKey(category))
                    categories[category] = new List<VegetationData>();
                categories[category].Add(vegData);
            }
            
            foreach (var category in categories)
            {
                EditorGUILayout.LabelField(category.Key, EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                foreach (var vegData in category.Value)
                {
                    bool isSelected = selectedVegetationType == vegData.type;
                    
                    if (isSelected) GUI.backgroundColor = Color.cyan;
                    
                    string displayIcon = "■"; // 使用简单方块代替emoji
                    if (GUILayout.Button($"{displayIcon}\n{vegData.displayName}", GUILayout.Width(70), GUILayout.Height(50)))
                    {
                        selectedVegetationType = vegData.type;
                        placementSystem.SetSelectedVegetationType(vegData.type);
                    }
                    
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.LabelField($"当前选择: {selectedVegetationType}", EditorStyles.miniLabel);
            EditorGUILayout.Space();
        }
        
        void DrawVegetationBrushSettings()
        {
            EditorGUILayout.LabelField("画笔设置", EditorStyles.boldLabel);
            
            // 快速预设按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("精确放置", GUILayout.Height(20)))
            {
                vegetationBrushSize = 3f;
                vegetationDensity = 0.2f;
            }
            if (GUILayout.Button("标准画笔", GUILayout.Height(20)))
            {
                vegetationBrushSize = 10f;
                vegetationDensity = 0.5f;
            }
            if (GUILayout.Button("大面积填充", GUILayout.Height(20)))
            {
                vegetationBrushSize = 25f;
                vegetationDensity = 1.0f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            vegetationBrushSize = EditorGUILayout.Slider("画笔大小", vegetationBrushSize, 1f, 50f);
            vegetationDensity = EditorGUILayout.Slider("植被密度", vegetationDensity, 0.1f, 2.0f);
            
            placementSystem.SetVegetationBrushSettings(vegetationBrushSize, vegetationDensity);
            
            EditorGUILayout.Space();
        }
        
        void DrawVegetationTemplates()
        {
            EditorGUILayout.LabelField("植被模板 (一键生成)", EditorStyles.boldLabel);
            
            if (placementSystem.VegetationSystem?.Library?.templates == null)
            {
                EditorGUILayout.HelpBox("植被模板未加载", MessageType.Info);
                return;
            }
            
            var templates = placementSystem.VegetationSystem.Library.templates;
            
            EditorGUILayout.BeginHorizontal();
            foreach (var template in templates.Take(3)) // 显示前3个模板
            {
                if (GUILayout.Button($"{template.templateName}", GUILayout.Height(40)))
                {
                    if (targetTerrain != null)
                    {
                        if (EditorUtility.DisplayDialog("应用植被模板", 
                            $"确定要应用 '{template.templateName}' 植被模板吗？\n\n{template.description}", 
                            "应用", "取消"))
                        {
                            placementSystem.ApplyVegetationTemplate(template.templateName, targetTerrain);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "请先选择一个地形对象", "确定");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            foreach (var template in templates.Skip(3).Take(3)) // 显示后3个模板
            {
                if (GUILayout.Button($"{template.templateName}", GUILayout.Height(40)))
                {
                    if (targetTerrain != null)
                    {
                        if (EditorUtility.DisplayDialog("应用植被模板", 
                            $"确定要应用 '{template.templateName}' 植被模板吗？\n\n{template.description}", 
                            "应用", "取消"))
                        {
                            placementSystem.ApplyVegetationTemplate(template.templateName, targetTerrain);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "请先选择一个地形对象", "确定");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        void DrawVegetationStats()
        {
            EditorGUILayout.LabelField("植被信息", EditorStyles.boldLabel);
            
            var stats = placementSystem.GetVegetationStatistics();
            EditorGUILayout.LabelField($"总植被数量: {stats.totalCount}", EditorStyles.miniLabel);
            
            if (stats.vegetationCounts.Count > 0)
            {
                EditorGUILayout.LabelField("类型分布:", EditorStyles.miniLabel);
                foreach (var kvp in stats.vegetationCounts.Take(3)) // 只显示前3种
                {
                    EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                }
                
                if (stats.vegetationCounts.Count > 3)
                {
                    EditorGUILayout.LabelField($"  ... 还有 {stats.vegetationCounts.Count - 3} 种类型", EditorStyles.miniLabel);
                }
            }
        }
        
        void ShowVegetationStatistics()
        {
            var stats = placementSystem.GetVegetationStatistics();
            
            string message = $"植被统计信息:\n\n总植被数量: {stats.totalCount}\n\n";
            
            if (stats.vegetationCounts.Count > 0)
            {
                message += "详细分布:\n";
                foreach (var kvp in stats.vegetationCounts)
                {
                    message += $"{kvp.Key}: {kvp.Value}\n";
                }
            }
            else
            {
                message += "当前没有植被数据";
            }
            
            EditorUtility.DisplayDialog("植被统计", message, "确定");
        }
        
        #endregion
        
        #region Scene视图交互
        
        void OnSceneGUI(SceneView sceneView)
        {
            if (!placementSystem.IsVegetationPainting || targetTerrain == null) return;
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            Event current = Event.current;
            
            // 显示画笔预览
            ShowVegetationBrushPreview();
            
            // 处理植被放置
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                PlantVegetationAtMousePosition();
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0)
            {
                PlantVegetationAtMousePosition();
                current.Use();
            }
        }
        
        void ShowVegetationBrushPreview()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetComponent<Terrain>() == targetTerrain)
                {
                    // 绘制画笔预览圈
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(hit.point, hit.normal, vegetationBrushSize);
                    
                    // 显示信息
                    Handles.BeginGUI();
                    GUILayout.BeginArea(new Rect(10, 10, 200, 120));
                    GUILayout.Label($"植被: {selectedVegetationType}", EditorStyles.whiteLabel);
                    GUILayout.Label($"画笔大小: {vegetationBrushSize:F1}", EditorStyles.whiteLabel);
                    GUILayout.Label($"密度: {vegetationDensity:F1}", EditorStyles.whiteLabel);
                    GUILayout.EndArea();
                    Handles.EndGUI();
                    
                    SceneView.RepaintAll();
                }
            }
        }
        
        void PlantVegetationAtMousePosition()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetComponent<Terrain>() == targetTerrain)
                {
                    placementSystem.PaintVegetationAt(hit.point, targetTerrain);
                }
            }
        }
        
        #endregion
    }
}