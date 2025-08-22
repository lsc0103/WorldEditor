using UnityEngine;

namespace WorldEditor.Core
{
    /// <summary>
    /// 世界生成参数 - 定义单次生成操作的所有参数
    /// </summary>
    [System.Serializable]
    public class WorldGenerationParameters
    {
        [Header("生成范围")]
        public Vector3 centerPosition = Vector3.zero;
        public Vector2 areaSize = new Vector2(1000f, 1000f);
        public Bounds generationBounds;
        
        [Header("生成选项")]
        public bool generateTerrain = true;
        public bool generateVegetation = true;
        public bool generateStructures = true;
        public bool generateEnvironment = true;
        public bool generateWater = true;
        
        [Header("地形参数")]
        public TerrainGenerationParams terrainParams = new TerrainGenerationParams();
        
        [Header("植被参数")]
        public VegetationGenerationParams vegetationParams = new VegetationGenerationParams();
        
        [Header("结构参数")]
        public StructureGenerationParams structureParams = new StructureGenerationParams();
        
        [Header("环境参数")]
        public EnvironmentGenerationParams environmentParams = new EnvironmentGenerationParams();
        
        [Header("AI参数")]
        public AIGenerationParams aiParams = new AIGenerationParams();
        
        [Header("质量设置")]
        public GenerationQuality quality = GenerationQuality.High;
        public bool enableRealTimePreview = true;
        public bool enableProgressTracking = true;
        
        public WorldGenerationParameters()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            generationBounds = new Bounds(centerPosition, new Vector3(areaSize.x, 1000f, areaSize.y));
            terrainParams.Initialize();
            vegetationParams.Initialize();
            structureParams.Initialize();
            environmentParams.Initialize();
            aiParams.Initialize();
        }
        
