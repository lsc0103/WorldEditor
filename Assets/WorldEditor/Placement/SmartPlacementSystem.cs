using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 智能放置系统 - 超越GeNa Pro的智能资源分布和放置
    /// 支持AI驱动的生态系统模拟、智能分布规则、动态密度调整等
    /// </summary>
    public class SmartPlacementSystem : MonoBehaviour
    {
        [Header("放置系统核心")]
        [SerializeField] private bool enableSmartPlacement = true;
        [SerializeField] private bool enableRealTimePlacement = true;
        [SerializeField] private int placementStepsPerFrame = 50;
        
        [Header("分析器组件")]
        [SerializeField] private TerrainAnalyzer terrainAnalyzer;
        [SerializeField] private BiomeAnalyzer biomeAnalyzer;
        [SerializeField] private EcosystemSimulator ecosystemSimulator;
        [SerializeField] private PlacementRuleEngine ruleEngine;
        [SerializeField] private DensityManager densityManager;
        
        [Header("植被系统")]
        [SerializeField] private VegetationSystem vegetationSystem = new VegetationSystem();
        
        /// <summary>
        /// 获取植被系统（用于外部访问）
        /// </summary>
        public VegetationSystem VegetationSystem => vegetationSystem;
        
        [Header("放置数据")]
        [SerializeField] private PlacementDatabase placementDatabase;
        [SerializeField] private List<PlacementLayer> placementLayers = new List<PlacementLayer>();
        
        // 事件
        public System.Action<float> OnPlacementProgress;
        public System.Action OnPlacementComplete;
        public System.Action<string> OnPlacementError;
        
        // 私有变量
        private bool isPlacing = false;
        private Coroutine placementCoroutine;
        private PlacementGrid placementGrid;
        private System.Random placementRandom;
        
        // 植被相关
        private bool isVegetationPainting = false;
        private VegetationType selectedVegetationType = VegetationType.针叶树;
        private float vegetationBrushSize = 10f;
        private float vegetationDensity = 0.5f;
        
        // 公开属性
        public bool IsVegetationPainting => isVegetationPainting;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void InitializeComponents()
        {
            Debug.Log("[SmartPlacement] 开始初始化组件...");
            
            // 初始化分析器组件
            if (terrainAnalyzer == null)
                terrainAnalyzer = GetComponent<TerrainAnalyzer>() ?? gameObject.AddComponent<TerrainAnalyzer>();
                
            if (biomeAnalyzer == null)
                biomeAnalyzer = GetComponent<BiomeAnalyzer>() ?? gameObject.AddComponent<BiomeAnalyzer>();
                
            if (ecosystemSimulator == null)
                ecosystemSimulator = GetComponent<EcosystemSimulator>() ?? gameObject.AddComponent<EcosystemSimulator>();
                
            if (ruleEngine == null)
                ruleEngine = GetComponent<PlacementRuleEngine>() ?? gameObject.AddComponent<PlacementRuleEngine>();
                
            if (densityManager == null)
                densityManager = GetComponent<DensityManager>() ?? gameObject.AddComponent<DensityManager>();
            
            // 初始化放置网格
            placementGrid = new PlacementGrid();
            Debug.Log("[SmartPlacement] 放置网格已初始化");
            
            // 初始化随机数生成器
            placementRandom = new System.Random();
            Debug.Log("[SmartPlacement] 随机数生成器已初始化");
            
            // 初始化植被系统
            Debug.Log("[SmartPlacement] 初始化植被系统...");
            if (vegetationSystem == null)
            {
                vegetationSystem = new VegetationSystem();
                Debug.Log("[SmartPlacement] 创建新的植被系统实例");
            }
            
            vegetationSystem.Initialize(transform);
            Debug.Log("[SmartPlacement] 植被系统初始化完成");
            
            
            Debug.Log("[SmartPlacement] 所有组件初始化完成");
        }
        
        /// <summary>
        /// 放置植被 - 主要入口点
        /// </summary>
        public void PlaceVegetation(WorldGenerationParameters parameters)
        {
            if (!enableSmartPlacement)
            {
                Debug.LogWarning("[SmartPlacement] 智能放置系统已禁用");
                return;
            }
            
            if (isPlacing)
            {
                Debug.LogWarning("[SmartPlacement] 放置操作正在进行中...");
                return;
            }
            
            Debug.Log("[SmartPlacement] 开始智能植被放置...");
            
            if (enableRealTimePlacement)
            {
                placementCoroutine = StartCoroutine(PlaceVegetationProgressive(parameters));
            }
            else
            {
                PlaceVegetationImmediate(parameters);
            }
        }
        
        /// <summary>
        /// 放置结构 - 建筑物、道路等
        /// </summary>
        public void PlaceStructures(WorldGenerationParameters parameters)
        {
            if (!enableSmartPlacement)
            {
                Debug.LogWarning("[SmartPlacement] 智能放置系统已禁用");
                return;
            }
            
            if (isPlacing)
            {
                Debug.LogWarning("[SmartPlacement] 放置操作正在进行中...");
                return;
            }
            
            Debug.Log("[SmartPlacement] 开始智能结构放置...");
            
            if (enableRealTimePlacement)
            {
                placementCoroutine = StartCoroutine(PlaceStructuresProgressive(parameters));
            }
            else
            {
                PlaceStructuresImmediate(parameters);
            }
        }
        
        #region 植被系统公开方法
        
        /// <summary>
        /// 激活植被绘制模式
        /// </summary>
        public void ActivateVegetationPainting(bool activate)
        {
            isVegetationPainting = activate;
            if (activate)
            {
                Debug.Log("[植被系统] 绘制模式已激活");
            }
            else
            {
                Debug.Log("[植被系统] 绘制模式已退出");
            }
        }
        
        /// <summary>
        /// 设置当前选中的植被类型
        /// </summary>
        public void SetSelectedVegetationType(VegetationType type)
        {
            selectedVegetationType = type;
        }
        
        /// <summary>
        /// 设置植被画笔参数
        /// </summary>
        public void SetVegetationBrushSettings(float brushSize, float density)
        {
            vegetationBrushSize = brushSize;
            vegetationDensity = density;
        }
        
        /// <summary>
        /// 在指定位置绘制植被
        /// </summary>
        public void PaintVegetationAt(Vector3 worldPosition, Terrain terrain)
        {
            if (!isVegetationPainting || vegetationSystem == null)
                return;
                
            EnsureVegetationSystemInitialized();
            
            // 使用资产库中的预制件进行植被绘制
            PaintVegetationFromAssetLibrary(worldPosition, terrain);
        }
        
        /// <summary>
        /// 从资产库中绘制植被
        /// </summary>
        void PaintVegetationFromAssetLibrary(Vector3 worldPosition, Terrain terrain)
        {
            // 从植被库获取对应类型的预制件
            var vegetationData = vegetationSystem.Library?.GetVegetationData(selectedVegetationType);
            if (vegetationData == null)
            {
                Debug.LogWarning($"[SmartPlacement] 没有找到 {selectedVegetationType} 类型的植被数据");
                return;
            }
            
            var availablePrefabs = vegetationData.GetAllPrefabs();
            if (availablePrefabs.Count == 0)
            {
                Debug.LogWarning($"[SmartPlacement] {selectedVegetationType} 类型没有配置预制件资产，请在植被库中添加预制件");
                return;
            }
            
            int paintCount = Mathf.RoundToInt(vegetationDensity * 10); // 根据密度计算绘制数量
            
            for (int i = 0; i < paintCount; i++)
            {
                // 在笔刷范围内随机生成位置
                Vector2 randomOffset = Random.insideUnitCircle * vegetationBrushSize;
                Vector3 paintPosition = worldPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
                
                // 获取地形高度
                if (terrain != null)
                {
                    float terrainHeight = terrain.SampleHeight(paintPosition);
                    paintPosition.y = terrainHeight;
                }
                
                // 随机选择一个预制件
                GameObject prefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
                
                if (prefab != null)
                {
                    // 实例化预制件
                    GameObject instance = Instantiate(prefab, paintPosition, Random.rotation);
                    instance.name = $"{selectedVegetationType}_Asset_{Time.time:F2}";
                    
                    // 随机缩放（使用植被数据中的缩放范围）
                    float randomScale = Random.Range(vegetationData.minScale, vegetationData.maxScale);
                    instance.transform.localScale = Vector3.one * randomScale;
                    
                    Debug.Log($"[SmartPlacement] 成功放置资产预制件: {instance.name} ({prefab.name}) 在位置 {paintPosition}");
                }
                else
                {
                    Debug.LogWarning($"[SmartPlacement] 预制件引用为空: {selectedVegetationType}");
                }
            }
        }
        
        /// <summary>
        /// 应用植被模板
        /// </summary>
        public void ApplyVegetationTemplate(string templateName, Terrain terrain)
        {
            EnsureVegetationSystemInitialized();
            
            if (vegetationSystem?.Library?.templates == null)
            {
                Debug.LogError("[植被系统] 植被库未初始化");
                return;
            }
            
            var template = vegetationSystem.Library.templates.Find(t => t.templateName == templateName);
            if (template != null)
            {
                var parameters = new VegetationDistributionParams
                {
                    globalDensity = template.density,
                    respectBiomes = true,
                    respectHeight = true,
                    respectSlope = true,
                    respectTextures = true
                };
                
                vegetationSystem.DistributeVegetation(terrain, parameters);
                Debug.Log($"[植被系统] 已应用模板: {templateName}");
            }
            else
            {
                Debug.LogWarning($"[植被系统] 未找到模板: {templateName}");
            }
        }
        
        /// <summary>
        /// 清除所有植被
        /// </summary>
        public void ClearAllVegetation()
        {
            EnsureVegetationSystemInitialized();
            vegetationSystem?.ClearAllVegetation();
        }
        
        /// <summary>
        /// 确保植被系统已初始化
        /// </summary>
        void EnsureVegetationSystemInitialized()
        {
            if (vegetationSystem != null && !vegetationSystem.IsInitialized)
            {
                vegetationSystem.Initialize(transform);
            }
        }
        
        /// <summary>
        /// 获取植被统计信息
        /// </summary>
        public VegetationStatistics GetVegetationStatistics()
        {
            EnsureVegetationSystemInitialized();
            return vegetationSystem?.GetStatistics() ?? new VegetationStatistics();
        }
        
        #endregion
        
        /// <summary>
        /// 立即放置植被（同步）
        /// </summary>
        void PlaceVegetationImmediate(WorldGenerationParameters parameters)
        {
            isPlacing = true;
            
            try
            {
                // 分析地形
                var terrainData = terrainAnalyzer.AnalyzeTerrain(parameters);
                var biomeData = biomeAnalyzer.AnalyzeBiomes(parameters, terrainData);
                
                // 设置放置网格
                placementGrid.Initialize(parameters.generationBounds, 5f); // 5米网格
                
                // 执行放置逻辑
                ExecuteVegetationPlacement(parameters, terrainData, biomeData);
                
                OnPlacementComplete?.Invoke();
                Debug.Log("[SmartPlacement] 植被放置完成!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmartPlacement] 植被放置错误: {e.Message}");
                OnPlacementError?.Invoke(e.Message);
            }
            finally
            {
                isPlacing = false;
            }
        }
        
        /// <summary>
        /// 渐进式放置植被（异步）
        /// </summary>
        IEnumerator PlaceVegetationProgressive(WorldGenerationParameters parameters)
        {
            isPlacing = true;
            float totalSteps = 4f;
            float currentStep = 0f;
            
            // 步骤1: 分析地形
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            var terrainData = TryAnalyzeTerrain(parameters);
            if (terrainData == null)
            {
                OnPlacementError?.Invoke("地形分析失败");
                isPlacing = false;
                placementCoroutine = null;
                yield break;
            }
            currentStep++;
            yield return null;
            
            // 步骤2: 分析生物群落
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            var biomeData = TryAnalyzeBiomes(parameters, terrainData);
            if (biomeData == null)
            {
                OnPlacementError?.Invoke("生物群落分析失败");
                isPlacing = false;
                placementCoroutine = null;
                yield break;
            }
            currentStep++;
            yield return null;
            
            // 步骤3: 设置放置网格
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            if (!TryInitializePlacementGrid(parameters))
            {
                OnPlacementError?.Invoke("放置网格初始化失败");
                isPlacing = false;
                placementCoroutine = null;
                yield break;
            }
            currentStep++;
            yield return null;
            
            // 步骤4: 执行植被放置
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            yield return StartCoroutine(ExecuteVegetationPlacementProgressive(parameters, terrainData, biomeData));
            currentStep++;
            
            OnPlacementProgress?.Invoke(1f);
            OnPlacementComplete?.Invoke();
            Debug.Log("[SmartPlacement] 渐进式植被放置完成!");
            
            isPlacing = false;
            placementCoroutine = null;
        }
        
        TerrainData TryAnalyzeTerrain(WorldGenerationParameters parameters)
        {
            try
            {
                return terrainAnalyzer.AnalyzeTerrain(parameters);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmartPlacement] 地形分析错误: {e.Message}");
                return null;
            }
        }
        
        BiomeData TryAnalyzeBiomes(WorldGenerationParameters parameters, TerrainData terrainData)
        {
            try
            {
                return biomeAnalyzer.AnalyzeBiomes(parameters, terrainData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmartPlacement] 生物群落分析错误: {e.Message}");
                return null;
            }
        }
        
        bool TryInitializePlacementGrid(WorldGenerationParameters parameters)
        {
            try
            {
                placementGrid.Initialize(parameters.generationBounds, 5f);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmartPlacement] 放置网格初始化错误: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 立即放置结构（同步）
        /// </summary>
        void PlaceStructuresImmediate(WorldGenerationParameters parameters)
        {
            isPlacing = true;
            
            try
            {
                var terrainData = terrainAnalyzer.AnalyzeTerrain(parameters);
                var biomeData = biomeAnalyzer.AnalyzeBiomes(parameters, terrainData);
                
                placementGrid.Initialize(parameters.generationBounds, 10f); // 10米网格用于结构
                
                ExecuteStructurePlacement(parameters, terrainData, biomeData);
                
                OnPlacementComplete?.Invoke();
                Debug.Log("[SmartPlacement] 结构放置完成!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmartPlacement] 结构放置错误: {e.Message}");
                OnPlacementError?.Invoke(e.Message);
            }
            finally
            {
                isPlacing = false;
            }
        }
        
        /// <summary>
        /// 渐进式放置结构（异步）
        /// </summary>
        IEnumerator PlaceStructuresProgressive(WorldGenerationParameters parameters)
        {
            isPlacing = true;
            float totalSteps = 4f;
            float currentStep = 0f;
            
            // 步骤1: 分析地形
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            var terrainData = TryAnalyzeTerrain(parameters);
            if (terrainData == null)
            {
                OnPlacementError?.Invoke("地形分析失败");
                isPlacing = false;
                placementCoroutine = null;
                yield break;
            }
            currentStep++;
            yield return null;
            
            // 步骤2: 分析生物群落
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            var biomeData = TryAnalyzeBiomes(parameters, terrainData);
            if (biomeData == null)
            {
                OnPlacementError?.Invoke("生物群落分析失败");
                isPlacing = false;
                placementCoroutine = null;
                yield break;
            }
            currentStep++;
            yield return null;
            
            // 步骤3: 初始化放置网格
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            if (!TryInitializeStructurePlacementGrid(parameters))
            {
                OnPlacementError?.Invoke("结构放置网格初始化失败");
                isPlacing = false;
                placementCoroutine = null;
                yield break;
            }
            currentStep++;
            yield return null;
            
            // 步骤4: 执行结构放置
            OnPlacementProgress?.Invoke(currentStep / totalSteps);
            yield return StartCoroutine(ExecuteStructurePlacementProgressive(parameters, terrainData, biomeData));
            currentStep++;
            
            OnPlacementProgress?.Invoke(1f);
            OnPlacementComplete?.Invoke();
            Debug.Log("[SmartPlacement] 渐进式结构放置完成!");
            
            isPlacing = false;
            placementCoroutine = null;
        }
        
        bool TryInitializeStructurePlacementGrid(WorldGenerationParameters parameters)
        {
            try
            {
                placementGrid.Initialize(parameters.generationBounds, 10f);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmartPlacement] 结构放置网格初始化错误: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 执行植被放置逻辑
        /// </summary>
        void ExecuteVegetationPlacement(WorldGenerationParameters parameters, TerrainData terrainData, BiomeData biomeData)
        {
            var vegetationParams = parameters.vegetationParams;
            
            // 为每个植被类型执行放置
            foreach (var layer in placementLayers)
            {
                if (layer.layerType != PlacementLayerType.Vegetation) continue;
                
                ExecutePlacementLayer(layer, parameters, terrainData, biomeData);
            }
            
            // 应用生态系统模拟
            if (vegetationParams.enableEcosystemSimulation)
            {
                ecosystemSimulator.SimulateEcosystem(placementGrid, parameters);
            }
        }
        
        /// <summary>
        /// 执行植被放置逻辑（异步）
        /// </summary>
        IEnumerator ExecuteVegetationPlacementProgressive(WorldGenerationParameters parameters, TerrainData terrainData, BiomeData biomeData)
        {
            var vegetationParams = parameters.vegetationParams;
            
            foreach (var layer in placementLayers)
            {
                if (layer.layerType != PlacementLayerType.Vegetation) continue;
                
                yield return StartCoroutine(ExecutePlacementLayerProgressive(layer, parameters, terrainData, biomeData));
            }
            
            if (vegetationParams.enableEcosystemSimulation)
            {
                yield return StartCoroutine(ecosystemSimulator.SimulateEcosystemProgressive(placementGrid, parameters, placementStepsPerFrame));
            }
        }
        
        /// <summary>
        /// 执行结构放置逻辑
        /// </summary>
        void ExecuteStructurePlacement(WorldGenerationParameters parameters, TerrainData terrainData, BiomeData biomeData)
        {
            foreach (var layer in placementLayers)
            {
                if (layer.layerType != PlacementLayerType.Structure) continue;
                
                ExecutePlacementLayer(layer, parameters, terrainData, biomeData);
            }
        }
        
        /// <summary>
        /// 执行结构放置逻辑（异步）
        /// </summary>
        IEnumerator ExecuteStructurePlacementProgressive(WorldGenerationParameters parameters, TerrainData terrainData, BiomeData biomeData)
        {
            foreach (var layer in placementLayers)
            {
                if (layer.layerType != PlacementLayerType.Structure) continue;
                
                yield return StartCoroutine(ExecutePlacementLayerProgressive(layer, parameters, terrainData, biomeData));
            }
        }
        
        /// <summary>
        /// 执行单个放置层
        /// </summary>
        void ExecutePlacementLayer(PlacementLayer layer, WorldGenerationParameters parameters, TerrainData terrainData, BiomeData biomeData)
        {
            var bounds = parameters.generationBounds;
            var density = densityManager.CalculateDensity(layer, parameters);
            
            int totalPlacementAttempts = Mathf.RoundToInt(bounds.size.x * bounds.size.z * density / 100f);
            
            for (int i = 0; i < totalPlacementAttempts; i++)
            {
                Vector3 candidatePosition = GenerateCandidatePosition(bounds);
                
                if (ruleEngine.EvaluatePlacementRules(layer, candidatePosition, terrainData, biomeData))
                {
                    PlaceObjectAtPosition(layer, candidatePosition, terrainData);
                }
            }
        }
        
        /// <summary>
        /// 执行单个放置层（异步）
        /// </summary>
        IEnumerator ExecutePlacementLayerProgressive(PlacementLayer layer, WorldGenerationParameters parameters, TerrainData terrainData, BiomeData biomeData)
        {
            var bounds = parameters.generationBounds;
            var density = densityManager.CalculateDensity(layer, parameters);
            
            int totalPlacementAttempts = Mathf.RoundToInt(bounds.size.x * bounds.size.z * density / 100f);
            int processedAttempts = 0;
            
            for (int i = 0; i < totalPlacementAttempts; i++)
            {
                Vector3 candidatePosition = GenerateCandidatePosition(bounds);
                
                if (ruleEngine.EvaluatePlacementRules(layer, candidatePosition, terrainData, biomeData))
                {
                    PlaceObjectAtPosition(layer, candidatePosition, terrainData);
                }
                
                processedAttempts++;
                if (processedAttempts >= placementStepsPerFrame)
                {
                    processedAttempts = 0;
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// 生成候选位置
        /// </summary>
        Vector3 GenerateCandidatePosition(Bounds bounds)
        {
            float x = (float)placementRandom.NextDouble() * bounds.size.x + bounds.min.x;
            float z = (float)placementRandom.NextDouble() * bounds.size.z + bounds.min.z;
            
            return new Vector3(x, 0f, z);
        }
        
        /// <summary>
        /// 在指定位置放置对象
        /// </summary>
        void PlaceObjectAtPosition(PlacementLayer layer, Vector3 position, TerrainData terrainData)
        {
            // 获取地形高度
            position.y = GetTerrainHeightAtPosition(position);
            
            // 选择预制件
            GameObject prefab = SelectPrefabFromLayer(layer);
            if (prefab == null) return;
            
            // 实例化对象
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            
            // 应用随机旋转
            if (layer.enableRandomRotation)
            {
                float randomY = (float)placementRandom.NextDouble() * 360f;
                instance.transform.rotation = Quaternion.Euler(0f, randomY, 0f);
            }
            
            // 应用随机缩放
            if (layer.enableRandomScale)
            {
                float scale = Mathf.Lerp(layer.minScale, layer.maxScale, (float)placementRandom.NextDouble());
                instance.transform.localScale = Vector3.one * scale;
            }
            
            // 设置父对象
            if (layer.parentTransform != null)
            {
                instance.transform.SetParent(layer.parentTransform);
            }
            
            // 注册到放置网格
            placementGrid.RegisterObject(instance, position);
        }
        
        /// <summary>
        /// 从层中选择预制件
        /// </summary>
        GameObject SelectPrefabFromLayer(PlacementLayer layer)
        {
            if (layer.prefabs == null || layer.prefabs.Length == 0) return null;
            
            if (layer.useWeightedSelection && layer.prefabWeights != null && layer.prefabWeights.Length == layer.prefabs.Length)
            {
                return SelectWeightedPrefab(layer.prefabs, layer.prefabWeights);
            }
            else
            {
                int randomIndex = placementRandom.Next(0, layer.prefabs.Length);
                return layer.prefabs[randomIndex];
            }
        }
        
        /// <summary>
        /// 使用权重选择预制件
        /// </summary>
        GameObject SelectWeightedPrefab(GameObject[] prefabs, float[] weights)
        {
            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }
            
            float randomValue = (float)placementRandom.NextDouble() * totalWeight;
            float currentWeight = 0f;
            
            for (int i = 0; i < prefabs.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return prefabs[i];
                }
            }
            
            return prefabs[prefabs.Length - 1];
        }
        
        /// <summary>
        /// 获取地形高度
        /// </summary>
        float GetTerrainHeightAtPosition(Vector3 worldPosition)
        {
            // 这里需要与地形系统集成来获取实际高度
            // 临时返回0
            return 0f;
        }
        
        /// <summary>
        /// 停止放置操作
        /// </summary>
        public void StopPlacement()
        {
            if (placementCoroutine != null)
            {
                StopCoroutine(placementCoroutine);
                placementCoroutine = null;
            }
            isPlacing = false;
            Debug.Log("[SmartPlacement] 放置操作已停止");
        }
        
        /// <summary>
        /// 清除所有放置的对象
        /// </summary>
        public void ClearAllPlacements()
        {
            placementGrid.ClearAll();
            Debug.Log("[SmartPlacement] 已清除所有放置的对象");
        }
        
        /// <summary>
        /// 获取当前放置状态
        /// </summary>
        public bool IsPlacing()
        {
            return isPlacing;
        }
        
        /// <summary>
        /// 获取放置是否活跃（与IsPlacing相同）
        /// </summary>
        public bool IsPlacementActive()
        {
            return isPlacing;
        }
    }
}