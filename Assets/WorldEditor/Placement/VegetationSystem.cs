using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 植被系统 - 智能放置系统的植被特化模块
    /// 专门处理各种植被类型的智能分布和放置
    /// </summary>
    [System.Serializable]
    public class VegetationSystem
    {
        [Header("植被设置")]
        [SerializeField] private bool enableVegetation = true;
        [SerializeField] private float globalVegetationDensity = 1.0f;
        [SerializeField] private VegetationLibrary vegetationLibrary;
        [SerializeField] private List<VegetationLayer> vegetationLayers = new List<VegetationLayer>();
        
        [Header("智能分布")]
        [SerializeField] private bool enableBiomeBasedDistribution = true;
        [SerializeField] private bool enableHeightBasedDistribution = true;
        [SerializeField] private bool enableSlopeBasedDistribution = true;
        [SerializeField] private bool enableTextureBasedDistribution = true;
        
        [Header("生态系统模拟")]
        [SerializeField] private bool enableEcosystemRules = true;
        [SerializeField] private bool enableSpeciesCompetition = false;
        [SerializeField] private bool enableSeasonalVariation = false;
        
        // 植被管理
        private Transform vegetationParent;
        private Dictionary<VegetationType, List<GameObject>> spawnedVegetation;
        private VegetationDistributor distributor;
        
        public bool EnableVegetation => enableVegetation;
        public VegetationLibrary Library => vegetationLibrary;
        public bool IsInitialized => spawnedVegetation != null;
        
        public void Initialize(Transform parent = null)
        {
            if (parent != null)
            {
                vegetationParent = parent;
            }
            else
            {
                var go = new GameObject("VegetationSystem");
                vegetationParent = go.transform;
            }
            
            spawnedVegetation = new Dictionary<VegetationType, List<GameObject>>();
            distributor = new VegetationDistributor(this);
            
            InitializeVegetationLibrary();
            InitializeVegetationLayers();
            
            Debug.Log($"[VegetationSystem] 已初始化，植被库包含 {vegetationLibrary?.vegetationTypes?.Count ?? 0} 种植被类型");
        }
        
        void InitializeVegetationLibrary()
        {
            if (vegetationLibrary == null)
            {
                vegetationLibrary = ScriptableObject.CreateInstance<VegetationLibrary>();
                vegetationLibrary.InitializeDefaultVegetation();
            }
        }
        
        void InitializeVegetationLayers()
        {
            if (vegetationLayers == null || vegetationLayers.Count == 0)
            {
                vegetationLayers = new List<VegetationLayer>
                {
                    CreateDefaultLayer("Trees", VegetationType.针叶树, VegetationType.阔叶树, VegetationType.果树),
                    CreateDefaultLayer("Bushes", VegetationType.普通灌木, VegetationType.浆果灌木),
                    CreateDefaultLayer("Grass", VegetationType.野草, VegetationType.鲜花),
                    CreateDefaultLayer("Special", VegetationType.仙人掌, VegetationType.蘑菇)
                };
            }
        }
        
        VegetationLayer CreateDefaultLayer(string name, params VegetationType[] types)
        {
            var layer = new VegetationLayer
            {
                layerName = name,
                enabled = true,
                density = 1.0f,
                minScale = 0.8f,
                maxScale = 1.2f,
                allowedTypes = new List<VegetationType>(types)
            };
            return layer;
        }
        
        /// <summary>
        /// 在指定位置放置植被
        /// </summary>
        public GameObject PlaceVegetation(VegetationType type, Vector3 position, VegetationPlacementSettings settings = null)
        {
            if (!enableVegetation) 
            {
                Debug.LogWarning("[VegetationSystem] 植被系统未启用");
                return null;
            }
            
            Debug.Log($"[VegetationSystem] 尝试放置植被 - 类型: {type}, 位置: {position}");
            
            // 确保字典已初始化
            if (spawnedVegetation == null)
            {
                spawnedVegetation = new Dictionary<VegetationType, List<GameObject>>();
                Debug.Log("[VegetationSystem] 初始化植被字典");
            }
            
            // 确保植被库已初始化
            if (vegetationLibrary == null)
            {
                Debug.LogWarning("[VegetationSystem] 植被库为空，尝试初始化...");
                InitializeVegetationLibrary();
            }
            
            var vegetationData = vegetationLibrary?.GetVegetationData(type);
            if (vegetationData == null)
            {
                Debug.LogError($"[VegetationSystem] 未找到植被类型: {type}，可用类型: {(vegetationLibrary?.vegetationTypes?.Count ?? 0)}");
                
                // 尝试列出所有可用的植被类型
                if (vegetationLibrary?.vegetationTypes != null)
                {
                    Debug.Log("[VegetationSystem] 可用植被类型列表:");
                    foreach (var veg in vegetationLibrary.vegetationTypes)
                    {
                        Debug.Log($"  - {veg.type}: {veg.displayName}");
                    }
                }
                
                return null;
            }
            
            Debug.Log($"[VegetationSystem] 找到植被数据: {vegetationData.displayName}");
            
            // 创建植被对象
            GameObject vegetationObject = CreateVegetationObject(vegetationData, position, settings);
            
            if (vegetationObject == null)
            {
                Debug.LogError($"[VegetationSystem] 创建植被对象失败 - 类型: {type}");
                return null;
            }
            
            // 添加到管理系统
            if (!spawnedVegetation.ContainsKey(type))
                spawnedVegetation[type] = new List<GameObject>();
            
            spawnedVegetation[type].Add(vegetationObject);
            
            Debug.Log($"[VegetationSystem] 成功创建植被: {vegetationObject.name}, 当前该类型数量: {spawnedVegetation[type].Count}");
            
            return vegetationObject;
        }
        
        /// <summary>
        /// 智能分布植被到整个地形
        /// </summary>
        public void DistributeVegetation(Terrain terrain, VegetationDistributionParams parameters)
        {
            if (!enableVegetation || terrain == null)
            {
                Debug.LogWarning("[VegetationSystem] 植被系统未启用或地形为空");
                return;
            }
            
            Debug.Log("[VegetationSystem] 开始智能植被分布...");
            
            distributor.DistributeOnTerrain(terrain, parameters);
        }
        
        /// <summary>
        /// 在画笔范围内放置植被
        /// </summary>
        public void PaintVegetation(Vector3 centerPosition, float brushSize, VegetationType type, float density, Terrain terrain)
        {
            if (!enableVegetation) 
            {
                Debug.LogWarning("[VegetationSystem] 植被系统未启用");
                return;
            }
            
            Debug.Log($"[VegetationSystem] 开始绘制植被 - 类型: {type}, 位置: {centerPosition}, 画笔大小: {brushSize}");
            
            // 确保植被库已初始化
            if (vegetationLibrary == null)
            {
                Debug.LogWarning("[VegetationSystem] 植被库未初始化，正在初始化...");
                InitializeVegetationLibrary();
            }
            
            // 修复计算公式，确保至少放置1个对象
            int count = Mathf.Max(1, Mathf.RoundToInt(density * brushSize * 0.5f));
            Debug.Log($"[VegetationSystem] 计算放置数量: {count}");
            
            int successCount = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * brushSize;
                Vector3 position = centerPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
                
                // 使用改进的地形高度获取方法
                Vector3 terrainPos;
                if (GetImprovedTerrainHeightAtPosition(terrain, position, out terrainPos))
                {
                    GameObject vegetation = PlaceVegetation(type, terrainPos);
                    if (vegetation != null)
                    {
                        successCount++;
                        Debug.Log($"[VegetationSystem] 成功放置植被 #{successCount}: {vegetation.name} 在位置 {terrainPos}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[VegetationSystem] 无法获取地形高度，位置: {position}");
                }
            }
            
            Debug.Log($"[VegetationSystem] 植被绘制完成 - 成功放置: {successCount}/{count}");
        }
        
        /// <summary>
        /// 清除所有植被
        /// </summary>
        public void ClearAllVegetation()
        {
            // 确保字典已初始化
            if (spawnedVegetation == null)
            {
                spawnedVegetation = new Dictionary<VegetationType, List<GameObject>>();
                return;
            }
            
            foreach (var vegetationList in spawnedVegetation.Values)
            {
                if (vegetationList != null)
                {
                    foreach (var vegetation in vegetationList)
                    {
                        if (vegetation != null)
                            Object.DestroyImmediate(vegetation);
                    }
                }
            }
            
            spawnedVegetation.Clear();
            Debug.Log("[VegetationSystem] 已清除所有植被");
        }
        
        /// <summary>
        /// 清除指定类型的植被
        /// </summary>
        public void ClearVegetationType(VegetationType type)
        {
            // 确保字典已初始化
            if (spawnedVegetation == null)
            {
                spawnedVegetation = new Dictionary<VegetationType, List<GameObject>>();
                return;
            }
            
            if (spawnedVegetation.ContainsKey(type))
            {
                foreach (var vegetation in spawnedVegetation[type])
                {
                    if (vegetation != null)
                        Object.DestroyImmediate(vegetation);
                }
                
                spawnedVegetation[type].Clear();
                Debug.Log($"[VegetationSystem] 已清除植被类型: {type}");
            }
        }
        
        /// <summary>
        /// 获取植被统计信息
        /// </summary>
        public VegetationStatistics GetStatistics()
        {
            var stats = new VegetationStatistics();
            
            // 确保字典已初始化
            if (spawnedVegetation == null)
            {
                spawnedVegetation = new Dictionary<VegetationType, List<GameObject>>();
            }
            
            foreach (var kvp in spawnedVegetation)
            {
                if (kvp.Value != null)
                {
                    stats.vegetationCounts[kvp.Key] = kvp.Value.Count;
                    stats.totalCount += kvp.Value.Count;
                }
            }
            
            return stats;
        }
        
        GameObject CreateVegetationObject(VegetationData data, Vector3 position, VegetationPlacementSettings settings)
        {
            Debug.Log($"[VegetationSystem] 创建植被对象 - 类型: {data.type}, 名称: {data.displayName}, 位置: {position}");
            
            GameObject vegetation;
            
            if (data.prefab != null)
            {
                Debug.Log($"[VegetationSystem] 使用预制件创建植被: {data.prefab.name}");
                vegetation = Object.Instantiate(data.prefab, position, Quaternion.identity, vegetationParent);
            }
            else
            {
                Debug.Log($"[VegetationSystem] 创建程序化植被 - 类型: {data.type}");
                
                // 创建程序化植被
                try
                {
                    vegetation = CreateProceduralVegetation(data.type);
                    if (vegetation == null)
                    {
                        Debug.LogError($"[VegetationSystem] CreateProceduralVegetation返回null - 类型: {data.type}");
                        return null;
                    }
                    
                    vegetation.transform.position = position;
                    vegetation.transform.SetParent(vegetationParent);
                    Debug.Log($"[VegetationSystem] 程序化植被创建成功: {vegetation.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[VegetationSystem] 创建程序化植被时出错: {e.Message}\n{e.StackTrace}");
                    return null;
                }
            }
            
            if (vegetation == null)
            {
                Debug.LogError("[VegetationSystem] 植被对象创建失败");
                return null;
            }
            
            // 应用变化设置
            try
            {
                ApplyVegetationVariations(vegetation, data, settings);
                Debug.Log($"[VegetationSystem] 已应用植被变化");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VegetationSystem] 应用植被变化时出错: {e.Message}");
            }
            
            // 添加植被组件
            try
            {
                var component = vegetation.GetComponent<VegetationComponent>() ?? vegetation.AddComponent<VegetationComponent>();
                component.vegetationType = data.type;
                component.plantingTime = System.DateTime.Now.ToString();
                Debug.Log($"[VegetationSystem] 已添加植被组件");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VegetationSystem] 添加植被组件时出错: {e.Message}");
            }
            
            // 设置名称
            try
            {
                vegetation.name = $"{data.displayName}_{spawnedVegetation.GetValueOrDefault(data.type, new List<GameObject>()).Count:000}";
                Debug.Log($"[VegetationSystem] 植被对象最终名称: {vegetation.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VegetationSystem] 设置植被名称时出错: {e.Message}");
                vegetation.name = $"Vegetation_{data.type}";
            }
            
            return vegetation;
        }
        
        void ApplyVegetationVariations(GameObject vegetation, VegetationData data, VegetationPlacementSettings settings)
        {
            if (settings == null) settings = VegetationPlacementSettings.Default;
            
            // 随机旋转
            if (settings.enableRotationVariation)
            {
                float randomRotation = Random.Range(0f, 360f);
                vegetation.transform.rotation = Quaternion.AngleAxis(randomRotation, Vector3.up);
            }
            
            // 随机缩放
            if (settings.enableScaleVariation)
            {
                float scaleVariation = Random.Range(data.minScale, data.maxScale);
                if (settings.scaleVariationAmount > 0)
                {
                    scaleVariation *= Random.Range(1f - settings.scaleVariationAmount, 1f + settings.scaleVariationAmount);
                }
                vegetation.transform.localScale = Vector3.one * scaleVariation;
            }
            
            // 颜色变化
            if (settings.enableColorVariation && settings.colorVariationAmount > 0)
            {
                var renderers = vegetation.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.material != null)
                    {
                        Color baseColor = data.tintColor;
                        Color variation = new Color(
                            Random.Range(-settings.colorVariationAmount, settings.colorVariationAmount),
                            Random.Range(-settings.colorVariationAmount, settings.colorVariationAmount),
                            Random.Range(-settings.colorVariationAmount, settings.colorVariationAmount),
                            0
                        );
                        renderer.material.color = baseColor + variation;
                    }
                }
            }
        }
        
        GameObject CreateProceduralVegetation(VegetationType type)
        {
            Debug.Log($"[VegetationSystem] 开始创建程序化植被 - 类型: {type}");
            
            GameObject result = null;
            
            try
            {
                switch (type)
                {
                    case VegetationType.针叶树:
                        Debug.Log("[VegetationSystem] 创建针叶树（北欧云杉）");
                        result = CreateProceduralTree(type);
                        break;
                        
                    case VegetationType.阔叶树:
                    case VegetationType.棕榈树:
                    case VegetationType.果树:
                    case VegetationType.枯树:
                        Debug.Log($"[VegetationSystem] 创建树木 - 类型: {type}");
                        result = CreateProceduralTree(type);
                        break;
                        
                    case VegetationType.普通灌木:
                    case VegetationType.浆果灌木:
                    case VegetationType.荆棘丛:
                    case VegetationType.竹子:
                        Debug.Log($"[VegetationSystem] 创建灌木 - 类型: {type}");
                        result = CreateProceduralBush(type);
                        break;
                        
                    case VegetationType.野草:
                    case VegetationType.鲜花:
                    case VegetationType.蕨类:
                        Debug.Log($"[VegetationSystem] 创建草本植物 - 类型: {type}");
                        result = CreateProceduralGrass(type);
                        break;
                        
                    case VegetationType.苔藓:
                        Debug.Log("[VegetationSystem] 创建苔藓");
                        result = CreateProceduralGrass(type);
                        break;
                        
                    case VegetationType.仙人掌:
                        Debug.Log("[VegetationSystem] 创建仙人掌");
                        result = CreateProceduralCactus();
                        break;
                        
                    case VegetationType.蘑菇:
                        Debug.Log("[VegetationSystem] 创建蘑菇");
                        result = CreateProceduralMushroom();
                        break;
                        
                    default:
                        Debug.LogWarning($"[VegetationSystem] 未知植被类型: {type}，使用默认野草");
                        result = CreateProceduralGrass(VegetationType.野草);
                        break;
                }
                
                if (result != null)
                {
                    Debug.Log($"[VegetationSystem] 程序化植被创建成功 - 类型: {type}, 对象名: {result.name}");
                }
                else
                {
                    Debug.LogError($"[VegetationSystem] 程序化植被创建失败 - 类型: {type}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VegetationSystem] 创建程序化植被时发生异常 - 类型: {type}, 错误: {e.Message}\n{e.StackTrace}");
                
                // 作为最后的备用方案，创建一个简单的立方体作为占位符
                Debug.Log("[VegetationSystem] 使用备用占位符");
                result = CreateFallbackVegetation(type);
            }
            
            return result;
        }
        
        /// <summary>
        /// 创建备用植被占位符
        /// </summary>
        GameObject CreateFallbackVegetation(VegetationType type)
        {
            Debug.Log($"[VegetationSystem] 创建备用植被占位符 - 类型: {type}");
            
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = $"Fallback_{type}";
            
            // 根据类型设置不同的颜色和大小
            var renderer = fallback.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            
            switch (type)
            {
                case VegetationType.针叶树:
                    fallback.transform.localScale = new Vector3(1f, 8f, 1f);
                    material.color = new Color(0.1f, 0.4f, 0.1f);
                    break;
                case VegetationType.阔叶树:
                    fallback.transform.localScale = new Vector3(1.5f, 6f, 1.5f);
                    material.color = new Color(0.2f, 0.6f, 0.2f);
                    break;
                default:
                    fallback.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                    material.color = new Color(0.3f, 0.7f, 0.3f);
                    break;
            }
            
            renderer.material = material;
            
            // 移动到正确位置（底部在地面上）
            var bounds = fallback.GetComponent<Collider>().bounds;
            fallback.transform.position += Vector3.up * bounds.extents.y;
            
            Debug.Log($"[VegetationSystem] 备用占位符创建完成: {fallback.name}");
            
            return fallback;
        }
        
        GameObject CreateProceduralTree(VegetationType type)
        {
            GameObject tree = new GameObject($"Tree_{type}");
            
            // 树干
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = Vector3.up * 1f;
            trunk.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
            
            // 树冠
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(tree.transform);
            crown.transform.localPosition = Vector3.up * 3f;
            
            // 根据类型调整外观
            switch (type)
            {
                case VegetationType.针叶树:
                    // 删除原始球形树冠
                    Object.DestroyImmediate(crown);
                    
                    // 创建多层锥形树冠，模拟云杉的分层结构
                    for (int i = 0; i < 4; i++)
                    {
                        GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        layer.name = $"Conifer_Layer_{i}";
                        layer.transform.SetParent(tree.transform);
                        
                        float layerHeight = 2.5f + i * 0.8f;
                        float layerScale = 2.2f - i * 0.4f; // 上层更小
                        
                        layer.transform.localPosition = Vector3.up * layerHeight;
                        layer.transform.localScale = new Vector3(layerScale, 0.6f, layerScale);
                        
                        // 渐变的绿色，上层稍微亮一些
                        float greenIntensity = 0.35f + i * 0.03f;
                        layer.GetComponent<Renderer>().sharedMaterial.color = new Color(0.08f, greenIntensity, 0.08f);
                    }
                    
                    // 调整树干，让它更高更细，符合云杉特征
                    trunk.transform.localScale = new Vector3(0.25f, 3.5f, 0.25f);
                    trunk.transform.localPosition = Vector3.up * 1.75f;
                    break;
                case VegetationType.阔叶树:
                    crown.transform.localScale = new Vector3(2.5f, 2f, 2.5f);
                    crown.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.6f, 0.2f);
                    break;
                case VegetationType.棕榈树:
                    crown.transform.localScale = new Vector3(1.8f, 0.5f, 1.8f);
                    crown.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.7f, 0.3f);
                    trunk.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
                    break;
                case VegetationType.果树:
                    crown.transform.localScale = new Vector3(2f, 2f, 2f);
                    crown.GetComponent<Renderer>().sharedMaterial.color = new Color(0.4f, 0.6f, 0.3f);
                    break;
                case VegetationType.枯树:
                    crown.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
                    crown.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.2f, 0.1f);
                    trunk.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.2f, 0.1f);
                    break;
            }
            
            trunk.GetComponent<Renderer>().sharedMaterial.color = new Color(0.4f, 0.2f, 0.1f);
            
            return tree;
        }
        
        GameObject CreateProceduralBush(VegetationType type)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = $"Bush_{type}";
            
            switch (type)
            {
                case VegetationType.普通灌木:
                    bush.transform.localScale = Vector3.one * Random.Range(1f, 1.8f);
                    bush.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.5f, 0.2f);
                    break;
                case VegetationType.浆果灌木:
                    bush.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
                    bush.GetComponent<Renderer>().sharedMaterial.color = new Color(0.4f, 0.5f, 0.2f);
                    break;
                case VegetationType.荆棘丛:
                    bush.transform.localScale = Vector3.one * Random.Range(0.6f, 1.2f);
                    bush.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.4f, 0.1f);
                    break;
                case VegetationType.竹子:
                    Object.DestroyImmediate(bush);
                    bush = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bush.name = "Bamboo";
                    bush.transform.localScale = new Vector3(0.1f, Random.Range(2f, 4f), 0.1f);
                    bush.GetComponent<Renderer>().sharedMaterial.color = new Color(0.5f, 0.7f, 0.3f);
                    break;
            }
            
            return bush;
        }
        
        GameObject CreateProceduralGrass(VegetationType type)
        {
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = $"Grass_{type}";
            
            switch (type)
            {
                case VegetationType.野草:
                    grass.transform.localScale = new Vector3(0.1f, Random.Range(0.3f, 0.8f), 0.1f);
                    grass.GetComponent<Renderer>().sharedMaterial.color = new Color(0.4f, 0.7f, 0.3f);
                    break;
                case VegetationType.鲜花:
                    grass.transform.localScale = new Vector3(0.1f, Random.Range(0.2f, 0.5f), 0.1f);
                    grass.GetComponent<Renderer>().sharedMaterial.color = new Color(0.8f, 0.4f, 0.6f);
                    break;
                case VegetationType.蕨类:
                    grass.transform.localScale = new Vector3(0.2f, Random.Range(0.4f, 0.9f), 0.2f);
                    grass.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.6f, 0.2f);
                    break;
                case VegetationType.苔藓:
                    grass.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
                    grass.GetComponent<Renderer>().sharedMaterial.color = new Color(0.3f, 0.5f, 0.1f);
                    break;
            }
            
            return grass;
        }
        
        GameObject CreateProceduralCactus()
        {
            GameObject cactus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cactus.name = "Cactus";
            cactus.transform.localScale = new Vector3(0.5f, Random.Range(1f, 2.5f), 0.5f);
            cactus.GetComponent<Renderer>().sharedMaterial.color = new Color(0.2f, 0.4f, 0.2f);
            return cactus;
        }
        
        GameObject CreateProceduralMushroom()
        {
            GameObject mushroom = new GameObject("Mushroom");
            
            // 蘑菇柄
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(mushroom.transform);
            stem.transform.localPosition = Vector3.up * 0.2f;
            stem.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
            
            // 蘑菇帽
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Cap";
            cap.transform.SetParent(mushroom.transform);
            cap.transform.localPosition = Vector3.up * 0.5f;
            cap.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
            
            // 设置颜色
            stem.GetComponent<Renderer>().sharedMaterial.color = Color.white;
            cap.GetComponent<Renderer>().sharedMaterial.color = new Color(0.8f, 0.2f, 0.2f);
            
            return mushroom;
        }
        
        bool GetTerrainHeightAtPosition(Terrain terrain, Vector3 worldPosition, out Vector3 terrainPosition)
        {
            terrainPosition = worldPosition;
            
            if (terrain == null) return false;
            
            Ray ray = new Ray(worldPosition + Vector3.up * 100f, Vector3.down);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 200f))
            {
                if (hit.collider.GetComponent<Terrain>() == terrain)
                {
                    terrainPosition = hit.point;
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 改进的地形高度获取方法 - 支持没有碰撞器的地形
        /// </summary>
        bool GetImprovedTerrainHeightAtPosition(Terrain terrain, Vector3 worldPosition, out Vector3 terrainPosition)
        {
            terrainPosition = worldPosition;
            
            if (terrain == null) 
            {
                Debug.LogError("[VegetationSystem] 地形对象为空");
                return false;
            }
            
            // 方法1: 先尝试使用物理raycast（如果地形有碰撞器）
            Ray ray = new Ray(worldPosition + Vector3.up * 100f, Vector3.down);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 200f))
            {
                if (hit.collider.GetComponent<Terrain>() == terrain)
                {
                    terrainPosition = hit.point;
                    Debug.Log($"[VegetationSystem] 使用物理raycast获取地形高度: {terrainPosition}");
                    return true;
                }
            }
            
            // 方法2: 使用Terrain API直接获取高度（推荐方法）
            try
            {
                UnityEngine.TerrainData terrainData = terrain.terrainData;
                if (terrainData == null)
                {
                    Debug.LogError("[VegetationSystem] TerrainData为空");
                    return false;
                }
                
                // 将世界坐标转换为地形相对坐标
                Vector3 terrainPos = worldPosition - terrain.transform.position;
                
                // 获取地形尺寸
                Vector3 terrainSize = terrainData.size;
                
                // 检查位置是否在地形范围内
                if (terrainPos.x < 0 || terrainPos.x > terrainSize.x || 
                    terrainPos.z < 0 || terrainPos.z > terrainSize.z)
                {
                    Debug.LogWarning($"[VegetationSystem] 位置超出地形范围: {terrainPos}, 地形大小: {terrainSize}");
                    return false;
                }
                
                // 归一化坐标 (0-1)
                float normalizedX = terrainPos.x / terrainSize.x;
                float normalizedZ = terrainPos.z / terrainSize.z;
                
                // 使用Terrain.SampleHeight获取高度
                float height = terrain.SampleHeight(worldPosition);
                terrainPosition = new Vector3(worldPosition.x, height, worldPosition.z);
                
                Debug.Log($"[VegetationSystem] 使用Terrain API获取地形高度: {terrainPosition}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VegetationSystem] 获取地形高度时出错: {e.Message}");
                return false;
            }
        }
    }
    
    /// <summary>
    /// 植被类型枚举
    /// </summary>
    public enum VegetationType
    {
        // 树木类
        针叶树, 阔叶树, 棕榈树, 果树, 枯树,
        
        // 灌木类
        普通灌木, 浆果灌木, 荆棘丛, 竹子,
        
        // 草本植物
        野草, 鲜花, 蕨类, 苔藓,
        
        // 特殊植物
        仙人掌, 蘑菇, 藤蔓, 水草
    }
    
    /// <summary>
    /// 植被数据结构
    /// </summary>
    [System.Serializable]
    public class VegetationData
    {
        public string displayName;
        public VegetationType type;
        [Header("预制件资产")]
        public List<GameObject> prefabs = new List<GameObject>(); // 支持多个预制件变体
        [Space]
        public GameObject prefab; // 保留向后兼容性
        public Texture2D icon;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
        public Color tintColor = Color.white;
        public float density = 1.0f;
        public bool canGrowOnSlope = true;
        public Vector2 heightRange = new Vector2(0, 1);
        public List<BiomeType> preferredBiomes = new List<BiomeType>();
        
        /// <summary>
        /// 获取所有可用的预制件（包括新旧格式）
        /// </summary>
        public List<GameObject> GetAllPrefabs()
        {
            var allPrefabs = new List<GameObject>();
            
            // 添加新格式的预制件列表
            if (prefabs != null)
            {
                allPrefabs.AddRange(prefabs.Where(p => p != null));
            }
            
            // 添加旧格式的单个预制件（向后兼容）
            if (prefab != null && !allPrefabs.Contains(prefab))
            {
                allPrefabs.Add(prefab);
            }
            
            return allPrefabs;
        }
    }
    
    /// <summary>
    /// 植被层配置
    /// </summary>
    [System.Serializable]
    public class VegetationLayer
    {
        public string layerName;
        public bool enabled = true;
        public float density = 1.0f;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
        public List<VegetationType> allowedTypes = new List<VegetationType>();
        public LayerMask collisionMask = -1;
    }
    
    /// <summary>
    /// 植被放置设置
    /// </summary>
    [System.Serializable]
    public class VegetationPlacementSettings
    {
        public bool enableRotationVariation = true;
        public bool enableScaleVariation = true;
        public bool enableColorVariation = false;
        public float scaleVariationAmount = 0.3f;
        public float colorVariationAmount = 0.1f;
        
        public static VegetationPlacementSettings Default => new VegetationPlacementSettings();
    }
    
    /// <summary>
    /// 植被分布参数
    /// </summary>
    [System.Serializable]
    public class VegetationDistributionParams
    {
        public float globalDensity = 1.0f;
        public bool respectBiomes = true;
        public bool respectHeight = true;
        public bool respectSlope = true;
        public bool respectTextures = true;
        public int maxVegetationPerType = 1000;
    }
    
    /// <summary>
    /// 植被统计信息
    /// </summary>
    public class VegetationStatistics
    {
        public Dictionary<VegetationType, int> vegetationCounts = new Dictionary<VegetationType, int>();
        public int totalCount = 0;
        
        public override string ToString()
        {
            string result = $"总植被数量: {totalCount}\n";
            foreach (var kvp in vegetationCounts)
            {
                result += $"{kvp.Key}: {kvp.Value}\n";
            }
            return result;
        }
    }
    
    /// <summary>
    /// 植被组件 - 附加到每个植被对象上
    /// </summary>
    public class VegetationComponent : MonoBehaviour
    {
        public VegetationType vegetationType;
        public string plantingTime;
        public float health = 1.0f;
        public bool canGrow = true;
        
        [Header("生长参数")]
        public float growthRate = 1.0f;
        public float maxSize = 1.0f;
        public bool enableSeasonalChange = false;
    }
}