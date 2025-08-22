using UnityEngine;
using System.Collections.Generic;

namespace WorldEditor.Core
{
    /// <summary>
    /// WorldEditor核心管理器 - 统一管理所有世界编辑器功能
    /// 超越Gaia Pro + GeNa Pro + Enviro 3的综合解决方案
    /// </summary>
    public class WorldEditorManager : MonoBehaviour
    {
        [Header("World Editor Core")]
        public static WorldEditorManager Instance { get; private set; }
        
        [Header("System References")]
        [SerializeField] private TerrainSystem.AdvancedTerrainGenerator terrainGenerator;
        [SerializeField] private TerrainSystem.MultiTerrainManager multiTerrainManager; // 新增：多地形管理器
        [SerializeField] private Placement.SmartPlacementSystem placementSystem;
        [SerializeField] private Environment.EnvironmentManager environmentManager;
        [SerializeField] private AI.AIGenerationSystem aiGenerationSystem;
        [SerializeField] private Optimization.WorldOptimizer worldOptimizer;
        
        [Header("World Settings")]
        [SerializeField] private WorldEditorSettings worldSettings;
        [SerializeField] private bool enableRealTimeGeneration = true;
        [SerializeField] private bool enableAutoOptimization = true;
        
        // Events
        public System.Action<WorldGenerationState> OnWorldGenerationStateChanged;
        public System.Action<float> OnGenerationProgress;
        
        private WorldGenerationState currentState = WorldGenerationState.Idle;
        private Queue<WorldGenerationTask> generationQueue = new Queue<WorldGenerationTask>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeSystems()
        {
            Debug.Log("[WorldEditor] 初始化世界编辑器核心系统...");
            
            // 初始化各个子系统
            if (terrainGenerator == null)
                terrainGenerator = Object.FindFirstObjectByType<TerrainSystem.AdvancedTerrainGenerator>();
                
            // 初始化多地形管理器（可选功能，不影响现有系统）
            if (multiTerrainManager == null)
                multiTerrainManager = Object.FindFirstObjectByType<TerrainSystem.MultiTerrainManager>();
            
            if (placementSystem == null)
                placementSystem = Object.FindFirstObjectByType<Placement.SmartPlacementSystem>();
                
            if (environmentManager == null)
                environmentManager = Object.FindFirstObjectByType<Environment.EnvironmentManager>();
                
            if (aiGenerationSystem == null)
                aiGenerationSystem = Object.FindFirstObjectByType<AI.AIGenerationSystem>();
                
            if (worldOptimizer == null)
                worldOptimizer = Object.FindFirstObjectByType<Optimization.WorldOptimizer>();
            
            // 加载默认设置
            if (worldSettings == null)
                LoadDefaultSettings();
                
            Debug.Log("[WorldEditor] 核心系统初始化完成!");
        }
        
        void LoadDefaultSettings()
        {
            // 创建默认世界设置
            worldSettings = ScriptableObject.CreateInstance<WorldEditorSettings>();
            worldSettings.Initialize();
        }
        
        void Update()
        {
            if (enableRealTimeGeneration && generationQueue.Count > 0)
            {
                ProcessGenerationQueue();
            }
            
            if (enableAutoOptimization && worldOptimizer != null)
            {
                worldOptimizer.UpdateOptimization();
            }
        }
        
        void ProcessGenerationQueue()
        {
            if (currentState == WorldGenerationState.Idle && generationQueue.Count > 0)
            {
                var task = generationQueue.Dequeue();
                ExecuteGenerationTask(task);
            }
        }
        
        void ExecuteGenerationTask(WorldGenerationTask task)
        {
            currentState = WorldGenerationState.Generating;
            OnWorldGenerationStateChanged?.Invoke(currentState);
            
            // 根据任务类型执行相应的生成逻辑
            switch (task.taskType)
            {
                case GenerationTaskType.Terrain:
                    terrainGenerator?.GenerateTerrain(task.parameters);
                    break;
                case GenerationTaskType.Vegetation:
                    placementSystem?.PlaceVegetation(task.parameters);
                    break;
                case GenerationTaskType.Structures:
                    placementSystem?.PlaceStructures(task.parameters);
                    break;
                case GenerationTaskType.Environment:
                    environmentManager?.UpdateEnvironment();
                    break;
            }
        }
        
