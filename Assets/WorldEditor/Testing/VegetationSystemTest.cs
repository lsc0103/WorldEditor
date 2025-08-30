using UnityEngine;
using WorldEditor.Placement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldEditor.Testing
{
    /// <summary>
    /// 植被系统测试脚本
    /// 用于测试北欧云杉的生成和放置功能
    /// </summary>
    public class VegetationSystemTest : MonoBehaviour
    {
        [Header("测试设置")]
        public Terrain targetTerrain;
        public SmartPlacementSystem placementSystem;
        
        [Header("测试参数")]
        public VegetationType testVegetationType = VegetationType.针叶树;
        public Vector3 testPosition = new Vector3(0, 0, 0);
        public float testBrushSize = 5f;
        public float testDensity = 0.3f;
        
        [Header("测试控制")]
        public bool autoFindTerrain = true;
        public bool autoFindPlacementSystem = true;
        
        void Start()
        {
            Debug.Log("[VegetationSystemTest] 测试脚本启动");
            
            if (autoFindTerrain && targetTerrain == null)
            {
                targetTerrain = FindFirstObjectByType<Terrain>();
                if (targetTerrain != null)
                {
                    Debug.Log($"[VegetationSystemTest] 自动找到地形: {targetTerrain.name}");
                }
            }
            
            if (autoFindPlacementSystem && placementSystem == null)
            {
                placementSystem = FindFirstObjectByType<SmartPlacementSystem>();
                if (placementSystem != null)
                {
                    Debug.Log($"[VegetationSystemTest] 自动找到智能放置系统: {placementSystem.name}");
                }
            }
        }
        
        [ContextMenu("测试创建北欧云杉")]
        public void TestCreateNordicSpruce()
        {
            if (ValidateTestConditions())
            {
                Debug.Log("[VegetationSystemTest] 开始测试创建真实北欧云杉...");
                
                // 设置植被类型为针叶树
                placementSystem.SetSelectedVegetationType(VegetationType.针叶树);
                
                // 设置画笔参数（精确放置）
                placementSystem.SetVegetationBrushSettings(3f, 0.2f);
                
                // 激活植被绘制
                placementSystem.ActivateVegetationPainting(true);
                
                // 在测试位置放置植被
                if (targetTerrain != null)
                {
                    Vector3 terrainCenter = targetTerrain.transform.position + 
                                          new Vector3(targetTerrain.terrainData.size.x * 0.5f, 0, 
                                                    targetTerrain.terrainData.size.z * 0.5f);
                    
                    Vector3 testPos = testPosition == Vector3.zero ? terrainCenter : testPosition;
                    
                    Debug.Log($"[VegetationSystemTest] 在位置 {testPos} 放置真实北欧云杉");
                    placementSystem.PaintVegetationAt(testPos, targetTerrain);
                    
                    // 再放置几棵树形成小树林
                    for (int i = 1; i <= 3; i++)
                    {
                        Vector3 offset = new Vector3(i * 8f, 0, i * 6f);
                        placementSystem.PaintVegetationAt(testPos + offset, targetTerrain);
                    }
                }
                
                Debug.Log("[VegetationSystemTest] 真实北欧云杉创建测试完成");
            }
        }
        
        [ContextMenu("测试游戏级北欧云杉森林")]
        public void TestGameQualitySpruceForest()
        {
            if (ValidateTestConditions())
            {
                Debug.Log("[VegetationSystemTest] 开始创建游戏级北欧云杉森林...");
                
                placementSystem.SetSelectedVegetationType(VegetationType.针叶树);
                placementSystem.SetVegetationBrushSettings(15f, 0.8f);
                placementSystem.ActivateVegetationPainting(true);
                
                if (targetTerrain != null)
                {
                    Vector3 terrainCenter = targetTerrain.transform.position + 
                                          new Vector3(targetTerrain.terrainData.size.x * 0.5f, 0, 
                                                    targetTerrain.terrainData.size.z * 0.5f);
                    
                    // 创建一个小森林区域
                    for (int x = -2; x <= 2; x++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            Vector3 forestPos = terrainCenter + new Vector3(x * 20f, 0, z * 20f);
                            placementSystem.PaintVegetationAt(forestPos, targetTerrain);
                        }
                    }
                }
                
                Debug.Log("[VegetationSystemTest] 游戏级北欧云杉森林创建完成");
            }
        }
        
        [ContextMenu("测试单棵完美云杉")]
        public void TestPerfectSingleSpruce()
        {
            if (ValidateTestConditions())
            {
                Debug.Log("[VegetationSystemTest] 开始创建单棵完美挪威云杉（用于详细检查）...");
                
                // 设置最佳参数
                placementSystem.SetSelectedVegetationType(VegetationType.针叶树);
                placementSystem.SetVegetationBrushSettings(1f, 0.1f); // 超精确放置
                placementSystem.ActivateVegetationPainting(true);
                
                if (targetTerrain != null)
                {
                    Vector3 terrainCenter = targetTerrain.transform.position + 
                                          new Vector3(targetTerrain.terrainData.size.x * 0.5f, 0, 
                                                    targetTerrain.terrainData.size.z * 0.5f);
                    
                    Debug.Log($"[VegetationSystemTest] 在地形中心创建完美云杉: {terrainCenter}");
                    placementSystem.PaintVegetationAt(terrainCenter, targetTerrain);
                    
                    // 聚焦到创建的树木
#if UNITY_EDITOR
                    Selection.activeGameObject = null;
                    EditorGUIUtility.PingObject(placementSystem.gameObject);
#endif
                }
                
                Debug.Log("[VegetationSystemTest] 完美挪威云杉创建完成 - 请检查详细结构");
            }
        }
        
        [ContextMenu("测试多个植被创建")]
        public void TestMultipleVegetationCreation()
        {
            if (ValidateTestConditions())
            {
                Debug.Log("[VegetationSystemTest] 开始测试多个植被创建...");
                
                VegetationType[] testTypes = { 
                    VegetationType.针叶树, 
                    VegetationType.阔叶树, 
                    VegetationType.野草 
                };
                
                placementSystem.ActivateVegetationPainting(true);
                
                Vector3 basePosition = targetTerrain.transform.position + 
                                     new Vector3(targetTerrain.terrainData.size.x * 0.5f, 0, 
                                               targetTerrain.terrainData.size.z * 0.5f);
                
                for (int i = 0; i < testTypes.Length; i++)
                {
                    placementSystem.SetSelectedVegetationType(testTypes[i]);
                    Vector3 testPos = basePosition + new Vector3(i * 10f, 0, 0);
                    
                    Debug.Log($"[VegetationSystemTest] 测试植被类型: {testTypes[i]} 在位置: {testPos}");
                    placementSystem.PaintVegetationAt(testPos, targetTerrain);
                }
                
                Debug.Log("[VegetationSystemTest] 多个植被创建测试完成");
            }
        }
        
        [ContextMenu("清除所有测试植被")]
        public void ClearAllTestVegetation()
        {
            if (placementSystem != null)
            {
                Debug.Log("[VegetationSystemTest] 清除所有测试植被");
                placementSystem.ClearAllVegetation();
            }
        }
        
        [ContextMenu("显示植被统计")]
        public void ShowVegetationStats()
        {
            if (placementSystem != null)
            {
                var stats = placementSystem.GetVegetationStatistics();
                Debug.Log($"[VegetationSystemTest] 植被统计:\n{stats}");
            }
        }
        
        [ContextMenu("验证系统状态")]
        public void ValidateSystemStatus()
        {
            Debug.Log("[VegetationSystemTest] 验证系统状态...");
            
            if (targetTerrain == null)
            {
                Debug.LogError("[VegetationSystemTest] 错误 目标地形未设置");
            }
            else
            {
                Debug.Log($"[VegetationSystemTest] 成功 目标地形: {targetTerrain.name}");
                Debug.Log($"[VegetationSystemTest] 地形大小: {targetTerrain.terrainData.size}");
            }
            
            if (placementSystem == null)
            {
                Debug.LogError("[VegetationSystemTest] 错误 智能放置系统未设置");
            }
            else
            {
                Debug.Log($"[VegetationSystemTest] 成功 智能放置系统: {placementSystem.name}");
                Debug.Log($"[VegetationSystemTest] 植被绘制状态: {placementSystem.IsVegetationPainting}");
                
                if (placementSystem.VegetationSystem != null)
                {
                    Debug.Log($"[VegetationSystemTest] 成功 植被系统已初始化");
                    
                    if (placementSystem.VegetationSystem.Library != null)
                    {
                        Debug.Log($"[VegetationSystemTest] 成功 植被库已加载，包含 {placementSystem.VegetationSystem.Library.vegetationTypes?.Count ?? 0} 种植被");
                    }
                    else
                    {
                        Debug.LogWarning("[VegetationSystemTest] 警告 植被库未加载");
                    }
                }
                else
                {
                    Debug.LogError("[VegetationSystemTest] 错误 植被系统未初始化");
                }
            }
        }
        
        bool ValidateTestConditions()
        {
            if (targetTerrain == null)
            {
                Debug.LogError("[VegetationSystemTest] 测试失败: 目标地形未设置");
                return false;
            }
            
            if (placementSystem == null)
            {
                Debug.LogError("[VegetationSystemTest] 测试失败: 智能放置系统未设置");
                return false;
            }
            
            return true;
        }
        
        void Update()
        {
            // 提供运行时快捷键测试
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestCreateNordicSpruce();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearAllTestVegetation();
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShowVegetationStats();
            }
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("植被系统测试控制", GUI.skin.box);
            
            if (GUILayout.Button("测试创建北欧云杉 (T)"))
            {
                TestCreateNordicSpruce();
            }
            
            if (GUILayout.Button("创建云杉森林"))
            {
                TestGameQualitySpruceForest();
            }
            
            if (GUILayout.Button("单棵完美云杉"))
            {
                TestPerfectSingleSpruce();
            }
            
            if (GUILayout.Button("测试多个植被创建"))
            {
                TestMultipleVegetationCreation();
            }
            
            if (GUILayout.Button("清除所有植被 (C)"))
            {
                ClearAllTestVegetation();
            }
            
            if (GUILayout.Button("显示统计 (S)"))
            {
                ShowVegetationStats();
            }
            
            if (GUILayout.Button("验证系统状态"))
            {
                ValidateSystemStatus();
            }
            
            GUILayout.Label($"地形: {(targetTerrain?.name ?? "未设置")}");
            GUILayout.Label($"放置系统: {(placementSystem?.name ?? "未设置")}");
            
            GUILayout.EndArea();
        }
    }
}