using UnityEngine;
using UnityEditor;
using WorldEditor.TerrainSystem;
using WorldEditor.Core;

namespace WorldEditor.Editor
{
    /// <summary>
    /// 地形生成器专用编辑器界面
    /// 提供详细的地形生成控制
    /// </summary>
    [CustomEditor(typeof(AdvancedTerrainGenerator))]
    public class TerrainGeneratorEditor : UnityEditor.Editor
    {
        private AdvancedTerrainGenerator terrainGen;
        private SerializedProperty targetTerrain;
        private SerializedProperty terrainData;
        private SerializedProperty enableRealTimeGeneration;
        private SerializedProperty enableProgressiveGeneration;
        private SerializedProperty generationStepsPerFrame;

        // 预览设置
        private bool showPreview = true;
        private bool showAdvancedSettings = false;
        private bool showPerformanceSettings = false;
        
        // 临时生成参数
        private TerrainGenerationParams tempParams;
        
        // 印章相关
        private bool stampingModeEnabled = false;
        private WorldEditor.TerrainSystem.TerrainStamper currentStamper;

        void OnEnable()
        {
            terrainGen = (AdvancedTerrainGenerator)target;
            
            // 绑定序列化属性
            targetTerrain = serializedObject.FindProperty("targetTerrain");
            terrainData = serializedObject.FindProperty("terrainData");
            enableRealTimeGeneration = serializedObject.FindProperty("enableRealTimeGeneration");
            enableProgressiveGeneration = serializedObject.FindProperty("enableProgressiveGeneration");
            generationStepsPerFrame = serializedObject.FindProperty("generationStepsPerFrame");
            
            // 初始化临时参数
            InitializeTempParams();
            
            // 初始化印章相关
            currentStamper = terrainGen.GetComponent<WorldEditor.TerrainSystem.TerrainStamper>();
        }
        
        void OnDisable()
        {
            stampingModeEnabled = false;
        }
        
        void OnSceneGUI()
        {
            if (!stampingModeEnabled || currentStamper == null) return;
            
            // 处理Scene视图中的鼠标事件
            HandleSceneStamping();
        }
        
        void HandleSceneStamping()
        {
            Event current = Event.current;
            
            // 只处理鼠标左键点击
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                // 将鼠标位置转换为世界坐标
                Vector2 mousePosition = current.mousePosition;
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                
                // 检测是否点击到地形
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    // 检查是否点击的是我们的地形
                    Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
                    Terrain ourTerrain = terrainGen.GetTerrain();
                    
                    if (hitTerrain != null && (hitTerrain == ourTerrain || IsOurTerrainChild(hitTerrain)))
                    {
                        Debug.Log($"[TerrainGeneratorEditor] 在Scene视图中点击地形位置: {hit.point}");
                        
                        // 应用印章
                        currentStamper.ApplyStampAtPosition(hit.point);
                        
                        // 消费这个事件，防止选择对象
                        current.Use();
                        
                        // 刷新Scene视图
                        SceneView.RepaintAll();
                    }
                }
            }
            
            // 在Scene视图中显示提示信息
            Handles.BeginGUI();
            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
            GUILayout.BeginArea(new Rect(10, 10, 250, 60), GUI.skin.box);
            GUILayout.Label("印章模式已启用", EditorStyles.boldLabel);
            GUILayout.Label("点击地形表面应用印章");
            if (GUILayout.Button("退出印章模式"))
            {
                stampingModeEnabled = false;
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }
        
