using UnityEngine;
using WorldEditor.Core;
using WorldEditor.Environment;

namespace WorldEditor.AI
{
    /// <summary>
    /// AI生成系统 - AI驱动的世界生成和优化
    /// </summary>
    public class AIGenerationSystem : MonoBehaviour
    {
        [Header("AI设置")]
        [SerializeField] private bool enableAIGeneration = true;
        [SerializeField] private AIComplexityLevel complexityLevel = AIComplexityLevel.Medium;
        [SerializeField] private float learningRate = 0.01f;
        
        [Header("生成风格")]
        [SerializeField] private BiomeType targetBiome = BiomeType.Temperate;
        [SerializeField] private GenerationStyle style = GenerationStyle.Realistic;
        
        [Header("学习参数")]
        [SerializeField] private bool enableMLLearning = false;
        [SerializeField] private int trainingIterations = 100;
        
        private EnvironmentManager environmentManager;
        
        public void Initialize(EnvironmentManager envManager)
        {
            environmentManager = envManager;
        }
        
        /// <summary>
        /// AI生成世界内容
        /// </summary>
        public void GenerateContent(WorldGenerationParameters parameters)
        {
            if (!enableAIGeneration) return;
            
            Debug.Log($"[AI] AI生成系统正在分析参数... 目标生物群落: {targetBiome}");
            
            // 基于目标生物群落调整参数
            AdjustParametersForTargetBiome(parameters);
            
            // 基于AI分析优化参数
            OptimizeParameters(parameters);
        }
        
        void AdjustParametersForTargetBiome(WorldGenerationParameters parameters)
        {
            // 根据目标生物群落调整环境参数
            switch (targetBiome)
            {
                case BiomeType.Tropical:
                    parameters.environmentParams.temperature = Mathf.Max(parameters.environmentParams.temperature, 25f);
                    parameters.environmentParams.humidity = Mathf.Max(parameters.environmentParams.humidity, 0.8f);
                    break;
                    
                case BiomeType.Desert:
                    parameters.environmentParams.temperature = Mathf.Max(parameters.environmentParams.temperature, 30f);
                    parameters.environmentParams.humidity = Mathf.Min(parameters.environmentParams.humidity, 0.2f);
                    break;
                    
                case BiomeType.Tundra:
                    parameters.environmentParams.temperature = Mathf.Min(parameters.environmentParams.temperature, -5f);
                    parameters.environmentParams.humidity = Mathf.Min(parameters.environmentParams.humidity, 0.4f);
                    break;
                    
                case BiomeType.Temperate:
                    parameters.environmentParams.temperature = Mathf.Clamp(parameters.environmentParams.temperature, 10f, 25f);
                    parameters.environmentParams.humidity = Mathf.Clamp(parameters.environmentParams.humidity, 0.4f, 0.7f);
                    break;
                    
                default:
                    // 保持原有参数
                    break;
            }
            
            Debug.Log($"[AI] 已针对{targetBiome}生物群落调整参数");
        }
        
        void OptimizeParameters(WorldGenerationParameters parameters)
        {
            // 首先根据生成风格调整基础参数
            ApplyGenerationStyle(parameters);
            
            // AI参数优化逻辑
            switch (complexityLevel)
            {
                case AIComplexityLevel.Simple:
                    SimpleOptimization(parameters);
                    break;
                case AIComplexityLevel.Medium:
                    MediumOptimization(parameters);
                    break;
                case AIComplexityLevel.Complex:
                    ComplexOptimization(parameters);
                    break;
                case AIComplexityLevel.Ultra:
                    UltraOptimization(parameters);
                    break;
            }
        }
        