        public void UpdateBounds()
        {
            generationBounds = new Bounds(centerPosition, new Vector3(areaSize.x, 1000f, areaSize.y));
        }
    }
    
    [System.Serializable]
    public class TerrainGenerationParams
    {
        [Header("地形类型")]
        public TerrainType terrainType = TerrainType.Procedural;
        public BiomeType biome = BiomeType.Temperate;
        
        [Header("高度参数")]
        public float baseHeight = 0f;
        public float heightVariation = 100f;
        public AnimationCurve heightFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Header("噪声设置")]
        public NoiseLayerSettings[] noiseLayers = new NoiseLayerSettings[3];
        
        [Header("特殊地形特征")]
        public bool generateRivers = true;
        public bool generateCliffs = true;
        public bool generateCaves = false;
        public bool generateVolcanoes = false;
        
        [Header("地质设置")]
        public bool enableGeologicalLayers = true;
        public GeologySettings geology = new GeologySettings();
        
        public void Initialize()
        {
            if (noiseLayers == null || noiseLayers.Length == 0)
            {
                noiseLayers = new NoiseLayerSettings[3];
                noiseLayers[0] = new NoiseLayerSettings { weight = 1f, frequency = 0.01f, amplitude = 1f };
                noiseLayers[1] = new NoiseLayerSettings { weight = 0.5f, frequency = 0.05f, amplitude = 0.5f };
                noiseLayers[2] = new NoiseLayerSettings { weight = 0.25f, frequency = 0.1f, amplitude = 0.25f };
            }
            
            if (heightFalloff == null)
            {
                heightFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }
            
            geology.Initialize();
        }
    }
    
    [System.Serializable]
    public class VegetationGenerationParams
    {
        [Header("植被密度")]
        public float overallDensity = 0.7f;
        public float treeDensity = 0.3f;
        public float grassDensity = 0.8f;
        public float bushDensity = 0.5f;
        
        [Header("分布规则")]
        public VegetationDistributionRule[] distributionRules;
        
        [Header("生态系统")]
        public bool enableEcosystemSimulation = true;
        public float biodiversityLevel = 0.6f;
        public bool enableSpeciesInteraction = true;
        
        [Header("季节变化")]
        public bool enableSeasonalVariation = true;
        public Season currentSeason = Season.Spring;
        
        public void Initialize()
        {
            if (distributionRules == null)
            {
                distributionRules = new VegetationDistributionRule[0];
            }
        }
    }
    
    [System.Serializable]
    public class StructureGenerationParams
    {
        [Header("结构类型")]
        public StructureType[] enabledStructures;
        
        [Header("建筑风格")]
        public ArchitecturalStyle architecturalStyle = ArchitecturalStyle.Medieval;
        public CivilizationLevel civilizationLevel = CivilizationLevel.Tribal;
        
        [Header("分布参数")]
        public float structureDensity = 0.1f;
        public float settlementSize = 1f;
        public bool generateRoads = true;
        public bool generateBridges = true;
        
        [Header("AI驱动建筑")]
        public bool enableAIArchitecture = true;
        public bool generateInteriors = false;
        
        public void Initialize()
        {
            if (enabledStructures == null)
            {
                enabledStructures = new StructureType[] { StructureType.Houses, StructureType.Roads };
            }
        }
    }
    
    [System.Serializable]
    public class EnvironmentGenerationParams
    {
        [Header("天气设置")]
        public WeatherType weather = WeatherType.Clear;
        public float temperature = 20f;
        public float humidity = 0.5f;
        public float windStrength = 0.3f;
        
        [Header("光照设置")]
        public TimeOfDay timeOfDay = TimeOfDay.Noon;
        public float sunIntensity = 1f;
        public Color sunColor = Color.white;
        public bool enableVolumetricLighting = true;
        
        [Header("大气效果")]
        public bool enableFog = true;
        public bool enableClouds = true;
        public bool enableAtmosphericScattering = true;
        
        [Header("音频环境")]
        public bool generateAmbientSounds = true;
        public AmbientSoundProfile soundProfile = AmbientSoundProfile.Forest;
        
        public void Initialize()
        {
            // 环境参数初始化
        }
    }
    
    [System.Serializable]
    public class AIGenerationParams
    {
        [Header("AI生成选项")]
        public bool useAIForTerrain = true;
        public bool useAIForVegetation = true;
        public bool useAIForStructures = true;
        
        [Header("学习模式")]
        public AILearningMode learningMode = AILearningMode.Adaptive;
        public string referenceStyle = "realistic";
        
        [Header("创意参数")]
        public float creativityLevel = 0.5f;
        public float consistencyLevel = 0.8f;
        public bool enableExperimentation = false;
        
        public void Initialize()
        {
            // AI参数初始化
        }
    }
    
    // 支持类和结构
    [System.Serializable]
    public class NoiseLayerSettings
    {
        public NoiseType noiseType = NoiseType.Perlin;
        public float weight = 1f;
        public float frequency = 0.01f;
        public float amplitude = 1f;
        public Vector2 offset = Vector2.zero;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2f;
    }
    
    [System.Serializable]
    public class GeologySettings
    {
        public bool enableSedimentLayers = true;
        public bool enableMineralDeposits = true;
        public float rockHardness = 0.7f;
        public float erosionResistance = 0.5f;
        public int erosionIterations = 10;
        public float erosionStrength = 0.3f;
        
        public void Initialize()
        {
            // 地质设置初始化
        }
    }
    
    [System.Serializable]
    public class VegetationDistributionRule
    {
        public string speciesName;
        public float altitudeMin = 0f;
        public float altitudeMax = 1000f;
        public float slopeMin = 0f;
        public float slopeMax = 90f;
        public float moistureMin = 0f;
        public float moistureMax = 1f;
        public float temperatureMin = -50f;
        public float temperatureMax = 50f;
        public float density = 1f;
    }
    
    // 枚举定义
    public enum TerrainType
    {
        Procedural,
        HeightmapBased,
        VoxelBased,
        HybridMesh
    }
    
    public enum StructureType
    {
        Houses,
        Roads,
        Bridges,
        Towers,
        Walls,
        Temples,
        Markets,
        Farms
    }
    
    public enum ArchitecturalStyle
    {
        Medieval,
        Modern,
        Fantasy,
        SciFi,
        Asian,
        Classical,
        Industrial
    }
    
    public enum CivilizationLevel
    {
        Tribal,
        Village,
        Town,
        City,
        Metropolis
    }
    
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    
    
    public enum AmbientSoundProfile
    {
        Forest,
        Desert,
        Ocean,
        Mountain,
        City,
        Countryside
    }
    
    public enum AILearningMode
    {
        Static,
        Adaptive,
        Experimental,
        UserGuided
    }
    
    public enum GenerationQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }
}