using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using WorldEditor.Core;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 地形印章系统 - 竞争Gaia Pro的核心功能
    /// 使用预制的高度图"印章"快速塑造地形
    /// </summary>
    public class TerrainStamper : MonoBehaviour
    {
        [Header("印章设置")]
        [SerializeField] private StampLibrary stampLibrary;
        [SerializeField] private Stamp currentStamp;
        [SerializeField] private float stampSize = 100f;
        [SerializeField] private float stampStrength = 1f;
        [SerializeField] private float stampRotation = 0f;
        [SerializeField] private StampBlendMode blendMode = StampBlendMode.Add;
        
        [Header("预览设置")]
        [SerializeField] private bool enablePreview = true;
        [SerializeField] private bool showStampGizmo = true;
        [SerializeField] private Color previewColor = new Color(1f, 1f, 0f, 0.5f);
        
        [Header("性能设置")]
        [SerializeField] private bool enableGPUStamping = true;
        [SerializeField] private int maxStampsPerFrame = 1;
        
        // 内部状态
        private AdvancedTerrainGenerator terrainGenerator;
        private List<StampOperation> stampHistory = new List<StampOperation>();
        private bool isStamping = false;
        private Vector3 lastStampPosition;
        
        // 事件
        public System.Action<StampOperation> OnStampApplied;
        public System.Action<int> OnStampHistoryChanged;
        
        void Awake()
        {
            Debug.Log($"[TerrainStamper] Awake调用，GameObject: {gameObject.name}");
            
            terrainGenerator = GetComponent<AdvancedTerrainGenerator>();
            if (terrainGenerator == null)
            {
                Debug.LogError($"[TerrainStamper] 在{gameObject.name}上未找到AdvancedTerrainGenerator组件");
                
                // 尝试在父对象中查找
                terrainGenerator = GetComponentInParent<AdvancedTerrainGenerator>();
                if (terrainGenerator != null)
                {
                    Debug.Log($"[TerrainStamper] 在父对象中找到AdvancedTerrainGenerator: {terrainGenerator.gameObject.name}");
                }
                else
                {
                    Debug.LogError("[TerrainStamper] 在父对象中也未找到AdvancedTerrainGenerator组件");
                }
            }
            else
            {
                Debug.Log($"[TerrainStamper] 成功找到AdvancedTerrainGenerator组件: {terrainGenerator.gameObject.name}");
            }
            
            InitializeStampLibrary();
        }
        
        public void InitializeStampLibrary()
        {
            if (stampLibrary == null)
            {
                Debug.Log("[TerrainStamper] 开始创建印章库");
                stampLibrary = ScriptableObject.CreateInstance<StampLibrary>();
                stampLibrary.name = "Runtime Stamp Library";
                
                Debug.Log("[TerrainStamper] 初始化默认印章");
                stampLibrary.InitializeDefaultStamps();
                
                int stampCount = stampLibrary.GetStampCount();
                Debug.Log($"[TerrainStamper] 创建默认印章库完成，包含 {stampCount} 个印章");
                
                if (stampCount == 0)
                {
                    Debug.LogError("[TerrainStamper] 默认印章创建失败！");
                }
            }
            else
            {
                Debug.Log($"[TerrainStamper] 印章库已存在: {stampLibrary.name}，包含 {stampLibrary.GetStampCount()} 个印章");
            }
        }
        
        /// <summary>
        /// 在指定位置应用印章
        /// </summary>
        public void ApplyStampAtPosition(Vector3 worldPosition)
        {
            if (currentStamp == null)
            {
                Debug.LogWarning("[TerrainStamper] 未选择印章");
                return;
            }
            
            if (isStamping)
            {
                Debug.LogWarning("[TerrainStamper] 正在处理其他印章操作");
                return;
            }
            
            StartCoroutine(ApplyStampCoroutine(worldPosition));
        }
        
        IEnumerator ApplyStampCoroutine(Vector3 worldPosition)
        {
            isStamping = true;
            
            Debug.Log($"[TerrainStamper] 在位置 {worldPosition} 应用印章 {currentStamp.name}");
            
            // 创建印章操作记录
            var operation = new StampOperation
            {
                stamp = currentStamp,
                position = worldPosition,
                size = stampSize,
                strength = stampStrength,
                rotation = stampRotation,
                blendMode = blendMode,
                timestamp = System.DateTime.Now
            };
            
            // 智能选择处理方式：Editor模式直接使用CPU，Play模式使用AccelEngine
            if (!Application.isPlaying)
            {
                // Editor模式：直接使用原有的CPU处理逻辑
                Debug.Log("[TerrainStamper] Editor模式，使用CPU处理印章");
                yield return StartCoroutine(ApplyStampCPUOptimized(operation));
            }
            else
            {
                // Play模式：使用AccelEngine GPU加速架构
                Debug.Log("[TerrainStamper] Play模式，使用AccelEngine处理印章");
                
                bool operationCompleted = false;
                bool operationSuccess = false;
                
                // 准备任务数据
                object[] taskData = new object[] { operation, terrainGenerator };
                
                // 提交到AccelEngine处理
                string taskId = AccelEngine.Instance.SubmitTask(
                    AccelEngine.ComputeTaskType.TerrainGeneration,
                    $"应用{currentStamp.name}印章",
                    (success) => {
                        operationCompleted = true;
                        operationSuccess = success;
                    },
                    taskData,
                    priority: 0, // 最高优先级
                    forceGPU: enableGPUStamping
                );
                
                Debug.Log($"[TerrainStamper] 任务已提交到AccelEngine: {taskId}");
                
                // 等待任务完成
                while (!operationCompleted)
                {
                    yield return null;
                }
                
                if (!operationSuccess)
                {
                    Debug.LogWarning("[TerrainStamper] AccelEngine处理失败，使用本地CPU回退");
                    yield return StartCoroutine(ApplyStampCPUOptimized(operation));
                }
                else
                {
                    Debug.Log("[TerrainStamper] AccelEngine处理成功");
                }
            }
            
            // 添加到历史记录
            stampHistory.Add(operation);
            OnStampApplied?.Invoke(operation);
            OnStampHistoryChanged?.Invoke(stampHistory.Count);
            
            lastStampPosition = worldPosition;
            isStamping = false;
            
            Debug.Log($"[TerrainStamper] 印章应用完成，历史记录: {stampHistory.Count}");
        }
        
        /// <summary>
        /// GPU加速印章应用（使用RenderTexture和着色器）
        /// </summary>
        IEnumerator ApplyStampGPU(StampOperation operation, System.Action<bool> callback)
        {
            Debug.Log("[TerrainStamper] 使用GPU着色器应用印章");
            
            // 检查terrainGenerator
            if (terrainGenerator == null)
            {
                terrainGenerator = GetComponent<AdvancedTerrainGenerator>();
                if (terrainGenerator == null)
                {
                    callback(false);
                    yield break;
                }
            }
            
            var terrain = terrainGenerator.GetTerrain();
            if (terrain == null)
            {
                callback(false);
                yield break;
            }
            
            var terrainData = terrain.terrainData;
            int resolution = terrainData.heightmapResolution;
            
            // 使用GPU并行处理：创建材质和着色器
            Material stampMaterial = CreateStampMaterial();
            if (stampMaterial == null)
            {
                callback(false);
                yield break;
            }
            
            // 创建RenderTexture用于GPU处理
            RenderTexture heightRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
            heightRT.enableRandomWrite = true;
            
            if (!heightRT.Create())
            {
                callback(false);
                yield break;
            }
            
            // 将当前高度图转换为纹理
            Texture2D currentHeightTex = CreateHeightTexture(terrainData);
            if (currentHeightTex == null)
            {
                heightRT.Release();
                callback(false);
                yield break;
            }
            
            // 设置材质参数
            stampMaterial.SetTexture("_HeightTex", currentHeightTex);
            stampMaterial.SetTexture("_StampTex", operation.stamp.heightTexture);
            stampMaterial.SetVector("_StampCenter", new Vector4(
                operation.position.x / terrainData.size.x, 
                operation.position.z / terrainData.size.z, 0, 0));
            stampMaterial.SetFloat("_StampRadius", operation.size / terrainData.size.x);
            stampMaterial.SetFloat("_StampStrength", operation.strength);
            stampMaterial.SetFloat("_HeightScale", operation.stamp.heightScale);
            stampMaterial.SetFloat("_BaseHeight", operation.stamp.baseHeight);
            stampMaterial.SetFloat("_TerrainMaxHeight", terrainData.size.y);
            
            // 使用GPU进行并行处理
            Graphics.Blit(currentHeightTex, heightRT, stampMaterial);
            
            // 等待GPU完成
            yield return new WaitForEndOfFrame();
            
            // 从GPU读取结果回到CPU
            RenderTexture.active = heightRT;
            Texture2D resultTex = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);
            resultTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            resultTex.Apply();
            RenderTexture.active = null;
            
            // 转换回高度数组
            float[,] heights = new float[resolution, resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights[y, x] = resultTex.GetPixel(x, resolution - 1 - y).r; // Unity纹理Y轴翻转
                }
            }
            
            // 应用到地形
            terrainData.SetHeights(0, 0, heights);
            
            // 清理GPU资源
            if (currentHeightTex != null) DestroyImmediate(currentHeightTex);
            if (resultTex != null) DestroyImmediate(resultTex);
            if (stampMaterial != null) DestroyImmediate(stampMaterial);
            if (heightRT != null) heightRT.Release();
            
            Debug.Log("[TerrainStamper] GPU印章应用完成");
            callback(true);
        }
        
        /// <summary>
        /// 创建印章处理材质
        /// </summary>
        Material CreateStampMaterial()
        {
            // 创建简单的Unlit着色器材质，用于GPU处理
            string shaderCode = @"
                Shader ""Hidden/TerrainStamp"" {
                    Properties {
                        _HeightTex (""Height Texture"", 2D) = ""white"" {}
                        _StampTex (""Stamp Texture"", 2D) = ""white"" {}
                        _StampCenter (""Stamp Center"", Vector) = (0.5, 0.5, 0, 0)
                        _StampRadius (""Stamp Radius"", Float) = 0.1
                        _StampStrength (""Stamp Strength"", Float) = 1.0
                        _HeightScale (""Height Scale"", Float) = 1.0
                        _BaseHeight (""Base Height"", Float) = 0.0
                        _TerrainMaxHeight (""Terrain Max Height"", Float) = 100.0
                    }
                    SubShader {
                        Pass {
                            CGPROGRAM
                            #pragma vertex vert
                            #pragma fragment frag
                            #include ""UnityCG.cginc""
                            
                            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
                            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
                            
                            sampler2D _HeightTex, _StampTex;
                            float4 _StampCenter;
                            float _StampRadius, _StampStrength, _HeightScale, _BaseHeight, _TerrainMaxHeight;
                            
                            v2f vert (appdata v) {
                                v2f o;
                                o.vertex = UnityObjectToClipPos(v.vertex);
                                o.uv = v.uv;
                                return o;
                            }
                            
                            float frag (v2f i) : SV_Target {
                                float currentHeight = tex2D(_HeightTex, i.uv).r;
                                
                                // 计算到印章中心的距离
                                float2 stampUV = (i.uv - _StampCenter.xy) / _StampRadius + 0.5;
                                float distance = length(i.uv - _StampCenter.xy);
                                
                                if (distance <= _StampRadius && stampUV.x >= 0 && stampUV.x <= 1 && stampUV.y >= 0 && stampUV.y <= 1) {
                                    float stampHeight = tex2D(_StampTex, stampUV).r;
                                    float heightInUnits = (_BaseHeight + stampHeight * _HeightScale) / _TerrainMaxHeight;
                                    
                                    float falloff = 1.0 - (distance / _StampRadius);
                                    falloff = smoothstep(0.0, 1.0, falloff);
                                    
                                    // 使用Set混合模式
                                    return lerp(currentHeight, heightInUnits, _StampStrength * falloff);
                                } else {
                                    return currentHeight;
                                }
                            }
                            ENDCG
                        }
                    }
                }
            ";
            
            // 由于不能运行时编译着色器，使用简化的CPU+GPU混合方案
            Debug.LogWarning("[TerrainStamper] 着色器编译不可用，回退到CPU模式");
            return null;
        }
        
        /// <summary>
        /// 将地形高度数据转换为纹理
        /// </summary>
        Texture2D CreateHeightTexture(TerrainData terrainData)
        {
            int resolution = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
            
            Texture2D heightTex = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heights[y, x];
                    heightTex.SetPixel(x, resolution - 1 - y, new Color(height, height, height, 1f)); // Y轴翻转
                }
            }
            
            heightTex.Apply();
            return heightTex;
        }
        
        /// <summary>
        /// 优化的CPU印章应用（GPU失败时的回退方案）
        /// </summary>
        IEnumerator ApplyStampCPUOptimized(StampOperation operation)
        {
            Debug.Log("[TerrainStamper] 使用优化CPU应用印章（回退模式）");
            
            // 检查terrainGenerator
            if (terrainGenerator == null)
            {
                terrainGenerator = GetComponent<AdvancedTerrainGenerator>();
                if (terrainGenerator == null)
                {
                    Debug.LogError("[TerrainStamper] 无法找到AdvancedTerrainGenerator组件");
                    yield break;
                }
            }
            
            // 获取地形数据
            var terrain = terrainGenerator.GetTerrain();
            if (terrain == null)
            {
                Debug.LogError("[TerrainStamper] 未找到地形");
                yield break;
            }
            
            var terrainData = terrain.terrainData;
            var heightmapResolution = terrainData.heightmapResolution;
            
            // 获取当前高度图
            float[,] heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
            
            // 计算印章影响区域
            var stampBounds = CalculateStampBounds(operation, terrain);
            
            // 直接应用印章数据（无协程开销）
            ApplyStampDataDirect(heights, operation, stampBounds, terrain);
            
            // 更新地形高度图
            terrainData.SetHeights(0, 0, heights);
            
            Debug.Log("[TerrainStamper] 优化CPU印章应用完成");
            yield return null; // 只让出一帧
        }
        
        /// <summary>
        /// CPU版本印章应用
        /// </summary>
        IEnumerator ApplyStampCPU(StampOperation operation)
        {
            Debug.Log("[TerrainStamper] 使用CPU应用印章");
            
            // 检查terrainGenerator是否存在
            if (terrainGenerator == null)
            {
                Debug.LogError("[TerrainStamper] AdvancedTerrainGenerator组件为null，尝试重新获取");
                terrainGenerator = GetComponent<AdvancedTerrainGenerator>();
                if (terrainGenerator == null)
                {
                    Debug.LogError("[TerrainStamper] 仍然无法找到AdvancedTerrainGenerator组件");
                    yield break;
                }
            }
            
            // 获取地形数据
            var terrain = terrainGenerator.GetTerrain();
            if (terrain == null)
            {
                Debug.LogError("[TerrainStamper] 未找到地形，terrainGenerator.GetTerrain()返回null");
                yield break;
            }
            
            // 添加详细的地形信息调试
            Debug.Log($"[TerrainStamper] 地形尺寸: {terrain.terrainData.size}");
            Debug.Log($"[TerrainStamper] 地形最大高度(size.y): {terrain.terrainData.size.y}");
            Debug.Log($"[TerrainStamper] 印章设置 - heightScale: {operation.stamp.heightScale}, baseHeight: {operation.stamp.baseHeight}");
            Debug.Log($"[TerrainStamper] 印章强度: {operation.strength}");
            Debug.Log($"[TerrainStamper] 印章应用位置: {operation.position}");
            
            var terrainData = terrain.terrainData;
            var heightmapResolution = terrainData.heightmapResolution;
            
            // 获取当前高度图
            float[,] heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
            
            // 计算印章影响区域
            var stampBounds = CalculateStampBounds(operation, terrain);
            
            // 应用印章数据
            yield return StartCoroutine(BlendStampData(heights, operation, stampBounds, heightmapResolution, terrain));
            
            // 更新地形高度图
            terrainData.SetHeights(0, 0, heights);
            
            Debug.Log("[TerrainStamper] CPU印章应用完成");
        }
        
        /// <summary>
        /// 计算印章影响边界
        /// </summary>
        StampBounds CalculateStampBounds(StampOperation operation, Terrain terrain)
        {
            var terrainPos = terrain.transform.position;
            var terrainSize = terrain.terrainData.size;
            var heightmapRes = terrain.terrainData.heightmapResolution;
            
            // 将世界坐标转换为地形坐标
            Vector3 localPos = operation.position - terrainPos;
            
            // 转换为高度图坐标
            int centerX = Mathf.RoundToInt((localPos.x / terrainSize.x) * (heightmapRes - 1));
            int centerZ = Mathf.RoundToInt((localPos.z / terrainSize.z) * (heightmapRes - 1));
            
            // 计算印章半径（以高度图像素为单位）
            int radiusPixels = Mathf.RoundToInt((operation.size / terrainSize.x) * (heightmapRes - 1) * 0.5f);
            
            return new StampBounds
            {
                centerX = centerX,
                centerZ = centerZ,
                radiusPixels = radiusPixels,
                minX = Mathf.Max(0, centerX - radiusPixels),
                maxX = Mathf.Min(heightmapRes - 1, centerX + radiusPixels),
                minZ = Mathf.Max(0, centerZ - radiusPixels),
                maxZ = Mathf.Min(heightmapRes - 1, centerZ + radiusPixels)
            };
        }
        
        /// <summary>
        /// 直接应用印章数据（无协程开销）
        /// </summary>
        void ApplyStampDataDirect(float[,] heights, StampOperation operation, StampBounds bounds, Terrain terrain)
        {
            Debug.Log($"[TerrainStamper] 直接应用印章数据，范围: ({bounds.minX},{bounds.minZ}) 到 ({bounds.maxX},{bounds.maxZ})");
            
            // 获取地形最大高度
            float terrainMaxHeight = terrain.terrainData.size.y;
            
            // 一次性处理所有像素，无协程开销
            for (int x = bounds.minX; x <= bounds.maxX; x++)
            {
                for (int z = bounds.minZ; z <= bounds.maxZ; z++)
                {
                    // 计算距印章中心的距离
                    float deltaX = x - bounds.centerX;
                    float deltaZ = z - bounds.centerZ;
                    float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                    
                    // 如果在印章范围内
                    if (distance <= bounds.radiusPixels)
                    {
                        // 计算印章UV坐标
                        float u = (deltaX / bounds.radiusPixels + 1f) * 0.5f;
                        float v = (deltaZ / bounds.radiusPixels + 1f) * 0.5f;
                        
                        // 应用旋转
                        if (operation.rotation != 0f)
                        {
                            Vector2 rotatedUV = RotateUV(new Vector2(u, v), operation.rotation);
                            u = rotatedUV.x;
                            v = rotatedUV.y;
                        }
                        
                        // 从印章纹理采样高度值
                        float stampHeight = SampleStampHeight(operation.stamp, u, v, terrainMaxHeight);
                        
                        // 计算衰减（基于距离）
                        float falloff = 1f - (distance / bounds.radiusPixels);
                        falloff = Mathf.SmoothStep(0f, 1f, falloff);
                        
                        // 应用混合模式
                        float newHeight = BlendHeight(heights[z, x], stampHeight, operation.strength * falloff, operation.blendMode);
                        heights[z, x] = Mathf.Clamp01(newHeight);
                    }
                }
            }
            
            Debug.Log("[TerrainStamper] 印章数据直接处理完成");
        }
        
        /// <summary>
        /// 混合印章数据到高度图（已废弃，使用ApplyStampDataDirect）
        /// </summary>
        IEnumerator BlendStampData(float[,] heights, StampOperation operation, StampBounds bounds, int resolution, Terrain terrain)
        {
            Debug.Log($"[TerrainStamper] 开始快速混合印章数据，范围: ({bounds.minX},{bounds.minZ}) 到 ({bounds.maxX},{bounds.maxZ})");
            
            // 获取地形最大高度
            float terrainMaxHeight = terrain.terrainData.size.y;
            
            for (int x = bounds.minX; x <= bounds.maxX; x++)
            {
                for (int z = bounds.minZ; z <= bounds.maxZ; z++)
                {
                    // 计算距印章中心的距离
                    float deltaX = x - bounds.centerX;
                    float deltaZ = z - bounds.centerZ;
                    float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                    
                    // 如果在印章范围内
                    if (distance <= bounds.radiusPixels)
                    {
                        // 计算印章UV坐标
                        float u = (deltaX / bounds.radiusPixels + 1f) * 0.5f;
                        float v = (deltaZ / bounds.radiusPixels + 1f) * 0.5f;
                        
                        // 应用旋转
                        if (operation.rotation != 0f)
                        {
                            Vector2 rotatedUV = RotateUV(new Vector2(u, v), operation.rotation);
                            u = rotatedUV.x;
                            v = rotatedUV.y;
                        }
                        
                        // 从印章纹理采样高度值（传入地形最大高度）
                        float stampHeight = SampleStampHeight(operation.stamp, u, v, terrainMaxHeight);
                        
                        // 计算衰减（基于距离）
                        float falloff = 1f - (distance / bounds.radiusPixels);
                        falloff = Mathf.SmoothStep(0f, 1f, falloff);
                        
                        // 记录中心点的详细信息用于调试
                        if (x == bounds.centerX && z == bounds.centerZ)
                        {
                            Debug.Log($"[TerrainStamper] 中心点调试 - 原始印章高度: {stampHeight:F4}, 衰减: {falloff:F4}, 强度: {operation.strength:F4}");
                            Debug.Log($"[TerrainStamper] 中心点调试 - 原始地形高度: {heights[z, x]:F4}");
                            Debug.Log($"[TerrainStamper] 中心点调试 - 地形最大高度: {terrainMaxHeight:F2}");
                            Debug.Log($"[TerrainStamper] 中心点调试 - 印章heightScale: {operation.stamp.heightScale:F2}");
                        }
                        
                        // 应用混合模式
                        float newHeight = BlendHeight(heights[z, x], stampHeight, operation.strength * falloff, operation.blendMode);
                        
                        // 关键问题：检查是否被Clamp01截断
                        float beforeClamp = newHeight;
                        heights[z, x] = Mathf.Clamp01(newHeight); // Unity地形要求0-1范围
                        
                        // 记录中心点混合后的高度
                        if (x == bounds.centerX && z == bounds.centerZ)
                        {
                            Debug.Log($"[TerrainStamper] 中心点调试 - 混合后高度(截断前): {beforeClamp:F4}");
                            Debug.Log($"[TerrainStamper] 中心点调试 - 混合后高度(截断后): {heights[z, x]:F4}");
                            if (beforeClamp > 1f)
                            {
                                Debug.LogWarning($"[TerrainStamper] 高度值被截断！原值: {beforeClamp:F4} -> 截断后: {heights[z, x]:F4}");
                            }
                        }
                    }
                }
            }
            
            Debug.Log("[TerrainStamper] 印章数据混合完成");
            yield return null; // 只暂停一帧
        }
        
        /// <summary>
        /// 旋转UV坐标
        /// </summary>
        Vector2 RotateUV(Vector2 uv, float angleDegrees)
        {
            // 将UV坐标移到原点
            uv -= Vector2.one * 0.5f;
            
            float angle = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            Vector2 rotated = new Vector2(
                uv.x * cos - uv.y * sin,
                uv.x * sin + uv.y * cos
            );
            
            // 移回中心
            return rotated + Vector2.one * 0.5f;
        }
        
        /// <summary>
        /// 从印章纹理采样高度值
        /// </summary>
        float SampleStampHeight(Stamp stamp, float u, float v, float terrainMaxHeight = 1000f)
        {
            if (stamp.heightTexture == null) return 0f;
            
            // 确保UV在有效范围内
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
            
            // 双线性插值采样
            int width = stamp.heightTexture.width;
            int height = stamp.heightTexture.height;
            
            float fx = u * (width - 1);
            float fy = v * (height - 1);
            
            int x1 = Mathf.FloorToInt(fx);
            int y1 = Mathf.FloorToInt(fy);
            int x2 = Mathf.Min(x1 + 1, width - 1);
            int y2 = Mathf.Min(y1 + 1, height - 1);
            
            float fracX = fx - x1;
            float fracY = fy - y1;
            
            // 获取四个采样点的值
            float h1 = stamp.heightTexture.GetPixel(x1, y1).grayscale;
            float h2 = stamp.heightTexture.GetPixel(x2, y1).grayscale;
            float h3 = stamp.heightTexture.GetPixel(x1, y2).grayscale;
            float h4 = stamp.heightTexture.GetPixel(x2, y2).grayscale;
            
            // 双线性插值
            float h12 = Mathf.Lerp(h1, h2, fracX);
            float h34 = Mathf.Lerp(h3, h4, fracX);
            float normalizedHeight = Mathf.Lerp(h12, h34, fracY);
            
            // 关键修复：应用印章的高度缩放，转换为地形高度单位
            // Unity地形高度是相对于terrainData.size.y的比例值(0-1)
            // heightScale应该是实际米数，需要转换为比例
            float stampHeightInMeters = stamp.baseHeight + normalizedHeight * stamp.heightScale;
            float stampHeightInTerrainUnits = stampHeightInMeters / terrainMaxHeight;
            
            // 不要在这里截断，让后面的混合函数处理截断
            return stampHeightInTerrainUnits;
        }
        
        /// <summary>
        /// 混合高度值
        /// </summary>
        float BlendHeight(float originalHeight, float stampHeight, float strength, StampBlendMode mode)
        {
            switch (mode)
            {
                case StampBlendMode.Add:
                    return originalHeight + stampHeight * strength;
                
                case StampBlendMode.Subtract:
                    return originalHeight - stampHeight * strength;
                
                case StampBlendMode.Multiply:
                    return Mathf.Lerp(originalHeight, originalHeight * stampHeight, strength);
                
                case StampBlendMode.Set:
                    return Mathf.Lerp(originalHeight, stampHeight, strength);
                
                case StampBlendMode.Max:
                    // 改进Max模式：取较大值，更适合山峰
                    float maxHeight = Mathf.Max(originalHeight, originalHeight + stampHeight * strength);
                    return Mathf.Lerp(originalHeight, maxHeight, strength);
                
                case StampBlendMode.Min:
                    return Mathf.Lerp(originalHeight, Mathf.Min(originalHeight, stampHeight), strength);
                
                default:
                    return originalHeight;
            }
        }
        
        /// <summary>
        /// 撤销上一个印章操作
        /// </summary>
        public void UndoLastStamp()
        {
            if (stampHistory.Count == 0)
            {
                Debug.LogWarning("[TerrainStamper] 没有可撤销的印章操作");
                return;
            }
            
            // TODO: 实现撤销功能
            // 需要保存每次操作前的高度图状态
            Debug.Log("[TerrainStamper] 撤销功能待实现");
        }
        
        /// <summary>
        /// 清除所有印章历史
        /// </summary>
        public void ClearStampHistory()
        {
            stampHistory.Clear();
            OnStampHistoryChanged?.Invoke(0);
            Debug.Log("[TerrainStamper] 印章历史已清除");
        }
        
        /// <summary>
        /// 设置当前印章
        /// </summary>
        public void SetCurrentStamp(Stamp stamp)
        {
            currentStamp = stamp;
            Debug.Log($"[TerrainStamper] 当前印章设置为: {stamp?.name ?? "空"}");
        }
        
        /// <summary>
        /// 获取印章库
        /// </summary>
        public StampLibrary GetStampLibrary()
        {
            // 如果印章库为null，尝试初始化
            if (stampLibrary == null)
            {
                Debug.Log("[TerrainStamper] GetStampLibrary发现印章库为null，尝试初始化");
                InitializeStampLibrary();
            }
            return stampLibrary;
        }
        
        // Gizmos绘制
        void OnDrawGizmos()
        {
            if (!showStampGizmo || currentStamp == null) return;
            
            Gizmos.color = previewColor;
            Gizmos.DrawWireSphere(lastStampPosition, stampSize * 0.5f);
        }
    }
    
    /// <summary>
    /// 印章混合模式
    /// </summary>
    public enum StampBlendMode
    {
        Add,        // 相加
        Subtract,   // 相减
        Multiply,   // 相乘
        Set,        // 设置
        Max,        // 最大值
        Min         // 最小值
    }
    
    /// <summary>
    /// 印章操作记录
    /// </summary>
    [System.Serializable]
    public class StampOperation
    {
        public Stamp stamp;
        public Vector3 position;
        public float size;
        public float strength;
        public float rotation;
        public StampBlendMode blendMode;
        public System.DateTime timestamp;
    }
    
    /// <summary>
    /// 印章边界信息
    /// </summary>
    public struct StampBounds
    {
        public int centerX;
        public int centerZ;
        public int radiusPixels;
        public int minX;
        public int maxX;
        public int minZ;
        public int maxZ;
    }
}