using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Optimization
{
    /// <summary>
    /// 大世界管理器 - 支持超大规模世界的流式加载和优化
    /// 提供分块管理、动态加载卸载、内存优化等功能
    /// </summary>
    public class LargeWorldManager : MonoBehaviour
    {
        [Header("世界分块设置")]
        [SerializeField] private Vector2 chunkSize = new Vector2(500f, 500f);
        [SerializeField] private int loadRadius = 3; // 加载半径（以块为单位）
        [SerializeField] private int unloadRadius = 5; // 卸载半径
        [SerializeField] private Vector2 worldBounds = new Vector2(10000f, 10000f);
        
        [Header("流式加载")]
        [SerializeField] private bool enableStreamingLoading = true;
        [SerializeField] private float loadingCheckInterval = 1f;
        [SerializeField] private int maxChunksPerFrame = 2;
        [SerializeField] private bool enablePreloading = true;
        
        [Header("内存管理")]
        [SerializeField] private long maxMemoryUsage = 1024 * 1024 * 1024; // 1GB
        [SerializeField] private bool enableMemoryOptimization = true;
        [SerializeField] private float memoryCheckInterval = 5f;
        
        [Header("LOD系统")]
        [SerializeField] private bool enableWorldLOD = true;
        [SerializeField] private float[] lodDistances = { 250f, 500f, 1000f, 2000f };
        [SerializeField] private LODQuality[] lodQualities;
        
        // 分块管理
        private Dictionary<Vector2Int, WorldChunk> loadedChunks = new Dictionary<Vector2Int, WorldChunk>();
        private Dictionary<Vector2Int, WorldChunk> loadingChunks = new Dictionary<Vector2Int, WorldChunk>();
        private Queue<Vector2Int> loadQueue = new Queue<Vector2Int>();
        private Queue<Vector2Int> unloadQueue = new Queue<Vector2Int>();
        
        // 玩家跟踪
        private Transform playerTransform;
        private Vector2Int currentPlayerChunk;
        private Vector2Int lastPlayerChunk;
        
        // 系统状态
        private bool isStreamingActive = false;
        private Coroutine streamingCoroutine;
        private Coroutine memoryManagementCoroutine;
        
        // 统计信息
        private WorldStatistics worldStats = new WorldStatistics();
        
        // 事件
        public System.Action<WorldChunk> OnChunkLoaded;
        public System.Action<WorldChunk> OnChunkUnloaded;
        public System.Action<WorldStatistics> OnStatisticsUpdated;

        void Awake()
        {
            InitializeLargeWorldManager();
        }

        void Start()
        {
            StartWorldStreaming();
        }

        void Update()
        {
            if (enableStreamingLoading)
            {
                UpdatePlayerPosition();
                CheckChunkLoadingRequests();
            }
        }

        /// <summary>
        /// 初始化大世界管理器
        /// </summary>
        void InitializeLargeWorldManager()
        {
            UnityEngine.Debug.Log("[LargeWorld] 大世界管理器初始化");
            
            // 寻找玩家对象
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
                else
                {
                    // 如果没有玩家，使用主摄像机
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        playerTransform = camera.transform;
                    }
                }
            }
            
            // 初始化LOD质量设置
            if (lodQualities == null || lodQualities.Length != lodDistances.Length)
            {
                InitializeLODQualities();
            }
            
            // 预计算世界分块
            CalculateWorldChunks();
        }

        /// <summary>
        /// 开始世界流式加载
        /// </summary>
        void StartWorldStreaming()
        {
            if (enableStreamingLoading && playerTransform != null)
            {
                isStreamingActive = true;
                streamingCoroutine = StartCoroutine(WorldStreamingLoop());
                
                if (enableMemoryOptimization)
                {
                    memoryManagementCoroutine = StartCoroutine(MemoryManagementLoop());
                }
                
                UnityEngine.Debug.Log("[LargeWorld] 世界流式加载已启动");
            }
        }

        /// <summary>
        /// 停止世界流式加载
        /// </summary>
        public void StopWorldStreaming()
        {
            isStreamingActive = false;
            
            if (streamingCoroutine != null)
            {
                StopCoroutine(streamingCoroutine);
                streamingCoroutine = null;
            }
            
            if (memoryManagementCoroutine != null)
            {
                StopCoroutine(memoryManagementCoroutine);
                memoryManagementCoroutine = null;
            }
            
            UnityEngine.Debug.Log("[LargeWorld] 世界流式加载已停止");
        }

        /// <summary>
        /// 世界流式加载循环
        /// </summary>
        IEnumerator WorldStreamingLoop()
        {
            while (isStreamingActive)
            {
                if (playerTransform != null)
                {
                    // 检查玩家位置变化
                    UpdatePlayerChunk();
                    
                    // 处理分块加载/卸载
                    ProcessChunkQueue();
                    
                    // 更新LOD
                    if (enableWorldLOD)
                    {
                        UpdateWorldLOD();
                    }
                    
                    // 更新统计信息
                    UpdateStatistics();
                }
                
                yield return new WaitForSeconds(loadingCheckInterval);
            }
        }

        /// <summary>
        /// 内存管理循环
        /// </summary>
        IEnumerator MemoryManagementLoop()
        {
            while (isStreamingActive)
            {
                // 检查内存使用情况
                long currentMemory = System.GC.GetTotalMemory(false);
                
                if (currentMemory > maxMemoryUsage)
                {
                    UnityEngine.Debug.LogWarning($"[LargeWorld] 内存使用过高: {currentMemory / (1024*1024)}MB");
                    
                    // 执行内存优化
                    yield return StartCoroutine(OptimizeMemoryUsage());
                }
                
                yield return new WaitForSeconds(memoryCheckInterval);
            }
        }

        /// <summary>
        /// 更新玩家位置
        /// </summary>
        void UpdatePlayerPosition()
        {
            if (playerTransform == null) return;
            
            Vector3 playerPos = playerTransform.position;
            currentPlayerChunk = WorldPositionToChunk(new Vector2(playerPos.x, playerPos.z));
        }

        /// <summary>
        /// 更新玩家分块
        /// </summary>
        void UpdatePlayerChunk()
        {
            if (currentPlayerChunk != lastPlayerChunk)
            {
                UnityEngine.Debug.Log($"[LargeWorld] 玩家移动到分块: {currentPlayerChunk}");
                
                // 更新需要加载的分块
                UpdateLoadingQueue();
                
                // 更新需要卸载的分块
                UpdateUnloadingQueue();
                
                lastPlayerChunk = currentPlayerChunk;
            }
        }

        /// <summary>
        /// 更新加载队列
        /// </summary>
        void UpdateLoadingQueue()
        {
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                for (int z = -loadRadius; z <= loadRadius; z++)
                {
                    Vector2Int chunkCoord = currentPlayerChunk + new Vector2Int(x, z);
                    
                    // 检查分块是否在世界范围内
                    if (IsChunkInWorldBounds(chunkCoord))
                    {
                        // 如果分块未加载且不在加载队列中，添加到加载队列
                        if (!loadedChunks.ContainsKey(chunkCoord) && 
                            !loadingChunks.ContainsKey(chunkCoord) &&
                            !loadQueue.Contains(chunkCoord))
                        {
                            loadQueue.Enqueue(chunkCoord);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新卸载队列
        /// </summary>
        void UpdateUnloadingQueue()
        {
            var chunksToUnload = new List<Vector2Int>();
            
            foreach (var chunkCoord in loadedChunks.Keys)
            {
                float distance = Vector2Int.Distance(chunkCoord, currentPlayerChunk);
                
                if (distance > unloadRadius)
                {
                    chunksToUnload.Add(chunkCoord);
                }
            }
            
            foreach (var chunkCoord in chunksToUnload)
            {
                if (!unloadQueue.Contains(chunkCoord))
                {
                    unloadQueue.Enqueue(chunkCoord);
                }
            }
        }

        /// <summary>
        /// 处理分块队列
        /// </summary>
        void ProcessChunkQueue()
        {
            int processedThisFrame = 0;
            
            // 处理卸载队列
            while (unloadQueue.Count > 0 && processedThisFrame < maxChunksPerFrame)
            {
                Vector2Int chunkCoord = unloadQueue.Dequeue();
                StartCoroutine(UnloadChunk(chunkCoord));
                processedThisFrame++;
            }
            
            // 处理加载队列
            while (loadQueue.Count > 0 && processedThisFrame < maxChunksPerFrame)
            {
                Vector2Int chunkCoord = loadQueue.Dequeue();
                StartCoroutine(LoadChunk(chunkCoord));
                processedThisFrame++;
            }
        }

        /// <summary>
        /// 检查分块加载请求
        /// </summary>
        void CheckChunkLoadingRequests()
        {
            // 检查是否有预加载请求
            if (enablePreloading)
            {
                CheckPreloadingRequests();
            }
        }

        /// <summary>
        /// 检查预加载请求
        /// </summary>
        void CheckPreloadingRequests()
        {
            // 基于玩家移动方向预测需要预加载的分块
            // 这里可以实现更复杂的预测算法
        }

        /// <summary>
        /// 加载分块
        /// </summary>
        IEnumerator LoadChunk(Vector2Int chunkCoord)
        {
            if (loadedChunks.ContainsKey(chunkCoord) || loadingChunks.ContainsKey(chunkCoord))
                yield break;
            
            UnityEngine.Debug.Log($"[LargeWorld] 开始加载分块: {chunkCoord}");
            
            // 创建分块对象
            WorldChunk chunk = new WorldChunk(chunkCoord, chunkSize);
            loadingChunks[chunkCoord] = chunk;
            
            // 生成分块内容
            yield return StartCoroutine(GenerateChunkContent(chunk));
            
            // 移动到已加载列表
            loadingChunks.Remove(chunkCoord);
            loadedChunks[chunkCoord] = chunk;
            
            OnChunkLoaded?.Invoke(chunk);
            worldStats.chunksLoaded++;
            
            UnityEngine.Debug.Log($"[LargeWorld] 分块加载完成: {chunkCoord}");
        }

        /// <summary>
        /// 卸载分块
        /// </summary>
        IEnumerator UnloadChunk(Vector2Int chunkCoord)
        {
            if (!loadedChunks.ContainsKey(chunkCoord))
                yield break;
            
            UnityEngine.Debug.Log($"[LargeWorld] 开始卸载分块: {chunkCoord}");
            
            WorldChunk chunk = loadedChunks[chunkCoord];
            
            // 卸载分块内容
            yield return StartCoroutine(UnloadChunkContent(chunk));
            
            // 从已加载列表移除
            loadedChunks.Remove(chunkCoord);
            
            OnChunkUnloaded?.Invoke(chunk);
            worldStats.chunksUnloaded++;
            
            UnityEngine.Debug.Log($"[LargeWorld] 分块卸载完成: {chunkCoord}");
        }

        /// <summary>
        /// 生成分块内容
        /// </summary>
        IEnumerator GenerateChunkContent(WorldChunk chunk)
        {
            // 集成地形生成
            var terrainGen = Object.FindFirstObjectByType<WorldEditor.TerrainSystem.AdvancedTerrainGenerator>();
            if (terrainGen != null)
            {
                yield return StartCoroutine(GenerateChunkTerrain(chunk, terrainGen));
            }
            
            // 集成放置系统
            var placementSystem = Object.FindFirstObjectByType<WorldEditor.Placement.SmartPlacementSystem>();
            if (placementSystem != null)
            {
                yield return StartCoroutine(GenerateChunkPlacements(chunk, placementSystem));
            }
            
            // 应用LOD
            ApplyChunkLOD(chunk);
        }

        /// <summary>
        /// 生成分块地形
        /// </summary>
        IEnumerator GenerateChunkTerrain(WorldChunk chunk, WorldEditor.TerrainSystem.AdvancedTerrainGenerator terrainGen)
        {
            // 为分块创建地形参数
            var chunkParams = CreateChunkGenerationParameters(chunk);
            
            // 这里需要扩展地形生成器以支持分块生成
            // 当前只是模拟
            yield return new WaitForSeconds(0.1f);
            
            chunk.hasTerrainGenerated = true;
        }

        /// <summary>
        /// 生成分块放置
        /// </summary>
        IEnumerator GenerateChunkPlacements(WorldChunk chunk, WorldEditor.Placement.SmartPlacementSystem placementSystem)
        {
            // 为分块创建放置参数
            var chunkParams = CreateChunkGenerationParameters(chunk);
            
            // 这里需要扩展放置系统以支持分块生成
            // 当前只是模拟
            yield return new WaitForSeconds(0.1f);
            
            chunk.hasPlacementsGenerated = true;
        }

        /// <summary>
        /// 卸载分块内容
        /// </summary>
        IEnumerator UnloadChunkContent(WorldChunk chunk)
        {
            // 销毁分块中的游戏对象
            if (chunk.chunkGameObject != null)
            {
                DestroyImmediate(chunk.chunkGameObject);
            }
            
            // 清理内存
            System.GC.Collect();
            
            yield return null;
        }

        /// <summary>
        /// 应用分块LOD
        /// </summary>
        void ApplyChunkLOD(WorldChunk chunk)
        {
            if (!enableWorldLOD) return;
            
            float distanceToPlayer = Vector2.Distance(chunk.worldPosition, 
                new Vector2(playerTransform.position.x, playerTransform.position.z));
            
            // 确定LOD级别
            int lodLevel = CalculateLODLevel(distanceToPlayer);
            chunk.currentLODLevel = lodLevel;
            
            // 应用LOD设置
            ApplyLODToChunk(chunk, lodLevel);
        }

        /// <summary>
        /// 更新世界LOD
        /// </summary>
        void UpdateWorldLOD()
        {
            foreach (var chunk in loadedChunks.Values)
            {
                ApplyChunkLOD(chunk);
            }
        }

        /// <summary>
        /// 计算LOD级别
        /// </summary>
        int CalculateLODLevel(float distance)
        {
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance <= lodDistances[i])
                {
                    return i;
                }
            }
            
            return lodDistances.Length - 1;
        }

        /// <summary>
        /// 应用LOD到分块
        /// </summary>
        void ApplyLODToChunk(WorldChunk chunk, int lodLevel)
        {
            if (lodLevel < 0 || lodLevel >= lodQualities.Length) return;
            
            LODQuality quality = lodQualities[lodLevel];
            
            // 应用质量设置到分块
            // 这里可以实现具体的LOD逻辑
            chunk.lodQuality = quality;
        }

        /// <summary>
        /// 优化内存使用
        /// </summary>
        IEnumerator OptimizeMemoryUsage()
        {
            UnityEngine.Debug.Log("[LargeWorld] 开始内存优化");
            
            // 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            yield return null;
            
            // 卸载距离最远的分块
            if (loadedChunks.Count > 0)
            {
                var farthestChunk = FindFarthestChunk();
                if (farthestChunk.HasValue)
                {
                    yield return StartCoroutine(UnloadChunk(farthestChunk.Value));
                }
            }
            
            UnityEngine.Debug.Log("[LargeWorld] 内存优化完成");
        }

        /// <summary>
        /// 寻找最远的分块
        /// </summary>
        Vector2Int? FindFarthestChunk()
        {
            Vector2Int? farthestChunk = null;
            float maxDistance = 0f;
            
            foreach (var chunkCoord in loadedChunks.Keys)
            {
                float distance = Vector2Int.Distance(chunkCoord, currentPlayerChunk);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestChunk = chunkCoord;
                }
            }
            
            return farthestChunk;
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        void UpdateStatistics()
        {
            worldStats.loadedChunkCount = loadedChunks.Count;
            worldStats.loadingChunkCount = loadingChunks.Count;
            worldStats.loadQueueCount = loadQueue.Count;
            worldStats.unloadQueueCount = unloadQueue.Count;
            worldStats.currentMemoryUsage = System.GC.GetTotalMemory(false);
            
            OnStatisticsUpdated?.Invoke(worldStats);
        }

        /// <summary>
        /// 初始化LOD质量
        /// </summary>
        void InitializeLODQualities()
        {
            lodQualities = new LODQuality[lodDistances.Length];
            
            for (int i = 0; i < lodQualities.Length; i++)
            {
                lodQualities[i] = new LODQuality
                {
                    level = i,
                    detailMultiplier = 1f / (i + 1),
                    textureResolution = Mathf.Max(256, 1024 / (i + 1)),
                    enableShadows = i < 2,
                    enableLighting = true
                };
            }
        }

        /// <summary>
        /// 计算世界分块
        /// </summary>
        void CalculateWorldChunks()
        {
            Vector2Int chunksCount = new Vector2Int(
                Mathf.CeilToInt(worldBounds.x / chunkSize.x),
                Mathf.CeilToInt(worldBounds.y / chunkSize.y)
            );
            
            worldStats.totalChunkCount = chunksCount.x * chunksCount.y;
            
            UnityEngine.Debug.Log($"[LargeWorld] 世界分块计算完成: {chunksCount.x} x {chunksCount.y} = {worldStats.totalChunkCount}");
        }

        /// <summary>
        /// 创建分块生成参数
        /// </summary>
        WorldGenerationParameters CreateChunkGenerationParameters(WorldChunk chunk)
        {
            var parameters = new WorldGenerationParameters();
            
            // 设置分块特定的生成边界
            parameters.generationBounds = new Bounds(
                new Vector3(chunk.worldPosition.x, 0f, chunk.worldPosition.y),
                new Vector3(chunk.size.x, 1000f, chunk.size.y)
            );
            
            parameters.areaSize = chunk.size;
            
            return parameters;
        }

        /// <summary>
        /// 世界坐标转分块坐标
        /// </summary>
        Vector2Int WorldPositionToChunk(Vector2 worldPosition)
        {
            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize.x);
            int chunkY = Mathf.FloorToInt(worldPosition.y / chunkSize.y);
            
            return new Vector2Int(chunkX, chunkY);
        }

        /// <summary>
        /// 分块坐标转世界坐标
        /// </summary>
        Vector2 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            return new Vector2(
                chunkCoord.x * chunkSize.x,
                chunkCoord.y * chunkSize.y
            );
        }

        /// <summary>
        /// 检查分块是否在世界边界内
        /// </summary>
        bool IsChunkInWorldBounds(Vector2Int chunkCoord)
        {
            Vector2 worldPos = ChunkToWorldPosition(chunkCoord);
            
            return worldPos.x >= -worldBounds.x * 0.5f && 
                   worldPos.x <= worldBounds.x * 0.5f &&
                   worldPos.y >= -worldBounds.y * 0.5f && 
                   worldPos.y <= worldBounds.y * 0.5f;
        }

        /// <summary>
        /// 获取已加载分块数量
        /// </summary>
        public int GetLoadedChunkCount()
        {
            return loadedChunks.Count;
        }

        /// <summary>
        /// 获取世界统计信息
        /// </summary>
        public WorldStatistics GetWorldStatistics()
        {
            return worldStats;
        }

        /// <summary>
        /// 设置玩家跟踪对象
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        void OnDestroy()
        {
            StopWorldStreaming();
        }

        void OnDrawGizmosSelected()
        {
            // 绘制已加载的分块
            Gizmos.color = Color.green;
            foreach (var chunk in loadedChunks.Values)
            {
                Vector3 center = new Vector3(chunk.worldPosition.x, 0f, chunk.worldPosition.y);
                Vector3 size = new Vector3(chunk.size.x, 10f, chunk.size.y);
                Gizmos.DrawWireCube(center, size);
            }
            
            // 绘制正在加载的分块
            Gizmos.color = Color.yellow;
            foreach (var chunk in loadingChunks.Values)
            {
                Vector3 center = new Vector3(chunk.worldPosition.x, 0f, chunk.worldPosition.y);
                Vector3 size = new Vector3(chunk.size.x, 10f, chunk.size.y);
                Gizmos.DrawWireCube(center, size);
            }
            
            // 绘制玩家位置
            if (playerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(playerTransform.position, 10f);
            }
        }
    }

    /// <summary>
    /// 世界分块
    /// </summary>
    [System.Serializable]
    public class WorldChunk
    {
        public Vector2Int coordinate;
        public Vector2 worldPosition;
        public Vector2 size;
        public GameObject chunkGameObject;
        public bool hasTerrainGenerated = false;
        public bool hasPlacementsGenerated = false;
        public int currentLODLevel = 0;
        public LODQuality lodQuality;
        public float lastAccessTime;
        
        public WorldChunk(Vector2Int coord, Vector2 chunkSize)
        {
            coordinate = coord;
            size = chunkSize;
            worldPosition = new Vector2(coord.x * chunkSize.x, coord.y * chunkSize.y);
            lastAccessTime = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// LOD质量设置
    /// </summary>
    [System.Serializable]
    public class LODQuality
    {
        public int level;
        public float detailMultiplier = 1f;
        public int textureResolution = 1024;
        public bool enableShadows = true;
        public bool enableLighting = true;
        public float cullingDistance = 1000f;
    }

    /// <summary>
    /// 世界统计信息
    /// </summary>
    [System.Serializable]
    public class WorldStatistics
    {
        public int totalChunkCount;
        public int loadedChunkCount;
        public int loadingChunkCount;
        public int loadQueueCount;
        public int unloadQueueCount;
        public int chunksLoaded;
        public int chunksUnloaded;
        public long currentMemoryUsage;
    }
}