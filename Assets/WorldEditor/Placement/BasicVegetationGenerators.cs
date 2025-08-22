using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 基础灌木和草本植物生成器
    /// </summary>
    public static class BasicVegetationGenerators
    {
        /// <summary>
        /// 创建普通灌木
        /// </summary>
        public static void CreateAdvancedCommonBush(GameObject bush, BushGenerationParams bushParams)
        {
            CreateBasicBush(bush, bushParams, false, false);
        }
        
        /// <summary>
        /// 创建浆果灌木
        /// </summary>
        public static void CreateAdvancedBerryBush(GameObject bush, BushGenerationParams bushParams)
        {
            CreateBasicBush(bush, bushParams, true, false);
        }
        
        /// <summary>
        /// 创建荆棘丛
        /// </summary>
        public static void CreateAdvancedThornBush(GameObject bush, BushGenerationParams bushParams)
        {
            CreateBasicBush(bush, bushParams, false, true);
        }
        
        /// <summary>
        /// 创建竹子
        /// </summary>
        public static void CreateAdvancedBamboo(GameObject bush, BushGenerationParams bushParams)
        {
            CreateBambooCluster(bush, bushParams);
        }
        
        /// <summary>
        /// 创建基础灌木结构
        /// </summary>
        static void CreateBasicBush(GameObject bush, BushGenerationParams bushParams, bool hasBerries, bool hasThorns)
        {
            // 创建主要分支
            for (int i = 0; i < bushParams.branchCount; i++)
            {
                float angle = (360f / bushParams.branchCount) * i + Random.Range(-30f, 30f);
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(20f, 50f)) * Vector3.forward;
                
                CreateBushBranch(bush, direction, bushParams.height, bushParams.width, hasThorns);
            }
            
            // 添加叶子
            CreateBushFoliage(bush, bushParams);
            
            // 添加浆果（如果需要）
            if (hasBerries)
            {
                CreateBerries(bush, bushParams);
            }
        }
        
        /// <summary>
        /// 创建灌木分支
        /// </summary>
        static void CreateBushBranch(GameObject parent, Vector3 direction, float height, float width, bool hasThorns)
        {
            var branchMesh = CreateSimpleBranch(height * 0.6f, 0.02f);
            var branchObj = CreateMeshObject(parent, "BushBranch", branchMesh, GetBushBranchMaterial(hasThorns));
            
            branchObj.transform.localRotation = Quaternion.LookRotation(direction);
            branchObj.transform.localPosition = Vector3.zero;
        }
        
        /// <summary>
        /// 创建灌木叶子
        /// </summary>
        static void CreateBushFoliage(GameObject bush, BushGenerationParams bushParams)
        {
            int leafClusterCount = bushParams.branchCount * 2;
            
            for (int i = 0; i < leafClusterCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-bushParams.width * 0.5f, bushParams.width * 0.5f),
                    Random.Range(bushParams.height * 0.2f, bushParams.height * 0.8f),
                    Random.Range(-bushParams.width * 0.5f, bushParams.width * 0.5f)
                );
                
                var leafMesh = CreateBushLeafCluster();
                var leafObj = CreateMeshObject(bush, "BushLeaves", leafMesh, GetBushLeafMaterial(bushParams.foliageColor));
                leafObj.transform.localPosition = position;
                leafObj.transform.localRotation = Random.rotation;
            }
        }
        
        /// <summary>
        /// 创建浆果
        /// </summary>
        static void CreateBerries(GameObject bush, BushGenerationParams bushParams)
        {
            int berryCount = Random.Range(5, 15);
            
            for (int i = 0; i < berryCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-bushParams.width * 0.4f, bushParams.width * 0.4f),
                    Random.Range(bushParams.height * 0.3f, bushParams.height * 0.7f),
                    Random.Range(-bushParams.width * 0.4f, bushParams.width * 0.4f)
                );
                
                var berryMesh = CreateBerry();
                var berryObj = CreateMeshObject(bush, "Berry", berryMesh, GetBerryMaterial());
                berryObj.transform.localPosition = position;
                berryObj.transform.localScale = Vector3.one * Random.Range(0.5f, 1f);
            }
        }
        
        /// <summary>
        /// 创建竹子集群
        /// </summary>
        static void CreateBambooCluster(GameObject bamboo, BushGenerationParams bushParams)
        {
            int bambooCount = Random.Range(3, 8);
            
            for (int i = 0; i < bambooCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-bushParams.width * 0.3f, bushParams.width * 0.3f),
                    0,
                    Random.Range(-bushParams.width * 0.3f, bushParams.width * 0.3f)
                );
                
                float individualHeight = bushParams.height * Random.Range(0.7f, 1.3f);
                CreateSingleBamboo(bamboo, position, individualHeight);
            }
        }
        
        /// <summary>
        /// 创建单根竹子
        /// </summary>
        static void CreateSingleBamboo(GameObject parent, Vector3 position, float height)
        {
            var bambooMesh = CreateBambooSegments(height);
            var bambooObj = CreateMeshObject(parent, "BambooStalk", bambooMesh, GetBambootMaterial());
            bambooObj.transform.localPosition = position;
            
            // 添加竹叶
            CreateBambooLeaves(bambooObj, height);
        }
        
        /// <summary>
        /// 创建竹子叶子
        /// </summary>
        static void CreateBambooLeaves(GameObject bambooStalk, float height)
        {
            int leafCount = Random.Range(4, 8);
            
            for (int i = 0; i < leafCount; i++)
            {
                float leafHeight = height * Random.Range(0.6f, 0.9f);
                Vector3 direction = Quaternion.Euler(0, Random.Range(0f, 360f), Random.Range(-30f, 30f)) * Vector3.forward;
                
                var leafMesh = CreateBambooLeaf();
                var leafObj = CreateMeshObject(bambooStalk, "BambooLeaf", leafMesh, GetBambooLeafMaterial());
                leafObj.transform.localPosition = new Vector3(0, leafHeight, 0);
                leafObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }
        
        /// <summary>
        /// 草本植物生成器
        /// </summary>
        public static void CreateAdvancedWildGrass(GameObject grass, GrassGenerationParams grassParams)
        {
            CreateGrassCluster(grass, grassParams, false);
        }
        
        public static void CreateAdvancedFlowers(GameObject grass, GrassGenerationParams grassParams)
        {
            CreateGrassCluster(grass, grassParams, true);
        }
        
        public static void CreateAdvancedFerns(GameObject grass, GrassGenerationParams grassParams)
        {
            CreateFernCluster(grass, grassParams);
        }
        
        public static void CreateAdvancedMoss(GameObject grass, GrassGenerationParams grassParams)
        {
            CreateMossCluster(grass, grassParams);
        }
        
        /// <summary>
        /// 创建草丛
        /// </summary>
        static void CreateGrassCluster(GameObject grass, GrassGenerationParams grassParams, bool hasFlowers)
        {
            for (int i = 0; i < grassParams.grassCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    0,
                    Random.Range(-0.5f, 0.5f)
                );
                
                var grassBlade = CreateGrassBlade(grassParams.height);
                var grassObj = CreateMeshObject(grass, "GrassBlade", grassBlade, GetGrassMaterial(grassParams.grassColor));
                grassObj.transform.localPosition = position;
                grassObj.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), Random.Range(-15f, 15f));
            }
            
            if (hasFlowers)
            {
                CreateWildFlowers(grass, grassParams);
            }
        }
        
        /// <summary>
        /// 创建野花
        /// </summary>
        static void CreateWildFlowers(GameObject parent, GrassGenerationParams grassParams)
        {
            int flowerCount = Random.Range(2, 8);
            
            for (int i = 0; i < flowerCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    grassParams.height * 0.7f,
                    Random.Range(-0.4f, 0.4f)
                );
                
                var flowerMesh = CreateSimpleFlower();
                var flowerObj = CreateMeshObject(parent, "Flower", flowerMesh, GetFlowerMaterial());
                flowerObj.transform.localPosition = position;
                flowerObj.transform.localScale = Vector3.one * Random.Range(0.5f, 1.2f);
            }
        }
        
        /// <summary>
        /// 创建蕨类植物
        /// </summary>
        static void CreateFernCluster(GameObject fern, GrassGenerationParams grassParams)
        {
            int fernCount = Random.Range(3, 8);
            
            for (int i = 0; i < fernCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    0,
                    Random.Range(-0.3f, 0.3f)
                );
                
                var fernMesh = CreateFernFrond();
                var fernObj = CreateMeshObject(fern, "FernFrond", fernMesh, GetFernMaterial());
                fernObj.transform.localPosition = position;
                fernObj.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
        }
        
        /// <summary>
        /// 创建苔藓
        /// </summary>
        static void CreateMossCluster(GameObject moss, GrassGenerationParams grassParams)
        {
            var mossMesh = CreateMossPatch();
            var mossObj = CreateMeshObject(moss, "MossPatch", mossMesh, GetMossMaterial());
            mossObj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
        }
        
        #region 网格创建辅助方法
        
        static Mesh CreateSimpleBranch(float length, float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 6;
            
            for (int h = 0; h <= 3; h++)
            {
                float t = (float)h / 3;
                float currentRadius = radius * (1f - t * 0.5f);
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
            
            for (int h = 0; h < 3; h++)
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
        
        static Mesh CreateBushLeafCluster()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int leafCount = 6;
            
            for (int i = 0; i < leafCount; i++)
            {
                float angle = (360f / leafCount) * i;
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(-20f, 20f)) * Vector3.forward * 0.15f;
                
                vertices.AddRange(new Vector3[] {
                    direction,
                    direction + Vector3.up * 0.1f + Vector3.right * 0.05f,
                    direction + Vector3.up * 0.1f + Vector3.left * 0.05f
                });
                
                int baseIndex = i * 3;
                triangles.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2 });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        static Mesh CreateBerry()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            float radius = 0.02f;
            int rings = 4;
            int sectors = 6;
            
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
        
        static Mesh CreateBambooSegments(float height)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            int heightSegments = 10;
            float radius = 0.03f;
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float t = (float)h / heightSegments;
                float y = height * t;
                float currentRadius = radius * (1f - t * 0.2f);
                
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
        
        static Mesh CreateBambooLeaf()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.05f, 0.1f, 0),
                new Vector3(-0.05f, 0.1f, 0),
                new Vector3(0, 0.2f, 0)
            };
            
            var triangles = new int[]
            {
                0, 1, 2,
                1, 3, 2
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
        
        static Mesh CreateGrassBlade(float height)
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                new Vector3(-0.01f, 0, 0),
                new Vector3(0.01f, 0, 0),
                new Vector3(-0.005f, height * 0.7f, 0),
                new Vector3(0.005f, height * 0.7f, 0),
                new Vector3(0, height, 0)
            };
            
            var triangles = new int[]
            {
                0, 2, 1,
                1, 2, 3,
                2, 4, 3
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
        
        static Mesh CreateSimpleFlower()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            // 花心
            vertices.Add(Vector3.zero);
            
            // 花瓣
            int petalCount = 5;
            for (int i = 0; i < petalCount; i++)
            {
                float angle = (360f / petalCount) * i * Mathf.Deg2Rad;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.03f, 0, Mathf.Sin(angle) * 0.03f));
            }
            
            // 创建三角面
            for (int i = 0; i < petalCount; i++)
            {
                triangles.AddRange(new[] { 0, i + 1, ((i + 1) % petalCount) + 1 });
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        static Mesh CreateFernFrond()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            float length = 0.3f;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float y = length * t;
                float width = 0.05f * Mathf.Sin(t * Mathf.PI);
                
                vertices.Add(new Vector3(-width, y, 0));
                vertices.Add(new Vector3(width, y, 0));
            }
            
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
        
        static Mesh CreateMossPatch()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                new Vector3(-0.1f, 0, -0.1f),
                new Vector3(0.1f, 0, -0.1f),
                new Vector3(0.1f, 0, 0.1f),
                new Vector3(-0.1f, 0, 0.1f)
            };
            
            var triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
        
        #endregion
        
        #region 材质创建辅助方法
        
        static GameObject CreateMeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            return obj;
        }
        
        static Material GetBushBranchMaterial(bool hasThorns)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", hasThorns ? new Color(0.3f, 0.2f, 0.1f) : new Color(0.4f, 0.3f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            return material;
        }
        
        static Material GetBushLeafMaterial(Color foliageColor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", foliageColor);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.3f);
            material.SetFloat("_Cull", 0);
            return material;
        }
        
        static Material GetBerryMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.6f, 0.1f, 0.4f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.8f);
            return material;
        }
        
        static Material GetBambootMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.5f, 0.7f, 0.3f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.4f);
            return material;
        }
        
        static Material GetBambooLeafMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.4f, 0.8f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.5f);
            material.SetFloat("_Cull", 0);
            return material;
        }
        
        static Material GetGrassMaterial(Color grassColor)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", grassColor);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_Cull", 0);
            return material;
        }
        
        static Material GetFlowerMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.9f, 0.6f, 0.8f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.6f);
            return material;
        }
        
        static Material GetFernMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.2f, 0.6f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.3f);
            material.SetFloat("_Cull", 0);
            return material;
        }
        
        static Material GetMossMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.1f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            return material;
        }
        
        #endregion
    }
}