using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 纹理混合器 - 智能地形纹理分布和混合
    /// 基于高度、坡度、湿度等因素自动分配地形纹理
    /// </summary>
    public class TextureBlender : MonoBehaviour
    {
        [Header("混合设置")]
        [SerializeField] private float blendSharpness = 8f;
        [SerializeField] private bool enableHeightBasedBlending = true;
        [SerializeField] private bool enableSlopeBasedBlending = true;
        [SerializeField] private bool enableMoistureBasedBlending = true;
        [SerializeField] private bool enableTemperatureBasedBlending = true;
        
        [Header("高度层")]
        [SerializeField] private TextureLayer[] heightLayers = new TextureLayer[4];
        
        [Header("坡度层")]
        [SerializeField] private TextureLayer rockLayer;
        [SerializeField] private float rockSlopeThreshold = 0.7f;
        
        [Header("环境层")]
        [SerializeField] private TextureLayer wetLayer;      // 湿润区域纹理
        [SerializeField] private TextureLayer dryLayer;     // 干燥区域纹理
        [SerializeField] private TextureLayer coldLayer;    // 寒冷区域纹理
        [SerializeField] private TextureLayer hotLayer;     // 炎热区域纹理
        
        // 纹理数据结构
        [System.Serializable]
        public class TextureLayer
        {
            public TerrainLayer terrainLayer;
            public float minHeight = 0f;
            public float maxHeight = 1f;
            public float minSlope = 0f;
            public float maxSlope = 90f;
            public float minMoisture = 0f;
            public float maxMoisture = 1f;
            public float minTemperature = -50f;
            public float maxTemperature = 50f;
            public float strength = 1f;
            public AnimationCurve distributionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        
        // 环境数据
        private float[,] moistureMap;
        private float[,] temperatureMap;
        private float[,] slopeMap;
        
        /// <summary>
        /// 应用纹理（同步版本）
        /// </summary>
        public void ApplyTextures(TerrainData terrainData, float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters)
        {
            Debug.Log("[TextureBlender] ========== 开始应用地形纹理 ==========");
            Debug.Log($"[TextureBlender] 当前纹理层数量: {(terrainData.terrainLayers?.Length ?? 0)}");
            Debug.Log($"[TextureBlender] 接收到的生物群系参数: {parameters.biome}");
            
            // 检查是否有现有纹理层，如果有则清除重新创建多层纹理
            bool hasExistingLayers = terrainData.terrainLayers != null && terrainData.terrainLayers.Length > 0;
            if (hasExistingLayers)
            {
                Debug.Log("[TextureBlender] 地形已有纹理层，将重新创建多层纹理以提供更好的视觉效果");
            }
            
            try
            {
                Debug.Log("[TextureBlender] 创建多层纹理系统");
                
                // 总是创建默认的多层纹理系统
                terrainData.terrainLayers = CreateDefaultTerrainLayers();
                
                // 使用多层纹理混合
                Debug.Log("[TextureBlender] 应用多层纹理混合");
                SetupDefaultAlphamap(terrainData, resolution);
                Debug.Log("[TextureBlender] 地形多层纹理应用完成");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TextureBlender] 纹理应用失败，使用默认纹理: {e.Message}");
                // 确保至少有基本纹理
                EnsureDefaultTerrainLayer(terrainData);
                SetupDefaultAlphamap(terrainData, resolution);
                Debug.Log("[TextureBlender] 地形纹理应用完成（fallback到默认）");
            }
        }
        
        /// <summary>
        /// 应用纹理（异步版本）
        /// </summary>
        public IEnumerator ApplyTexturesProgressive(TerrainData terrainData, float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters, int stepsPerFrame)
        {
            Debug.Log("[TextureBlender] 开始渐进式应用地形纹理...");
            
            // 生成环境数据
            yield return StartCoroutine(GenerateEnvironmentMapsProgressive(heightMap, resolution, parameters, stepsPerFrame));
            
            // 设置地形层
            SetupTerrainLayers(terrainData, parameters);
            
            // 计算混合权重
            yield return StartCoroutine(CalculateBlendWeightsProgressive(heightMap, resolution, parameters, stepsPerFrame, 
                (blendWeights) => {
                    // 应用到地形
                    terrainData.SetAlphamaps(0, 0, blendWeights);
                    Debug.Log("[TextureBlender] 地形纹理应用完成");
                }));
        }
        
        /// <summary>
        /// 生成环境贴图
        /// </summary>
        void GenerateEnvironmentMaps(float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters)
        {
            moistureMap = new float[resolution.x, resolution.y];
            temperatureMap = new float[resolution.x, resolution.y];
            slopeMap = new float[resolution.x, resolution.y];
            
            for (int x = 0; x < resolution.x; x++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    // 计算湿度（基于高度和距离水体的距离）
                    moistureMap[x, y] = CalculateMoisture(heightMap, resolution, x, y, parameters);
                    
                    // 计算温度（基于高度和纬度）
                    temperatureMap[x, y] = CalculateTemperature(heightMap, resolution, x, y, parameters);
                    
                    // 计算坡度
                    slopeMap[x, y] = CalculateSlope(heightMap, resolution, x, y);
                }
            }
        }
        
        /// <summary>
        /// 生成环境贴图（异步版本）
        /// </summary>
        IEnumerator GenerateEnvironmentMapsProgressive(float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters, int stepsPerFrame)
        {
            moistureMap = new float[resolution.x, resolution.y];
            temperatureMap = new float[resolution.x, resolution.y];
            slopeMap = new float[resolution.x, resolution.y];
            
            int processedCells = 0;
            
            for (int x = 0; x < resolution.x; x++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    moistureMap[x, y] = CalculateMoisture(heightMap, resolution, x, y, parameters);
                    temperatureMap[x, y] = CalculateTemperature(heightMap, resolution, x, y, parameters);
                    slopeMap[x, y] = CalculateSlope(heightMap, resolution, x, y);
                    
                    processedCells++;
                    if (processedCells >= stepsPerFrame)
                    {
                        processedCells = 0;
                        yield return null;
                    }
                }
            }
        }
        
        /// <summary>
        /// 计算湿度
        /// </summary>
        float CalculateMoisture(float[,] heightMap, Vector2Int resolution, int x, int y, TerrainGenerationParams parameters)
        {
            float height = heightMap[x, y];
            
            // 基础湿度（高度越低湿度越高）
            float baseMoisture = 1f - height;
            
            // 添加噪声变化
            float moistureNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.3f;
            
            // 根据生物群落调整
            float biomeMoisture = GetBiomeMoisture(parameters.biome);
            
            // 调试信息：第一次计算时输出生物群系影响
            if (x == 0 && y == 0)
            {
                Debug.Log($"[TextureBlender] 生物群系 {parameters.biome} 的湿度系数: {biomeMoisture}");
            }
            
            float finalMoisture = (baseMoisture + moistureNoise) * biomeMoisture;
            return Mathf.Clamp01(finalMoisture);
        }
        
        /// <summary>
        /// 计算温度
        /// </summary>
        float CalculateTemperature(float[,] heightMap, Vector2Int resolution, int x, int y, TerrainGenerationParams parameters)
        {
            float height = heightMap[x, y];
            
            // 基于高度的温度（高度越高温度越低）
            float heightTemperature = 1f - (height * 0.8f);
            
            // 基于纬度的温度（假设y轴代表纬度）
            float latitude = (float)y / resolution.y;
            float latitudeTemperature = 1f - Mathf.Abs(latitude - 0.5f) * 2f;
            
            // 添加温度噪声
            float temperatureNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.2f;
            
            // 根据生物群落调整
            float biomeTemperature = GetBiomeTemperature(parameters.biome);
            
            // 调试信息：第一次计算时输出生物群系影响
            if (x == 0 && y == 0)
            {
                Debug.Log($"[TextureBlender] 生物群系 {parameters.biome} 的温度系数: {biomeTemperature}");
            }
            
            float finalTemperature = (heightTemperature * 0.4f + latitudeTemperature * 0.4f + temperatureNoise) * biomeTemperature;
            return Mathf.Clamp01(finalTemperature);
        }
        
        /// <summary>
        /// 计算坡度
        /// </summary>
        float CalculateSlope(float[,] heightMap, Vector2Int resolution, int x, int y)
        {
            // 边界处理
            int x1 = Mathf.Max(0, x - 1);
            int x2 = Mathf.Min(resolution.x - 1, x + 1);
            int y1 = Mathf.Max(0, y - 1);
            int y2 = Mathf.Min(resolution.y - 1, y + 1);
            
            // 计算梯度
            float gradientX = heightMap[x2, y] - heightMap[x1, y];
            float gradientY = heightMap[x, y2] - heightMap[x, y1];
            
            // 计算坡度角度（弧度转度数）
            float slope = Mathf.Atan(Mathf.Sqrt(gradientX * gradientX + gradientY * gradientY)) * Mathf.Rad2Deg;
            
            return slope / 90f; // 标准化到0-1范围
        }
        
        /// <summary>
        /// 设置地形层
        /// </summary>
        void SetupTerrainLayers(TerrainData terrainData, TerrainGenerationParams parameters)
        {
            // 创建地形层数组
            var layers = new System.Collections.Generic.List<TerrainLayer>();
            
            // 安全检查heightLayers数组
            if (heightLayers != null)
            {
                // 添加高度层
                foreach (var heightLayer in heightLayers)
                {
                    if (heightLayer != null && heightLayer.terrainLayer != null)
                        layers.Add(heightLayer.terrainLayer);
                }
            }
            
            // 添加特殊层（安全检查）
            if (rockLayer != null && rockLayer.terrainLayer != null)
                layers.Add(rockLayer.terrainLayer);
            if (wetLayer != null && wetLayer.terrainLayer != null)
                layers.Add(wetLayer.terrainLayer);
            if (dryLayer != null && dryLayer.terrainLayer != null)
                layers.Add(dryLayer.terrainLayer);
            if (coldLayer != null && coldLayer.terrainLayer != null)
                layers.Add(coldLayer.terrainLayer);
            if (hotLayer != null && hotLayer.terrainLayer != null)
                layers.Add(hotLayer.terrainLayer);
            
            // 如果没有设置层，创建默认层
            if (layers.Count == 0)
            {
                Debug.Log("[TextureBlender] 未配置纹理层，创建默认纹理层");
                layers.AddRange(CreateDefaultTerrainLayers());
            }
            
            if (layers.Count > 0)
            {
                terrainData.terrainLayers = layers.ToArray();
                Debug.Log($"[TextureBlender] 设置了{layers.Count}个地形纹理层");
            }
            else
            {
                Debug.LogWarning("[TextureBlender] 无法创建任何地形纹理层");
            }
        }
        
        /// <summary>
        /// 计算混合权重
        /// </summary>
        float[,,] CalculateBlendWeights(float[,] heightMap, Vector2Int alphamapResolution, TerrainGenerationParams parameters)
        {
            int layerCount = GetActiveLayerCount();
            float[,,] weights = new float[alphamapResolution.x, alphamapResolution.y, layerCount];
            
            // 获取高度图的分辨率
            Vector2Int heightmapResolution = new Vector2Int(heightMap.GetLength(0), heightMap.GetLength(1));
            
            Debug.Log($"[TextureBlender] 开始计算混合权重 - Alphamap: {alphamapResolution.x}x{alphamapResolution.y}, Heightmap: {heightmapResolution.x}x{heightmapResolution.y}");
            
            for (int x = 0; x < alphamapResolution.x; x++)
            {
                for (int y = 0; y < alphamapResolution.y; y++)
                {
                    // 将alphamap坐标映射到heightmap坐标
                    float heightX = (float)x / alphamapResolution.x * heightmapResolution.x;
                    float heightY = (float)y / alphamapResolution.y * heightmapResolution.y;
                    
                    int hx = Mathf.Clamp(Mathf.FloorToInt(heightX), 0, heightmapResolution.x - 1);
                    int hy = Mathf.Clamp(Mathf.FloorToInt(heightY), 0, heightmapResolution.y - 1);
                    
                    CalculatePixelWeights(heightMap, heightmapResolution, hx, hy, parameters, weights, layerCount, x, y);
                }
            }
            
            Debug.Log($"[TextureBlender] 混合权重计算完成");
            return weights;
        }
        
        /// <summary>
        /// 计算混合权重（异步版本）
        /// </summary>
        IEnumerator CalculateBlendWeightsProgressive(float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters, int stepsPerFrame, System.Action<float[,,]> onComplete)
        {
            int layerCount = GetActiveLayerCount();
            float[,,] weights = new float[resolution.x, resolution.y, layerCount];
            int processedCells = 0;
            
            for (int x = 0; x < resolution.x; x++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    CalculatePixelWeights(heightMap, resolution, x, y, parameters, weights, layerCount);
                    
                    processedCells++;
                    if (processedCells >= stepsPerFrame)
                    {
                        processedCells = 0;
                        yield return null;
                    }
                }
            }
            
            onComplete?.Invoke(weights);
        }
        
        /// <summary>
        /// 计算单个像素的权重
        /// </summary>
        void CalculatePixelWeights(float[,] heightMap, Vector2Int heightmapResolution, int hx, int hy, TerrainGenerationParams parameters, float[,,] weights, int layerCount, int wx = -1, int wy = -1)
        {
            // 如果没有指定权重坐标，使用高度图坐标
            if (wx == -1) wx = hx;
            if (wy == -1) wy = hy;
            
            float height = heightMap[hx, hy];
            float slope = CalculateSlope(heightMap, heightmapResolution, hx, hy);
            float moisture = CalculateMoisture(heightMap, heightmapResolution, hx, hy, parameters);
            float temperature = CalculateTemperature(heightMap, heightmapResolution, hx, hy, parameters);
            
            float[] layerWeights = new float[layerCount];
            int layerIndex = 0;
            
            // 计算高度层权重
            if (heightLayers != null)
            {
                foreach (var heightLayer in heightLayers)
                {
                    if (heightLayer == null || heightLayer.terrainLayer == null) continue;
                    
                    float weight = CalculateLayerWeight(height, slope, moisture, temperature, heightLayer);
                    layerWeights[layerIndex] = weight;
                    layerIndex++;
                }
            }
            
            // 计算岩石层权重
            if (rockLayer != null && rockLayer.terrainLayer != null)
            {
                float rockWeight = slope > rockSlopeThreshold ? (slope - rockSlopeThreshold) / (1f - rockSlopeThreshold) : 0f;
                layerWeights[layerIndex] = rockWeight * rockLayer.strength;
                layerIndex++;
            }
            
            // 计算环境层权重
            if (wetLayer != null && wetLayer.terrainLayer != null)
            {
                layerWeights[layerIndex] = moisture * wetLayer.strength;
                layerIndex++;
            }
            
            if (dryLayer != null && dryLayer.terrainLayer != null)
            {
                layerWeights[layerIndex] = (1f - moisture) * dryLayer.strength;
                layerIndex++;
            }
            
            if (coldLayer != null && coldLayer.terrainLayer != null)
            {
                layerWeights[layerIndex] = (1f - temperature) * coldLayer.strength;
                layerIndex++;
            }
            
            if (hotLayer != null && hotLayer.terrainLayer != null)
            {
                layerWeights[layerIndex] = temperature * hotLayer.strength;
                layerIndex++;
            }
            
            // 标准化权重
            NormalizeWeights(layerWeights);
            
            // 应用到权重数组
            for (int i = 0; i < layerCount; i++)
            {
                weights[wx, wy, i] = layerWeights[i];
            }
        }
        
        /// <summary>
        /// 计算层权重
        /// </summary>
        float CalculateLayerWeight(float height, float slope, float moisture, float temperature, TextureLayer layer)
        {
            float weight = 1f;
            
            // 高度权重
            if (enableHeightBasedBlending)
            {
                if (height < layer.minHeight || height > layer.maxHeight)
                    weight *= 0f;
                else
                {
                    float heightWeight = layer.distributionCurve.Evaluate((height - layer.minHeight) / (layer.maxHeight - layer.minHeight));
                    weight *= heightWeight;
                }
            }
            
            // 坡度权重
            if (enableSlopeBasedBlending)
            {
                float slopeDegrees = slope * 90f;
                if (slopeDegrees < layer.minSlope || slopeDegrees > layer.maxSlope)
                    weight *= 0.1f;
            }
            
            // 湿度权重
            if (enableMoistureBasedBlending)
            {
                if (moisture < layer.minMoisture || moisture > layer.maxMoisture)
                    weight *= 0.1f;
            }
            
            // 温度权重
            if (enableTemperatureBasedBlending)
            {
                if (temperature < layer.minTemperature / 100f || temperature > layer.maxTemperature / 100f)
                    weight *= 0.1f;
            }
            
            return weight * layer.strength;
        }
        
        /// <summary>
        /// 标准化权重
        /// </summary>
        void NormalizeWeights(float[] weights)
        {
            // 应用锐化效果增强对比度
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Pow(weights[i], blendSharpness);
            }
            
            float totalWeight = 0f;
            
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }
            
            if (totalWeight > 0f)
            {
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] /= totalWeight;
                }
            }
            else
            {
                // 如果没有权重，均匀分布
                float evenWeight = 1f / weights.Length;
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = evenWeight;
                }
            }
        }
        
        /// <summary>
        /// 获取活动层数量
        /// </summary>
        int GetActiveLayerCount()
        {
            int count = 0;
            
            // 安全检查heightLayers数组
            if (heightLayers != null)
            {
                foreach (var layer in heightLayers)
                {
                    if (layer != null && layer.terrainLayer != null) count++;
                }
            }
            
            // 安全检查其他层
            if (rockLayer != null && rockLayer.terrainLayer != null) count++;
            if (wetLayer != null && wetLayer.terrainLayer != null) count++;
            if (dryLayer != null && dryLayer.terrainLayer != null) count++;
            if (coldLayer != null && coldLayer.terrainLayer != null) count++;
            if (hotLayer != null && hotLayer.terrainLayer != null) count++;
            
            return Mathf.Max(1, count); // 至少有一层
        }
        
        /// <summary>
        /// 确保地形有默认纹理层
        /// </summary>
        void EnsureDefaultTerrainLayer(TerrainData terrainData)
        {
            if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
            {
                Debug.Log("[TextureBlender] 创建默认地形纹理层");
                terrainData.terrainLayers = CreateDefaultTerrainLayers();
            }
        }
        
        /// <summary>
        /// 设置默认的alpha贴图（基于高度混合多层纹理）
        /// </summary>
        void SetupDefaultAlphamap(TerrainData terrainData, Vector2Int resolution)
        {
            if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
                return;
                
            int layerCount = terrainData.terrainLayers.Length;
            
            // 使用地形的alphamap分辨率
            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            float[,,] alphaMap = new float[alphamapWidth, alphamapHeight, layerCount];
            
            // 获取地形高度数据进行基于高度的纹理分布
            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            
            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int y = 0; y < alphamapHeight; y++)
                {
                    // 将alphamap坐标映射到heightmap坐标
                    float heightX = (float)x / alphamapWidth * terrainData.heightmapResolution;
                    float heightY = (float)y / alphamapHeight * terrainData.heightmapResolution;
                    
                    int hx = Mathf.Clamp(Mathf.FloorToInt(heightX), 0, terrainData.heightmapResolution - 1);
                    int hy = Mathf.Clamp(Mathf.FloorToInt(heightY), 0, terrainData.heightmapResolution - 1);
                    
                    float height = heights[hx, hy];
                    
                    // 计算坡度（简化版本）
                    float slope = 0f;
                    if (hx > 0 && hx < terrainData.heightmapResolution - 1 && hy > 0 && hy < terrainData.heightmapResolution - 1)
                    {
                        float gradX = heights[hx + 1, hy] - heights[hx - 1, hy];
                        float gradY = heights[hx, hy + 1] - heights[hx, hy - 1];
                        slope = Mathf.Sqrt(gradX * gradX + gradY * gradY);
                    }
                    
                    // 基于高度和坡度分配纹理权重
                    float[] weights = new float[layerCount];
                    
                    if (layerCount >= 4)
                    {
                        // 4层纹理：低地(0), 中地(1), 高地(2), 岩石(3)
                        weights[0] = Mathf.Clamp01(1f - height * 3f); // 低地：低海拔
                        weights[1] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.4f) * 4f); // 中地：中等海拔
                        weights[2] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.7f) * 3f); // 高地：高海拔
                        weights[3] = Mathf.Clamp01(slope * 10f); // 岩石：陡坡
                    }
                    else
                    {
                        // 如果层数不足4层，简单分配
                        for (int i = 0; i < layerCount; i++)
                        {
                            float heightRange = (float)i / (layerCount - 1);
                            weights[i] = 1f - Mathf.Abs(height - heightRange) * 2f;
                            weights[i] = Mathf.Clamp01(weights[i]);
                        }
                    }
                    
                    // 标准化权重
                    float totalWeight = 0f;
                    for (int i = 0; i < layerCount; i++)
                        totalWeight += weights[i];
                    
                    if (totalWeight > 0f)
                    {
                        for (int i = 0; i < layerCount; i++)
                        {
                            alphaMap[x, y, i] = weights[i] / totalWeight;
                        }
                    }
                    else
                    {
                        // 如果没有权重，使用第一层
                        alphaMap[x, y, 0] = 1.0f;
                        for (int i = 1; i < layerCount; i++)
                            alphaMap[x, y, i] = 0.0f;
                    }
                }
            }
            
            terrainData.SetAlphamaps(0, 0, alphaMap);
            Debug.Log($"[TextureBlender] 设置基于高度的多层纹理alpha贴图，分辨率: {alphamapWidth}x{alphamapHeight}，层数: {layerCount}");
        }
        
        /// <summary>
        /// 创建默认地形层
        /// </summary>
        TerrainLayer[] CreateDefaultTerrainLayers()
        {
            Debug.Log("[TextureBlender] 创建多层默认地形纹理层");
            
            try
            {
                List<TerrainLayer> layers = new List<TerrainLayer>();
                
                // 1. 低地层（深绿色 - 草地/湿地）
                TerrainLayer lowlandLayer = new TerrainLayer();
                Texture2D lowlandTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                lowlandTexture.SetPixel(0, 0, new Color(0.2f, 0.6f, 0.2f, 1f)); // 深绿色
                lowlandTexture.Apply();
                lowlandTexture.name = "LowlandTexture";
                lowlandLayer.diffuseTexture = lowlandTexture;
                lowlandLayer.tileSize = new Vector2(50f, 50f);
                lowlandLayer.tileOffset = Vector2.zero;
                layers.Add(lowlandLayer);
                
                // 2. 中地层（浅绿色 - 草原）
                TerrainLayer midlandLayer = new TerrainLayer();
                Texture2D midlandTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                midlandTexture.SetPixel(0, 0, new Color(0.4f, 0.7f, 0.3f, 1f)); // 浅绿色
                midlandTexture.Apply();
                midlandTexture.name = "MidlandTexture";
                midlandLayer.diffuseTexture = midlandTexture;
                midlandLayer.tileSize = new Vector2(75f, 75f);
                midlandLayer.tileOffset = Vector2.zero;
                layers.Add(midlandLayer);
                
                // 3. 高地层（棕色 - 山坡/土壤）
                TerrainLayer highlandLayer = new TerrainLayer();
                Texture2D highlandTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                highlandTexture.SetPixel(0, 0, new Color(0.6f, 0.5f, 0.3f, 1f)); // 棕色
                highlandTexture.Apply();
                highlandTexture.name = "HighlandTexture";
                highlandLayer.diffuseTexture = highlandTexture;
                highlandLayer.tileSize = new Vector2(100f, 100f);
                highlandLayer.tileOffset = Vector2.zero;
                layers.Add(highlandLayer);
                
                // 4. 岩石层（灰色 - 陡坡/岩石）
                TerrainLayer rockLayer = new TerrainLayer();
                Texture2D rockTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                rockTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 1f)); // 灰色
                rockTexture.Apply();
                rockTexture.name = "RockTexture";
                rockLayer.diffuseTexture = rockTexture;
                rockLayer.tileSize = new Vector2(25f, 25f);
                rockLayer.tileOffset = Vector2.zero;
                layers.Add(rockLayer);
                
                Debug.Log($"[TextureBlender] 创建了 {layers.Count} 个默认纹理层");
                return layers.ToArray();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TextureBlender] 创建默认纹理层失败: {e.Message}");
                return new TerrainLayer[0];
            }
        }
        
        /// <summary>
        /// 获取生物群落湿度
        /// </summary>
        float GetBiomeMoisture(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 0.2f;
                case BiomeType.Forest: return 0.8f;
                case BiomeType.Grassland: return 0.5f;
                case BiomeType.Mountain: return 0.6f;
                case BiomeType.Tundra: return 0.3f;
                case BiomeType.Tropical: return 0.9f;
                case BiomeType.Temperate: return 0.6f;
                case BiomeType.Swamp: return 1.0f;
                default: return 0.5f;
            }
        }
        
        /// <summary>
        /// 获取生物群落温度
        /// </summary>
        float GetBiomeTemperature(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 0.9f;
                case BiomeType.Forest: return 0.6f;
                case BiomeType.Grassland: return 0.7f;
                case BiomeType.Mountain: return 0.4f;
                case BiomeType.Tundra: return 0.1f;
                case BiomeType.Tropical: return 0.95f;
                case BiomeType.Temperate: return 0.6f;
                case BiomeType.Swamp: return 0.8f;
                default: return 0.5f;
            }
        }
    }
}