using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 放置数据库 - 管理所有可放置的预制件和相关数据
    /// </summary>
    [CreateAssetMenu(fileName = "PlacementDatabase", menuName = "WorldEditor/Placement Database")]
    public class PlacementDatabase : ScriptableObject
    {
        [Header("植被预制件")]
        [SerializeField] private VegetationPrefabData[] treePrefabs;
        [SerializeField] private VegetationPrefabData[] bushPrefabs;
        [SerializeField] private VegetationPrefabData[] grassPrefabs;
        [SerializeField] private VegetationPrefabData[] flowerPrefabs;
        
        [Header("结构预制件")]
        [SerializeField] private StructurePrefabData[] buildingPrefabs;
        [SerializeField] private StructurePrefabData[] roadPrefabs;
        [SerializeField] private StructurePrefabData[] decorationPrefabs;
        
        [Header("自然要素")]
        [SerializeField] private NaturalPrefabData[] rockPrefabs;
        [SerializeField] private NaturalPrefabData[] waterPrefabs;
        [SerializeField] private NaturalPrefabData[] terrainFeaturePrefabs;
        
        [Header("数据库设置")]
        [SerializeField] private bool enableAutoSorting = true;
        [SerializeField] private bool enableValidation = true;
        
        // 私有缓存
        private Dictionary<BiomeType, List<VegetationPrefabData>> biomePrefabCache;
        private Dictionary<string, GameObject> prefabNameCache;
        private bool cacheInitialized = false;
        
        void OnEnable()
        {
            InitializeCache();
        }
        
        void InitializeCache()
        {
            if (cacheInitialized) return;
            
            biomePrefabCache = new Dictionary<BiomeType, List<VegetationPrefabData>>();
            prefabNameCache = new Dictionary<string, GameObject>();
            
            BuildBiomeCache();
            BuildNameCache();
            
            cacheInitialized = true;
        }
        
        void BuildBiomeCache()
        {
            // 为每个生物群落创建预制件列表
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                biomePrefabCache[biome] = new List<VegetationPrefabData>();
            }
            
            // 添加树木预制件
            if (treePrefabs != null)
            {
                foreach (var treePrefab in treePrefabs)
                {
                    AddToBiomeCache(treePrefab);
                }
            }
            
            // 添加灌木预制件
            if (bushPrefabs != null)
            {
                foreach (var bushPrefab in bushPrefabs)
                {
                    AddToBiomeCache(bushPrefab);
                }
            }
            
            // 添加草类预制件
            if (grassPrefabs != null)
            {
                foreach (var grassPrefab in grassPrefabs)
                {
                    AddToBiomeCache(grassPrefab);
                }
            }
            
            // 添加花卉预制件
            if (flowerPrefabs != null)
            {
                foreach (var flowerPrefab in flowerPrefabs)
                {
                    AddToBiomeCache(flowerPrefab);
                }
            }
        }
        
        void AddToBiomeCache(VegetationPrefabData prefabData)
        {
            if (prefabData == null || prefabData.suitableBiomes == null) return;
            
            foreach (var biome in prefabData.suitableBiomes)
            {
                if (biomePrefabCache.ContainsKey(biome))
                {
                    biomePrefabCache[biome].Add(prefabData);
                }
            }
        }
        
        void BuildNameCache()
        {
            // 构建名称到预制件的映射
            AddToNameCache(treePrefabs);
            AddToNameCache(bushPrefabs);
            AddToNameCache(grassPrefabs);
            AddToNameCache(flowerPrefabs);
        }
        
        void AddToNameCache<T>(T[] prefabArray) where T : IPrefabData
        {
            if (prefabArray == null) return;
            
            foreach (var prefabData in prefabArray)
            {
                if (prefabData != null && prefabData.GetPrefab() != null)
                {
                    string name = prefabData.GetPrefabName();
                    if (!string.IsNullOrEmpty(name) && !prefabNameCache.ContainsKey(name))
                    {
                        prefabNameCache[name] = prefabData.GetPrefab();
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取适合指定生物群落的植被预制件
        /// </summary>
        public List<VegetationPrefabData> GetVegetationForBiome(BiomeType biome)
        {
            InitializeCache();
            
            if (biomePrefabCache.ContainsKey(biome))
            {
                var prefabList = new List<VegetationPrefabData>(biomePrefabCache[biome]);
                
                // 如果启用自动排序，按适应性和稀有度排序
                if (enableAutoSorting)
                {
                    prefabList.Sort((a, b) => 
                    {
                        // 首先按稀有度排序（稀有度低的优先）
                        int rarityComparison = a.rarity.CompareTo(b.rarity);
                        if (rarityComparison != 0) return rarityComparison;
                        
                        // 然后按密度排序（密度高的优先）
                        return b.density.CompareTo(a.density);
                    });
                }
                
                return prefabList;
            }
            
            return new List<VegetationPrefabData>();
        }
        
        /// <summary>
        /// 根据名称获取预制件
        /// </summary>
        public GameObject GetPrefabByName(string prefabName)
        {
            InitializeCache();
            
            if (prefabNameCache.ContainsKey(prefabName))
            {
                return prefabNameCache[prefabName];
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取所有树木预制件
        /// </summary>
        public VegetationPrefabData[] GetAllTrees()
        {
            return treePrefabs ?? new VegetationPrefabData[0];
        }
        
        /// <summary>
        /// 获取所有灌木预制件
        /// </summary>
        public VegetationPrefabData[] GetAllBushes()
        {
            return bushPrefabs ?? new VegetationPrefabData[0];
        }
        
        /// <summary>
        /// 获取所有草类预制件
        /// </summary>
        public VegetationPrefabData[] GetAllGrass()
        {
            return grassPrefabs ?? new VegetationPrefabData[0];
        }
        
        /// <summary>
        /// 获取所有建筑预制件
        /// </summary>
        public StructurePrefabData[] GetAllBuildings()
        {
            return buildingPrefabs ?? new StructurePrefabData[0];
        }
        
        /// <summary>
        /// 获取所有岩石预制件
        /// </summary>
        public NaturalPrefabData[] GetAllRocks()
        {
            return rockPrefabs ?? new NaturalPrefabData[0];
        }
        
        /// <summary>
        /// 验证数据库完整性
        /// </summary>
        public bool ValidateDatabase()
        {
            if (!enableValidation) return true;
            
            bool isValid = true;
            
            // 验证植被预制件
            isValid &= ValidatePrefabArray(treePrefabs, "树木");
            isValid &= ValidatePrefabArray(bushPrefabs, "灌木");
            isValid &= ValidatePrefabArray(grassPrefabs, "草类");
            isValid &= ValidatePrefabArray(flowerPrefabs, "花卉");
            
            // 验证结构预制件
            isValid &= ValidatePrefabArray(buildingPrefabs, "建筑");
            isValid &= ValidatePrefabArray(roadPrefabs, "道路");
            isValid &= ValidatePrefabArray(decorationPrefabs, "装饰");
            
            // 验证自然要素
            isValid &= ValidatePrefabArray(rockPrefabs, "岩石");
            isValid &= ValidatePrefabArray(waterPrefabs, "水体");
            isValid &= ValidatePrefabArray(terrainFeaturePrefabs, "地形特征");
            
            return isValid;
        }
        
        bool ValidatePrefabArray<T>(T[] prefabArray, string categoryName) where T : IPrefabData
        {
            if (prefabArray == null)
            {
                Debug.LogWarning($"[PlacementDatabase] {categoryName}预制件数组为空");
                return false;
            }
            
            bool isValid = true;
            
            for (int i = 0; i < prefabArray.Length; i++)
            {
                if (prefabArray[i] == null)
                {
                    Debug.LogError($"[PlacementDatabase] {categoryName}预制件[{i}]为空");
                    isValid = false;
                    continue;
                }
                
                if (prefabArray[i].GetPrefab() == null)
                {
                    Debug.LogError($"[PlacementDatabase] {categoryName}预制件[{i}]的GameObject为空");
                    isValid = false;
                }
                
                if (string.IsNullOrEmpty(prefabArray[i].GetPrefabName()))
                {
                    Debug.LogWarning($"[PlacementDatabase] {categoryName}预制件[{i}]缺少名称");
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 重新构建缓存
        /// </summary>
        public void RebuildCache()
        {
            cacheInitialized = false;
            InitializeCache();
        }
        
        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        public string GetDatabaseStats()
        {
            int treeCount = treePrefabs?.Length ?? 0;
            int bushCount = bushPrefabs?.Length ?? 0;
            int grassCount = grassPrefabs?.Length ?? 0;
            int flowerCount = flowerPrefabs?.Length ?? 0;
            int buildingCount = buildingPrefabs?.Length ?? 0;
            int roadCount = roadPrefabs?.Length ?? 0;
            int decorationCount = decorationPrefabs?.Length ?? 0;
            int rockCount = rockPrefabs?.Length ?? 0;
            int waterCount = waterPrefabs?.Length ?? 0;
            int terrainFeatureCount = terrainFeaturePrefabs?.Length ?? 0;
            
            int totalVegetation = treeCount + bushCount + grassCount + flowerCount;
            int totalStructures = buildingCount + roadCount + decorationCount;
            int totalNatural = rockCount + waterCount + terrainFeatureCount;
            int totalPrefabs = totalVegetation + totalStructures + totalNatural;
            
            return $"放置数据库统计:\n" +
                   $"总预制件数量: {totalPrefabs}\n" +
                   $"植被预制件: {totalVegetation} (树:{treeCount}, 灌木:{bushCount}, 草:{grassCount}, 花:{flowerCount})\n" +
                   $"结构预制件: {totalStructures} (建筑:{buildingCount}, 道路:{roadCount}, 装饰:{decorationCount})\n" +
                   $"自然要素: {totalNatural} (岩石:{rockCount}, 水体:{waterCount}, 地形:{terrainFeatureCount})\n" +
                   $"缓存状态: {(cacheInitialized ? "已初始化" : "未初始化")}";
        }
    }
    
    /// <summary>
    /// 预制件数据接口
    /// </summary>
    public interface IPrefabData
    {
        GameObject GetPrefab();
        string GetPrefabName();
    }
    
    /// <summary>
    /// 植被预制件数据
    /// </summary>
    [System.Serializable]
    public class VegetationPrefabData : IPrefabData
    {
        [Header("基本信息")]
        public string prefabName;
        public GameObject prefab;
        public VegetationType vegetationType;
        
        [Header("生物群落适应性")]
        public BiomeType[] suitableBiomes;
        public float biomeFitness = 1f;
        
        [Header("环境需求")]
        public Vector2 temperatureRange = new Vector2(0f, 40f);
        public Vector2 humidityRange = new Vector2(0.2f, 0.8f);
        public Vector2 altitudeRange = new Vector2(0f, 1000f);
        public Vector2 slopeRange = new Vector2(0f, 30f);
        
        [Header("生长特性")]
        public float growthRate = 1f;
        public float maxAge = 100f;
        public float reproductionRate = 0.1f;
        public Vector2 sizeRange = new Vector2(0.8f, 1.2f);
        
        [Header("放置特性")]
        public float rarity = 1f;     // 稀有度（数值越高越稀有）
        public float density = 0.5f;  // 密度偏好（数值越高密度越大）
        
        [Header("生态角色")]
        public EcosystemRole ecosystemRole = EcosystemRole.Producer;
        public string[] competingSpecies;
        public string[] symbioticSpecies;
        
        public GameObject GetPrefab() => prefab;
        public string GetPrefabName() => prefabName;
    }
    
    /// <summary>
    /// 结构预制件数据
    /// </summary>
    [System.Serializable]
    public class StructurePrefabData : IPrefabData
    {
        [Header("基本信息")]
        public string prefabName;
        public GameObject prefab;
        public StructureType structureType;
        
        [Header("放置需求")]
        public float minFlatness = 0.8f; // 需要的平坦度
        public float minAccessibility = 0.5f; // 可达性要求
        public bool requiresRoadAccess = false;
        public bool requiresWaterAccess = false;
        
        [Header("文明等级")]
        public CivilizationLevel requiredCivilizationLevel = CivilizationLevel.Tribal;
        public ArchitecturalStyle architecturalStyle = ArchitecturalStyle.Medieval;
        
        [Header("尺寸和影响")]
        public Vector3 size = Vector3.one;
        public float influenceRadius = 10f;
        public bool blocksVegetation = true;
        
        public GameObject GetPrefab() => prefab;
        public string GetPrefabName() => prefabName;
    }
    
    /// <summary>
    /// 自然要素预制件数据
    /// </summary>
    [System.Serializable]
    public class NaturalPrefabData : IPrefabData
    {
        [Header("基本信息")]
        public string prefabName;
        public GameObject prefab;
        public NaturalFeatureType featureType;
        
        [Header("地质需求")]
        public GeologyType[] suitableGeology;
        public Vector2 slopePreference = new Vector2(0f, 90f);
        public bool requiresExposedRock = false;
        
        [Header("尺寸变化")]
        public Vector2 scaleRange = new Vector2(0.5f, 2f);
        public bool allowRandomRotation = true;
        public bool allowRandomTilt = false;
        
        public GameObject GetPrefab() => prefab;
        public string GetPrefabName() => prefabName;
    }
    
    // 枚举定义
    public enum NaturalFeatureType
    {
        Rock,
        Boulder,
        Cliff,
        Cave,
        WaterSource,
        Mineral,
        Fossil
    }
    
    public enum GeologyType
    {
        Sedimentary,
        Igneous,
        Metamorphic,
        Volcanic,
        Limestone,
        Granite,
        Sandstone
    }
}