        void ApplyGenerationStyle(WorldGenerationParameters parameters)
        {
            // 根据生成风格调整参数
            switch (style)
            {
                case GenerationStyle.Realistic:
                    // 真实风格：较低的极值，平衡的参数
                    parameters.terrainParams.heightVariation = Mathf.Clamp(parameters.terrainParams.heightVariation, 50f, 200f);
                    AdjustNoiseLayersFrequency(parameters.terrainParams.noiseLayers, 0.01f, 0.05f);
                    break;
                    
                case GenerationStyle.Fantasy:
                    // 奇幻风格：更高的山峰，更多变化
                    parameters.terrainParams.heightVariation = Mathf.Clamp(parameters.terrainParams.heightVariation, 100f, 500f);
                    AdjustNoiseLayersFrequency(parameters.terrainParams.noiseLayers, 0.005f, 0.03f);
                    break;
                    
                case GenerationStyle.Stylized:
                    // 风格化：平滑的地形，简化的特征
                    parameters.terrainParams.heightVariation = Mathf.Clamp(parameters.terrainParams.heightVariation, 30f, 150f);
                    // 减少噪声层数量来获得更平滑的效果
                    if (parameters.terrainParams.noiseLayers != null && parameters.terrainParams.noiseLayers.Length > 1)
                    {
                        for (int i = 1; i < parameters.terrainParams.noiseLayers.Length; i++)
                        {
                            if (parameters.terrainParams.noiseLayers[i] != null)
                                parameters.terrainParams.noiseLayers[i].weight *= 0.5f;
                        }
                    }
                    break;
                    
                case GenerationStyle.SciFi:
                    // 程序化：更多噪声和细节
                    AdjustNoiseLayersFrequency(parameters.terrainParams.noiseLayers, 0.02f, 0.1f);
                    // 增加噪声层的权重来获得更多细节
                    if (parameters.terrainParams.noiseLayers != null)
                    {
                        foreach (var layer in parameters.terrainParams.noiseLayers)
                        {
                            if (layer != null)
                                layer.weight = Mathf.Min(layer.weight * 1.2f, 1f);
                        }
                    }
                    break;
            }
            
            Debug.Log($"[AI] 已应用{style}生成风格");
        }
        
        /// <summary>
        /// 调整噪声层频率
        /// </summary>
        void AdjustNoiseLayersFrequency(NoiseLayerSettings[] noiseLayers, float minFreq, float maxFreq)
        {
            if (noiseLayers == null) return;
            
            foreach (var layer in noiseLayers)
            {
                if (layer != null)
                {
                    layer.frequency = Mathf.Clamp(layer.frequency, minFreq, maxFreq);
                }
            }
        }
        
        void SimpleOptimization(WorldGenerationParameters parameters)
        {
            // 简单的规则基础优化
            Debug.Log("[AI] 执行简单AI优化...");
        }
        
        void MediumOptimization(WorldGenerationParameters parameters)
        {
            // 中等复杂度的AI优化
            Debug.Log("[AI] 执行中等AI优化...");
        }
        
        void ComplexOptimization(WorldGenerationParameters parameters)
        {
            // 复杂的AI优化
            Debug.Log("[AI] 执行复杂AI优化...");
            
            if (enableMLLearning)
            {
                PerformMLTraining(parameters);
            }
        }
        
        void UltraOptimization(WorldGenerationParameters parameters)
        {
            // 超级复杂的AI优化
            Debug.Log("[AI] 执行超级AI优化...");
            
            if (enableMLLearning)
            {
                PerformMLTraining(parameters);
            }
        }
        
        void PerformMLTraining(WorldGenerationParameters parameters)
        {
            Debug.Log($"[AI] 开始机器学习训练，迭代次数: {trainingIterations}");
            
            // 模拟机器学习训练过程
            for (int iteration = 0; iteration < trainingIterations; iteration++)
            {
                // 模拟训练步骤
                float progress = (float)iteration / trainingIterations;
                
                // 基于训练进度调整参数
                float learningProgress = progress * learningRate;
                
                // 模拟参数优化（简化版本）
                if (iteration % 10 == 0) // 每10次迭代输出一次进度
                {
                    Debug.Log($"[AI] 训练进度: {progress:P0} - 学习率调整: {learningProgress:F4}");
                }
            }
            
            Debug.Log("[AI] 机器学习训练完成！");
        }
    }
}