using UnityEngine;

namespace WorldEditor.Core
{
    /// <summary>
    /// 世界编辑器全局设置 - 配置所有生成参数
    /// </summary>
    [CreateAssetMenu(fileName = "WorldEditorSettings", menuName = "WorldEditor/Settings")]
    public class WorldEditorSettings : ScriptableObject
    {
        [Header("项目设置")]
        public string projectName = "WorldEditor Project";
        public string version = "1.0.0";
        
        [Header("默认世界参数")]
        public Vector2 defaultWorldSize = new Vector2(1000f, 1000f);
        public float defaultTerrainHeight = 600f;
        
        [Header("自动保存")]
        public bool enableAutoSave = true;
        public int autoSaveInterval = 5;
        
        [Header("性能设置")]
        public bool enableMultithreading = true;
        public int maxWorkerThreads = 4;
        public int maxMemoryUsage = 2048;
        public bool enableMemoryOptimization = true;
        public bool enableLODSystem = true;
        public bool enableOcclusion = true;
        
        [Header("调试设置")]
        public bool enableLogging = true;
        public LogLevel logLevel = LogLevel.Info;
        public bool logToFile = false;
        public bool showDebugGizmos = false;
        public bool showPerformanceStats = false;
        
        [Header("高级设置")]
        public bool enableExperimentalFeatures = false;
        public bool enableCaching = true;
        public int cacheSize = 512;
        
        [Header("地形设置")]
        [SerializeField] public TerrainSettings terrainSettings = new TerrainSettings();
        
        [Header("环境设置")]
        [SerializeField] public EnvironmentSettings environmentSettings = new EnvironmentSettings();
        
        [Header("放置系统设置")]
        [SerializeField] public PlacementSettings placementSettings = new PlacementSettings();
        
        [Header("AI生成设置")]
        [SerializeField] public AIGenerationSettings aiSettings = new AIGenerationSettings();
        
        [Header("优化设置")]
        [SerializeField] public OptimizationSettings optimizationSettings = new OptimizationSettings();
        
        public void Initialize()
        {
            terrainSettings.Initialize();
            environmentSettings.Initialize();
            placementSettings.Initialize();
            aiSettings.Initialize();
            optimizationSettings.Initialize();
            
            // 初始化新添加的属性
            if (maxWorkerThreads <= 0)
                maxWorkerThreads = System.Environment.ProcessorCount;
        }
        
        public void ResetToDefaults()
        {
            projectName = "WorldEditor Project";
            version = "1.0.0";
            defaultWorldSize = new Vector2(1000f, 1000f);
            defaultTerrainHeight = 600f;
            enableAutoSave = true;
            autoSaveInterval = 5;
            enableMultithreading = true;
            maxWorkerThreads = System.Environment.ProcessorCount;
            maxMemoryUsage = 2048;
            enableMemoryOptimization = true;
            enableLODSystem = true;
            enableOcclusion = true;
            enableLogging = true;
            logLevel = LogLevel.Info;
            logToFile = false;
            showDebugGizmos = false;
            showPerformanceStats = false;
            enableExperimentalFeatures = false;
            enableCaching = true;
            cacheSize = 512;
            
            // 重置子设置
            terrainSettings.Initialize();
            environmentSettings.Initialize();
            placementSettings.Initialize();
            aiSettings.Initialize();
            optimizationSettings.Initialize();
        }
        
        /// <summary>
        /// 创建世界生成参数
        /// </summary>
        public WorldGenerationParameters CreateGenerationParameters()
        {
            var parameters = new WorldGenerationParameters();
            
            // 基础设置
            parameters.areaSize = defaultWorldSize;
            parameters.centerPosition = Vector3.zero;
            
            // 地形参数
            parameters.terrainParams.baseHeight = 0f;
            parameters.terrainParams.heightVariation = defaultTerrainHeight;
            parameters.terrainParams.terrainType = terrainSettings.terrainType;
            parameters.terrainParams.biome = terrainSettings.biomeType;
            
            // 植被参数
            parameters.vegetationParams.overallDensity = placementSettings.vegetationDensity;
            parameters.vegetationParams.treeDensity = placementSettings.vegetationDensity * 0.3f;
            parameters.vegetationParams.grassDensity = placementSettings.vegetationDensity * 0.8f;
            
            // 环境参数
            parameters.environmentParams.weather = environmentSettings.defaultWeather;
            parameters.environmentParams.timeOfDay = environmentSettings.defaultTimeOfDay;
            parameters.environmentParams.temperature = environmentSettings.defaultTemperature;
            parameters.environmentParams.humidity = environmentSettings.defaultHumidity;
            
            // AI参数
            parameters.aiParams.useAIForTerrain = aiSettings.enableAIGeneration;
            parameters.aiParams.useAIForVegetation = aiSettings.enableAIGeneration;
            parameters.aiParams.creativityLevel = 0.5f;
            
            // 初始化参数
            parameters.Initialize();
            
            return parameters;
        }
    }
    
    [System.Serializable]
    public class TerrainSettings
    {
        [Header("地形尺寸")]
        public int terrainWidth = 1024;
        public int terrainHeight = 1024;
        public float terrainHeightScale = 600f;
        
        [Header("高度图生成")]
        public NoiseType primaryNoiseType = NoiseType.Perlin;
        public float primaryNoiseScale = 0.01f;
        public int primaryNoiseOctaves = 6;
        public float primaryNoisePersistence = 0.5f;
        public float primaryNoiseLacunarity = 2f;
        
        [Header("纹理混合")]
        public TerrainLayer[] terrainLayers;
        public bool enableAutoTexturing = true;
        public float textureBlendSharpness = 8f;
        
