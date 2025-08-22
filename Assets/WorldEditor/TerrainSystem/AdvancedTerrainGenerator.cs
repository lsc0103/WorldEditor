using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 高级地形生成器 - 超越Gaia Pro的地形生成能力
    /// 支持多层噪声、地质模拟、实时侵蚀、程序化河流等
    /// </summary>
    public class AdvancedTerrainGenerator : MonoBehaviour
    {
        [Header("地形引用")]
        [SerializeField] private Terrain targetTerrain;
        [SerializeField] private UnityEngine.TerrainData terrainData;
        
        [Header("生成设置")]
        [SerializeField] private bool enableRealTimeGeneration = true;
        [SerializeField] private bool enableProgressiveGeneration = true;
        [SerializeField] private int generationStepsPerFrame = 100;
        
        [Header("噪声生成器")]
        [SerializeField] private NoiseGenerator noiseGenerator;
        [SerializeField] private ErosionSimulator erosionSimulator;
        [SerializeField] private RiverGenerator riverGenerator;
        [SerializeField] private TextureBlender textureBlender;
        
        [Header("地形扩展设置")]
        [SerializeField] private bool enableTerrainExpansion = true;
        [SerializeField] private float terrainSpacing = 1000f; // 相邻地形间距
        [SerializeField] private bool autoSyncNeighborParameters = true; // 自动同步邻接地形参数
        [SerializeField] private Vector2Int gridPosition = Vector2Int.zero; // 当前地形的网格位置
        
        // 事件
        public System.Action<float> OnGenerationProgress;
        public System.Action OnGenerationComplete;
        
        // 私有变量
        private bool isGenerating = false;
        private Coroutine generationCoroutine;
        private float[,] heightMap;
        private Vector2Int mapResolution;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void InitializeComponents()
        {
            Debug.Log("[TerrainGenerator] 初始化组件...");
            
            if (targetTerrain == null)
                targetTerrain = GetComponent<Terrain>();
                
            if (targetTerrain != null && terrainData == null)
                terrainData = targetTerrain.terrainData;
                
            if (noiseGenerator == null)
            {
                noiseGenerator = GetComponent<NoiseGenerator>() ?? gameObject.AddComponent<NoiseGenerator>();
                Debug.Log("[TerrainGenerator] NoiseGenerator已初始化");
            }
                
            if (erosionSimulator == null)
            {
                erosionSimulator = GetComponent<ErosionSimulator>() ?? gameObject.AddComponent<ErosionSimulator>();
                Debug.Log("[TerrainGenerator] ErosionSimulator已初始化");
            }
                
            if (riverGenerator == null)
            {
                riverGenerator = GetComponent<RiverGenerator>() ?? gameObject.AddComponent<RiverGenerator>();
                Debug.Log("[TerrainGenerator] RiverGenerator已初始化");
            }
                
            if (textureBlender == null)
            {
                textureBlender = GetComponent<TextureBlender>() ?? gameObject.AddComponent<TextureBlender>();
                Debug.Log("[TerrainGenerator] TextureBlender已初始化");
            }
            
            Debug.Log("[TerrainGenerator] 组件初始化完成");
        }
        
        /// <summary>
        /// 生成地形 - 主要入口点（AccelEngine加速版）
        /// </summary>
        public void GenerateTerrain(WorldGenerationParameters parameters)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[TerrainGenerator] 地形生成正在进行中...");
                return;
            }
            
            Debug.Log("[TerrainGenerator] 开始生成地形（AccelEngine加速）...");
            Debug.Log($"[TerrainGenerator] GameObject激活状态: {gameObject.activeInHierarchy}");
            
            // 使用AccelEngine进行GPU加速地形生成
            StartCoroutine(GenerateTerrainWithAccelEngine(parameters));
            Debug.Log($"[TerrainGenerator] 组件启用状态: {enabled}");
            Debug.Log($"[TerrainGenerator] 实时生成: {enableRealTimeGeneration}, 渐进式: {enableProgressiveGeneration}");
            
            // 根据实时生成设置选择生成方式
            if (enableRealTimeGeneration && enableProgressiveGeneration)
            {
                Debug.Log("[TerrainGenerator] 启动渐进式生成协程...");
                generationCoroutine = StartCoroutine(GenerateTerrainProgressively(parameters));
                Debug.Log($"[TerrainGenerator] 协程已启动: {generationCoroutine != null}");
            }
            else if (enableRealTimeGeneration)
            {
                // 实时生成但不渐进式，直接同步生成
                Debug.Log("[TerrainGenerator] 使用立即生成模式");
                GenerateTerrainImmediate(parameters);
            }
            else
            {
                // 非实时生成，可能需要批处理或离线处理
                Debug.LogWarning("[TerrainGenerator] 实时生成已禁用，跳过地形生成");
                OnGenerationComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// 使用AccelEngine加速地形生成
        /// </summary>
        IEnumerator GenerateTerrainWithAccelEngine(WorldGenerationParameters parameters)
        {
            isGenerating = true;
            
            Debug.Log("[TerrainGenerator] 开始AccelEngine加速地形生成");
            
            // 提交噪声生成任务到AccelEngine
            bool noiseCompleted = false;
            bool noiseSuccess = false;
            
            object[] noiseTaskData = new object[] { parameters, noiseGenerator, terrainData };
            
            string noiseTaskId = AccelEngine.Instance.SubmitTask(
                AccelEngine.ComputeTaskType.NoiseGeneration,
                "地形噪声生成",
                (success) => {
                    noiseCompleted = true;
                    noiseSuccess = success;
                },
                noiseTaskData,
                priority: 0
            );
            
            Debug.Log($"[TerrainGenerator] 噪声生成任务已提交: {noiseTaskId}");
            
            // 等待噪声生成完成
            while (!noiseCompleted)
            {
                yield return null;
            }
            
            if (!noiseSuccess)
            {
                Debug.LogWarning("[TerrainGenerator] AccelEngine噪声生成失败，使用传统方法");
                // 回退到传统生成方法
                if (enableProgressiveGeneration)
                {
                    yield return StartCoroutine(GenerateTerrainProgressively(parameters));
                }
                else
                {
                    GenerateTerrainImmediate(parameters);
                }
            }
            else
            {
                Debug.Log("[TerrainGenerator] AccelEngine噪声生成成功");
                
                // 可以继续提交其他GPU加速任务（侵蚀、河流生成等）
                // 检查是否启用侵蚀（通过反射安全检查）
                bool enableErosion = false;
                var parametersType = parameters.GetType();
                
                // 先尝试字段
                var erosionField = parametersType.GetField("enableErosion");
                if (erosionField != null)
                {
                    object erosionValue = erosionField.GetValue(parameters);
                    enableErosion = erosionValue != null && (bool)erosionValue;
                }
                else
                {
                    // 再尝试属性
                    var erosionProperty = parametersType.GetProperty("enableErosion");
                    if (erosionProperty != null)
                    {
                        object erosionValue = erosionProperty.GetValue(parameters);
                        enableErosion = erosionValue != null && (bool)erosionValue;
                    }
                }
                
                if (enableErosion)
                {
                    bool erosionCompleted = false;
                    bool erosionSuccess = false;
                    
                    object[] erosionTaskData = new object[] { heightMap, erosionSimulator, parameters };
                    
                    string erosionTaskId = AccelEngine.Instance.SubmitTask(
                        AccelEngine.ComputeTaskType.ErosionSimulation,
                        "地形侵蚀模拟",
                        (success) => {
                            erosionCompleted = true;
                            erosionSuccess = success;
                        },
                        erosionTaskData,
                        priority: 1
                    );
                    
                    Debug.Log($"[TerrainGenerator] 侵蚀模拟任务已提交: {erosionTaskId}");
                    
                    while (!erosionCompleted)
                    {
                        yield return null;
                    }
                    
                    if (erosionSuccess)
                    {
                        Debug.Log("[TerrainGenerator] AccelEngine侵蚀模拟成功");
                    }
                    else
                    {
                        Debug.LogWarning("[TerrainGenerator] AccelEngine侵蚀模拟失败");
                    }
                }
                
                // 应用最终的高度图到地形
                if (terrainData != null && heightMap != null)
                {
                    terrainData.SetHeights(0, 0, heightMap);
                    Debug.Log("[TerrainGenerator] 地形高度图已应用");
                }
            }
            
            isGenerating = false;
            OnGenerationComplete?.Invoke();
            Debug.Log("[TerrainGenerator] AccelEngine地形生成完成");
        }
        
        /// <summary>
        /// 立即生成地形（同步）
        /// </summary>
        void GenerateTerrainImmediate(WorldGenerationParameters parameters)
        {
            isGenerating = true;
            
            try
            {
                Debug.Log("[TerrainGenerator] 开始立即生成模式");
                
                // 确保组件已初始化
                InitializeComponents();
                
                Debug.Log("[TerrainGenerator] 步骤1: 设置地形数据");
                SetupTerrainData(parameters);
                
                Debug.Log("[TerrainGenerator] 步骤2: 生成高度图");
                if (noiseGenerator == null)
                {
                    Debug.LogError("[TerrainGenerator] NoiseGenerator为空！");
                    return;
                }
                GenerateHeightMap(parameters.terrainParams);
                
                // 高度图设置后重新确保材质正确（高度图可能会影响纹理设置）
                Debug.Log("[TerrainGenerator] 高度图生成后重新设置地形材质");
                SetupTerrainMaterial(targetTerrain);
                
                Debug.Log("[TerrainGenerator] 步骤3: 检查侵蚀");
                if (parameters.terrainParams.enableGeologicalLayers)
                {
                    Debug.Log("[TerrainGenerator] 应用侵蚀");
                    ApplyErosion(parameters.terrainParams);
                }
                else
                {
                    Debug.Log("[TerrainGenerator] 跳过侵蚀");
                }
                
                Debug.Log("[TerrainGenerator] 步骤4: 检查河流");
                if (parameters.terrainParams.generateRivers)
                {
                    Debug.Log("[TerrainGenerator] 生成河流");
                    GenerateRivers(parameters.terrainParams);
                }
                else
                {
                    Debug.Log("[TerrainGenerator] 跳过河流");
                }
                
                Debug.Log("[TerrainGenerator] 步骤5: 应用纹理");
                ApplyTextures(parameters.terrainParams);
                
                // 最终确保地形材质正确
                Debug.Log("[TerrainGenerator] 最终确保地形材质正确");
                SetupTerrainMaterial(targetTerrain);
                
                OnGenerationComplete?.Invoke();
                Debug.Log("[TerrainGenerator] 地形生成完成!");
                
                // 显示地形信息帮助用户找到地形
                if (targetTerrain != null)
                {
                    Debug.Log($"[TerrainGenerator] ========== 地形生成完成信息 ==========");
                    Debug.Log($"[TerrainGenerator] 地形生成器: {gameObject.name}");
                    Debug.Log($"[TerrainGenerator] 生成的地形: {targetTerrain.gameObject.name}");
                    Debug.Log($"[TerrainGenerator] 层级结构: {gameObject.name} -> {targetTerrain.gameObject.name}");
                    Debug.Log($"[TerrainGenerator] 地形位置: {targetTerrain.transform.position}");
                    Debug.Log($"[TerrainGenerator] 地形尺寸: {terrainData.size}");
                    Debug.Log($"[TerrainGenerator] 地形激活状态: {targetTerrain.gameObject.activeInHierarchy}");
                    Debug.Log($"[TerrainGenerator] 在Hierarchy中查找: {gameObject.name} 下的 {targetTerrain.gameObject.name}");
                    Debug.Log($"[TerrainGenerator] =========================================");
                    
                    // 确保地形可见
                    targetTerrain.gameObject.SetActive(true);
                    
                    // 检查Terrain组件状态
                    Debug.Log($"[TerrainGenerator] Terrain组件启用状态: {targetTerrain.enabled}");
                    
                    // 检查是否有Renderer组件
                    var renderer = targetTerrain.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Debug.Log($"[TerrainGenerator] Renderer状态: enabled={renderer.enabled}");
                    }
                    
                    // 移动相机到地形中心上方便于观察
                    var camera = UnityEngine.Camera.main;
                    if (camera != null)
                    {
                        Vector3 terrainCenter = targetTerrain.transform.position + new Vector3(terrainData.size.x * 0.5f, terrainData.size.y + 50f, terrainData.size.z * 0.5f);
                        camera.transform.position = terrainCenter;
                        camera.transform.LookAt(targetTerrain.transform.position + new Vector3(terrainData.size.x * 0.5f, 0f, terrainData.size.z * 0.5f));
                        Debug.Log($"[TerrainGenerator] 已移动相机到: {camera.transform.position}");
                    }
                }
                else
                {
                    Debug.LogError("[TerrainGenerator] 目标地形为空！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainGenerator] 地形生成错误: {e.Message}");
                Debug.LogError($"[TerrainGenerator] 堆栈跟踪: {e.StackTrace}");
            }
            finally
            {
                isGenerating = false;
            }
        }
        
        /// <summary>
        /// 渐进式生成地形（异步）
        /// </summary>
        IEnumerator GenerateTerrainProgressively(WorldGenerationParameters parameters)
        {
            isGenerating = true;
            float totalSteps = 5f; // 总步骤数
            float currentStep = 0f;
            
            Debug.Log("[TerrainGenerator] 开始渐进式生成流程...");
            
            // 确保组件已初始化
            InitializeComponents();
            
            // 步骤1: 设置地形数据
            Debug.Log("[TerrainGenerator] 步骤1: 设置地形数据");
            OnGenerationProgress?.Invoke(currentStep / totalSteps);
            if (!TrySetupTerrainData(parameters))
            {
                Debug.LogError("[TerrainGenerator] 地形数据设置失败！");
                isGenerating = false;
                generationCoroutine = null;
                yield break;
            }
            currentStep++;
            Debug.Log("[TerrainGenerator] 步骤1完成，准备进入步骤2...");
            yield return null; // 使用null而不是WaitForEndOfFrame
            
            // 步骤2: 生成高度图
            Debug.Log("[TerrainGenerator] 步骤2: 开始生成高度图");
            OnGenerationProgress?.Invoke(currentStep / totalSteps);
            yield return StartCoroutine(GenerateHeightMapProgressive(parameters.terrainParams));
            currentStep++;
            Debug.Log("[TerrainGenerator] 步骤2: 高度图生成完成");
            
            // 步骤3: 应用侵蚀
            Debug.Log("[TerrainGenerator] 步骤3: 检查侵蚀设置");
            if (parameters.terrainParams.enableGeologicalLayers)
            {
                Debug.Log("[TerrainGenerator] 步骤3: 开始应用侵蚀");
                OnGenerationProgress?.Invoke(currentStep / totalSteps);
                yield return StartCoroutine(ApplyErosionProgressive(parameters.terrainParams));
                Debug.Log("[TerrainGenerator] 步骤3: 侵蚀应用完成");
            }
            else
            {
                Debug.Log("[TerrainGenerator] 步骤3: 跳过侵蚀（高原预设）");
            }
            currentStep++;
            
            // 步骤4: 生成河流
            Debug.Log("[TerrainGenerator] 步骤4: 检查河流设置");
            if (parameters.terrainParams.generateRivers)
            {
                Debug.Log("[TerrainGenerator] 步骤4: 开始生成河流");
                OnGenerationProgress?.Invoke(currentStep / totalSteps);
                yield return StartCoroutine(GenerateRiversProgressive(parameters.terrainParams));
                Debug.Log("[TerrainGenerator] 步骤4: 河流生成完成");
            }
            else
            {
                Debug.Log("[TerrainGenerator] 步骤4: 跳过河流生成（高原预设）");
            }
            currentStep++;
            
            // 步骤5: 应用纹理
            Debug.Log("[TerrainGenerator] 步骤5: 开始应用纹理");
            OnGenerationProgress?.Invoke(currentStep / totalSteps);
            yield return StartCoroutine(ApplyTexturesProgressive(parameters.terrainParams));
            currentStep++;
            Debug.Log("[TerrainGenerator] 步骤5: 纹理应用完成");
            
            OnGenerationProgress?.Invoke(1f);
            OnGenerationComplete?.Invoke();
            Debug.Log("[TerrainGenerator] 渐进式地形生成完成!");
            
            isGenerating = false;
            generationCoroutine = null;
        }
        
        bool TrySetupTerrainData(WorldGenerationParameters parameters)
        {
            try
            {
                SetupTerrainData(parameters);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainGenerator] 地形数据设置错误: {e.Message}");
                return false;
            }
        }
        
        void SetupTerrainData(WorldGenerationParameters parameters)
        {
            if (terrainData == null)
            {
                terrainData = new UnityEngine.TerrainData();
                terrainData.name = "Generated Terrain Data";
            }
            
            var terrainParams = parameters.terrainParams;
            
            // 计算合理的地形分辨率 (默认513x513，这是Unity地形的标准分辨率)
            int resolution = 513; // 常用的Unity地形分辨率
            mapResolution = new Vector2Int(resolution - 1, resolution - 1); // 高度图数组是512x512
            
            // 设置地形数据尺寸
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(parameters.areaSize.x, terrainParams.heightVariation, parameters.areaSize.y);
            
            // 初始化高度图（清空所有高度数据）
            heightMap = new float[mapResolution.x, mapResolution.y];
            
            // 立即清空现有地形的高度数据，确保移除河流等之前的痕迹
            Debug.Log("[TerrainGenerator] 清空现有地形高度数据，移除河流痕迹");
            float[,] emptyHeights = new float[resolution, resolution];
            // emptyHeights 数组默认全为0，代表平坦地形
            terrainData.SetHeights(0, 0, emptyHeights);
            
            // 创建或查找地形子对象
            GameObject terrainGameObject = null;
            Transform terrainChild = transform.Find("Generated Terrain");
            
            if (terrainChild != null)
            {
                terrainGameObject = terrainChild.gameObject;
                Debug.Log("[TerrainGenerator] 找到现有的地形子对象");
            }
            else
            {
                // 创建新的子GameObject用于地形
                terrainGameObject = new GameObject("Generated Terrain");
                terrainGameObject.transform.SetParent(transform);
                terrainGameObject.transform.localPosition = Vector3.zero;
                Debug.Log("[TerrainGenerator] 创建新的地形子对象");
            }
            
            // 确保有Terrain组件
            if (targetTerrain == null)
            {
                targetTerrain = terrainGameObject.GetComponent<Terrain>();
                if (targetTerrain == null)
                {
                    Debug.Log("[TerrainGenerator] 在子对象上创建Terrain组件");
                    targetTerrain = terrainGameObject.AddComponent<Terrain>();
                }
            }
            
            // 确保有TerrainCollider
            var collider = terrainGameObject.GetComponent<TerrainCollider>();
            if (collider == null)
            {
                Debug.Log("[TerrainGenerator] 在子对象上创建TerrainCollider组件");
                collider = terrainGameObject.AddComponent<TerrainCollider>();
            }
            
            // 设置地形数据
            targetTerrain.terrainData = terrainData;
            collider.terrainData = terrainData;
            
            // 确保地形有正确的材质，防止紫色显示
            SetupTerrainMaterial(targetTerrain);
            
            // 确保地形子对象激活
            terrainGameObject.SetActive(true);
            
            Debug.Log($"[TerrainGenerator] 地形数据已设置 - 分辨率: {resolution}x{resolution}, 尺寸: {terrainData.size}");
            Debug.Log($"[TerrainGenerator] 地形生成器: {gameObject.name}, 生成的地形: {terrainGameObject.name}");
            Debug.Log($"[TerrainGenerator] 层级结构: {gameObject.name} -> {terrainGameObject.name}");
        }
        
        /// <summary>
        /// 设置地形材质，确保地形正确显示
        /// </summary>
        void SetupTerrainMaterial(Terrain terrain)
        {
            Debug.Log("[TerrainMaterial] 根据Grok-4分析，修复URP渲染管线的地形紫色问题");
            
            try
            {
                // 检查当前项目的渲染管线
                var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
                bool isBuiltInRP = pipeline == null;
                Debug.Log($"[TerrainMaterial] 渲染管线检测: {(isBuiltInRP ? "Built-in RP" : "URP/HDRP等Scriptable RP")}");
                
                if (isBuiltInRP)
                {
                    // Built-in渲染管线的处理
                    Debug.Log("[TerrainMaterial] 为Built-in RP设置地形材质");
                    Material builtinTerrainMat = Resources.GetBuiltinResource<Material>("Default-Terrain-Standard.mat");
                    if (builtinTerrainMat != null)
                    {
                        terrain.materialTemplate = builtinTerrainMat;
                        Debug.Log($"[TerrainMaterial] 设置Built-in地形材质: {builtinTerrainMat.name}");
                    }
                    else
                    {
                        terrain.materialTemplate = null;
                        Debug.Log("[TerrainMaterial] 使用Built-in默认地形材质");
                    }
                }
                else
                {
                    // URP/HDRP等SRP的处理
                    Debug.Log("[TerrainMaterial] 检测到URP/HDRP，为SRP设置地形材质");
                    
                    // 方法1：尝试查找URP地形材质
                    Material urpTerrainMat = null;
                    
                    // 尝试各种可能的URP地形材质名称
                    string[] urpMaterialNames = {
                        "Universal Render Pipeline/Terrain/Lit",
                        "URP-Terrain-Lit",
                        "Universal/Terrain/Lit"
                    };
                    
                    foreach (string matName in urpMaterialNames)
                    {
                        var shader = Shader.Find(matName);
                        if (shader != null)
                        {
                            urpTerrainMat = new Material(shader);
                            Debug.Log($"[TerrainMaterial] 找到URP地形着色器: {matName}");
                            break;
                        }
                    }
                    
                    if (urpTerrainMat != null)
                    {
                        terrain.materialTemplate = urpTerrainMat;
                        Debug.Log("[TerrainMaterial] 设置了URP地形材质");
                    }
                    else
                    {
                        // 如果找不到URP材质，尝试使用通用材质
                        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                        if (unlitShader != null)
                        {
                            terrain.materialTemplate = new Material(unlitShader);
                            Debug.Log("[TerrainMaterial] 使用URP Unlit着色器作为备选");
                        }
                        else
                        {
                            terrain.materialTemplate = null;
                            Debug.Log("[TerrainMaterial] 未找到URP着色器，使用默认");
                        }
                    }
                }
                
                // 设置基础纹理层
                TerrainLayer defaultLayer = new TerrainLayer();
                defaultLayer.diffuseTexture = Texture2D.whiteTexture;
                defaultLayer.tileSize = Vector2.one * 15f;
                defaultLayer.tileOffset = Vector2.zero;
                
                terrain.terrainData.terrainLayers = new TerrainLayer[] { defaultLayer };
                
                // 设置透明度贴图确保纹理层可见
                int alphamapWidth = terrain.terrainData.alphamapWidth;
                int alphamapHeight = terrain.terrainData.alphamapHeight;
                
                if (alphamapWidth > 0 && alphamapHeight > 0)
                {
                    float[,,] alphaMaps = new float[alphamapWidth, alphamapHeight, 1];
                    for (int x = 0; x < alphamapWidth; x++)
                    {
                        for (int y = 0; y < alphamapHeight; y++)
                        {
                            alphaMaps[x, y, 0] = 1f; // 第一层完全可见
                        }
                    }
                    terrain.terrainData.SetAlphamaps(0, 0, alphaMaps);
                    Debug.Log("[TerrainMaterial] 设置了透明度贴图");
                }
                
                // 强制刷新
                terrain.Flush();
                
                Debug.Log("[TerrainMaterial] 地形材质设置完成");
                
                // 验证设置
                Debug.Log($"[TerrainMaterial] 验证 - 渲染管线: {(isBuiltInRP ? "Built-in" : "SRP(URP/HDRP)")}");
                Debug.Log($"[TerrainMaterial] 验证 - 材质模板: {(terrain.materialTemplate != null ? terrain.materialTemplate.shader.name : "null (默认)")}");
                Debug.Log($"[TerrainMaterial] 验证 - 纹理层数量: {terrain.terrainData.terrainLayers?.Length ?? 0}");
                Debug.Log($"[TerrainMaterial] 验证 - 透明度图层数: {terrain.terrainData.alphamapLayers}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainMaterial] 地形材质设置失败: {e.Message}");
                Debug.LogError($"[TerrainMaterial] 堆栈跟踪: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 强制刷新地形以确保材质和纹理更改生效
        /// </summary>
        void ForceTerrainRefresh(Terrain terrain)
        {
            Debug.Log("[TerrainMaterial] 强制刷新地形");
            
            try
            {
                // 方法1：重新分配TerrainData来强制刷新
                var terrainData = terrain.terrainData;
                terrain.terrainData = null;
                terrain.terrainData = terrainData;
                
                // 方法2：重新启用地形组件
                terrain.enabled = false;
                terrain.enabled = true;
                
                // 方法3：重新分配材质模板
                var material = terrain.materialTemplate;
                terrain.materialTemplate = null;
                terrain.materialTemplate = material;
                
                // 方法4：标记地形需要重绘
                terrain.Flush();
                
                Debug.Log("[TerrainMaterial] 地形刷新完成");
                
                // 最终验证
                Debug.Log($"[TerrainMaterial] 刷新后验证 - 纹理层: {terrain.terrainData.terrainLayers?.Length ?? 0}");
                Debug.Log($"[TerrainMaterial] 刷新后验证 - 材质: {(terrain.materialTemplate != null ? terrain.materialTemplate.name : "null")}");
                Debug.Log($"[TerrainMaterial] 刷新后验证 - 地形启用: {terrain.enabled}");
                Debug.Log($"[TerrainMaterial] 刷新后验证 - GameObject激活: {terrain.gameObject.activeInHierarchy}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainMaterial] 强制刷新失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 尝试使用Unity默认的地形设置方式
        /// </summary>
        void TryUnityDefaultSetup(Terrain terrain)
        {
            Debug.Log("[TerrainMaterial] 尝试Unity默认设置方式");
            
            try
            {
                // 清空所有自定义设置，让Unity使用默认方式
                terrain.materialTemplate = null;
                terrain.terrainData.terrainLayers = null;
                
                // 创建最基础的地形层 - 使用Unity的标准方式
                TerrainLayer defaultLayer = new TerrainLayer();
                
                // 使用Unity内置的默认纹理
                Texture2D builtinTexture = Resources.GetBuiltinResource<Texture2D>("Default-Checker-Gray.exr");
                if (builtinTexture == null)
                {
                    // 如果找不到内置纹理，使用白色纹理
                    builtinTexture = Texture2D.whiteTexture;
                }
                
                defaultLayer.diffuseTexture = builtinTexture;
                defaultLayer.tileSize = Vector2.one * 50f;
                defaultLayer.tileOffset = Vector2.zero;
                
                terrain.terrainData.terrainLayers = new TerrainLayer[] { defaultLayer };
                
                Debug.Log($"[TerrainMaterial] Unity默认设置完成，使用纹理: {builtinTexture.name}");
                
                // 最终验证
                Debug.Log($"[TerrainMaterial] Unity默认方式 - 纹理层: {terrain.terrainData.terrainLayers?.Length ?? 0}");
                Debug.Log($"[TerrainMaterial] Unity默认方式 - 材质模板: {(terrain.materialTemplate != null ? terrain.materialTemplate.name : "null（使用默认）")}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainMaterial] Unity默认设置失败: {e.Message}");
            }
        }
        
        
        void GenerateHeightMap(TerrainGenerationParams parameters)
        {
            Debug.Log($"[TerrainGenerator] 检查高度图生成参数");
            Debug.Log($"[TerrainGenerator] NoiseGenerator状态: {noiseGenerator != null}");
            Debug.Log($"[TerrainGenerator] HeightMap状态: {heightMap != null}");
            Debug.Log($"[TerrainGenerator] TerrainData状态: {terrainData != null}");
            Debug.Log($"[TerrainGenerator] 地图分辨率: {mapResolution.x}x{mapResolution.y}");
            Debug.Log($"[TerrainGenerator] 参数状态: {parameters != null}");
            
            if (parameters?.noiseLayers != null)
            {
                Debug.Log($"[TerrainGenerator] 噪声层数量: {parameters.noiseLayers.Length}");
            }
            else
            {
                Debug.LogError("[TerrainGenerator] 噪声层参数为空！");
                return;
            }
            
            noiseGenerator.GenerateHeightMap(ref heightMap, mapResolution, parameters);
            terrainData.SetHeights(0, 0, heightMap);
            Debug.Log("[TerrainGenerator] 高度图应用到地形完成");
        }
        
        IEnumerator GenerateHeightMapProgressive(TerrainGenerationParams parameters)
        {
            Debug.Log("[TerrainGenerator] 开始生成高度图...");
            
            if (noiseGenerator == null)
            {
                Debug.LogError("[TerrainGenerator] NoiseGenerator未初始化！");
                yield break;
            }
            
            if (heightMap == null)
            {
                Debug.LogError("[TerrainGenerator] HeightMap未初始化！");
                yield break;
            }
            
            Debug.Log($"[TerrainGenerator] 高度图分辨率: {mapResolution.x}x{mapResolution.y}");
            yield return StartCoroutine(noiseGenerator.GenerateHeightMapProgressive(heightMap, mapResolution, parameters, generationStepsPerFrame));
            
            Debug.Log("[TerrainGenerator] 应用高度图到地形...");
            terrainData.SetHeights(0, 0, heightMap);
            Debug.Log("[TerrainGenerator] 高度图生成完成");
        }
        
        void ApplyErosion(TerrainGenerationParams parameters)
        {
            if (parameters.enableGeologicalLayers)
            {
                erosionSimulator.ApplyErosion(ref heightMap, mapResolution, parameters.geology);
                terrainData.SetHeights(0, 0, heightMap);
            }
        }
        
        IEnumerator ApplyErosionProgressive(TerrainGenerationParams parameters)
        {
            if (parameters.enableGeologicalLayers)
            {
                Debug.Log("[TerrainGenerator] 开始应用侵蚀...");
                if (erosionSimulator == null)
                {
                    Debug.LogError("[TerrainGenerator] ErosionSimulator未初始化！");
                    yield break;
                }
                yield return StartCoroutine(erosionSimulator.ApplyErosionProgressive(heightMap, mapResolution, parameters.geology, generationStepsPerFrame));
                terrainData.SetHeights(0, 0, heightMap);
                Debug.Log("[TerrainGenerator] 侵蚀应用完成");
            }
            else
            {
                Debug.Log("[TerrainGenerator] 跳过侵蚀步骤");
            }
        }
        
        void GenerateRivers(TerrainGenerationParams parameters)
        {
            if (parameters.generateRivers)
            {
                riverGenerator.GenerateRivers(ref heightMap, mapResolution, parameters);
                terrainData.SetHeights(0, 0, heightMap);
            }
        }
        
        IEnumerator GenerateRiversProgressive(TerrainGenerationParams parameters)
        {
            if (parameters.generateRivers)
            {
                Debug.Log("[TerrainGenerator] 开始生成河流...");
                if (riverGenerator == null)
                {
                    Debug.LogError("[TerrainGenerator] RiverGenerator未初始化！");
                    yield break;
                }
                yield return StartCoroutine(riverGenerator.GenerateRiversProgressive(heightMap, mapResolution, parameters, generationStepsPerFrame));
                terrainData.SetHeights(0, 0, heightMap);
                Debug.Log("[TerrainGenerator] 河流生成完成");
            }
            else
            {
                Debug.Log("[TerrainGenerator] 跳过河流生成");
            }
        }
        
        void ApplyTextures(TerrainGenerationParams parameters)
        {
            textureBlender.ApplyTextures(terrainData, heightMap, mapResolution, parameters);
        }
        
        IEnumerator ApplyTexturesProgressive(TerrainGenerationParams parameters)
        {
            Debug.Log("[TerrainGenerator] 开始应用纹理...");
            if (textureBlender == null)
            {
                Debug.LogError("[TerrainGenerator] TextureBlender未初始化！");
                yield break;
            }
            yield return StartCoroutine(textureBlender.ApplyTexturesProgressive(terrainData, heightMap, mapResolution, parameters, generationStepsPerFrame));
            Debug.Log("[TerrainGenerator] 纹理应用完成");
        }
        
        /// <summary>
        /// 停止地形生成
        /// </summary>
        public void StopGeneration()
        {
            if (generationCoroutine != null)
            {
                StopCoroutine(generationCoroutine);
                generationCoroutine = null;
            }
            isGenerating = false;
            Debug.Log("[TerrainGenerator] 地形生成已停止");
        }
        
        /// <summary>
        /// 获取当前生成状态
        /// </summary>
        public bool IsGenerating()
        {
            return isGenerating;
        }
        
        /// <summary>
        /// 获取地形高度在指定世界坐标
        /// </summary>
        public float GetHeightAtWorldPosition(Vector3 worldPosition)
        {
            if (targetTerrain == null) return 0f;
            
            Vector3 terrainPosition = worldPosition - targetTerrain.transform.position;
            Vector3 terrainSize = targetTerrain.terrainData.size;
            
            float normalizedX = terrainPosition.x / terrainSize.x;
            float normalizedZ = terrainPosition.z / terrainSize.z;
            
            return targetTerrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
        }
        
        /// <summary>
        /// 获取地形法线在指定世界坐标
        /// </summary>
        public Vector3 GetNormalAtWorldPosition(Vector3 worldPosition)
        {
            if (targetTerrain == null) return Vector3.up;
            
            Vector3 terrainPosition = worldPosition - targetTerrain.transform.position;
            Vector3 terrainSize = targetTerrain.terrainData.size;
            
            float normalizedX = terrainPosition.x / terrainSize.x;
            float normalizedZ = terrainPosition.z / terrainSize.z;
            
            return targetTerrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        }
        
        // ========== 地形扩展功能 ==========
        
        /// <summary>
        /// 向指定方向扩展新的地形生成器
        /// </summary>
        public AdvancedTerrainGenerator ExpandTerrain(TerrainDirection direction)
        {
            if (!enableTerrainExpansion)
            {
                Debug.LogWarning("[TerrainGenerator] 地形扩展功能未启用");
                return null;
            }
            
            // 计算新地形的网格位置
            Vector2Int newGridPos = gridPosition + GetDirectionVector(direction);
            
            // 检查该位置是否已存在地形生成器
            AdvancedTerrainGenerator existing = FindTerrainGeneratorAtGridPosition(newGridPos);
            if (existing != null)
            {
                Debug.Log($"[TerrainGenerator] 网格位置 {newGridPos} 已存在地形生成器: {existing.name}");
                return existing;
            }
            
            // 创建新的地形生成器
            AdvancedTerrainGenerator newGenerator = CreateAdjacentTerrainGenerator(newGridPos, direction);
            
            if (newGenerator != null)
            {
                Debug.Log($"[TerrainGenerator] 成功向{direction}方向扩展地形，网格位置: {newGridPos}");
                
                // 同步参数到新地形
                if (autoSyncNeighborParameters)
                {
                    SyncParametersToTerrain(newGenerator);
                }
            }
            
            return newGenerator;
        }
        
        /// <summary>
        /// 创建相邻的地形生成器
        /// </summary>
        AdvancedTerrainGenerator CreateAdjacentTerrainGenerator(Vector2Int gridPos, TerrainDirection direction)
        {
            // 计算新地形的世界坐标
            Vector3 worldPosition = GridToWorldPosition(gridPos);
            
            // 创建新的GameObject
            GameObject newTerrainObj = new GameObject($"TerrainGenerator_{gridPos.x}_{gridPos.y}");
            newTerrainObj.transform.position = worldPosition;
            
            // 将新地形放在与当前地形相同的父级下
            if (transform.parent != null)
            {
                newTerrainObj.transform.SetParent(transform.parent);
            }
            
            // 添加AdvancedTerrainGenerator组件
            AdvancedTerrainGenerator newGenerator = newTerrainObj.AddComponent<AdvancedTerrainGenerator>();
            
            // 设置网格位置
            newGenerator.gridPosition = gridPos;
            newGenerator.terrainSpacing = this.terrainSpacing;
            newGenerator.enableTerrainExpansion = true;
            newGenerator.autoSyncNeighborParameters = this.autoSyncNeighborParameters;
            
            Debug.Log($"[TerrainGenerator] 在位置 {worldPosition} 创建新地形生成器: {newTerrainObj.name}");
            
            return newGenerator;
        }
        
        /// <summary>
        /// 同步参数到目标地形生成器
        /// </summary>
        void SyncParametersToTerrain(AdvancedTerrainGenerator targetGenerator)
        {
            if (targetGenerator == null) return;
            
            Debug.Log($"[TerrainGenerator] 开始同步参数到 {targetGenerator.name}");
            
            try
            {
                // 同步基本设置
                targetGenerator.enableRealTimeGeneration = this.enableRealTimeGeneration;
                targetGenerator.enableProgressiveGeneration = this.enableProgressiveGeneration;
                targetGenerator.generationStepsPerFrame = this.generationStepsPerFrame;
                
                // 同步扩展设置
                targetGenerator.terrainSpacing = this.terrainSpacing;
                targetGenerator.autoSyncNeighborParameters = this.autoSyncNeighborParameters;
                
                // 如果有WorldEditorManager组件，也复制相关设置
                var sourceWorldManager = GetComponent<WorldEditor.Core.WorldEditorManager>();
                if (sourceWorldManager != null)
                {
                    var targetWorldManager = targetGenerator.GetComponent<WorldEditor.Core.WorldEditorManager>();
                    if (targetWorldManager == null)
                    {
                        targetWorldManager = targetGenerator.gameObject.AddComponent<WorldEditor.Core.WorldEditorManager>();
                    }
                    
                    // 这里可以添加更多的WorldManager参数同步
                    Debug.Log($"[TerrainGenerator] WorldEditorManager组件已同步到 {targetGenerator.name}");
                }
                
                Debug.Log($"[TerrainGenerator] 参数同步完成: {targetGenerator.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainGenerator] 参数同步失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 查找指定网格位置的地形生成器
        /// </summary>
        AdvancedTerrainGenerator FindTerrainGeneratorAtGridPosition(Vector2Int gridPos)
        {
            // 在场景中查找所有的地形生成器
            AdvancedTerrainGenerator[] allGenerators = FindObjectsByType<AdvancedTerrainGenerator>(FindObjectsSortMode.None);
            
            foreach (var generator in allGenerators)
            {
                if (generator.gridPosition == gridPos)
                {
                    return generator;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            Vector3 basePosition = transform.position;
            
            // 移除当前网格位置的偏移，计算出网格原点
            Vector3 gridOrigin = basePosition - new Vector3(this.gridPosition.x * terrainSpacing, 0, this.gridPosition.y * terrainSpacing);
            
            // 计算目标网格位置的世界坐标
            return gridOrigin + new Vector3(gridPos.x * terrainSpacing, 0, gridPos.y * terrainSpacing);
        }
        
        /// <summary>
        /// 获取方向向量
        /// </summary>
        Vector2Int GetDirectionVector(TerrainDirection direction)
        {
            switch (direction)
            {
                case TerrainDirection.North: return Vector2Int.up;    // +Z
                case TerrainDirection.South: return Vector2Int.down;  // -Z  
                case TerrainDirection.East: return Vector2Int.right;  // +X
                case TerrainDirection.West: return Vector2Int.left;   // -X
                default: return Vector2Int.zero;
            }
        }
        
        /// <summary>
        /// 获取所有相邻地形生成器
        /// </summary>
        public List<AdvancedTerrainGenerator> GetNeighborGenerators()
        {
            List<AdvancedTerrainGenerator> neighbors = new List<AdvancedTerrainGenerator>();
            
            // 检查四个方向
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in directions)
            {
                Vector2Int neighborPos = gridPosition + dir;
                AdvancedTerrainGenerator neighbor = FindTerrainGeneratorAtGridPosition(neighborPos);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// 获取当前地形的网格位置
        /// </summary>
        public Vector2Int GetGridPosition()
        {
            return gridPosition;
        }
        
        /// <summary>
        /// 设置当前地形的网格位置
        /// </summary>
        public void SetGridPosition(Vector2Int newGridPosition)
        {
            gridPosition = newGridPosition;
            
            // 更新GameObject名称以反映网格位置
            if (gameObject.name.Contains("TerrainGenerator"))
            {
                gameObject.name = $"TerrainGenerator_{gridPosition.x}_{gridPosition.y}";
            }
        }
        
        /// <summary>
        /// 检查地形扩展功能是否启用
        /// </summary>
        public bool IsTerrainExpansionEnabled()
        {
            return enableTerrainExpansion;
        }
        
        /// <summary>
        /// 获取目标地形组件
        /// </summary>
        public Terrain GetTerrain()
        {
            return targetTerrain;
        }
    }
}