        // 公共API
        public void GenerateWorld(WorldGenerationParameters parameters)
        {
            Debug.Log("[WorldEditor] 开始生成世界...");
            
            // 创建生成任务序列
            var tasks = CreateGenerationTasks(parameters);
            foreach (var task in tasks)
            {
                generationQueue.Enqueue(task);
            }
        }
        
        List<WorldGenerationTask> CreateGenerationTasks(WorldGenerationParameters parameters)
        {
            var tasks = new List<WorldGenerationTask>();
            
            // 按优先级添加任务
            if (parameters.generateTerrain)
                tasks.Add(new WorldGenerationTask(GenerationTaskType.Terrain, parameters));
                
            if (parameters.generateVegetation)
                tasks.Add(new WorldGenerationTask(GenerationTaskType.Vegetation, parameters));
                
            if (parameters.generateStructures)
                tasks.Add(new WorldGenerationTask(GenerationTaskType.Structures, parameters));
                
            if (parameters.generateEnvironment)
                tasks.Add(new WorldGenerationTask(GenerationTaskType.Environment, parameters));
            
            return tasks;
        }
        
        public void StopGeneration()
        {
            generationQueue.Clear();
            currentState = WorldGenerationState.Idle;
            OnWorldGenerationStateChanged?.Invoke(currentState);
        }
        
        public WorldGenerationState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// 获取世界生成参数
        /// </summary>
        public WorldGenerationParameters GetGenerationParameters()
        {
            if (worldSettings != null)
            {
                return worldSettings.CreateGenerationParameters();
            }
            else
            {
                // 返回默认参数
                var defaultParams = new WorldGenerationParameters();
                defaultParams.Initialize();
                return defaultParams;
            }
        }
        
        /// <summary>
        /// 快速生成世界（使用默认参数）
        /// </summary>
        public void GenerateWorld()
        {
            var parameters = GetGenerationParameters();
            GenerateWorld(parameters);
        }
        
        // ========== 多地形管理API（新增功能，不影响现有系统） ==========
        
        /// <summary>
        /// 启用多地形模式
        /// </summary>
        public void EnableMultiTerrainMode()
        {
            if (multiTerrainManager != null)
            {
                multiTerrainManager.EnableMultiTerrainMode();
                Debug.Log("[WorldEditor] 多地形模式已启用");
            }
            else
            {
                Debug.LogWarning("[WorldEditor] 未找到多地形管理器，请添加MultiTerrainManager组件");
            }
        }
        
        /// <summary>
        /// 禁用多地形模式（回到纯单地形）
        /// </summary>
        public void DisableMultiTerrainMode()
        {
            if (multiTerrainManager != null)
            {
                multiTerrainManager.DisableMultiTerrainMode();
                Debug.Log("[WorldEditor] 已回到单地形模式");
            }
        }
        
        /// <summary>
        /// 扩展地形到指定方向
        /// </summary>
        public void ExpandTerrain(TerrainSystem.TerrainDirection direction, int count = 1)
        {
            if (multiTerrainManager != null && multiTerrainManager.IsMultiTerrainEnabled)
            {
                multiTerrainManager.ExpandTerrain(direction, count);
            }
            else
            {
                Debug.LogWarning("[WorldEditor] 多地形模式未启用，无法扩展地形");
            }
        }
        
        /// <summary>
        /// 获取当前地形数量
        /// </summary>
        public int GetTerrainCount()
        {
            if (multiTerrainManager != null)
                return multiTerrainManager.TerrainCount;
            return terrainGenerator != null ? 1 : 0; // 单地形模式返回1
        }
        
        /// <summary>
        /// 检查是否启用了多地形模式
        /// </summary>
        public bool IsMultiTerrainEnabled()
        {
            return multiTerrainManager != null && multiTerrainManager.IsMultiTerrainEnabled;
        }
    }
    
    // 数据结构定义
    public enum WorldGenerationState
    {
        Idle,
        Generating,
        Optimizing,
        Completed,
        Error
    }
    
    public enum GenerationTaskType
    {
        Terrain,
        Vegetation,
        Structures,
        Environment
    }
    
    public class WorldGenerationTask
    {
        public GenerationTaskType taskType;
        public WorldGenerationParameters parameters;
        
        public WorldGenerationTask(GenerationTaskType type, WorldGenerationParameters param)
        {
            taskType = type;
            parameters = param;
        }
    }
}