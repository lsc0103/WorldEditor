using UnityEngine;
using System.Collections;
using WorldEditor.Core;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 高级噪声生成器 - 支持多种噪声类型和分层噪声
    /// </summary>
    public class NoiseGenerator : MonoBehaviour
    {
        [Header("噪声设置")]
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useRandomSeed = true;
        
        // 噪声计算缓存
        private System.Random random;
        private float[,] permutationTable;
        
        void Awake()
        {
            InitializeNoise();
        }
        
        void InitializeNoise()
        {
            if (useRandomSeed)
                seed = System.DateTime.Now.GetHashCode();
                
            random = new System.Random(seed);
            GeneratePermutationTable();
        }
        
        void GeneratePermutationTable()
        {
            permutationTable = new float[256, 256];
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    permutationTable[x, y] = (float)random.NextDouble();
                }
            }
        }
        
        /// <summary>
        /// 生成高度图（同步版本）
        /// </summary>
        public void GenerateHeightMap(ref float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    float height = CalculateHeightAtPosition(x, y, resolution, parameters);
                    heightMap[x, y] = height;
                }
            }
        }
        
        /// <summary>
        /// 生成高度图（异步版本）
        /// </summary>
        public IEnumerator GenerateHeightMapProgressive(float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters, int stepsPerFrame)
        {
            Debug.Log($"[NoiseGenerator] 开始生成{resolution.x}x{resolution.y}高度图，每帧{stepsPerFrame}步");
            
            int totalPixels = resolution.x * resolution.y;
            int processedPixels = 0;
            int currentSteps = 0;
            int progressReportInterval = totalPixels / 10; // 每10%报告一次进度
            int nextProgressReport = progressReportInterval;
            
            for (int x = 0; x < resolution.x; x++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    float height = CalculateHeightAtPosition(x, y, resolution, parameters);
                    heightMap[x, y] = height;
                    
                    processedPixels++;
                    currentSteps++;
                    
                    // 进度报告
                    if (processedPixels >= nextProgressReport)
                    {
                        float progress = (float)processedPixels / totalPixels * 100f;
                        Debug.Log($"[NoiseGenerator] 高度图生成进度: {progress:F1}%");
                        nextProgressReport += progressReportInterval;
                    }
                    
                    if (currentSteps >= stepsPerFrame)
                    {
                        currentSteps = 0;
                        yield return null;
                    }
                }
            }
            
            Debug.Log($"[NoiseGenerator] 高度图生成完成，共处理{processedPixels}个像素");
        }
        
        /// <summary>
        /// 计算指定位置的高度值
        /// </summary>
        float CalculateHeightAtPosition(int x, int y, Vector2Int resolution, TerrainGenerationParams parameters)
        {
            if (parameters.noiseLayers == null || parameters.noiseLayers.Length == 0)
            {
                Debug.LogError("[NoiseGenerator] 噪声层为空或未初始化！");
                return 0.5f; // 返回中等高度
            }
            
            float totalHeight = 0f;
            float totalWeight = 0f;
            
            // 应用所有噪声层
            for (int i = 0; i < parameters.noiseLayers.Length; i++)
            {
                var layer = parameters.noiseLayers[i];
                if (layer == null)
                {
                    Debug.LogWarning($"[NoiseGenerator] 噪声层[{i}]为空！");
                    continue;
                }
                
                if (layer.weight <= 0f) continue;
                
                float layerHeight = GenerateNoiseLayer(x, y, resolution, layer);
                totalHeight += layerHeight * layer.weight;
                totalWeight += layer.weight;
            }
            
            // 标准化高度
            if (totalWeight > 0f)
                totalHeight /= totalWeight;
            
            // 应用高度衰减曲线（在应用基础高度之前）
            if (parameters.heightFalloff != null)
            {
                float distanceFromCenter = CalculateDistanceFromCenter(x, y, resolution);
                float falloffMultiplier = parameters.heightFalloff.Evaluate(distanceFromCenter);
                totalHeight *= falloffMultiplier;
            }
            
            // 应用基础高度和高度变化
            totalHeight = parameters.baseHeight / parameters.heightVariation + totalHeight;
            
            return Mathf.Clamp01(totalHeight);
        }
        
        /// <summary>
        /// 生成单个噪声层
        /// </summary>
        float GenerateNoiseLayer(int x, int y, Vector2Int resolution, NoiseLayerSettings layer)
        {
            switch (layer.noiseType)
            {
                case NoiseType.Perlin:
                    return GeneratePerlinNoise(x, y, resolution, layer);
                case NoiseType.Simplex:
                    return GenerateSimplexNoise(x, y, resolution, layer);
                case NoiseType.Ridged:
                    return GenerateRidgedNoise(x, y, resolution, layer);
                case NoiseType.Cellular:
                    return GenerateCellularNoise(x, y, resolution, layer);
                case NoiseType.Voronoi:
                    return GenerateVoronoiNoise(x, y, resolution, layer);
                default:
                    return GeneratePerlinNoise(x, y, resolution, layer);
            }
        }
        
        /// <summary>
        /// Perlin噪声生成
        /// </summary>
        float GeneratePerlinNoise(int x, int y, Vector2Int resolution, NoiseLayerSettings layer)
        {
            float noise = 0f;
            float amplitude = layer.amplitude;
            float frequency = layer.frequency;
            
            for (int octave = 0; octave < layer.octaves; octave++)
            {
                float sampleX = (x + layer.offset.x) * frequency;
                float sampleY = (y + layer.offset.y) * frequency;
                
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noise += perlinValue * amplitude;
                
                amplitude *= layer.persistence;
                frequency *= layer.lacunarity;
            }
            
            return noise;
        }
        
        /// <summary>
        /// Simplex噪声生成（简化版本）
        /// </summary>
        float GenerateSimplexNoise(int x, int y, Vector2Int resolution, NoiseLayerSettings layer)
        {
            // 这里使用简化的Simplex噪声实现
            // 实际项目中可以使用更完整的Simplex噪声库
            float noise = 0f;
            float amplitude = layer.amplitude;
            float frequency = layer.frequency;
            
            for (int octave = 0; octave < layer.octaves; octave++)
            {
                float sampleX = (x + layer.offset.x) * frequency;
                float sampleY = (y + layer.offset.y) * frequency;
                
                // 简化的Simplex噪声，实际实现会更复杂
                float simplexValue = SimplexNoise(sampleX, sampleY);
                noise += simplexValue * amplitude;
                
                amplitude *= layer.persistence;
                frequency *= layer.lacunarity;
            }
            
            return noise;
        }
        
        /// <summary>
        /// 山脊噪声生成
        /// </summary>
        float GenerateRidgedNoise(int x, int y, Vector2Int resolution, NoiseLayerSettings layer)
        {
            float noise = 0f;
            float amplitude = layer.amplitude;
            float frequency = layer.frequency;
            
            for (int octave = 0; octave < layer.octaves; octave++)
            {
                float sampleX = (x + layer.offset.x) * frequency;
                float sampleY = (y + layer.offset.y) * frequency;
                
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                // 创建山脊效果
                perlinValue = 1f - Mathf.Abs(perlinValue * 2f - 1f);
                noise += perlinValue * amplitude;
                
                amplitude *= layer.persistence;
                frequency *= layer.lacunarity;
            }
            
            return noise;
        }
        
        /// <summary>
        /// 细胞噪声生成
        /// </summary>
        float GenerateCellularNoise(int x, int y, Vector2Int resolution, NoiseLayerSettings layer)
        {
            float cellSize = 1f / layer.frequency;
            int cellX = Mathf.FloorToInt((x + layer.offset.x) / cellSize);
            int cellY = Mathf.FloorToInt((y + layer.offset.y) / cellSize);
            
            float minDistance = float.MaxValue;
            
            // 检查周围的细胞
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int neighborCellX = cellX + offsetX;
                    int neighborCellY = cellY + offsetY;
                    
                    // 获取细胞中心点
                    Vector2 cellCenter = GetCellCenter(neighborCellX, neighborCellY, cellSize);
                    
                    // 计算距离
                    float distance = Vector2.Distance(new Vector2(x, y), cellCenter);
                    minDistance = Mathf.Min(minDistance, distance);
                }
            }
            
            return (minDistance / cellSize) * layer.amplitude;
        }
        
        /// <summary>
        /// Voronoi噪声生成
        /// </summary>
        float GenerateVoronoiNoise(int x, int y, Vector2Int resolution, NoiseLayerSettings layer)
        {
            float cellSize = 1f / layer.frequency;
            int cellX = Mathf.FloorToInt((x + layer.offset.x) / cellSize);
            int cellY = Mathf.FloorToInt((y + layer.offset.y) / cellSize);
            
            float minDistance = float.MaxValue;
            Vector2 closestPoint = Vector2.zero;
            
            // 检查周围的细胞
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int neighborCellX = cellX + offsetX;
                    int neighborCellY = cellY + offsetY;
                    
                    Vector2 cellCenter = GetCellCenter(neighborCellX, neighborCellY, cellSize);
                    float distance = Vector2.Distance(new Vector2(x, y), cellCenter);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = cellCenter;
                    }
                }
            }
            
            // 返回基于最近点的值
            return GetCellValue(closestPoint) * layer.amplitude;
        }
        
        // 辅助方法
        float SimplexNoise(float x, float y)
        {
            // 简化的Simplex噪声实现
            // 实际应用中建议使用专门的Simplex噪声库
            return Mathf.PerlinNoise(x * 0.8f, y * 0.8f) * 0.7f + 
                   Mathf.PerlinNoise(x * 1.3f + 100f, y * 1.3f + 100f) * 0.3f;
        }
        
        Vector2 GetCellCenter(int cellX, int cellY, float cellSize)
        {
            // 使用伪随机数生成细胞中心点
            System.Random cellRandom = new System.Random((cellX * 73856093) ^ (cellY * 19349663));
            
            float centerX = cellX * cellSize + (float)cellRandom.NextDouble() * cellSize;
            float centerY = cellY * cellSize + (float)cellRandom.NextDouble() * cellSize;
            
            return new Vector2(centerX, centerY);
        }
        
        float GetCellValue(Vector2 cellCenter)
        {
            // 基于细胞中心位置生成值
            int hash = ((int)cellCenter.x * 73856093) ^ ((int)cellCenter.y * 19349663);
            System.Random cellRandom = new System.Random(hash);
            return (float)cellRandom.NextDouble();
        }
        
        float CalculateDistanceFromCenter(int x, int y, Vector2Int resolution)
        {
            float centerX = resolution.x * 0.5f;
            float centerY = resolution.y * 0.5f;
            
            float distanceX = (x - centerX) / centerX;
            float distanceY = (y - centerY) / centerY;
            
            return Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);
        }
        
        /// <summary>
        /// 设置噪声种子
        /// </summary>
        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            InitializeNoise();
        }
        
        /// <summary>
        /// 获取当前种子
        /// </summary>
        public int GetSeed()
        {
            return seed;
        }
    }
}