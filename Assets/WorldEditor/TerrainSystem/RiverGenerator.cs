using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 河流生成器 - 生成真实感的河流系统
    /// 支持从源头到河口的完整河流网络生成
    /// </summary>
    public class RiverGenerator : MonoBehaviour
    {
        [Header("河流设置")]
        [SerializeField] private int maxRivers = 5;
        [SerializeField] private float riverWidth = 2f;
        [SerializeField] private float riverDepth = 3f;
        [SerializeField] private float minFlowRate = 0.1f;
        [SerializeField] private float maxFlowRate = 2f;
        [SerializeField] private float meanderingStrength = 0.3f;
        
        [Header("水源设置")]
        [SerializeField] private float minSourceHeight = 0.7f;
        [SerializeField] private float sourceRadius = 10f;
        [SerializeField] private int maxSourceAttempts = 100;
        
        [Header("河流追踪")]
        [SerializeField] private int maxRiverLength = 1000;
        [SerializeField] private float stepSize = 1f;
        [SerializeField] private float gravityInfluence = 1f;
        [SerializeField] private float momentumInfluence = 0.5f;
        
        // 河流数据结构
        [System.Serializable]
        public class RiverPoint
        {
            public Vector2 position;
            public float width;
            public float depth;
            public float flowRate;
            public Vector2 direction;
        }
        
        [System.Serializable]
        public class River
        {
            public List<RiverPoint> points = new List<RiverPoint>();
            public Vector2 source;
            public Vector2 mouth;
            public float totalLength;
            public int tributaryCount;
        }
        
        private List<River> generatedRivers = new List<River>();
        private Vector2Int mapResolution;
        
        /// <summary>
        /// 生成河流系统（同步版本）
        /// </summary>
        public void GenerateRivers(ref float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters)
        {
            mapResolution = resolution;
            generatedRivers.Clear();
            
            if (!parameters.generateRivers) return;
            
            Debug.Log("[RiverGenerator] 开始生成河流系统...");
            
            // 寻找河流源头
            List<Vector2> sources = FindRiverSources(heightMap, resolution);
            
            // 为每个源头生成河流
            foreach (Vector2 source in sources)
            {
                if (generatedRivers.Count >= maxRivers) break;
                
                River river = TraceRiver(heightMap, resolution, source);
                if (river != null && river.points.Count > 10)
                {
                    generatedRivers.Add(river);
                    CarveRiverIntoTerrain(ref heightMap, resolution, river);
                }
            }
            
            Debug.Log($"[RiverGenerator] 生成了 {generatedRivers.Count} 条河流");
        }
        
        /// <summary>
        /// 生成河流系统（异步版本）
        /// </summary>
        public IEnumerator GenerateRiversProgressive(float[,] heightMap, Vector2Int resolution, TerrainGenerationParams parameters, int stepsPerFrame)
        {
            mapResolution = resolution;
            generatedRivers.Clear();
            
            if (!parameters.generateRivers) yield break;
            
            Debug.Log("[RiverGenerator] 开始渐进式生成河流系统...");
            
            // 寻找河流源头
            List<Vector2> sources = FindRiverSources(heightMap, resolution);
            
            int processedSteps = 0;
            
            // 为每个源头生成河流
            foreach (Vector2 source in sources)
            {
                if (generatedRivers.Count >= maxRivers) break;
                
                River river = TraceRiver(heightMap, resolution, source);
                if (river != null && river.points.Count > 10)
                {
                    generatedRivers.Add(river);
                    
                    // 雕刻河流到地形中
                    yield return StartCoroutine(CarveRiverIntoTerrainProgressive(heightMap, resolution, river, stepsPerFrame));
                }
                
                processedSteps++;
                if (processedSteps >= stepsPerFrame)
                {
                    processedSteps = 0;
                    yield return null;
                }
            }
            
            Debug.Log($"[RiverGenerator] 生成了 {generatedRivers.Count} 条河流");
        }
        
        /// <summary>
        /// 寻找河流源头
        /// </summary>
        List<Vector2> FindRiverSources(float[,] heightMap, Vector2Int resolution)
        {
            List<Vector2> sources = new List<Vector2>();
            
            Debug.Log($"[RiverGenerator] 智能寻找河流源头 - 目标河流数: {maxRivers}");
            Debug.Log($"[RiverGenerator] 源头高度要求: >= {minSourceHeight:F3}, 源头半径: {sourceRadius}");
            
            // 先检查地形高度分布
            float minH = float.MaxValue, maxH = float.MinValue;
            for (int x = 0; x < resolution.x; x++)
            {
                for (int y = 0; y < resolution.y; y++)
                {
                    float h = heightMap[x, y];
                    minH = Mathf.Min(minH, h);
                    maxH = Mathf.Max(maxH, h);
                }
            }
            Debug.Log($"[RiverGenerator] 高度图实际范围: {minH:F3} - {maxH:F3}");
            
            // 智能搜索：找到所有符合条件的高点，按高度排序
            List<System.Tuple<float, Vector2>> candidatePoints = new List<System.Tuple<float, Vector2>>();
            
            int margin = Mathf.RoundToInt(sourceRadius);
            for (int x = margin; x < resolution.x - margin; x++)
            {
                for (int y = margin; y < resolution.y - margin; y++)
                {
                    float height = heightMap[x, y];
                    
                    // 检查是否符合高度要求
                    if (height >= minSourceHeight)
                    {
                        // 检查是否是局部高点
                        if (IsLocalPeak(heightMap, resolution, x, y, sourceRadius))
                        {
                            candidatePoints.Add(new System.Tuple<float, Vector2>(height, new Vector2(x, y)));
                        }
                    }
                }
            }
            
            Debug.Log($"[RiverGenerator] 找到 {candidatePoints.Count} 个候选源头点");
            
            // 按高度降序排序（最高的点优先）
            candidatePoints.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            
            // 选择最佳的源头点，确保它们之间有足够距离
            for (int i = 0; i < candidatePoints.Count && sources.Count < maxRivers; i++)
            {
                Vector2 candidate = candidatePoints[i].Item2;
                float candidateHeight = candidatePoints[i].Item1;
                
                // 检查与已选择源头的距离
                bool tooClose = false;
                foreach (Vector2 existingSource in sources)
                {
                    if (Vector2.Distance(candidate, existingSource) < sourceRadius * 2)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (!tooClose)
                {
                    sources.Add(candidate);
                    Debug.Log($"[RiverGenerator] 选择源头点: ({candidate.x:F0},{candidate.y:F0}) 高度: {candidateHeight:F3}");
                }
            }
            
            Debug.Log($"[RiverGenerator] 最终选择了 {sources.Count} 个河流源头");
            return sources;
        }
        
        /// <summary>
        /// 检查是否是局部峰值
        /// </summary>
        bool IsLocalPeak(float[,] heightMap, Vector2Int resolution, int centerX, int centerY, float radius)
        {
            float centerHeight = heightMap[centerX, centerY];
            int checkRadius = Mathf.RoundToInt(radius);
            
            for (int x = centerX - checkRadius; x <= centerX + checkRadius; x++)
            {
                for (int y = centerY - checkRadius; y <= centerY + checkRadius; y++)
                {
                    if (x < 0 || x >= resolution.x || y < 0 || y >= resolution.y) continue;
                    if (x == centerX && y == centerY) continue;
                    
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance <= radius && heightMap[x, y] > centerHeight)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 追踪河流路径
        /// </summary>
        River TraceRiver(float[,] heightMap, Vector2Int resolution, Vector2 source)
        {
            River river = new River();
            river.source = source;
            
            Vector2 currentPos = source;
            Vector2 currentDirection = Vector2.zero;
            float currentWidth = riverWidth * 0.3f; // 源头较窄
            float currentFlowRate = minFlowRate;
            
            for (int step = 0; step < maxRiverLength; step++)
            {
                // 计算当前高度
                float currentHeight = GetHeightAtPosition(heightMap, resolution, currentPos);
                
                // 寻找最佳下游方向
                Vector2 gradientDirection = CalculateGradientDirection(heightMap, resolution, currentPos);
                
                // 应用重力影响增强梯度方向
                gradientDirection *= gravityInfluence;
                
                // 应用动量和曲流
                Vector2 newDirection = Vector2.Lerp(gradientDirection, currentDirection, momentumInfluence);
                
                // 添加曲流效果（河流自然弯曲）
                Vector2 meanderingOffset = GetMeanderingOffset(currentDirection, step);
                newDirection += meanderingOffset * meanderingStrength;
                
                newDirection.Normalize();
                
                // 移动到下一个位置
                Vector2 nextPos = currentPos + newDirection * stepSize;
                
                // 边界检查
                if (nextPos.x < 0 || nextPos.x >= resolution.x - 1 || nextPos.y < 0 || nextPos.y >= resolution.y - 1)
                    break;
                
                float nextHeight = GetHeightAtPosition(heightMap, resolution, nextPos);
                
                // 如果高度增加，河流结束
                if (nextHeight > currentHeight)
                    break;
                
                // 检查是否到达水体（海拔很低的地方）
                if (nextHeight < 0.1f)
                {
                    river.mouth = nextPos;
                    break;
                }
                
                // 创建河流点
                RiverPoint point = new RiverPoint
                {
                    position = currentPos,
                    width = currentWidth,
                    depth = riverDepth * (currentFlowRate / maxFlowRate),
                    flowRate = currentFlowRate,
                    direction = newDirection
                };
                
                river.points.Add(point);
                
                // 更新河流属性
                currentPos = nextPos;
                currentDirection = newDirection;
                currentWidth = Mathf.Min(riverWidth, currentWidth + 0.01f); // 河流逐渐变宽
                currentFlowRate = Mathf.Min(maxFlowRate, currentFlowRate + 0.001f); // 流量逐渐增加
            }
            
            // 计算河流总长度
            river.totalLength = CalculateRiverLength(river);
            
            return river;
        }
        
        /// <summary>
        /// 计算梯度方向
        /// </summary>
        Vector2 CalculateGradientDirection(float[,] heightMap, Vector2Int resolution, Vector2 position)
        {
            int x = Mathf.RoundToInt(position.x);
            int y = Mathf.RoundToInt(position.y);
            
            // 边界检查
            x = Mathf.Clamp(x, 1, resolution.x - 2);
            y = Mathf.Clamp(y, 1, resolution.y - 2);
            
            // 计算梯度
            float gradX = heightMap[x + 1, y] - heightMap[x - 1, y];
            float gradY = heightMap[x, y + 1] - heightMap[x, y - 1];
            
            // 梯度指向下坡方向
            return new Vector2(-gradX, -gradY).normalized;
        }
        
        /// <summary>
        /// 获取曲流偏移
        /// </summary>
        Vector2 GetMeanderingOffset(Vector2 currentDirection, int step)
        {
            // 使用正弦波创建自然的河流弯曲
            float meanderingAngle = Mathf.Sin(step * 0.1f) * Mathf.PI * 0.25f;
            
            // 计算垂直于当前方向的向量
            Vector2 perpendicular = new Vector2(-currentDirection.y, currentDirection.x);
            
            return perpendicular * Mathf.Sin(meanderingAngle);
        }
        
        /// <summary>
        /// 获取指定位置的高度
        /// </summary>
        float GetHeightAtPosition(float[,] heightMap, Vector2Int resolution, Vector2 position)
        {
            int x1 = Mathf.FloorToInt(position.x);
            int y1 = Mathf.FloorToInt(position.y);
            int x2 = Mathf.Min(x1 + 1, resolution.x - 1);
            int y2 = Mathf.Min(y1 + 1, resolution.y - 1);
            
            x1 = Mathf.Max(x1, 0);
            y1 = Mathf.Max(y1, 0);
            
            float fx = position.x - x1;
            float fy = position.y - y1;
            
            // 双线性插值
            float h1 = Mathf.Lerp(heightMap[x1, y1], heightMap[x2, y1], fx);
            float h2 = Mathf.Lerp(heightMap[x1, y2], heightMap[x2, y2], fx);
            
            return Mathf.Lerp(h1, h2, fy);
        }
        
        /// <summary>
        /// 计算河流长度
        /// </summary>
        float CalculateRiverLength(River river)
        {
            float length = 0f;
            
            for (int i = 1; i < river.points.Count; i++)
            {
                length += Vector2.Distance(river.points[i - 1].position, river.points[i].position);
            }
            
            return length;
        }
        
        /// <summary>
        /// 将河流雕刻到地形中
        /// </summary>
        void CarveRiverIntoTerrain(ref float[,] heightMap, Vector2Int resolution, River river)
        {
            foreach (RiverPoint point in river.points)
            {
                CarveRiverPoint(ref heightMap, resolution, point);
            }
        }
        
        /// <summary>
        /// 将河流雕刻到地形中（异步版本）
        /// </summary>
        IEnumerator CarveRiverIntoTerrainProgressive(float[,] heightMap, Vector2Int resolution, River river, int stepsPerFrame)
        {
            int processedPoints = 0;
            
            foreach (RiverPoint point in river.points)
            {
                CarveRiverPoint(ref heightMap, resolution, point);
                
                processedPoints++;
                if (processedPoints >= stepsPerFrame)
                {
                    processedPoints = 0;
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// 雕刻单个河流点
        /// </summary>
        void CarveRiverPoint(ref float[,] heightMap, Vector2Int resolution, RiverPoint point)
        {
            int centerX = Mathf.RoundToInt(point.position.x);
            int centerY = Mathf.RoundToInt(point.position.y);
            int radius = Mathf.RoundToInt(point.width);
            
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x < 0 || x >= resolution.x || y < 0 || y >= resolution.y) continue;
                    
                    float distance = Vector2.Distance(new Vector2(x, y), point.position);
                    
                    if (distance <= point.width)
                    {
                        // 计算河床深度
                        float depthFactor = 1f - (distance / point.width);
                        float riverBedDepth = point.depth * depthFactor * depthFactor;
                        
                        // 应用河流深度
                        float targetHeight = heightMap[centerX, centerY] - riverBedDepth;
                        heightMap[x, y] = Mathf.Min(heightMap[x, y], targetHeight);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取生成的河流列表
        /// </summary>
        public List<River> GetGeneratedRivers()
        {
            return new List<River>(generatedRivers);
        }
        
        /// <summary>
        /// 清除生成的河流
        /// </summary>
        public void ClearRivers()
        {
            generatedRivers.Clear();
        }
    }
}