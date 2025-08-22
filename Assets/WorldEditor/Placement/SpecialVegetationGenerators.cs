using UnityEngine;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 特殊植物生成器
    /// </summary>
    public static class SpecialVegetationGenerators
    {
        /// <summary>
        /// 创建仙人掌
        /// </summary>
        public static void CreateAdvancedCactus(GameObject cactus, SpecialPlantParams specialParams)
        {
            CreateCactusStructure(cactus, specialParams);
        }
        
        /// <summary>
        /// 创建蘑菇
        /// </summary>
        public static void CreateAdvancedMushroom(GameObject mushroom, SpecialPlantParams specialParams)
        {
            CreateMushroomStructure(mushroom, specialParams);
        }
        
        /// <summary>
        /// 创建藤蔓
        /// </summary>
        public static void CreateAdvancedVine(GameObject vine, SpecialPlantParams specialParams)
        {
            CreateVineStructure(vine, specialParams);
        }
        
        /// <summary>
        /// 创建水草
        /// </summary>
        public static void CreateAdvancedAquaticPlant(GameObject aquatic, SpecialPlantParams specialParams)
        {
            CreateAquaticStructure(aquatic, specialParams);
        }
        
        #region 仙人掌生成
        
        /// <summary>
        /// 创建仙人掌结构
        /// </summary>
        static void CreateCactusStructure(GameObject cactus, SpecialPlantParams specialParams)
        {
            // 创建主茎
            var mainStem = CreateCactusSegment(specialParams.height, 0.1f);
            var mainStemObj = CreateMeshObject(cactus, "MainStem", mainStem, GetCactusMaterial());
            
            // 创建侧枝
            CreateCactusBranches(cactus, specialParams);
            
            // 添加刺
            if (specialParams.hasSpecialFeatures)
            {
                CreateCactusSpines(cactus, specialParams);
            }
        }
        
        static void CreateCactusBranches(GameObject parent, SpecialPlantParams specialParams)
        {
            int branchCount = Random.Range(2, 5);
            
            for (int i = 0; i < branchCount; i++)
            {
                float branchHeight = specialParams.height * Random.Range(0.3f, 0.8f);
                float angle = Random.Range(0f, 360f);
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(30f, 60f)) * Vector3.forward;
                
                var branchMesh = CreateCactusSegment(branchHeight * 0.6f, 0.06f);
                var branchObj = CreateMeshObject(parent, "CactusBranch", branchMesh, GetCactusMaterial());
                
                branchObj.transform.localPosition = new Vector3(0, branchHeight, 0);
                branchObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }
        
        static void CreateCactusSpines(GameObject parent, SpecialPlantParams specialParams)
        {
            int spineCount = Random.Range(20, 40);
            
            for (int i = 0; i < spineCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-0.08f, 0.08f),
                    Random.Range(0.1f, specialParams.height * 0.9f),
                    Random.Range(-0.08f, 0.08f)
                );
                
                var spineMesh = CreateCactusSpine();
                var spineObj = CreateMeshObject(parent, "Spine", spineMesh, GetSpineMaterial());
                spineObj.transform.localPosition = position;
                spineObj.transform.localRotation = Random.rotation;
            }
        }
        
        static Mesh CreateCactusSegment(float height, float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            int heightSegments = 12;
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float t = (float)h / heightSegments;
                float y = height * t;
                float currentRadius = radius * (1f + Mathf.Sin(t * Mathf.PI * 4) * 0.1f); // 波浪形
                
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
        
        static Mesh CreateCactusSpine()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.001f, 0.02f, 0),
                new Vector3(-0.001f, 0.02f, 0),
                new Vector3(0, 0.02f, 0.001f),
                new Vector3(0, 0.02f, -0.001f)
            };
            
            var triangles = new int[]
            {
                0, 1, 2,
                0, 2, 4,
                0, 4, 3,
                0, 3, 1,
                1, 3, 2,
                2, 3, 4
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
        
        #endregion
        
        #region 蘑菇生成
        
        /// <summary>
        /// 创建蘑菇结构
        /// </summary>
        static void CreateMushroomStructure(GameObject mushroom, SpecialPlantParams specialParams)
        {
            // 创建蘑菇茎
            var stemMesh = CreateMushroomStem(specialParams.height * 0.7f, 0.02f);
            var stemObj = CreateMeshObject(mushroom, "MushroomStem", stemMesh, GetMushroomStemMaterial());
            
            // 创建蘑菇帽
            var capMesh = CreateMushroomCap(specialParams.width * 0.5f);
            var capObj = CreateMeshObject(mushroom, "MushroomCap", capMesh, GetMushroomCapMaterial());
            capObj.transform.localPosition = new Vector3(0, specialParams.height * 0.7f, 0);
            
            // 如果有特殊特征，添加斑点
            if (specialParams.hasSpecialFeatures)
            {
                CreateMushroomSpots(capObj, specialParams.width * 0.5f);
            }
        }
        
        static void CreateMushroomSpots(GameObject cap, float capRadius)
        {
            int spotCount = Random.Range(3, 8);
            
            for (int i = 0; i < spotCount; i++)
            {
                Vector2 randomPos = Random.insideUnitCircle * capRadius * 0.7f;
                Vector3 position = new Vector3(randomPos.x, 0.02f, randomPos.y);
                
                var spotMesh = CreateMushroomSpot();
                var spotObj = CreateMeshObject(cap, "Spot", spotMesh, GetSpotMaterial());
                spotObj.transform.localPosition = position;
                spotObj.transform.localScale = Vector3.one * Random.Range(0.5f, 1.2f);
            }
        }
        
        static Mesh CreateMushroomStem(float height, float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            int heightSegments = 6;
            
            for (int h = 0; h <= heightSegments; h++)
            {
                float t = (float)h / heightSegments;
                float y = height * t;
                float currentRadius = radius * (1f - t * 0.2f + Mathf.Sin(t * Mathf.PI * 2) * 0.1f);
                
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
        
        static Mesh CreateMushroomCap(float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int rings = 6;
            int sectors = 12;
            
            // 顶点
            vertices.Add(new Vector3(0, radius * 0.3f, 0));
            
            for (int r = 1; r <= rings; r++)
            {
                float phi = Mathf.PI * 0.5f * r / rings;
                for (int s = 0; s <= sectors; s++)
                {
                    float theta = 2 * Mathf.PI * s / sectors;
                    
                    float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                    float y = radius * 0.3f * Mathf.Cos(phi);
                    float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
                    
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            
            // 创建三角面
            for (int s = 0; s < sectors; s++)
            {
                triangles.AddRange(new[] { 0, s + 1, ((s + 1) % sectors) + 1 });
            }
            
            for (int r = 1; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    int current = (r - 1) * (sectors + 1) + s + 1;
                    int next = r * (sectors + 1) + s + 1;
                    
                    triangles.AddRange(new[] { current, next, current + 1 });
                    triangles.AddRange(new[] { current + 1, next, next + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        static Mesh CreateMushroomSpot()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                new Vector3(-0.01f, 0, -0.01f),
                new Vector3(0.01f, 0, -0.01f),
                new Vector3(0.01f, 0, 0.01f),
                new Vector3(-0.01f, 0, 0.01f)
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
        
        #region 藤蔓生成
        
        /// <summary>
        /// 创建藤蔓结构
        /// </summary>
        static void CreateVineStructure(GameObject vine, SpecialPlantParams specialParams)
        {
            // 创建主藤蔓
            var mainVineMesh = CreateVineSegment(specialParams.height, 0.02f);
            var mainVineObj = CreateMeshObject(vine, "MainVine", mainVineMesh, GetVineMaterial());
            
            // 创建侧藤
            CreateVineBranches(vine, specialParams);
            
            // 添加叶子
            CreateVineLeaves(vine, specialParams);
        }
        
        static void CreateVineBranches(GameObject parent, SpecialPlantParams specialParams)
        {
            int branchCount = Random.Range(3, 8);
            
            for (int i = 0; i < branchCount; i++)
            {
                float branchPosition = specialParams.height * Random.Range(0.2f, 0.8f);
                float angle = Random.Range(0f, 360f);
                Vector3 direction = Quaternion.Euler(0, angle, Random.Range(20f, 45f)) * Vector3.forward;
                
                var branchMesh = CreateVineSegment(specialParams.height * 0.4f, 0.015f);
                var branchObj = CreateMeshObject(parent, "VineBranch", branchMesh, GetVineMaterial());
                
                branchObj.transform.localPosition = new Vector3(0, branchPosition, 0);
                branchObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }
        
        static void CreateVineLeaves(GameObject parent, SpecialPlantParams specialParams)
        {
            int leafCount = Random.Range(10, 20);
            
            for (int i = 0; i < leafCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(0.1f, specialParams.height * 0.9f),
                    Random.Range(-0.3f, 0.3f)
                );
                
                var leafMesh = CreateVineLeaf();
                var leafObj = CreateMeshObject(parent, "VineLeaf", leafMesh, GetVineLeafMaterial());
                leafObj.transform.localPosition = position;
                leafObj.transform.localRotation = Random.rotation;
            }
        }
        
        static Mesh CreateVineSegment(float length, float radius)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 6;
            int lengthSegments = 12;
            
            for (int l = 0; l <= lengthSegments; l++)
            {
                float t = (float)l / lengthSegments;
                float y = length * t;
                
                // 添加扭曲效果
                float twist = t * Mathf.PI * 2;
                float currentRadius = radius * (1f + Mathf.Sin(t * Mathf.PI * 4) * 0.2f);
                
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2 * Mathf.PI * s / segments + twist;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * currentRadius,
                        y,
                        Mathf.Sin(angle) * currentRadius
                    ));
                }
            }
            
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
        
        static Mesh CreateVineLeaf()
        {
            var mesh = new Mesh();
            var vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.03f, 0.05f, 0),
                new Vector3(-0.03f, 0.05f, 0),
                new Vector3(0.02f, 0.08f, 0),
                new Vector3(-0.02f, 0.08f, 0),
                new Vector3(0, 0.1f, 0)
            };
            
            var triangles = new int[]
            {
                0, 1, 2,
                1, 3, 2,
                2, 3, 4,
                3, 5, 4
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
        
        #endregion
        
        #region 水草生成
        
        /// <summary>
        /// 创建水草结构
        /// </summary>
        static void CreateAquaticStructure(GameObject aquatic, SpecialPlantParams specialParams)
        {
            // 创建多根水草
            int grassCount = Random.Range(5, 12);
            
            for (int i = 0; i < grassCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    0,
                    Random.Range(-0.2f, 0.2f)
                );
                
                CreateSingleAquaticPlant(aquatic, position, specialParams);
            }
        }
        
        static void CreateSingleAquaticPlant(GameObject parent, Vector3 position, SpecialPlantParams specialParams)
        {
            var aquaticMesh = CreateAquaticBlade(specialParams.height);
            var aquaticObj = CreateMeshObject(parent, "AquaticBlade", aquaticMesh, GetAquaticMaterial());
            
            aquaticObj.transform.localPosition = position;
            aquaticObj.transform.localRotation = Quaternion.Euler(
                Random.Range(-20f, 20f),
                Random.Range(0f, 360f),
                Random.Range(-30f, 30f)
            );
        }
        
        static Mesh CreateAquaticBlade(float height)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            int segments = 8;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float y = height * t;
                float width = 0.02f * (1f - t * 0.5f) * Mathf.Sin(t * Mathf.PI);
                
                // 添加波浪效果
                float wave = Mathf.Sin(t * Mathf.PI * 3) * 0.01f;
                
                vertices.Add(new Vector3(-width + wave, y, 0));
                vertices.Add(new Vector3(width + wave, y, 0));
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
        
        #endregion
        
        #region 辅助方法
        
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
        
        #endregion
        
        #region 材质创建
        
        static Material GetCactusMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.2f, 0.4f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.3f);
            return material;
        }
        
        static Material GetSpineMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.6f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            return material;
        }
        
        static Material GetMushroomStemMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.8f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.2f);
            return material;
        }
        
        static Material GetMushroomCapMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.6f, 0.3f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.4f);
            return material;
        }
        
        static Material GetSpotMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.6f);
            return material;
        }
        
        static Material GetVineMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.3f);
            return material;
        }
        
        static Material GetVineLeafMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.3f, 0.6f, 0.2f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.4f);
            material.SetFloat("_Cull", 0); // 双面渲染
            return material;
        }
        
        static Material GetAquaticMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.2f, 0.5f, 0.3f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.6f);
            material.SetFloat("_Cull", 0); // 双面渲染
            
            // 添加一些透明度效果
            material.SetFloat("_Surface", 1); // Transparent
            Color baseColor = material.GetColor("_BaseColor");
            baseColor.a = 0.8f;
            material.SetColor("_BaseColor", baseColor);
            
            return material;
        }
        
        #endregion
    }
}