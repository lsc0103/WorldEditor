using UnityEngine;
using UnityEditor;
using WorldEditor.Core;
using System.Collections.Generic;

namespace WorldEditor.Editor
{
    /// <summary>
    /// 地形纹理绘制工具 - 新手友好的画笔绘制系统
    /// 让用户像画画一样在地形上绘制不同纹理
    /// </summary>
    public class TerrainTexturePainter : EditorWindow
    {
        [MenuItem("世界编辑器/地形纹理绘制器")]
        public static void ShowWindow()
        {
            TerrainTexturePainter window = GetWindow<TerrainTexturePainter>("地形纹理绘制器");
            window.minSize = new Vector2(300, 500);
            window.Show();
        }
        
        /// <summary>
        /// 为指定地形打开纹理绘制器（从地形生成器调用）
        /// </summary>
        public static TerrainTexturePainter ShowWindowForTerrain(Terrain terrain)
        {
            TerrainTexturePainter window = GetWindow<TerrainTexturePainter>("地形纹理绘制器");
            window.minSize = new Vector2(300, 500);
            
            // 自动设置目标地形
            window.targetTerrain = terrain;
            
            window.Show();
            return window;
        }
        
        // 画笔设置
        private float brushSize = 10f;
        private float brushStrength = 0.5f;
        private bool isPainting = false;
        
        // 纹理画笔类型
        public enum TextureBrushType
        {
            草地,
            沙漠,
            雪地,
            岩石,
            泥土,
            水面,
            石路,
            苔藓,
            自定义颜色,
            橡皮擦
        }
        
        // 画笔形状类型
        public enum BrushShape
        {
            圆形,      // 圆形，边缘柔和渐变
            方形,      // 方形，边缘柔和渐变
            硬圆形,    // 圆形，边缘锐利
            硬方形     // 方形，边缘锐利
        }
        
        // 专业地形纹理模板
        [System.Serializable]
        public class TerrainTemplate
        {
            public string name;
            public string description;
            public string emoji;
            public TerrainTemplateType type;
            
            public TerrainTemplate(string name, string description, string emoji, TerrainTemplateType type)
            {
                this.name = name;
                this.description = description;
                this.emoji = emoji;
                this.type = type;
            }
        }
        
        public enum TerrainTemplateType
        {
            // 自然地形类型
            平原草地,
            山脉雪峰,
            丘陵森林,
            河谷湿地,
            沙漠戈壁,
            海岸悬崖,
            高原台地,
            火山群岛,
            
            // 生态系统类型
            温带森林,
            热带雨林,
            北极苔原,
            地中海气候,
            
            // 人工地形
            农业区域,
            城市郊区,
            工业园区,
            度假村
        }
        
        
        private TextureBrushType selectedBrush = TextureBrushType.草地;
        private BrushShape brushShape = BrushShape.圆形;
        private Color customColor = Color.red; // 自定义颜色
        private Terrain targetTerrain;
        private List<Color> customColorLayers = new List<Color>(); // 已创建的自定义颜色层
        
        // 专业地形模板
        private TerrainTemplate[] terrainTemplates;
        
        // Tab页管理
        public enum TabType
        {
            画笔工具,
            专业模板
        }
        private TabType currentTab = TabType.画笔工具;
        
        // 预览设置
        private bool showBrushPreview = true;
        private Color brushPreviewColor = Color.white;
        
        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Tools.hidden = false;
            InitializeTerrainTemplates();
        }
        
