using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 生态系统模拟器 - 模拟植物和动物的生态交互
    /// </summary>
    public class EcosystemSimulator : MonoBehaviour
    {
        [Header("生态系统设置")]
        [SerializeField] private bool enableEcosystemSimulation = true;
        [SerializeField] private float simulationSpeed = 1f;
        [SerializeField] private int maxOrganisms = 10000;
        
        [Header("生物多样性")]
        [SerializeField] private float biodiversityTarget = 0.8f;
        [SerializeField] private bool enableSpeciesInteraction = true;
        [SerializeField] private bool enableNaturalSelection = true;
        
        [Header("环境影响")]
        [SerializeField] private bool environmentAffectsGrowth = true;
        [SerializeField] private float climateChangeRate = 0.01f;
        
        // 私有变量
        private List<EcosystemOrganism> organisms = new List<EcosystemOrganism>();
        private Dictionary<string, SpeciesData> speciesDatabase = new Dictionary<string, SpeciesData>();
        
        /// <summary>
        /// 模拟生态系统
        /// </summary>
        public void SimulateEcosystem(PlacementGrid placementGrid, WorldGenerationParameters parameters)
        {
            if (!enableEcosystemSimulation) return;
            
            Debug.Log("[Ecosystem] 开始生态系统模拟...");
            
            // 同步版本的简化模拟
            InitializeEcosystem(placementGrid);
            UpdateEcosystemState(parameters);
        }
        
        /// <summary>
        /// 模拟生态系统（异步版本）
        /// </summary>
        public IEnumerator SimulateEcosystemProgressive(PlacementGrid placementGrid, WorldGenerationParameters parameters, int stepsPerFrame)
        {
            if (!enableEcosystemSimulation) yield break;
            
            Debug.Log("[Ecosystem] 开始渐进式生态系统模拟...");
            
            yield return StartCoroutine(InitializeEcosystemProgressive(placementGrid, stepsPerFrame));
            yield return StartCoroutine(UpdateEcosystemStateProgressive(parameters, stepsPerFrame));
        }
        
        void InitializeEcosystem(PlacementGrid placementGrid)
        {
            // 从放置网格中收集生物信息
            var grid = placementGrid.GetGrid();
            
            foreach (var cell in grid.Values)
            {
                foreach (var placedObject in cell)
                {
                    if (placedObject.ecosystemRole == EcosystemRole.Producer)
                    {
                        CreateOrganism(placedObject);
                    }
                }
            }
        }
        
        IEnumerator InitializeEcosystemProgressive(PlacementGrid placementGrid, int stepsPerFrame)
        {
            var grid = placementGrid.GetGrid();
            int processedObjects = 0;
            
            foreach (var cell in grid.Values)
            {
                foreach (var placedObject in cell)
                {
                    if (placedObject.ecosystemRole == EcosystemRole.Producer)
                    {
                        CreateOrganism(placedObject);
                    }
                    
                    processedObjects++;
                    if (processedObjects >= stepsPerFrame)
                    {
                        processedObjects = 0;
                        yield return null;
                    }
                }
            }
        }
        
        void CreateOrganism(PlacedObject placedObject)
        {
            // 检查是否超过最大生物数量限制
            if (organisms.Count >= maxOrganisms)
            {
                return; // 达到最大限制，不再创建新生物
            }
            
            var organism = new EcosystemOrganism
            {
                id = System.Guid.NewGuid().ToString(),
                speciesName = placedObject.speciesName ?? "Unknown",
                position = placedObject.position,
                health = placedObject.health,
                age = placedObject.age,
                ecosystemRole = placedObject.ecosystemRole,
                gameObject = placedObject.gameObject
            };
            
            organisms.Add(organism);
        }
        
        void UpdateEcosystemState(WorldGenerationParameters parameters)
        {
            // 简化的生态系统更新逻辑
            foreach (var organism in organisms)
            {
                UpdateOrganismState(organism, parameters);
            }
        }
        
        IEnumerator UpdateEcosystemStateProgressive(WorldGenerationParameters parameters, int stepsPerFrame)
        {
            int processedOrganisms = 0;
            
            foreach (var organism in organisms)
            {
                UpdateOrganismState(organism, parameters);
                
                processedOrganisms++;
                if (processedOrganisms >= stepsPerFrame)
                {
                    processedOrganisms = 0;
                    yield return null;
                }
            }
        }
        
        void UpdateOrganismState(EcosystemOrganism organism, WorldGenerationParameters parameters)
        {
            // 基于环境条件更新生物状态
            if (environmentAffectsGrowth)
            {
                float environmentalFitness = CalculateEnvironmentalFitness(organism, parameters);
                organism.health *= environmentalFitness;
            }
            
            // 气候变化影响
            if (climateChangeRate > 0f)
            {
                float climateStress = CalculateClimateChangeStress(organism);
                organism.health *= (1f - climateStress * climateChangeRate * Time.deltaTime);
                
                // 气候变化也可能影响物种适应性
                organism.adaptability = Mathf.Max(0f, organism.adaptability - climateChangeRate * Time.deltaTime * 0.1f);
            }
            
            // 物种交互影响
            if (enableSpeciesInteraction)
            {
                float interactionEffect = CalculateSpeciesInteraction(organism);
                organism.health *= interactionEffect;
            }
            
            // 自然选择压力
            if (enableNaturalSelection)
            {
                ApplyNaturalSelection(organism);
            }
            
            // 年龄增长
            organism.age += Time.deltaTime * simulationSpeed;
            
            // 健康度限制
            organism.health = Mathf.Clamp01(organism.health);
        }
        
        float CalculateSpeciesInteraction(EcosystemOrganism organism)
        {
            float interactionEffect = 1f;
            
            // 查找附近的其他生物
            foreach (var otherOrganism in organisms)
            {
                if (otherOrganism == organism) continue;
                
                float distance = Vector3.Distance(organism.position, otherOrganism.position);
                if (distance < 10f) // 10米范围内的交互
                {
                    // 同种竞争
                    if (otherOrganism.speciesName == organism.speciesName)
                    {
                        interactionEffect *= 0.95f; // 轻微竞争效应
                    }
                    // 不同种互动（简化版本）
                    else
                    {
                        interactionEffect *= 1.01f; // 轻微互利效应
                    }
                }
            }
            
            return Mathf.Clamp(interactionEffect, 0.8f, 1.2f);
        }
        
        float CalculateEnvironmentalFitness(EcosystemOrganism organism, WorldGenerationParameters parameters)
        {
            // 基于环境参数计算生物的适应性
            float fitness = 1f;
            
            // 温度适应性
            float temperature = parameters.environmentParams.temperature;
            if (temperature < 0f || temperature > 40f)
            {
                fitness *= 0.8f; // 极端温度降低适应性
            }
            
            // 湿度适应性
            float humidity = parameters.environmentParams.humidity;
            if (humidity < 0.2f || humidity > 0.9f)
            {
                fitness *= 0.9f; // 极端湿度轻微降低适应性
            }
            
            return fitness;
        }
        
        /// <summary>
        /// 计算气候变化压力
        /// </summary>
        float CalculateClimateChangeStress(EcosystemOrganism organism)
        {
            // 基于生物的环境耐受性和适应性计算气候变化压力
            float baseStress = 1f - organism.environmentalTolerance;
            
            // 适应性高的物种受气候变化影响较小
            float adaptabilityModifier = 1f - organism.adaptability;
            
            // 年龄也影响抗压能力，老年生物更脆弱
            float ageStress = organism.age > 50f ? (organism.age - 50f) / 50f * 0.2f : 0f;
            
            // 不同生态角色的气候敏感性不同
            float roleModifier = 1f;
            switch (organism.ecosystemRole)
            {
                case EcosystemRole.Producer:
                    roleModifier = 0.8f; // 生产者(植物)相对较稳定
                    break;
                case EcosystemRole.Consumer:
                    roleModifier = 1.1f; // 消费者中等敏感
                    break;
                case EcosystemRole.Decomposer:
                    roleModifier = 0.6f; // 分解者较不敏感
                    break;
            }
            
            float totalStress = (baseStress + adaptabilityModifier + ageStress) * roleModifier;
            return Mathf.Clamp01(totalStress);
        }
        
        /// <summary>
        /// 应用自然选择压力
        /// </summary>
        void ApplyNaturalSelection(EcosystemOrganism organism)
        {
            // 基于适应性的选择压力
            float selectionPressure = CalculateSelectionPressure(organism);
            
            // 适应性较低的个体更容易被淘汰
            if (organism.health < 0.3f && selectionPressure > 0.7f)
            {
                // 逐渐降低健康度，模拟淘汰过程
                organism.health *= 0.98f;
            }
            
            // 高适应性个体有更高的繁殖率
            if (organism.health > 0.8f && selectionPressure < 0.3f)
            {
                organism.reproductionRate = Mathf.Min(organism.reproductionRate * 1.01f, 0.5f);
            }
            
            // 年龄对选择压力的影响
            if (organism.age > 80f) // 老年个体更容易被淘汰
            {
                organism.health *= 0.995f;
            }
        }
        
        /// <summary>
        /// 计算选择压力
        /// </summary>
        float CalculateSelectionPressure(EcosystemOrganism organism)
        {
            float pressure = 0f;
            
            // 环境容量压力
            float capacityPressure = (float)organisms.Count / maxOrganisms;
            pressure += capacityPressure * 0.4f;
            
            // 竞争压力
            int competitorCount = 0;
            foreach (var other in organisms)
            {
                if (other != organism && 
                    other.speciesName == organism.speciesName &&
                    Vector3.Distance(other.position, organism.position) < 5f)
                {
                    competitorCount++;
                }
            }
            float competitionPressure = Mathf.Clamp01(competitorCount / 5f);
            pressure += competitionPressure * 0.3f;
            
            // 环境适应性压力
            float adaptabilityPressure = 1f - organism.environmentalTolerance;
            pressure += adaptabilityPressure * 0.3f;
            
            return Mathf.Clamp01(pressure);
        }
        
        /// <summary>
        /// 获取生态系统统计信息
        /// </summary>
        public string GetEcosystemStats()
        {
            if (!enableEcosystemSimulation)
                return "生态系统模拟已禁用";
            
            int healthyOrganisms = 0;
            int unhealthyOrganisms = 0;
            
            foreach (var organism in organisms)
            {
                if (organism.health > 0.5f)
                    healthyOrganisms++;
                else
                    unhealthyOrganisms++;
            }
            
            float averageHealth = 0f;
            if (organisms.Count > 0)
            {
                float totalHealth = 0f;
                foreach (var organism in organisms)
                {
                    totalHealth += organism.health;
                }
                averageHealth = totalHealth / organisms.Count;
            }
            
            return $"生态系统统计:\n" +
                   $"总生物数量: {organisms.Count}\n" +
                   $"健康生物: {healthyOrganisms}\n" +
                   $"不健康生物: {unhealthyOrganisms}\n" +
                   $"平均健康度: {averageHealth:F2}\n" +
                   $"生物多样性目标: {biodiversityTarget:F2}";
        }
    }
    
    /// <summary>
    /// 生态系统生物
    /// </summary>
    [System.Serializable]
    public class EcosystemOrganism
    {
        public string id;
        public string speciesName;
        public Vector3 position;
        public float health = 1f;
        public float age = 0f;
        public EcosystemRole ecosystemRole;
        public GameObject gameObject;
        
        // 生态属性
        public float reproductionRate = 0.1f;
        public float mortalityRate = 0.05f;
        public float competitiveness = 0.5f;
        public float environmentalTolerance = 0.8f;
        public float adaptability = 1f;
    }
    
    /// <summary>
    /// 物种数据
    /// </summary>
    [System.Serializable]
    public class SpeciesData
    {
        public string speciesName;
        public EcosystemRole role;
        public float optimalTemperature = 20f;
        public float temperatureTolerance = 10f;
        public float optimalHumidity = 0.6f;
        public float humidityTolerance = 0.3f;
        public float growthRate = 1f;
        public float maxAge = 100f;
        public string[] competingSpecies;
        public string[] symbioticSpecies;
    }
}