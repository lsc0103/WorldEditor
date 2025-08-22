using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 高级摄影级树木生成系统
    /// 使用更复杂的算法接近真实感
    /// </summary>
    public class AdvancedTreeGeneration
    {
        /// <summary>
        /// 使用L-System算法生成更自然的分支结构
        /// </summary>
        public static GameObject CreatePhotorealisticSpruce(Vector3 position)
        {
            GameObject tree = new GameObject("PhotorealisticSpruce");
            tree.transform.position = position;
            
            // 1. L-System分支生成
            var lSystem = new LSystemBranching();
            var branchStructure = lSystem.GenerateSpruceStructure(iterations: 6);
            
            // 2. Space Colonization叶子分布
            var leafDistribution = SpaceColonization.DistributeNeedles(branchStructure);
            
            // 3. 高级材质系统
            var materials = CreatePhotorealisticMaterials();
            
            // 4. 微观细节添加
            AddMicroscopicDetails(tree, branchStructure);
            
            // 5. 环境交互模拟
            SimulateEnvironmentalEffects(tree);
            
            return tree;
        }
        
        /// <summary>
        /// L-System分支生成器
        /// </summary>
        public class LSystemBranching
        {
            private string axiom = "F";
            private Dictionary<char, string> rules = new Dictionary<char, string>
            {
                {'F', "F[+F]F[-F]F"},  // 主分支规则
                {'+', "+"},            // 向左转
                {'-', "-"},            // 向右转
                {'[', "["},            // 开始分支
                {']', "]"}             // 结束分支
            };
            
            public TreeStructure GenerateSpruceStructure(int iterations)
            {
                string current = axiom;
                
                // 迭代生成语法树
                for (int i = 0; i < iterations; i++)
                {
                    current = ExpandString(current);
                }
                
                // 解析为3D结构
                return ParseToTreeStructure(current);
            }
            
            private string ExpandString(string input)
            {
                var result = "";
                foreach (char c in input)
                {
                    if (rules.ContainsKey(c))
                        result += rules[c];
                    else
                        result += c;
                }
                return result;
            }
            
            private TreeStructure ParseToTreeStructure(string lString)
            {
                var structure = new TreeStructure();
                var stack = new Stack<BranchNode>();
                var currentNode = new BranchNode(Vector3.zero, Vector3.up);
                structure.root = currentNode;
                
                float angle = 25f; // 分支角度
                float length = 2f; // 分支长度
                
                foreach (char c in lString)
                {
                    switch (c)
                    {
                        case 'F':
                            // 前进并创建分支
                            var newPos = currentNode.position + currentNode.direction * length;
                            var newNode = new BranchNode(newPos, currentNode.direction);
                            currentNode.children.Add(newNode);
                            currentNode = newNode;
                            length *= 0.9f; // 分支逐渐变短
                            break;
                            
                        case '+':
                            // 向左旋转
                            currentNode.direction = Quaternion.AngleAxis(angle, Vector3.up) * currentNode.direction;
                            break;
                            
                        case '-':
                            // 向右旋转
                            currentNode.direction = Quaternion.AngleAxis(-angle, Vector3.up) * currentNode.direction;
                            break;
                            
                        case '[':
                            // 保存当前状态
                            stack.Push(currentNode);
                            break;
                            
                        case ']':
                            // 恢复状态
                            if (stack.Count > 0)
                                currentNode = stack.Pop();
                            break;
                    }
                }
                
                return structure;
            }
        }
        
        /// <summary>
        /// Space Colonization算法 - 模拟叶子向光生长
        /// </summary>
        public static class SpaceColonization
        {
            public static List<NeedleCluster> DistributeNeedles(TreeStructure tree)
            {
                var clusters = new List<NeedleCluster>();
                var attractors = GenerateLightAttractors(tree.GetBounds());
                
                // 模拟针叶向光生长
                foreach (var branch in tree.GetAllBranches())
                {
                    var nearbyAttractors = attractors.Where(a => 
                        Vector3.Distance(a, branch.position) < 2f).ToList();
                    
                    foreach (var attractor in nearbyAttractors)
                    {
                        var direction = (attractor - branch.position).normalized;
                        var cluster = new NeedleCluster
                        {
                            position = branch.position,
                            direction = direction,
                            density = CalculateLightExposure(branch.position, attractor)
                        };
                        clusters.Add(cluster);
                    }
                }
                
                return clusters;
            }
            
            private static List<Vector3> GenerateLightAttractors(Bounds bounds)
            {
                var attractors = new List<Vector3>();
                
                // 模拟主要光源（太阳）方向
                var sunDirection = new Vector3(0.3f, 1f, 0.3f).normalized;
                
                // 在树冠周围生成光源吸引点
                for (int i = 0; i < 100; i++)
                {
                    var randomPoint = bounds.center + Random.insideUnitSphere * bounds.size.magnitude;
                    randomPoint += sunDirection * 5f; // 偏向光源方向
                    attractors.Add(randomPoint);
                }
                
                return attractors;
            }
            
            private static float CalculateLightExposure(Vector3 position, Vector3 lightSource)
            {
                var distance = Vector3.Distance(position, lightSource);
                return Mathf.Exp(-distance / 10f); // 指数衰减
            }
        }
        
        /// <summary>
        /// 创建摄影级材质
        /// </summary>
        private static PhotorealisticMaterials CreatePhotorealisticMaterials()
        {
            return new PhotorealisticMaterials
            {
                bark = CreateAdvancedBarkMaterial(),
                needles = CreateAdvancedNeedleMaterial(),
                branches = CreateAdvancedBranchMaterial()
            };
        }
        
        private static Material CreateAdvancedBarkMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // 高级树皮特征
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetColor("_BaseColor", new Color(0.4f, 0.3f, 0.2f));
            
            // 程序化细节纹理
            var detailTexture = GenerateAdvancedBarkTexture();
            material.SetTexture("_BaseMap", detailTexture);
            
            // 法线贴图模拟深度
            var normalMap = GenerateBarkNormalMap();
            material.SetTexture("_BumpMap", normalMap);
            
            return material;
        }
        
        private static Texture2D GenerateAdvancedBarkTexture()
        {
            int size = 1024; // 高分辨率
            var texture = new Texture2D(size, size);
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 多层噪声合成
                    float noise1 = Mathf.PerlinNoise(x * 0.01f, y * 0.01f) * 0.5f;
                    float noise2 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.3f;
                    float noise3 = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.2f;
                    
                    // 纵向纹理（树皮特征）
                    float verticalPattern = Mathf.Sin(x * 0.02f) * 0.1f;
                    
                    float combined = noise1 + noise2 + noise3 + verticalPattern;
                    
                    // 真实树皮颜色变化
                    var color = new Color(
                        0.3f + combined * 0.3f,
                        0.2f + combined * 0.2f,
                        0.1f + combined * 0.1f
                    );
                    
                    texture.SetPixel(x, y, color);
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        private static Texture2D GenerateBarkNormalMap()
        {
            int size = 1024;
            var normalMap = new Texture2D(size, size);
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 计算高度梯度
                    float heightL = Mathf.PerlinNoise((x-1) * 0.02f, y * 0.02f);
                    float heightR = Mathf.PerlinNoise((x+1) * 0.02f, y * 0.02f);
                    float heightD = Mathf.PerlinNoise(x * 0.02f, (y-1) * 0.02f);
                    float heightU = Mathf.PerlinNoise(x * 0.02f, (y+1) * 0.02f);
                    
                    // 计算法线
                    Vector3 normal = new Vector3(
                        (heightL - heightR) * 2f,
                        (heightD - heightU) * 2f,
                        1f
                    ).normalized;
                    
                    // 转换为法线贴图格式
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f
                    );
                    
                    normalMap.SetPixel(x, y, normalColor);
                }
            }
            
            normalMap.Apply();
            return normalMap;
        }
        
        private static Material CreateAdvancedNeedleMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // 针叶的次表面散射效果
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.2f);
            material.SetColor("_BaseColor", new Color(0.08f, 0.35f, 0.12f));
            
            // 启用透明度和双面渲染
            material.SetFloat("_Surface", 1); // Transparent
            material.SetFloat("_Cull", 0); // No Culling
            
            return material;
        }
        
        private static Material CreateAdvancedBranchMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetColor("_BaseColor", new Color(0.25f, 0.18f, 0.12f));
            
            return material;
        }
        
        /// <summary>
        /// 添加微观细节
        /// </summary>
        private static void AddMicroscopicDetails(GameObject tree, TreeStructure structure)
        {
            // 添加地衣和苔藓
            AddLichensAndMoss(tree);
            
            // 添加昆虫咬痕
            AddInsectDamage(tree);
            
            // 添加风化效果
            AddWeatheringEffects(tree);
        }
        
        private static void AddLichensAndMoss(GameObject tree)
        {
            // 在树干低处添加苔藓效果
            // 在分支连接处添加地衣
        }
        
        private static void AddInsectDamage(GameObject tree)
        {
            // 随机添加小洞和咬痕
        }
        
        private static void AddWeatheringEffects(GameObject tree)
        {
            // 添加风吹日晒的痕迹
        }
        
        /// <summary>
        /// 模拟环境交互
        /// </summary>
        private static void SimulateEnvironmentalEffects(GameObject tree)
        {
            // 模拟重力对分支的影响
            // 模拟风力造成的变形
            // 模拟光照影响的不对称生长
        }
    }
    
    // 辅助数据结构
    public class TreeStructure
    {
        public BranchNode root;
        
        public Bounds GetBounds()
        {
            // 计算树的边界框
            return new Bounds(Vector3.zero, Vector3.one * 20f);
        }
        
        public List<BranchNode> GetAllBranches()
        {
            var branches = new List<BranchNode>();
            CollectBranches(root, branches);
            return branches;
        }
        
        private void CollectBranches(BranchNode node, List<BranchNode> collection)
        {
            if (node == null) return;
            collection.Add(node);
            foreach (var child in node.children)
            {
                CollectBranches(child, collection);
            }
        }
    }
    
    public class BranchNode
    {
        public Vector3 position;
        public Vector3 direction;
        public List<BranchNode> children = new List<BranchNode>();
        
        public BranchNode(Vector3 pos, Vector3 dir)
        {
            position = pos;
            direction = dir;
        }
    }
    
    public class NeedleCluster
    {
        public Vector3 position;
        public Vector3 direction;
        public float density;
    }
    
    public class PhotorealisticMaterials
    {
        public Material bark;
        public Material needles;
        public Material branches;
    }
}