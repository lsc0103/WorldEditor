using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 密度管理器 - 智能控制对象放置密度
    /// 支持基于环境条件的动态密度调整、密度梯度、聚类效应等
    /// </summary>
    public class DensityManager : MonoBehaviour
    {
        [Header("密度控制设置")]
        [SerializeField] private bool enableAdaptiveDensity = true;
        [SerializeField] private bool enableClusteringEffects = true;
        [SerializeField] private bool enableCompetitionEffects = true;
        [SerializeField] private bool enableSuccessionEffects = false;
        
        [Header("全局密度设置")]
        [SerializeField] private float globalDensityMultiplier = 1f;
        [SerializeField] private AnimationCurve globalDensityFalloff;
        [SerializeField] private float densityNoiseScale = 0.1f;
        [SerializeField] private float densityNoiseInfluence = 0.3f;
        
        [Header("环境密度因子")]
        [SerializeField] private AnimationCurve temperatureDensityCurve;
        [SerializeField] private AnimationCurve moistureDensityCurve;
        [SerializeField] private AnimationCurve heightDensityCurve;
        [SerializeField] private AnimationCurve slopeDensityCurve;
        
        [Header("聚类设置")]
        [SerializeField] private float clusterRadius = 15f;
        [SerializeField] private float clusterStrength = 2f;
        [SerializeField] private int maxClusterSize = 20;
        [SerializeField] private float clusterDecayRate = 0.1f;
        
        [Header("竞争设置")]
        [SerializeField] private float competitionRadius = 8f;
        [SerializeField] private float competitionStrength = 0.5f;
        [SerializeField] private string[] competitiveSpecies;
        
        [Header("演替设置")]
        [SerializeField] private bool enablePrimarySuccession = true;
        [SerializeField] private bool enableSecondarySuccession = true;
        [SerializeField] private float successionRate = 0.01f;
        
        // 密度数据缓存
        private float[,] densityMap;
        private Vector2Int densityMapResolution;
        private Bounds densityMapBounds;
        private bool isDensityMapValid = false;
        
        void Awake()
        {
            InitializeCurves();
        }
        
        void InitializeCurves()
        {
            // 初始化默认密度曲线
            if (temperatureDensityCurve == null)
            {
                temperatureDensityCurve = new AnimationCurve();
                temperatureDensityCurve.AddKey(0f, 0.2f);    // 极寒
                temperatureDensityCurve.AddKey(0.3f, 1f);    // 温带
                temperatureDensityCurve.AddKey(0.7f, 1.2f);  // 热带
                temperatureDensityCurve.AddKey(1f, 0.8f);    // 极热
            }
            
            if (moistureDensityCurve == null)
            {
                moistureDensityCurve = new AnimationCurve();
                moistureDensityCurve.AddKey(0f, 0.1f);      // 极干旱
                moistureDensityCurve.AddKey(0.6f, 1.5f);    // 湿润
                moistureDensityCurve.AddKey(1f, 1f);        // 过湿
            }
            
            if (heightDensityCurve == null)
            {
                heightDensityCurve = new AnimationCurve();
                heightDensityCurve.AddKey(0f, 1.2f);        // 低海拔
                heightDensityCurve.AddKey(0.5f, 1f);        // 中海拔
                heightDensityCurve.AddKey(1f, 0.3f);        // 高海拔
            }
            
            if (slopeDensityCurve == null)
            {
                slopeDensityCurve = new AnimationCurve();
                slopeDensityCurve.AddKey(0f, 1f);           // 平地
                slopeDensityCurve.AddKey(0.3f, 0.8f);       // 缓坡
                slopeDensityCurve.AddKey(0.7f, 0.3f);       // 陡坡
                slopeDensityCurve.AddKey(1f, 0.1f);         // 悬崖
            }
            
            if (globalDensityFalloff == null)
            {
                globalDensityFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }
        }
        
        /// <summary>
        /// 计算指定层的密度
        /// </summary>
        public float CalculateDensity(PlacementLayer layer, WorldGenerationParameters parameters)
        {
            if (!enableAdaptiveDensity)
                return layer.baseDensity * globalDensityMultiplier;
            
            // 确保密度图有效
            if (!isDensityMapValid)
            {
                GenerateDensityMap(parameters);
            }
            
            // 计算层的平均密度
            return CalculateLayerAverageDensity(layer, parameters);
        }
        
        /// <summary>
        /// 计算指定位置的密度
        /// </summary>
        public float CalculateDensityAtPosition(PlacementLayer layer, Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            float baseDensity = layer.baseDensity * globalDensityMultiplier;
            
            if (!enableAdaptiveDensity)
                return baseDensity;
            
            // 获取环境参数
            float temperature = terrainData.GetTemperatureAtPosition(position);
            float moisture = terrainData.GetMoistureAtPosition(position);
            float height = terrainData.GetHeightAtPosition(position);
            float slope = terrainData.GetSlopeAtPosition(position);
            
            // 计算环境密度因子
            float tempFactor = temperatureDensityCurve.Evaluate(temperature);
            float moistureFactor = moistureDensityCurve.Evaluate(moisture);
            float heightFactor = heightDensityCurve.Evaluate(height);
            float slopeFactor = slopeDensityCurve.Evaluate(slope);
            
            // 计算噪声影响
            float noiseFactor = 1f;
            if (layer.useNoiseDensity)
            {
                float noiseX = position.x * densityNoiseScale;
                float noiseZ = position.z * densityNoiseScale;
                float noiseValue = Mathf.PerlinNoise(noiseX, noiseZ);
                noiseFactor = 1f + (noiseValue - 0.5f) * densityNoiseInfluence;
            }
            
            // 计算演替影响
            float successionFactor = 1f;
            if (enableSuccessionEffects)
            {
                successionFactor = CalculateSuccessionEffect(layer, position, terrainData);
            }
            
            // 计算距离衰减
            float distanceFactor = 1f;
            if (layer.densityFalloff != null)
            {
                float distanceFromCenter = CalculateDistanceFromCenter(position);
                distanceFactor = layer.densityFalloff.Evaluate(distanceFromCenter);
            }
            
            // 计算聚类效应
            float clusterFactor = 1f;
            if (enableClusteringEffects)
            {
                clusterFactor = CalculateClusterFactor(layer, position);
            }
            
            // 计算竞争效应
            float competitionFactor = 1f;
            if (enableCompetitionEffects)
            {
                competitionFactor = CalculateCompetitionFactor(layer, position);
            }
            
            // 计算生物群落密度因子
            float biomeFactor = CalculateBiomeDensityFactor(layer, biomeData.GetBiomeAtPosition(position));
            
            // 最终密度计算
            float finalDensity = baseDensity * 
                               tempFactor * 
                               moistureFactor * 
                               heightFactor * 
                               slopeFactor * 
                               noiseFactor * 
                               distanceFactor * 
                               clusterFactor * 
                               competitionFactor * 
                               biomeFactor * 
                               successionFactor;
            
            return Mathf.Max(0f, finalDensity);
        }
        
        /// <summary>
        /// 计算演替效应
        /// </summary>
        float CalculateSuccessionEffect(PlacementLayer layer, Vector3 position, TerrainData terrainData)
        {
            float successionEffect = 1f;
            
            // 基于高度的演替阶段判断
            float height = terrainData.GetHeightAtPosition(position);
            float moisture = terrainData.GetMoistureAtPosition(position);
            
            // 根据演替理论，不同植被在不同阶段占优势
            float successionStage = CalculateSuccessionStage(height, moisture);
            
            // 不同植被类型在不同演替阶段的适应性
            switch (layer.layerName.ToLower())
            {
                case "grass":
                case "herb":
                    // 草本植物在早期演替阶段占优势
                    if (enablePrimarySuccession && successionStage < 0.3f)
                        successionEffect = 1f + successionRate * 10f;
                    else if (successionStage > 0.7f)
                        successionEffect = 1f - successionRate * 5f;
                    break;
                    
                case "bush":
                case "shrub":
                    // 灌木在中期演替阶段占优势
                    if (enableSecondarySuccession && successionStage > 0.3f && successionStage < 0.8f)
                        successionEffect = 1f + successionRate * 8f;
                    break;
                    
                case "tree":
                case "forest":
                    // 树木在后期演替阶段占优势
                    if (enableSecondarySuccession && successionStage > 0.6f)
                        successionEffect = 1f + successionRate * 15f;
                    else if (successionStage < 0.2f)
                        successionEffect = 1f - successionRate * 3f;
                    break;
            }
            
            return Mathf.Clamp(successionEffect, 0.1f, 2f);
        }
        
        /// <summary>
        /// 计算演替阶段 (0=早期, 1=晚期)
        /// </summary>
        float CalculateSuccessionStage(float height, float moisture)
        {
            // 高海拔和低湿度地区演替较慢
            float baseStage = (moisture * 0.7f + (1f - height) * 0.3f);
            
            // 添加时间因素（简化版本）
            float timeEffect = Mathf.Sin(Time.time * 0.001f) * 0.1f + 0.5f;
            
            return Mathf.Clamp01(baseStage * timeEffect);
        }
        
        /// <summary>
        /// 生成密度图
        /// </summary>
        void GenerateDensityMap(WorldGenerationParameters parameters)
        {
            densityMapResolution = new Vector2Int(128, 128); // 密度图分辨率
            densityMapBounds = parameters.generationBounds;
            densityMap = new float[densityMapResolution.x, densityMapResolution.y];
            
            // 生成基础密度图
            for (int x = 0; x < densityMapResolution.x; x++)
            {
                for (int y = 0; y < densityMapResolution.y; y++)
                {
                    Vector3 worldPos = DensityMapToWorldPosition(x, y);
                    float distanceFromCenter = CalculateDistanceFromCenter(worldPos);
                    
                    // 基础密度基于距离衰减
                    float baseDensity = globalDensityFalloff.Evaluate(distanceFromCenter);
                    
                    // 添加噪声变化
                    float noiseX = x * densityNoiseScale;
                    float noiseY = y * densityNoiseScale;
                    float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);
                    float noiseFactor = 1f + (noiseValue - 0.5f) * densityNoiseInfluence;
                    
                    densityMap[x, y] = baseDensity * noiseFactor;
                }
            }
            
            isDensityMapValid = true;
        }
        
        /// <summary>
        /// 计算层的平均密度
        /// </summary>
        float CalculateLayerAverageDensity(PlacementLayer layer, WorldGenerationParameters parameters)
        {
            float totalDensity = 0f;
            int sampleCount = 0;
            int sampleStep = densityMapResolution.x / 16; // 采样步长
            
            for (int x = 0; x < densityMapResolution.x; x += sampleStep)
            {
                for (int y = 0; y < densityMapResolution.y; y += sampleStep)
                {
                    totalDensity += densityMap[x, y];
                    sampleCount++;
                }
            }
            
            float averageDensity = sampleCount > 0 ? totalDensity / sampleCount : 1f;
            return layer.baseDensity * averageDensity * globalDensityMultiplier;
        }
        
        /// <summary>
        /// 计算聚类因子
        /// </summary>
        float CalculateClusterFactor(PlacementLayer layer, Vector3 position)
        {
            if (layer.ecosystemRole != EcosystemRole.Producer)
                return 1f; // 非植物不聚类
            
            // 寻找附近同种对象
            int nearbyCount = CountNearbyObjects(position, clusterRadius, layer.layerName);
            
            if (nearbyCount == 0)
            {
                // 没有附近对象，可能开始新聚类
                return 1f;
            }
            else if (nearbyCount < maxClusterSize * 0.3f)
            {
                // 聚类增长阶段
                return 1f + clusterStrength * (nearbyCount / (float)maxClusterSize);
            }
            else if (nearbyCount < maxClusterSize)
            {
                // 聚类成熟阶段
                return 1f;
            }
            else
            {
                // 聚类过密，开始衰减
                float overpopulation = (nearbyCount - maxClusterSize) / (float)maxClusterSize;
                return Mathf.Max(0.1f, 1f - overpopulation * clusterDecayRate);
            }
        }
        
        /// <summary>
        /// 计算竞争因子
        /// </summary>
        float CalculateCompetitionFactor(PlacementLayer layer, Vector3 position)
        {
            if (competitiveSpecies == null || competitiveSpecies.Length == 0)
                return 1f;
            
            int competitorCount = 0;
            
            foreach (string speciesName in competitiveSpecies)
            {
                competitorCount += CountNearbyObjects(position, competitionRadius, speciesName);
            }
            
            if (competitorCount == 0)
                return 1f;
            
            // 竞争强度基于竞争者数量
            float competitionEffect = competitorCount * competitionStrength;
            return Mathf.Max(0.1f, 1f - competitionEffect);
        }
        
        /// <summary>
        /// 计算生物群落密度因子
        /// </summary>
        float CalculateBiomeDensityFactor(PlacementLayer layer, BiomeType biome)
        {
            // 根据生物群落和层类型调整密度
            switch (biome)
            {
                case BiomeType.Tropical:
                    return layer.layerType == PlacementLayerType.Vegetation ? 1.5f : 1f;
                    
                case BiomeType.Forest:
                    return layer.layerType == PlacementLayerType.Vegetation ? 1.3f : 1f;
                    
                case BiomeType.Grassland:
                    return layer.layerType == PlacementLayerType.Vegetation ? 
                           (layer.layerName.Contains("Grass") ? 1.4f : 0.8f) : 1f;
                    
                case BiomeType.Desert:
                    return layer.layerType == PlacementLayerType.Vegetation ? 0.2f : 1f;
                    
                case BiomeType.Tundra:
                    return layer.layerType == PlacementLayerType.Vegetation ? 0.4f : 1f;
                    
                case BiomeType.Swamp:
                    return layer.layerType == PlacementLayerType.Vegetation ? 
                           (layer.layerName.Contains("Water") ? 1.6f : 1.1f) : 1f;
                    
                case BiomeType.Mountain:
                    return layer.layerType == PlacementLayerType.Vegetation ? 0.6f : 1f;
                    
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// 计算距离中心的距离（标准化）
        /// </summary>
        float CalculateDistanceFromCenter(Vector3 position)
        {
            Vector3 center = densityMapBounds.center;
            float maxDistance = Mathf.Max(densityMapBounds.size.x, densityMapBounds.size.z) * 0.5f;
            float distance = Vector3.Distance(new Vector3(position.x, 0f, position.z), 
                                            new Vector3(center.x, 0f, center.z));
            
            return Mathf.Clamp01(distance / maxDistance);
        }
        
        /// <summary>
        /// 密度图坐标转世界坐标
        /// </summary>
        Vector3 DensityMapToWorldPosition(int x, int y)
        {
            float worldX = densityMapBounds.min.x + (float)x / densityMapResolution.x * densityMapBounds.size.x;
            float worldZ = densityMapBounds.min.z + (float)y / densityMapResolution.y * densityMapBounds.size.z;
            
            return new Vector3(worldX, 0f, worldZ);
        }
        
        /// <summary>
        /// 计算附近对象数量
        /// </summary>
        int CountNearbyObjects(Vector3 position, float radius, string layerOrSpeciesName)
        {
            // 这里需要集成放置网格系统
            // 目前使用简化的检查
            
            Collider[] nearbyColliders = Physics.OverlapSphere(position, radius);
            int count = 0;
            
            foreach (Collider collider in nearbyColliders)
            {
                // 检查对象名称或标签
                if (collider.name.Contains(layerOrSpeciesName) || 
                    collider.CompareTag(layerOrSpeciesName))
                {
                    count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 更新密度图（用于动态调整）
        /// </summary>
        public void UpdateDensityMap(WorldGenerationParameters parameters)
        {
            isDensityMapValid = false;
            GenerateDensityMap(parameters);
        }
        
        /// <summary>
        /// 获取密度统计信息
        /// </summary>
        public string GetDensityStats()
        {
            if (!isDensityMapValid)
                return "密度图未生成";
            
            float minDensity = float.MaxValue;
            float maxDensity = float.MinValue;
            float totalDensity = 0f;
            int cellCount = densityMapResolution.x * densityMapResolution.y;
            
            for (int x = 0; x < densityMapResolution.x; x++)
            {
                for (int y = 0; y < densityMapResolution.y; y++)
                {
                    float density = densityMap[x, y];
                    minDensity = Mathf.Min(minDensity, density);
                    maxDensity = Mathf.Max(maxDensity, density);
                    totalDensity += density;
                }
            }
            
            float averageDensity = totalDensity / cellCount;
            
            return $"密度统计信息:\n" +
                   $"平均密度: {averageDensity:F3}\n" +
                   $"最小密度: {minDensity:F3}\n" +
                   $"最大密度: {maxDensity:F3}\n" +
                   $"密度图分辨率: {densityMapResolution.x}x{densityMapResolution.y}\n" +
                   $"全局密度乘数: {globalDensityMultiplier:F2}";
        }
        
        /// <summary>
        /// 可视化密度图（调试用）
        /// </summary>
        public Texture2D GenerateDensityVisualization()
        {
            if (!isDensityMapValid)
                return null;
            
            Texture2D texture = new Texture2D(densityMapResolution.x, densityMapResolution.y);
            
            for (int x = 0; x < densityMapResolution.x; x++)
            {
                for (int y = 0; y < densityMapResolution.y; y++)
                {
                    float density = densityMap[x, y];
                    Color color = new Color(density, density, density, 1f);
                    texture.SetPixel(x, y, color);
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 重置密度管理器
        /// </summary>
        public void Reset()
        {
            isDensityMapValid = false;
            densityMap = null;
            Debug.Log("[DensityManager] 密度管理器已重置");
        }
    }
}