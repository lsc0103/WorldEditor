using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 放置层 - 定义特定类型对象的放置规则和参数
    /// </summary>
    [System.Serializable]
    public class PlacementLayer
    {
        [Header("基本信息")]
        public string layerName = "New Layer";
        public PlacementLayerType layerType = PlacementLayerType.Vegetation;
        public bool enabled = true;
        public float priority = 1f;
        
        [Header("预制件设置")]
        public GameObject[] prefabs;
        public bool useWeightedSelection = false;
        public float[] prefabWeights;
        
        [Header("密度控制")]
        public float baseDensity = 1f;
        public AnimationCurve densityFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public bool useNoiseDensity = true;
        public float noiseScale = 0.1f;
        public float noiseInfluence = 0.5f;
        
        [Header("放置规则")]
        public PlacementRule[] placementRules;
        public bool requireAllRules = false; // 是否需要满足所有规则
        
        [Header("变换设置")]
        public bool enableRandomRotation = true;
        public bool enableRandomScale = true;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
        public bool alignToSurface = true;
        public float surfaceOffset = 0f;
        
        [Header("分组设置")]
        public Transform parentTransform;
        public bool createLayerParent = true;
        public string layerParentName = "Layer Parent";
        
        [Header("LOD设置")]
        public bool enableLOD = true;
        public float[] lodDistances = { 50f, 150f, 500f };
        public GameObject[] lodPrefabs; // 不同LOD级别的预制件
        
        [Header("生态系统设置")]
        public EcosystemRole ecosystemRole = EcosystemRole.Producer;
        public string[] requiredSpecies; // 需要的共生物种
        public string[] competingSpecies; // 竞争物种
        public float influenceRadius = 5f;
        
        public PlacementLayer()
        {
            prefabs = new GameObject[0];
            prefabWeights = new float[0];
            placementRules = new PlacementRule[0];
            lodDistances = new float[] { 50f, 150f, 500f };
            lodPrefabs = new GameObject[0];
            requiredSpecies = new string[0];
            competingSpecies = new string[0];
        }
    }
    
    /// <summary>
    /// 放置规则 - 定义对象放置的条件
    /// </summary>
    [System.Serializable]
    public class PlacementRule
    {
        [Header("规则信息")]
        public string ruleName = "New Rule";
        public bool enabled = true;
        public float weight = 1f;
        
        [Header("地形条件")]
        public bool checkHeight = false;
        public float minHeight = 0f;
        public float maxHeight = 100f;
        
        public bool checkSlope = false;
        public float minSlope = 0f;
        public float maxSlope = 45f;
        
        [Header("环境条件")]
        public bool checkMoisture = false;
        public float minMoisture = 0f;
        public float maxMoisture = 1f;
        
        public bool checkTemperature = false;
        public float minTemperature = -50f;
        public float maxTemperature = 50f;
        
        [Header("生物群落条件")]
        public bool checkBiome = false;
        public BiomeType[] allowedBiomes;
        
        [Header("距离条件")]
        public bool checkDistanceToWater = false;
        public float minDistanceToWater = 0f;
        public float maxDistanceToWater = 100f;
        
        public bool checkDistanceToOtherObjects = false;
        public float minDistanceToOthers = 1f;
        public string[] avoidObjectTags;
        
        [Header("自定义条件")]
        public bool useCustomRule = false;
        public string customRuleScript; // 自定义规则脚本名称
        
        public PlacementRule()
        {
            allowedBiomes = new BiomeType[] { BiomeType.Temperate };
            avoidObjectTags = new string[0];
        }
    }
    
    /// <summary>
    /// 放置网格 - 管理和优化对象放置
    /// </summary>
    public class PlacementGrid
    {
        private Dictionary<Vector2Int, List<PlacedObject>> grid;
        private float cellSize;
        private Bounds bounds;
        private Vector2Int gridSize;
        
        public PlacementGrid()
        {
            grid = new Dictionary<Vector2Int, List<PlacedObject>>();
        }
        
        public void Initialize(Bounds worldBounds, float gridCellSize)
        {
            bounds = worldBounds;
            cellSize = gridCellSize;
            
            gridSize = new Vector2Int(
                Mathf.CeilToInt(bounds.size.x / cellSize),
                Mathf.CeilToInt(bounds.size.z / cellSize)
            );
            
            grid.Clear();
        }
        
        public void RegisterObject(GameObject obj, Vector3 position)
        {
            Vector2Int cellCoord = WorldToCellCoordinate(position);
            
            if (!grid.ContainsKey(cellCoord))
            {
                grid[cellCoord] = new List<PlacedObject>();
            }
            
            PlacedObject placedObj = new PlacedObject
            {
                gameObject = obj,
                position = position,
                cellCoordinate = cellCoord
            };
            
            grid[cellCoord].Add(placedObj);
        }
        
        public List<PlacedObject> GetObjectsInRadius(Vector3 center, float radius)
        {
            List<PlacedObject> result = new List<PlacedObject>();
            
            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            Vector2Int centerCell = WorldToCellCoordinate(center);
            
            for (int x = centerCell.x - cellRadius; x <= centerCell.x + cellRadius; x++)
            {
                for (int y = centerCell.y - cellRadius; y <= centerCell.y + cellRadius; y++)
                {
                    Vector2Int cellCoord = new Vector2Int(x, y);
                    
                    if (grid.ContainsKey(cellCoord))
                    {
                        foreach (var obj in grid[cellCoord])
                        {
                            if (Vector3.Distance(center, obj.position) <= radius)
                            {
                                result.Add(obj);
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        public bool IsPositionOccupied(Vector3 position, float checkRadius, string[] avoidTags = null)
        {
            var objectsInRadius = GetObjectsInRadius(position, checkRadius);
            
            foreach (var obj in objectsInRadius)
            {
                if (obj.gameObject == null) continue;
                
                if (avoidTags != null && avoidTags.Length > 0)
                {
                    bool hasAvoidTag = false;
                    foreach (string tag in avoidTags)
                    {
                        if (obj.gameObject.CompareTag(tag))
                        {
                            hasAvoidTag = true;
                            break;
                        }
                    }
                    
                    if (hasAvoidTag)
                        return true;
                }
                else
                {
                    return true; // 有对象存在
                }
            }
            
            return false;
        }
        
        public void ClearAll()
        {
            foreach (var cell in grid.Values)
            {
                foreach (var obj in cell)
                {
                    if (obj.gameObject != null)
                    {
                        Object.DestroyImmediate(obj.gameObject);
                    }
                }
            }
            
            grid.Clear();
        }
        
        public void ClearCell(Vector2Int cellCoord)
        {
            if (grid.ContainsKey(cellCoord))
            {
                foreach (var obj in grid[cellCoord])
                {
                    if (obj.gameObject != null)
                    {
                        Object.DestroyImmediate(obj.gameObject);
                    }
                }
                
                grid[cellCoord].Clear();
            }
        }
        
        Vector2Int WorldToCellCoordinate(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - bounds.min.x) / cellSize);
            int z = Mathf.FloorToInt((worldPosition.z - bounds.min.z) / cellSize);
            
            return new Vector2Int(x, z);
        }
        
        public int GetTotalObjectCount()
        {
            int count = 0;
            foreach (var cell in grid.Values)
            {
                count += cell.Count;
            }
            return count;
        }
        
        public Dictionary<Vector2Int, List<PlacedObject>> GetGrid()
        {
            return grid;
        }
    }
    
    /// <summary>
    /// 放置的对象信息
    /// </summary>
    [System.Serializable]
    public class PlacedObject
    {
        public GameObject gameObject;
        public Vector3 position;
        public Vector2Int cellCoordinate;
        public PlacementLayerType layerType;
        public string speciesName;
        public float influenceRadius;
        public EcosystemRole ecosystemRole;
        public float health = 1f;
        public float age = 0f;
        public Dictionary<string, object> customData;
        
        public PlacedObject()
        {
            customData = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// 地形数据 - 用于放置分析
    /// </summary>
    public class TerrainData
    {
        public float[,] heightMap;
        public float[,] slopeMap;
        public float[,] moistureMap;
        public float[,] temperatureMap;
        public float[,] exposureMap;
        public Vector2Int resolution;
        public Bounds bounds;
        
        public float GetHeightAtPosition(Vector3 worldPosition)
        {
            Vector2 localPos = WorldToLocal(worldPosition);
            return BilinearInterpolate(heightMap, localPos);
        }
        
        public float GetSlopeAtPosition(Vector3 worldPosition)
        {
            Vector2 localPos = WorldToLocal(worldPosition);
            return BilinearInterpolate(slopeMap, localPos);
        }
        
        public float GetMoistureAtPosition(Vector3 worldPosition)
        {
            Vector2 localPos = WorldToLocal(worldPosition);
            return BilinearInterpolate(moistureMap, localPos);
        }
        
        public float GetTemperatureAtPosition(Vector3 worldPosition)
        {
            Vector2 localPos = WorldToLocal(worldPosition);
            return BilinearInterpolate(temperatureMap, localPos);
        }
        
        Vector2 WorldToLocal(Vector3 worldPosition)
        {
            float x = (worldPosition.x - bounds.min.x) / bounds.size.x;
            float z = (worldPosition.z - bounds.min.z) / bounds.size.z;
            
            x = Mathf.Clamp01(x) * (resolution.x - 1);
            z = Mathf.Clamp01(z) * (resolution.y - 1);
            
            return new Vector2(x, z);
        }
        
        float BilinearInterpolate(float[,] map, Vector2 position)
        {
            int x1 = Mathf.FloorToInt(position.x);
            int y1 = Mathf.FloorToInt(position.y);
            int x2 = Mathf.Min(x1 + 1, resolution.x - 1);
            int y2 = Mathf.Min(y1 + 1, resolution.y - 1);
            
            float fx = position.x - x1;
            float fy = position.y - y1;
            
            float v1 = Mathf.Lerp(map[x1, y1], map[x2, y1], fx);
            float v2 = Mathf.Lerp(map[x1, y2], map[x2, y2], fx);
            
            return Mathf.Lerp(v1, v2, fy);
        }
    }
    
    /// <summary>
    /// 生物群落数据
    /// </summary>
    public class BiomeData
    {
        public BiomeType[,] biomeMap;
        public float[,] biodiversityMap;
        public Vector2Int resolution;
        public Bounds bounds;
        
        public BiomeType GetBiomeAtPosition(Vector3 worldPosition)
        {
            Vector2Int localPos = WorldToLocalInt(worldPosition);
            return biomeMap[localPos.x, localPos.y];
        }
        
        public float GetBiodiversityAtPosition(Vector3 worldPosition)
        {
            Vector2 localPos = WorldToLocal(worldPosition);
            return BilinearInterpolate(biodiversityMap, localPos);
        }
        
        Vector2Int WorldToLocalInt(Vector3 worldPosition)
        {
            float x = (worldPosition.x - bounds.min.x) / bounds.size.x;
            float z = (worldPosition.z - bounds.min.z) / bounds.size.z;
            
            x = Mathf.Clamp01(x) * (resolution.x - 1);
            z = Mathf.Clamp01(z) * (resolution.y - 1);
            
            return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(z));
        }
        
        Vector2 WorldToLocal(Vector3 worldPosition)
        {
            float x = (worldPosition.x - bounds.min.x) / bounds.size.x;
            float z = (worldPosition.z - bounds.min.z) / bounds.size.z;
            
            x = Mathf.Clamp01(x) * (resolution.x - 1);
            z = Mathf.Clamp01(z) * (resolution.y - 1);
            
            return new Vector2(x, z);
        }
        
        float BilinearInterpolate(float[,] map, Vector2 position)
        {
            int x1 = Mathf.FloorToInt(position.x);
            int y1 = Mathf.FloorToInt(position.y);
            int x2 = Mathf.Min(x1 + 1, resolution.x - 1);
            int y2 = Mathf.Min(y1 + 1, resolution.y - 1);
            
            float fx = position.x - x1;
            float fy = position.y - y1;
            
            float v1 = Mathf.Lerp(map[x1, y1], map[x2, y1], fx);
            float v2 = Mathf.Lerp(map[x1, y2], map[x2, y2], fx);
            
            return Mathf.Lerp(v1, v2, fy);
        }
    }
    
    // 枚举定义
    public enum PlacementLayerType
    {
        Vegetation,
        Structure,
        Decoration,
        Particle,
        Audio,
        Lighting
    }
    
    public enum EcosystemRole
    {
        Producer,    // 生产者（植物）
        Consumer,    // 消费者（动物）
        Decomposer,  // 分解者
        Neutral      // 中性（非生物）
    }
    
    public enum PlacementStrategy
    {
        Random,
        Grid,
        Poisson,
        Cluster,
        Path,
        Custom
    }
}