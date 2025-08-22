using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 预制件植被系统
    /// 为所有17种植被类型创建高质量预制件并智能管理
    /// </summary>
    public class PrefabVegetationSystem : MonoBehaviour
    {
        [Header("预制件集合")]
        public VegetationPrefabCollection prefabCollection;
        
        [Header("生成设置")]
        public bool generatePrefabsOnStart = true;
        public bool enableVariantSystem = true;
        public bool enableSeasonalVariants = true;
        
        [Header("质量设置")]
        [Range(1, 5)] public int variantsPerType = 3; // 每种植被的变体数量
        [Range(512, 2048)] public int textureResolution = 1024;
        [Range(100, 1000)] public int meshComplexity = 300;
        
        private Dictionary<VegetationType, List<GameObject>> generatedPrefabs;
        private VegetationLibrary vegetationLibrary;
        
        void Start()
        {
            if (generatePrefabsOnStart)
            {
                InitializePrefabSystem();
            }
        }
        
        /// <summary>
        /// 初始化预制件系统
        /// </summary>
        public void InitializePrefabSystem()
        {
            Debug.Log("[PrefabVegetationSystem] 开始为所有17种植被生成高质量预制件...");
            
            generatedPrefabs = new Dictionary<VegetationType, List<GameObject>>();
            vegetationLibrary = ScriptableObject.CreateInstance<VegetationLibrary>();
            vegetationLibrary.InitializeDefaultVegetation();
            
            // 为每种植被类型生成预制件
            var allTypes = System.Enum.GetValues(typeof(VegetationType)).Cast<VegetationType>();
            
            foreach (var vegType in allTypes)
            {
                GeneratePrefabsForType(vegType);
            }
            
            Debug.Log($"[PrefabVegetationSystem] 预制件生成完成！总计: {generatedPrefabs.Values.Sum(list => list.Count)} 个预制件");
        }
        
        /// <summary>
        /// 为指定植被类型生成多个预制件变体
        /// </summary>
        void GeneratePrefabsForType(VegetationType vegType)
        {
            Debug.Log($"[PrefabVegetationSystem] 生成 {vegType} 的预制件变体...");
            
            var variants = new List<GameObject>();
            
            for (int i = 0; i < variantsPerType; i++)
            {
                GameObject prefab = GenerateHighQualityPrefab(vegType, i);
                if (prefab != null)
                {
                    variants.Add(prefab);
                    prefab.name = $"{vegType}_Variant{i:00}";
                }
            }
            
            // 如果启用季节变体，添加季节版本
            if (enableSeasonalVariants && IsTreeOrBushType(vegType))
            {
                variants.AddRange(GenerateSeasonalVariants(vegType));
            }
            
            generatedPrefabs[vegType] = variants;
            Debug.Log($"[PrefabVegetationSystem] {vegType} 完成，生成了 {variants.Count} 个变体");
        }
        
        /// <summary>
        /// 生成高质量预制件
        /// </summary>
        GameObject GenerateHighQualityPrefab(VegetationType vegType, int variantIndex)
        {
            switch (GetPlantCategory(vegType))
            {
                case PlantCategory.Tree:
                    return GenerateTreePrefab(vegType, variantIndex);
                
                case PlantCategory.Bush:
                    return GenerateBushPrefab(vegType, variantIndex);
                
                case PlantCategory.Grass:
                    return GenerateGrassPrefab(vegType, variantIndex);
                
                case PlantCategory.Special:
                    return GenerateSpecialPrefab(vegType, variantIndex);
                
                default:
                    return GenerateFallbackPrefab(vegType, variantIndex);
            }
        }
        
        /// <summary>
        /// 生成树木类预制件
        /// </summary>
        GameObject GenerateTreePrefab(VegetationType vegType, int variantIndex)
        {
            GameObject tree = new GameObject($"{vegType}_Tree");
            
            // 根据变体索引调整参数
            TreeGenerationParams treeParams = GetTreeParams(vegType, variantIndex);
            
            switch (vegType)
            {
                case VegetationType.针叶树:
                    CreateAdvancedConiferTree(tree, treeParams);
                    break;
                    
                case VegetationType.阔叶树:
                    CreateAdvancedDeciduousTree(tree, treeParams);
                    break;
                    
                case VegetationType.棕榈树:
                    CreateAdvancedPalmTree(tree, treeParams);
                    break;
                    
                case VegetationType.果树:
                    CreateAdvancedFruitTree(tree, treeParams);
                    break;
                    
                case VegetationType.枯树:
                    CreateAdvancedDeadTree(tree, treeParams);
                    break;
            }
            
            // 添加通用树木组件
            AddTreeComponents(tree, vegType, treeParams);
            
            return tree;
        }
        
        /// <summary>
        /// 创建高级针叶树（基于我们改进的算法）
        /// </summary>
        void CreateAdvancedConiferTree(GameObject tree, TreeGenerationParams treeParams)
        {
            // 使用改进的挪威云杉算法
            var generator = new AdvancedConiferGenerator();
            generator.GenerateRealisticConifer(tree, treeParams);
        }
        
        /// <summary>
        /// 创建高级阔叶树
        /// </summary>
        void CreateAdvancedDeciduousTree(GameObject tree, TreeGenerationParams treeParams)
        {
            var generator = new AdvancedDeciduousGenerator();
            generator.GenerateRealisticDeciduous(tree, treeParams);
        }
        
        /// <summary>
        /// 创建高级棕榈树
        /// </summary>
        void CreateAdvancedPalmTree(GameObject tree, TreeGenerationParams treeParams)
        {
            var generator = new AdvancedPalmGenerator();
            generator.GenerateRealisticPalm(tree, treeParams);
        }
        
        /// <summary>
        /// 创建高级果树
        /// </summary>
        void CreateAdvancedFruitTree(GameObject tree, TreeGenerationParams treeParams)
        {
            var generator = new AdvancedFruitTreeGenerator();
            generator.GenerateRealisticFruitTree(tree, treeParams);
        }
        
        /// <summary>
        /// 创建高级枯树
        /// </summary>
        void CreateAdvancedDeadTree(GameObject tree, TreeGenerationParams treeParams)
        {
            var generator = new AdvancedDeadTreeGenerator();
            generator.GenerateRealisticDeadTree(tree, treeParams);
        }
        
        /// <summary>
        /// 生成灌木类预制件
        /// </summary>
        GameObject GenerateBushPrefab(VegetationType vegType, int variantIndex)
        {
            GameObject bush = new GameObject($"{vegType}_Bush");
            
            BushGenerationParams bushParams = GetBushParams(vegType, variantIndex);
            
            switch (vegType)
            {
                case VegetationType.普通灌木:
                    CreateAdvancedCommonBush(bush, bushParams);
                    break;
                    
                case VegetationType.浆果灌木:
                    CreateAdvancedBerryBush(bush, bushParams);
                    break;
                    
                case VegetationType.荆棘丛:
                    CreateAdvancedThornBush(bush, bushParams);
                    break;
                    
                case VegetationType.竹子:
                    CreateAdvancedBamboo(bush, bushParams);
                    break;
            }
            
            AddBushComponents(bush, vegType, bushParams);
            
            return bush;
        }
        
        /// <summary>
        /// 生成草本植物预制件
        /// </summary>
        GameObject GenerateGrassPrefab(VegetationType vegType, int variantIndex)
        {
            GameObject grass = new GameObject($"{vegType}_Grass");
            
            GrassGenerationParams grassParams = GetGrassParams(vegType, variantIndex);
            
            switch (vegType)
            {
                case VegetationType.野草:
                    CreateAdvancedWildGrass(grass, grassParams);
                    break;
                    
                case VegetationType.鲜花:
                    CreateAdvancedFlowers(grass, grassParams);
                    break;
                    
                case VegetationType.蕨类:
                    CreateAdvancedFerns(grass, grassParams);
                    break;
                    
                case VegetationType.苔藓:
                    CreateAdvancedMoss(grass, grassParams);
                    break;
            }
            
            AddGrassComponents(grass, vegType, grassParams);
            
            return grass;
        }
        
        /// <summary>
        /// 生成特殊植物预制件
        /// </summary>
        GameObject GenerateSpecialPrefab(VegetationType vegType, int variantIndex)
        {
            GameObject special = new GameObject($"{vegType}_Special");
            
            SpecialPlantParams specialParams = GetSpecialParams(vegType, variantIndex);
            
            switch (vegType)
            {
                case VegetationType.仙人掌:
                    CreateAdvancedCactus(special, specialParams);
                    break;
                    
                case VegetationType.蘑菇:
                    CreateAdvancedMushroom(special, specialParams);
                    break;
                    
                case VegetationType.藤蔓:
                    CreateAdvancedVine(special, specialParams);
                    break;
                    
                case VegetationType.水草:
                    CreateAdvancedAquaticPlant(special, specialParams);
                    break;
            }
            
            AddSpecialComponents(special, vegType, specialParams);
            
            return special;
        }
        
        /// <summary>
        /// 生成季节变体
        /// </summary>
        List<GameObject> GenerateSeasonalVariants(VegetationType vegType)
        {
            var seasonalVariants = new List<GameObject>();
            
            var seasons = new[] { "Spring", "Summer", "Autumn", "Winter" };
            
            foreach (var season in seasons)
            {
                GameObject seasonalPrefab = GenerateSeasonalVariant(vegType, season);
                if (seasonalPrefab != null)
                {
                    seasonalPrefab.name = $"{vegType}_{season}";
                    seasonalVariants.Add(seasonalPrefab);
                }
            }
            
            return seasonalVariants;
        }
        
        /// <summary>
        /// 智能选择最适合的预制件
        /// </summary>
        public GameObject SelectBestPrefab(VegetationType vegType, Vector3 position, UnityEngine.TerrainData terrainData = null)
        {
            if (!generatedPrefabs.ContainsKey(vegType) || generatedPrefabs[vegType].Count == 0)
            {
                Debug.LogWarning($"[PrefabVegetationSystem] 没有找到 {vegType} 的预制件！");
                return null;
            }
            
            var availablePrefabs = generatedPrefabs[vegType];
            
            // 智能选择逻辑
            GameObject selectedPrefab = SelectBasedOnEnvironment(availablePrefabs, position, terrainData);
            
            return selectedPrefab;
        }
        
        /// <summary>
        /// 基于环境条件选择预制件
        /// </summary>
        GameObject SelectBasedOnEnvironment(List<GameObject> prefabs, Vector3 position, UnityEngine.TerrainData terrainData)
        {
            // 基础随机选择
            int baseIndex = Random.Range(0, Mathf.Min(variantsPerType, prefabs.Count));
            
            // 如果有地形数据，进行智能选择
            if (terrainData != null)
            {
                // 简化版本的地形分析
                float normalizedX = position.x / terrainData.size.x;
                float normalizedZ = position.z / terrainData.size.z;
                
                if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1)
                {
                    float height = terrainData.GetHeight(
                        Mathf.RoundToInt(normalizedX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normalizedZ * terrainData.heightmapResolution));
                    
                    // 根据高度选择年龄变体
                    if (height > 100f) // 高海拔选择较老的变体
                        baseIndex = Mathf.Min(variantsPerType - 1, prefabs.Count - 1);
                    else if (height < 50f) // 低海拔选择年轻变体
                        baseIndex = 0;
                }
            }
            
            // 季节变体选择（如果启用）
            if (enableSeasonalVariants && prefabs.Count > variantsPerType)
            {
                // 这里可以根据实际的季节系统选择
                // 暂时随机选择季节变体
                int seasonalOffset = Random.Range(0, 2) == 0 ? 0 : variantsPerType;
                baseIndex = Mathf.Min(baseIndex + seasonalOffset, prefabs.Count - 1);
            }
            
            return prefabs[baseIndex];
        }
        
        /// <summary>
        /// 获取植物类别
        /// </summary>
        PlantCategory GetPlantCategory(VegetationType vegType)
        {
            switch (vegType)
            {
                case VegetationType.针叶树:
                case VegetationType.阔叶树:
                case VegetationType.棕榈树:
                case VegetationType.果树:
                case VegetationType.枯树:
                    return PlantCategory.Tree;
                    
                case VegetationType.普通灌木:
                case VegetationType.浆果灌木:
                case VegetationType.荆棘丛:
                case VegetationType.竹子:
                    return PlantCategory.Bush;
                    
                case VegetationType.野草:
                case VegetationType.鲜花:
                case VegetationType.蕨类:
                case VegetationType.苔藓:
                    return PlantCategory.Grass;
                    
                case VegetationType.仙人掌:
                case VegetationType.蘑菇:
                case VegetationType.藤蔓:
                case VegetationType.水草:
                    return PlantCategory.Special;
                    
                default:
                    return PlantCategory.Tree;
            }
        }
        
        bool IsTreeOrBushType(VegetationType vegType)
        {
            var category = GetPlantCategory(vegType);
            return category == PlantCategory.Tree || category == PlantCategory.Bush;
        }
        
        #region 参数生成方法
        
        TreeGenerationParams GetTreeParams(VegetationType vegType, int variantIndex)
        {
            var baseParams = new TreeGenerationParams();
            
            // 根据植被类型设置基础参数
            switch (vegType)
            {
                case VegetationType.针叶树:
                    baseParams.height = Random.Range(12f, 25f);
                    baseParams.trunkRadius = Random.Range(0.3f, 0.8f);
                    baseParams.branchLayers = Random.Range(6, 12);
                    baseParams.foliageColor = new Color(0.08f, 0.35f, 0.12f);
                    break;
                    
                case VegetationType.阔叶树:
                    baseParams.height = Random.Range(15f, 30f);
                    baseParams.trunkRadius = Random.Range(0.4f, 1.2f);
                    baseParams.branchLayers = Random.Range(8, 15);
                    baseParams.foliageColor = new Color(0.2f, 0.6f, 0.2f);
                    break;
                    
                case VegetationType.棕榈树:
                    baseParams.height = Random.Range(8f, 20f);
                    baseParams.trunkRadius = Random.Range(0.2f, 0.5f);
                    baseParams.branchLayers = Random.Range(5, 8);
                    baseParams.foliageColor = new Color(0.3f, 0.7f, 0.3f);
                    break;
                    
                case VegetationType.果树:
                    baseParams.height = Random.Range(8f, 15f);
                    baseParams.trunkRadius = Random.Range(0.3f, 0.7f);
                    baseParams.branchLayers = Random.Range(6, 10);
                    baseParams.foliageColor = new Color(0.4f, 0.6f, 0.3f);
                    break;
                    
                case VegetationType.枯树:
                    baseParams.height = Random.Range(10f, 18f);
                    baseParams.trunkRadius = Random.Range(0.25f, 0.6f);
                    baseParams.branchLayers = Random.Range(4, 8);
                    baseParams.hasLeaves = false;
                    baseParams.foliageColor = new Color(0.3f, 0.2f, 0.1f);
                    break;
            }
            
            // 根据变体索引调整年龄和特征
            baseParams.ageVariation = (float)variantIndex / (variantsPerType - 1);
            
            // 年龄影响
            if (baseParams.ageVariation > 0.7f) // 老树
            {
                baseParams.height *= Random.Range(1.2f, 1.5f);
                baseParams.trunkRadius *= Random.Range(1.3f, 1.8f);
                baseParams.branchLayers = Mathf.RoundToInt(baseParams.branchLayers * 1.4f);
            }
            else if (baseParams.ageVariation < 0.3f) // 幼树
            {
                baseParams.height *= Random.Range(0.6f, 0.8f);
                baseParams.trunkRadius *= Random.Range(0.7f, 0.9f);
                baseParams.branchLayers = Mathf.RoundToInt(baseParams.branchLayers * 0.7f);
            }
            
            return baseParams;
        }

        BushGenerationParams GetBushParams(VegetationType vegType, int variantIndex)
        {
            var baseParams = new BushGenerationParams();
            
            // 根据植被类型设置基础参数
            switch (vegType)
            {
                case VegetationType.普通灌木:
                    baseParams.height = Random.Range(1.5f, 3f);
                    baseParams.width = Random.Range(1.2f, 2.5f);
                    baseParams.branchCount = Random.Range(8, 16);
                    baseParams.foliageColor = new Color(0.3f, 0.5f, 0.2f);
                    break;
                    
                case VegetationType.浆果灌木:
                    baseParams.height = Random.Range(1f, 2f);
                    baseParams.width = Random.Range(1f, 1.8f);
                    baseParams.branchCount = Random.Range(6, 12);
                    baseParams.hasBerries = true;
                    baseParams.foliageColor = new Color(0.4f, 0.5f, 0.2f);
                    break;
                    
                case VegetationType.荆棘丛:
                    baseParams.height = Random.Range(1.2f, 2.5f);
                    baseParams.width = Random.Range(1.5f, 2.2f);
                    baseParams.branchCount = Random.Range(12, 20);
                    baseParams.foliageColor = new Color(0.2f, 0.4f, 0.1f);
                    break;
                    
                case VegetationType.竹子:
                    baseParams.height = Random.Range(3f, 6f);
                    baseParams.width = Random.Range(0.8f, 1.5f);
                    baseParams.branchCount = Random.Range(5, 12);
                    baseParams.foliageColor = new Color(0.5f, 0.7f, 0.3f);
                    break;
            }
            
            // 根据变体索引调整特征
            float ageVariation = (float)variantIndex / (variantsPerType - 1);
            
            // 年龄影响灌木特征
            if (ageVariation > 0.7f) // 成熟灌木
            {
                baseParams.height *= Random.Range(1.3f, 1.6f);
                baseParams.width *= Random.Range(1.4f, 1.8f);
                baseParams.branchCount = Mathf.RoundToInt(baseParams.branchCount * 1.5f);
            }
            else if (ageVariation < 0.3f) // 幼小灌木
            {
                baseParams.height *= Random.Range(0.5f, 0.7f);
                baseParams.width *= Random.Range(0.6f, 0.8f);
                baseParams.branchCount = Mathf.RoundToInt(baseParams.branchCount * 0.7f);
            }
            
            return baseParams;
        }

        GrassGenerationParams GetGrassParams(VegetationType vegType, int variantIndex)
        {
            var baseParams = new GrassGenerationParams();
            
            // 根据植被类型设置基础参数
            switch (vegType)
            {
                case VegetationType.野草:
                    baseParams.height = Random.Range(0.3f, 0.8f);
                    baseParams.grassCount = Random.Range(15, 30);
                    baseParams.density = Random.Range(0.8f, 1.2f);
                    baseParams.grassColor = new Color(0.4f, 0.7f, 0.3f);
                    break;
                    
                case VegetationType.鲜花:
                    baseParams.height = Random.Range(0.2f, 0.5f);
                    baseParams.grassCount = Random.Range(8, 20);
                    baseParams.density = Random.Range(0.6f, 1.0f);
                    baseParams.hasFlowers = true;
                    baseParams.grassColor = new Color(0.5f, 0.8f, 0.4f);
                    break;
                    
                case VegetationType.蕨类:
                    baseParams.height = Random.Range(0.4f, 1.0f);
                    baseParams.grassCount = Random.Range(5, 12);
                    baseParams.density = Random.Range(0.7f, 1.1f);
                    baseParams.grassColor = new Color(0.2f, 0.6f, 0.2f);
                    break;
                    
                case VegetationType.苔藓:
                    baseParams.height = Random.Range(0.02f, 0.1f);
                    baseParams.grassCount = Random.Range(1, 3);
                    baseParams.density = Random.Range(1.0f, 1.5f);
                    baseParams.grassColor = new Color(0.3f, 0.5f, 0.1f);
                    break;
            }
            
            // 根据变体索引调整特征
            float variation = (float)variantIndex / (variantsPerType - 1);
            
            // 变体影响
            if (variation > 0.7f) // 茂盛变体
            {
                baseParams.height *= Random.Range(1.2f, 1.5f);
                baseParams.grassCount = Mathf.RoundToInt(baseParams.grassCount * 1.4f);
                baseParams.density *= Random.Range(1.2f, 1.6f);
            }
            else if (variation < 0.3f) // 稀疏变体
            {
                baseParams.height *= Random.Range(0.6f, 0.8f);
                baseParams.grassCount = Mathf.RoundToInt(baseParams.grassCount * 0.7f);
                baseParams.density *= Random.Range(0.6f, 0.8f);
            }
            
            return baseParams;
        }

        SpecialPlantParams GetSpecialParams(VegetationType vegType, int variantIndex)
        {
            var baseParams = new SpecialPlantParams();
            
            // 根据植被类型设置基础参数
            switch (vegType)
            {
                case VegetationType.仙人掌:
                    baseParams.height = Random.Range(0.8f, 2.5f);
                    baseParams.width = Random.Range(0.15f, 0.3f);
                    baseParams.segmentCount = Random.Range(3, 8);
                    baseParams.hasSpecialFeatures = Random.Range(0f, 1f) > 0.3f; // 70%概率有刺
                    baseParams.primaryColor = new Color(0.2f, 0.4f, 0.2f);
                    break;
                    
                case VegetationType.蘑菇:
                    baseParams.height = Random.Range(0.08f, 0.4f);
                    baseParams.width = Random.Range(0.1f, 0.3f);
                    baseParams.segmentCount = Random.Range(1, 3);
                    baseParams.hasSpecialFeatures = Random.Range(0f, 1f) > 0.5f; // 50%概率有斑点
                    baseParams.primaryColor = new Color(0.6f, 0.3f, 0.2f);
                    break;
                    
                case VegetationType.藤蔓:
                    baseParams.height = Random.Range(2f, 5f);
                    baseParams.width = Random.Range(0.5f, 1.2f);
                    baseParams.segmentCount = Random.Range(8, 15);
                    baseParams.hasSpecialFeatures = true; // 总是有叶子
                    baseParams.primaryColor = new Color(0.3f, 0.5f, 0.2f);
                    break;
                    
                case VegetationType.水草:
                    baseParams.height = Random.Range(0.2f, 0.8f);
                    baseParams.width = Random.Range(0.1f, 0.4f);
                    baseParams.segmentCount = Random.Range(5, 12);
                    baseParams.hasSpecialFeatures = false;
                    baseParams.primaryColor = new Color(0.2f, 0.5f, 0.3f);
                    break;
            }
            
            // 根据变体索引调整特征
            float variation = (float)variantIndex / (variantsPerType - 1);
            
            // 变体影响
            if (variation > 0.7f) // 大型变体
            {
                baseParams.height *= Random.Range(1.3f, 1.8f);
                baseParams.width *= Random.Range(1.2f, 1.5f);
                baseParams.segmentCount = Mathf.RoundToInt(baseParams.segmentCount * 1.3f);
            }
            else if (variation < 0.3f) // 小型变体
            {
                baseParams.height *= Random.Range(0.5f, 0.7f);
                baseParams.width *= Random.Range(0.6f, 0.8f);
                baseParams.segmentCount = Mathf.RoundToInt(baseParams.segmentCount * 0.7f);
            }
            
            return baseParams;
        }
        
        #endregion
        
        #region 生成器调用方法
        
        void CreateAdvancedCommonBush(GameObject bush, BushGenerationParams bushParams) 
        {
            BasicVegetationGenerators.CreateAdvancedCommonBush(bush, bushParams);
        }
        
        void CreateAdvancedBerryBush(GameObject bush, BushGenerationParams bushParams) 
        {
            BasicVegetationGenerators.CreateAdvancedBerryBush(bush, bushParams);
        }
        
        void CreateAdvancedThornBush(GameObject bush, BushGenerationParams bushParams) 
        {
            BasicVegetationGenerators.CreateAdvancedThornBush(bush, bushParams);
        }
        
        void CreateAdvancedBamboo(GameObject bush, BushGenerationParams bushParams) 
        {
            BasicVegetationGenerators.CreateAdvancedBamboo(bush, bushParams);
        }
        
        void CreateAdvancedWildGrass(GameObject grass, GrassGenerationParams grassParams) 
        {
            BasicVegetationGenerators.CreateAdvancedWildGrass(grass, grassParams);
        }
        
        void CreateAdvancedFlowers(GameObject grass, GrassGenerationParams grassParams) 
        {
            BasicVegetationGenerators.CreateAdvancedFlowers(grass, grassParams);
        }
        
        void CreateAdvancedFerns(GameObject grass, GrassGenerationParams grassParams) 
        {
            BasicVegetationGenerators.CreateAdvancedFerns(grass, grassParams);
        }
        
        void CreateAdvancedMoss(GameObject grass, GrassGenerationParams grassParams) 
        {
            BasicVegetationGenerators.CreateAdvancedMoss(grass, grassParams);
        }
        
        void CreateAdvancedCactus(GameObject special, SpecialPlantParams specialParams) 
        {
            SpecialVegetationGenerators.CreateAdvancedCactus(special, specialParams);
        }
        
        void CreateAdvancedMushroom(GameObject special, SpecialPlantParams specialParams) 
        {
            SpecialVegetationGenerators.CreateAdvancedMushroom(special, specialParams);
        }
        
        void CreateAdvancedVine(GameObject special, SpecialPlantParams specialParams) 
        {
            SpecialVegetationGenerators.CreateAdvancedVine(special, specialParams);
        }
        
        void CreateAdvancedAquaticPlant(GameObject special, SpecialPlantParams specialParams) 
        {
            SpecialVegetationGenerators.CreateAdvancedAquaticPlant(special, specialParams);
        }
        
        void AddTreeComponents(GameObject tree, VegetationType vegType, TreeGenerationParams treeParams) { }
        void AddBushComponents(GameObject bush, VegetationType vegType, BushGenerationParams bushParams) { }
        void AddGrassComponents(GameObject grass, VegetationType vegType, GrassGenerationParams grassParams) { }
        void AddSpecialComponents(GameObject special, VegetationType vegType, SpecialPlantParams specialParams) { }
        
        GameObject GenerateSeasonalVariant(VegetationType vegType, string season) => null;
        GameObject GenerateFallbackPrefab(VegetationType vegType, int variantIndex) => null;
        
        #endregion
    }
    
    // 数据结构
    public enum PlantCategory
    {
        Tree, Bush, Grass, Special
    }
    
    [System.Serializable]
    public class VegetationPrefabCollection
    {
        public List<GameObject> treeVariants = new List<GameObject>();
        public List<GameObject> bushVariants = new List<GameObject>();
        public List<GameObject> grassVariants = new List<GameObject>();
        public List<GameObject> specialVariants = new List<GameObject>();
    }
    
    // 生成参数
    [System.Serializable]
    public class TreeGenerationParams
    {
        public float height = 15f;
        public float trunkRadius = 0.5f;
        public int branchLayers = 8;
        public float ageVariation = 0f; // 0=年轻, 1=老年
        public bool hasLeaves = true;
        public Color foliageColor = Color.green;
    }
    
    [System.Serializable]
    public class BushGenerationParams
    {
        public float height = 2f;
        public float width = 1.5f;
        public int branchCount = 12;
        public bool hasBerries = false;
        public bool hasFlowers = false;
        public Color foliageColor = Color.green;
    }
    
    [System.Serializable]
    public class GrassGenerationParams
    {
        public float height = 0.5f;
        public int grassCount = 20;
        public float density = 1f;
        public bool hasFlowers = false;
        public Color grassColor = Color.green;
    }
    
    [System.Serializable]
    public class SpecialPlantParams
    {
        public float height = 1f;
        public float width = 1f;
        public int segmentCount = 5;
        public bool hasSpecialFeatures = false;
        public Color primaryColor = Color.green;
    }
}