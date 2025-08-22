using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 地形分析器 - 分析地形特征用于智能放置
    /// </summary>
    public class TerrainAnalyzer : MonoBehaviour
    {
        [Header("分析设置")]
        [SerializeField] private int analysisResolution = 256;
        [SerializeField] private bool enableSlopeAnalysis = true;
        [SerializeField] private bool enableMoistureAnalysis = true;
        [SerializeField] private bool enableTemperatureAnalysis = true;
        [SerializeField] private bool enableExposureAnalysis = true;
        
        [Header("坡度分析")]
        [SerializeField] private float slopeThreshold = 30f;
        [SerializeField] private bool useSmoothSlope = true;
        [SerializeField] private int slopeSmoothRadius = 2;
        
        [Header("湿度分析")]
        [SerializeField] private float moistureDecayDistance = 50f;
        [SerializeField] private float riverMoistureInfluence = 2f;
        [SerializeField] private float lakeMoistureInfluence = 1.5f;
        
        [Header("温度分析")]
        [SerializeField] private float temperatureLapseRate = 6.5f; // 每1000米高度下降6.5度
        [SerializeField] private float baseTemperature = 20f;
        [SerializeField] private AnimationCurve seasonalVariation;
        
        [Header("光照分析")]
        [SerializeField] private Vector3 sunDirection = new Vector3(0.3f, 0.7f, 0.3f);
        [SerializeField] private bool calculateShadows = true;
        [SerializeField] private int shadowRaySteps = 10;
        
        /// <summary>
        /// 分析地形数据
        /// </summary>
        public TerrainData AnalyzeTerrain(WorldGenerationParameters parameters)
        {
            Debug.Log("[TerrainAnalyzer] 开始分析地形数据...");
            
            TerrainData terrainData = new TerrainData();
            terrainData.resolution = new Vector2Int(analysisResolution, analysisResolution);
            terrainData.bounds = parameters.generationBounds;
            
            // 获取高度数据
            terrainData.heightMap = ExtractHeightMap(parameters);
            
            // 分析坡度
            if (enableSlopeAnalysis)
            {
                terrainData.slopeMap = AnalyzeSlope(terrainData.heightMap);
            }
            
            // 分析湿度
            if (enableMoistureAnalysis)
            {
                terrainData.moistureMap = AnalyzeMoisture(terrainData.heightMap, parameters);
            }
            
            // 分析温度
            if (enableTemperatureAnalysis)
            {
                terrainData.temperatureMap = AnalyzeTemperature(terrainData.heightMap, parameters);
            }
            
            // 分析光照暴露度
            if (enableExposureAnalysis)
            {
                terrainData.exposureMap = AnalyzeExposure(terrainData.heightMap, parameters);
            }
            
            Debug.Log("[TerrainAnalyzer] 地形分析完成");
            return terrainData;
        }
        
        /// <summary>
        /// 提取高度图数据
        /// </summary>
        float[,] ExtractHeightMap(WorldGenerationParameters parameters)
        {
            float[,] heightMap = new float[analysisResolution, analysisResolution];
            
            // 尝试从Unity地形组件获取高度数据
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            
            if (terrain != null && terrain.terrainData != null)
            {
                var unityHeightMap = terrain.terrainData.GetHeights(0, 0, 
                    terrain.terrainData.heightmapResolution, 
                    terrain.terrainData.heightmapResolution);
                
                // 重采样到分析分辨率
                heightMap = ResampleHeightMap(unityHeightMap, analysisResolution);
            }
            else
            {
                // 如果没有地形数据，生成平坦地形
                Debug.LogWarning("[TerrainAnalyzer] 未找到地形数据，使用平坦地形");
                for (int x = 0; x < analysisResolution; x++)
                {
                    for (int y = 0; y < analysisResolution; y++)
                    {
                        heightMap[x, y] = 0.5f; // 中等高度
                    }
                }
            }
            
            return heightMap;
        }
        
        /// <summary>
        /// 重采样高度图
        /// </summary>
        float[,] ResampleHeightMap(float[,] originalMap, int targetResolution)
        {
            int originalResolution = originalMap.GetLength(0);
            float[,] resampledMap = new float[targetResolution, targetResolution];
            
            float scale = (float)originalResolution / targetResolution;
            
            for (int x = 0; x < targetResolution; x++)
            {
                for (int y = 0; y < targetResolution; y++)
                {
                    float srcX = x * scale;
                    float srcY = y * scale;
                    
                    resampledMap[x, y] = BilinearSample(originalMap, srcX, srcY, originalResolution);
                }
            }
            
            return resampledMap;
        }
        
        /// <summary>
        /// 双线性采样
        /// </summary>
        float BilinearSample(float[,] map, float x, float y, int resolution)
        {
            int x1 = Mathf.FloorToInt(x);
            int y1 = Mathf.FloorToInt(y);
            int x2 = Mathf.Min(x1 + 1, resolution - 1);
            int y2 = Mathf.Min(y1 + 1, resolution - 1);
            
            float fx = x - x1;
            float fy = y - y1;
            
            float v1 = Mathf.Lerp(map[x1, y1], map[x2, y1], fx);
            float v2 = Mathf.Lerp(map[x1, y2], map[x2, y2], fx);
            
            return Mathf.Lerp(v1, v2, fy);
        }
        
        /// <summary>
        /// 分析坡度
        /// </summary>
        float[,] AnalyzeSlope(float[,] heightMap)
        {
            int resolution = heightMap.GetLength(0);
            float[,] slopeMap = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    slopeMap[x, y] = CalculateSlope(heightMap, x, y, resolution);
                }
            }
            
            // 平滑坡度数据
            if (useSmoothSlope)
            {
                slopeMap = SmoothMap(slopeMap, slopeSmoothRadius);
            }
            
            return slopeMap;
        }
        
        /// <summary>
        /// 计算单点坡度
        /// </summary>
        float CalculateSlope(float[,] heightMap, int x, int y, int resolution)
        {
            // 获取相邻点高度
            float heightN = GetHeightSafe(heightMap, x, y - 1, resolution);
            float heightS = GetHeightSafe(heightMap, x, y + 1, resolution);
            float heightE = GetHeightSafe(heightMap, x + 1, y, resolution);
            float heightW = GetHeightSafe(heightMap, x - 1, y, resolution);
            
            // 计算梯度
            float gradX = heightE - heightW;
            float gradY = heightN - heightS;
            
            // 计算坡度角（度数）
            float slope = Mathf.Atan(Mathf.Sqrt(gradX * gradX + gradY * gradY)) * Mathf.Rad2Deg;
            
            // 应用坡度阈值进行分类增强
            if (slope > slopeThreshold)
            {
                // 超过阈值的陡坡进行增强
                slope = slopeThreshold + (slope - slopeThreshold) * 1.5f;
            }
            
            // 标准化到0-1范围
            return Mathf.Clamp01(slope / 90f);
        }
        
        /// <summary>
        /// 安全获取高度值
        /// </summary>
        float GetHeightSafe(float[,] heightMap, int x, int y, int resolution)
        {
            x = Mathf.Clamp(x, 0, resolution - 1);
            y = Mathf.Clamp(y, 0, resolution - 1);
            return heightMap[x, y];
        }
        
        /// <summary>
        /// 分析湿度
        /// </summary>
        float[,] AnalyzeMoisture(float[,] heightMap, WorldGenerationParameters parameters)
        {
            int resolution = heightMap.GetLength(0);
            float[,] moistureMap = new float[resolution, resolution];
            
            // 基于高度的基础湿度
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float height = heightMap[x, y];
                    
                    // 低海拔地区湿度较高
                    float baseMoisture = 1f - height;
                    
                    // 添加噪声变化
                    float noiseX = (float)x / resolution * 10f;
                    float noiseY = (float)y / resolution * 10f;
                    float moistureNoise = Mathf.PerlinNoise(noiseX, noiseY) * 0.3f;
                    
                    moistureMap[x, y] = Mathf.Clamp01(baseMoisture + moistureNoise);
                }
            }
            
            // 考虑水体影响
            AddWaterBodyInfluence(moistureMap, parameters);
            
            // 考虑雨影效应
            AddRainShadowEffect(moistureMap, heightMap);
            
            return moistureMap;
        }
        
        /// <summary>
        /// 添加水体影响
        /// </summary>
        void AddWaterBodyInfluence(float[,] moistureMap, WorldGenerationParameters parameters)
        {
            // 这里可以集成河流生成器的数据
            // 目前使用简化版本
            
            int resolution = moistureMap.GetLength(0);
            
            // 模拟河流和湖泊对湿度的影响
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    // 寻找最近的水体（简化版本）
                    float distanceToWater = FindDistanceToNearestWater(x, y, resolution);
                    
                    if (distanceToWater < moistureDecayDistance)
                    {
                        float influence = 1f - (distanceToWater / moistureDecayDistance);
                        
                        // 区分河流和湖泊的影响（简化版本）
                        bool isLake = distanceToWater < moistureDecayDistance * 0.3f; // 假设近距离为湖泊
                        float moistureInfluence = isLake ? lakeMoistureInfluence : riverMoistureInfluence;
                        
                        moistureMap[x, y] = Mathf.Min(1f, moistureMap[x, y] + influence * moistureInfluence);
                    }
                }
            }
        }
        
        /// <summary>
        /// 添加雨影效应
        /// </summary>
        void AddRainShadowEffect(float[,] moistureMap, float[,] heightMap)
        {
            int resolution = moistureMap.GetLength(0);
            Vector2 windDirection = new Vector2(1f, 0f); // 假设风向为东风
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    // 检查迎风坡和背风坡
                    float rainShadowEffect = CalculateRainShadowEffect(heightMap, x, y, windDirection, resolution);
                    moistureMap[x, y] *= rainShadowEffect;
                }
            }
        }
        
        /// <summary>
        /// 计算雨影效应
        /// </summary>
        float CalculateRainShadowEffect(float[,] heightMap, int x, int y, Vector2 windDirection, int resolution)
        {
            float currentHeight = heightMap[x, y];
            
            // 沿风向检查前方地形
            for (int step = 1; step <= 10; step++)
            {
                int checkX = x - Mathf.RoundToInt(windDirection.x * step);
                int checkY = y - Mathf.RoundToInt(windDirection.y * step);
                
                if (checkX < 0 || checkX >= resolution || checkY < 0 || checkY >= resolution)
                    break;
                
                float checkHeight = heightMap[checkX, checkY];
                
                // 如果前方有更高的地形，形成雨影
                if (checkHeight > currentHeight + 0.1f)
                {
                    float shadowStrength = (checkHeight - currentHeight) * 0.5f;
                    return Mathf.Max(0.2f, 1f - shadowStrength);
                }
            }
            
            return 1f; // 无雨影影响
        }
        
        /// <summary>
        /// 寻找最近水体距离（简化版本）
        /// </summary>
        float FindDistanceToNearestWater(int x, int y, int resolution)
        {
            // 这里应该集成实际的水体数据
            // 目前返回一个基于位置的估算值
            
            float centerX = resolution * 0.5f;
            float centerY = resolution * 0.5f;
            
            float distanceToCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
            
            // 假设中心有一个湖泊
            return Mathf.Max(0f, distanceToCenter - resolution * 0.1f);
        }
        
        /// <summary>
        /// 分析温度
        /// </summary>
        float[,] AnalyzeTemperature(float[,] heightMap, WorldGenerationParameters parameters)
        {
            int resolution = heightMap.GetLength(0);
            float[,] temperatureMap = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float height = heightMap[x, y];
                    float latitude = (float)y / resolution; // 假设y轴代表纬度
                    
                    // 基于高度的温度衰减
                    float heightEffect = height * (temperatureLapseRate / 100f);
                    
                    // 基于纬度的温度变化
                    float latitudeEffect = Mathf.Cos(latitude * Mathf.PI) * 0.3f;
                    
                    // 季节变化（如果有的话）
                    float seasonalEffect = 0f;
                    if (seasonalVariation != null)
                    {
                        seasonalEffect = seasonalVariation.Evaluate(Time.time * 0.1f) * 0.2f;
                    }
                    
                    // 阴影效应（如果启用的话）
                    float shadowEffect = 0f;
                    if (calculateShadows)
                    {
                        shadowEffect = CalculateShadowEffect(heightMap, x, y, resolution) * 5f; // 阴影降温效应
                    }
                    
                    // 计算最终温度
                    float temperature = baseTemperature - heightEffect + latitudeEffect + seasonalEffect - shadowEffect;
                    
                    // 标准化到0-1范围（假设温度范围为0-40度）
                    temperatureMap[x, y] = Mathf.Clamp01(temperature / 40f);
                }
            }
            
            return temperatureMap;
        }
        
        /// <summary>
        /// 计算阴影效应
        /// </summary>
        float CalculateShadowEffect(float[,] heightMap, int x, int y, int resolution)
        {
            float currentHeight = heightMap[x, y];
            float shadowIntensity = 0f;
            
            // 沿太阳方向检查阴影
            Vector3 normalizedSunDir = sunDirection.normalized;
            
            for (int step = 1; step <= shadowRaySteps; step++)
            {
                int checkX = x - Mathf.RoundToInt(normalizedSunDir.x * step);
                int checkY = y - Mathf.RoundToInt(normalizedSunDir.z * step);
                
                if (checkX < 0 || checkX >= resolution || checkY < 0 || checkY >= resolution)
                    break;
                
                float checkHeight = heightMap[checkX, checkY];
                float expectedHeight = currentHeight + normalizedSunDir.y * step * 0.1f;
                
                // 如果检查点比预期高度高，说明在阴影中
                if (checkHeight > expectedHeight)
                {
                    shadowIntensity += (checkHeight - expectedHeight) * 0.1f;
                }
            }
            
            return Mathf.Clamp01(shadowIntensity);
        }
        
        /// <summary>
        /// 分析光照暴露度
        /// </summary>
        float[,] AnalyzeExposure(float[,] heightMap, WorldGenerationParameters parameters)
        {
            int resolution = heightMap.GetLength(0);
            float[,] exposureMap = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    exposureMap[x, y] = CalculateExposure(heightMap, x, y, resolution);
                }
            }
            
            return exposureMap;
        }
        
        /// <summary>
        /// 计算单点光照暴露度
        /// </summary>
        float CalculateExposure(float[,] heightMap, int x, int y, int resolution)
        {
            float currentHeight = heightMap[x, y];
            float exposure = 1f; // 基础暴露度
            
            // 检查东、南、西、北四个主要方向的遮挡
            Vector2[] directions = {
                new Vector2(1, 0),   // 东
                new Vector2(0, 1),   // 南  
                new Vector2(-1, 0),  // 西
                new Vector2(0, -1)   // 北
            };
            
            float totalExposure = 0f;
            
            foreach (var direction in directions)
            {
                float directionExposure = CalculateDirectionalExposure(heightMap, x, y, direction, resolution, currentHeight);
                totalExposure += directionExposure;
            }
            
            // 平均四个方向的暴露度
            exposure = totalExposure / directions.Length;
            
            // 考虑坡度对暴露度的影响
            float slope = CalculateSlope(heightMap, x, y, resolution);
            float slopeModifier = 1f + slope * 0.3f; // 坡度增加暴露度
            
            return Mathf.Clamp01(exposure * slopeModifier);
        }
        
        /// <summary>
        /// 计算指定方向的暴露度
        /// </summary>
        float CalculateDirectionalExposure(float[,] heightMap, int startX, int startY, Vector2 direction, int resolution, float startHeight)
        {
            float exposure = 1f;
            int maxDistance = 20; // 检查距离
            
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                int checkX = startX + Mathf.RoundToInt(direction.x * distance);
                int checkY = startY + Mathf.RoundToInt(direction.y * distance);
                
                // 边界检查
                if (checkX < 0 || checkX >= resolution || checkY < 0 || checkY >= resolution)
                    break;
                
                float checkHeight = heightMap[checkX, checkY];
                float heightDifference = checkHeight - startHeight;
                
                // 如果远处有更高的地形，会产生遮挡
                if (heightDifference > distance * 0.05f) // 考虑距离衰减
                {
                    float blockage = Mathf.Clamp01(heightDifference / (distance * 0.1f));
                    exposure = Mathf.Min(exposure, 1f - blockage * 0.5f);
                }
            }
            
            return exposure;
        }
        
        /// <summary>
        /// 平滑地图数据
        /// </summary>
        float[,] SmoothMap(float[,] map, int radius)
        {
            int resolution = map.GetLength(0);
            float[,] smoothedMap = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    smoothedMap[x, y] = SmoothPoint(map, x, y, radius, resolution);
                }
            }
            
            return smoothedMap;
        }
        
        /// <summary>
        /// 平滑单点
        /// </summary>
        float SmoothPoint(float[,] map, int centerX, int centerY, int radius, int resolution)
        {
            float sum = 0f;
            int count = 0;
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        sum += map[x, y];
                        count++;
                    }
                }
            }
            
            return count > 0 ? sum / count : map[centerX, centerY];
        }
        
        /// <summary>
        /// 获取分析统计信息
        /// </summary>
        public string GetAnalysisStats(TerrainData terrainData)
        {
            if (terrainData == null) return "无数据";
            
            var heightStats = CalculateMapStats(terrainData.heightMap);
            var slopeStats = CalculateMapStats(terrainData.slopeMap);
            var moistureStats = CalculateMapStats(terrainData.moistureMap);
            var temperatureStats = CalculateMapStats(terrainData.temperatureMap);
            
            return $"地形分析统计:\n" +
                   $"高度: 平均={heightStats.average:F3}, 最小={heightStats.min:F3}, 最大={heightStats.max:F3}\n" +
                   $"坡度: 平均={slopeStats.average:F3}, 最小={slopeStats.min:F3}, 最大={slopeStats.max:F3}\n" +
                   $"湿度: 平均={moistureStats.average:F3}, 最小={moistureStats.min:F3}, 最大={moistureStats.max:F3}\n" +
                   $"温度: 平均={temperatureStats.average:F3}, 最小={temperatureStats.min:F3}, 最大={temperatureStats.max:F3}";
        }
        
        MapStats CalculateMapStats(float[,] map)
        {
            if (map == null) return new MapStats();
            
            int resolution = map.GetLength(0);
            float sum = 0f;
            float min = float.MaxValue;
            float max = float.MinValue;
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float value = map[x, y];
                    sum += value;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                }
            }
            
            return new MapStats
            {
                average = sum / (resolution * resolution),
                min = min,
                max = max
            };
        }
        
        struct MapStats
        {
            public float average;
            public float min;
            public float max;
        }
    }
}