        [Header("细节设置")]
        public bool enableErosion = true;
        public float erosionStrength = 0.3f;
        public int erosionIterations = 50;
        
        [Header("地形类型")]
        public TerrainType terrainType = TerrainType.Procedural;
        public BiomeType biomeType = BiomeType.Temperate;
        
        public void Initialize()
        {
            if (terrainLayers == null || terrainLayers.Length == 0)
            {
                // 创建默认地形图层
                terrainLayers = new TerrainLayer[4];
            }
        }
    }
    
    [System.Serializable]
    public class EnvironmentSettings
    {
        [Header("天气系统")]
        public bool enableDynamicWeather = true;
        public WeatherType defaultWeather = WeatherType.Clear;
        public float weatherTransitionSpeed = 1f;
        
        [Header("日夜循环")]
        public bool enableDayNightCycle = true;
        public float dayDuration = 1440f; // 24分钟 = 24小时
        public TimeOfDay defaultTimeOfDay = TimeOfDay.Noon;
        public Gradient skyColorGradient;
        public AnimationCurve sunIntensityCurve;
        
        [Header("环境参数")]
        public float defaultTemperature = 20f;
        public float defaultHumidity = 0.5f;
        
        [Header("大气渲染")]
        public bool enableVolumetricFog = true;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
        public float atmosphereThickness = 1f;
        
        [Header("水体系统")]
        public bool enableDynamicWater = true;
        public Material waterMaterial;
        public float waveStrength = 1f;
        public float waterReflectionQuality = 1f;
        
        public void Initialize()
        {
            if (skyColorGradient == null)
            {
                skyColorGradient = new Gradient();
                // 设置默认天空颜色渐变
                var colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.3f, 0.6f), 0f);    // 夜晚
                colorKeys[1] = new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0.5f);     // 白天
                colorKeys[2] = new GradientColorKey(new Color(0.2f, 0.3f, 0.6f), 1f);     // 夜晚
                skyColorGradient.colorKeys = colorKeys;
            }
            
            if (sunIntensityCurve == null)
            {
                sunIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
    
    [System.Serializable]
    public class PlacementSettings
    {
        [Header("植被放置")]
        public bool enableVegetationPlacement = true;
        public GameObject[] treePrefabs;
        public GameObject[] grassPrefabs;
        public GameObject[] rockPrefabs;
        
        [Header("放置规则")]
        public float vegetationDensity = 0.5f;
        public float minSlopeForVegetation = 0f;
        public float maxSlopeForVegetation = 30f;
        public float minHeightForVegetation = 0f;
        public float maxHeightForVegetation = 500f;
        
        [Header("智能分布")]
        public bool useSmartDistribution = true;
        public bool avoidWaterBodies = true;
        public bool clusterVegetation = true;
        public float clusterRadius = 10f;
        
        [Header("结构放置")]
        public GameObject[] structurePrefabs;
        public bool enableProceduralStructures = true;
        public float structureDensity = 0.1f;
        
        public void Initialize()
        {
            // 初始化默认预制件数组
            if (treePrefabs == null) treePrefabs = new GameObject[0];
            if (grassPrefabs == null) grassPrefabs = new GameObject[0];
            if (rockPrefabs == null) rockPrefabs = new GameObject[0];
            if (structurePrefabs == null) structurePrefabs = new GameObject[0];
        }
    }
    
    [System.Serializable]
    public class AIGenerationSettings
    {
        [Header("AI驱动生成")]
        public bool enableAIGeneration = true;
        public AIComplexityLevel complexityLevel = AIComplexityLevel.Medium;
        
        [Header("生成风格")]
        public BiomeType targetBiome = BiomeType.Temperate;
        public GenerationStyle style = GenerationStyle.Realistic;
        
        [Header("学习参数")]
        public bool enableMLLearning = false;
        public int trainingIterations = 100;
        public float learningRate = 0.01f;
        
        public void Initialize()
        {
            // AI设置初始化
        }
    }
    
    [System.Serializable]
    public class OptimizationSettings
    {
        [Header("LOD系统")]
        public bool enableLODSystem = true;
        public float[] lodDistances = { 50f, 150f, 500f, 1500f };
        
        [Header("遮挡剔除")]
        public bool enableOcclusionCulling = true;
        public float occlusionCullingDistance = 1000f;
        
        [Header("内存管理")]
        public bool enableStreamingLOD = true;
        public int maxActiveChunks = 25;
        public float chunkSize = 100f;
        
        [Header("渲染优化")]
        public bool enableGPUInstancing = true;
        public bool enableBatching = true;
        public int maxInstancesPerBatch = 1000;
        
        public void Initialize()
        {
            if (lodDistances == null || lodDistances.Length == 0)
            {
                lodDistances = new float[] { 50f, 150f, 500f, 1500f };
            }
        }
    }
    
    // 枚举定义
    public enum NoiseType
    {
        Perlin,
        Simplex,
        Ridged,
        Cellular,
        Voronoi
    }
    
    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rainy,
        Stormy,
        Foggy,
        Snowy
    }
    
    public enum BiomeType
    {
        Desert,
        Forest,
        Grassland,
        Mountain,
        Tundra,
        Tropical,
        Temperate,
        Swamp
    }
    
    public enum GenerationStyle
    {
        Realistic,
        Stylized,
        Fantasy,
        SciFi,
        Post_Apocalyptic
    }
    
    public enum TerrainTextureStyle
    {
        Realistic,
        Stylized,
        Cartoon,
        Photorealistic
    }
    
    public enum AIComplexityLevel
    {
        Simple,
        Medium,
        Complex,
        Ultra
    }
    
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Debug,
        Verbose
    }
    
    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
        Night
    }
}