using UnityEngine;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 植被系统测试和验证
    /// </summary>
    public class VegetationSystemTest : MonoBehaviour
    {
        [Header("测试设置")]
        public bool runTestOnStart = false;
        public VegetationType testVegetationType = VegetationType.针叶树;
        
        void Start()
        {
            if (runTestOnStart)
            {
                TestAssetVegetationSystem();
            }
        }
        
        [ContextMenu("测试资产植被系统")]
        public void TestAssetVegetationSystem()
        {
            Debug.Log("[VegetationSystemTest] 开始测试资产植被系统...");
            
            // 获取智能放置系统
            var smartPlacementSystem = GetComponent<SmartPlacementSystem>();
            if (smartPlacementSystem == null)
            {
                Debug.LogError("[VegetationSystemTest] 未找到SmartPlacementSystem组件！");
                return;
            }
            
            // 获取植被库数据
            var vegetationData = smartPlacementSystem.VegetationSystem?.Library?.GetVegetationData(testVegetationType);
            if (vegetationData == null)
            {
                Debug.LogError($"[VegetationSystemTest] 未找到 {testVegetationType} 的植被数据！");
                return;
            }
            
            var availablePrefabs = vegetationData.GetAllPrefabs();
            if (availablePrefabs.Count == 0)
            {
                Debug.LogError($"[VegetationSystemTest] {testVegetationType} 没有配置预制件资产！请在植被库中添加预制件。");
                return;
            }
            
            // 随机选择一个预制件进行测试
            GameObject selectedPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
            
            if (selectedPrefab != null)
            {
                Debug.Log($"[VegetationSystemTest] 成功为 {testVegetationType} 选择了预制件: {selectedPrefab.name}");
                
                // 创建测试实例
                Vector3 testPosition = transform.position + Vector3.right * 5f;
                GameObject testInstance = Instantiate(selectedPrefab, testPosition, Random.rotation);
                testInstance.name = $"Test_{testVegetationType}_{Time.time:F2}";
                
                Debug.Log($"[VegetationSystemTest] 成功创建测试实例: {testInstance.name}");
            }
            else
            {
                Debug.LogError($"[VegetationSystemTest] 预制件引用为空: {testVegetationType}！");
            }
        }
        
        [ContextMenu("测试所有植被类型")]
        public void TestAllVegetationTypes()
        {
            Debug.Log("[VegetationSystemTest] 开始测试所有17种植被类型...");
            
            var smartPlacementSystem = GetComponent<SmartPlacementSystem>();
            if (smartPlacementSystem == null)
            {
                Debug.LogError("[VegetationSystemTest] 未找到SmartPlacementSystem组件！");
                return;
            }
            
            var allTypes = System.Enum.GetValues(typeof(VegetationType));
            Vector3 basePosition = transform.position;
            
            int successCount = 0;
            int index = 0;
            
            foreach (VegetationType vegType in allTypes)
            {
                Vector3 testPos = basePosition + new Vector3(index * 3f, 0, 0);
                
                // 获取植被数据
                var vegetationData = smartPlacementSystem.VegetationSystem?.Library?.GetVegetationData(vegType);
                if (vegetationData != null)
                {
                    var availablePrefabs = vegetationData.GetAllPrefabs();
                    if (availablePrefabs.Count > 0)
                    {
                        GameObject prefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
                        if (prefab != null)
                        {
                            GameObject instance = Instantiate(prefab, testPos, Quaternion.identity);
                            instance.name = $"Test_{vegType}";
                            successCount++;
                            Debug.Log($"[VegetationSystemTest] ✅ {vegType} - 成功 ({prefab.name})");
                        }
                        else
                        {
                            Debug.LogWarning($"[VegetationSystemTest] ❌ {vegType} - 预制件引用为空");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[VegetationSystemTest] ❌ {vegType} - 没有配置预制件");
                    }
                }
                else
                {
                    Debug.LogWarning($"[VegetationSystemTest] ❌ {vegType} - 没有植被数据");
                }
                
                index++;
            }
            
            Debug.Log($"[VegetationSystemTest] 测试完成！成功: {successCount}/{allTypes.Length}");
        }
        
        [ContextMenu("清除测试实例")]
        public void ClearTestInstances()
        {
            var testObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int cleared = 0;
            
            foreach (var obj in testObjects)
            {
                if (obj.name.StartsWith("Test_"))
                {
                    DestroyImmediate(obj);
                    cleared++;
                }
            }
            
            Debug.Log($"[VegetationSystemTest] 已清除 {cleared} 个测试实例");
        }
    }
}