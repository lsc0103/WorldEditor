using UnityEngine;
using System.Collections.Generic;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 专业级程序化植被Mesh生成器
    /// 生成游戏级别的真实植被几何体
    /// </summary>
    public class ProceduralVegetationMeshGenerator
    {
        /// <summary>
        /// 生成针叶树Mesh（北欧云杉）
        /// </summary>
        public static GameObject CreateRealisticConifer(VegetationType type = VegetationType.针叶树)
        {
            Debug.Log($"[ProceduralVegetationMeshGenerator] 开始创建真实针叶树 - 类型: {type}");
            
            try
            {
                GameObject tree = new GameObject($"Realistic_{type}");
                Debug.Log($"[ProceduralVegetationMeshGenerator] 创建树木根对象: {tree.name}");
                
                // 创建树干
                Debug.Log("[ProceduralVegetationMeshGenerator] 创建树干...");
                GameObject trunk = CreateTrunkMesh(tree.transform);
                trunk.name = "Trunk";
                Debug.Log("[ProceduralVegetationMeshGenerator] 树干创建完成");
                
                // 创建针叶
                Debug.Log("[ProceduralVegetationMeshGenerator] 创建针叶...");
                CreateConiferFoliage(tree.transform);
                Debug.Log("[ProceduralVegetationMeshGenerator] 针叶创建完成");
                
                // 添加材质
                Debug.Log("[ProceduralVegetationMeshGenerator] 应用材质...");
                ApplyRealisticMaterials(tree, type);
                Debug.Log("[ProceduralVegetationMeshGenerator] 材质应用完成");
                
                // 添加LOD组件
                Debug.Log("[ProceduralVegetationMeshGenerator] 设置LOD系统...");
                SetupLODSystem(tree);
                Debug.Log("[ProceduralVegetationMeshGenerator] LOD系统设置完成");
                
                Debug.Log($"[ProceduralVegetationMeshGenerator] 针叶树创建成功: {tree.name}");
                return tree;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ProceduralVegetationMeshGenerator] 创建针叶树时发生错误: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// 创建真实的树干几何体
        /// </summary>
        static GameObject CreateTrunkMesh(Transform parent)
        {
            GameObject trunkObj = new GameObject("TrunkMesh");
            trunkObj.transform.SetParent(parent);
            trunkObj.transform.localPosition = Vector3.zero; // 确保在原点
            
            MeshFilter meshFilter = trunkObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = trunkObj.AddComponent<MeshRenderer>();
            
            Debug.Log("[ProceduralVegetationMeshGenerator] 创建改进的树干网格...");
            
            // 生成改进的树干Mesh
            Mesh trunkMesh = GenerateImprovedTrunkMesh();
            meshFilter.mesh = trunkMesh;
            
            Debug.Log($"[ProceduralVegetationMeshGenerator] 树干网格创建完成，顶点数: {trunkMesh.vertexCount}");
            
            return trunkObj;
        }
        
        /// <summary>
        /// 生成树干Mesh几何体
        /// </summary>
        static Mesh GenerateTrunkMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ProceduralTrunk";
            
            // 树干参数
            int segments = 8; // 圆周分段
            int heightSegments = 6; // 高度分段
            float height = 8f;
            float bottomRadius = 0.6f;
            float topRadius = 0.3f;
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            
            // 生成顶点
            for (int h = 0; h <= heightSegments; h++)
            {
                float y = (float)h / heightSegments * height;
                float radius = Mathf.Lerp(bottomRadius, topRadius, (float)h / heightSegments);
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = (float)s / segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    
                    vertices.Add(new Vector3(x, y, z));
                    normals.Add(new Vector3(x, 0, z).normalized);
                    uvs.Add(new Vector2((float)s / segments, (float)h / heightSegments));
                }
            }
            
            // 生成三角形
            for (int h = 0; h < heightSegments; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    // 第一个三角形
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);
                    
                    // 第二个三角形
                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        /// <summary>
        /// 创建真实的挪威云杉针叶结构
        /// </summary>
        static void CreateConiferFoliage(Transform parent)
        {
            Debug.Log("[ProceduralVegetationMeshGenerator] 创建真实挪威云杉针叶结构...");
            
            // 挪威云杉的真实参数 - 与树干参数匹配
            int layers = 10; // 适当减少层数，更真实
            float startHeight = 3f; // 从树干底部稍高开始
            float totalHeight = 18f; // 与树干高度匹配
            float trunkTopRadius = 0.25f; // 与树干顶部半径匹配
            float trunkBaseRadius = 0.7f; // 与树干底部半径匹配
            
            for (int i = 0; i < layers; i++)
            {
                float progress = (float)i / (layers - 1);
                float height = startHeight + (totalHeight - startHeight) * progress;
                
                // 计算当前高度的树干半径（用于分枝连接）
                float heightOnTrunk = height / 16f; // 树干高度是16f
                float trunkRadiusAtHeight = Mathf.Lerp(trunkBaseRadius, trunkTopRadius, Mathf.Pow(heightOnTrunk, 1.2f));
                
                // 挪威云杉特征：下层最宽，向上逐渐收窄
                float baseRadius = 5f; // 底部分枝长度
                float topRadius = 0.8f; // 顶部分枝长度
                float branchLength = Mathf.Lerp(baseRadius, topRadius, Mathf.Pow(progress, 0.6f));
                
                // 创建具有下垂特征的分枝层
                GameObject layer = CreateRealisticBranchLayer(parent, height, branchLength, i, layers, trunkRadiusAtHeight);
                layer.name = $"Branch_Whorl_{i}";
            }
            
            // 创建特征性的顶部尖端 - 确保在最高分枝之上
            float topPosition = totalHeight - 1f; // 从最高分枝层开始
            CreateSpruceTop(parent, topPosition);
            
            Debug.Log("[ProceduralVegetationMeshGenerator] 挪威云杉针叶结构创建完成");
        }
        
        /// <summary>
        /// 创建真实的分枝层（具有下垂特征）
        /// </summary>
        static GameObject CreateRealisticBranchLayer(Transform parent, float height, float branchLength, int layerIndex, int totalLayers, float trunkRadius)
        {
            GameObject layer = new GameObject($"BranchWhorl_{layerIndex}");
            layer.transform.SetParent(parent);
            layer.transform.localPosition = new Vector3(0, height, 0);
            
            Debug.Log($"[ProceduralVegetationMeshGenerator] 创建分枝层 {layerIndex}，高度: {height}，分枝长度: {branchLength}，树干半径: {trunkRadius}");
            
            // 挪威云杉特征：每层有5-7个主要分枝（较少更自然）
            int branchCount = layerIndex == 0 ? 5 : Random.Range(5, 7);
            float angleStep = 360f / branchCount;
            
            // 为每一层增加轻微的角度偏移，避免分枝对齐
            float layerRotationOffset = (layerIndex * 360f / 7f) % 360f;
            
            for (int b = 0; b < branchCount; b++)
            {
                float angle = b * angleStep + layerRotationOffset + Random.Range(-8f, 8f);
                Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                
                // 计算分枝起点（从树干表面开始）
                Vector3 branchStartOffset = direction * trunkRadius;
                
                // 创建单个下垂分枝
                GameObject branch = CreateDroopingBranch(layer.transform, direction, branchLength, layerIndex, totalLayers, branchStartOffset);
                branch.name = $"Branch_{b}";
            }
            
            return layer;
        }
        
        /// <summary>
        /// 创建下垂的分枝（挪威云杉的标志性特征）
        /// </summary>
        static GameObject CreateDroopingBranch(Transform parent, Vector3 direction, float length, int layerIndex, int totalLayers, Vector3 startOffset)
        {
            GameObject branch = new GameObject("DroopingBranch");
            branch.transform.SetParent(parent);
            branch.transform.localPosition = startOffset; // 从树干表面开始
            
            // 计算下垂程度（下层分枝下垂更明显）
            float droopFactor = Mathf.Lerp(1.2f, 0.4f, (float)layerIndex / totalLayers);
            int segments = 10; // 更多分段，更平滑的曲线
            
            List<Vector3> branchPoints = new List<Vector3>();
            
            // 起始点在树干表面
            branchPoints.Add(Vector3.zero);
            
            // 生成下垂曲线点
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                
                // 主分枝方向（从起始偏移开始）
                Vector3 outward = direction * length * t;
                
                // 改进的下垂效果 - 更自然的抛物线
                float droopAmount = -droopFactor * 4f * t * (1f - t) * length * 0.4f;
                
                // 添加轻微的向上弯曲（分枝末端稍微向上）
                if (t > 0.7f)
                {
                    float upwardBend = (t - 0.7f) / 0.3f; // 0到1
                    droopAmount += upwardBend * 0.3f * length;
                }
                
                Vector3 droop = Vector3.up * droopAmount;
                
                // 添加细微的自然变化
                Vector3 noise = new Vector3(
                    (Mathf.PerlinNoise(t * 2f, layerIndex * 0.3f) - 0.5f) * 0.2f,
                    (Mathf.PerlinNoise(t * 2f + 50f, layerIndex * 0.3f) - 0.5f) * 0.1f,
                    (Mathf.PerlinNoise(t * 2f + 100f, layerIndex * 0.3f) - 0.5f) * 0.2f
                );
                
                branchPoints.Add(outward + droop + noise);
            }
            
            // 创建分枝几何体
            CreateBranchMesh(branch, branchPoints);
            
            // 在分枝上添加针叶束
            AddNeedleClusters(branch, branchPoints);
            
            return branch;
        }
        
        /// <summary>
        /// 创建分枝的网格
        /// </summary>
        static void CreateBranchMesh(GameObject branch, List<Vector3> branchPoints)
        {
            MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
            MeshRenderer renderer = branch.AddComponent<MeshRenderer>();
            
            Mesh mesh = new Mesh();
            mesh.name = "DroopingBranchMesh";
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            int segments = branchPoints.Count - 1;
            int radialSegments = 6; // 分枝圆周分段
            
            // 为每个点创建圆形截面
            for (int i = 0; i < branchPoints.Count; i++)
            {
                float t = (float)i / segments;
                float radius = Mathf.Lerp(0.08f, 0.02f, t); // 分枝从粗到细
                
                Vector3 point = branchPoints[i];
                Vector3 forward = i < segments ? (branchPoints[i + 1] - point).normalized : 
                                               (point - branchPoints[i - 1]).normalized;
                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, forward).normalized;
                
                // 创建圆形截面
                for (int r = 0; r < radialSegments; r++)
                {
                    float angle = (float)r / radialSegments * Mathf.PI * 2f;
                    Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                    
                    vertices.Add(point + offset);
                    normals.Add(offset.normalized);
                    uvs.Add(new Vector2((float)r / radialSegments, t));
                }
            }
            
            // 生成三角形
            for (int i = 0; i < segments; i++)
            {
                for (int r = 0; r < radialSegments; r++)
                {
                    int current = i * radialSegments + r;
                    int next = i * radialSegments + (r + 1) % radialSegments;
                    int currentNext = (i + 1) * radialSegments + r;
                    int nextNext = (i + 1) * radialSegments + (r + 1) % radialSegments;
                    
                    // 两个三角形形成四边形
                    triangles.AddRange(new int[] { current, currentNext, next });
                    triangles.AddRange(new int[] { next, currentNext, nextNext });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }
        
        /// <summary>
        /// 在分枝上添加针叶束
        /// </summary>
        static void AddNeedleClusters(GameObject branch, List<Vector3> branchPoints)
        {
            // 沿着分枝添加针叶束 - 更密集，更真实
            for (int i = 2; i < branchPoints.Count; i++) // 从第3个点开始
            {
                if (i % 2 == 0) // 每隔一个点添加针叶
                {
                    Vector3 position = branchPoints[i];
                    
                    // 计算分枝方向用于针叶朝向
                    Vector3 branchDirection = Vector3.zero;
                    if (i < branchPoints.Count - 1)
                    {
                        branchDirection = (branchPoints[i + 1] - branchPoints[i - 1]).normalized;
                    }
                    
                    CreateNeedleCluster(branch.transform, position, i, branchDirection);
                }
            }
        }
        
        /// <summary>
        /// 创建单个针叶束
        /// </summary>
        static void CreateNeedleCluster(Transform parent, Vector3 position, int index, Vector3 branchDirection)
        {
            GameObject needleCluster = new GameObject($"NeedleCluster_{index}");
            needleCluster.transform.SetParent(parent);
            needleCluster.transform.localPosition = position;
            
            MeshFilter meshFilter = needleCluster.AddComponent<MeshFilter>();
            MeshRenderer renderer = needleCluster.AddComponent<MeshRenderer>();
            
            Mesh mesh = new Mesh();
            mesh.name = "NeedleClusterMesh";
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            // 创建更真实的针叶束
            int needleCount = Random.Range(12, 20); // 更多针叶
            for (int n = 0; n < needleCount; n++)
            {
                // 改进的针叶方向 - 更符合挪威云杉特征
                float angle = Random.Range(0f, 360f);
                
                // 针叶主要向下和沿分枝方向分布
                float tilt = Random.Range(-20f, -50f); // 不要太垂直
                
                // 结合分枝方向，让针叶有自然的朝向
                Vector3 baseDirection = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * Mathf.Cos(tilt * Mathf.Deg2Rad),
                    Mathf.Sin(tilt * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad) * Mathf.Cos(tilt * Mathf.Deg2Rad)
                );
                
                // 与分枝方向混合，让针叶稍微朝向分枝末端
                Vector3 direction = Vector3.Lerp(baseDirection, branchDirection, 0.3f).normalized;
                
                // 针叶长度变化 - 挪威云杉的针叶约12-20mm
                float needleLength = Random.Range(1.2f, 2.0f);
                
                CreateSingleNeedle(vertices, triangles, normals, uvs, direction * needleLength);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }
        
        /// <summary>
        /// 创建单根针叶
        /// </summary>
        static void CreateSingleNeedle(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs, Vector3 direction)
        {
            int startIndex = vertices.Count;
            
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * 0.02f;
            
            // 针叶是细长的四边形
            vertices.Add(Vector3.zero - right);
            vertices.Add(Vector3.zero + right);
            vertices.Add(direction + right);
            vertices.Add(direction - right);
            
            // 法线
            Vector3 normal = Vector3.Cross(right, direction).normalized;
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
            }
            
            // UV
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
            
            // 三角形（双面）
            triangles.AddRange(new int[] { 
                startIndex, startIndex + 1, startIndex + 2,
                startIndex, startIndex + 2, startIndex + 3,
                // 反面
                startIndex + 2, startIndex + 1, startIndex,
                startIndex + 3, startIndex + 2, startIndex
            });
        }
        
        /// <summary>
        /// 创建挪威云杉特征性的尖顶
        /// </summary>
        static void CreateSpruceTop(Transform parent, float height)
        {
            GameObject top = new GameObject("SpruceTop");
            top.transform.SetParent(parent);
            top.transform.localPosition = new Vector3(0, height, 0);
            
            MeshFilter meshFilter = top.AddComponent<MeshFilter>();
            MeshRenderer renderer = top.AddComponent<MeshRenderer>();
            
            Debug.Log($"[ProceduralVegetationMeshGenerator] 创建挪威云杉尖顶，高度: {height}");
            
            // 创建改进的细长尖顶
            Mesh topMesh = CreateImprovedSpruceTopMesh();
            meshFilter.mesh = topMesh;
        }
        
        /// <summary>
        /// 生成改进的挪威云杉顶部网格
        /// </summary>
        static Mesh CreateImprovedSpruceTopMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ImprovedSpruceTopMesh";
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            // 改进的树顶参数 - 更细长，更自然
            float topHeight = 3f; // 更高的尖顶
            float baseRadius = 0.15f; // 更小的基础半径
            int segments = 6; // 减少分段，更简洁
            int heightSegments = 8; // 高度分段，更平滑
            
            Debug.Log($"[CreateImprovedSpruceTopMesh] 树顶参数 - 高度: {topHeight}, 基础半径: {baseRadius}");
            
            // 生成多层圆锥，从底部到顶部逐渐变细
            for (int h = 0; h <= heightSegments; h++)
            {
                float heightProgress = (float)h / heightSegments;
                float y = heightProgress * topHeight;
                
                // 指数衰减半径，顶部非常尖
                float radius = baseRadius * Mathf.Pow(1f - heightProgress, 2.5f);
                
                if (h == heightSegments)
                {
                    // 最顶端是单个点
                    vertices.Add(new Vector3(0, y, 0));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(0.5f, 1f));
                }
                else
                {
                    // 当前高度的圆形截面
                    for (int s = 0; s < segments; s++)
                    {
                        float angle = (float)s / segments * Mathf.PI * 2f;
                        
                        Vector3 vertex = new Vector3(
                            Mathf.Cos(angle) * radius,
                            y,
                            Mathf.Sin(angle) * radius
                        );
                        
                        vertices.Add(vertex);
                        
                        // 计算法线（指向外部和稍微向上）
                        Vector3 radialDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                        Vector3 normal = Vector3.Lerp(radialDirection, Vector3.up, 0.6f).normalized;
                        normals.Add(normal);
                        
                        // UV坐标
                        uvs.Add(new Vector2((float)s / segments, heightProgress));
                    }
                }
            }
            
            // 生成三角形（侧面）
            for (int h = 0; h < heightSegments; h++)
            {
                if (h == heightSegments - 1)
                {
                    // 连接到顶点
                    int topVertexIndex = vertices.Count - 1;
                    int currentRingStart = h * segments;
                    
                    for (int s = 0; s < segments; s++)
                    {
                        int current = currentRingStart + s;
                        int next = currentRingStart + (s + 1) % segments;
                        
                        // 三角形连接到顶点
                        triangles.AddRange(new int[] { 
                            topVertexIndex, next, current 
                        });
                    }
                }
                else
                {
                    // 连接相邻的圆环
                    int currentRingStart = h * segments;
                    int nextRingStart = (h + 1) * segments;
                    
                    for (int s = 0; s < segments; s++)
                    {
                        int current = currentRingStart + s;
                        int next = currentRingStart + (s + 1) % segments;
                        int currentUpper = nextRingStart + s;
                        int nextUpper = nextRingStart + (s + 1) % segments;
                        
                        // 两个三角形组成四边形
                        triangles.AddRange(new int[] { 
                            current, currentUpper, next,
                            next, currentUpper, nextUpper 
                        });
                    }
                }
            }
            
            // 添加底部封闭（可选，因为会被树干遮挡）
            int bottomCenterIndex = vertices.Count;
            vertices.Add(Vector3.zero);
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0f));
            
            // 底部三角形扇形
            for (int s = 0; s < segments; s++)
            {
                int current = s;
                int next = (s + 1) % segments;
                
                triangles.AddRange(new int[] { 
                    bottomCenterIndex, next, current 
                });
            }
            
            // 设置网格数据
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            
            Debug.Log($"[CreateImprovedSpruceTopMesh] 树顶网格完成 - 顶点: {mesh.vertexCount}, 三角形: {mesh.triangles.Length / 3}");
            
            return mesh;
        }
        
        /// <summary>
        /// 创建单层针叶
        /// </summary>
        static GameObject CreateFoliageLayer(Transform parent, float height, float radius, int layerIndex)
        {
            GameObject layer = new GameObject($"FoliageLayer_{layerIndex}");
            layer.transform.SetParent(parent);
            layer.transform.localPosition = new Vector3(0, height, 0);
            
            MeshFilter meshFilter = layer.AddComponent<MeshFilter>();
            MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
            
            // 生成针叶层Mesh
            Mesh foliageMesh = GenerateFoliageMesh(radius, layerIndex);
            meshFilter.mesh = foliageMesh;
            
            return layer;
        }
        
        /// <summary>
        /// 生成针叶层的Mesh
        /// </summary>
        static Mesh GenerateFoliageMesh(float radius, int layerIndex)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"FoliageLayer_{layerIndex}";
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            
            // 生成多个针叶束
            int branchCount = 8 + layerIndex * 2; // 下层更密
            float branchLength = radius * 0.8f;
            
            for (int b = 0; b < branchCount; b++)
            {
                float angle = (float)b / branchCount * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                
                // 创建针叶束
                CreateNeedleCluster(vertices, normals, uvs, triangles, direction * radius, branchLength);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        /// <summary>
        /// 创建针叶束几何体
        /// </summary>
        static void CreateNeedleCluster(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, 
                                      List<int> triangles, Vector3 position, float length)
        {
            int startIndex = vertices.Count;
            
            // 针叶束参数
            int needleCount = 12;
            float needleWidth = 0.1f;
            float droop = -0.3f; // 下垂角度
            
            for (int n = 0; n < needleCount; n++)
            {
                float needleAngle = (float)n / needleCount * Mathf.PI * 2f;
                float needleRadius = length * 0.2f;
                
                Vector3 needleBase = position + new Vector3(
                    Mathf.Cos(needleAngle) * needleRadius,
                    0,
                    Mathf.Sin(needleAngle) * needleRadius
                );
                
                Vector3 needleTip = needleBase + Vector3.forward * length + Vector3.up * droop;
                
                // 创建针叶四边形
                CreateNeedleQuad(vertices, normals, uvs, triangles, needleBase, needleTip, needleWidth);
            }
        }
        
        /// <summary>
        /// 创建单根针叶的四边形
        /// </summary>
        static void CreateNeedleQuad(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
                                   List<int> triangles, Vector3 basePos, Vector3 tipPos, float width)
        {
            int startIndex = vertices.Count;
            
            Vector3 direction = (tipPos - basePos).normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * width * 0.5f;
            
            // 四个顶点
            vertices.Add(basePos - right);  // 0
            vertices.Add(basePos + right);  // 1
            vertices.Add(tipPos + right);   // 2
            vertices.Add(tipPos - right);   // 3
            
            // 法线
            Vector3 normal = Vector3.Cross(right, direction);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
            }
            
            // UV坐标
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
            
            // 三角形
            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
            
            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
        }
        
        /// <summary>
        /// 创建树顶
        /// </summary>
        static void CreateTreeTop(Transform parent, float height)
        {
            GameObject top = new GameObject("TreeTop");
            top.transform.SetParent(parent);
            top.transform.localPosition = new Vector3(0, height, 0);
            
            MeshFilter meshFilter = top.AddComponent<MeshFilter>();
            MeshRenderer renderer = top.AddComponent<MeshRenderer>();
            
            // 创建尖锐的树顶
            Mesh topMesh = GenerateTreeTopMesh();
            meshFilter.mesh = topMesh;
        }
        
        static Mesh GenerateTreeTopMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "TreeTop";
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            
            // 简单的锥形树顶
            vertices.Add(Vector3.zero); // 中心点
            vertices.Add(Vector3.up * 1.5f); // 顶点
            
            int segments = 8;
            float radius = 0.5f;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
            }
            
            // 生成三角形
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(1); // 顶点
                triangles.Add(2 + i);
                triangles.Add(2 + (i + 1) % segments);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// 应用真实的材质
        /// </summary>
        static void ApplyRealisticMaterials(GameObject tree, VegetationType type)
        {
            Debug.Log("[ProceduralVegetationMeshGenerator] 应用真实材质...");
            
            // 创建改进的树皮材质
            Material barkMaterial = CreateImprovedBarkMaterial();
            
            // 创建真实的针叶材质
            Material foliageMaterial = CreateFoliageMaterial(type);
            
            // 创建分枝材质（略深于树皮）
            Material branchMaterial = CreateBranchMaterial();
            
            // 应用材质到树干
            var trunkRenderer = tree.transform.Find("TrunkMesh")?.GetComponent<MeshRenderer>();
            if (trunkRenderer != null)
            {
                trunkRenderer.material = barkMaterial;
                Debug.Log("[ProceduralVegetationMeshGenerator] 树干材质已应用");
            }
            
            // 递归应用材质到所有子对象
            ApplyMaterialsRecursively(tree.transform, barkMaterial, foliageMaterial, branchMaterial);
            
            Debug.Log("[ProceduralVegetationMeshGenerator] 所有材质应用完成");
        }
        
        /// <summary>
        /// 递归应用材质到所有子对象
        /// </summary>
        static void ApplyMaterialsRecursively(Transform parent, Material barkMaterial, Material foliageMaterial, Material branchMaterial)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var renderer = child.GetComponent<MeshRenderer>();
                
                if (renderer != null)
                {
                    // 根据对象名称分配不同材质
                    if (child.name.Contains("Trunk") || child.name.Contains("TrunkMesh"))
                    {
                        renderer.material = barkMaterial;
                    }
                    else if (child.name.Contains("Branch") || child.name.Contains("Drooping"))
                    {
                        renderer.material = branchMaterial;
                    }
                    else if (child.name.Contains("Needle") || child.name.Contains("Foliage") || 
                            child.name.Contains("Top") || child.name.Contains("Spruce"))
                    {
                        renderer.material = foliageMaterial;
                    }
                    else
                    {
                        // 默认使用针叶材质
                        renderer.material = foliageMaterial;
                    }
                }
                
                // 递归处理子对象
                ApplyMaterialsRecursively(child, barkMaterial, foliageMaterial, branchMaterial);
            }
        }
        
        /// <summary>
        /// 创建分枝材质
        /// </summary>
        static Material CreateBranchMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material material = new Material(shader);
            material.name = "RealisticBranch";
            
            // 分枝颜色（比树皮稍深，带红褐色）
            material.color = new Color(0.3f, 0.2f, 0.15f, 1f);
            
            // 表面属性
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.2f);
            
            return material;
        }
        
        /// <summary>
        /// 创建真实的树皮材质
        /// </summary>
        static Material CreateBarkMaterial()
        {
            // URP兼容的着色器
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard"); // 后备方案
            
            Material material = new Material(shader);
            material.name = "RealisticBark";
            
            // 树皮颜色
            material.color = new Color(0.35f, 0.25f, 0.15f, 1f);
            
            // 表面属性
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.2f);
            
            // 程序化生成树皮纹理
            Texture2D barkTexture = GenerateBarkTexture();
            material.mainTexture = barkTexture;
            
            return material;
        }
        
        /// <summary>
        /// 创建真实的针叶材质
        /// </summary>
        static Material CreateFoliageMaterial(VegetationType type)
        {
            // URP兼容的着色器
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard"); // 后备方案
            
            Material material = new Material(shader);
            material.name = "RealisticNorwaySpruceNeedles";
            
            // 挪威云杉针叶的真实颜色（深绿色带蓝色调）
            material.color = new Color(0.08f, 0.35f, 0.12f, 1f);
            
            // 表面属性 - 针叶是哑光的，不反光
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.1f); // 非常低的光滑度
            
            // 启用双面渲染（针叶需要双面可见）
            material.SetInt("_Cull", 0); // 双面渲染
            
            // 创建简单的针叶纹理
            Texture2D needleTexture = CreateNeedleTexture();
            material.mainTexture = needleTexture;
            
            // 设置为半透明cutout模式，用于针叶边缘
            material.SetFloat("_Mode", 1f); // Cutout模式
            material.SetFloat("_Cutoff", 0.3f); // Alpha cutoff
            
            return material;
        }
        
        /// <summary>
        /// 创建程序化针叶纹理
        /// </summary>
        static Texture2D CreateNeedleTexture()
        {
            int width = 64;
            int height = 256; // 针叶是细长的
            Texture2D texture = new Texture2D(width, height);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float centerDistance = Mathf.Abs((float)x / width - 0.5f) * 2f;
                    float lengthProgress = (float)y / height;
                    
                    // 针叶形状 - 中间宽，两端窄
                    float needleWidth = (1f - centerDistance) * (1f - Mathf.Pow(lengthProgress, 2f));
                    
                    // 基础绿色
                    float greenIntensity = 0.3f + needleWidth * 0.2f;
                    
                    // 添加细微的颜色变化
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.1f;
                    
                    Color needleColor = new Color(
                        0.08f + noise,
                        greenIntensity + noise,
                        0.12f + noise * 0.5f,
                        needleWidth > 0.1f ? 1f : 0f // Alpha cutoff for needle shape
                    );
                    
                    texture.SetPixel(x, y, needleColor);
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 创建改进的树皮材质
        /// </summary>
        static Material CreateImprovedBarkMaterial()
        {
            // URP兼容的着色器
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material material = new Material(shader);
            material.name = "RealisticNorwaySpruceBar";
            
            // 挪威云杉树皮颜色（红褐色到灰褐色）
            material.color = new Color(0.45f, 0.35f, 0.25f, 1f);
            
            // 表面属性
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f); // 粗糙的树皮表面
            
            // 创建更详细的树皮纹理
            Texture2D barkTexture = CreateDetailedBarkTexture();
            material.mainTexture = barkTexture;
            
            // 添加法线贴图效果（模拟）
            material.SetFloat("_BumpScale", 1f);
            
            return material;
        }
        
        /// <summary>
        /// 创建详细的树皮纹理
        /// </summary>
        static Texture2D CreateDetailedBarkTexture()
        {
            int width = 512;
            int height = 512;
            Texture2D texture = new Texture2D(width, height);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 多层噪声创建复杂的树皮纹理
                    float noise1 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                    float noise2 = Mathf.PerlinNoise(x * 0.1f, y * 0.3f) * 0.5f; // 垂直纹理
                    float noise3 = Mathf.PerlinNoise(x * 0.2f, y * 0.1f) * 0.3f; // 细节纹理
                    float noise4 = Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.2f; // 高频细节
                    
                    float combined = (noise1 + noise2 + noise3 + noise4) / 2.0f;
                    
                    // 挪威云杉树皮的特征色彩变化
                    float r = 0.35f + combined * 0.25f + Mathf.Sin(y * 0.02f) * 0.05f;
                    float g = 0.25f + combined * 0.2f;
                    float b = 0.15f + combined * 0.15f;
                    
                    // 添加红褐色调（挪威云杉的特征）
                    r += 0.1f * Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                    
                    texture.SetPixel(x, y, new Color(r, g, b, 1f));
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 程序化生成树皮纹理
        /// </summary>
        static Texture2D GenerateBarkTexture()
        {
            int width = 256;
            int height = 256;
            Texture2D texture = new Texture2D(width, height);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 使用Perlin噪声创建树皮纹理
                    float noise1 = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    float noise2 = Mathf.PerlinNoise(x * 0.05f, y * 0.3f) * 0.5f;
                    float noise3 = Mathf.PerlinNoise(x * 0.2f, y * 0.05f) * 0.3f;
                    
                    float combined = (noise1 + noise2 + noise3) / 1.8f;
                    
                    // 树皮颜色变化
                    float r = 0.3f + combined * 0.2f;
                    float g = 0.2f + combined * 0.15f;
                    float b = 0.1f + combined * 0.1f;
                    
                    texture.SetPixel(x, y, new Color(r, g, b, 1f));
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// 生成真实的草地Mesh
        /// </summary>
        public static GameObject CreateRealisticGrass(VegetationType type = VegetationType.野草)
        {
            GameObject grassCluster = new GameObject($"Realistic_{type}");
            
            // 创建多个草叶
            int grassCount = Random.Range(8, 15);
            
            for (int i = 0; i < grassCount; i++)
            {
                GameObject grass = CreateSingleGrassBlade(grassCluster.transform, i);
            }
            
            // 应用草地材质
            ApplyGrassMaterial(grassCluster, type);
            
            return grassCluster;
        }
        
        /// <summary>
        /// 创建单根草叶
        /// </summary>
        static GameObject CreateSingleGrassBlade(Transform parent, int index)
        {
            GameObject grassBlade = new GameObject($"GrassBlade_{index}");
            grassBlade.transform.SetParent(parent);
            
            MeshFilter meshFilter = grassBlade.AddComponent<MeshFilter>();
            MeshRenderer renderer = grassBlade.AddComponent<MeshRenderer>();
            
            // 生成草叶几何体
            Mesh grassMesh = GenerateGrassBladeMesh();
            meshFilter.mesh = grassMesh;
            
            // 随机位置和旋转
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(0f, 0.5f);
            
            grassBlade.transform.localPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );
            
            grassBlade.transform.localRotation = Quaternion.Euler(
                Random.Range(-10f, 10f),
                Random.Range(0f, 360f),
                Random.Range(-15f, 15f)
            );
            
            float scale = Random.Range(0.8f, 1.4f);
            grassBlade.transform.localScale = Vector3.one * scale;
            
            return grassBlade;
        }
        
        /// <summary>
        /// 生成单根草叶的Mesh
        /// </summary>
        static Mesh GenerateGrassBladeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "GrassBlade";
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            
            // 草叶参数
            float width = 0.05f;
            float height = Random.Range(0.8f, 1.5f);
            int segments = 4;
            
            // 生成弯曲的草叶
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float y = t * height;
                
                // 添加弯曲效果
                float bend = Mathf.Sin(t * Mathf.PI * 0.5f) * 0.3f;
                float currentWidth = width * (1f - t * 0.7f); // 顶部更窄
                
                // 左侧顶点
                vertices.Add(new Vector3(-currentWidth * 0.5f, y, bend));
                normals.Add(Vector3.forward);
                uvs.Add(new Vector2(0, t));
                
                // 右侧顶点
                vertices.Add(new Vector3(currentWidth * 0.5f, y, bend));
                normals.Add(Vector3.forward);
                uvs.Add(new Vector2(1, t));
            }
            
            // 生成三角形
            for (int i = 0; i < segments; i++)
            {
                int baseIndex = i * 2;
                
                // 第一个三角形
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                
                // 第二个三角形
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
                
                // 反面（双面渲染）
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        /// <summary>
        /// 应用草地材质
        /// </summary>
        static void ApplyGrassMaterial(GameObject grassCluster, VegetationType type)
        {
            Material grassMaterial = CreateGrassMaterial(type);
            
            // 应用到所有草叶
            var renderers = grassCluster.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = grassMaterial;
            }
        }
        
        /// <summary>
        /// 创建草地材质
        /// </summary>
        static Material CreateGrassMaterial(VegetationType type)
        {
            // URP兼容的着色器
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard"); // 后备方案
            
            Material material = new Material(shader);
            material.name = "RealisticGrass";
            
            // 根据类型设置不同颜色
            switch (type)
            {
                case VegetationType.野草:
                    material.color = new Color(0.2f, 0.6f, 0.1f, 1f);
                    break;
                case VegetationType.鲜花:
                    material.color = new Color(0.8f, 0.4f, 0.6f, 1f);
                    break;
                case VegetationType.蕨类:
                    material.color = new Color(0.1f, 0.5f, 0.2f, 1f);
                    break;
                default:
                    material.color = new Color(0.3f, 0.7f, 0.2f, 1f);
                    break;
            }
            
            // 表面属性
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.4f);
            
            // 双面渲染
            material.SetInt("_Cull", 0);
            
            return material;
        }
        
        /// <summary>
        /// 生成改进的树干网格 - 修复断裂问题
        /// </summary>
        static Mesh GenerateImprovedTrunkMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ImprovedTrunkMesh";
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            // 改进的树干参数
            float trunkHeight = 16f; // 总高度的80%，为分枝留空间
            float baseRadius = 0.7f; // 底部半径
            float topRadius = 0.25f; // 顶部半径（更细）
            int heightSegments = 24; // 更多高度分段，更平滑
            int radialSegments = 16; // 更多圆周分段，更圆滑
            
            Debug.Log($"[GenerateImprovedTrunkMesh] 参数 - 高度: {trunkHeight}, 底部半径: {baseRadius}, 顶部半径: {topRadius}");
            
            // 生成树干的圆形截面
            for (int h = 0; h <= heightSegments; h++)
            {
                float heightProgress = (float)h / heightSegments;
                float y = heightProgress * trunkHeight;
                
                // 改进的锥形计算 - 使用指数曲线让顶部更细
                float radiusProgress = Mathf.Pow(1f - heightProgress, 1.2f);
                float radius = Mathf.Lerp(topRadius, baseRadius, radiusProgress);
                
                // 添加轻微的自然变化（树干不是完美的圆锥）
                float naturalTaper = 1f + Mathf.Sin(heightProgress * Mathf.PI * 0.5f) * 0.05f;
                radius *= naturalTaper;
                
                // 创建当前高度的圆形截面
                for (int r = 0; r < radialSegments; r++)
                {
                    float angle = (float)r / radialSegments * Mathf.PI * 2f;
                    
                    // 添加轻微的表面起伏（模拟树皮纹理）
                    float barkNoise = 1f + Mathf.PerlinNoise(
                        angle * 3f + heightProgress * 0.5f, 
                        heightProgress * 4f
                    ) * 0.03f;
                    
                    float finalRadius = radius * barkNoise;
                    
                    Vector3 vertex = new Vector3(
                        Mathf.Cos(angle) * finalRadius,
                        y,
                        Mathf.Sin(angle) * finalRadius
                    );
                    
                    vertices.Add(vertex);
                    
                    // 计算正确的法线（考虑锥形）
                    Vector3 radialDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 normal = Vector3.Lerp(radialDirection, Vector3.up, 0.1f).normalized;
                    normals.Add(normal);
                    
                    // UV坐标 - 改进映射
                    uvs.Add(new Vector2((float)r / radialSegments, heightProgress));
                }
            }
            
            // 生成三角形（确保正确的绕向）
            for (int h = 0; h < heightSegments; h++)
            {
                for (int r = 0; r < radialSegments; r++)
                {
                    int current = h * radialSegments + r;
                    int next = h * radialSegments + (r + 1) % radialSegments;
                    int currentUpper = (h + 1) * radialSegments + r;
                    int nextUpper = (h + 1) * radialSegments + (r + 1) % radialSegments;
                    
                    // 确保正确的三角形绕向
                    triangles.AddRange(new int[] { 
                        current, currentUpper, next,
                        next, currentUpper, nextUpper 
                    });
                }
            }
            
            // 添加底部封闭（重要：避免洞）
            int bottomCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0, 0, 0)); // 底部中心点
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));
            
            // 底部三角形扇形
            for (int r = 0; r < radialSegments; r++)
            {
                int current = r;
                int next = (r + 1) % radialSegments;
                
                // 注意绕向：从下面看是逆时针
                triangles.AddRange(new int[] { 
                    bottomCenterIndex, next, current 
                });
            }
            
            // 可选：添加顶部封闭（虽然会被分枝遮挡）
            int topCenterIndex = vertices.Count;
            Vector3 topCenter = new Vector3(0, trunkHeight, 0);
            vertices.Add(topCenter);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 1f));
            
            // 顶部三角形扇形
            int topRowStart = heightSegments * radialSegments;
            for (int r = 0; r < radialSegments; r++)
            {
                int current = topRowStart + r;
                int next = topRowStart + (r + 1) % radialSegments;
                
                // 从上面看是顺时针
                triangles.AddRange(new int[] { 
                    topCenterIndex, current, next 
                });
            }
            
            // 设置网格数据
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            
            // 重新计算边界和切线
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            
            Debug.Log($"[GenerateImprovedTrunkMesh] 树干网格完成 - 顶点: {mesh.vertexCount}, 三角形: {mesh.triangles.Length / 3}");
            
            return mesh;
        }
        
        /// <summary>
        /// 设置LOD系统
        /// </summary>
        static void SetupLODSystem(GameObject tree)
        {
            LODGroup lodGroup = tree.AddComponent<LODGroup>();
            
            // 创建LOD级别
            LOD[] lods = new LOD[3];
            
            // LOD 0 - 高质量（近距离）
            lods[0] = new LOD(0.6f, tree.GetComponentsInChildren<Renderer>());
            
            // LOD 1 - 中等质量（中距离）
            lods[1] = new LOD(0.2f, tree.GetComponentsInChildren<Renderer>());
            
            // LOD 2 - 低质量（远距离）
            lods[2] = new LOD(0.05f, new Renderer[0]); // 消失
            
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
    }
}