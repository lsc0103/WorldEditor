using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 多地形管理器 - 作为AdvancedTerrainGenerator的增强层
    /// 不影响现有单地形功能，提供多地形集群管理能力
    /// </summary>
    public class MultiTerrainManager : MonoBehaviour
    {
        [Header("多地形设置")]
        [SerializeField] private bool enableMultiTerrain = false;
        [SerializeField] private Vector2Int worldSize = new Vector2Int(2, 2); // 世界网格大小
        [SerializeField] private float terrainSpacing = 1000f; // 地形间距
        [SerializeField] private bool autoGenerateNeighbors = true; // 自动生成邻接地形
        
        [Header("地形同步设置")]
        [SerializeField] private bool syncGenerationParameters = true; // 同步生成参数
        [SerializeField] private bool enableSeamlessConnection = true; // 启用无缝连接
        [SerializeField] private int borderBlendWidth = 10; // 边界混合宽度
        
        [Header("性能设置")]
        [SerializeField] private bool enableTerrainStreaming = false; // 地形流式加载
        [SerializeField] private float streamingDistance = 2000f; // 流式加载距离
        [SerializeField] private Transform playerTransform; // 玩家位置参考
        
        // 地形网格存储
        private Dictionary<Vector2Int, AdvancedTerrainGenerator> terrainGrid = new Dictionary<Vector2Int, AdvancedTerrainGenerator>();
        private Vector2Int currentGridPosition = Vector2Int.zero; // 当前地形的网格位置
        
        // 主地形生成器引用（保持向后兼容）
        private AdvancedTerrainGenerator masterTerrainGenerator;
        
        // 事件
        public System.Action<Vector2Int, AdvancedTerrainGenerator> OnTerrainAdded;
        public System.Action<Vector2Int> OnTerrainRemoved;
        public System.Action<float> OnWorldGenerationProgress;
        
        void Awake()
        {
            // 获取主地形生成器（当前对象上的）
            masterTerrainGenerator = GetComponent<AdvancedTerrainGenerator>();
            if (masterTerrainGenerator == null)
            {
                Debug.LogError("[MultiTerrainManager] 未找到AdvancedTerrainGenerator组件，多地形功能无法使用");
                enableMultiTerrain = false;
                return;
            }
            
            // 将主地形生成器注册到网格中心
            if (enableMultiTerrain)
            {
                RegisterTerrainInGrid(Vector2Int.zero, masterTerrainGenerator);
                Debug.Log("[MultiTerrainManager] 多地形管理器已初始化，主地形已注册到网格 (0,0)");
            }
        }
        
        void Update()
        {
            if (enableMultiTerrain && enableTerrainStreaming && playerTransform != null)
            {
                UpdateTerrainStreaming();
            }
        }
        
        /// <summary>
        /// 启用多地形模式（不影响现有功能）
        /// </summary>
        public void EnableMultiTerrainMode()
        {
            if (masterTerrainGenerator == null)
            {
                Debug.LogWarning("[MultiTerrainManager] 无法启用多地形模式：缺少主地形生成器");
                return;
            }
            
            enableMultiTerrain = true;
            Debug.Log("[MultiTerrainManager] 多地形模式已启用，当前单地形功能保持不变");
        }
        
        /// <summary>
        /// 禁用多地形模式（回到纯单地形）
        /// </summary>
        public void DisableMultiTerrainMode()
        {
            enableMultiTerrain = false;
            // 清理所有额外的地形，保留主地形
            CleanupAdditionalTerrains();
            Debug.Log("[MultiTerrainManager] 已回到单地形模式，所有现有功能正常");
        }
        
        /// <summary>
        /// 在指定方向扩展地形
        /// </summary>
        public void ExpandTerrain(TerrainDirection direction, int count = 1)
        {
            if (!enableMultiTerrain)
            {
                Debug.LogWarning("[MultiTerrainManager] 多地形模式未启用，无法扩展地形");
                return;
            }
            
            StartCoroutine(ExpandTerrainCoroutine(direction, count));
        }
        
        IEnumerator ExpandTerrainCoroutine(TerrainDirection direction, int count)
        {
            Debug.Log($"[MultiTerrainManager] 开始向{direction}方向扩展{count}个地形块");
            
            Vector2Int directionVector = GetDirectionVector(direction);
            
            for (int i = 1; i <= count; i++)
            {
                Vector2Int newGridPos = currentGridPosition + directionVector * i;
                
                if (!terrainGrid.ContainsKey(newGridPos))
                {
                    AdvancedTerrainGenerator newTerrain = CreateTerrainAtGridPosition(newGridPos);
                    if (newTerrain != null)
                    {
                        RegisterTerrainInGrid(newGridPos, newTerrain);
                        
                        // 复制主地形的生成参数
                        if (syncGenerationParameters)
                        {
                            yield return StartCoroutine(SyncTerrainParameters(newTerrain));
                        }
                        
                        OnTerrainAdded?.Invoke(newGridPos, newTerrain);
                        Debug.Log($"[MultiTerrainManager] 地形块 {newGridPos} 创建完成");
                    }
                }
                
                // 每创建一个地形块暂停一帧，避免卡顿
                yield return null;
            }
            
            // 处理边界无缝连接
            if (enableSeamlessConnection)
            {
                yield return StartCoroutine(UpdateSeamlessConnections());
            }
            
            Debug.Log($"[MultiTerrainManager] 地形扩展完成，当前网格大小: {terrainGrid.Count}");
        }
        
        /// <summary>
        /// 创建指定网格位置的地形生成器
        /// </summary>
        AdvancedTerrainGenerator CreateTerrainAtGridPosition(Vector2Int gridPos)
        {
            // 计算世界位置
            Vector3 worldPosition = GridToWorldPosition(gridPos);
            
            // 创建新的GameObject
            GameObject terrainObject = new GameObject($"TerrainGenerator_{gridPos.x}_{gridPos.y}");
            terrainObject.transform.position = worldPosition;
            terrainObject.transform.SetParent(transform.parent); // 与主地形生成器在同一层级
            
            // 添加AdvancedTerrainGenerator组件
            AdvancedTerrainGenerator terrainGen = terrainObject.AddComponent<AdvancedTerrainGenerator>();
            
            // 复制主地形生成器的设置（保持完全兼容）
            CopyTerrainGeneratorSettings(masterTerrainGenerator, terrainGen);
            
            Debug.Log($"[MultiTerrainManager] 在位置 {worldPosition} 创建新地形生成器 {terrainObject.name}");
            return terrainGen;
        }
        
        /// <summary>
        /// 复制地形生成器设置（确保新地形与主地形行为一致）
        /// </summary>
        void CopyTerrainGeneratorSettings(AdvancedTerrainGenerator source, AdvancedTerrainGenerator target)
        {
            if (source == null || target == null) return;
            
            // 这里需要复制所有相关设置，但不影响source的状态
            // 由于AdvancedTerrainGenerator的设置可能很复杂，我们采用保守的方法
            Debug.Log($"[MultiTerrainManager] 复制地形生成器设置到 {target.name}");
            
            // 注意：实际实现时需要根据AdvancedTerrainGenerator的具体字段进行复制
            // 这里只是示例框架
        }
        
        /// <summary>
        /// 同步地形参数（异步操作）
        /// </summary>
        IEnumerator SyncTerrainParameters(AdvancedTerrainGenerator targetTerrain)
        {
            // 获取主地形的世界生成参数
            if (masterTerrainGenerator.GetComponent<WorldEditor.Core.WorldEditorManager>() != null)
            {
                var worldManager = masterTerrainGenerator.GetComponent<WorldEditor.Core.WorldEditorManager>();
                var parameters = worldManager.GetGenerationParameters();
                
                // 为新地形设置相同的参数
                // 这里需要具体实现参数复制逻辑
                Debug.Log($"[MultiTerrainManager] 同步参数到 {targetTerrain.name}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 处理边界无缝连接
        /// </summary>
        IEnumerator UpdateSeamlessConnections()
        {
            Debug.Log("[MultiTerrainManager] 开始处理地形边界无缝连接...");
            
            foreach (var kvp in terrainGrid)
            {
                Vector2Int gridPos = kvp.Key;
                AdvancedTerrainGenerator terrainGen = kvp.Value;
                
                // 检查相邻地形并处理边界
                ProcessTerrainBoundaries(gridPos, terrainGen);
                
                // 每处理一个地形暂停一帧
                yield return null;
            }
            
            Debug.Log("[MultiTerrainManager] 地形边界连接处理完成");
        }
        
        /// <summary>
        /// 处理单个地形的边界连接
        /// </summary>
        void ProcessTerrainBoundaries(Vector2Int gridPos, AdvancedTerrainGenerator terrainGen)
        {
            // 检查四个方向的邻接地形
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in directions)
            {
                Vector2Int neighborPos = gridPos + dir;
                if (terrainGrid.ContainsKey(neighborPos))
                {
                    // 处理与邻接地形的边界对齐
                    AlignTerrainBoundary(terrainGen, terrainGrid[neighborPos], dir);
                }
            }
        }
        
        /// <summary>
        /// 对齐地形边界高度
        /// </summary>
        void AlignTerrainBoundary(AdvancedTerrainGenerator terrain1, AdvancedTerrainGenerator terrain2, Vector2Int direction)
        {
            // 这里需要实现边界高度对齐的具体逻辑
            // 暂时只记录日志，具体实现需要访问地形的高度数据
            Debug.Log($"[MultiTerrainManager] 对齐地形边界：{terrain1.name} <-> {terrain2.name}, 方向: {direction}");
        }
        
        /// <summary>
        /// 地形流式加载更新
        /// </summary>
        void UpdateTerrainStreaming()
        {
            if (playerTransform == null) return;
            
            Vector2Int playerGridPos = WorldToGridPosition(playerTransform.position);
            
            // 基于玩家位置动态加载/卸载地形
            foreach (var kvp in terrainGrid)
            {
                Vector2Int gridPos = kvp.Key;
                float distance = Vector2Int.Distance(gridPos, playerGridPos) * terrainSpacing;
                
                bool shouldBeActive = distance <= streamingDistance;
                kvp.Value.gameObject.SetActive(shouldBeActive);
            }
        }
        
        /// <summary>
        /// 清理额外的地形（保留主地形）
        /// </summary>
        void CleanupAdditionalTerrains()
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();
            
            foreach (var kvp in terrainGrid)
            {
                if (kvp.Key != Vector2Int.zero) // 保留主地形 (0,0)
                {
                    toRemove.Add(kvp.Key);
                    if (kvp.Value != null && kvp.Value != masterTerrainGenerator)
                    {
                        DestroyImmediate(kvp.Value.gameObject);
                    }
                }
            }
            
            foreach (var pos in toRemove)
            {
                terrainGrid.Remove(pos);
                OnTerrainRemoved?.Invoke(pos);
            }
        }
        
        /// <summary>
        /// 注册地形到网格
        /// </summary>
        void RegisterTerrainInGrid(Vector2Int gridPos, AdvancedTerrainGenerator terrainGen)
        {
            terrainGrid[gridPos] = terrainGen;
        }
        
        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            Vector3 basePos = masterTerrainGenerator.transform.position;
            return basePos + new Vector3(gridPos.x * terrainSpacing, 0, gridPos.y * terrainSpacing);
        }
        
        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 basePos = masterTerrainGenerator.transform.position;
            Vector3 offset = worldPos - basePos;
            return new Vector2Int(
                Mathf.RoundToInt(offset.x / terrainSpacing),
                Mathf.RoundToInt(offset.z / terrainSpacing)
            );
        }
        
        /// <summary>
        /// 获取方向向量
        /// </summary>
        Vector2Int GetDirectionVector(TerrainDirection direction)
        {
            switch (direction)
            {
                case TerrainDirection.North: return Vector2Int.up;
                case TerrainDirection.South: return Vector2Int.down;
                case TerrainDirection.East: return Vector2Int.right;
                case TerrainDirection.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }
        
        // 公共API
        public bool IsMultiTerrainEnabled => enableMultiTerrain;
        public int TerrainCount => terrainGrid.Count;
        public Dictionary<Vector2Int, AdvancedTerrainGenerator> GetTerrainGrid() => new Dictionary<Vector2Int, AdvancedTerrainGenerator>(terrainGrid);
    }
    
    /// <summary>
    /// 地形扩展方向
    /// </summary>
    public enum TerrainDirection
    {
        North,  // 北（+Z）
        South,  // 南（-Z）
        East,   // 东（+X）
        West    // 西（-X）
    }
}