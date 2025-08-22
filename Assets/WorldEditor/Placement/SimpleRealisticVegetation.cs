using UnityEngine;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 简化但逼真的植被生成器 - URP兼容
    /// </summary>
    public class SimpleRealisticVegetation
    {
        /// <summary>
        /// 创建简化但逼真的针叶树（URP兼容）
        /// </summary>
        public static GameObject CreateSimpleConifer(VegetationType type = VegetationType.针叶树)
        {
            GameObject tree = new GameObject($"SimpleConifer_{type}");
            
            // 创建树干
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 2.5f, 0);
            trunk.transform.localScale = new Vector3(0.3f, 5f, 0.3f);
            
            // 删除碰撞器
            Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());
            
            // 创建3层针叶（锥形）
            for (int i = 0; i < 3; i++)
            {
                GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                layer.name = $"Foliage_{i}";
                layer.transform.SetParent(tree.transform);
                
                float height = 4.5f + i * 1.2f;
                float width = 2.8f - i * 0.6f; // 上层更小
                
                layer.transform.localPosition = new Vector3(0, height, 0);
                layer.transform.localScale = new Vector3(width, 0.7f, width);
                
                // 删除碰撞器
                Object.DestroyImmediate(layer.GetComponent<SphereCollider>());
            }
            
            // 应用URP兼容材质
            ApplySimpleMaterials(tree);
            
            return tree;
        }
        
        /// <summary>
        /// 创建简化的真实草地
        /// </summary>
        public static GameObject CreateSimpleGrass(VegetationType type = VegetationType.野草)
        {
            GameObject grassCluster = new GameObject($"SimpleGrass_{type}");
            
            // 创建5-8根简化的草叶
            int grassCount = Random.Range(5, 9);
            
            for (int i = 0; i < grassCount; i++)
            {
                GameObject grassBlade = CreateSimpleGrassBlade(grassCluster.transform, i);
            }
            
            // 应用草地材质
            ApplyGrassMaterials(grassCluster, type);
            
            return grassCluster;
        }
        
        /// <summary>
        /// 创建简化的草叶
        /// </summary>
        static GameObject CreateSimpleGrassBlade(Transform parent, int index)
        {
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grass.name = $"Grass_{index}";
            grass.transform.SetParent(parent);
            
            // 删除碰撞器
            Object.DestroyImmediate(grass.GetComponent<MeshCollider>());
            
            // 随机位置和旋转
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(0f, 0.3f);
            
            grass.transform.localPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                0.4f, // 草叶中心高度
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );
            
            grass.transform.localRotation = Quaternion.Euler(
                Random.Range(75f, 105f), // 接近竖直
                Random.Range(0f, 360f),
                Random.Range(-10f, 10f)
            );
            
            // 随机大小
            float height = Random.Range(0.6f, 1.2f);
            float width = Random.Range(0.05f, 0.1f);
            grass.transform.localScale = new Vector3(width, height, 1f);
            
            return grass;
        }
        
        /// <summary>
        /// 应用简单的URP兼容材质
        /// </summary>
        static void ApplySimpleMaterials(GameObject tree)
        {
            // 查找URP着色器
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard"); // 后备
            }
            
            // 树干材质
            Material trunkMaterial = new Material(urpShader);
            trunkMaterial.name = "SimpleTrunk";
            trunkMaterial.color = new Color(0.4f, 0.25f, 0.15f, 1f);
            
            // 针叶材质
            Material foliageMaterial = new Material(urpShader);
            foliageMaterial.name = "SimpleFoliage";
            foliageMaterial.color = new Color(0.1f, 0.45f, 0.15f, 1f);
            
            // 应用材质到各个部分
            var renderers = tree.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.name.Contains("Trunk"))
                {
                    renderer.material = trunkMaterial;
                }
                else if (renderer.gameObject.name.Contains("Foliage"))
                {
                    renderer.material = foliageMaterial;
                }
            }
        }
        
        /// <summary>
        /// 应用草地材质
        /// </summary>
        static void ApplyGrassMaterials(GameObject grassCluster, VegetationType type)
        {
            // 查找URP着色器
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard"); // 后备
            }
            
            Material grassMaterial = new Material(urpShader);
            grassMaterial.name = "SimpleGrass";
            
            // 根据类型设置颜色
            switch (type)
            {
                case VegetationType.野草:
                    grassMaterial.color = new Color(0.2f, 0.7f, 0.2f, 1f);
                    break;
                case VegetationType.鲜花:
                    grassMaterial.color = new Color(0.8f, 0.4f, 0.6f, 1f);
                    break;
                case VegetationType.蕨类:
                    grassMaterial.color = new Color(0.1f, 0.6f, 0.3f, 1f);
                    break;
                default:
                    grassMaterial.color = new Color(0.3f, 0.6f, 0.2f, 1f);
                    break;
            }
            
            // 设置双面渲染（如果支持）
            if (grassMaterial.HasProperty("_Cull"))
            {
                grassMaterial.SetInt("_Cull", 0); // 双面
            }
            
            // 应用到所有草叶
            var renderers = grassCluster.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = grassMaterial;
            }
        }
    }
}