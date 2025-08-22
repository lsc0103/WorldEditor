using UnityEngine;
using System.Collections;
using WorldEditor.Core;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 侵蚀模拟器 - 模拟真实的地形侵蚀过程
    /// 包括水力侵蚀、热侵蚀、沉积等自然过程
    /// </summary>
    public class ErosionSimulator : MonoBehaviour
    {
        [Header("侵蚀设置")]
        [SerializeField] private int maxDroplets = 10000;
        [SerializeField] private int maxLifetime = 30;
        [SerializeField] private float inertia = 0.05f;
        [SerializeField] private float sedimentCapacityFactor = 4f;
        [SerializeField] private float minSedimentCapacity = 0.01f;
        [SerializeField] private float erodeSpeed = 0.3f;
        [SerializeField] private float depositSpeed = 0.3f;
        [SerializeField] private float evaporateSpeed = 0.01f;
        [SerializeField] private float gravity = 4f;
        [SerializeField] private int erosionRadius = 3;
        
        [Header("热侵蚀设置")]
        [SerializeField] private bool enableThermalErosion = true;
        [SerializeField] private float thermalErosionRate = 0.1f;
        [SerializeField] private float thermalErosionThreshold = 0.2f;
        
        // 私有变量
        private int mapSize;
        private float[,] erosionBrushIndices;
        private float[,] erosionBrushWeights;
        
        void Awake()
        {
            InitializeErosionBrushes();
        }
        
        void InitializeErosionBrushes()
        {
            Debug.Log($"[ErosionSimulator] 初始化侵蚀笔刷，半径: {erosionRadius}");
            
            // 确保erosionRadius至少为1
            if (erosionRadius <= 0)
            {
                Debug.LogWarning("[ErosionSimulator] 侵蚀半径为0或负数，设置为默认值1");
                erosionRadius = 1;
            }
            
            int brushLength = erosionRadius * 2 + 1;
            erosionBrushIndices = new float[brushLength * brushLength, 2];
            erosionBrushWeights = new float[brushLength * brushLength, 1];
            
            float weightSum = 0;
            int brushIndex = 0;
            
            for (int y = -erosionRadius; y <= erosionRadius; y++)
            {
                for (int x = -erosionRadius; x <= erosionRadius; x++)
                {
                    float sqrDst = x * x + y * y;
                    if (sqrDst <= erosionRadius * erosionRadius) // 改为<=以包含边界
                    {
                        erosionBrushIndices[brushIndex, 0] = x;
                        erosionBrushIndices[brushIndex, 1] = y;
                        
                        float brushWeight = erosionRadius == 0 ? 1f : (1 - Mathf.Sqrt(sqrDst) / erosionRadius);
                        weightSum += brushWeight;
                        erosionBrushWeights[brushIndex, 0] = brushWeight;
                        brushIndex++;
                    }
                }
            }
            
            Debug.Log($"[ErosionSimulator] 创建了{brushIndex}个笔刷点");
            
            // 标准化权重
            for (int i = 0; i < brushIndex; i++)
            {
                erosionBrushWeights[i, 0] /= weightSum;
            }
        }
        
        /// <summary>
        /// 应用侵蚀（同步版本）
        /// </summary>
        public void ApplyErosion(ref float[,] heightMap, Vector2Int mapResolution, GeologySettings geology)
        {
            Debug.Log("[ErosionSimulator] 开始应用侵蚀");
            Debug.Log($"[ErosionSimulator] 地图分辨率: {mapResolution.x}x{mapResolution.y}");
            Debug.Log($"[ErosionSimulator] 侵蚀半径: {erosionRadius}");
            
            // 确保侵蚀笔刷已初始化
            if (erosionBrushIndices == null || erosionBrushWeights == null)
            {
                Debug.Log("[ErosionSimulator] 初始化侵蚀笔刷");
                InitializeErosionBrushes();
            }
            
            mapSize = mapResolution.x;
            
            // 执行水力侵蚀
            PerformHydraulicErosion(ref heightMap, mapResolution, geology);
            
            // 执行热侵蚀
            if (enableThermalErosion)
            {
                PerformThermalErosion(ref heightMap, mapResolution, geology);
            }
            
            Debug.Log("[ErosionSimulator] 侵蚀应用完成");
        }
        
        /// <summary>
        /// 应用侵蚀（异步版本）
        /// </summary>
        public IEnumerator ApplyErosionProgressive(float[,] heightMap, Vector2Int mapResolution, GeologySettings geology, int stepsPerFrame)
        {
            mapSize = mapResolution.x;
            
            // 执行水力侵蚀
            yield return StartCoroutine(PerformHydraulicErosionProgressive(heightMap, mapResolution, geology, stepsPerFrame));
            
            // 执行热侵蚀
            if (enableThermalErosion)
            {
                yield return StartCoroutine(PerformThermalErosionProgressive(heightMap, mapResolution, geology, stepsPerFrame));
            }
        }
        
        /// <summary>
        /// 执行水力侵蚀
        /// </summary>
        void PerformHydraulicErosion(ref float[,] heightMap, Vector2Int mapResolution, GeologySettings geology)
        {
            System.Random random = new System.Random();
            
            for (int iteration = 0; iteration < maxDroplets; iteration++)
            {
                // 随机选择水滴起始位置
                float posX = random.Next(0, mapResolution.x);
                float posY = random.Next(0, mapResolution.y);
                
                SimulateWaterDroplet(ref heightMap, mapResolution, posX, posY, geology, random);
            }
        }
        
        /// <summary>
        /// 执行水力侵蚀（异步版本）
        /// </summary>
        IEnumerator PerformHydraulicErosionProgressive(float[,] heightMap, Vector2Int mapResolution, GeologySettings geology, int stepsPerFrame)
        {
            System.Random random = new System.Random();
            int processedDroplets = 0;
            
            for (int iteration = 0; iteration < maxDroplets; iteration++)
            {
                float posX = random.Next(0, mapResolution.x);
                float posY = random.Next(0, mapResolution.y);
                
                SimulateWaterDroplet(ref heightMap, mapResolution, posX, posY, geology, random);
                
                processedDroplets++;
                if (processedDroplets >= stepsPerFrame)
                {
                    processedDroplets = 0;
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// 模拟单个水滴的侵蚀过程
        /// </summary>
        void SimulateWaterDroplet(ref float[,] heightMap, Vector2Int mapResolution, float posX, float posY, GeologySettings geology, System.Random random)
        {
            float dirX = 0, dirY = 0;
            float speed = 1;
            float water = 1;
            float sediment = 0;
            
            for (int lifetime = 0; lifetime < maxLifetime; lifetime++)
            {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                
                // 边界检查
                if (nodeX < 0 || nodeX >= mapResolution.x - 1 || nodeY < 0 || nodeY >= mapResolution.y - 1)
                    break;
                
                // 计算水滴的高度和梯度
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;
                
                float height = CalculateHeightAndGradient(heightMap, mapResolution, posX, posY, out float gradX, out float gradY);
                
                // 更新水滴方向和速度
                dirX = (dirX * inertia - gradX * (1 - inertia));
                dirY = (dirY * inertia - gradY * (1 - inertia));
                
                // 标准化方向
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len != 0)
                {
                    dirX /= len;
                    dirY /= len;
                }
                
                // 移动水滴
                posX += dirX;
                posY += dirY;
                
                // 计算新高度
                float newHeight = CalculateHeightAndGradient(heightMap, mapResolution, posX, posY, out float newGradX, out float newGradY);
                float deltaHeight = newHeight - height;
                
                // 计算沉积物容量
                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);
                
                // 如果超过容量或向上移动，沉积沉积物
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;
                    
                    // 在当前位置沉积
                    DepositSediment(ref heightMap, mapResolution, posX, posY, amountToDeposit);
                }
                else
                {
                    // 侵蚀
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);
                    
                    // 根据地质硬度调整侵蚀量
                    amountToErode *= (1f - geology.rockHardness);
                    
                    ErodeTerrain(ref heightMap, mapResolution, posX, posY, amountToErode);
                    sediment += amountToErode;
                }
                
                // 更新速度和蒸发水分
                speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
                water *= (1 - evaporateSpeed);
                
                // 如果水分太少，停止模拟
                if (water < 0.01f)
                    break;
            }
        }
        
        /// <summary>
        /// 计算高度和梯度
        /// </summary>
        float CalculateHeightAndGradient(float[,] heightMap, Vector2Int mapResolution, float posX, float posY, out float gradX, out float gradY)
        {
            int coordX = (int)posX;
            int coordY = (int)posY;
            
            // 边界处理
            coordX = Mathf.Clamp(coordX, 0, mapResolution.x - 2);
            coordY = Mathf.Clamp(coordY, 0, mapResolution.y - 2);
            
            float u = posX - coordX;
            float v = posY - coordY;
            
            // 双线性插值计算高度
            float heightNW = heightMap[coordX, coordY];
            float heightNE = heightMap[coordX + 1, coordY];
            float heightSW = heightMap[coordX, coordY + 1];
            float heightSE = heightMap[coordX + 1, coordY + 1];
            
            float height = heightNW * (1 - u) * (1 - v) + heightNE * u * (1 - v) + heightSW * (1 - u) * v + heightSE * u * v;
            
            // 计算梯度
            gradX = (heightNE - heightNW) * (1 - v) + (heightSE - heightSW) * v;
            gradY = (heightSW - heightNW) * (1 - u) + (heightSE - heightNE) * u;
            
            return height;
        }
        
        /// <summary>
        /// 沉积沉积物
        /// </summary>
        void DepositSediment(ref float[,] heightMap, Vector2Int mapResolution, float posX, float posY, float amount)
        {
            int coordX = (int)posX;
            int coordY = (int)posY;
            
            // 边界检查
            if (coordX < 0 || coordX >= mapResolution.x - 1 || coordY < 0 || coordY >= mapResolution.y - 1)
                return;
            
            float u = posX - coordX;
            float v = posY - coordY;
            
            // 分布沉积物到周围的四个点
            heightMap[coordX, coordY] += amount * (1 - u) * (1 - v);
            heightMap[coordX + 1, coordY] += amount * u * (1 - v);
            heightMap[coordX, coordY + 1] += amount * (1 - u) * v;
            heightMap[coordX + 1, coordY + 1] += amount * u * v;
        }
        
        /// <summary>
        /// 侵蚀地形
        /// </summary>
        void ErodeTerrain(ref float[,] heightMap, Vector2Int mapResolution, float posX, float posY, float amount)
        {
            int coordX = (int)posX;
            int coordY = (int)posY;
            
            // 使用侵蚀笔刷
            for (int i = 0; i < erosionBrushIndices.GetLength(0); i++)
            {
                if (erosionBrushWeights[i, 0] <= 0) break;
                
                int nodeX = coordX + (int)erosionBrushIndices[i, 0];
                int nodeY = coordY + (int)erosionBrushIndices[i, 1];
                
                // 边界检查
                if (nodeX < 0 || nodeX >= mapResolution.x || nodeY < 0 || nodeY >= mapResolution.y)
                    continue;
                
                float erosionAmount = amount * erosionBrushWeights[i, 0];
                heightMap[nodeX, nodeY] -= erosionAmount;
                
                // 确保高度不为负数
                if (heightMap[nodeX, nodeY] < 0)
                    heightMap[nodeX, nodeY] = 0;
            }
        }
        
        /// <summary>
        /// 执行热侵蚀
        /// </summary>
        void PerformThermalErosion(ref float[,] heightMap, Vector2Int mapResolution, GeologySettings geology)
        {
            float[,] newHeightMap = new float[mapResolution.x, mapResolution.y];
            
            for (int x = 0; x < mapResolution.x; x++)
            {
                for (int y = 0; y < mapResolution.y; y++)
                {
                    float erosionAmount = CalculateThermalErosion(heightMap, mapResolution, x, y, geology);
                    newHeightMap[x, y] = heightMap[x, y] - erosionAmount;
                }
            }
            
            // 应用新的高度图
            for (int x = 0; x < mapResolution.x; x++)
            {
                for (int y = 0; y < mapResolution.y; y++)
                {
                    heightMap[x, y] = newHeightMap[x, y];
                }
            }
        }
        
        /// <summary>
        /// 执行热侵蚀（异步版本）
        /// </summary>
        IEnumerator PerformThermalErosionProgressive(float[,] heightMap, Vector2Int mapResolution, GeologySettings geology, int stepsPerFrame)
        {
            float[,] newHeightMap = new float[mapResolution.x, mapResolution.y];
            int processedCells = 0;
            
            for (int x = 0; x < mapResolution.x; x++)
            {
                for (int y = 0; y < mapResolution.y; y++)
                {
                    float erosionAmount = CalculateThermalErosion(heightMap, mapResolution, x, y, geology);
                    newHeightMap[x, y] = heightMap[x, y] - erosionAmount;
                    
                    processedCells++;
                    if (processedCells >= stepsPerFrame)
                    {
                        processedCells = 0;
                        yield return null;
                    }
                }
            }
            
            // 应用新的高度图
            for (int x = 0; x < mapResolution.x; x++)
            {
                for (int y = 0; y < mapResolution.y; y++)
                {
                    heightMap[x, y] = newHeightMap[x, y];
                }
            }
        }
        
        /// <summary>
        /// 计算热侵蚀量
        /// </summary>
        float CalculateThermalErosion(float[,] heightMap, Vector2Int mapResolution, int x, int y, GeologySettings geology)
        {
            float currentHeight = heightMap[x, y];
            float maxDifference = 0f;
            
            // 检查8个邻居
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int neighborX = x + dx;
                    int neighborY = y + dy;
                    
                    // 边界检查
                    if (neighborX < 0 || neighborX >= mapResolution.x || neighborY < 0 || neighborY >= mapResolution.y)
                        continue;
                    
                    float neighborHeight = heightMap[neighborX, neighborY];
                    float difference = currentHeight - neighborHeight;
                    
                    if (difference > maxDifference)
                        maxDifference = difference;
                }
            }
            
            // 如果坡度超过阈值，应用热侵蚀
            if (maxDifference > thermalErosionThreshold)
            {
                float erosionAmount = (maxDifference - thermalErosionThreshold) * thermalErosionRate;
                
                // 根据岩石硬度调整侵蚀量
                erosionAmount *= (1f - geology.rockHardness);
                
                return erosionAmount;
            }
            
            return 0f;
        }
    }
}