        bool IsOurTerrainChild(Terrain terrain)
        {
            // 检查地形是否是我们地形生成器的子对象
            Transform terrainTransform = terrain.transform;
            while (terrainTransform.parent != null)
            {
                if (terrainTransform.parent == terrainGen.transform)
                {
                    return true;
                }
                terrainTransform = terrainTransform.parent;
            }
            return false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            DrawBasicSettings();
            DrawGenerationControls();
            DrawAdvancedSettings();
            DrawPerformanceSettings();
            DrawPreview();
            
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
            
            GUILayout.Label("高级地形生成器", titleStyle);
            GUILayout.Label("超越 Gaia Pro 的地形生成系统", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space();
            
            // 状态信息
            string status = terrainGen.IsGenerating() ? "正在生成..." : "就绪";
            Color statusColor = terrainGen.IsGenerating() ? Color.yellow : Color.green;
            
            GUI.color = statusColor;
            GUILayout.Label($"状态: {status}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.EndVertical();
        }

        void DrawBasicSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("基础设置", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(targetTerrain, new GUIContent("目标地形", "要生成的Unity地形组件"));
            EditorGUILayout.PropertyField(terrainData, new GUIContent("地形数据", "地形数据资源，留空则自动创建"));
            
            EditorGUILayout.Space();
            
            
            EditorGUILayout.PropertyField(enableRealTimeGeneration, new GUIContent("启用实时生成", "启用后可以实时生成地形"));
            
            if (enableRealTimeGeneration.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(enableProgressiveGeneration, new GUIContent("渐进式生成", "分帧生成，避免卡顿"));
                
                if (enableProgressiveGeneration.boolValue)
                {
                    EditorGUILayout.PropertyField(generationStepsPerFrame, new GUIContent("每帧步数", "每帧执行的生成步数"));
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawGenerationControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("生成控制", EditorStyles.boldLabel);
            
            // 主要生成按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !terrainGen.IsGenerating();
            if (GUILayout.Button("生成地形", GUILayout.Height(30)))
            {
                GenerateTerrain();
            }
            
            GUI.enabled = terrainGen.IsGenerating();
            if (GUILayout.Button("停止生成", GUILayout.Height(30)))
            {
                terrainGen.StopGeneration();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 分步生成按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("仅生成高度"))
            {
                GenerateHeightOnly();
            }
            
            if (GUILayout.Button("应用侵蚀"))
            {
                ApplyErosion();
            }
            
            if (GUILayout.Button("生成河流"))
            {
                GenerateRivers();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("混合纹理"))
            {
                BlendTextures();
            }
            
            if (GUILayout.Button("纹理绘制"))
            {
                OpenTexturePainter();
            }
            
            if (GUILayout.Button("重置地形"))
            {
                ResetTerrain();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 高级地形特征（未来功能）
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = false; // 禁用这些按钮，因为功能未实现
            if (GUILayout.Button("生成悬崖"))
            {
                // TODO: 实现悬崖生成功能
                EditorUtility.DisplayDialog("功能开发中", "悬崖生成功能正在开发中，敬请期待！", "确定");
            }
            
            if (GUILayout.Button("生成洞穴"))
            {
                // TODO: 实现洞穴生成功能
                EditorUtility.DisplayDialog("功能开发中", "洞穴生成功能正在开发中，敬请期待！", "确定");
            }
            
            if (GUILayout.Button("生成火山"))
            {
                // TODO: 实现火山生成功能
                EditorUtility.DisplayDialog("功能开发中", "火山生成功能正在开发中，敬请期待！", "确定");
            }
            GUI.enabled = true; // 恢复GUI启用状态
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // ========== 地形印章卡片 ==========
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            
            DrawStampingSystem();
            
            EditorGUILayout.EndVertical();
            
            // ========== 地形扩展卡片 ==========
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("地形扩展", EditorStyles.boldLabel);
            
            if (terrainGen.IsTerrainExpansionEnabled())
            {
                
                // 地形扩展按钮（十字布局）
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("↑ 北", GUILayout.Width(50), GUILayout.Height(25)))
                {
                    terrainGen.ExpandTerrain(WorldEditor.TerrainSystem.TerrainDirection.North);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("← 西", GUILayout.Width(50), GUILayout.Height(25)))
                {
                    terrainGen.ExpandTerrain(WorldEditor.TerrainSystem.TerrainDirection.West);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("东 →", GUILayout.Width(50), GUILayout.Height(25)))
                {
                    terrainGen.ExpandTerrain(WorldEditor.TerrainSystem.TerrainDirection.East);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("↓ 南", GUILayout.Width(50), GUILayout.Height(25)))
                {
                    terrainGen.ExpandTerrain(WorldEditor.TerrainSystem.TerrainDirection.South);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("点击方向按钮创建相邻地形", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("地形扩展功能已禁用", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawStampingSystem()
        {
            GUILayout.Label("地形印章", EditorStyles.boldLabel);
            
            // 获取或创建TerrainStamper组件
            var terrainStamper = terrainGen.GetComponent<WorldEditor.TerrainSystem.TerrainStamper>();
            if (terrainStamper == null)
            {
                EditorGUILayout.HelpBox("需要TerrainStamper组件来使用印章功能", MessageType.Info);
                if (GUILayout.Button("添加地形印章系统"))
                {
                    terrainGen.gameObject.AddComponent<WorldEditor.TerrainSystem.TerrainStamper>();
                    Debug.Log("[TerrainGeneratorEditor] 已添加TerrainStamper组件");
                }
                return;
            }
            
            // 印章库状态
            var stampLibrary = terrainStamper.GetStampLibrary();
            if (stampLibrary != null)
            {
                int stampCount = stampLibrary.GetStampCount();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("印章库:", $"{stampCount} 个印章");
                if (GUILayout.Button("重新创建", GUILayout.Width(80)))
                {
                    Debug.Log("[TerrainGeneratorEditor] 强制重新创建印章库");
                    stampLibrary.ForceRecreateStamps();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // 印章选择区域
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前印章:", GUILayout.Width(80));
            
            // 简化的印章选择（这里可以后续扩展为更复杂的印章浏览器）
            if (GUILayout.Button("选择印章", GUILayout.Height(25)))
            {
                Debug.Log("[TerrainGeneratorEditor] 点击了选择印章按钮");
                if (terrainStamper == null)
                {
                    Debug.LogError("[TerrainGeneratorEditor] TerrainStamper组件为null");
                    EditorUtility.DisplayDialog("错误", "TerrainStamper组件未找到", "确定");
                    return;
                }
                ShowStampSelectionMenu(terrainStamper);
            }
            EditorGUILayout.EndHorizontal();
            
            // 印章参数设置
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("大小:", GUILayout.Width(40));
            // 这里需要使用反射或公开字段来访问stampSize
            EditorGUILayout.Slider(100f, 10f, 500f); // 暂时的静态滑条
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("强度:", GUILayout.Width(40));
            EditorGUILayout.Slider(1f, 0f, 2f); // 暂时的静态滑条
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 印章操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用印章"))
            {
                // 在地形中心应用印章（示例）
                var terrain = terrainGen.GetTerrain();
                if (terrain != null)
                {
                    Vector3 center = terrain.transform.position + terrain.terrainData.size * 0.5f;
                    center.y = terrain.SampleHeight(center);
                    terrainStamper.ApplyStampAtPosition(center);
                }
            }
            
            if (GUILayout.Button("清除历史"))
            {
                terrainStamper.ClearStampHistory();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            
            // Scene视图印章模式切换
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = stampingModeEnabled ? Color.green : Color.white;
            string modeButtonText = stampingModeEnabled ? "退出Scene印章模式" : "启用Scene印章模式";
            if (GUILayout.Button(modeButtonText))
            {
                stampingModeEnabled = !stampingModeEnabled;
                currentStamper = terrainStamper; // 更新当前印章器引用
                Debug.Log($"[TerrainGeneratorEditor] Scene印章模式: {(stampingModeEnabled ? "已启用" : "已关闭")}");
                SceneView.RepaintAll(); // 刷新Scene视图
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            if (stampingModeEnabled)
            {
                EditorGUILayout.HelpBox("Scene印章模式已启用！在Scene视图中点击地形表面来应用印章。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("启用Scene印章模式后可在Scene视图中点击应用", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        void ShowStampSelectionMenu(WorldEditor.TerrainSystem.TerrainStamper stamper)
        {
            Debug.Log("[TerrainGeneratorEditor] 开始显示印章选择菜单");
            
            var stampLibrary = stamper.GetStampLibrary();
            if (stampLibrary == null) 
            {
                Debug.LogError("[TerrainGeneratorEditor] 印章库为null，尝试强制初始化");
                
                // 强制初始化印章库
                Debug.Log("[TerrainGeneratorEditor] 强制调用InitializeStampLibrary");
                stamper.InitializeStampLibrary();
                stampLibrary = stamper.GetStampLibrary();
                Debug.Log($"[TerrainGeneratorEditor] 强制初始化后印章库状态: {(stampLibrary != null ? "成功" : "仍为null")}");
                
                if (stampLibrary == null)
                {
                    EditorUtility.DisplayDialog("印章库错误", "无法初始化印章库，请尝试重新添加TerrainStamper组件", "确定");
                    return;
                }
            }
            
            Debug.Log($"[TerrainGeneratorEditor] 找到印章库: {stampLibrary.name}");
            
            var stamps = stampLibrary.GetAllStamps();
            Debug.Log($"[TerrainGeneratorEditor] 印章库中有 {stamps.Count} 个印章");
            
            if (stamps.Count == 0)
            {
                Debug.LogWarning("[TerrainGeneratorEditor] 印章库为空，尝试初始化默认印章");
                stampLibrary.InitializeDefaultStamps();
                stamps = stampLibrary.GetAllStamps();
                Debug.Log($"[TerrainGeneratorEditor] 初始化后印章数量: {stamps.Count}");
                
                if (stamps.Count == 0)
                {
                    EditorUtility.DisplayDialog("印章库", "印章库为空且无法创建默认印章，请手动添加印章", "确定");
                    return;
                }
            }
            
            GenericMenu menu = new GenericMenu();
            
            foreach (var stamp in stamps)
            {
                if (stamp == null) 
                {
                    Debug.LogWarning("[TerrainGeneratorEditor] 发现null印章，跳过");
                    continue;
                }
                
                Debug.Log($"[TerrainGeneratorEditor] 添加印章到菜单: {stamp.stampName} ({stamp.category})");
                
                menu.AddItem(new GUIContent($"{stamp.category}/{stamp.stampName}"), 
                           false, 
                           () => { 
                               Debug.Log($"[TerrainGeneratorEditor] 选择了印章: {stamp.stampName}");
                               stamper.SetCurrentStamp(stamp); 
                           });
            }
            
            Debug.Log("[TerrainGeneratorEditor] 显示印章选择菜单");
            menu.ShowAsContext();
        }

        void DrawAdvancedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", true);
            
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                // 噪声设置
                GUILayout.Label("噪声生成", EditorStyles.boldLabel);
                tempParams.heightVariation = EditorGUILayout.Slider("高度变化", tempParams.heightVariation, 0f, 1000f);
                tempParams.baseHeight = EditorGUILayout.Slider("基础高度", tempParams.baseHeight, -500f, 500f);
                
                // 噪声层设置
                EditorGUILayout.Space();
                GUILayout.Label("噪声层设置", EditorStyles.boldLabel);
                
                if (tempParams.noiseLayers != null && tempParams.noiseLayers.Length > 0)
                {
                    for (int i = 0; i < tempParams.noiseLayers.Length; i++)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label($"噪声层 {i + 1}", EditorStyles.boldLabel);
                        
                        var layer = tempParams.noiseLayers[i];
                        layer.noiseType = (NoiseType)EditorGUILayout.EnumPopup("噪声类型", layer.noiseType);
                        layer.weight = EditorGUILayout.Slider("权重", layer.weight, 0f, 2f);
                        layer.frequency = EditorGUILayout.Slider("频率", layer.frequency, 0.001f, 0.1f);
                        layer.amplitude = EditorGUILayout.Slider("振幅", layer.amplitude, 0f, 2f);
                        layer.octaves = EditorGUILayout.IntSlider("倍频", layer.octaves, 1, 10);
                        layer.persistence = EditorGUILayout.Slider("持续性", layer.persistence, 0f, 1f);
                        layer.lacunarity = EditorGUILayout.Slider("间隙度", layer.lacunarity, 1f, 4f);
                        
                        Vector2 offset = layer.offset;
                        offset.x = EditorGUILayout.Slider("X偏移", offset.x, -1000f, 1000f);
                        offset.y = EditorGUILayout.Slider("Y偏移", offset.y, -1000f, 1000f);
                        layer.offset = offset;
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("添加噪声层"))
                    {
                        AddNoiseLayer();
                    }
                    if (tempParams.noiseLayers.Length > 1 && GUILayout.Button("删除最后一层"))
                    {
                        RemoveNoiseLayer();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space();
                
                // 地质设置
                GUILayout.Label("地质模拟", EditorStyles.boldLabel);
                tempParams.enableGeologicalLayers = EditorGUILayout.Toggle("启用地质分层", tempParams.enableGeologicalLayers);
                
                if (tempParams.enableGeologicalLayers)
                {
                    EditorGUI.indentLevel++;
                    tempParams.geology.erosionIterations = EditorGUILayout.IntSlider("侵蚀迭代次数", tempParams.geology.erosionIterations, 1, 100);
                    tempParams.geology.erosionStrength = EditorGUILayout.Slider("侵蚀强度", tempParams.geology.erosionStrength, 0f, 1f);
                    tempParams.geology.rockHardness = EditorGUILayout.Slider("岩石硬度", tempParams.geology.rockHardness, 0f, 1f);
                    tempParams.geology.erosionResistance = EditorGUILayout.Slider("抗侵蚀性", tempParams.geology.erosionResistance, 0f, 1f);
                    EditorGUI.indentLevel--;
                }
                
                // 河流生成功能已移至【生成控制】区域
                
                
                
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
                
                GUILayout.Label("生成性能", EditorStyles.boldLabel);
                
                int currentSteps = generationStepsPerFrame.intValue;
                string[] stepOptions = { "低 (50)", "中 (100)", "高 (200)", "极高 (500)" };
                int[] stepValues = { 50, 100, 200, 500 };
                
                int selectedIndex = System.Array.IndexOf(stepValues, currentSteps);
                if (selectedIndex == -1) selectedIndex = 1; // 默认中等
                
                selectedIndex = EditorGUILayout.Popup("性能等级", selectedIndex, stepOptions);
                generationStepsPerFrame.intValue = stepValues[selectedIndex];
                
                EditorGUILayout.Space();
                
                EditorGUILayout.HelpBox(
                    "性能等级决定每帧处理的生成步数。" +
                    "\n• 低: 适合低端设备，生成较慢但不卡顿" +
                    "\n• 中: 平衡性能和速度" +
                    "\n• 高: 快速生成，可能有短暂卡顿" +
                    "\n• 极高: 最快生成，适合强力设备",
                    MessageType.Info
                );
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showPreview = EditorGUILayout.Foldout(showPreview, "预览和信息", true);
            
            if (showPreview)
            {
                // 地形信息
                if (terrainGen.GetComponent<Terrain>() != null)
                {
                    Terrain terrain = terrainGen.GetComponent<Terrain>();
                    Vector3 size = terrain.terrainData.size;
                    
                    EditorGUILayout.LabelField("地形尺寸", $"{size.x} × {size.z} × {size.y}");
                    EditorGUILayout.LabelField("高度图分辨率", $"{terrain.terrainData.heightmapResolution}");
                }
                
                EditorGUILayout.Space();
                
                // 生成预设
                GUILayout.Label("快速预设", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("平原"))
                {
                    ApplyPlainPreset();
                }
                
                if (GUILayout.Button("丘陵"))
                {
                    ApplyHillPreset();
                }
                
                if (GUILayout.Button("山脉"))
                {
                    ApplyMountainPreset();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("峡谷"))
                {
                    ApplyCanyonPreset();
                }
                
                if (GUILayout.Button("高原"))
                {
                    ApplyPlateauPreset();
                }
                
                if (GUILayout.Button("随机"))
                {
                    ApplyRandomPreset();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 快捷操作
                GUILayout.Label("快捷操作", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("随机种子"))
                {
                    RandomizeSeed();
                }
                
                if (GUILayout.Button("保存预设"))
                {
                    SaveCurrentPreset();
                }
                
                if (GUILayout.Button("加载预设"))
                {
                    LoadPreset();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("导出高度图"))
                {
                    ExportHeightmap();
                }
                
                if (GUILayout.Button("导入高度图"))
                {
                    ImportHeightmap();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        void InitializeTempParams()
        {
            tempParams = new TerrainGenerationParams();
            // 设置默认值
            tempParams.heightVariation = 100f;
            tempParams.enableGeologicalLayers = true;
            tempParams.generateRivers = true;
            
            
            // 初始化地质参数
            tempParams.geology = new GeologySettings();
            tempParams.geology.erosionIterations = 10;
            tempParams.geology.erosionStrength = 0.3f;
            
            // 初始化噪声层
            tempParams.noiseLayers = new NoiseLayerSettings[1];
            tempParams.noiseLayers[0] = new NoiseLayerSettings();
            tempParams.noiseLayers[0].noiseType = NoiseType.Perlin;
            tempParams.noiseLayers[0].weight = 1f;
            tempParams.noiseLayers[0].frequency = 0.01f;
            tempParams.noiseLayers[0].amplitude = 1f;
            tempParams.noiseLayers[0].octaves = 8;
            tempParams.noiseLayers[0].persistence = 0.5f;
            tempParams.noiseLayers[0].lacunarity = 2f;
            tempParams.noiseLayers[0].offset = Vector2.zero;
        }

        // 功能方法
        void GenerateTerrain()
        {
            Debug.Log("[TerrainGeneratorEditor] 编辑器生成地形请求");
            
            // 在编辑器模式下，强制使用立即生成避免协程问题
            // 使用反射修改私有字段
            var realTimeField = typeof(AdvancedTerrainGenerator).GetField("enableRealTimeGeneration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressiveField = typeof(AdvancedTerrainGenerator).GetField("enableProgressiveGeneration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // 保存原始值
            bool originalRealTime = (bool)realTimeField?.GetValue(terrainGen);
            bool originalProgressive = (bool)progressiveField?.GetValue(terrainGen);
            
            // 临时设置为立即生成模式
            realTimeField?.SetValue(terrainGen, true);
            progressiveField?.SetValue(terrainGen, false); // 禁用渐进式，使用立即生成
            
            try
            {
                if (terrainGen.GetComponent<WorldEditorManager>() != null)
                {
                    var worldManager = terrainGen.GetComponent<WorldEditorManager>();
                    var parameters = worldManager.GetGenerationParameters();
                    // 应用编辑器中的临时参数
                    parameters.terrainParams = tempParams;
                    terrainGen.GenerateTerrain(parameters);
                }
                else
                {
                    // 使用临时参数
                    var tempWorldParams = CreateTempWorldParams();
                    terrainGen.GenerateTerrain(tempWorldParams);
                }
            }
            finally
            {
                // 恢复原始设置
                realTimeField?.SetValue(terrainGen, originalRealTime);
                progressiveField?.SetValue(terrainGen, originalProgressive);
            }
        }

        void GenerateHeightOnly()
        {
            Debug.Log("[TerrainGeneratorEditor] 仅生成高度 - 使用快速同步模式");
            
            // 强制使用立即生成模式，避免缓慢的渐进式生成
            var realTimeField = typeof(AdvancedTerrainGenerator).GetField("enableRealTimeGeneration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressiveField = typeof(AdvancedTerrainGenerator).GetField("enableProgressiveGeneration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // 保存原始值
            bool originalRealTime = (bool)realTimeField?.GetValue(terrainGen);
            bool originalProgressive = (bool)progressiveField?.GetValue(terrainGen);
            
            try
            {
                // 设置为立即同步生成模式
                realTimeField?.SetValue(terrainGen, true);
                progressiveField?.SetValue(terrainGen, false);
                
                var tempWorldParams = CreateTempWorldParams();
                // 禁用其他功能，只生成高度
                tempWorldParams.terrainParams.enableGeologicalLayers = false;
                tempWorldParams.terrainParams.generateRivers = false;
                
                terrainGen.GenerateTerrain(tempWorldParams);
                Debug.Log("[TerrainGeneratorEditor] 仅生成高度图完成 - 立即模式");
            }
            finally
            {
                // 恢复原始设置
                realTimeField?.SetValue(terrainGen, originalRealTime);
                progressiveField?.SetValue(terrainGen, originalProgressive);
            }
        }

        void ApplyErosion()
        {
            Debug.Log("[TerrainGeneratorEditor] 开始应用侵蚀效果");
            
            // 查找地形：先检查子对象，再检查本身
            Terrain terrain = null;
            Transform terrainChild = terrainGen.transform.Find("Generated Terrain");
            if (terrainChild != null)
            {
                terrain = terrainChild.GetComponent<Terrain>();
                Debug.Log("[TerrainGeneratorEditor] 在子对象中找到地形");
            }
            else
            {
                terrain = terrainGen.GetComponent<Terrain>();
                if (terrain != null)
                {
                    Debug.Log("[TerrainGeneratorEditor] 在主对象中找到地形");
                }
            }
            
            if (terrain != null)
            {
                try
                {
                    var tempWorldParams = CreateTempWorldParams();
                    tempWorldParams.terrainParams.enableGeologicalLayers = true;
                    
                    Debug.Log($"[TerrainGeneratorEditor] 侵蚀参数 - 迭代次数: {tempWorldParams.terrainParams.geology.erosionIterations}, 强度: {tempWorldParams.terrainParams.geology.erosionStrength}");
                    
                    // 直接对现有地形应用侵蚀
                    var erosionSim = terrainGen.GetComponent<ErosionSimulator>();
                    if (erosionSim == null)
                    {
                        Debug.Log("[TerrainGeneratorEditor] 创建ErosionSimulator组件");
                        erosionSim = terrainGen.gameObject.AddComponent<ErosionSimulator>();
                    }
                    
                    // terrain变量已在上面定义
                    int resolution = terrain.terrainData.heightmapResolution;
                    Debug.Log($"[TerrainGeneratorEditor] 地形分辨率: {resolution}x{resolution}");
                    
                    float[,] heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    Vector2Int mapRes = new Vector2Int(resolution, resolution);
                    
                    // 检查高度图是否有数据
                    float minHeight = float.MaxValue, maxHeight = float.MinValue;
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int y = 0; y < resolution; y++)
                        {
                            minHeight = Mathf.Min(minHeight, heights[x, y]);
                            maxHeight = Mathf.Max(maxHeight, heights[x, y]);
                        }
                    }
                    Debug.Log($"[TerrainGeneratorEditor] 侵蚀前高度范围: {minHeight:F3} - {maxHeight:F3}");
                    
                    // 应用侵蚀
                    erosionSim.ApplyErosion(ref heights, mapRes, tempWorldParams.terrainParams.geology);
                    
                    // 检查侵蚀后的高度变化
                    float newMinHeight = float.MaxValue, newMaxHeight = float.MinValue;
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int y = 0; y < resolution; y++)
                        {
                            newMinHeight = Mathf.Min(newMinHeight, heights[x, y]);
                            newMaxHeight = Mathf.Max(newMaxHeight, heights[x, y]);
                        }
                    }
                    Debug.Log($"[TerrainGeneratorEditor] 侵蚀后高度范围: {newMinHeight:F3} - {newMaxHeight:F3}");
                    
                    // 应用高度图回地形
                    terrain.terrainData.SetHeights(0, 0, heights);
                    
                    // 强制刷新地形
                    terrain.Flush();
                    
                    Debug.Log("[TerrainGeneratorEditor]  侵蚀效果应用完成！");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TerrainGeneratorEditor] 应用侵蚀失败: {e.Message}");
                    Debug.LogError($"[TerrainGeneratorEditor] 堆栈跟踪: {e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError("[TerrainGeneratorEditor] 没有找到地形组件，请先生成地形");
            }
        }

        void GenerateRivers()
        {
            Debug.Log("[TerrainGeneratorEditor] 开始生成河流");
            
            // 查找地形：先检查子对象，再检查本身
            Terrain terrain = null;
            Transform terrainChild = terrainGen.transform.Find("Generated Terrain");
            if (terrainChild != null)
            {
                terrain = terrainChild.GetComponent<Terrain>();
                Debug.Log("[TerrainGeneratorEditor] 在子对象中找到地形");
            }
            else
            {
                terrain = terrainGen.GetComponent<Terrain>();
                if (terrain != null)
                {
                    Debug.Log("[TerrainGeneratorEditor] 在主对象中找到地形");
                }
            }
            
            if (terrain != null)
            {
                try
                {
                    var tempWorldParams = CreateTempWorldParams();
                    tempWorldParams.terrainParams.generateRivers = true;
                    
                    Debug.Log("[TerrainGeneratorEditor] 河流生成参数已启用");
                    
                    var riverGen = terrainGen.GetComponent<RiverGenerator>();
                    if (riverGen == null)
                    {
                        Debug.Log("[TerrainGeneratorEditor] 创建RiverGenerator组件");
                        riverGen = terrainGen.gameObject.AddComponent<RiverGenerator>();
                    }
                    
                    // 检查地形高度范围，调整河流源头高度要求
                    float minHeight = float.MaxValue, maxHeight = float.MinValue;
                    for (int x = 0; x < terrain.terrainData.heightmapResolution; x++)
                    {
                        for (int y = 0; y < terrain.terrainData.heightmapResolution; y++)
                        {
                            float h = terrain.terrainData.GetHeights(x, y, 1, 1)[0, 0];
                            minHeight = Mathf.Min(minHeight, h);
                            maxHeight = Mathf.Max(maxHeight, h);
                        }
                    }
                    Debug.Log($"[TerrainGeneratorEditor] 当前地形高度范围: {minHeight:F3} - {maxHeight:F3}");
                    
                    // 设置合理的高度要求（40%），配合智能搜索
                    float adjustedMinSourceHeight = minHeight + (maxHeight - minHeight) * 0.4f; // 地形高度的40%
                    Debug.Log($"[TerrainGeneratorEditor] 设置河流源头高度要求: {adjustedMinSourceHeight:F3}");
                    
                    // 使用反射设置河流生成器的参数
                    var minSourceHeightField = typeof(RiverGenerator).GetField("minSourceHeight", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (minSourceHeightField != null)
                    {
                        minSourceHeightField.SetValue(riverGen, adjustedMinSourceHeight);
                        Debug.Log("[TerrainGeneratorEditor] 已取消严格的高度限制");
                    }
                    
                    // 同时增加尝试次数，确保能找到源头
                    var maxAttemptsField = typeof(RiverGenerator).GetField("maxSourceAttempts", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (maxAttemptsField != null)
                    {
                        maxAttemptsField.SetValue(riverGen, 500); // 增加到500次尝试
                        Debug.Log("[TerrainGeneratorEditor] 增加了河流源头搜索次数到500次");
                    }
                    
                    // terrain变量已在上面定义
                int resolution = terrain.terrainData.heightmapResolution;
                float[,] heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
                Vector2Int mapRes = new Vector2Int(resolution, resolution);
                
                    Debug.Log($"[TerrainGeneratorEditor] 地形分辨率: {resolution}x{resolution}");
                    
                    riverGen.GenerateRivers(ref heights, mapRes, tempWorldParams.terrainParams);
                    terrain.terrainData.SetHeights(0, 0, heights);
                    
                    // 强制刷新地形
                    terrain.Flush();
                    
                    Debug.Log("[TerrainGeneratorEditor]  河流生成完成！");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TerrainGeneratorEditor] 河流生成失败: {e.Message}");
                    Debug.LogError($"[TerrainGeneratorEditor] 堆栈跟踪: {e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError("[TerrainGeneratorEditor] 没有找到地形组件，请先生成地形");
            }
        }

        void BlendTextures()
        {
            Debug.Log("[TerrainGeneratorEditor] 开始混合纹理");
            
            // 查找地形：先检查子对象，再检查本身
            Terrain terrain = null;
            Transform terrainChild = terrainGen.transform.Find("Generated Terrain");
            if (terrainChild != null)
            {
                terrain = terrainChild.GetComponent<Terrain>();
                Debug.Log("[TerrainGeneratorEditor] 在子对象中找到地形");
            }
            else
            {
                terrain = terrainGen.GetComponent<Terrain>();
                if (terrain != null)
                {
                    Debug.Log("[TerrainGeneratorEditor] 在主对象中找到地形");
                }
            }
            
            if (terrain != null)
            {
                try
                {
                    var tempWorldParams = CreateTempWorldParams();
                    
                    Debug.Log("[TerrainGeneratorEditor] 纹理混合参数已准备");
                    Debug.Log($"[TerrainGeneratorEditor] 当前生物群系: {tempWorldParams.terrainParams.biome}");
                    
                    var textureBlender = terrainGen.GetComponent<TextureBlender>();
                    if (textureBlender == null)
                    {
                        Debug.Log("[TerrainGeneratorEditor] 创建TextureBlender组件");
                        textureBlender = terrainGen.gameObject.AddComponent<TextureBlender>();
                    }
                    
                    // terrain变量已在上面定义
                    int resolution = terrain.terrainData.heightmapResolution;
                    Debug.Log($"[TerrainGeneratorEditor] 地形分辨率: {resolution}x{resolution}");
                    
                    float[,] heights = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    Vector2Int mapRes = new Vector2Int(resolution, resolution);
                    
                    // 强制清空现有纹理层，确保应用新的混合纹理
                    Debug.Log("[TerrainGeneratorEditor] 清空现有纹理层，准备应用混合纹理");
                    terrain.terrainData.terrainLayers = null;
                    
                    Debug.Log("[TerrainGeneratorEditor] 开始应用纹理混合");
                    textureBlender.ApplyTextures(terrain.terrainData, heights, mapRes, tempWorldParams.terrainParams);
                    
                    // 强制刷新地形
                    terrain.Flush();
                    
                    Debug.Log("[TerrainGeneratorEditor]  纹理混合完成！");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TerrainGeneratorEditor] 纹理混合失败: {e.Message}");
                    Debug.LogError($"[TerrainGeneratorEditor] 堆栈跟踪: {e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError("[TerrainGeneratorEditor] 没有找到地形组件，请先生成地形");
            }
        }

        void ResetTerrain()
        {
            if (EditorUtility.DisplayDialog("确认重置", "这将重置地形为平面，确定继续吗？", "确定", "取消"))
            {
                Debug.Log("[TerrainGeneratorEditor] ========== 开始重置地形 ==========");
                
                // 使用与其他按钮相同的地形查找逻辑
                Terrain terrain = null;
                Transform terrainChild = terrainGen.transform.Find("Generated Terrain");
                if (terrainChild != null)
                {
                    terrain = terrainChild.GetComponent<Terrain>();
                    Debug.Log("[TerrainGeneratorEditor] 在子对象中找到地形");
                }
                else
                {
                    terrain = terrainGen.GetComponent<Terrain>();
                    Debug.Log("[TerrainGeneratorEditor] 在主对象中找到地形");
                }

                if (terrain != null && terrain.terrainData != null)
                {
                    Debug.Log("[TerrainGeneratorEditor] 开始重置地形数据...");
                    
                    // 重置高度数据
                    int resolution = terrain.terrainData.heightmapResolution;
                    float[,] heights = new float[resolution, resolution];
                    
                    // 设置所有高度为0 (完全平坦)
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int y = 0; y < resolution; y++)
                        {
                            heights[x, y] = 0f;
                        }
                    }
                    
                    terrain.terrainData.SetHeights(0, 0, heights);
                    Debug.Log($"[TerrainGeneratorEditor] 高度数据重置完成，分辨率: {resolution}x{resolution}");
                    
                    // 清除纹理层
                    if (terrain.terrainData.terrainLayers != null && terrain.terrainData.terrainLayers.Length > 0)
                    {
                        terrain.terrainData.terrainLayers = new TerrainLayer[0];
                        Debug.Log("[TerrainGeneratorEditor] 纹理层清除完成");
                    }
                    
                    // 清除河流数据（如果有RiverGenerator组件）
                    RiverGenerator riverGen = terrainGen.GetComponent<RiverGenerator>();
                    if (riverGen != null)
                    {
                        riverGen.ClearRivers();
                        Debug.Log("[TerrainGeneratorEditor] 河流数据清除完成");
                    }
                    
                    Debug.Log("[TerrainGeneratorEditor] 地形重置完成");
                    EditorUtility.DisplayDialog("重置完成", "地形已重置为平面", "确定");
                }
                else
                {
                    Debug.LogError("[TerrainGeneratorEditor] 没有找到地形组件，请先生成地形");
                    EditorUtility.DisplayDialog("错误", "没有找到地形组件，请先生成地形", "确定");
                }
            }
        }

        // 预设方法
        void ApplyPlainPreset()
        {
            Debug.Log("[TerrainGeneratorEditor] 应用平原预设并立即生成干净地形");
            tempParams.heightVariation = 20f;
            tempParams.enableGeologicalLayers = false;
            tempParams.generateRivers = false;
            
            // 立即生成地形，确保清除所有河流痕迹
            GeneratePresetTerrain();
        }

        void ApplyHillPreset()
        {
            Debug.Log("[TerrainGeneratorEditor] 应用丘陵预设并立即生成干净地形");
            tempParams.heightVariation = 80f;
            tempParams.enableGeologicalLayers = true;
            tempParams.geology.erosionStrength = 0.2f;
            tempParams.generateRivers = false; // 预设不包含河流，用户可以后续手动添加
            
            GeneratePresetTerrain();
        }

        void ApplyMountainPreset()
        {
            Debug.Log("[TerrainGeneratorEditor] 应用山脉预设并立即生成干净地形");
            tempParams.heightVariation = 300f;
            tempParams.enableGeologicalLayers = true;
            tempParams.geology.erosionStrength = 0.4f;
            tempParams.generateRivers = false; // 预设不包含河流，用户可以后续手动添加
            
            GeneratePresetTerrain();
        }

        void ApplyCanyonPreset()
        {
            Debug.Log("[TerrainGeneratorEditor] 应用峡谷预设并立即生成干净地形");
            tempParams.heightVariation = 150f;
            tempParams.enableGeologicalLayers = true;
            tempParams.geology.erosionStrength = 0.8f;
            tempParams.generateRivers = false; // 预设不包含河流，用户可以后续手动添加
            
            GeneratePresetTerrain();
        }

        void ApplyPlateauPreset()
        {
            Debug.Log("[TerrainGeneratorEditor] 应用高原预设并立即生成干净地形");
            tempParams.heightVariation = 100f;
            tempParams.enableGeologicalLayers = false; // 高原地形不需要侵蚀
            tempParams.geology.erosionStrength = 0.1f;
            tempParams.generateRivers = false;
            
            GeneratePresetTerrain();
        }

        void ApplyRandomPreset()
        {
            Debug.Log("[TerrainGeneratorEditor] 应用随机预设并立即生成干净地形");
            tempParams.heightVariation = Random.Range(50f, 250f);
            tempParams.enableGeologicalLayers = Random.value > 0.5f;
            tempParams.geology.erosionStrength = Random.Range(0.1f, 0.6f);
            tempParams.generateRivers = false; // 预设不包含河流，用户可以后续手动添加
            
            GeneratePresetTerrain();
        }

        /// <summary>
        /// 立即生成预设地形，确保清除所有河流痕迹
        /// </summary>
        void GeneratePresetTerrain()
        {
            Debug.Log("[TerrainGeneratorEditor] 开始生成预设地形 - 使用立即模式清除所有痕迹");
            
            // 强制使用立即生成模式，确保完全清除现有数据
            var realTimeField = typeof(AdvancedTerrainGenerator).GetField("enableRealTimeGeneration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressiveField = typeof(AdvancedTerrainGenerator).GetField("enableProgressiveGeneration", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // 保存原始值
            bool originalRealTime = (bool)realTimeField?.GetValue(terrainGen);
            bool originalProgressive = (bool)progressiveField?.GetValue(terrainGen);
            
            try
            {
                // 设置为立即模式
                realTimeField?.SetValue(terrainGen, true);
                progressiveField?.SetValue(terrainGen, false);
                
                // 创建临时参数并生成地形
                WorldGenerationParameters tempWorldParams = CreateTempWorldParams();
                terrainGen.GenerateTerrain(tempWorldParams);
                
                Debug.Log("[TerrainGeneratorEditor] 预设地形生成完成 - 已清除所有痕迹");
            }
            finally
            {
                // 恢复原始设置
                realTimeField?.SetValue(terrainGen, originalRealTime);
                progressiveField?.SetValue(terrainGen, originalProgressive);
            }
        }

        WorldGenerationParameters CreateTempWorldParams()
        {
            var worldParams = new WorldGenerationParameters();
            worldParams.terrainParams = tempParams;
            worldParams.areaSize = new Vector2(1000f, 1000f);
            
            return worldParams;
        }
        
        void AddNoiseLayer()
        {
            var newLayer = new NoiseLayerSettings
            {
                noiseType = NoiseType.Perlin,
                weight = 0.5f,
                frequency = 0.01f,
                amplitude = 1f,
                octaves = 4,
                persistence = 0.5f,
                lacunarity = 2f,
                offset = Vector2.zero
            };
            
            var newArray = new NoiseLayerSettings[tempParams.noiseLayers.Length + 1];
            for (int i = 0; i < tempParams.noiseLayers.Length; i++)
            {
                newArray[i] = tempParams.noiseLayers[i];
            }
            newArray[newArray.Length - 1] = newLayer;
            tempParams.noiseLayers = newArray;
        }
        
        void RemoveNoiseLayer()
        {
            if (tempParams.noiseLayers.Length <= 1) return;
            
            var newArray = new NoiseLayerSettings[tempParams.noiseLayers.Length - 1];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = tempParams.noiseLayers[i];
            }
            tempParams.noiseLayers = newArray;
        }
        
        void RandomizeSeed()
        {
            var noiseGen = terrainGen.GetComponent<NoiseGenerator>();
            if (noiseGen == null)
                noiseGen = terrainGen.gameObject.AddComponent<NoiseGenerator>();
            
            int newSeed = Random.Range(1, 999999);
            noiseGen.SetSeed(newSeed);
            Debug.Log($"新的随机种子: {newSeed}");
        }
        
        void SaveCurrentPreset()
        {
            string path = EditorUtility.SaveFilePanel("保存地形预设", "Assets", "TerrainPreset", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = JsonUtility.ToJson(tempParams, true);
                    System.IO.File.WriteAllText(path, json);
                    Debug.Log($"地形预设已保存到: {path}");
                    EditorUtility.DisplayDialog("保存成功", "地形预设已成功保存", "确定");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"保存预设失败: {e.Message}");
                    EditorUtility.DisplayDialog("保存失败", $"保存预设失败: {e.Message}", "确定");
                }
            }
        }
        
        void LoadPreset()
        {
            string path = EditorUtility.OpenFilePanel("加载地形预设", "Assets", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    var loadedParams = JsonUtility.FromJson<TerrainGenerationParams>(json);
                    if (loadedParams != null)
                    {
                        tempParams = loadedParams;
                        Debug.Log($"地形预设已加载: {path}");
                        EditorUtility.DisplayDialog("加载成功", "地形预设已成功加载", "确定");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载预设失败: {e.Message}");
                    EditorUtility.DisplayDialog("加载失败", $"加载预设失败: {e.Message}", "确定");
                }
            }
        }
        
        void ExportHeightmap()
        {
            if (terrainGen.GetComponent<Terrain>() == null)
            {
                EditorUtility.DisplayDialog("导出失败", "请先生成地形再导出高度图", "确定");
                return;
            }
            
            string path = EditorUtility.SaveFilePanel("导出高度图", "Assets", "Heightmap", "png");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    Terrain terrain = terrainGen.GetComponent<Terrain>();
                    var heightData = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
                    
                    int width = terrain.terrainData.heightmapResolution;
                    int height = terrain.terrainData.heightmapResolution;
                    
                    Texture2D heightTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float heightValue = heightData[y, x]; // 注意Unity的坐标系
                            Color pixelColor = new Color(heightValue, heightValue, heightValue, 1f);
                            heightTexture.SetPixel(x, y, pixelColor);
                        }
                    }
                    
                    heightTexture.Apply();
                    byte[] bytes = heightTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, bytes);
                    
                    DestroyImmediate(heightTexture);
                    Debug.Log($"高度图已导出到: {path}");
                    EditorUtility.DisplayDialog("导出成功", "高度图已成功导出", "确定");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"导出高度图失败: {e.Message}");
                    EditorUtility.DisplayDialog("导出失败", $"导出高度图失败: {e.Message}", "确定");
                }
            }
        }
        
        void ImportHeightmap()
        {
            string path = EditorUtility.OpenFilePanel("导入高度图", "Assets", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    byte[] imageData = System.IO.File.ReadAllBytes(path);
                    Texture2D heightTexture = new Texture2D(2, 2);
                    
                    if (heightTexture.LoadImage(imageData))
                    {
                        if (terrainGen.GetComponent<Terrain>() == null)
                        {
                            // 创建新地形
                            var terrain = terrainGen.gameObject.AddComponent<Terrain>();
                            var terrainData = new TerrainData();
                            terrainData.heightmapResolution = heightTexture.width;
                            terrainData.size = new Vector3(1000f, tempParams.heightVariation, 1000f);
                            terrain.terrainData = terrainData;
                        }
                        
                        Terrain targetTerrain = terrainGen.GetComponent<Terrain>();
                        int resolution = heightTexture.width;
                        
                        float[,] heights = new float[resolution, resolution];
                        
                        for (int x = 0; x < resolution; x++)
                        {
                            for (int y = 0; y < resolution; y++)
                            {
                                Color pixel = heightTexture.GetPixel(x, y);
                                heights[y, x] = pixel.grayscale; // 注意Unity的坐标系
                            }
                        }
                        
                        targetTerrain.terrainData.SetHeights(0, 0, heights);
                        
                        DestroyImmediate(heightTexture);
                        Debug.Log($"高度图已导入: {path}");
                        EditorUtility.DisplayDialog("导入成功", "高度图已成功导入", "确定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("导入失败", "无法读取图像文件", "确定");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"导入高度图失败: {e.Message}");
                    EditorUtility.DisplayDialog("导入失败", $"导入高度图失败: {e.Message}", "确定");
                }
            }
        }
        
        void OpenTexturePainter()
        {
            // 获取当前地形生成器的地形
            Terrain currentTerrain = terrainGen.GetComponent<Terrain>();
            if (currentTerrain == null)
            {
                // 尝试查找子对象中的地形
                currentTerrain = terrainGen.GetComponentInChildren<Terrain>();
            }
            
            if (currentTerrain == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到地形对象！请先生成地形。", "确定");
                return;
            }
            
            // 打开纹理绘制器并自动设置目标地形
            var window = TerrainTexturePainter.ShowWindowForTerrain(currentTerrain);
            Debug.Log($"已为地形 '{currentTerrain.name}' 打开纹理绘制器");
        }
    }
}