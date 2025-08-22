using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 高级针叶树生成器 - 基于改进的挪威云杉算法
    /// </summary>
    public class AdvancedConiferGenerator
    {
        public void GenerateRealisticConifer(GameObject tree, TreeGenerationParams treeParams)
        {
            // 使用我们改进的挪威云杉算法创建针叶树
            CreateImprovedSpruceStructure(tree, treeParams);
        }
        
        void CreateImprovedSpruceStructure(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建树干
            var trunkMesh = CreateTrunkMesh(treeParams.height, treeParams.trunkRadius, 24, 24);
            var trunkObj = CreateMeshObject(tree, "Trunk", trunkMesh, GetBarkMaterial());
            
            // 创建分支层
            int layers = treeParams.branchLayers;
            for (int i = 0; i < layers; i++)
            {
                float layerHeight = treeParams.height * (0.2f + 0.6f * i / layers); // 从20%开始到80%
                float branchLength = treeParams.trunkRadius * (3f - 2f * i / layers); // 上层分支更短
                
                var branchMesh = CreateBranchLayerMesh(layerHeight, branchLength, 6 + i, treeParams.trunkRadius);
                var branchObj = CreateMeshObject(tree, $"BranchLayer_{i}", branchMesh, GetBranchMaterial());
                
                // 添加针叶
                if (treeParams.hasLeaves)
                {
                    var needleMesh = CreateNeedleClusterMesh(layerHeight, branchLength * 0.8f, 0.8f - 0.1f * i / layers);
                    var needleObj = CreateMeshObject(tree, $"Needles_{i}", needleMesh, GetNeedleMaterial(treeParams.foliageColor));
                }
            }
            
            // 创建树顶
            if (treeParams.hasLeaves)
            {
                var topMesh = CreateSpruceTopMesh();
                var topObj = CreateMeshObject(tree, "Top", topMesh, GetNeedleMaterial(treeParams.foliageColor));
                topObj.transform.localPosition = new Vector3(0, treeParams.height * 0.85f, 0);
            }
        }
        
        GameObject CreateMeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            return obj;
        }
        
        Material GetBarkMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.4f, 0.3f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            return material;
        }
        
        Material GetBranchMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.25f, 0.18f, 0.12f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.15f);
            return material;
        }
        
        Material GetNeedleMaterial(Color foliageColor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", foliageColor);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.2f);
            // 启用双面渲染
            material.SetFloat("_Cull", 0);
            return material;
        }
        
        // 网格生成方法
        Mesh CreateTrunkMesh(float height, float radius, int segments, int heightSegments)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float t = (float)h / heightSegments;
                float y = height * t;
                float currentRadius = radius * (1f - t * 0.3f); // 向上逐渐变细
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * currentRadius,
                        y,
                        Mathf.Sin(angle) * currentRadius
                    ));
                }
            }
            
            for (int h = 0; h < heightSegments; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        Mesh CreateBranchLayerMesh(float height, float branchLength, int branchCount, float trunkRadius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            for (int i = 0; i < branchCount; i++)
            {
                float angle = (360f / branchCount) * i * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 branchStart = direction * trunkRadius;
                Vector3 branchEnd = direction * branchLength + Vector3.down * branchLength * 0.3f;
                
                // 简化的分支几何
                int branchSegments = 4;
                for (int j = 0; j <= branchSegments; j++)
                {
                    float t = (float)j / branchSegments;
                    Vector3 pos = Vector3.Lerp(branchStart, branchEnd, t);
                    pos.y = height;
                    
                    float branchRadius = Mathf.Lerp(0.05f, 0.02f, t);
                    
                    // 添加简单的圆形截面
                    for (int k = 0; k < 4; k++)
                    {
                        float circleAngle = k * Mathf.PI * 0.5f;
                        Vector3 offset = new Vector3(
                            Mathf.Cos(circleAngle) * branchRadius,
                            Mathf.Sin(circleAngle) * branchRadius,
                            0
                        );
                        vertices.Add(pos + offset);
                    }
                }
                
                // 生成三角面
                int baseIndex = i * (branchSegments + 1) * 4;
                for (int j = 0; j < branchSegments; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        int current = baseIndex + j * 4 + k;
                        int next = baseIndex + (j + 1) * 4 + k;
                        int currentNext = baseIndex + j * 4 + ((k + 1) % 4);
                        int nextNext = baseIndex + (j + 1) * 4 + ((k + 1) % 4);
                        
                        triangles.AddRange(new[] { current, next, currentNext });
                        triangles.AddRange(new[] { currentNext, next, nextNext });
                    }
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        Mesh CreateNeedleClusterMesh(float height, float radius, float density)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int needleCount = Mathf.RoundToInt(density * 50);
            
            for (int i = 0; i < needleCount; i++)
            {
                // 随机位置在圆形区域内
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 needlePos = new Vector3(randomCircle.x, height, randomCircle.y);
                
                // 创建简单的针叶
                float needleLength = 0.1f;
                Vector3[] needleVerts = new Vector3[]
                {
                    needlePos,
                    needlePos + Vector3.up * needleLength * 0.5f + Vector3.right * 0.01f,
                    needlePos + Vector3.up * needleLength * 0.5f + Vector3.left * 0.01f,
                    needlePos + Vector3.up * needleLength
                };
                
                int baseIndex = vertices.Count;
                vertices.AddRange(needleVerts);
                
                triangles.AddRange(new[] {
                    baseIndex, baseIndex + 1, baseIndex + 2,
                    baseIndex + 1, baseIndex + 3, baseIndex + 2
                });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        Mesh CreateSpruceTopMesh()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            float topHeight = 2f;
            float baseRadius = 0.3f;
            int segments = 8;
            
            // 锥形顶部
            vertices.Add(new Vector3(0, topHeight, 0)); // 顶点
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * baseRadius,
                    0,
                    Mathf.Sin(angle) * baseRadius
                ));
            }
            
            // 生成三角面
            for (int i = 0; i < segments; i++)
            {
                triangles.AddRange(new[] { 0, i + 1, ((i + 1) % segments) + 1 });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    /// <summary>
    /// 高级阔叶树生成器
    /// </summary>
    public class AdvancedDeciduousGenerator
    {
        public void GenerateRealisticDeciduous(GameObject tree, TreeGenerationParams treeParams)
        {
            CreateDeciduousStructure(tree, treeParams);
        }
        
        void CreateDeciduousStructure(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建较粗的主干
            var trunkMesh = CreateTrunkMesh(treeParams.height * 0.7f, treeParams.trunkRadius * 1.2f, 12, 16);
            var trunkObj = CreateMeshObject(tree, "Trunk", trunkMesh, GetOakBarkMaterial());
            
            // 创建主要分支
            CreateMainBranches(tree, treeParams);
            
            // 创建叶冠
            if (treeParams.hasLeaves)
            {
                CreateLeafCanopy(tree, treeParams);
            }
        }
        
        void CreateMainBranches(GameObject tree, TreeGenerationParams treeParams)
        {
            int mainBranches = Random.Range(4, 8);
            float branchStartHeight = treeParams.height * 0.6f;
            
            for (int i = 0; i < mainBranches; i++)
            {
                float angle = (360f / mainBranches) * i + Random.Range(-30f, 30f);
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(20f, 45f)) * Vector3.forward;
                
                CreateBranch(tree, branchStartHeight, direction, treeParams.trunkRadius * 0.3f, treeParams.height * 0.4f);
            }
        }
        
        void CreateBranch(GameObject parent, float startHeight, Vector3 direction, float radius, float length)
        {
            var branchMesh = CreateBranchMesh(radius, length);
            var branchObj = CreateMeshObject(parent, "Branch", branchMesh, GetBranchMaterial());
            branchObj.transform.localPosition = new Vector3(0, startHeight, 0);
            branchObj.transform.localRotation = Quaternion.LookRotation(direction);
        }
        
        void CreateLeafCanopy(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建球形叶冠
            float canopyRadius = treeParams.trunkRadius * 4f;
            var canopyMesh = CreateSphericalCanopy(canopyRadius);
            var canopyObj = CreateMeshObject(tree, "Canopy", canopyMesh, GetLeafMaterial(treeParams.foliageColor));
            canopyObj.transform.localPosition = new Vector3(0, treeParams.height * 0.75f, 0);
        }
        
        Mesh CreateBranchMesh(float radius, float length)
        {
            // 简化的分支网格创建
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius * 0.3f, length, Mathf.Sin(angle) * radius * 0.3f));
            }
            
            // 创建三角面（简化）
            for (int i = 0; i < segments; i++)
            {
                int i0 = i * 2;
                int i1 = (i + 1) * 2 % (segments * 2);
                
                triangles.AddRange(new[] { i0, i1, i0 + 1, i1, i1 + 1, i0 + 1 });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        Mesh CreateSphericalCanopy(float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int rings = 8;
            int sectors = 12;
            
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s <= sectors; s++)
                {
                    float theta = 2 * Mathf.PI * s / sectors;
                    
                    float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                    float y = radius * Mathf.Cos(phi);
                    float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
                    
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            
            // 创建三角面
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    int current = r * (sectors + 1) + s;
                    int next = current + sectors + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        GameObject CreateMeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            return obj;
        }
        
        Material GetOakBarkMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.5f, 0.35f, 0.25f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            return material;
        }
        
        Material GetBranchMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.3f, 0.22f, 0.15f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.15f);
            return material;
        }
        
        Material GetLeafMaterial(Color foliageColor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", foliageColor);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.3f);
            material.SetFloat("_Cull", 0); // 双面渲染
            return material;
        }
        
        Mesh CreateTrunkMesh(float height, float radius, int segments, int heightSegments)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float y = height * h / heightSegments;
                float currentRadius = radius * (1f + Mathf.Sin(y * 0.5f) * 0.1f);
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * currentRadius,
                        y,
                        Mathf.Sin(angle) * currentRadius
                    ));
                }
            }
            
            for (int h = 0; h < heightSegments; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
    }

    /// <summary>
    /// 高级棕榈树生成器
    /// </summary>
    public class AdvancedPalmGenerator
    {
        public void GenerateRealisticPalm(GameObject tree, TreeGenerationParams treeParams)
        {
            CreatePalmStructure(tree, treeParams);
        }
        
        void CreatePalmStructure(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建略弯曲的棕榈树干
            var trunkMesh = CreateCurvedTrunk(treeParams.height, treeParams.trunkRadius);
            var trunkObj = CreateMeshObject(tree, "Trunk", trunkMesh, GetPalmBarkMaterial());
            
            // 创建棕榈叶
            if (treeParams.hasLeaves)
            {
                CreatePalmFronds(tree, treeParams);
            }
        }
        
        Mesh CreateCurvedTrunk(float height, float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 12;
            int heightSegments = 20;
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float t = (float)h / heightSegments;
                float y = height * t;
                
                // 添加轻微弯曲
                float bendOffset = Mathf.Sin(t * Mathf.PI * 0.5f) * radius * 0.3f;
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments;
                    float currentRadius = radius * (1f - t * 0.3f); // 向上逐渐变细
                    
                    float x = Mathf.Cos(angle) * currentRadius + bendOffset;
                    float z = Mathf.Sin(angle) * currentRadius;
                    
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            
            // 创建三角面
            for (int h = 0; h < heightSegments; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        void CreatePalmFronds(GameObject tree, TreeGenerationParams treeParams)
        {
            int frondCount = Random.Range(6, 12);
            
            for (int i = 0; i < frondCount; i++)
            {
                float angle = (360f / frondCount) * i + Random.Range(-15f, 15f);
                var frondMesh = CreatePalmFrond();
                var frondObj = CreateMeshObject(tree, $"Frond_{i}", frondMesh, GetFrondMaterial(treeParams.foliageColor));
                
                frondObj.transform.localPosition = new Vector3(0, treeParams.height * 0.9f, 0);
                frondObj.transform.localRotation = Quaternion.Euler(Random.Range(-20f, 20f), angle, 0);
            }
        }
        
        Mesh CreatePalmFrond()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 10;
            float length = 3f;
            float maxWidth = 0.5f;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float y = length * t;
                float width = maxWidth * Mathf.Sin(t * Mathf.PI); // 中间宽，两端窄
                
                vertices.Add(new Vector3(-width, y, 0));
                vertices.Add(new Vector3(width, y, 0));
            }
            
            // 创建三角面
            for (int i = 0; i < segments; i++)
            {
                int i0 = i * 2;
                int i1 = (i + 1) * 2;
                
                triangles.AddRange(new[] { i0, i1, i0 + 1 });
                triangles.AddRange(new[] { i0 + 1, i1, i1 + 1 });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        GameObject CreateMeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            return obj;
        }
        
        Material GetPalmBarkMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.6f, 0.5f, 0.3f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.2f);
            return material;
        }
        
        Material GetFrondMaterial(Color foliageColor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", foliageColor);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.4f);
            material.SetFloat("_Cull", 0); // 双面渲染
            return material;
        }
    }

    /// <summary>
    /// 高级果树生成器
    /// </summary>
    public class AdvancedFruitTreeGenerator
    {
        public void GenerateRealisticFruitTree(GameObject tree, TreeGenerationParams treeParams)
        {
            CreateFruitTreeStructure(tree, treeParams);
        }
        
        void CreateFruitTreeStructure(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建相对较短的主干
            var trunkMesh = CreateTrunkMesh(
                treeParams.height * 0.6f, treeParams.trunkRadius, 12, 12);
            var trunkObj = CreateMeshObject(tree, "Trunk", trunkMesh, GetFruitTreeBarkMaterial());
            
            // 创建分支系统
            CreateFruitTreeBranches(tree, treeParams);
            
            // 添加叶子和果实
            if (treeParams.hasLeaves)
            {
                CreateFoliageAndFruit(tree, treeParams);
            }
        }
        
        void CreateFruitTreeBranches(GameObject tree, TreeGenerationParams treeParams)
        {
            int mainBranches = Random.Range(5, 9);
            float branchStartHeight = treeParams.height * 0.4f;
            
            for (int i = 0; i < mainBranches; i++)
            {
                float angle = (360f / mainBranches) * i;
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(30f, 60f)) * Vector3.forward;
                
                CreateFruitBranch(tree, branchStartHeight, direction, treeParams.trunkRadius * 0.25f, treeParams.height * 0.3f);
            }
        }
        
        void CreateFruitBranch(GameObject parent, float startHeight, Vector3 direction, float radius, float length)
        {
            // 创建主分支
            var branchMesh = CreateTaperedBranch(radius, length);
            var branchObj = CreateMeshObject(parent, "FruitBranch", branchMesh, GetBranchMaterial());
            branchObj.transform.localPosition = new Vector3(0, startHeight, 0);
            branchObj.transform.localRotation = Quaternion.LookRotation(direction);
            
            // 添加小分支
            CreateSmallBranches(branchObj, length, radius * 0.5f);
        }
        
        void CreateSmallBranches(GameObject parentBranch, float parentLength, float radius)
        {
            int smallBranchCount = Random.Range(3, 6);
            
            for (int i = 0; i < smallBranchCount; i++)
            {
                float position = Random.Range(0.3f, 0.9f) * parentLength;
                Vector3 direction = Quaternion.Euler(
                    Random.Range(-45f, 45f), 
                    Random.Range(-90f, 90f), 
                    Random.Range(20f, 45f)) * Vector3.forward;
                
                var smallBranchMesh = CreateTaperedBranch(radius, parentLength * 0.4f);
                var smallBranchObj = CreateMeshObject(parentBranch, "SmallBranch", smallBranchMesh, GetBranchMaterial());
                smallBranchObj.transform.localPosition = new Vector3(0, 0, position);
                smallBranchObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }
        
        void CreateFoliageAndFruit(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建叶子簇
            CreateLeafClusters(tree, treeParams);
            
            // 添加果实
            CreateFruits(tree, treeParams);
        }
        
        void CreateLeafClusters(GameObject tree, TreeGenerationParams treeParams)
        {
            int clusterCount = Random.Range(15, 25);
            
            for (int i = 0; i < clusterCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-treeParams.trunkRadius * 2f, treeParams.trunkRadius * 2f),
                    Random.Range(treeParams.height * 0.4f, treeParams.height * 0.9f),
                    Random.Range(-treeParams.trunkRadius * 2f, treeParams.trunkRadius * 2f)
                );
                
                var leafMesh = CreateLeafCluster();
                var leafObj = CreateMeshObject(tree, "LeafCluster", leafMesh, GetLeafMaterial(treeParams.foliageColor));
                leafObj.transform.localPosition = position;
                leafObj.transform.localRotation = Random.rotation;
            }
        }
        
        void CreateFruits(GameObject tree, TreeGenerationParams treeParams)
        {
            int fruitCount = Random.Range(8, 15);
            
            for (int i = 0; i < fruitCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-treeParams.trunkRadius * 1.5f, treeParams.trunkRadius * 1.5f),
                    Random.Range(treeParams.height * 0.5f, treeParams.height * 0.8f),
                    Random.Range(-treeParams.trunkRadius * 1.5f, treeParams.trunkRadius * 1.5f)
                );
                
                var fruitMesh = CreateApple();
                var fruitObj = CreateMeshObject(tree, "Fruit", fruitMesh, GetFruitMaterial());
                fruitObj.transform.localPosition = position;
                fruitObj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
            }
        }
        
        Mesh CreateTaperedBranch(float radius, float length)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 6;
            
            for (int h = 0; h <= 5; h++)
            {
                float t = (float)h / 5;
                float currentRadius = radius * (1f - t * 0.7f);
                float y = length * t;
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * currentRadius,
                        y,
                        Mathf.Sin(angle) * currentRadius
                    ));
                }
            }
            
            // 创建三角面
            for (int h = 0; h < 5; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        Mesh CreateLeafCluster()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int leafCount = 8;
            
            for (int i = 0; i < leafCount; i++)
            {
                float angle = (360f / leafCount) * i;
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(-30f, 30f)) * Vector3.forward * 0.3f;
                
                // 简化的叶子形状
                vertices.AddRange(new Vector3[] {
                    direction,
                    direction + Vector3.up * 0.2f + Vector3.right * 0.1f,
                    direction + Vector3.up * 0.2f + Vector3.left * 0.1f
                });
                
                int baseIndex = i * 3;
                triangles.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2 });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        Mesh CreateApple()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int rings = 6;
            int sectors = 8;
            float radius = 0.08f;
            
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s <= sectors; s++)
                {
                    float theta = 2 * Mathf.PI * s / sectors;
                    
                    float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                    float y = radius * Mathf.Cos(phi) * 0.8f; // 稍微压扁
                    float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
                    
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            
            // 创建三角面
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    int current = r * (sectors + 1) + s;
                    int next = current + sectors + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        GameObject CreateMeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            return obj;
        }
        
        Material GetFruitTreeBarkMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.45f, 0.35f, 0.25f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.15f);
            return material;
        }
        
        Material GetBranchMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.35f, 0.25f, 0.15f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.2f);
            return material;
        }
        
        Material GetLeafMaterial(Color foliageColor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", foliageColor);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.3f);
            material.SetFloat("_Cull", 0);
            return material;
        }
        
        Material GetFruitMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.8f, 0.2f, 0.2f)); // 红苹果
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.7f);
            return material;
        }
        
        Mesh CreateTrunkMesh(float height, float radius, int segments, int heightSegments)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float y = height * h / heightSegments;
                float currentRadius = radius * (1f + Mathf.Sin(y * 0.5f) * 0.1f);
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * currentRadius,
                        y,
                        Mathf.Sin(angle) * currentRadius
                    ));
                }
            }
            
            for (int h = 0; h < heightSegments; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
    }

    /// <summary>
    /// 高级枯树生成器
    /// </summary>
    public class AdvancedDeadTreeGenerator
    {
        public void GenerateRealisticDeadTree(GameObject tree, TreeGenerationParams treeParams)
        {
            CreateDeadTreeStructure(tree, treeParams);
        }
        
        void CreateDeadTreeStructure(GameObject tree, TreeGenerationParams treeParams)
        {
            // 创建扭曲的枯树干
            var trunkMesh = CreateTwistedDeadTrunk(treeParams.height, treeParams.trunkRadius);
            var trunkObj = CreateMeshObject(tree, "DeadTrunk", trunkMesh, GetDeadBarkMaterial());
            
            // 创建断裂的分支
            CreateBrokenBranches(tree, treeParams);
        }
        
        Mesh CreateTwistedDeadTrunk(float height, float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            int heightSegments = 15;
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float t = (float)h / heightSegments;
                float y = height * t;
                
                // 添加扭曲效果
                float twist = t * 45f * Mathf.Deg2Rad;
                float currentRadius = radius * (1f - t * 0.4f); // 向上变细
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments + twist;
                    
                    // 添加不规则形状
                    float irregularity = Mathf.PerlinNoise(s * 0.5f, h * 0.3f) * 0.3f + 0.7f;
                    float finalRadius = currentRadius * irregularity;
                    
                    float x = Mathf.Cos(angle) * finalRadius;
                    float z = Mathf.Sin(angle) * finalRadius;
                    
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            
            // 创建三角面
            for (int h = 0; h < heightSegments; h++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = h * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        void CreateBrokenBranches(GameObject tree, TreeGenerationParams treeParams)
        {
            int branchCount = Random.Range(3, 7);
            
            for (int i = 0; i < branchCount; i++)
            {
                float height = Random.Range(treeParams.height * 0.3f, treeParams.height * 0.8f);
                float angle = Random.Range(0f, 360f);
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(20f, 70f)) * Vector3.forward;
                
                // 创建断裂的分支
                float branchLength = Random.Range(treeParams.height * 0.15f, treeParams.height * 0.4f);
                CreateBrokenBranch(tree, height, direction, treeParams.trunkRadius * 0.2f, branchLength);
            }
        }
        
        void CreateBrokenBranch(GameObject parent, float startHeight, Vector3 direction, float radius, float length)
        {
            var branchMesh = CreateIrregularBranch(radius, length);
            var branchObj = CreateMeshObject(parent, "BrokenBranch", branchMesh, GetDeadBranchMaterial());
            branchObj.transform.localPosition = new Vector3(0, startHeight, 0);
            branchObj.transform.localRotation = Quaternion.LookRotation(direction);
        }
        
        Mesh CreateIrregularBranch(float radius, float length)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 6;
            int lengthSegments = 8;
            
            for (int l = 0; l <= lengthSegments; l++)
            {
                float t = (float)l / lengthSegments;
                float y = length * t;
                
                // 不规则的半径变化
                float currentRadius = radius * (1f - t * 0.8f) * (0.5f + Mathf.PerlinNoise(l * 0.5f, 0) * 0.5f);
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * currentRadius,
                        y,
                        Mathf.Sin(angle) * currentRadius
                    ));
                }
            }
            
            // 创建三角面
            for (int l = 0; l < lengthSegments; l++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int current = l * (segments + 1) + s;
                    int next = current + segments + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        GameObject CreateMeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            return obj;
        }
        
        Material GetDeadBarkMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.35f, 0.25f, 0.15f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.05f);
            return material;
        }
        
        Material GetDeadBranchMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.4f, 0.3f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            return material;
        }
    }
}