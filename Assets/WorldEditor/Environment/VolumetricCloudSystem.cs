using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 体积云系统 - 渲染真实的体积云效果
    /// </summary>
    public class VolumetricCloudSystem : MonoBehaviour
    {
        [Header("云系统设置")]
        [SerializeField] private bool enableClouds = true;
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private Mesh cloudMesh;
        [SerializeField] private int cloudLayers = 3;
        
        [Header("云属性")]
        [SerializeField] private float cloudCoverage = 0.5f;
        [SerializeField] private float cloudDensity = 0.7f;
        [SerializeField] private float cloudHeight = 2000f;
        [SerializeField] private float cloudThickness = 1000f;
        [SerializeField] private float cloudSpeed = 5f;
        
        [Header("云噪声")]
        [SerializeField] private float noiseScale = 0.001f;
        [SerializeField] private float detailScale = 0.01f;
        [SerializeField] private Vector3 windDirection = Vector3.forward;
        
        [Header("光照")]
        [SerializeField] private float lightScattering = 1f;
        [SerializeField] private float lightAbsorption = 0.3f;
        [SerializeField] private Color cloudColor = Color.white;
        [SerializeField] private float ambientLight = 0.3f;
        
        [Header("性能")]
        [SerializeField] private int rayMarchSteps = 64;
        [SerializeField] private int lightSteps = 8;
        [SerializeField] private float maxRenderDistance = 10000f;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private EnvironmentQuality currentQuality = EnvironmentQuality.High;
        private Camera mainCamera;
        private GameObject[] cloudObjects;
        private float timeOffset;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            SetupCloudSystem();
        }
        
        void SetupCloudSystem()
        {
            // 创建云对象
            CreateCloudObjects();
            
            // 设置云材质
            SetupCloudMaterial();
        }
        
        void CreateCloudObjects()
        {
            if (!enableClouds) return;
            
            cloudObjects = new GameObject[cloudLayers];
            
            for (int i = 0; i < cloudLayers; i++)
            {
                GameObject cloudObj = new GameObject($"Cloud Layer {i}");
                cloudObj.transform.SetParent(transform);
                
                // 设置云层高度
                float layerHeight = cloudHeight + (i * cloudThickness / cloudLayers);
                cloudObj.transform.position = new Vector3(0f, layerHeight, 0f);
                
                // 添加渲染组件
                MeshRenderer renderer = cloudObj.AddComponent<MeshRenderer>();
                MeshFilter filter = cloudObj.AddComponent<MeshFilter>();
                
                // 创建云网格
                filter.mesh = CreateCloudMesh(i);
                renderer.material = cloudMaterial;
                
                cloudObjects[i] = cloudObj;
            }
        }
        
        Mesh CreateCloudMesh(int layerIndex)
        {
            // 创建简单的云平面网格
            Mesh mesh = new Mesh();
            
            float size = maxRenderDistance * 2f;
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-size, 0, -size),
                new Vector3(size, 0, -size),
                new Vector3(size, 0, size),
                new Vector3(-size, 0, size)
            };
            
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        void SetupCloudMaterial()
        {
            if (cloudMaterial == null)
            {
                // 创建默认云材质
                cloudMaterial = new Material(Shader.Find("Unlit/Transparent"));
            }
            
            // 设置云材质参数
            UpdateCloudMaterialProperties();
        }
        
        void UpdateCloudMaterialProperties()
        {
            if (cloudMaterial == null) return;
            
            // 设置云参数
            cloudMaterial.SetFloat("_CloudCoverage", cloudCoverage);
            cloudMaterial.SetFloat("_CloudDensity", cloudDensity);
            cloudMaterial.SetFloat("_NoiseScale", noiseScale);
            cloudMaterial.SetFloat("_DetailScale", detailScale);
            cloudMaterial.SetVector("_WindDirection", windDirection);
            cloudMaterial.SetFloat("_TimeOffset", timeOffset);
            
            // 设置光照参数
            cloudMaterial.SetFloat("_LightScattering", lightScattering);
            cloudMaterial.SetFloat("_LightAbsorption", lightAbsorption);
            cloudMaterial.SetColor("_CloudColor", cloudColor);
            cloudMaterial.SetFloat("_AmbientLight", ambientLight);
            
            // 设置质量参数
            cloudMaterial.SetInt("_RayMarchSteps", rayMarchSteps);
            cloudMaterial.SetInt("_LightSteps", lightSteps);
        }
        
        public void UpdateClouds(float deltaTime, EnvironmentState environmentState, AtmosphericData atmosphericData)
        {
            if (!enableClouds) return;
            
            // 更新时间偏移
            timeOffset += deltaTime * cloudSpeed;
            
            // 更新云属性
            UpdateCloudProperties(environmentState, atmosphericData);
            
            // 更新云位置
            UpdateCloudPositions();
            
            // 更新材质属性
            UpdateCloudMaterialProperties();
        }
        
        void UpdateCloudProperties(EnvironmentState environmentState, AtmosphericData atmosphericData)
        {
            // 根据环境状态调整云属性
            cloudCoverage = environmentState.cloudCoverage;
            cloudDensity = environmentState.cloudDensity;
            cloudHeight = environmentState.cloudHeight;
            cloudSpeed = environmentState.cloudSpeed;
            
            // 根据风向调整云移动
            Vector3 windAt2000m = atmosphericData.GetWindAtHeight(2000f);
            windDirection = windAt2000m.normalized;
        }
        
        void UpdateCloudPositions()
        {
            if (cloudObjects == null || mainCamera == null) return;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            for (int i = 0; i < cloudObjects.Length; i++)
            {
                if (cloudObjects[i] != null)
                {
                    // 云层跟随摄像机移动（保持相对位置）
                    float layerHeight = cloudHeight + (i * cloudThickness / cloudLayers);
                    Vector3 targetPos = new Vector3(cameraPos.x, layerHeight, cameraPos.z);
                    
                    cloudObjects[i].transform.position = targetPos;
                }
            }
        }
        
        public void SetCloudsEnabled(bool enabled)
        {
            enableClouds = enabled;
            
            if (cloudObjects != null)
            {
                foreach (var cloudObj in cloudObjects)
                {
                    if (cloudObj != null)
                        cloudObj.SetActive(enabled);
                }
            }
        }
        
        public void SetQuality(EnvironmentQuality quality)
        {
            currentQuality = quality;
            
            // 根据质量调整参数
            switch (quality)
            {
                case EnvironmentQuality.Low:
                    rayMarchSteps = 32;
                    lightSteps = 4;
                    cloudLayers = 1;
                    break;
                case EnvironmentQuality.Medium:
                    rayMarchSteps = 48;
                    lightSteps = 6;
                    cloudLayers = 2;
                    break;
                case EnvironmentQuality.High:
                    rayMarchSteps = 64;
                    lightSteps = 8;
                    cloudLayers = 3;
                    break;
                case EnvironmentQuality.Ultra:
                    rayMarchSteps = 96;
                    lightSteps = 12;
                    cloudLayers = 4;
                    break;
            }
            
            // 重新创建云对象
            if (cloudObjects != null)
            {
                foreach (var cloudObj in cloudObjects)
                {
                    if (cloudObj != null)
                        DestroyImmediate(cloudObj);
                }
            }
            
            CreateCloudObjects();
        }
        
        public float GetCloudCoverage()
        {
            return cloudCoverage;
        }
        
        public float GetCloudDensity()
        {
            return cloudDensity;
        }
        
        void OnDestroy()
        {
            if (cloudObjects != null)
            {
                foreach (var cloudObj in cloudObjects)
                {
                    if (cloudObj != null)
                        DestroyImmediate(cloudObj);
                }
            }
        }
    }
}