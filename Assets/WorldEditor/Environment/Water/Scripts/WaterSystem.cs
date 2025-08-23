using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 水体系统 - 管理动态水体渲染和水文循环
    /// 
    /// 核心功能：
    /// - 动态水体表面渲染和反射
    /// - 水位变化和潮汐系统
    /// - 水体物理交互和浮力计算
    /// - 水文循环模拟（蒸发、降水、径流）
    /// - 水质和透明度动态管理
    /// </summary>
    public class WaterSystem : MonoBehaviour
    {
        #region 水体配置参数

        [Header("水体基础配置")]
        [Tooltip("全局海平面高度")]
        public float globalWaterLevel = 0f;
        
        [Tooltip("水体颜色")]
        public Color waterColor = new Color(0.1f, 0.4f, 0.8f, 0.8f);
        
        [Tooltip("水体透明度 (0=完全透明, 1=完全不透明)")]
        [Range(0f, 1f)]
        public float waterTransparency = 0.7f;
        
        [Tooltip("水体反射强度")]
        [Range(0f, 1f)]
        public float reflectionIntensity = 0.8f;

        [Header("水体表面效果")]
        [Tooltip("波浪强度")]
        [Range(0f, 2f)]
        public float waveStrength = 1f;
        
        [Tooltip("波浪速度")]
        [Range(0f, 5f)]
        public float waveSpeed = 1f;
        
        [Tooltip("波浪方向")]
        public Vector2 waveDirection = Vector2.right;
        
        [Tooltip("水体泡沫强度")]
        [Range(0f, 1f)]
        public float foamIntensity = 0.3f;

        #endregion

        #region 潮汐系统配置

        [Header("潮汐系统")]
        [Tooltip("是否启用潮汐系统")]
        public bool enableTides = true;
        
        [Tooltip("潮汐变化幅度 (米)")]
        [Range(0f, 10f)]
        public float tidalRange = 2f;
        
        [Tooltip("潮汐周期 (小时)")]
        [Range(1f, 24f)]
        public float tidalCycle = 12f;
        
        [Tooltip("当前潮汐阶段 (0=低潮, 0.5=平均, 1=高潮)")]
        [Range(0f, 1f)]
        public float currentTidalPhase = 0.5f;

        #endregion

        #region 水文循环配置

        [Header("水文循环系统")]
        [Tooltip("是否启用水文循环")]
        public bool enableHydroCycle = true;
        
        [Tooltip("蒸发率 (米/小时)")]
        [Range(0f, 0.01f)]
        public float evaporationRate = 0.001f;
        
        [Tooltip("降水对水位的影响系数")]
        [Range(0f, 1f)]
        public float precipitationInfluence = 0.5f;
        
        [Tooltip("径流汇集效率")]
        [Range(0f, 1f)]
        public float runoffEfficiency = 0.3f;

        #endregion

        #region 水质管理

        [Header("水质管理")]
        [Tooltip("水体温度 (摄氏度)")]
        [Range(-10f, 40f)]
        public float waterTemperature = 15f;
        
        [Tooltip("水体浑浊度 (0=清澈, 1=浑浊)")]
        [Range(0f, 1f)]
        public float waterTurbidity = 0.1f;
        
        [Tooltip("水体盐分含量 (0=淡水, 1=海水)")]
        [Range(0f, 1f)]
        public float salinity = 0f;
        
        [Tooltip("水体污染程度")]
        [Range(0f, 1f)]
        public float pollutionLevel = 0f;

        #endregion

        #region 渲染配置

        [Header("水体渲染")]
        [Tooltip("水体材质")]
        public Material waterMaterial;
        
        [Tooltip("水体网格")]
        public Mesh waterMesh;
        
        [Tooltip("水体渲染层级")]
        [Range(1, 32)]
        public int waterRenderLayer = 4;
        
        [Tooltip("反射摄像机分辨率")]
        public int reflectionTextureSize = 512;
        
        [Tooltip("是否启用实时反射")]
        public bool enableRealtimeReflection = true;

        #endregion

        #region 物理交互配置

        [Header("物理交互")]
        [Tooltip("是否启用浮力系统")]
        public bool enableBuoyancy = true;
        
        [Tooltip("浮力强度")]
        [Range(0f, 10f)]
        public float buoyancyStrength = 5f;
        
        [Tooltip("水阻力系数")]
        [Range(0f, 2f)]
        public float waterDrag = 0.5f;
        
        [Tooltip("水体碰撞检测")]
        public LayerMask buoyancyLayerMask = -1;

        #endregion

        #region 运行时状态

        private bool isInitialized = false;
        private bool isActive = false;
        private float baseWaterLevel;
        private float currentWaterLevel;
        private Material waterMaterialInstance;
        private Camera reflectionCamera;
        private RenderTexture reflectionTexture;
        private EnvironmentState linkedEnvironmentState;
        private List<GameObject> waterBodies = new List<GameObject>();
        private Dictionary<Collider, float> buoyancyObjects = new Dictionary<Collider, float>();

        #endregion

        #region 着色器属性ID

        private static readonly int WaterColor = Shader.PropertyToID("_WaterColor");
        private static readonly int WaveStrength = Shader.PropertyToID("_WaveStrength");
        private static readonly int WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveDirection = Shader.PropertyToID("_WaveDirection");
        private static readonly int ReflectionTexture = Shader.PropertyToID("_ReflectionTexture");
        private static readonly int WaterLevel = Shader.PropertyToID("_WaterLevel");
        private static readonly int Transparency = Shader.PropertyToID("_Transparency");
        private static readonly int Turbidity = Shader.PropertyToID("_Turbidity");
        private static readonly int Temperature = Shader.PropertyToID("_Temperature");

        #endregion

        #region 事件系统

        /// <summary>水位变化事件</summary>
        public event Action<float> OnWaterLevelChanged;
        
        /// <summary>潮汐变化事件</summary>
        public event Action<float> OnTidalPhaseChanged;
        
        /// <summary>水质变化事件</summary>
        public event Action<float, float> OnWaterQualityChanged; // 浑浊度, 污染程度

        #endregion

        #region 公共属性

        /// <summary>水体系统是否激活</summary>
        public bool IsActive => isActive && isInitialized;
        
        /// <summary>当前水位</summary>
        public float CurrentWaterLevel => currentWaterLevel;
        
        /// <summary>当前潮汐高度</summary>
        public float CurrentTidalHeight => enableTides ? (currentTidalPhase - 0.5f) * tidalRange : 0f;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化水体系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[WaterSystem] 水体系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[WaterSystem] 开始初始化水体系统...");

            // 链接环境状态
            linkedEnvironmentState = environmentState;

            // 设置基础水位
            baseWaterLevel = globalWaterLevel;
            currentWaterLevel = baseWaterLevel;

            // 初始化材质
            InitializeMaterial();

            // 初始化反射系统
            if (enableRealtimeReflection)
            {
                InitializeReflectionSystem();
            }

            // 创建默认水体
            CreateDefaultWaterBodies();

            // 同步环境状态
            if (linkedEnvironmentState != null)
            {
                SyncFromEnvironmentState();
            }

            isActive = true;
            isInitialized = true;

            Debug.Log("[WaterSystem] 水体系统初始化完成");
        }

        /// <summary>
        /// 初始化水体材质
        /// </summary>
        private void InitializeMaterial()
        {
            if (waterMaterial != null)
            {
                waterMaterialInstance = new Material(waterMaterial);
            }
            else
            {
                // 使用默认水体着色器
                waterMaterialInstance = new Material(Shader.Find("Standard"));
            }

            UpdateMaterialProperties();
        }

        /// <summary>
        /// 初始化反射系统
        /// </summary>
        private void InitializeReflectionSystem()
        {
            // 创建反射摄像机
            GameObject reflectionCamGO = new GameObject("WaterReflectionCamera");
            reflectionCamera = reflectionCamGO.AddComponent<Camera>();
            reflectionCamera.enabled = false;
            reflectionCamera.renderingPath = RenderingPath.Forward;
            reflectionCamera.backgroundColor = Color.black;
            reflectionCamera.clearFlags = CameraClearFlags.Skybox;

            // 创建反射纹理
            reflectionTexture = new RenderTexture(reflectionTextureSize, reflectionTextureSize, 16);
            reflectionTexture.name = "WaterReflectionTexture";
            reflectionCamera.targetTexture = reflectionTexture;

            // 设置到材质
            if (waterMaterialInstance != null && waterMaterialInstance.HasProperty(ReflectionTexture))
            {
                waterMaterialInstance.SetTexture(ReflectionTexture, reflectionTexture);
            }
        }

        /// <summary>
        /// 创建默认水体
        /// </summary>
        private void CreateDefaultWaterBodies()
        {
            if (waterMesh == null)
            {
                // 创建基础平面网格
                waterMesh = CreateWaterPlaneMesh();
            }

            // 创建主要水体对象
            GameObject waterBodyGO = new GameObject("MainWaterBody");
            waterBodyGO.layer = waterRenderLayer;
            
            MeshRenderer renderer = waterBodyGO.AddComponent<MeshRenderer>();
            MeshFilter filter = waterBodyGO.AddComponent<MeshFilter>();
            
            renderer.material = waterMaterialInstance;
            filter.mesh = waterMesh;
            
            // 设置位置
            waterBodyGO.transform.position = new Vector3(0, currentWaterLevel, 0);
            waterBodyGO.transform.localScale = new Vector3(100, 1, 100); // 100x100的水面
            
            waterBodies.Add(waterBodyGO);

            // 添加碰撞体（用于浮力计算）
            if (enableBuoyancy)
            {
                BoxCollider waterCollider = waterBodyGO.AddComponent<BoxCollider>();
                waterCollider.isTrigger = true;
                waterCollider.size = new Vector3(1, 0.1f, 1);
            }
        }

        /// <summary>
        /// 创建水体平面网格
        /// </summary>
        private Mesh CreateWaterPlaneMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "WaterPlane";

            // 创建顶点
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f)
            };

            // 创建UV坐标
            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            // 创建三角形
            int[] triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };

            // 创建法线
            Vector3[] normals = new Vector3[4]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.normals = normals;

            return mesh;
        }

        #endregion

        #region 水位控制方法

        /// <summary>
        /// 设置全局水位
        /// </summary>
        public void SetWaterLevel(float level)
        {
            if (Mathf.Abs(currentWaterLevel - level) < 0.001f) return;

            float previousLevel = currentWaterLevel;
            currentWaterLevel = level;

            // 更新所有水体位置
            foreach (GameObject waterBody in waterBodies)
            {
                if (waterBody != null)
                {
                    Vector3 pos = waterBody.transform.position;
                    pos.y = currentWaterLevel;
                    waterBody.transform.position = pos;
                }
            }

            // 更新材质属性
            if (waterMaterialInstance != null && waterMaterialInstance.HasProperty(WaterLevel))
            {
                waterMaterialInstance.SetFloat(WaterLevel, currentWaterLevel);
            }

            // 同步到环境状态
            SyncToEnvironmentState();

            // 触发事件
            OnWaterLevelChanged?.Invoke(currentWaterLevel);

            Debug.Log($"[WaterSystem] 水位从 {previousLevel:F2} 变更为 {currentWaterLevel:F2}");
        }

        /// <summary>
        /// 调整水位（相对变化）
        /// </summary>
        public void AdjustWaterLevel(float delta)
        {
            SetWaterLevel(currentWaterLevel + delta);
        }

        #endregion

        #region 潮汐系统

        /// <summary>
        /// 更新潮汐系统
        /// </summary>
        private void UpdateTidalSystem(float deltaTime)
        {
            if (!enableTides) return;

            // 根据时间推进潮汐相位
            float tidalSpeed = 1f / (tidalCycle * 3600f); // 转换为秒
            currentTidalPhase += tidalSpeed * deltaTime;
            
            // 保持在0-1范围内循环
            if (currentTidalPhase > 1f)
                currentTidalPhase -= 1f;

            // 计算潮汐高度
            float tidalHeight = Mathf.Sin(currentTidalPhase * 2f * Mathf.PI) * tidalRange * 0.5f;
            float targetWaterLevel = baseWaterLevel + tidalHeight;

            // 平滑过渡到目标水位
            float smoothedLevel = Mathf.Lerp(currentWaterLevel, targetWaterLevel, deltaTime * 0.5f);
            
            if (Mathf.Abs(smoothedLevel - currentWaterLevel) > 0.01f)
            {
                SetWaterLevel(smoothedLevel);
                OnTidalPhaseChanged?.Invoke(currentTidalPhase);
            }
        }

        #endregion

        #region 水文循环系统

        /// <summary>
        /// 更新水文循环
        /// </summary>
        private void UpdateHydroCycle(float deltaTime)
        {
            if (!enableHydroCycle || linkedEnvironmentState == null) return;

            float waterLevelChange = 0f;

            // 蒸发效应（温度越高蒸发越快）
            float temperatureFactor = Mathf.Clamp01((linkedEnvironmentState.temperature + 10f) / 40f);
            waterLevelChange -= evaporationRate * temperatureFactor * deltaTime;

            // 降水效应
            if (linkedEnvironmentState.currentWeather == WeatherType.Rainy ||
                linkedEnvironmentState.currentWeather == WeatherType.Storm)
            {
                waterLevelChange += precipitationInfluence * linkedEnvironmentState.weatherIntensity * 0.001f * deltaTime;
            }
            else if (linkedEnvironmentState.currentWeather == WeatherType.Snowy)
            {
                // 雪天时降水效应减半（雪融化较慢）
                waterLevelChange += precipitationInfluence * linkedEnvironmentState.weatherIntensity * 0.0005f * deltaTime;
            }

            // 径流汇集（基于地形坡度和降雨）
            if (linkedEnvironmentState.weatherIntensity > 0.3f)
            {
                waterLevelChange += runoffEfficiency * linkedEnvironmentState.weatherIntensity * 0.0005f * deltaTime;
            }

            // 应用水位变化
            if (Mathf.Abs(waterLevelChange) > 0.0001f)
            {
                AdjustWaterLevel(waterLevelChange);
            }
        }

        #endregion

        #region 水质管理

        /// <summary>
        /// 更新水质参数
        /// </summary>
        private void UpdateWaterQuality()
        {
            if (linkedEnvironmentState == null) return;

            bool qualityChanged = false;

            // 温度影响水体颜色和透明度
            if (Mathf.Abs(waterTemperature - linkedEnvironmentState.waterTemperature) > 0.5f)
            {
                waterTemperature = linkedEnvironmentState.waterTemperature;
                qualityChanged = true;
            }

            // 天气对浑浊度的影响
            float targetTurbidity = linkedEnvironmentState.waterTurbidity;
            if (linkedEnvironmentState.currentWeather == WeatherType.Storm)
            {
                targetTurbidity = Mathf.Min(1f, targetTurbidity + 0.3f * linkedEnvironmentState.weatherIntensity);
            }

            if (Mathf.Abs(waterTurbidity - targetTurbidity) > 0.01f)
            {
                waterTurbidity = Mathf.Lerp(waterTurbidity, targetTurbidity, Time.deltaTime * 0.5f);
                qualityChanged = true;
            }

            // 更新材质属性
            if (qualityChanged)
            {
                UpdateMaterialProperties();
                OnWaterQualityChanged?.Invoke(waterTurbidity, pollutionLevel);
            }
        }

        #endregion

        #region 渲染和材质管理

        /// <summary>
        /// 更新材质属性
        /// </summary>
        private void UpdateMaterialProperties()
        {
            if (waterMaterialInstance == null) return;

            // 基础属性
            if (waterMaterialInstance.HasProperty(WaterColor))
                waterMaterialInstance.SetColor(WaterColor, waterColor);

            if (waterMaterialInstance.HasProperty(Transparency))
                waterMaterialInstance.SetFloat(Transparency, waterTransparency);

            if (waterMaterialInstance.HasProperty(Turbidity))
                waterMaterialInstance.SetFloat(Turbidity, waterTurbidity);

            if (waterMaterialInstance.HasProperty(Temperature))
                waterMaterialInstance.SetFloat(Temperature, waterTemperature);

            // 波浪属性
            if (waterMaterialInstance.HasProperty(WaveStrength))
                waterMaterialInstance.SetFloat(WaveStrength, waveStrength);

            if (waterMaterialInstance.HasProperty(WaveSpeed))
                waterMaterialInstance.SetFloat(WaveSpeed, waveSpeed);

            if (waterMaterialInstance.HasProperty(WaveDirection))
                waterMaterialInstance.SetVector(WaveDirection, waveDirection);
        }

        /// <summary>
        /// 更新反射系统
        /// </summary>
        private void UpdateReflectionSystem()
        {
            if (!enableRealtimeReflection || reflectionCamera == null) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // 设置反射摄像机属性
            reflectionCamera.fieldOfView = mainCamera.fieldOfView;
            reflectionCamera.nearClipPlane = mainCamera.nearClipPlane;
            reflectionCamera.farClipPlane = mainCamera.farClipPlane;
            reflectionCamera.renderingPath = mainCamera.renderingPath;

            // 计算反射变换
            Vector3 pos = mainCamera.transform.position;
            Vector3 normal = Vector3.up;
            float d = -Vector3.Dot(normal, Vector3.up * currentWaterLevel);
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = CalculateReflectionMatrix(reflectionPlane);
            Vector3 newpos = reflection.MultiplyPoint(pos);
            reflectionCamera.transform.position = newpos;

            Vector3 newdir = reflection.MultiplyVector(mainCamera.transform.forward);
            reflectionCamera.transform.LookAt(newpos + newdir, reflection.MultiplyVector(mainCamera.transform.up));

            // 渲染反射
            reflectionCamera.Render();
        }

        /// <summary>
        /// 计算反射矩阵
        /// </summary>
        private Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 reflectionMat = Matrix4x4.identity;

            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }

        #endregion

        #region 物理交互系统

        /// <summary>
        /// 处理浮力物体进入水体
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (!enableBuoyancy || (buoyancyLayerMask & (1 << other.gameObject.layer)) == 0) return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !buoyancyObjects.ContainsKey(other))
            {
                buoyancyObjects.Add(other, other.bounds.center.y);
            }
        }

        /// <summary>
        /// 处理浮力物体离开水体
        /// </summary>
        void OnTriggerExit(Collider other)
        {
            if (buoyancyObjects.ContainsKey(other))
            {
                buoyancyObjects.Remove(other);
            }
        }

        /// <summary>
        /// 应用浮力效果
        /// </summary>
        private void ApplyBuoyancyForces()
        {
            if (!enableBuoyancy) return;

            List<Collider> toRemove = new List<Collider>();

            foreach (var kvp in buoyancyObjects)
            {
                Collider col = kvp.Key;
                if (col == null || col.attachedRigidbody == null)
                {
                    toRemove.Add(col);
                    continue;
                }

                Rigidbody rb = col.attachedRigidbody;
                
                // 计算物体在水中的深度
                float objectBottom = col.bounds.center.y - col.bounds.extents.y;
                float objectTop = col.bounds.center.y + col.bounds.extents.y;
                float waterLevel = currentWaterLevel;

                if (objectBottom < waterLevel)
                {
                    // 计算浸入水中的比例
                    float submersionRatio = Mathf.Clamp01((waterLevel - objectBottom) / col.bounds.size.y);
                    
                    // 应用浮力
                    Vector3 buoyancyForce = Vector3.up * buoyancyStrength * submersionRatio * rb.mass;
                    rb.AddForce(buoyancyForce);
                    
                    // 应用水阻力
                    Vector3 dragForce = -rb.linearVelocity * waterDrag * submersionRatio;
                    rb.AddForce(dragForce);
                }
            }

            // 清理无效引用
            foreach (Collider col in toRemove)
            {
                buoyancyObjects.Remove(col);
            }
        }

        #endregion

        #region 系统更新

        /// <summary>
        /// 更新水体系统 (由EnvironmentManager调用)
        /// </summary>
        public void UpdateSystem()
        {
            if (!isActive) return;

            float deltaTime = Time.deltaTime;

            // 更新潮汐系统
            UpdateTidalSystem(deltaTime);

            // 更新水文循环
            UpdateHydroCycle(deltaTime);

            // 更新水质
            UpdateWaterQuality();

            // 更新反射系统
            if (enableRealtimeReflection)
            {
                UpdateReflectionSystem();
            }

            // 应用浮力效果
            ApplyBuoyancyForces();

            // 从环境状态同步
            if (linkedEnvironmentState != null)
            {
                if (Mathf.Abs(currentWaterLevel - linkedEnvironmentState.globalWaterLevel) > 0.01f)
                {
                    SetWaterLevel(linkedEnvironmentState.globalWaterLevel);
                }
            }
        }

        #endregion

        #region 环境状态同步

        /// <summary>
        /// 从环境状态同步
        /// </summary>
        private void SyncFromEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;
            
            SetWaterLevel(linkedEnvironmentState.globalWaterLevel);
            waterTemperature = linkedEnvironmentState.waterTemperature;
            waterTurbidity = linkedEnvironmentState.waterTurbidity;
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 同步到环境状态
        /// </summary>
        private void SyncToEnvironmentState()
        {
            if (linkedEnvironmentState == null) return;

            linkedEnvironmentState.globalWaterLevel = currentWaterLevel;
            linkedEnvironmentState.waterTemperature = waterTemperature;
            linkedEnvironmentState.waterTurbidity = waterTurbidity;
        }

        #endregion

        #region 清理资源

        void OnDestroy()
        {
            // 清理材质实例
            if (waterMaterialInstance != null)
            {
                DestroyImmediate(waterMaterialInstance);
            }

            // 清理反射纹理
            if (reflectionTexture != null)
            {
                reflectionTexture.Release();
                DestroyImmediate(reflectionTexture);
            }

            // 清理反射摄像机
            if (reflectionCamera != null)
            {
                DestroyImmediate(reflectionCamera.gameObject);
            }
        }

        #endregion

        #region 调试信息

        void OnGUI()
        {
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            GUILayout.BeginArea(new Rect(1160, 10, 200, 180));
            GUILayout.Box("水体系统调试");
            
            GUILayout.Label($"当前水位: {currentWaterLevel:F2}m");
            GUILayout.Label($"潮汐相位: {currentTidalPhase:F2}");
            GUILayout.Label($"潮汐高度: {CurrentTidalHeight:F2}m");
            GUILayout.Label($"水温: {waterTemperature:F1}°C");
            GUILayout.Label($"浑浊度: {waterTurbidity:F2}");
            GUILayout.Label($"波浪强度: {waveStrength:F2}");
            GUILayout.Label($"浮力对象: {buoyancyObjects.Count}个");
            GUILayout.Label($"潮汐: {(enableTides ? "开启" : "关闭")}");
            GUILayout.Label($"水文循环: {(enableHydroCycle ? "开启" : "关闭")}");
            
            GUILayout.EndArea();
        }

        #endregion
    }
}