        void InitializeTerrainTemplates()
        {
            terrainTemplates = new TerrainTemplate[]
            {
                // 自然地形 - 游戏级真实效果
                new TerrainTemplate("平原草地", "广阔平坦的草原，远山环绕，适合开放世界游戏", "🌾", TerrainTemplateType.平原草地),
                new TerrainTemplate("山脉雪峰", "高耸入云的雪山，分层明显的高山生态", "⛰️", TerrainTemplateType.山脉雪峰),
                new TerrainTemplate("丘陵森林", "起伏温和的森林丘陵，层次丰富的植被分布", "🌲", TerrainTemplateType.丘陵森林),
                new TerrainTemplate("河谷湿地", "蜿蜒河流穿越的湿润谷地，生机勃勃", "🏞️", TerrainTemplateType.河谷湿地),
                new TerrainTemplate("沙漠戈壁", "广袤的沙漠景观，沙丘与岩石的组合", "🏜️", TerrainTemplateType.沙漠戈壁),
                new TerrainTemplate("海岸悬崖", "壮观的海岸线，悬崖峭壁与海滩", "🌊", TerrainTemplateType.海岸悬崖),
                new TerrainTemplate("高原台地", "高海拔平台地形，开阔而神秘", "🗻", TerrainTemplateType.高原台地),
                new TerrainTemplate("火山群岛", "活跃的火山地貌，熔岩与灰烬的世界", "🌋", TerrainTemplateType.火山群岛),
                
                // 生态系统
                new TerrainTemplate("温带森林", "四季分明的温带森林生态系统", "🍂", TerrainTemplateType.温带森林),
                new TerrainTemplate("热带雨林", "茂密湿润的热带雨林，生物多样性丰富", "🌴", TerrainTemplateType.热带雨林),
                new TerrainTemplate("北极苔原", "严寒的极地苔原，冰雪覆盖的荒原", "🧊", TerrainTemplateType.北极苔原),
                new TerrainTemplate("地中海气候", "温暖干燥的地中海风情，橄榄与薰衣草", "🫒", TerrainTemplateType.地中海气候),
                
                // 人工环境
                new TerrainTemplate("农业区域", "现代化农田，规整的农作物种植区", "🚜", TerrainTemplateType.农业区域),
                new TerrainTemplate("城市郊区", "城市边缘的住宅区，绿化与建筑并存", "🏘️", TerrainTemplateType.城市郊区),
                new TerrainTemplate("工业园区", "现代工业区，混凝土与钢铁的世界", "🏭", TerrainTemplateType.工业园区),
                new TerrainTemplate("度假村", "精心设计的旅游度假区，景观与设施完美结合", "🏖️", TerrainTemplateType.度假村)
            };
        }
        
        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Tools.hidden = false;
            isPainting = false;
        }
        
        void OnGUI()
        {
            DrawHeader();
            DrawTabSelection();
            
            switch (currentTab)
            {
                case TabType.画笔工具:
                    DrawBrushTab();
                    break;
                case TabType.专业模板:
                    DrawTemplateTab();
                    break;
            }
            
            DrawTerrainSelection();
            DrawControls();
            DrawInstructions();
        }
        
        void DrawTabSelection()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 画笔工具Tab
            bool brushTabSelected = currentTab == TabType.画笔工具;
            GUI.backgroundColor = brushTabSelected ? Color.cyan : Color.white;
            if (GUILayout.Button("🖌️ 画笔工具", GUILayout.Height(30)))
            {
                currentTab = TabType.画笔工具;
            }
            
            // 专业模板Tab
            bool templateTabSelected = currentTab == TabType.专业模板;
            GUI.backgroundColor = templateTabSelected ? Color.cyan : Color.white;
            if (GUILayout.Button("🎯 专业模板", GUILayout.Height(30)))
            {
                currentTab = TabType.专业模板;
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        void DrawBrushTab()
        {
            DrawBrushSelection();
            DrawBrushSettings();
        }
        
        void DrawTemplateTab()
        {
            DrawTerrainTemplates();
        }
        
        void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("🎨 地形纹理绘制器", titleStyle);
            GUILayout.Label("像画画一样绘制地形纹理", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space();
            
            // 状态显示
            string status = isPainting ? "🖌️ 绘制模式激活" : "⭕ 未激活";
            Color statusColor = isPainting ? Color.green : Color.gray;
            
            GUI.color = statusColor;
            GUILayout.Label($"状态: {status}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawBrushSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🖌️ 选择画笔", EditorStyles.boldLabel);
            
            // 准备所有画笔数据
            var brushData = new (string label, TextureBrushType type, Color color)[]
            {
                ("🌱\n草地", TextureBrushType.草地, new Color(0.4f, 0.8f, 0.3f)),
                ("🏜️\n沙漠", TextureBrushType.沙漠, new Color(0.8f, 0.7f, 0.4f)),
                ("❄️\n雪地", TextureBrushType.雪地, new Color(0.9f, 0.9f, 1.0f)),
                ("🪨\n岩石", TextureBrushType.岩石, new Color(0.5f, 0.5f, 0.5f)),
                ("🟫\n泥土", TextureBrushType.泥土, new Color(0.6f, 0.4f, 0.2f)),
                ("💧\n水面", TextureBrushType.水面, new Color(0.2f, 0.6f, 0.9f)),
                ("🛤️\n石路", TextureBrushType.石路, new Color(0.7f, 0.7f, 0.6f)),
                ("🍃\n苔藓", TextureBrushType.苔藓, new Color(0.2f, 0.5f, 0.2f)),
                ("🎨\n自定义", TextureBrushType.自定义颜色, customColor),
                ("🧽\n橡皮擦", TextureBrushType.橡皮擦, Color.white)
            };
            
            // 动态计算每行的画笔数量
            float windowWidth = EditorGUIUtility.currentViewWidth - 40; // 减去边距
            int brushesPerRow = Mathf.Max(1, Mathf.FloorToInt(windowWidth / 70)); // 每个画笔按钮70px宽度
            
            // 按行显示画笔
            for (int i = 0; i < brushData.Length; i += brushesPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int j = 0; j < brushesPerRow && i + j < brushData.Length; j++)
                {
                    var brush = brushData[i + j];
                    if (DrawBrushButton(brush.label, brush.type, brush.color))
                    {
                        selectedBrush = brush.type;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // 自定义颜色设置区域
            if (selectedBrush == TextureBrushType.自定义颜色)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("🎨 自定义颜色设置", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("选择颜色:", EditorStyles.miniLabel);
                Color newColor = EditorGUILayout.ColorField(customColor, GUILayout.Width(60), GUILayout.Height(30));
                if (newColor != customColor)
                {
                    customColor = newColor;
                    // 纹理层会在绘制时按需创建
                }
                EditorGUILayout.EndHorizontal();
                
                // 快捷颜色按钮
                GUILayout.Label("快捷颜色:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (DrawColorButton(Color.red, "红"))
                    SetCustomColor(Color.red);
                if (DrawColorButton(Color.blue, "蓝"))
                    SetCustomColor(Color.blue);
                if (DrawColorButton(Color.yellow, "黄"))
                    SetCustomColor(Color.yellow);
                if (DrawColorButton(Color.magenta, "紫"))
                    SetCustomColor(Color.magenta);
                if (DrawColorButton(Color.green, "绿"))
                    SetCustomColor(Color.green);
                if (DrawColorButton(Color.cyan, "青"))
                    SetCustomColor(Color.cyan);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            if (selectedBrush == TextureBrushType.自定义颜色)
            {
                GUILayout.Label($"当前画笔: 自定义颜色 {ColorToHex(customColor)}", EditorStyles.miniLabel);
            }
            else if (selectedBrush == TextureBrushType.橡皮擦)
            {
                GUILayout.Label($"当前画笔: 橡皮擦 (恢复白色基础)", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label($"当前画笔: {selectedBrush}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawTerrainTemplates()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 模板页面标题
            GUILayout.Label("🎯 专业地形模板 (新手推荐)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("选择一个模板，一键应用到整个地形:", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("?", GUILayout.Width(20)))
            {
                EditorUtility.DisplayDialog("专业地形模板", 
                    "这些是为新手用户准备的专业地形纹理模板，一键应用即可获得真实的地形效果。\n\n" +
                    "每个模板都经过精心设计，模拟真实世界的地形特征，让您无需绘画技巧也能创建专业级地形。", "了解");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 动态计算每行的模板数量
            float windowWidth = EditorGUIUtility.currentViewWidth - 40; // 减去边距
            int buttonsPerRow = Mathf.Max(2, Mathf.FloorToInt(windowWidth / 110)); // 每个按钮最小110px宽度
            
            // 按行显示模板
            for (int i = 0; i < terrainTemplates.Length; i += buttonsPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int j = 0; j < buttonsPerRow && i + j < terrainTemplates.Length; j++)
                {
                    if (DrawTemplateButton(terrainTemplates[i + j]))
                    {
                        ApplyTerrainTemplate(terrainTemplates[i + j].type);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("💡 提示: 应用模板后，您依然可以使用画笔工具进行细节调整", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        bool DrawBrushButton(string label, TextureBrushType brushType, Color brushColor)
        {
            bool isSelected = selectedBrush == brushType;
            
            if (isSelected)
            {
                GUI.backgroundColor = brushColor;
            }
            
            bool clicked = GUILayout.Button(label, GUILayout.Height(60), GUILayout.Width(65));
            
            GUI.backgroundColor = Color.white;
            
            return clicked;
        }
        
        bool DrawColorButton(Color color, string label)
        {
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(label, GUILayout.Width(35), GUILayout.Height(25));
            GUI.backgroundColor = Color.white;
            return clicked;
        }
        
        bool DrawShapeButton(string symbol, BrushShape shape, string tooltip)
        {
            bool isSelected = brushShape == shape;
            
            if (isSelected)
            {
                GUI.backgroundColor = Color.cyan;
            }
            
            GUIContent content = new GUIContent(symbol, tooltip);
            bool clicked = GUILayout.Button(content, GUILayout.Width(30), GUILayout.Height(30));
            
            GUI.backgroundColor = Color.white;
            
            return clicked;
        }
        
        bool DrawTemplateButton(TerrainTemplate template)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            
            string buttonText = $"{template.emoji}\n{template.name}";
            GUIContent content = new GUIContent(buttonText, template.description);
            
            return GUILayout.Button(content, buttonStyle, GUILayout.Height(50), GUILayout.ExpandWidth(true));
        }
        
        /// <summary>
        /// 应用专业地形模板，生成真实的地形纹理分布
        /// </summary>
        void ApplyTerrainTemplate(TerrainTemplateType templateType)
        {
            if (targetTerrain == null || targetTerrain.terrainData == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个地形对象", "确定");
                return;
            }
            
            if (EditorUtility.DisplayDialog("应用地形模板", 
                $"确定要应用 '{GetTemplateName(templateType)}' 模板吗？\n\n这将覆盖当前的地形纹理。", 
                "应用", "取消"))
            {
                // 确保有足够的纹理层
                if (targetTerrain.terrainData.terrainLayers == null || targetTerrain.terrainData.terrainLayers.Length < 9)
                {
                    CreateBasePaintingLayers();
                }
                
                // 根据模板类型生成纹理分布
                GenerateAdvancedTemplateTextures(templateType);
                
                Debug.Log($"已成功应用地形模板: {GetTemplateName(templateType)}");
                EditorUtility.DisplayDialog("模板应用成功", 
                    $"'{GetTemplateName(templateType)}' 模板已成功应用到地形！\n\n您现在可以使用画笔工具进行细节调整。", "确定");
            }
        }
        
        
        string GetTemplateName(TerrainTemplateType templateType)
        {
            foreach (var template in terrainTemplates)
            {
                if (template.type == templateType)
                    return template.name;
            }
            return templateType.ToString();
        }
        
        
        /// <summary>
        /// 根据模板类型生成高级的游戏品质地形纹理分布
        /// </summary>
        void GenerateAdvancedTemplateTextures(TerrainTemplateType templateType)
        {
            TerrainData terrainData = targetTerrain.terrainData;
            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int layerCount = terrainData.terrainLayers.Length;
            
            float[,,] alphamap = new float[alphamapWidth, alphamapHeight, layerCount];
            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            
            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int y = 0; y < alphamapHeight; y++)
                {
                    float normalizedX = (float)x / alphamapWidth;
                    float normalizedY = (float)y / alphamapHeight;
                    
                    // 获取高度信息
                    int heightX = Mathf.FloorToInt(normalizedX * terrainData.heightmapResolution);
                    int heightY = Mathf.FloorToInt(normalizedY * terrainData.heightmapResolution);
                    heightX = Mathf.Clamp(heightX, 0, terrainData.heightmapResolution - 1);
                    heightY = Mathf.Clamp(heightY, 0, terrainData.heightmapResolution - 1);
                    
                    float currentHeight = heights[heightX, heightY];
                    
                    // 多层噪声用于自然分布
                    float noise1 = Mathf.PerlinNoise(normalizedX * 4f, normalizedY * 4f);
                    float noise2 = Mathf.PerlinNoise(normalizedX * 8f, normalizedY * 8f);
                    float noise3 = Mathf.PerlinNoise(normalizedX * 16f, normalizedY * 16f);
                    float detailNoise = Mathf.PerlinNoise(normalizedX * 32f, normalizedY * 32f);
                    
                    float combinedNoise = (noise1 + noise2 * 0.5f + noise3 * 0.25f + detailNoise * 0.1f) / 1.85f;
                    
                    // 距离中心的距离（用于径向效果）
                    float distanceFromCenter = Vector2.Distance(new Vector2(normalizedX, normalizedY), new Vector2(0.5f, 0.5f));
                    
                    // 根据模板类型应用高级纹理分布
                    ApplyAdvancedTemplateTextures(alphamap, x, y, currentHeight, normalizedX, normalizedY, 
                                                 combinedNoise, distanceFromCenter, templateType, 
                                                 noise1, noise2, noise3, detailNoise);
                    
                    // 归一化权重
                    NormalizeTemplateWeights(alphamap, x, y, layerCount);
                }
            }
            
            // 应用生成的alphamap
            terrainData.SetAlphamaps(0, 0, alphamap);
            targetTerrain.Flush();
        }
        
        /// <summary>
        /// 为不同模板类型应用真实的游戏级多层纹理分布
        /// </summary>
        void ApplyAdvancedTemplateTextures(float[,,] alphamap, int x, int y, float height, 
                                          float normalizedX, float normalizedY, float combinedNoise, 
                                          float distanceFromCenter, TerrainTemplateType templateType,
                                          float noise1, float noise2, float noise3, float detailNoise)
        {
            // 计算坡度因子（用于现实感的地形分布）
            float slopeInfluence = Mathf.Abs(height - 0.5f) * 2f; // 0-1范围
            
            // 温度区域模拟（用于不同气候的地形）
            float temperatureZone = (normalizedY + noise1 * 0.2f); // 北在上，南在下
            
            switch (templateType)
            {
                case TerrainTemplateType.平原草地:
                    // 真实的大平原：主要草地 + 自然风化区域 + 小型水系
                    float grassDensity = 0.75f + noise1 * 0.25f; // 草地密度变化
                    alphamap[x, y, 1] = grassDensity;
                    
                    // 风化作用产生的沙土区域
                    if (noise2 > 0.7f)
                        alphamap[x, y, 2] = 0.2f + detailNoise * 0.15f; // 沙土斑块
                    
                    // 沼泽地和小溪（低洼处）
                    if (height < 0.25f && combinedNoise < 0.3f)
                    {
                        alphamap[x, y, 6] = 0.6f + noise3 * 0.3f; // 水域
                        alphamap[x, y, 8] = 0.4f + detailNoise * 0.2f; // 沼泽植被
                    }
                    
                    // 小山丘区域
                    if (height > 0.6f && noise1 > 0.6f)
                        alphamap[x, y, 4] = 0.2f + noise2 * 0.1f; // 零星岩石
                    break;
                    
                case TerrainTemplateType.山脉雪峰:
                    // 高山垂直带谱：雪线、高山草甸、针叶林、山脚
                    if (height > 0.8f) // 高山带：永久的雪盖
                    {
                        alphamap[x, y, 3] = 0.95f + detailNoise * 0.05f; // 厚雪
                        // 风吹估的岩石露头
                        if (noise1 > 0.8f) alphamap[x, y, 4] = 0.15f + noise2 * 0.1f;
                    }
                    else if (height > 0.6f) // 亚高山带：高山草甸
                    {
                        alphamap[x, y, 8] = 0.6f + noise2 * 0.3f; // 高山苔藓和矮灌
                        alphamap[x, y, 4] = 0.3f + noise3 * 0.2f; // 碎石坡
                        // 残雪在阴面坡上
                        if (noise1 < 0.4f) alphamap[x, y, 3] = 0.2f + detailNoise * 0.15f;
                    }
                    else if (height > 0.35f) // 山地针叶林带
                    {
                        alphamap[x, y, 1] = 0.4f + noise1 * 0.3f; // 森林地被
                        alphamap[x, y, 8] = 0.4f + noise2 * 0.2f; // 针叶林苔藓
                        alphamap[x, y, 5] = 0.2f + noise3 * 0.15f; // 森林土
                    }
                    else // 山脚带：温带森林草地
                    {
                        alphamap[x, y, 1] = 0.7f + noise1 * 0.2f; // 山脚草地
                        alphamap[x, y, 5] = 0.25f + noise2 * 0.15f; // 肥沃土壤
                        // 山溪和小河
                        if (combinedNoise < 0.2f) alphamap[x, y, 6] = 0.3f + detailNoise * 0.2f;
                    }
                    break;
                    
                case TerrainTemplateType.丘陵森林:
                    // 混交林生态：丰富的植被层次和微地形
                    float forestDensity = 0.6f + noise1 * 0.3f;
                    alphamap[x, y, 1] = forestDensity; // 林下草本
                    alphamap[x, y, 8] = 0.4f + noise2 * 0.4f; // 苔藓和蕨类
                    
                    // 林间空地和草地
                    if (noise3 > 0.7f)
                        alphamap[x, y, 5] = 0.3f + detailNoise * 0.2f; // 落叶土壤
                    
                    // 溪流和湖泊（在低洼处）
                    if (height < 0.3f && combinedNoise < 0.25f)
                    {
                        alphamap[x, y, 6] = 0.7f + noise1 * 0.2f; // 清澈溪流
                        alphamap[x, y, 8] = 0.6f + detailNoise * 0.3f; // 水边苔藓
                    }
                    
                    // 山脊处的岩石露头
                    if (height > 0.7f && noise2 > 0.8f)
                        alphamap[x, y, 4] = 0.25f + noise3 * 0.15f;
                    break;
                    
                case TerrainTemplateType.河谷湿地:
                    // 河流系统：主河道 + 支流 + 洪泛平原 + 河岸阶地
                    float riverDistance = Mathf.Abs(normalizedY - 0.5f);
                    float riverMeander = Mathf.Sin(normalizedX * 8f + noise1 * 2f) * 0.05f; // 河流弯曲
                    float adjustedRiverDistance = riverDistance + riverMeander;
                    
                    if (adjustedRiverDistance < 0.08f) // 主河道
                    {
                        alphamap[x, y, 6] = 0.95f + detailNoise * 0.05f; // 深水区
                    }
                    else if (adjustedRiverDistance < 0.2f) // 河滩和浅水区
                    {
                        alphamap[x, y, 6] = 0.6f + noise2 * 0.3f; // 浅水
                        alphamap[x, y, 2] = 0.3f + noise3 * 0.2f; // 沙滩砂石
                        alphamap[x, y, 8] = 0.2f + detailNoise * 0.15f; // 水边植被
                    }
                    else if (adjustedRiverDistance < 0.4f) // 湿地草地
                    {
                        alphamap[x, y, 8] = 0.7f + noise1 * 0.25f; // 湿地苔藓
                        alphamap[x, y, 1] = 0.4f + noise2 * 0.3f; // 湿润草地
                        alphamap[x, y, 5] = 0.3f + noise3 * 0.2f; // 湿润土壤
                    }
                    else // 高地台地
                    {
                        alphamap[x, y, 1] = 0.8f + noise1 * 0.15f; // 早生草地
                        alphamap[x, y, 5] = 0.15f + noise2 * 0.1f; // 台地土壤
                    }
                    break;
                    
                case TerrainTemplateType.沙漠戈壁:
                    // 干旱沙漠：沙丘 + 戈壁 + 绿洲 + 干河床
                    float duneMagnitude = Mathf.Sin(normalizedX * 6f + noise1 * 3f) * 0.3f + 0.5f;
                    
                    // 主体沙漠区域
                    alphamap[x, y, 2] = duneMagnitude * (0.8f + noise2 * 0.2f); // 沙丘
                    
                    // 戈壁地区（岩石露头）
                    if (noise3 > 0.75f)
                        alphamap[x, y, 4] = 0.4f + detailNoise * 0.3f; // 风化岩石
                    
                    // 遗存绿洲（水源附近）
                    if (combinedNoise < 0.05f && distanceFromCenter < 0.25f)
                    {
                        alphamap[x, y, 1] = 0.5f + noise1 * 0.3f; // 绿洲植被
                        alphamap[x, y, 6] = 0.4f + noise2 * 0.25f; // 地下水源
                        alphamap[x, y, 8] = 0.3f + detailNoise * 0.2f; // 沙漠苔藓
                    }
                    
                    // 干涸的河床（季节性水流）
                    if (Mathf.Abs(normalizedY - 0.3f) < 0.1f + noise1 * 0.05f)
                        alphamap[x, y, 5] = 0.3f + noise3 * 0.2f; // 干涸的泥土
                    break;
                    
                case TerrainTemplateType.海岸悬崖:
                    // 海岸地貌：悬崖顶部 + 峭壁 + 滩涂 + 海洋
                    if (normalizedY > 0.75f) // 悬崖顶部高原
                    {
                        alphamap[x, y, 1] = 0.8f + noise1 * 0.15f; // 海风草地
                        // 面向海洋的风化作用
                        if (noise2 > 0.7f) alphamap[x, y, 4] = 0.25f + detailNoise * 0.15f; // 风化岩石
                    }
                    else if (normalizedY > 0.6f) // 悬崖斜坡
                    {
                        alphamap[x, y, 4] = 0.7f + noise2 * 0.2f; // 斜坡岩石
                        alphamap[x, y, 1] = 0.2f + noise3 * 0.15f; // 稀疏海岸植被
                        alphamap[x, y, 8] = 0.1f + detailNoise * 0.1f; // 岩缝苔藓
                    }
                    else if (normalizedY > 0.25f) // 峭壁
                    {
                        alphamap[x, y, 4] = 0.95f + detailNoise * 0.05f; // 垂直岩壁
                        if (noise1 < 0.2f) alphamap[x, y, 8] = 0.05f + noise2 * 0.05f; // 岩缝植被
                    }
                    else if (normalizedY > 0.1f) // 岩石滩
                    {
                        alphamap[x, y, 4] = 0.6f + noise3 * 0.3f; // 海蜥岩石
                        alphamap[x, y, 2] = 0.3f + detailNoise * 0.2f; // 滩涂沙砂
                    }
                    else // 海洋
                    {
                        alphamap[x, y, 6] = 1.0f; // 深蓝海水
                    }
                    break;
                    
                case TerrainTemplateType.高原台地:
                    // 高海拔平原：高山草甸 + 季节性积雪 + 高原湖泊
                    alphamap[x, y, 1] = 0.7f + noise1 * 0.2f; // 高原草甸
                    
                    // 高原风化作用
                    if (noise2 > 0.6f)
                        alphamap[x, y, 5] = 0.3f + noise3 * 0.2f; // 风化土壤
                    
                    // 季节性积雪（在北向斜坡）
                    if (temperatureZone > 0.7f && noise1 < 0.4f)
                        alphamap[x, y, 3] = 0.4f + detailNoise * 0.2f; // 雪斑
                    
                    // 高原湖泊系统
                    if (combinedNoise < 0.15f)
                    {
                        alphamap[x, y, 6] = 0.8f + noise2 * 0.15f; // 高原湖泊
                        alphamap[x, y, 8] = 0.3f + detailNoise * 0.2f; // 湖边沼泽
                    }
                    
                    // 风颤区域的岩石露头
                    if (height > 0.8f && noise3 > 0.8f)
                        alphamap[x, y, 4] = 0.2f + noise1 * 0.15f;
                    break;
                    
                case TerrainTemplateType.火山群岛:
                    // 火山地貌：火山熔岩 + 灰烬地 + 新生熔岩 + 残存植被
                    float volcanicActivity = combinedNoise;
                    
                    if (height > 0.7f) // 火山锥顶区域
                    {
                        alphamap[x, y, 4] = 0.8f + noise1 * 0.2f; // 凝固熔岩
                        if (volcanicActivity > 0.8f) // 活跃火山口
                            alphamap[x, y, 5] = 0.6f + detailNoise * 0.3f; // 火山灰
                    }
                    else if (height > 0.4f) // 火山斜坡
                    {
                        alphamap[x, y, 4] = 0.6f + noise2 * 0.3f; // 熔岩流
                        alphamap[x, y, 5] = 0.4f + noise3 * 0.2f; // 火山土
                        // 残存植被（遵循熔岩流路径）
                        if (noise1 < 0.3f) alphamap[x, y, 1] = 0.2f + detailNoise * 0.15f;
                    }
                    else if (height > 0.1f) // 岛屿平原
                    {
                        alphamap[x, y, 5] = 0.5f + noise1 * 0.3f; // 火山灰土
                        alphamap[x, y, 1] = 0.4f + noise2 * 0.25f; // 新生植被
                        // 温泉和地热区域
                        if (combinedNoise > 0.85f) alphamap[x, y, 6] = 0.3f + noise3 * 0.2f;
                    }
                    else // 海岸线
                    {
                        alphamap[x, y, 6] = 0.9f + detailNoise * 0.1f; // 海水
                        alphamap[x, y, 4] = 0.2f + noise1 * 0.1f; // 海岸熔岩
                    }
                    break;
                    
                case TerrainTemplateType.温带森林:
                    // 混交林生态：复杂的植被结构和季节性变化
                    float canopyCover = 0.6f + noise1 * 0.3f;
                    alphamap[x, y, 1] = canopyCover * 0.6f; // 林下草本层
                    alphamap[x, y, 8] = canopyCover * 0.5f + noise2 * 0.3f; // 苔藓和蕨类
                    alphamap[x, y, 5] = 0.4f + noise3 * 0.2f; // 落叶腐殖质
                    
                    // 森林空地和草地
                    if (noise1 > 0.8f)
                        alphamap[x, y, 1] = 0.8f + detailNoise * 0.15f; // 草地空间
                    
                    // 源泉和溪流
                    if (height < 0.25f && combinedNoise < 0.2f)
                    {
                        alphamap[x, y, 6] = 0.7f + noise2 * 0.2f; // 清澈溪流
                        alphamap[x, y, 8] = 0.8f + detailNoise * 0.15f; // 水边苔藓
                    }
                    
                    // 山脊岩石露头
                    if (height > 0.75f && noise3 > 0.75f)
                        alphamap[x, y, 4] = 0.3f + noise1 * 0.2f; // 花岗岩露头
                    break;
                    
                case TerrainTemplateType.热带雨林:
                    // 热带雨林：极高的生物多样性和层次结构
                    float rainforestDensity = 0.8f + noise1 * 0.2f;
                    alphamap[x, y, 8] = rainforestDensity * 0.7f; // 厚重的苔藓层
                    alphamap[x, y, 1] = rainforestDensity * 0.4f + noise2 * 0.2f; // 底层植被
                    alphamap[x, y, 5] = 0.3f + noise3 * 0.15f; // 湿润腐殖土
                    
                    // 河流和水潭
                    if (height < 0.2f && combinedNoise < 0.3f)
                    {
                        alphamap[x, y, 6] = 0.8f + detailNoise * 0.15f; // 雨林河流
                        alphamap[x, y, 8] = 0.9f + noise1 * 0.1f; // 繁茂水边植被
                    }
                    
                    // 山顶云雾林（高海拔区域）
                    if (height > 0.7f)
                    {
                        alphamap[x, y, 8] = 0.9f + detailNoise * 0.1f; // 附生植物
                        alphamap[x, y, 1] = 0.3f + noise2 * 0.2f; // 稀疏地被
                    }
                    break;
                    
                case TerrainTemplateType.北极苔原:
                    // 寒带苔原：永久冻土 + 苔藓 + 季节性雪盖
                    alphamap[x, y, 8] = 0.6f + noise1 * 0.3f; // 苔藓和地衣
                    alphamap[x, y, 5] = 0.4f + noise2 * 0.2f; // 冻土和泥炎
                    
                    // 季节性雪盖（冬季）
                    if (temperatureZone > 0.6f)
                        alphamap[x, y, 3] = 0.7f + noise3 * 0.2f; // 积雪
                    
                    // 小型永久冻土湖泊
                    if (combinedNoise < 0.1f && distanceFromCenter < 0.3f)
                        alphamap[x, y, 6] = 0.5f + detailNoise * 0.2f; // 冻土湖
                    
                    // 岩石露头（风化作用）
                    if (noise3 > 0.8f && height > 0.5f)
                        alphamap[x, y, 4] = 0.3f + noise1 * 0.2f; // 风化岩石
                    break;
                    
                case TerrainTemplateType.地中海气候:
                    // 地中海植被：旱生灌丛 + 草地 + 香草植物
                    alphamap[x, y, 1] = 0.6f + noise1 * 0.3f; // 地中海草地
                    alphamap[x, y, 8] = 0.3f + noise2 * 0.2f; // 香草类植物
                    
                    // 旱生灌丛区域（橄榄林）
                    if (noise3 > 0.6f)
                        alphamap[x, y, 5] = 0.4f + detailNoise * 0.2f; // 早生土壤
                    
                    // 地中海气候特有的岩石地貌
                    if (height > 0.6f && noise1 > 0.7f)
                        alphamap[x, y, 4] = 0.35f + noise2 * 0.2f; // 石灰岩地貌
                    
                    // 季节性小溪（干湿季交替）
                    if (height < 0.3f && combinedNoise < 0.3f)
                        alphamap[x, y, 6] = 0.4f + noise3 * 0.25f; // 季节性水流
                    break;
                    
                case TerrainTemplateType.农业区域:
                    // 现代农业：规模化种植 + 灌溉系统 + 道路网络
                    int gridSize = 6;
                    int fieldX = Mathf.FloorToInt(normalizedX * gridSize);
                    int fieldY = Mathf.FloorToInt(normalizedY * gridSize);
                    bool isMainRoad = (fieldX % 3 == 0) || (fieldY % 3 == 0);
                    bool isFieldRoad = (fieldX % 2 == 0) || (fieldY % 2 == 0);
                    
                    if (isMainRoad) // 主要道路
                    {
                        alphamap[x, y, 7] = 0.9f + detailNoise * 0.1f; // 水泥路
                    }
                    else if (isFieldRoad) // 田间小路
                    {
                        alphamap[x, y, 5] = 0.7f + noise1 * 0.2f; // 土路
                    }
                    else // 农田区域
                    {
                        int cropType = (fieldX + fieldY) % 3;
                        switch (cropType)
                        {
                            case 0: // 谷物作物
                                alphamap[x, y, 1] = 0.8f + noise2 * 0.15f; // 绿色作物
                                break;
                            case 1: // 根茎类作物
                                alphamap[x, y, 5] = 0.7f + noise3 * 0.2f; // 耕地
                                break;
                            case 2: // 油料作物
                                alphamap[x, y, 1] = 0.6f + detailNoise * 0.2f; // 混合作物
                                alphamap[x, y, 2] = 0.2f + noise1 * 0.1f; // 收获后的土地
                                break;
                        }
                    }
                    
                    // 灌溉水渠
                    if (Mathf.Abs(normalizedY - 0.5f) < 0.05f + noise1 * 0.02f)
                        alphamap[x, y, 6] = 0.6f + noise2 * 0.2f; // 灌溉水道
                    break;
                    
                case TerrainTemplateType.城市郊区:
                    // 郊区地区：住宅绿化 + 公共绿地 + 交通网络
                    alphamap[x, y, 1] = 0.7f + noise1 * 0.2f; // 基础绿化草坪
                    
                    // 城市道路网络
                    bool isUrbanRoad = (Mathf.FloorToInt(normalizedX * 8) % 3 == 0) || 
                                      (Mathf.FloorToInt(normalizedY * 8) % 3 == 0);
                    if (isUrbanRoad)
                        alphamap[x, y, 7] = 0.8f + detailNoise * 0.15f; // 柏油路
                    
                    // 公园和绿化带
                    if (noise2 > 0.7f)
                    {
                        alphamap[x, y, 1] = 0.9f + noise3 * 0.1f; // 精心维护的草坪
                        alphamap[x, y, 8] = 0.1f + detailNoise * 0.1f; // 装饰性植被
                    }
                    
                    // 小型人工水体（景观池塘）
                    if (combinedNoise < 0.05f && distanceFromCenter < 0.2f)
                        alphamap[x, y, 6] = 0.8f + noise1 * 0.15f; // 人工水体
                    break;
                    
                case TerrainTemplateType.工业园区:
                    // 工业区：混凝土 + 工业土壤 + 污染区域
                    alphamap[x, y, 7] = 0.6f + noise1 * 0.3f; // 混凝土地面
                    alphamap[x, y, 5] = 0.4f + noise2 * 0.2f; // 工业土壤
                    
                    // 道路和停车场
                    if ((Mathf.FloorToInt(normalizedX * 6) % 2 == 0) || 
                        (Mathf.FloorToInt(normalizedY * 6) % 2 == 0))
                        alphamap[x, y, 7] = 0.9f + detailNoise * 0.1f; // 沙磾路面
                    
                    // 残存绿化（少量）
                    if (noise3 < 0.2f)
                        alphamap[x, y, 1] = 0.3f + detailNoise * 0.15f; // 抗性植被
                    
                    // 污染水体
                    if (height < 0.2f && combinedNoise < 0.15f)
                        alphamap[x, y, 6] = 0.5f + noise1 * 0.2f; // 工业污水
                    break;
                    
                case TerrainTemplateType.度假村:
                    // 度假区：景观草坪 + 人工水体 + 休闲设施
                    alphamap[x, y, 1] = 0.8f + noise1 * 0.15f; // 高品质维护草坪
                    
                    // 度假村道路（景观性设计）
                    float pathPattern = Mathf.Sin(normalizedX * 10f + noise1 * 2f) * 
                                       Mathf.Sin(normalizedY * 10f + noise2 * 2f);
                    if (pathPattern > 0.7f)
                        alphamap[x, y, 7] = 0.7f + detailNoise * 0.2f; // 景观石路
                    
                    // 人工湖泊和水景
                    if (distanceFromCenter < 0.3f && combinedNoise < 0.3f)
                    {
                        alphamap[x, y, 6] = 0.9f + noise3 * 0.1f; // 清澈人工湖
                        alphamap[x, y, 1] = 0.95f + detailNoise * 0.05f; // 湖边精美草坪
                    }
                    
                    // 花园和景观植被
                    if (noise2 > 0.8f)
                        alphamap[x, y, 8] = 0.4f + noise3 * 0.2f; // 装饰性植被
                    break;
                    
                default:
                    // 默认混合地形：自然草地景观
                    alphamap[x, y, 1] = 0.6f + noise1 * 0.3f; // 基础草地
                    alphamap[x, y, 5] = 0.25f + noise2 * 0.2f; // 土壤散布
                    if (noise3 > 0.75f) alphamap[x, y, 4] = 0.3f + detailNoise * 0.2f; // 岩石露头
                    if (height < 0.3f && combinedNoise < 0.2f) alphamap[x, y, 6] = 0.4f + noise1 * 0.2f; // 小水体
                    break;
            }
        }
        
        /// <summary>
        /// 根据模板类型生成专业的地形纹理分布（旧版本保留）
        /// </summary>
        void GenerateTemplateTextures(TerrainTemplateType templateType)
        {
            int alphamapWidth = targetTerrain.terrainData.alphamapWidth;
            int alphamapHeight = targetTerrain.terrainData.alphamapHeight;
            int layerCount = targetTerrain.terrainData.terrainLayers.Length;
            
            float[,,] alphamap = new float[alphamapWidth, alphamapHeight, layerCount];
            
            // 使用Perlin噪声生成自然的纹理分布
            System.Random random = new System.Random();
            
            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int y = 0; y < alphamapHeight; y++)
                {
                    // 归一化坐标
                    float normalizedX = (float)x / alphamapWidth;
                    float normalizedY = (float)y / alphamapHeight;
                    
                    // 根据模板类型设置不同的纹理权重
                    SetTemplateWeights(alphamap, x, y, normalizedX, normalizedY, templateType, random);
                    
                    // 归一化权重
                    NormalizeTemplateWeights(alphamap, x, y, layerCount);
                }
            }
            
            // 应用生成的alphamap
            targetTerrain.terrainData.SetAlphamaps(0, 0, alphamap);
            targetTerrain.Flush();
        }
        
        void SetTemplateWeights(float[,,] alphamap, int x, int y, float normalizedX, float normalizedY, 
                               TerrainTemplateType templateType, System.Random random)
        {
            // 生成多层次的噪声用于自然分布
            float noise1 = Mathf.PerlinNoise(normalizedX * 3f, normalizedY * 3f);
            float noise2 = Mathf.PerlinNoise(normalizedX * 8f, normalizedY * 8f);
            float noise3 = Mathf.PerlinNoise(normalizedX * 15f, normalizedY * 15f);
            float combinedNoise = (noise1 + noise2 * 0.5f + noise3 * 0.25f) / 1.75f;
            
            // 距离中心的距离（用于创建渐变效果）
            float distanceFromCenter = Vector2.Distance(new Vector2(normalizedX, normalizedY), new Vector2(0.5f, 0.5f));
            
            switch (templateType)
            {
                // 旧版模板支持（保持兼容性）
                default:
                    // 默认草地分布
                    alphamap[x, y, 1] = 0.7f + noise1 * 0.3f; // 草地
                    alphamap[x, y, 5] = 0.2f + noise2 * 0.2f; // 泥土
                    break;
            }
        }
        
        void NormalizeTemplateWeights(float[,,] alphamap, int x, int y, int layerCount)
        {
            float totalWeight = 0f;
            
            // 计算总权重
            for (int layer = 0; layer < layerCount; layer++)
            {
                totalWeight += alphamap[x, y, layer];
            }
            
            // 如果没有设置任何纹理，默认为白色基础
            if (totalWeight <= 0f)
            {
                alphamap[x, y, 0] = 1f;
                return;
            }
            
            // 归一化所有层的权重
            for (int layer = 0; layer < layerCount; layer++)
            {
                alphamap[x, y, layer] /= totalWeight;
            }
        }
        
        void SetCustomColor(Color color)
        {
            customColor = color;
            // 纹理层会在绘制时按需创建
        }
        
        string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
        
        /// <summary>
        /// 根据画笔形状和位置计算绘制强度
        /// </summary>
        float CalculateBrushStrength(float dx, float dy, float brushPixelSize)
        {
            switch (brushShape)
            {
                case BrushShape.圆形:
                    {
                        // 柔边圆形：距离越远强度越低
                        float distance = Vector2.Distance(new Vector2(dx, dy), new Vector2(brushPixelSize, brushPixelSize));
                        if (distance > brushPixelSize) return 0f;
                        
                        float falloff = 1f - (distance / brushPixelSize);
                        return brushStrength * falloff;
                    }
                    
                case BrushShape.方形:
                    {
                        // 柔边方形：距离中心越远强度越低
                        float centerX = brushPixelSize;
                        float centerY = brushPixelSize;
                        float maxDistance = brushPixelSize;
                        
                        float distanceFromCenter = Mathf.Max(Mathf.Abs(dx - centerX), Mathf.Abs(dy - centerY));
                        if (distanceFromCenter > maxDistance) return 0f;
                        
                        float falloff = 1f - (distanceFromCenter / maxDistance);
                        return brushStrength * falloff;
                    }
                    
                case BrushShape.硬圆形:
                    {
                        // 硬边圆形：范围内全强度，范围外为0
                        float distance = Vector2.Distance(new Vector2(dx, dy), new Vector2(brushPixelSize, brushPixelSize));
                        return distance <= brushPixelSize ? brushStrength : 0f;
                    }
                    
                case BrushShape.硬方形:
                    {
                        // 硬边方形：范围内全强度，范围外为0
                        float centerX = brushPixelSize;
                        float centerY = brushPixelSize;
                        float maxDistance = brushPixelSize;
                        
                        float distanceFromCenter = Mathf.Max(Mathf.Abs(dx - centerX), Mathf.Abs(dy - centerY));
                        return distanceFromCenter <= maxDistance ? brushStrength : 0f;
                    }
                    
                default:
                    return 0f;
            }
        }
        
        void DrawBrushSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("⚙️ 画笔设置", EditorStyles.boldLabel);
            
            brushSize = EditorGUILayout.Slider("画笔大小", brushSize, 1f, 50f);
            brushStrength = EditorGUILayout.Slider("绘制强度", brushStrength, 0.1f, 1.0f);
            
            EditorGUILayout.Space();
            
            // 画笔形状选择
            GUILayout.Label("画笔形状:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (DrawShapeButton("●", BrushShape.圆形, "圆形(柔边)"))
                brushShape = BrushShape.圆形;
            if (DrawShapeButton("■", BrushShape.方形, "方形(柔边)"))
                brushShape = BrushShape.方形;
            if (DrawShapeButton("⚫", BrushShape.硬圆形, "圆形(硬边)"))
                brushShape = BrushShape.硬圆形;
            if (DrawShapeButton("⬛", BrushShape.硬方形, "方形(硬边)"))
                brushShape = BrushShape.硬方形;
                
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Label($"当前形状: {brushShape}", EditorStyles.miniLabel);
            
            EditorGUILayout.Space();
            showBrushPreview = EditorGUILayout.Toggle("显示画笔预览", showBrushPreview);
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawTerrainSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🗻 目标地形", EditorStyles.boldLabel);
            
            // 允许手动更改地形（即使是自动设置的）
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("选择地形:", targetTerrain, typeof(Terrain), true);
            
            if (targetTerrain == null)
            {
                EditorGUILayout.HelpBox("请选择一个地形对象来绘制纹理", MessageType.Warning);
                
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
                EditorGUILayout.HelpBox($"✅ 目标地形: {targetTerrain.name}\n地形大小: {targetTerrain.terrainData.size}", MessageType.Info);
                
                // 显示地形基本信息
                GUILayout.Label($"高度图分辨率: {targetTerrain.terrainData.heightmapResolution}x{targetTerrain.terrainData.heightmapResolution}", EditorStyles.miniLabel);
                GUILayout.Label($"纹理分辨率: {targetTerrain.terrainData.alphamapWidth}x{targetTerrain.terrainData.alphamapHeight}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🎮 控制", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 激活绘制模式按钮
            if (targetTerrain != null)
            {
                if (!isPainting)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("🖌️ 激活绘制模式", GUILayout.Height(30)))
                    {
                        isPainting = true;
                        Tools.hidden = true;
                        SetupTerrainLayers();
                        Debug.Log("绘制模式已激活！在Scene视图中点击地形来绘制纹理");
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("⭕ 退出绘制模式", GUILayout.Height(30)))
                    {
                        isPainting = false;
                        Tools.hidden = false;
                        Debug.Log("绘制模式已退出");
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("请先选择地形", GUILayout.Height(30));
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("🔄 重置地形纹理"))
            {
                if (targetTerrain != null && EditorUtility.DisplayDialog("确认重置", "这将清除地形上的所有纹理，确定继续吗？", "确定", "取消"))
                {
                    ResetTerrainTextures();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawInstructions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("📖 使用说明", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "使用步骤:\n" +
                "1. 选择要绘制的地形\n" +
                "2. 选择画笔类型（预设纹理、自定义颜色或橡皮擦）\n" +
                "3. 如选择自定义颜色，可用颜色选择器或快捷按钮\n" +
                "4. 选择画笔形状（圆形/方形，柔边/硬边）\n" +
                "5. 调整画笔大小和强度\n" +
                "6. 点击'激活绘制模式'\n" +
                "7. 在Scene视图中点击地形来绘制!\n\n" +
                "画笔形状说明:\n" +
                "- 圆形(柔边): 中心强，边缘渐变\n" +
                "- 方形(柔边): 方形区域，边缘渐变\n" +
                "- 圆形(硬边): 圆形区域，边缘锐利\n" +
                "- 方形(硬边): 方形区域，边缘锐利\n\n" +
                "提示: \n" +
                "- 按住鼠标可以连续绘制\n" +
                "- 橡皮擦可以局部恢复为白色基础纹理", 
                MessageType.Info
            );
            
            EditorGUILayout.EndVertical();
        }
        
        void OnSceneGUI(SceneView sceneView)
        {
            if (!isPainting || targetTerrain == null) return;
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            Event current = Event.current;
            
            // 显示画笔预览
            if (showBrushPreview)
            {
                ShowBrushPreview();
            }
            
            // 处理鼠标绘制
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                PaintAtMousePosition();
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0)
            {
                PaintAtMousePosition();
                current.Use();
            }
        }
        
        void ShowBrushPreview()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetComponent<Terrain>() == targetTerrain)
                {
                    // 根据画笔形状绘制不同的预览
                    Handles.color = GetBrushColor(selectedBrush);
                    
                    switch (brushShape)
                    {
                        case BrushShape.圆形:
                        case BrushShape.硬圆形:
                            Handles.DrawWireDisc(hit.point, hit.normal, brushSize);
                            break;
                            
                        case BrushShape.方形:
                        case BrushShape.硬方形:
                            // 绘制方形预览
                            Vector3 right = Vector3.Cross(hit.normal, Vector3.up).normalized;
                            Vector3 forward = Vector3.Cross(right, hit.normal).normalized;
                            
                            Vector3[] square = new Vector3[4];
                            square[0] = hit.point + (right + forward) * brushSize;
                            square[1] = hit.point + (-right + forward) * brushSize;
                            square[2] = hit.point + (-right - forward) * brushSize;
                            square[3] = hit.point + (right - forward) * brushSize;
                            
                            Handles.DrawLine(square[0], square[1]);
                            Handles.DrawLine(square[1], square[2]);
                            Handles.DrawLine(square[2], square[3]);
                            Handles.DrawLine(square[3], square[0]);
                            break;
                    }
                    
                    // 显示画笔信息
                    Handles.BeginGUI();
                    GUILayout.BeginArea(new Rect(10, 10, 200, 120));
                    GUILayout.Label($"画笔: {selectedBrush}", EditorStyles.whiteLabel);
                    GUILayout.Label($"形状: {brushShape}", EditorStyles.whiteLabel);
                    GUILayout.Label($"大小: {brushSize:F1}", EditorStyles.whiteLabel);
                    GUILayout.Label($"强度: {brushStrength:F1}", EditorStyles.whiteLabel);
                    GUILayout.EndArea();
                    Handles.EndGUI();
                    
                    SceneView.RepaintAll();
                }
            }
        }
        
        void PaintAtMousePosition()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetComponent<Terrain>() == targetTerrain)
                {
                    PaintTextureAtPosition(hit.point);
                }
            }
        }
        
        void PaintTextureAtPosition(Vector3 worldPosition)
        {
            // 检查地形纹理层
            if (targetTerrain.terrainData.terrainLayers == null || targetTerrain.terrainData.terrainLayers.Length == 0)
            {
                Debug.LogError("地形没有纹理层！请先激活绘制模式");
                return;
            }
            
            // 检查是否需要重新创建绘制层（例如重置后只有1个白色层）
            int currentLayerCount = targetTerrain.terrainData.terrainLayers.Length;
            int expectedMinLayers = 9; // 至少需要9层（白色基础+8个预设）
            
            if (currentLayerCount < expectedMinLayers)
            {
                Debug.Log($"检测到纹理层不足（当前{currentLayerCount}层，需要至少{expectedMinLayers}层），重新创建基础绘制层");
                CreateBasePaintingLayers();
            }
            
            // 如果是自定义颜色，确保有对应的纹理层
            if (selectedBrush == TextureBrushType.自定义颜色)
            {
                int customColorIndex = customColorLayers.IndexOf(customColor);
                if (customColorIndex == -1)
                {
                    // 当前颜色没有对应的纹理层，立即创建
                    Debug.Log($"为新的自定义颜色 {ColorToHex(customColor)} 创建纹理层");
                    AddCustomColorLayer(customColor);
                    customColorIndex = customColorLayers.Count - 1;
                }
                
                // 检查纹理层索引是否有效
                int expectedLayerIndex = 9 + customColorIndex;
                if (expectedLayerIndex >= targetTerrain.terrainData.terrainLayers.Length)
                {
                    Debug.LogError($"自定义颜色纹理层索引超出范围！需要索引: {expectedLayerIndex}, 当前最大: {targetTerrain.terrainData.terrainLayers.Length - 1}");
                    return;
                }
            }
            
            // 将世界坐标转换为地形坐标
            Vector3 terrainPos = targetTerrain.transform.InverseTransformPoint(worldPosition);
            Vector3 terrainSize = targetTerrain.terrainData.size;
            
            // 计算在alphamap中的位置
            int alphamapWidth = targetTerrain.terrainData.alphamapWidth;
            int alphamapHeight = targetTerrain.terrainData.alphamapHeight;
            
            int x = Mathf.RoundToInt((terrainPos.x / terrainSize.x) * alphamapWidth);
            int z = Mathf.RoundToInt((terrainPos.z / terrainSize.z) * alphamapHeight);
            
            // 计算画笔影响范围
            int brushPixelSize = Mathf.RoundToInt(brushSize * alphamapWidth / terrainSize.x);
            
            // 获取当前alphamap（在检查纹理层后重新获取，确保层数匹配）
            try
            {
                float[,,] alphamap = targetTerrain.terrainData.GetAlphamaps(
                    Mathf.Max(0, x - brushPixelSize), 
                    Mathf.Max(0, z - brushPixelSize),
                    Mathf.Min(alphamapWidth, brushPixelSize * 2),
                    Mathf.Min(alphamapHeight, brushPixelSize * 2)
                );
                
                // 橡皮擦的特殊逻辑
                if (selectedBrush == TextureBrushType.橡皮擦)
                {
                    // 橡皮擦：将白色基础层设为1，其他层设为0
                    for (int dy = 0; dy < alphamap.GetLength(1); dy++)
                    {
                        for (int dx = 0; dx < alphamap.GetLength(0); dx++)
                        {
                            // 使用新的画笔强度计算
                            float strength = CalculateBrushStrength(dx, dy, brushPixelSize);
                            
                            if (strength > 0f)
                            {
                                // 增加白色基础层的权重
                                float oldWhiteValue = alphamap[dx, dy, 0];
                                alphamap[dx, dy, 0] = Mathf.Min(1f, oldWhiteValue + strength);
                                
                                // 按比例减少其他所有层的权重
                                float reduction = strength;
                                for (int layer = 1; layer < alphamap.GetLength(2); layer++)
                                {
                                    float currentValue = alphamap[dx, dy, layer];
                                    alphamap[dx, dy, layer] = Mathf.Max(0f, currentValue * (1f - reduction));
                                }
                                
                                // 归一化权重
                                NormalizeTextureWeights(alphamap, dx, dy);
                            }
                        }
                    }
                }
                else
                {
                    // 普通画笔的逻辑
                    // 获取目标纹理层索引
                    int targetLayerIndex = GetTextureLayerIndex(selectedBrush);
                    
                    // 检查索引是否有效
                    if (targetLayerIndex == -1 || targetLayerIndex >= alphamap.GetLength(2))
                    {
                        if (targetLayerIndex == -1)
                        {
                            Debug.LogError("无法获取有效的纹理层索引！");
                        }
                        else
                        {
                            Debug.LogError($"纹理层索引超出范围！索引: {targetLayerIndex}, 最大: {alphamap.GetLength(2) - 1}");
                        }
                        return;
                    }
                    
                    // 在画笔范围内绘制
                    for (int dy = 0; dy < alphamap.GetLength(1); dy++)
                    {
                        for (int dx = 0; dx < alphamap.GetLength(0); dx++)
                        {
                            // 使用新的画笔强度计算
                            float strength = CalculateBrushStrength(dx, dy, brushPixelSize);
                            
                            if (strength > 0f)
                            {
                                // 增加目标纹理的权重
                                float oldValue = alphamap[dx, dy, targetLayerIndex];
                                alphamap[dx, dy, targetLayerIndex] = Mathf.Min(1f, oldValue + strength);
                                
                                // 归一化其他纹理权重
                                NormalizeTextureWeights(alphamap, dx, dy);
                            }
                        }
                    }
                }
                
                // 应用修改后的alphamap
                targetTerrain.terrainData.SetAlphamaps(
                    Mathf.Max(0, x - brushPixelSize), 
                    Mathf.Max(0, z - brushPixelSize),
                    alphamap
                );
                
                // 刷新地形
                targetTerrain.Flush();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"绘制时发生错误: {e.Message}");
            }
        }
        
        void NormalizeTextureWeights(float[,,] alphamap, int x, int y)
        {
            float totalWeight = 0f;
            int layerCount = alphamap.GetLength(2);
            
            // 计算总权重
            for (int i = 0; i < layerCount; i++)
            {
                totalWeight += alphamap[x, y, i];
            }
            
            // 归一化
            if (totalWeight > 1f)
            {
                for (int i = 0; i < layerCount; i++)
                {
                    alphamap[x, y, i] /= totalWeight;
                }
            }
        }
        
        void SetupTerrainLayers()
        {
            if (targetTerrain == null || targetTerrain.terrainData == null) return;
            
            // 保存当前alphamap（如果存在）
            float[,,] currentAlphamap = null;
            bool hasExistingAlphamap = targetTerrain.terrainData.alphamapLayers > 0;
            
            if (hasExistingAlphamap)
            {
                try
                {
                    currentAlphamap = targetTerrain.terrainData.GetAlphamaps(0, 0, 
                        targetTerrain.terrainData.alphamapWidth, 
                        targetTerrain.terrainData.alphamapHeight);
                }
                catch
                {
                    hasExistingAlphamap = false;
                }
            }
            
            // 检查是否已经有足够的纹理层用于绘制（至少需要9层：1个白色基础 + 8个预设画笔）
            bool hasEnoughLayers = targetTerrain.terrainData.terrainLayers != null && 
                                 targetTerrain.terrainData.terrainLayers.Length >= 9;
            
            // 总是重新创建绘制层，确保有正确的纹理结构
            Debug.Log($"[SetupTerrainLayers] 当前纹理层数: {targetTerrain.terrainData.terrainLayers?.Length ?? 0}");
            CreatePaintingLayers();
            Debug.Log("地形纹理层已重新设置完成！准备绘制功能");
        }
        
        void CreatePaintingLayers()
        {
            // 创建基础层：白色基础层 + 8种预设画笔纹理层 + 已有的自定义颜色层
            List<TerrainLayer> layersList = new List<TerrainLayer>();
            
            // 第0层：白色基础层（保持原有白色外观）
            layersList.Add(CreateTerrainLayer(Color.white, "白色基础"));
            
            // 第1-8层：预设画笔纹理
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.草地), "草地"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.沙漠), "沙漠"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.雪地), "雪地"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.岩石), "岩石"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.泥土), "泥土"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.水面), "水面"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.石路), "石路"));
            layersList.Add(CreateTerrainLayer(GetBrushColor(TextureBrushType.苔藓), "苔藓"));
            
            // 添加所有已创建的自定义颜色层
            foreach (Color customCol in customColorLayers)
            {
                layersList.Add(CreateTerrainLayer(customCol, $"自定义颜色 {ColorToHex(customCol)}"));
            }
            
            targetTerrain.terrainData.terrainLayers = layersList.ToArray();
            
            // 设置默认alphamap（白色基础层100%，其他层0%）
            SetupDefaultAlphamap();
        }
        
        void CreateBasePaintingLayers()
        {
            // 只创建基础的9层：白色基础层 + 8种预设画笔纹理层
            TerrainLayer[] layers = new TerrainLayer[9];
            
            // 第0层：白色基础层（保持原有白色外观）
            layers[0] = CreateTerrainLayer(Color.white, "白色基础");
            
            // 第1-8层：预设画笔纹理
            layers[1] = CreateTerrainLayer(GetBrushColor(TextureBrushType.草地), "草地");
            layers[2] = CreateTerrainLayer(GetBrushColor(TextureBrushType.沙漠), "沙漠");
            layers[3] = CreateTerrainLayer(GetBrushColor(TextureBrushType.雪地), "雪地");
            layers[4] = CreateTerrainLayer(GetBrushColor(TextureBrushType.岩石), "岩石");
            layers[5] = CreateTerrainLayer(GetBrushColor(TextureBrushType.泥土), "泥土");
            layers[6] = CreateTerrainLayer(GetBrushColor(TextureBrushType.水面), "水面");
            layers[7] = CreateTerrainLayer(GetBrushColor(TextureBrushType.石路), "石路");
            layers[8] = CreateTerrainLayer(GetBrushColor(TextureBrushType.苔藓), "苔藓");
            
            targetTerrain.terrainData.terrainLayers = layers;
            
            // 设置默认alphamap（白色基础层100%，其他层0%）
            SetupDefaultAlphamap();
        }
        
        void SetupDefaultAlphamap()
        {
            int alphamapWidth = targetTerrain.terrainData.alphamapWidth;
            int alphamapHeight = targetTerrain.terrainData.alphamapHeight;
            int layerCount = targetTerrain.terrainData.terrainLayers.Length;
            
            float[,,] alphamap = new float[alphamapWidth, alphamapHeight, layerCount];
            
            // 将第0层（白色基础）设为1，其他层设为0
            // 这样地形保持白色外观
            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int y = 0; y < alphamapHeight; y++)
                {
                    alphamap[x, y, 0] = 1f; // 白色基础层
                    for (int i = 1; i < layerCount; i++)
                    {
                        alphamap[x, y, i] = 0f; // 画笔纹理初始为0
                    }
                }
            }
            
            targetTerrain.terrainData.SetAlphamaps(0, 0, alphamap);
        }
        
        TerrainLayer CreateTerrainLayer(Color color, string name)
        {
            TerrainLayer layer = new TerrainLayer();
            
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.name = name;
            
            layer.diffuseTexture = texture;
            layer.tileSize = new Vector2(15f, 15f);
            layer.tileOffset = Vector2.zero;
            
            return layer;
        }
        
        Color GetBrushColor(TextureBrushType brushType)
        {
            switch (brushType)
            {
                case TextureBrushType.草地: return new Color(0.4f, 0.8f, 0.3f);
                case TextureBrushType.沙漠: return new Color(0.8f, 0.7f, 0.4f);
                case TextureBrushType.雪地: return new Color(0.9f, 0.9f, 1.0f);
                case TextureBrushType.岩石: return new Color(0.5f, 0.5f, 0.5f);
                case TextureBrushType.泥土: return new Color(0.6f, 0.4f, 0.2f);
                case TextureBrushType.水面: return new Color(0.2f, 0.6f, 0.9f);
                case TextureBrushType.石路: return new Color(0.7f, 0.7f, 0.6f);
                case TextureBrushType.苔藓: return new Color(0.2f, 0.5f, 0.2f);
                case TextureBrushType.自定义颜色: return customColor;
                case TextureBrushType.橡皮擦: return Color.white;
                default: return Color.white;
            }
        }
        
        int GetTextureLayerIndex(TextureBrushType brushType)
        {
            // 画笔纹理从索引1开始（索引0是白色基础层）
            if (brushType == TextureBrushType.自定义颜色)
            {
                // 查找当前自定义颜色在自定义颜色层列表中的位置
                int customColorIndex = customColorLayers.IndexOf(customColor);
                if (customColorIndex == -1)
                {
                    // 如果当前颜色不存在，说明纹理层刚刚被添加，重新查找
                    customColorIndex = customColorLayers.IndexOf(customColor);
                    if (customColorIndex == -1)
                    {
                        Debug.LogError($"自定义颜色 {ColorToHex(customColor)} 没有对应的纹理层！");
                        return -1;
                    }
                }
                // 自定义颜色层从索引9开始（0基础 + 8预设）
                return 9 + customColorIndex;
            }
            return (int)brushType + 1;
        }
        
        /// <summary>
        /// 安全地添加新的自定义颜色层，同时保持现有的alphamap数据
        /// </summary>
        void AddCustomColorLayer(Color newColor)
        {
            if (targetTerrain == null || targetTerrain.terrainData == null) return;
            
            // 添加颜色到列表
            customColorLayers.Add(newColor);
            
            // 获取当前的纹理层和alphamap
            TerrainLayer[] currentLayers = targetTerrain.terrainData.terrainLayers;
            int currentLayerCount = currentLayers?.Length ?? 0;
            
            // 获取当前的alphamap数据
            float[,,] currentAlphamap = null;
            if (currentLayerCount > 0)
            {
                try
                {
                    currentAlphamap = targetTerrain.terrainData.GetAlphamaps(0, 0, 
                        targetTerrain.terrainData.alphamapWidth, 
                        targetTerrain.terrainData.alphamapHeight);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"获取当前alphamap失败: {e.Message}");
                    return;
                }
            }
            
            // 创建新的纹理层数组
            TerrainLayer[] newLayers = new TerrainLayer[currentLayerCount + 1];
            
            // 复制现有层
            if (currentLayers != null)
            {
                for (int i = 0; i < currentLayerCount; i++)
                {
                    newLayers[i] = currentLayers[i];
                }
            }
            
            // 添加新的自定义颜色层
            newLayers[currentLayerCount] = CreateTerrainLayer(newColor, $"自定义颜色 {ColorToHex(newColor)}");
            
            // 设置新的纹理层
            targetTerrain.terrainData.terrainLayers = newLayers;
            
            // 扩展alphamap以包含新层
            if (currentAlphamap != null)
            {
                int alphamapWidth = targetTerrain.terrainData.alphamapWidth;
                int alphamapHeight = targetTerrain.terrainData.alphamapHeight;
                
                float[,,] newAlphamap = new float[alphamapWidth, alphamapHeight, currentLayerCount + 1];
                
                // 复制现有的alphamap数据
                for (int x = 0; x < alphamapWidth; x++)
                {
                    for (int y = 0; y < alphamapHeight; y++)
                    {
                        for (int layer = 0; layer < currentLayerCount; layer++)
                        {
                            newAlphamap[x, y, layer] = currentAlphamap[x, y, layer];
                        }
                        // 新层初始为0
                        newAlphamap[x, y, currentLayerCount] = 0f;
                    }
                }
                
                // 设置新的alphamap
                targetTerrain.terrainData.SetAlphamaps(0, 0, newAlphamap);
            }
            
            Debug.Log($"已添加新的自定义颜色层: {ColorToHex(newColor)}，总层数: {newLayers.Length}");
        }
        
        void ResetTerrainTextures()
        {
            if (targetTerrain == null || targetTerrain.terrainData == null) return;
            
            // 清空自定义颜色层列表
            customColorLayers.Clear();
            
            // 创建单一的白色纹理层，避免棋盘格
            TerrainLayer[] whiteLayers = new TerrainLayer[1];
            whiteLayers[0] = CreateTerrainLayer(Color.white, "白色基础");
            
            targetTerrain.terrainData.terrainLayers = whiteLayers;
            
            // 设置alphamap让白色纹理覆盖整个地形
            int alphamapWidth = targetTerrain.terrainData.alphamapWidth;
            int alphamapHeight = targetTerrain.terrainData.alphamapHeight;
            
            float[,,] alphamap = new float[alphamapWidth, alphamapHeight, 1];
            
            // 全部设为白色纹理
            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int y = 0; y < alphamapHeight; y++)
                {
                    alphamap[x, y, 0] = 1f; // 100% 白色
                }
            }
            
            targetTerrain.terrainData.SetAlphamaps(0, 0, alphamap);
            targetTerrain.Flush();
            
            Debug.Log("地形纹理已重置为纯白色状态，自定义颜色层已清空");
        }
    }
}