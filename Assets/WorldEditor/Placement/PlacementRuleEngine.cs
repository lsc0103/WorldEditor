using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 放置规则引擎 - 评估和执行智能放置规则
    /// </summary>
    public class PlacementRuleEngine : MonoBehaviour
    {
        [Header("规则引擎设置")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private float ruleEvaluationThreshold = 0.5f;
        [SerializeField] private bool useWeightedRuleEvaluation = true;
        
        [Header("性能设置")]
        [SerializeField] private bool enableRuleCaching = true;
        [SerializeField] private int maxCacheSize = 1000;
        [SerializeField] private float cacheExpirationTime = 60f;
        
        // 规则缓存
        private Dictionary<string, RuleEvaluationResult> ruleCache;
        private Dictionary<string, float> cacheTimestamps;
        
        // 自定义规则脚本
        private Dictionary<string, ICustomPlacementRule> customRules;
        
        void Awake()
        {
            InitializeRuleEngine();
        }
        
        void InitializeRuleEngine()
        {
            if (enableRuleCaching)
            {
                ruleCache = new Dictionary<string, RuleEvaluationResult>();
                cacheTimestamps = new Dictionary<string, float>();
            }
            
            customRules = new Dictionary<string, ICustomPlacementRule>();
            LoadCustomRules();
        }
        
        /// <summary>
        /// 评估放置规则
        /// </summary>
        public bool EvaluatePlacementRules(PlacementLayer layer, Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            if (layer.placementRules == null || layer.placementRules.Length == 0)
                return true; // 没有规则，允许放置
            
            // 生成缓存键
            string cacheKey = GenerateCacheKey(layer, position);
            
            // 检查缓存
            if (enableRuleCaching && TryGetCachedResult(cacheKey, out RuleEvaluationResult cachedResult))
            {
                return cachedResult.canPlace;
            }
            
            // 评估所有规则
            float totalScore = 0f;
            float totalWeight = 0f;
            bool allRulesPassed = true;
            
            foreach (var rule in layer.placementRules)
            {
                if (!rule.enabled) continue;
                
                float ruleScore = EvaluateIndividualRule(rule, position, terrainData, biomeData);
                
                if (layer.requireAllRules && ruleScore <= 0f)
                {
                    allRulesPassed = false;
                    break;
                }
                
                if (useWeightedRuleEvaluation)
                {
                    totalScore += ruleScore * rule.weight;
                    totalWeight += rule.weight;
                }
                else
                {
                    totalScore += ruleScore;
                    totalWeight += 1f;
                }
            }
            
            // 计算最终决策
            bool canPlace;
            if (layer.requireAllRules)
            {
                canPlace = allRulesPassed;
            }
            else
            {
                float averageScore = totalWeight > 0f ? totalScore / totalWeight : 0f;
                canPlace = averageScore >= ruleEvaluationThreshold;
            }
            
            // 缓存结果
            if (enableRuleCaching)
            {
                CacheResult(cacheKey, new RuleEvaluationResult 
                { 
                    canPlace = canPlace, 
                    score = totalScore / totalWeight,
                    evaluatedRules = layer.placementRules.Length
                });
            }
            
            if (enableDebugLogging && !canPlace)
            {
                Debug.Log($"[RuleEngine] 位置 {position} 不满足放置条件，平均分数: {totalScore / totalWeight:F3}");
            }
            
            return canPlace;
        }
        
        /// <summary>
        /// 评估单个规则
        /// </summary>
        float EvaluateIndividualRule(PlacementRule rule, Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            float score = 1f;
            
            // 高度检查
            if (rule.checkHeight)
            {
                float height = terrainData.GetHeightAtPosition(position);
                if (height < rule.minHeight || height > rule.maxHeight)
                {
                    score *= 0f; // 不满足高度条件
                }
                else
                {
                    // 计算高度适应性得分
                    float heightRange = rule.maxHeight - rule.minHeight;
                    float heightCenter = (rule.minHeight + rule.maxHeight) * 0.5f;
                    float distanceFromCenter = Mathf.Abs(height - heightCenter) / (heightRange * 0.5f);
                    score *= Mathf.Max(0f, 1f - distanceFromCenter);
                }
            }
            
            // 坡度检查
            if (rule.checkSlope)
            {
                float slope = terrainData.GetSlopeAtPosition(position) * 90f; // 转换为度数
                if (slope < rule.minSlope || slope > rule.maxSlope)
                {
                    score *= 0f;
                }
                else
                {
                    // 计算坡度适应性得分
                    float slopeRange = rule.maxSlope - rule.minSlope;
                    float slopeCenter = (rule.minSlope + rule.maxSlope) * 0.5f;
                    float distanceFromCenter = Mathf.Abs(slope - slopeCenter) / (slopeRange * 0.5f);
                    score *= Mathf.Max(0f, 1f - distanceFromCenter);
                }
            }
            
            // 湿度检查
            if (rule.checkMoisture)
            {
                float moisture = terrainData.GetMoistureAtPosition(position);
                if (moisture < rule.minMoisture || moisture > rule.maxMoisture)
                {
                    score *= 0f;
                }
                else
                {
                    float moistureRange = rule.maxMoisture - rule.minMoisture;
                    float moistureCenter = (rule.minMoisture + rule.maxMoisture) * 0.5f;
                    float distanceFromCenter = Mathf.Abs(moisture - moistureCenter) / (moistureRange * 0.5f);
                    score *= Mathf.Max(0f, 1f - distanceFromCenter);
                }
            }
            
            // 温度检查
            if (rule.checkTemperature)
            {
                float temperature = terrainData.GetTemperatureAtPosition(position) * 100f; // 转换为摄氏度范围
                if (temperature < rule.minTemperature || temperature > rule.maxTemperature)
                {
                    score *= 0f;
                }
                else
                {
                    float tempRange = rule.maxTemperature - rule.minTemperature;
                    float tempCenter = (rule.minTemperature + rule.maxTemperature) * 0.5f;
                    float distanceFromCenter = Mathf.Abs(temperature - tempCenter) / (tempRange * 0.5f);
                    score *= Mathf.Max(0f, 1f - distanceFromCenter);
                }
            }
            
            // 生物群落检查
            if (rule.checkBiome)
            {
                BiomeType currentBiome = biomeData.GetBiomeAtPosition(position);
                bool biomeAllowed = false;
                
                foreach (BiomeType allowedBiome in rule.allowedBiomes)
                {
                    if (currentBiome == allowedBiome)
                    {
                        biomeAllowed = true;
                        break;
                    }
                }
                
                if (!biomeAllowed)
                {
                    score *= 0f;
                }
            }
            
            // 距离水体检查
            if (rule.checkDistanceToWater)
            {
                float distanceToWater = CalculateDistanceToWater(position);
                if (distanceToWater < rule.minDistanceToWater || distanceToWater > rule.maxDistanceToWater)
                {
                    score *= 0f;
                }
                else
                {
                    // 计算距离适应性得分
                    float distanceRange = rule.maxDistanceToWater - rule.minDistanceToWater;
                    float optimalDistance = (rule.minDistanceToWater + rule.maxDistanceToWater) * 0.5f;
                    float distanceFromOptimal = Mathf.Abs(distanceToWater - optimalDistance) / (distanceRange * 0.5f);
                    score *= Mathf.Max(0f, 1f - distanceFromOptimal);
                }
            }
            
            // 与其他对象的距离检查
            if (rule.checkDistanceToOtherObjects)
            {
                bool tooClose = CheckProximityToOtherObjects(position, rule.minDistanceToOthers, rule.avoidObjectTags);
                if (tooClose)
                {
                    score *= 0f;
                }
            }
            
            // 自定义规则检查
            if (rule.useCustomRule && !string.IsNullOrEmpty(rule.customRuleScript))
            {
                float customScore = EvaluateCustomRule(rule.customRuleScript, position, terrainData, biomeData);
                score *= customScore;
            }
            
            return score;
        }
        
        /// <summary>
        /// 计算到水体的距离
        /// </summary>
        float CalculateDistanceToWater(Vector3 position)
        {
            // 这里应该集成实际的水体数据
            // 目前返回一个估算值
            
            // 假设有一些已知的水体位置
            Vector3[] waterBodies = {
                new Vector3(0f, 0f, 0f),       // 中心湖泊
                new Vector3(100f, 0f, 50f),    // 河流点1
                new Vector3(200f, 0f, 100f)    // 河流点2
            };
            
            float minDistance = float.MaxValue;
            
            foreach (Vector3 waterPos in waterBodies)
            {
                float distance = Vector3.Distance(position, waterPos);
                minDistance = Mathf.Min(minDistance, distance);
            }
            
            return minDistance != float.MaxValue ? minDistance : 1000f; // 默认很远的距离
        }
        
        /// <summary>
        /// 检查与其他对象的邻近性
        /// </summary>
        bool CheckProximityToOtherObjects(Vector3 position, float minDistance, string[] avoidTags)
        {
            // 这里需要集成放置网格系统
            // 目前使用简化的检查
            
            Collider[] nearbyColliders = Physics.OverlapSphere(position, minDistance);
            
            foreach (Collider collider in nearbyColliders)
            {
                if (avoidTags != null && avoidTags.Length > 0)
                {
                    foreach (string tag in avoidTags)
                    {
                        if (collider.CompareTag(tag))
                        {
                            return true; // 太接近了
                        }
                    }
                }
                else
                {
                    return true; // 太接近任何对象
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 评估自定义规则
        /// </summary>
        float EvaluateCustomRule(string ruleScript, Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            if (customRules.ContainsKey(ruleScript))
            {
                return customRules[ruleScript].Evaluate(position, terrainData, biomeData);
            }
            
            // 如果找不到自定义规则，返回中性分数
            Debug.LogWarning($"[RuleEngine] 找不到自定义规则脚本: {ruleScript}");
            return 0.5f;
        }
        
        /// <summary>
        /// 加载自定义规则
        /// </summary>
        void LoadCustomRules()
        {
            // 这里可以通过反射或其他方式加载自定义规则脚本
            // 目前注册一些示例规则
            
            customRules["ProximityToRocks"] = new ProximityToRocksRule();
            customRules["SunExposure"] = new SunExposureRule();
            customRules["WindExposure"] = new WindExposureRule();
        }
        
        /// <summary>
        /// 生成缓存键
        /// </summary>
        string GenerateCacheKey(PlacementLayer layer, Vector3 position)
        {
            // 将位置量化以减少缓存键的数量
            int x = Mathf.RoundToInt(position.x / 5f) * 5; // 5米精度
            int z = Mathf.RoundToInt(position.z / 5f) * 5;
            
            return $"{layer.layerName}_{x}_{z}";
        }
        
        /// <summary>
        /// 尝试获取缓存结果
        /// </summary>
        bool TryGetCachedResult(string cacheKey, out RuleEvaluationResult result)
        {
            result = default;
            
            if (!ruleCache.ContainsKey(cacheKey))
                return false;
            
            // 检查缓存是否过期
            if (cacheTimestamps.ContainsKey(cacheKey))
            {
                float age = Time.time - cacheTimestamps[cacheKey];
                if (age > cacheExpirationTime)
                {
                    ruleCache.Remove(cacheKey);
                    cacheTimestamps.Remove(cacheKey);
                    return false;
                }
            }
            
            result = ruleCache[cacheKey];
            return true;
        }
        
        /// <summary>
        /// 缓存结果
        /// </summary>
        void CacheResult(string cacheKey, RuleEvaluationResult result)
        {
            // 如果缓存太大，清理一些旧条目
            if (ruleCache.Count >= maxCacheSize)
            {
                ClearOldCacheEntries();
            }
            
            ruleCache[cacheKey] = result;
            cacheTimestamps[cacheKey] = Time.time;
        }
        
        /// <summary>
        /// 清理旧的缓存条目
        /// </summary>
        void ClearOldCacheEntries()
        {
            var keysToRemove = new List<string>();
            float currentTime = Time.time;
            
            foreach (var kvp in cacheTimestamps)
            {
                if (currentTime - kvp.Value > cacheExpirationTime * 0.5f) // 清理较老的一半
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (string key in keysToRemove)
            {
                ruleCache.Remove(key);
                cacheTimestamps.Remove(key);
            }
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            if (enableRuleCaching)
            {
                ruleCache.Clear();
                cacheTimestamps.Clear();
                Debug.Log("[RuleEngine] 规则缓存已清理");
            }
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            if (!enableRuleCaching)
                return "缓存已禁用";
            
            return $"规则缓存统计:\n" +
                   $"缓存条目: {ruleCache.Count}/{maxCacheSize}\n" +
                   $"内存使用估算: {ruleCache.Count * 64} bytes";
        }
    }
    
    /// <summary>
    /// 规则评估结果
    /// </summary>
    public struct RuleEvaluationResult
    {
        public bool canPlace;
        public float score;
        public int evaluatedRules;
    }
    
    /// <summary>
    /// 自定义放置规则接口
    /// </summary>
    public interface ICustomPlacementRule
    {
        float Evaluate(Vector3 position, TerrainData terrainData, BiomeData biomeData);
    }
    
    /// <summary>
    /// 示例自定义规则 - 与岩石的邻近性
    /// </summary>
    public class ProximityToRocksRule : ICustomPlacementRule
    {
        public float Evaluate(Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            // 检查附近是否有岩石对象
            Collider[] rocks = Physics.OverlapSphere(position, 10f);
            int rockCount = 0;
            
            foreach (var collider in rocks)
            {
                if (collider.CompareTag("Rock"))
                    rockCount++;
            }
            
            // 适度的岩石数量得分最高
            if (rockCount >= 1 && rockCount <= 3)
                return 1f;
            else if (rockCount == 0)
                return 0.3f;
            else
                return 0.1f;
        }
    }
    
    /// <summary>
    /// 示例自定义规则 - 阳光照射
    /// </summary>
    public class SunExposureRule : ICustomPlacementRule
    {
        public float Evaluate(Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            // 检查坡度和朝向来估算阳光照射
            float slope = terrainData.GetSlopeAtPosition(position);
            
            // 南向坡面通常阳光照射更好（北半球）
            // 这里简化为基于坡度的计算
            if (slope < 0.3f) // 平地
                return 0.8f;
            else if (slope < 0.6f) // 缓坡
                return 1f;
            else // 陡坡
                return 0.4f;
        }
    }
    
    /// <summary>
    /// 示例自定义规则 - 风暴露
    /// </summary>
    public class WindExposureRule : ICustomPlacementRule
    {
        public float Evaluate(Vector3 position, TerrainData terrainData, BiomeData biomeData)
        {
            float height = terrainData.GetHeightAtPosition(position);
            
            // 高海拔地区风暴露较强
            if (height > 0.8f)
                return 0.2f; // 风太强，不适合大多数植物
            else if (height > 0.6f)
                return 0.6f; // 中等风暴露
            else
                return 1f; // 低风暴露，适合生长
        }
    }
}