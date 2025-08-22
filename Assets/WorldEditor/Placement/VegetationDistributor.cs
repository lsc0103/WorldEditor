using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 植被分布器 - 负责智能植被分布算法
    /// </summary>
    public class VegetationDistributor
    {
        private VegetationSystem vegetationSystem;
        private System.Random distributionRandom;
        
        public VegetationDistributor(VegetationSystem system)
        {
            vegetationSystem = system;
            distributionRandom = new System.Random();
        }
        
        /// <summary>
        /// 在地形上分布植被
        /// </summary>
        public void DistributeOnTerrain(Terrain terrain, VegetationDistributionParams parameters)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("[VegetationDistributor] 地形或地形数据为空");
                return;
            }
            
            var terrainData = terrain.terrainData;
            var terrainSize = terrainData.size;
            var heightmapResolution = terrainData.heightmapResolution;
            
            Debug.Log($"[VegetationDistributor] 开始分布植被 - 地形大小: {terrainSize}, 高度图分辨率: {heightmapResolution}");
            
            // 获取地形数据
            float[,] heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            
            // 分布不同类型的植被
            foreach (var vegData in vegetationSystem.Library.vegetationTypes)
            {
                DistributeVegetationType(terrain, vegData, heights, alphamaps, parameters);
            }
            
            Debug.Log("[VegetationDistributor] 植被分布完成");
        }
        
        /// <summary>
        /// 应用植被模板
        /// </summary>
        public void ApplyTemplate(Terrain terrain, VegetationTemplate template, VegetationDistributionParams parameters)
        {
            if (terrain == null || template == null)
            {
                Debug.LogError("[VegetationDistributor] 地形或模板为空");
                return;
            }
            
            Debug.Log($"[VegetationDistributor] 应用植被模板: {template.templateName}");
            
            var terrainData = terrain.terrainData;
            var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            var alphamaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            
            // 分布主要植被
            foreach (var primaryType in template.primaryTypes)
            {
                var vegData = vegetationSystem.Library.GetVegetationData(primaryType);
                if (vegData != null)
                {
                    DistributeVegetationType(terrain, vegData, heights, alphamaps, parameters, template.primaryDensity);
                }
            }
            
            // 分布次要植被
            foreach (var secondaryType in template.secondaryTypes)
            {
                var vegData = vegetationSystem.Library.GetVegetationData(secondaryType);
                if (vegData != null)
                {
                    DistributeVegetationType(terrain, vegData, heights, alphamaps, parameters, template.secondaryDensity);
                }
            }
        }
        
        void DistributeVegetationType(Terrain terrain, VegetationData vegData, float[,] heights, float[,,] alphamaps, 
                                     VegetationDistributionParams parameters, float densityMultiplier = 1.0f)
        {
            var terrainData = terrain.terrainData;
            var terrainSize = terrainData.size;
            
            // 计算采样密度
            int sampleCount = CalculateSampleCount(vegData, terrainSize, parameters.globalDensity * densityMultiplier);
            
            Debug.Log($"[VegetationDistributor] 分布 {vegData.displayName}: {sampleCount} 个样本");
            
            int placed = 0;
            int maxAttempts = sampleCount * 3; // 防止无限循环
            
            for (int i = 0; i < maxAttempts && placed < sampleCount; i++)
            {
                // 生成随机位置
                Vector3 worldPos = GenerateRandomPosition(terrain, terrainSize);
                
                // 检查是否适合放置
                if (IsSuitableForVegetation(worldPos, terrain, vegData, heights, alphamaps, parameters))
                {
                    // 放置植被
                    vegetationSystem.PlaceVegetation(vegData.type, worldPos);
                    placed++;
                    
                    // 定期更新进度
                    if (placed % 50 == 0)
                    {
                        Debug.Log($"[VegetationDistributor] 已放置 {placed}/{sampleCount} 个 {vegData.displayName}");
                    }
                }
            }
            
            Debug.Log($"[VegetationDistributor] 完成 {vegData.displayName} 分布: {placed}/{sampleCount}");
        }
        
        int CalculateSampleCount(VegetationData vegData, Vector3 terrainSize, float globalDensity)
        {
            // 基础密度计算
            float area = terrainSize.x * terrainSize.z;
            float baseDensity = vegData.density * globalDensity;
            
            // 根据植被类型调整密度
            float typeDensityModifier = GetTypeDensityModifier(vegData.type);
            
            int count = Mathf.RoundToInt(area * baseDensity * typeDensityModifier * 0.001f); // 0.001f 是缩放因子
            
            // 限制最大数量防止性能问题
            return Mathf.Clamp(count, 0, 1000);
        }
        
        float GetTypeDensityModifier(VegetationType type)
        {
            switch (type)
            {
                // 树木密度较低
                case VegetationType.针叶树:
                case VegetationType.阔叶树:
                case VegetationType.棕榈树:
                case VegetationType.果树:
                    return 0.5f;
                
                case VegetationType.枯树:
                    return 0.2f;
                
                // 灌木密度中等
                case VegetationType.普通灌木:
                case VegetationType.浆果灌木:
                case VegetationType.荆棘丛:
                    return 1.0f;
                
                case VegetationType.竹子:
                    return 0.8f;
                
                // 草本植物密度较高
                case VegetationType.野草:
                case VegetationType.鲜花:
                case VegetationType.蕨类:
                    return 2.0f;
                
                case VegetationType.苔藓:
                    return 3.0f;
                
                // 特殊植物密度很低
                case VegetationType.仙人掌:
                    return 0.3f;
                
                case VegetationType.蘑菇:
                    return 1.5f;
                
                case VegetationType.藤蔓:
                case VegetationType.水草:
                    return 0.6f;
                
                default:
                    return 1.0f;
            }
        }
        
        Vector3 GenerateRandomPosition(Terrain terrain, Vector3 terrainSize)
        {
            float x = Random.Range(0f, terrainSize.x);
            float z = Random.Range(0f, terrainSize.z);
            
            // 获取地形高度
            float normalizedX = x / terrainSize.x;
            float normalizedZ = z / terrainSize.z;
            float height = terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
            
            return new Vector3(x, height, z) + terrain.transform.position;
        }
        
        bool IsSuitableForVegetation(Vector3 worldPos, Terrain terrain, VegetationData vegData, 
                                   float[,] heights, float[,,] alphamaps, VegetationDistributionParams parameters)
        {
            // 转换为地形本地坐标
            Vector3 localPos = worldPos - terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            
            float normalizedX = localPos.x / terrainSize.x;
            float normalizedZ = localPos.z / terrainSize.z;
            float normalizedHeight = localPos.y / terrainSize.y;
            
            // 边界检查
            if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
                return false;
            
            // 高度范围检查
            if (parameters.respectHeight && 
                (normalizedHeight < vegData.heightRange.x || normalizedHeight > vegData.heightRange.y))
                return false;
            
            // 坡度检查
            if (parameters.respectSlope && !vegData.canGrowOnSlope)
            {
                float slope = CalculateSlope(heights, normalizedX, normalizedZ);
                if (slope > 30f) // 30度坡度限制
                    return false;
            }
            
            // 纹理基础分布检查
            if (parameters.respectTextures)
            {
                float textureWeight = GetTextureWeight(alphamaps, normalizedX, normalizedZ, vegData.type);
                if (textureWeight < 0.1f) // 纹理权重太低
                    return false;
            }
            
            // 生物群系检查
            if (parameters.respectBiomes && vegData.preferredBiomes.Count > 0)
            {
                BiomeType currentBiome = DetermineBiome(normalizedHeight, normalizedX, normalizedZ);
                if (!vegData.preferredBiomes.Contains(currentBiome))
                    return false;
            }
            
            // 添加一些随机性
            if (Random.Range(0f, 1f) > vegData.density)
                return false;
            
            return true;
        }
        
        float CalculateSlope(float[,] heights, float normalizedX, float normalizedZ)
        {
            int heightmapWidth = heights.GetLength(0);
            int heightmapHeight = heights.GetLength(1);
            
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * heightmapWidth), 1, heightmapWidth - 2);
            int z = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * heightmapHeight), 1, heightmapHeight - 2);
            
            float heightL = heights[x - 1, z];
            float heightR = heights[x + 1, z];
            float heightD = heights[x, z - 1];
            float heightU = heights[x, z + 1];
            
            float dx = heightR - heightL;
            float dy = heightU - heightD;
            
            float slope = Mathf.Sqrt(dx * dx + dy * dy) / 2f;
            return Mathf.Atan(slope) * Mathf.Rad2Deg;
        }
        
        float GetTextureWeight(float[,,] alphamaps, float normalizedX, float normalizedZ, VegetationType vegType)
        {
            int alphamapWidth = alphamaps.GetLength(0);
            int alphamapHeight = alphamaps.GetLength(1);
            int layerCount = alphamaps.GetLength(2);
            
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * alphamapWidth), 0, alphamapWidth - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * alphamapHeight), 0, alphamapHeight - 1);
            
            // 根据植被类型返回对应纹理层的权重
            // 这里需要根据实际的纹理绘制系统来映射
            switch (vegType)
            {
                case VegetationType.针叶树:
                case VegetationType.阔叶树:
                    // 在草地和苔藓纹理上生长较好
                    if (layerCount > 1) return alphamaps[x, z, 1]; // 草地层
                    break;
                    
                case VegetationType.野草:
                case VegetationType.鲜花:
                    // 主要在草地纹理上
                    if (layerCount > 1) return alphamaps[x, z, 1]; // 草地层
                    break;
                    
                case VegetationType.仙人掌:
                    // 在沙漠纹理上
                    if (layerCount > 2) return alphamaps[x, z, 2]; // 沙漠层
                    break;
                    
                case VegetationType.水草:
                    // 在水面纹理上
                    if (layerCount > 6) return alphamaps[x, z, 6]; // 水面层
                    break;
                    
                case VegetationType.苔藓:
                    // 在苔藓纹理上
                    if (layerCount > 8) return alphamaps[x, z, 8]; // 苔藓层
                    break;
            }
            
            return 0.5f; // 默认权重
        }
        
        BiomeType DetermineBiome(float height, float x, float z)
        {
            // 简单的生物群系判定逻辑
            if (height > 0.8f)
                return BiomeType.Mountain;
            
            if (height > 0.6f)
                return BiomeType.Forest;
                
            if (height < 0.2f)
                return BiomeType.Swamp;
            
            // 基于位置的生物群系变化
            float temperature = Mathf.PerlinNoise(x * 2f, z * 2f);
            float humidity = Mathf.PerlinNoise(x * 3f + 100f, z * 3f + 100f);
            
            if (temperature > 0.7f && humidity < 0.3f)
                return BiomeType.Desert;
                
            if (temperature > 0.6f && humidity > 0.6f)
                return BiomeType.Tropical;
                
            if (temperature < 0.3f)
                return BiomeType.Temperate;
            
            if (humidity < 0.4f)
                return BiomeType.Grassland;
            
            return BiomeType.Forest; // 默认生物群系
        }
    }
}