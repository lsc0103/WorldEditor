using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 生物群落分析器 - 分析和确定不同区域的生物群落类型
    /// </summary>
    public class BiomeAnalyzer : MonoBehaviour
    {
        [Header("生物群落设置")]
        [SerializeField] private bool enableAdvancedBiomeAnalysis = true;
        [SerializeField] private float biomeTransitionSharpness = 2f;
        [SerializeField] private int biomeMapResolution = 128;
        
        [Header("温度阈值")]
        [SerializeField] private float coldThreshold = 0.2f;
        [SerializeField] private float coolThreshold = 0.4f;
        [SerializeField] private float warmThreshold = 0.6f;
        [SerializeField] private float hotThreshold = 0.8f;
        
        [Header("湿度阈值")]
        [SerializeField] private float aridThreshold = 0.2f;
        [SerializeField] private float dryThreshold = 0.4f;
        [SerializeField] private float moistThreshold = 0.6f;
        [SerializeField] private float wetThreshold = 0.8f;
        
        [Header("高度阈值")]
        [SerializeField] private float lowlandThreshold = 0.3f;
        [SerializeField] private float hillThreshold = 0.6f;
        [SerializeField] private float mountainThreshold = 0.8f;
        
        [Header("生物多样性")]
        [SerializeField] private bool calculateBiodiversity = true;
        [SerializeField] private float biodiversityNoiseScale = 0.1f;
        [SerializeField] private AnimationCurve biodiversityByTemperature;
        [SerializeField] private AnimationCurve biodiversityByMoisture;
        
        void Awake()
        {
            InitializeCurves();
        }
        
        void InitializeCurves()
        {
            // 初始化生物多样性曲线
            if (biodiversityByTemperature == null)
            {
                biodiversityByTemperature = new AnimationCurve();
                biodiversityByTemperature.AddKey(0f, 0.2f);    // 极寒地区生物多样性低
                biodiversityByTemperature.AddKey(0.3f, 0.8f);  // 温带地区生物多样性高
                biodiversityByTemperature.AddKey(0.7f, 1f);    // 热带地区生物多样性最高
                biodiversityByTemperature.AddKey(1f, 0.6f);    // 极热地区生物多样性下降
            }
            
            if (biodiversityByMoisture == null)
            {
                biodiversityByMoisture = new AnimationCurve();
                biodiversityByMoisture.AddKey(0f, 0.1f);      // 极干旱地区
                biodiversityByMoisture.AddKey(0.6f, 1f);      // 湿润地区生物多样性最高
                biodiversityByMoisture.AddKey(1f, 0.8f);      // 过度湿润地区稍有下降
            }
        }
        
        /// <summary>
        /// 分析生物群落
        /// </summary>
        public BiomeData AnalyzeBiomes(WorldGenerationParameters parameters, TerrainData terrainData)
        {
            Debug.Log("[BiomeAnalyzer] 开始分析生物群落...");
            
            BiomeData biomeData = new BiomeData();
            biomeData.resolution = new Vector2Int(biomeMapResolution, biomeMapResolution);
            biomeData.bounds = parameters.generationBounds;
            
            // 生成生物群落地图
            biomeData.biomeMap = GenerateBiomeMap(terrainData, parameters);
            
            // 计算生物多样性
            if (calculateBiodiversity)
            {
                biomeData.biodiversityMap = CalculateBiodiversity(terrainData, biomeData.biomeMap);
            }
            
            Debug.Log("[BiomeAnalyzer] 生物群落分析完成");
            return biomeData;
        }
        
        /// <summary>
        /// 生成生物群落地图
        /// </summary>
        BiomeType[,] GenerateBiomeMap(TerrainData terrainData, WorldGenerationParameters parameters)
        {
            BiomeType[,] biomeMap = new BiomeType[biomeMapResolution, biomeMapResolution];
            
            for (int x = 0; x < biomeMapResolution; x++)
            {
                for (int y = 0; y < biomeMapResolution; y++)
                {
                    // 将生物群落地图坐标转换为地形数据坐标
                    Vector3 worldPos = BiomeToWorldPosition(x, y, parameters.generationBounds);
                    
                    float height = terrainData.GetHeightAtPosition(worldPos);
                    float temperature = terrainData.GetTemperatureAtPosition(worldPos);
                    float moisture = terrainData.GetMoistureAtPosition(worldPos);
                    float slope = terrainData.GetSlopeAtPosition(worldPos);
                    
                    // 确定生物群落类型
                    biomeMap[x, y] = DetermineBiomeType(height, temperature, moisture, slope);
                }
            }
            
            // 平滑生物群落边界
            if (enableAdvancedBiomeAnalysis)
            {
                biomeMap = SmoothBiomeBoundaries(biomeMap);
            }
            
            return biomeMap;
        }
        
        /// <summary>
        /// 确定生物群落类型
        /// </summary>
        BiomeType DetermineBiomeType(float height, float temperature, float moisture, float slope)
        {
            // 应用过渡锐度来软化边界
            float transitionFactor = 1f / biomeTransitionSharpness;
            
            // 高海拔地区优先处理
            if (height > mountainThreshold - transitionFactor * 0.1f)
            {
                if (temperature < coldThreshold + transitionFactor * 0.1f)
                    return BiomeType.Tundra;
                else if (slope > 0.5f - transitionFactor * 0.1f)
                    return BiomeType.Mountain;
                else
                    return BiomeType.Mountain;
            }
            else if (height > hillThreshold - transitionFactor * 0.05f)
            {
                // 丘陵地区，根据温湿度决定具体类型
                if (temperature < coolThreshold && moisture > moistThreshold)
                    return BiomeType.Forest;
                else if (moisture < dryThreshold)
                    return BiomeType.Grassland;
                else
                    return BiomeType.Temperate;
            }
            else if (height <= lowlandThreshold + transitionFactor * 0.05f)
            {
                // 低地区域，通常湿度较高，适合湿地和沼泽
                if (moisture > wetThreshold)
                    return BiomeType.Swamp;
                else if (temperature > warmThreshold && moisture > moistThreshold)
                    return BiomeType.Tropical;
                else if (moisture > moistThreshold)
                    return BiomeType.Forest;
                else
                    return BiomeType.Grassland;
            }
            
            // 基于温度和湿度的主要分类，使用过渡锐度
            if (temperature < coldThreshold + transitionFactor * 0.05f)
            {
                // 寒冷气候
                return BiomeType.Tundra;
            }
            else if (temperature < coolThreshold)
            {
                // 凉爽气候
                if (moisture < dryThreshold)
                    return BiomeType.Grassland;
                else
                    return BiomeType.Temperate;
            }
            else if (temperature < warmThreshold)
            {
                // 温和气候
                if (moisture < aridThreshold)
                    return BiomeType.Desert;
                else if (moisture < moistThreshold)
                    return BiomeType.Grassland;
                else if (moisture < wetThreshold)
                    return BiomeType.Forest;
                else
                    return BiomeType.Swamp;
            }
            else if (temperature < hotThreshold)
            {
                // 炎热气候
                if (moisture < aridThreshold)
                    return BiomeType.Desert;
                else if (moisture > wetThreshold)
                    return BiomeType.Tropical;
                else
                    return BiomeType.Forest;
            }
            else
            {
                // 极热气候
                if (moisture < dryThreshold)
                    return BiomeType.Desert;
                else
                    return BiomeType.Tropical;
            }
        }
        
        /// <summary>
        /// 平滑生物群落边界
        /// </summary>
        BiomeType[,] SmoothBiomeBoundaries(BiomeType[,] originalBiomeMap)
        {
            BiomeType[,] smoothedMap = new BiomeType[biomeMapResolution, biomeMapResolution];
            
            for (int x = 0; x < biomeMapResolution; x++)
            {
                for (int y = 0; y < biomeMapResolution; y++)
                {
                    smoothedMap[x, y] = GetDominantBiomeInNeighborhood(originalBiomeMap, x, y, 2);
                }
            }
            
            return smoothedMap;
        }
        
        /// <summary>
        /// 获取邻域内的主导生物群落
        /// </summary>
        BiomeType GetDominantBiomeInNeighborhood(BiomeType[,] biomeMap, int centerX, int centerY, int radius)
        {
            var biomeCounts = new System.Collections.Generic.Dictionary<BiomeType, int>();
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < biomeMapResolution && y >= 0 && y < biomeMapResolution)
                    {
                        BiomeType biome = biomeMap[x, y];
                        
                        if (biomeCounts.ContainsKey(biome))
                            biomeCounts[biome]++;
                        else
                            biomeCounts[biome] = 1;
                    }
                }
            }
            
            // 找到最常见的生物群落
            BiomeType dominantBiome = biomeMap[centerX, centerY];
            int maxCount = 0;
            
            foreach (var kvp in biomeCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    dominantBiome = kvp.Key;
                }
            }
            
            return dominantBiome;
        }
        
        /// <summary>
        /// 计算生物多样性
        /// </summary>
        float[,] CalculateBiodiversity(TerrainData terrainData, BiomeType[,] biomeMap)
        {
            float[,] biodiversityMap = new float[biomeMapResolution, biomeMapResolution];
            
            for (int x = 0; x < biomeMapResolution; x++)
            {
                for (int y = 0; y < biomeMapResolution; y++)
                {
                    // 获取环境参数
                    Vector3 worldPos = BiomeToWorldPosition(x, y, terrainData.bounds);
                    float temperature = terrainData.GetTemperatureAtPosition(worldPos);
                    float moisture = terrainData.GetMoistureAtPosition(worldPos);
                    
                    // 基于环境条件计算基础生物多样性
                    float tempBiodiversity = biodiversityByTemperature.Evaluate(temperature);
                    float moistureBiodiversity = biodiversityByMoisture.Evaluate(moisture);
                    float baseBiodiversity = (tempBiodiversity + moistureBiodiversity) * 0.5f;
                    
                    // 基于生物群落类型调整
                    float biomeMultiplier = GetBiomeBiodiversityMultiplier(biomeMap[x, y]);
                    
                    // 添加生物多样性热点（使用噪声）
                    float noiseX = x * biodiversityNoiseScale;
                    float noiseY = y * biodiversityNoiseScale;
                    float biodiversityNoise = Mathf.PerlinNoise(noiseX, noiseY);
                    
                    // 边缘效应 - 生物群落边界通常有更高的生物多样性
                    float edgeEffect = CalculateEdgeEffect(biomeMap, x, y);
                    
                    // 计算最终生物多样性
                    float finalBiodiversity = baseBiodiversity * biomeMultiplier * (1f + biodiversityNoise * 0.3f) * (1f + edgeEffect);
                    
                    biodiversityMap[x, y] = Mathf.Clamp01(finalBiodiversity);
                }
            }
            
            return biodiversityMap;
        }
        
        /// <summary>
        /// 获取生物群落的生物多样性乘数
        /// </summary>
        float GetBiomeBiodiversityMultiplier(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Tropical: return 1.5f;      // 热带雨林生物多样性最高
                case BiomeType.Forest: return 1.2f;        // 森林生物多样性较高
                case BiomeType.Temperate: return 1.1f;     // 温带生物多样性中等偏高
                case BiomeType.Swamp: return 1.3f;         // 湿地生物多样性高
                case BiomeType.Grassland: return 0.9f;     // 草原生物多样性中等
                case BiomeType.Mountain: return 0.8f;      // 山地生物多样性中等偏低
                case BiomeType.Desert: return 0.4f;        // 沙漠生物多样性低
                case BiomeType.Tundra: return 0.3f;        // 苔原生物多样性最低
                default: return 1f;
            }
        }
        
        /// <summary>
        /// 计算边缘效应
        /// </summary>
        float CalculateEdgeEffect(BiomeType[,] biomeMap, int x, int y)
        {
            BiomeType centerBiome = biomeMap[x, y];
            int differentNeighbors = 0;
            int totalNeighbors = 0;
            
            // 检查3x3邻域
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < biomeMapResolution && ny >= 0 && ny < biomeMapResolution)
                    {
                        if (biomeMap[nx, ny] != centerBiome)
                            differentNeighbors++;
                        totalNeighbors++;
                    }
                }
            }
            
            // 边缘效应强度基于不同邻居的比例
            return totalNeighbors > 0 ? (float)differentNeighbors / totalNeighbors * 0.3f : 0f;
        }
        
        /// <summary>
        /// 生物群落坐标转世界坐标
        /// </summary>
        Vector3 BiomeToWorldPosition(int x, int y, Bounds bounds)
        {
            float worldX = bounds.min.x + (float)x / biomeMapResolution * bounds.size.x;
            float worldZ = bounds.min.z + (float)y / biomeMapResolution * bounds.size.z;
            
            return new Vector3(worldX, 0f, worldZ);
        }
        
        /// <summary>
        /// 获取指定位置的生物群落适应性
        /// </summary>
        public float GetBiomeCompatibility(Vector3 worldPosition, BiomeType targetBiome, BiomeData biomeData)
        {
            BiomeType actualBiome = biomeData.GetBiomeAtPosition(worldPosition);
            
            if (actualBiome == targetBiome)
                return 1f;
            
            // 计算生物群落之间的兼容性
            return CalculateBiomeCompatibility(actualBiome, targetBiome);
        }
        
        /// <summary>
        /// 计算生物群落兼容性
        /// </summary>
        float CalculateBiomeCompatibility(BiomeType biome1, BiomeType biome2)
        {
            // 定义生物群落兼容性矩阵
            if (biome1 == biome2) return 1f;
            
            // 相似的生物群落有较高的兼容性
            if ((biome1 == BiomeType.Forest && biome2 == BiomeType.Temperate) ||
                (biome1 == BiomeType.Temperate && biome2 == BiomeType.Forest))
                return 0.8f;
                
            if ((biome1 == BiomeType.Grassland && biome2 == BiomeType.Temperate) ||
                (biome1 == BiomeType.Temperate && biome2 == BiomeType.Grassland))
                return 0.7f;
                
            if ((biome1 == BiomeType.Forest && biome2 == BiomeType.Tropical) ||
                (biome1 == BiomeType.Tropical && biome2 == BiomeType.Forest))
                return 0.6f;
            
            // 默认兼容性
            return 0.3f;
        }
        
        /// <summary>
        /// 获取生物群落统计信息
        /// </summary>
        public string GetBiomeStats(BiomeData biomeData)
        {
            if (biomeData?.biomeMap == null) return "无数据";
            
            var biomeCounts = new System.Collections.Generic.Dictionary<BiomeType, int>();
            int totalCells = biomeMapResolution * biomeMapResolution;
            
            // 统计各生物群落的分布
            for (int x = 0; x < biomeMapResolution; x++)
            {
                for (int y = 0; y < biomeMapResolution; y++)
                {
                    BiomeType biome = biomeData.biomeMap[x, y];
                    
                    if (biomeCounts.ContainsKey(biome))
                        biomeCounts[biome]++;
                    else
                        biomeCounts[biome] = 1;
                }
            }
            
            // 计算平均生物多样性
            float avgBiodiversity = 0f;
            if (biomeData.biodiversityMap != null)
            {
                float sum = 0f;
                for (int x = 0; x < biomeMapResolution; x++)
                {
                    for (int y = 0; y < biomeMapResolution; y++)
                    {
                        sum += biomeData.biodiversityMap[x, y];
                    }
                }
                avgBiodiversity = sum / totalCells;
            }
            
            // 生成统计报告
            string stats = $"生物群落分析统计:\n";
            stats += $"平均生物多样性: {avgBiodiversity:F3}\n";
            stats += "生物群落分布:\n";
            
            foreach (var kvp in biomeCounts)
            {
                float percentage = (float)kvp.Value / totalCells * 100f;
                stats += $"  {kvp.Key}: {percentage:F1}%\n";
            }
            
            return stats;
        }
    }
}