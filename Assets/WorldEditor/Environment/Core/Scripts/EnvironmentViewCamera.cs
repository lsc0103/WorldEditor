using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境观察摄像机 - 专门用于观察环境系统效果
    /// 提供最佳视角观察地形和光照变化
    /// </summary>
    public class EnvironmentViewCamera : MonoBehaviour
    {
        [Header("摄像机设置")]
        [Tooltip("目标地形中心点")]
        public Transform terrainTarget;
        
        [Tooltip("观察高度")]
        [Range(10f, 200f)]
        public float viewHeight = 50f;
        
        [Tooltip("观察距离")]
        [Range(10f, 100f)]
        public float viewDistance = 30f;
        
        [Tooltip("俯视角度")]
        [Range(0f, 60f)]
        public float viewAngle = 25f;
        
        [Header("摄像机参数")]
        [Tooltip("视野角度")]
        [Range(30f, 120f)]
        public float fieldOfView = 65f;
        
        [Tooltip("远裁剪面")]
        public float farClipPlane = 500f;

        [Header("自动模式")]
        [Tooltip("是否自动寻找最佳位置")]
        public bool autoPositioning = true;
        
        [Tooltip("是否跟随地形中心")]
        public bool followTerrain = true;

        private Camera cam;
        private Terrain currentTerrain;

        void Start()
        {
            Debug.Log("[EnvironmentViewCamera] 开始初始化环境观察相机...");
            
            SetupCamera();
            
            if (autoPositioning)
            {
                FindOptimalPosition();
            }
            
            // 延迟一帧确保所有设置都已应用
            if (Application.isPlaying)
            {
                StartCoroutine(LateInitialization());
            }
        }
        
        private System.Collections.IEnumerator LateInitialization()
        {
            yield return null; // 等待一帧
            
            // 再次确认相机设置
            if (cam != null)
            {
                cam.enabled = true;
                Debug.Log($"[EnvironmentViewCamera] 延迟初始化完成 - 相机启用状态: {cam.enabled}");
            }
        }

        void Update()
        {
            if (followTerrain && currentTerrain != null)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// 设置摄像机组件 - 优化光照和环境观察效果
        /// </summary>
        private void SetupCamera()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }

            // 基础相机参数
            cam.fieldOfView = fieldOfView;
            cam.farClipPlane = farClipPlane;
            cam.nearClipPlane = 0.3f; // 稍微调整近裁剪面以避免z-fighting
            
            // 渲染设置 - 专注于环境观察
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.depth = 10; // 设置较高深度确保主导显示
            cam.cullingMask = -1; // 渲染所有层级
            
            // 禁用UI层级渲染，专注于3D环境
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                cam.cullingMask &= ~(1 << uiLayer);
            }
            
            // 优化渲染路径以获得更好的光照效果
            cam.renderingPath = RenderingPath.UsePlayerSettings;
            cam.useOcclusionCulling = true; // 启用遮挡剔除提高性能
            cam.allowHDR = true; // 启用HDR以获得更好的光照效果
            cam.allowMSAA = true; // 启用抗锯齿
            
            // 设置相机标签确保它是主相机
            cam.tag = "MainCamera";
            
            // 后处理设置（如果可用）
            SetupPostProcessing();
            
            Debug.Log($"[EnvironmentViewCamera] 环境观察相机设置完成:");
            Debug.Log($"  - 深度: {cam.depth}");
            Debug.Log($"  - 渲染层级: {cam.cullingMask}");
            Debug.Log($"  - HDR: {cam.allowHDR}");
            Debug.Log($"  - 抗锯齿: {cam.allowMSAA}");
        }
        
        /// <summary>
        /// 设置后处理效果以增强环境观察
        /// </summary>
        private void SetupPostProcessing()
        {
            // 尝试获取后处理组件（如果项目中使用了Post Processing Stack）
            var postProcessLayer = GetComponent<MonoBehaviour>();
            if (postProcessLayer != null && postProcessLayer.GetType().Name.Contains("PostProcess"))
            {
                Debug.Log("[EnvironmentViewCamera] 检测到后处理组件，已启用");
            }
            
            // 设置天空盒相关参数
            if (RenderSettings.skybox != null)
            {
                Debug.Log($"[EnvironmentViewCamera] 使用天空盒: {RenderSettings.skybox.name}");
            }
        }

        /// <summary>
        /// 智能寻找最佳观察位置 - 全新算法确保观察整个地形和光照效果
        /// </summary>
        [ContextMenu("寻找最佳位置")]
        public void FindOptimalPosition()
        {
            Debug.Log("[EnvironmentViewCamera] 开始智能定位最佳观察位置...");
            
            // 查找所有地形对象（支持多地形）
            Terrain[] allTerrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            
            if (allTerrains.Length > 0)
            {
                // 计算所有地形的边界
                Bounds worldBounds = CalculateWorldBounds(allTerrains);
                currentTerrain = allTerrains[0]; // 使用第一个地形作为参考
                
                Debug.Log($"[EnvironmentViewCamera] 发现 {allTerrains.Length} 个地形");
                Debug.Log($"[EnvironmentViewCamera] 世界边界: {worldBounds}");
                
                // 设置观察目标为世界中心，但稍微偏向地形中心
                Vector3 worldCenter = worldBounds.center;
                worldCenter.y = worldBounds.min.y + worldBounds.size.y * 0.3f; // 观察点在30%高度处
                
                if (terrainTarget == null)
                {
                    GameObject targetGO = new GameObject("World Center Target");
                    targetGO.transform.position = worldCenter;
                    terrainTarget = targetGO.transform;
                }
                else
                {
                    terrainTarget.position = worldCenter;
                }

                // 智能计算最佳相机参数
                CalculateOptimalCameraParameters(worldBounds);
                
                Debug.Log($"[EnvironmentViewCamera] 目标位置: {worldCenter}");
                Debug.Log($"[EnvironmentViewCamera] 相机参数 - 高度: {viewHeight:F1}, 距离: {viewDistance:F1}, 角度: {viewAngle:F1}");
            }
            else
            {
                Debug.LogWarning("[EnvironmentViewCamera] 未找到地形对象，尝试查找AdvancedTerrainGenerator...");
                FindTerrainFromGenerator();
            }
            
            UpdatePosition();
        }
        
        /// <summary>
        /// 计算世界边界包围所有地形
        /// </summary>
        private Bounds CalculateWorldBounds(Terrain[] terrains)
        {
            if (terrains.Length == 0) return new Bounds();
            
            Bounds bounds = new Bounds();
            bool first = true;
            
            foreach (var terrain in terrains)
            {
                if (terrain.terrainData != null)
                {
                    Vector3 terrainPos = terrain.transform.position;
                    Vector3 terrainSize = terrain.terrainData.size;
                    
                    Bounds terrainBounds = new Bounds();
                    terrainBounds.SetMinMax(terrainPos, terrainPos + terrainSize);
                    
                    if (first)
                    {
                        bounds = terrainBounds;
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(terrainBounds);
                    }
                }
            }
            
            return bounds;
        }
        
        /// <summary>
        /// 智能计算最佳相机参数 - 修正版确保看到完整地形
        /// </summary>
        private void CalculateOptimalCameraParameters(Bounds worldBounds)
        {
            Vector3 size = worldBounds.size;
            float maxHorizontalSize = Mathf.Max(size.x, size.z);
            float terrainHeight = size.y;
            
            Debug.Log($"[EnvironmentViewCamera] 地形分析:");
            Debug.Log($"  - 地形尺寸: {size}");
            Debug.Log($"  - 最大水平尺寸: {maxHorizontalSize:F1}m");
            Debug.Log($"  - 地形高度: {terrainHeight:F1}m");
            
            // 重新设计距离计算 - 确保能看到整个地形
            // 使用更大的倍数来确保全景视野
            float fovRadians = fieldOfView * Mathf.Deg2Rad;
            float requiredDistanceForWidth = (maxHorizontalSize * 1.2f) / (2f * Mathf.Tan(fovRadians * 0.5f));
            
            // 确保距离足够远以看到整个地形
            viewDistance = Mathf.Max(requiredDistanceForWidth, maxHorizontalSize * 0.8f); // 增加到80%
            viewDistance = Mathf.Max(viewDistance, 200f); // 最小距离保证
            
            // 重新计算观察高度 - 更高的视角
            viewHeight = Mathf.Max(terrainHeight * 1.5f, maxHorizontalSize * 0.5f); // 增加高度倍数
            viewHeight = Mathf.Max(viewHeight, 150f); // 提高最小高度
            
            // 根据地形规模调整参数
            if (maxHorizontalSize > 2000f) // 大型地形
            {
                viewAngle = 45f; // 更大的俯视角度
                fieldOfView = 80f; // 更大的视野
                viewDistance *= 1.3f; // 进一步增加距离
                viewHeight *= 1.2f; // 进一步增加高度
            }
            else if (maxHorizontalSize > 1000f) // 中型地形
            {
                viewAngle = 40f;
                fieldOfView = 75f;
                viewDistance *= 1.2f;
                viewHeight *= 1.1f;
            }
            else if (maxHorizontalSize > 500f) // 小型地形
            {
                viewAngle = 35f;
                fieldOfView = 70f;
                viewDistance *= 1.1f;
            }
            else // 很小的地形
            {
                viewAngle = 30f;
                fieldOfView = 65f;
            }
            
            // 根据地形高度比例进一步调整
            float heightRatio = terrainHeight / maxHorizontalSize;
            if (heightRatio > 0.3f) // 地形较陡峭
            {
                viewAngle += 15f; // 显著增加俯视角度
                viewHeight *= 1.3f; // 增加高度以获得更好的俯视效果
            }
            
            // 最终安全检查 - 确保参数合理
            viewDistance = Mathf.Clamp(viewDistance, 100f, 5000f);
            viewHeight = Mathf.Clamp(viewHeight, 100f, 2000f);
            viewAngle = Mathf.Clamp(viewAngle, 20f, 60f);
            fieldOfView = Mathf.Clamp(fieldOfView, 60f, 90f);
            
            Debug.Log($"[EnvironmentViewCamera] 计算结果:");
            Debug.Log($"  - 观察距离: {viewDistance:F1}m");
            Debug.Log($"  - 观察高度: {viewHeight:F1}m");
            Debug.Log($"  - 俯视角度: {viewAngle:F1}°");
            Debug.Log($"  - 视野角度: {fieldOfView:F1}°");
            Debug.Log($"  - 高度比例: {heightRatio:F2}");
        }
        
        /// <summary>
        /// 从地形生成器中查找地形信息
        /// </summary>
        private void FindTerrainFromGenerator()
        {
            var terrainGenerators = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var generator in terrainGenerators)
            {
                if (generator.GetType().Name.Contains("TerrainGenerator"))
                {
                    var terrain = generator.GetComponent<Terrain>();
                    if (terrain != null)
                    {
                        currentTerrain = terrain;
                        Debug.Log($"[EnvironmentViewCamera] 从生成器找到地形: {terrain.name}");
                        
                        Terrain[] terrains = { terrain };
                        Bounds worldBounds = CalculateWorldBounds(terrains);
                        CalculateOptimalCameraParameters(worldBounds);
                        return;
                    }
                }
            }
            
            // 最后的备选方案：使用默认参数并设置一个较好的观察位置
            SetDefaultOptimalPosition();
        }
        
        /// <summary>
        /// 设置默认的最佳观察位置 - 适用于大型地形
        /// </summary>
        private void SetDefaultOptimalPosition()
        {
            Debug.Log("[EnvironmentViewCamera] 使用默认大型地形观察参数");
            
            // 假设是大型地形，使用更保守的参数确保能看到全貌
            viewHeight = 300f;  // 很高的观察高度
            viewDistance = 400f; // 很远的观察距离
            viewAngle = 50f;    // 较大的俯视角度
            fieldOfView = 80f;  // 很大的视野角度
            
            Debug.Log($"[EnvironmentViewCamera] 默认参数设置完成:");
            Debug.Log($"  - 默认高度: {viewHeight}m (适用于大型地形)");
            Debug.Log($"  - 默认距离: {viewDistance}m");
            Debug.Log($"  - 默认俯视角: {viewAngle}°");
            Debug.Log($"  - 默认视野角: {fieldOfView}°");
            
            if (terrainTarget == null)
            {
                GameObject targetGO = new GameObject("Default Large Terrain Target");
                targetGO.transform.position = Vector3.zero;
                terrainTarget = targetGO.transform;
            }
        }

        /// <summary>
        /// 智能更新摄像机位置 - 优化光照观察效果
        /// </summary>
        private void UpdatePosition()
        {
            if (terrainTarget == null) return;

            Vector3 targetPos = terrainTarget.position;
            
            // 计算最佳观察位置 - 考虑太阳光角度和阴影效果
            Vector3 cameraPosition = CalculateOptimalCameraPosition(targetPos);
            
            // 平滑移动到目标位置（如果在运行时）
            if (Application.isPlaying)
            {
                transform.position = Vector3.Lerp(transform.position, cameraPosition, Time.deltaTime * 2f);
            }
            else
            {
                transform.position = cameraPosition;
            }
            
            // 智能计算朝向 - 确保最佳观察角度
            CalculateOptimalCameraRotation(targetPos);
            
            Debug.Log($"[EnvironmentViewCamera] 相机位置更新: {transform.position}");
        }
        
        /// <summary>
        /// 计算最佳相机位置 - 考虑光照和阴影效果
        /// </summary>
        private Vector3 CalculateOptimalCameraPosition(Vector3 targetPos)
        {
            // 获取主光源方向（通常是太阳光）
            Vector3 lightDirection = GetMainLightDirection();
            
            // 计算基础位置偏移
            Vector3 baseOffset = new Vector3(0, viewHeight, -viewDistance);
            
            // 根据光照方向调整相机位置，以获得更好的光影效果
            // 相机应该在光源的侧面或对面，这样能更好地观察光影变化
            float lightAngle = Mathf.Atan2(lightDirection.x, lightDirection.z) * Mathf.Rad2Deg;
            
            // 将相机定位在光源侧面约45-60度的位置
            float cameraAngle = lightAngle + 135f; // 135度角度提供良好的光影观察效果
            
            // 应用旋转偏移
            Vector3 rotatedOffset = Quaternion.Euler(0, cameraAngle, 0) * baseOffset;
            
            // 确保相机不会太靠近地面
            Vector3 finalPosition = targetPos + rotatedOffset;
            finalPosition.y = Mathf.Max(finalPosition.y, targetPos.y + 50f);
            
            return finalPosition;
        }
        
        /// <summary>
        /// 计算最佳相机旋转 - 确保完美的俯视角度
        /// </summary>
        private void CalculateOptimalCameraRotation(Vector3 targetPos)
        {
            // 计算朝向目标的基础旋转
            Vector3 directionToTarget = (targetPos - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            
            // 应用俯视角度调整
            Vector3 eulerAngles = lookRotation.eulerAngles;
            eulerAngles.x = viewAngle; // 设置俯视角度
            
            // 应用旋转
            Quaternion finalRotation = Quaternion.Euler(eulerAngles);
            
            if (Application.isPlaying)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * 3f);
            }
            else
            {
                transform.rotation = finalRotation;
            }
        }
        
        /// <summary>
        /// 获取主光源方向
        /// </summary>
        private Vector3 GetMainLightDirection()
        {
            // 查找主光源（通常是Directional Light）
            Light mainLight = RenderSettings.sun;
            if (mainLight == null)
            {
                // 如果没有设置sun，查找第一个方向光
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional && light.enabled)
                    {
                        mainLight = light;
                        break;
                    }
                }
            }
            
            if (mainLight != null)
            {
                return -mainLight.transform.forward; // 光照方向
            }
            
            // 默认假设太阳在西南方向（下午的光照）
            return new Vector3(-0.5f, -0.7f, -0.5f).normalized;
        }

        /// <summary>
        /// 设置观察目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            terrainTarget = target;
            UpdatePosition();
        }

        /// <summary>
        /// 设置观察参数
        /// </summary>
        public void SetViewParameters(float height, float distance, float angle)
        {
            viewHeight = height;
            viewDistance = distance;
            viewAngle = angle;
            
            UpdatePosition();
            
            Debug.Log($"[EnvironmentViewCamera] 更新观察参数 - 高度:{height}, 距离:{distance}, 角度:{angle}");
        }

        /// <summary>
        /// 地形全景模式 - 查看整个地形全貌（强化版）
        /// </summary>
        [ContextMenu("地形全景模式")]
        public void SetTerrainPanoramaMode()
        {
            Debug.Log("[EnvironmentViewCamera] ==========================================");
            Debug.Log("[EnvironmentViewCamera] 启动地形全景模式...");
            
            // 重新计算最佳参数
            FindOptimalPosition();
            
            // 针对全景观察的大幅度调整 - 确保能看到整个地形
            viewHeight *= 1.5f; // 增加50%高度
            viewDistance *= 1.3f; // 增加30%距离  
            viewAngle = Mathf.Max(viewAngle, 45f); // 更大的俯视角度
            
            // 确保视野角度足够大
            fieldOfView = Mathf.Max(fieldOfView, 80f);
            
            if (cam != null)
            {
                cam.fieldOfView = fieldOfView;
                // 立即应用相机设置
                SetupCamera();
            }
            
            // 立即更新位置，不使用插值
            if (terrainTarget != null)
            {
                Vector3 targetPos = terrainTarget.position;
                Vector3 cameraPos = CalculateOptimalCameraPosition(targetPos);
                transform.position = cameraPos;
                CalculateOptimalCameraRotation(targetPos);
            }
            
            Debug.Log($"[EnvironmentViewCamera] 全景模式参数:");
            Debug.Log($"  - 最终高度: {viewHeight:F1}m");
            Debug.Log($"  - 最终距离: {viewDistance:F1}m"); 
            Debug.Log($"  - 最终角度: {viewAngle:F1}°");
            Debug.Log($"  - 最终FOV: {fieldOfView:F1}°");
            Debug.Log($"  - 相机位置: {transform.position}");
            Debug.Log("[EnvironmentViewCamera] ==========================================");
        }

        /// <summary>
        /// 光影观察模式 - 最佳光照和阴影观察角度
        /// </summary>
        [ContextMenu("光影观察模式")]
        public void SetLightingObservationMode()
        {
            // 动态调整以获得最佳光照观察效果
            Vector3 lightDir = GetMainLightDirection();
            
            // 根据光照方向优化观察参数
            viewAngle = 25f; // 较浅的角度以更好观察阴影
            
            if (cam != null)
            {
                cam.fieldOfView = 65f; // 适中的视野角度
            }
            
            UpdatePosition();
            Debug.Log($"[EnvironmentViewCamera] 已切换到光影观察模式 - 光照方向: {lightDir}");
        }

        /// <summary>
        /// 动态跟踪模式 - 跟踪光照和环境变化
        /// </summary>
        [ContextMenu("动态跟踪模式")]
        public void SetDynamicTrackingMode()
        {
            followTerrain = true;
            autoPositioning = true;
            
            // 启动动态调整协程
            if (Application.isPlaying)
            {
                StartCoroutine(DynamicPositionAdjustment());
            }
            
            Debug.Log("[EnvironmentViewCamera] 已启用动态跟踪模式");
        }
        
        /// <summary>
        /// 动态位置调整协程
        /// </summary>
        private System.Collections.IEnumerator DynamicPositionAdjustment()
        {
            while (followTerrain)
            {
                // 每30秒重新评估最佳位置
                yield return new WaitForSeconds(30f);
                
                if (autoPositioning)
                {
                    Debug.Log("[EnvironmentViewCamera] 动态调整相机位置...");
                    FindOptimalPosition();
                }
            }
        }

        /// <summary>
        /// 性能优化模式 - 降低渲染负载但保持观察效果
        /// </summary>
        [ContextMenu("性能优化模式")]
        public void SetPerformanceMode()
        {
            if (cam != null)
            {
                cam.allowMSAA = false; // 关闭抗锯齿以提高性能
                cam.renderingPath = RenderingPath.Forward; // 使用前向渲染
                cam.useOcclusionCulling = true;
                
                // 适当降低视野角度减少渲染负载
                cam.fieldOfView = Mathf.Min(cam.fieldOfView, 60f);
            }
            
            Debug.Log("[EnvironmentViewCamera] 已启用性能优化模式");
        }

        /// <summary>
        /// 质量优先模式 - 最佳视觉效果，忽略性能
        /// </summary>
        [ContextMenu("质量优先模式")]
        public void SetQualityMode()
        {
            if (cam != null)
            {
                cam.allowMSAA = true; // 启用抗锯齿
                cam.allowHDR = true; // 启用HDR
                cam.renderingPath = RenderingPath.UsePlayerSettings; // 使用项目设置
                
                // 更大的视野角度以获得更好的观察效果
                cam.fieldOfView = Mathf.Max(fieldOfView, 75f);
            }
            
            Debug.Log("[EnvironmentViewCamera] 已启用质量优先模式");
        }

        /// <summary>
        /// 紧急广角模式 - 最大视野确保看到所有地形
        /// </summary>
        [ContextMenu("紧急广角模式")]
        public void SetEmergencyWideAngleMode()
        {
            Debug.Log("[EnvironmentViewCamera] ==========================================");
            Debug.Log("[EnvironmentViewCamera] 启动紧急广角模式 - 强制显示全地形！");
            
            // 使用极端参数确保能看到整个地形
            viewHeight = 500f;    // 极高的观察高度
            viewDistance = 600f;  // 极远的观察距离
            viewAngle = 60f;      // 极大的俯视角度
            fieldOfView = 90f;    // 最大的视野角度
            
            if (cam != null)
            {
                cam.fieldOfView = fieldOfView;
                cam.farClipPlane = 2000f; // 增加远裁剪面
                SetupCamera();
            }
            
            // 立即设置相机位置，不依赖地形检测
            if (terrainTarget == null)
            {
                GameObject targetGO = new GameObject("Emergency Target");
                targetGO.transform.position = Vector3.zero;
                terrainTarget = targetGO.transform;
            }
            
            // 直接计算广角位置
            Vector3 targetPos = terrainTarget.position;
            Vector3 offset = new Vector3(0, viewHeight, -viewDistance);
            transform.position = targetPos + offset;
            
            // 直接设置朝向
            transform.LookAt(targetPos);
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.x = viewAngle;
            transform.rotation = Quaternion.Euler(eulerAngles);
            
            Debug.Log($"[EnvironmentViewCamera] 紧急广角模式设置完成:");
            Debug.Log($"  - 极限高度: {viewHeight}m");
            Debug.Log($"  - 极限距离: {viewDistance}m");
            Debug.Log($"  - 极限角度: {viewAngle}°");
            Debug.Log($"  - 极限FOV: {fieldOfView}°");
            Debug.Log($"  - 相机位置: {transform.position}");
            Debug.Log($"  - 相机旋转: {transform.rotation.eulerAngles}");
            Debug.Log("[EnvironmentViewCamera] 如果还是看不全，请检查地形实际尺寸！");
            Debug.Log("[EnvironmentViewCamera] ==========================================");
        }

        /// <summary>
        /// 完全重新初始化相机 - 解决所有渲染问题
        /// </summary>
        [ContextMenu("完全重新初始化")]
        public void ForceReinitializeCamera()
        {
            Debug.Log("[EnvironmentViewCamera] ==========================================");
            Debug.Log("[EnvironmentViewCamera] 开始完全重新初始化相机系统...");
            
            // 第一步：停用所有其他相机避免冲突
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var camera in allCameras)
            {
                if (camera != cam && camera.depth >= 0)
                {
                    camera.depth = -10; // 降低其他相机优先级
                    Debug.Log($"[EnvironmentViewCamera] 降低相机优先级: {camera.name}");
                }
            }
            
            // 第二步：完全重新设置相机组件
            SetupCamera();
            
            // 第三步：强制切换到地形全景模式（最佳的观察模式）
            SetTerrainPanoramaMode();
            
            // 第四步：验证相机状态
            if (cam != null)
            {
                // 强制刷新相机状态
                cam.enabled = false;
                cam.enabled = true;
                
                // 立即更新位置（不使用插值）
                if (terrainTarget != null)
                {
                    Vector3 targetPos = terrainTarget.position;
                    Vector3 cameraPos = CalculateOptimalCameraPosition(targetPos);
                    transform.position = cameraPos;
                    CalculateOptimalCameraRotation(targetPos);
                }
                
                // 输出完整的相机状态报告
                Debug.Log("[EnvironmentViewCamera] ==========================================");
                Debug.Log("[EnvironmentViewCamera] 相机系统完全重新初始化完成！");
                Debug.Log($"[EnvironmentViewCamera] 相机状态报告:");
                Debug.Log($"  ✓ 相机启用: {cam.enabled}");
                Debug.Log($"  ✓ 相机标签: {cam.tag}");
                Debug.Log($"  ✓ 相机深度: {cam.depth}");
                Debug.Log($"  ✓ 位置: {transform.position}");
                Debug.Log($"  ✓ 旋转: {transform.rotation.eulerAngles}");
                Debug.Log($"  ✓ 视野角度: {cam.fieldOfView}°");
                Debug.Log($"  ✓ 渲染层级: {Convert.ToString(cam.cullingMask, 2)}");
                Debug.Log($"  ✓ 清除标志: {cam.clearFlags}");
                Debug.Log($"  ✓ HDR: {cam.allowHDR}");
                Debug.Log($"  ✓ 抗锯齿: {cam.allowMSAA}");
                Debug.Log($"  ✓ 远裁剪面: {cam.farClipPlane}");
                Debug.Log($"  ✓ 近裁剪面: {cam.nearClipPlane}");
                
                if (terrainTarget != null)
                {
                    Debug.Log($"  ✓ 观察目标: {terrainTarget.position}");
                    Debug.Log($"  ✓ 观察距离: {Vector3.Distance(transform.position, terrainTarget.position):F1}m");
                }
                
                Debug.Log("[EnvironmentViewCamera] ==========================================");
                Debug.Log("[EnvironmentViewCamera] 如果相机仍有问题，请检查:");
                Debug.Log("[EnvironmentViewCamera] 1. 场景中是否有地形对象");
                Debug.Log("[EnvironmentViewCamera] 2. 是否有其他相机干扰");
                Debug.Log("[EnvironmentViewCamera] 3. 渲染管线设置是否正确");
            }
            else
            {
                Debug.LogError("[EnvironmentViewCamera] 相机组件创建失败！");
            }
        }
        
        /// <summary>
        /// 快速诊断相机问题
        /// </summary>
        [ContextMenu("诊断相机问题")]
        public void DiagnoseCameraIssues()
        {
            Debug.Log("[EnvironmentViewCamera] ==========================================");
            Debug.Log("[EnvironmentViewCamera] 开始诊断相机问题...");
            
            // 检查相机组件
            if (cam == null)
            {
                Debug.LogError("[诊断] ❌ 相机组件缺失");
                return;
            }
            else
            {
                Debug.Log("[诊断] ✓ 相机组件正常");
            }
            
            // 检查相机启用状态
            Debug.Log($"[诊断] 相机启用状态: {(cam.enabled ? "✓" : "❌")} {cam.enabled}");
            
            // 检查地形对象
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            Debug.Log($"[诊断] 发现地形数量: {terrains.Length}");
            
            // 检查其他相机
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Debug.Log($"[诊断] 场景中相机总数: {allCameras.Length}");
            foreach (var camera in allCameras)
            {
                Debug.Log($"[诊断] 相机: {camera.name}, 深度: {camera.depth}, 启用: {camera.enabled}");
            }
            
            // 检查渲染设置
            Debug.Log($"[诊断] 渲染管线: {GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in RP"}");
            Debug.Log($"[诊断] 天空盒: {RenderSettings.skybox?.name ?? "无"}");
            
            Debug.Log("[EnvironmentViewCamera] ==========================================");
        }

        // 在Inspector中实时调整参数
        void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdatePosition();
                
                if (cam != null)
                {
                    cam.fieldOfView = fieldOfView;
                    cam.farClipPlane = farClipPlane;
                }
            }
        }

        // 在Scene视图中绘制辅助线
        void OnDrawGizmos()
        {
            if (terrainTarget != null)
            {
                // 绘制到目标的连线
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, terrainTarget.position);
                
                // 绘制视野范围
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(terrainTarget.position, 5f);
            }
        }
    }
}