# WorldEditor 用户指南

## 目录
1. [系统概述](#系统概述)
2. [基础操作](#基础操作)
3. [地形生成系统](#地形生成系统)
4. [智能放置系统](#智能放置系统)
5. [环境天气系统](#环境天气系统)
6. [性能优化](#性能优化)
7. [高级功能](#高级功能)
8. [最佳实践](#最佳实践)
9. [问题解决](#问题解决)

---

## 系统概述

WorldEditor 是一个集成的世界创建工具，包含以下主要系统：

### 🏔️ 地形生成系统
- 程序化地形生成
- 多层噪声系统
- 地质模拟和侵蚀
- 生物群落支持

### 🌿 智能放置系统  
- AI驱动的植被放置
- 生态系统模拟
- 地形适应性分析
- 自然分布算法

### 🌦️ 环境天气系统
- 物理天气模拟
- 动态日夜循环
- 季节变化
- 实时光照控制

### ⚡ 性能优化系统
- LOD管理
- 流式加载
- 内存优化
- 性能监控

---

## 基础操作

### 打开WorldEditor窗口

1. **通过菜单栏**：
   ```
   菜单栏 → 世界编辑器 → 打开WorldEditor
   ```

2. **快捷键**：
   ```
   Ctrl+Shift+W (Windows)
   Cmd+Shift+W (Mac)
   ```

### WorldEditor界面布局

WorldEditor窗口包含以下标签页：

- **项目总览**: 系统状态和快速操作
- **地形生成**: 地形参数配置和生成控制  
- **智能放置**: 植被和物体放置设置
- **环境天气**: 天气和光照控制
- **AI生成**: AI辅助功能（未来版本）
- **性能优化**: 性能分析和优化工具
- **设置**: 全局配置选项

### 初始化系统

首次使用时需要初始化系统：

1. 打开WorldEditor窗口
2. 在"项目总览"标签中检查系统状态
3. 点击"创建"按钮来创建缺失的组件
4. 点击"刷新系统"确保所有组件正常加载

---

## 地形生成系统

### 基本地形生成

#### 步骤1：配置地形参数

1. 切换到"地形生成"标签页
2. 设置基础参数：
   ```
   地形大小: 1000x1000m
   高度范围: 0-600m
   分辨率: 513x513
   ```

3. 配置噪声设置：
   ```
   噪声类型: Perlin
   频率: 0.01
   振幅: 1.0
   八度数: 4
   ```

#### 步骤2：选择生物群落

```csharp
// 可用的生物群落类型
public enum BiomeType
{
    Temperate,    // 温带
    Desert,       // 沙漠
    Tropical,     // 热带
    Arctic,       // 极地
    Mountain,     // 山地
    Coastal       // 海岸
}
```

#### 步骤3：生成地形

1. 点击"生成地形"按钮
2. 等待生成过程完成
3. 在Scene视图中查看结果

### 高级地形功能

#### 多层噪声系统

```csharp
// 在WorldEditorSettings中配置
[SerializeField] private List<NoiseLayerSettings> noiseLayers = new List<NoiseLayerSettings>
{
    new NoiseLayerSettings 
    { 
        noiseType = NoiseType.Perlin,
        frequency = 0.01f,
        amplitude = 1.0f,
        octaves = 4
    },
    new NoiseLayerSettings 
    { 
        noiseType = NoiseType.Ridged,
        frequency = 0.005f,
        amplitude = 0.5f,
        octaves = 3
    }
};
```

#### 地质模拟

启用地质特征：

1. 在地形设置中启用"地质模拟"
2. 配置侵蚀参数：
   ```
   侵蚀迭代: 50
   侵蚀强度: 0.3
   沉积率: 0.4
   蒸发率: 0.01
   ```

3. 设置岩石硬度：
   ```
   花岗岩: 0.9
   砂岩: 0.6
   页岩: 0.4
   ```

#### 河流生成

自动生成河流网络：

1. 启用"河流生成"选项
2. 设置参数：
   ```
   河流数量: 3-5条
   河流宽度: 2-10m
   河流深度: 1-3m
   汇流阈值: 0.1
   ```

3. 配置河流材质和效果

---

## 智能放置系统

### 植被放置基础

#### 步骤1：准备植被预制体

创建植被预制体并配置：

```csharp
[CreateAssetMenu(menuName = "WorldEditor/Vegetation Profile")]
public class VegetationProfile : ScriptableObject
{
    public GameObject[] trees;          // 树木预制体
    public GameObject[] bushes;         // 灌木预制体
    public GameObject[] grass;          // 草类预制体
    public GameObject[] flowers;        // 花卉预制体
}
```

#### 步骤2：配置放置规则

```csharp
[System.Serializable]
public class PlacementRule
{
    public BiomeType biome;             // 适用的生物群落
    public float minSlope = 0f;         // 最小坡度
    public float maxSlope = 30f;        // 最大坡度
    public float minAltitude = 0f;      // 最小海拔
    public float maxAltitude = 1000f;   // 最大海拔
    public float density = 1f;          // 密度因子
    public float clusteringFactor = 0.5f; // 聚集度
}
```

#### 步骤3：执行智能放置

1. 切换到"智能放置"标签页
2. 选择植被配置文件
3. 调整密度和分布参数
4. 点击"放置植被"按钮

### 生态系统模拟

#### 植被竞争模型

系统模拟植被之间的竞争：

```csharp
public class PlantCompetition
{
    public float lightCompetition;      // 光照竞争
    public float waterCompetition;      // 水分竞争
    public float nutrientCompetition;   // 营养竞争
    public float spaceCompetition;      // 空间竞争
    
    // 计算存活概率
    public float CalculateSurvivalRate()
    {
        float totalCompetition = (lightCompetition + waterCompetition + 
                                 nutrientCompetition + spaceCompetition) / 4f;
        return Mathf.Clamp01(1f - totalCompetition);
    }
}
```

#### 自然演替

启用自然演替模拟：

1. 在放置系统中启用"演替模拟"
2. 设置演替参数：
   ```
   演替速度: 0.1（慢）- 1.0（快）
   气候稳定性: 0.8
   干扰频率: 0.1
   ```

3. 观察植被群落随时间的变化

### 地形分析

系统自动分析地形特征：

#### 坡度分析
```csharp
public float CalculateSlope(Vector3 position, TerrainData terrain)
{
    float height = terrain.GetInterpolatedHeight(position.x, position.z);
    Vector3 normal = terrain.GetInterpolatedNormal(position.x, position.z);
    return Vector3.Angle(normal, Vector3.up);
}
```

#### 朝向分析
```csharp
public enum Aspect
{
    North,    // 北坡（阴坡）
    South,    // 南坡（阳坡）
    East,     // 东坡（晨坡）
    West,     // 西坡（暮坡）
    Flat      // 平地
}
```

#### 湿度分析
```csharp
public float CalculateMoisture(Vector3 position)
{
    float baseMetrics = 0.5f;
    float altitudeEffect = Mathf.Clamp01(1f - position.y / 1000f);
    float slopeEffect = 1f - (CalculateSlope(position) / 90f);
    float proximityToWater = CalculateWaterProximity(position);
    
    return (baseMetrics + altitudeEffect + slopeEffect + proximityToWater) / 4f;
}
```

---

## 环境天气系统

### 天气控制

#### 基本天气类型

```csharp
public enum WeatherType
{
    Clear,    // 晴朗
    Cloudy,   // 多云
    Rainy,    // 雨天
    Stormy,   // 暴风雨
    Foggy,    // 雾天
    Snowy     // 雪天
}
```

#### 设置天气

1. **通过编辑器界面**：
   - 切换到"环境天气"标签页
   - 点击对应的天气按钮

2. **通过代码**：
   ```csharp
   var environmentSystem = FindObjectOfType<DynamicEnvironmentSystem>();
   environmentSystem.SetTargetWeather(WeatherType.Rainy);
   ```

### 日夜循环系统

#### 时间控制

```csharp
public enum TimeOfDay
{
    Dawn,       // 黎明 (05:00-07:00)
    Morning,    // 上午 (07:00-11:00)
    Noon,       // 正午 (11:00-14:00)
    Afternoon,  // 下午 (14:00-18:00)
    Dusk,       // 黄昏 (18:00-20:00)
    Night       // 夜晚 (20:00-05:00)
}
```

#### 配置日夜循环

```csharp
[SerializeField] private float dayDuration = 1440f;    // 24分钟 = 24小时
[SerializeField] private float timeScale = 1f;        // 时间缩放
[SerializeField] private bool pauseTime = false;      // 暂停时间
```

### 物理天气模拟

#### 大气压力系统

系统模拟真实的大气压力变化：

```csharp
public void UpdateAtmosphericPressure(float deltaTime)
{
    float altitude = cameraPosition.y;
    float standardPressure = 1013.25f; // 海平面标准大气压
    
    // 使用气压高度公式
    float pressureAtAltitude = standardPressure * 
        Mathf.Pow(1f - (altitude * 0.0065f) / 288.15f, 5.255f);
    
    currentState.atmosphericPressure = pressureAtAltitude / standardPressure;
}
```

#### 温度梯度

基于高度和纬度计算温度：

```csharp
public float CalculateTemperature(float height, float latitude)
{
    // 高度影响：每100米下降0.65度
    float heightTemperature = 15f - (height * 0.0065f);
    
    // 纬度影响
    float latitudeTemperature = Mathf.Cos(latitude * Mathf.Deg2Rad) * 20f + 10f;
    
    return (heightTemperature + latitudeTemperature) / 2f;
}
```

#### 风系统模拟

包含科里奥利效应的风模拟：

```csharp
public Vector3 CalculateWindDirection(Vector3 position)
{
    // 压力梯度力
    Vector3 pressureGradient = CalculatePressureGradient(position);
    
    // 科里奥利力
    Vector3 coriolisEffect = CalculateCoriolisEffect(currentWind, position);
    
    // 地形影响
    Vector3 terrainEffect = CalculateTerrainWindEffect(position);
    
    return (pressureGradient + coriolisEffect + terrainEffect).normalized;
}
```

### 季节变化

#### 配置季节系统

```csharp
[System.Serializable]
public class SeasonSettings
{
    public Season season;
    public float temperature;        // 平均温度
    public float precipitation;      // 降水量
    public float dayLightHours;     // 日照时长
    public Color foliageColor;      // 植被颜色
}
```

#### 季节过渡

```csharp
public void UpdateSeasonTransition()
{
    float yearProgress = GetYearProgress();  // 0-1
    Season currentSeason = GetSeasonFromProgress(yearProgress);
    
    // 平滑过渡季节参数
    SeasonSettings current = GetSeasonSettings(currentSeason);
    SeasonSettings next = GetSeasonSettings(GetNextSeason(currentSeason));
    
    float lerpFactor = GetSeasonLerpFactor(yearProgress);
    ApplyInterpolatedSeasonSettings(current, next, lerpFactor);
}
```

---

## 性能优化

### LOD系统

#### 配置LOD距离

```csharp
[SerializeField] private float[] lodDistances = { 50f, 150f, 500f, 1500f };

public void UpdateLOD(GameObject target, float distanceToCamera)
{
    int lodLevel = 0;
    for (int i = 0; i < lodDistances.Length; i++)
    {
        if (distanceToCamera > lodDistances[i])
        {
            lodLevel = i + 1;
        }
    }
    
    ApplyLODLevel(target, lodLevel);
}
```

#### 动态LOD调整

系统根据性能自动调整LOD：

```csharp
public void AutoAdjustLOD()
{
    float currentFPS = GetCurrentFPS();
    float targetFPS = 60f;
    
    if (currentFPS < targetFPS * 0.8f)
    {
        // 性能不足，增加LOD距离
        for (int i = 0; i < lodDistances.Length; i++)
        {
            lodDistances[i] *= 0.9f;
        }
    }
    else if (currentFPS > targetFPS * 1.2f)
    {
        // 性能良好，减少LOD距离
        for (int i = 0; i < lodDistances.Length; i++)
        {
            lodDistances[i] *= 1.1f;
        }
    }
}
```

### 大世界支持

#### 区块管理

```csharp
public class WorldChunk
{
    public Vector2Int coordinates;     // 区块坐标
    public GameObject chunkObject;     // 区块游戏对象
    public bool isLoaded;             // 是否已加载
    public bool isActive;             // 是否激活
    public float lastAccessTime;      // 最后访问时间
}

public void UpdateChunkLoading(Vector3 playerPosition)
{
    Vector2Int playerChunk = WorldToChunkCoords(playerPosition);
    
    // 加载周围区块
    for (int x = -loadRadius; x <= loadRadius; x++)
    {
        for (int z = -loadRadius; z <= loadRadius; z++)
        {
            Vector2Int chunkCoords = playerChunk + new Vector2Int(x, z);
            LoadChunkIfNeeded(chunkCoords);
        }
    }
    
    // 卸载远离的区块
    UnloadDistantChunks(playerChunk);
}
```

#### 流式加载

```csharp
IEnumerator LoadChunkAsync(Vector2Int chunkCoords)
{
    var chunk = new WorldChunk { coordinates = chunkCoords };
    
    // 异步生成地形
    yield return StartCoroutine(GenerateTerrainAsync(chunk));
    
    // 异步放置植被
    yield return StartCoroutine(PlaceVegetationAsync(chunk));
    
    // 激活区块
    chunk.isLoaded = true;
    chunks[chunkCoords] = chunk;
}
```

### 性能监控

#### 实时性能指标

```csharp
public class PerformanceMetrics
{
    public float averageFPS;
    public float minFPS;
    public float maxFPS;
    public float memoryUsage;
    public int drawCalls;
    public int triangleCount;
    
    public void Update()
    {
        averageFPS = 1f / Time.unscaledDeltaTime;
        memoryUsage = Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f);
        
        #if UNITY_EDITOR
        drawCalls = UnityEditor.UnityStats.drawCalls;
        triangleCount = UnityEditor.UnityStats.triangles;
        #endif
    }
}
```

#### 性能报告

```csharp
public string GeneratePerformanceReport()
{
    return $"性能报告:\n" +
           $"FPS: {metrics.averageFPS:F1} (最小: {metrics.minFPS:F1})\n" +
           $"内存: {metrics.memoryUsage:F1}MB\n" +
           $"渲染调用: {metrics.drawCalls}\n" +
           $"三角形数: {metrics.triangleCount}";
}
```

---

## 高级功能

### 自定义生物群落

#### 创建生物群落定义

```csharp
[CreateAssetMenu(menuName = "WorldEditor/Biome Definition")]
public class CustomBiome : BiomeDefinition
{
    [Header("气候参数")]
    public float temperature = 20f;
    public float precipitation = 0.5f;
    public float humidity = 0.6f;
    
    [Header("植被配置")]
    public VegetationProfile vegetationProfile;
    public float vegetationDensity = 1f;
    
    public override void ApplyBiomeSettings(TerrainData terrain, Vector2 position)
    {
        // 修改地形纹理
        ApplyTerrainTextures(terrain, position);
        
        // 设置植被参数
        ApplyVegetationSettings(position);
        
        // 配置环境参数
        ApplyEnvironmentSettings();
    }
}
```

### 自定义天气效果

#### 扩展天气系统

```csharp
public class CustomWeatherEffect : MonoBehaviour
{
    [Header("天气参数")]
    public WeatherType weatherType = WeatherType.Clear;
    public float intensity = 1f;
    
    [Header("视觉效果")]
    public ParticleSystem particles;
    public Light lighting;
    public AudioSource audioSource;
    
    public void UpdateWeatherEffect(float deltaTime, WeatherState weatherState)
    {
        // 更新粒子效果
        UpdateParticleEffects(weatherState);
        
        // 更新光照
        UpdateLightingEffects(weatherState);
        
        // 更新音效
        UpdateAudioEffects(weatherState);
    }
}
```

### AI辅助功能（预览）

未来版本将包含AI辅助功能：

```csharp
public class AIWorldGenerator
{
    public async Task<WorldGenerationParameters> GenerateWorldFromPrompt(string prompt)
    {
        // 使用AI分析用户描述
        var analysis = await AnalyzePrompt(prompt);
        
        // 生成参数
        var parameters = new WorldGenerationParameters();
        parameters.biome = analysis.suggestedBiome;
        parameters.terrainRoughness = analysis.suggestedRoughness;
        parameters.vegetationDensity = analysis.suggestedVegetationDensity;
        
        return parameters;
    }
}
```

---

## 最佳实践

### 性能优化建议

1. **地形系统**
   - 使用合适的地形分辨率（推荐 513x513）
   - 限制同时生成的地形区块数量
   - 启用地形LOD和遮挡剔除

2. **植被系统**
   - 使用LOD Group组件
   - 设置合理的渲染距离
   - 启用GPU Instancing
   - 使用Billboard渲染远处植被

3. **环境系统**
   - 降低粒子系统质量
   - 减少实时光照计算
   - 使用烘焙光照图
   - 优化雾效和后处理效果

4. **内存管理**
   - 定期运行垃圾回收
   - 及时释放不用的资源
   - 使用对象池技术
   - 监控内存使用情况

### 工作流程建议

1. **项目规划**
   - 明确世界规模和细节需求
   - 设定性能目标
   - 选择合适的工具配置

2. **迭代开发**
   - 先创建基础地形
   - 逐步添加植被和细节
   - 持续优化性能
   - 定期测试和调整

3. **团队协作**
   - 使用版本控制
   - 建立资源命名规范
   - 定期备份项目
   - 文档化配置参数

### 质量控制

1. **视觉质量**
   - 保持风格一致性
   - 注意植被分布的自然性
   - 合理配置光照和天气
   - 定期查看不同时间和天气下的效果

2. **性能质量**
   - 设定FPS目标并持续监控
   - 控制内存使用
   - 优化渲染调用次数
   - 测试不同硬件配置

---

## 问题解决

### 常见问题

#### 1. 地形生成失败

**症状**：点击生成地形按钮无反应或出现错误

**解决方案**：
```csharp
// 检查系统初始化
var worldManager = FindObjectOfType<WorldEditorManager>();
if (worldManager == null)
{
    Debug.LogError("WorldEditorManager 未找到，请先创建");
}

// 检查生成参数
var parameters = worldManager.GetGenerationParameters();
if (parameters == null)
{
    Debug.LogError("生成参数为空，请检查配置");
}
```

#### 2. 植被放置异常

**症状**：植被放置在不合适的位置或密度过高/过低

**解决方案**：
```csharp
// 检查地形分析
var analyzer = FindObjectOfType<TerrainAnalyzer>();
if (analyzer != null)
{
    analyzer.AnalyzeTerrain();  // 重新分析地形
}

// 调整放置规则
placementRules.density *= 0.5f;  // 降低密度
placementRules.maxSlope = 20f;   // 限制坡度
```

#### 3. 天气效果不显示

**症状**：设置天气后没有视觉效果

**解决方案**：
```csharp
// 检查环境系统
var envSystem = FindObjectOfType<DynamicEnvironmentSystem>();
if (!envSystem.enableDynamicEnvironment)
{
    envSystem.enableDynamicEnvironment = true;
}

// 检查天气控制器
var weatherController = envSystem.GetComponent<WeatherController>();
if (weatherController != null)
{
    weatherController.Initialize(envSystem);
}
```

#### 4. 性能问题

**症状**：帧率低于预期

**解决方案**：

1. **降低质量设置**
   ```csharp
   var optimizer = FindObjectOfType<WorldOptimizer>();
   optimizer.SetEnvironmentQuality(EnvironmentQuality.Medium);
   ```

2. **调整LOD距离**
   ```csharp
   float[] newLodDistances = { 25f, 75f, 250f, 750f };  // 更激进的LOD
   ```

3. **减少植被密度**
   ```csharp
   placementSettings.density *= 0.5f;
   ```

### 调试工具

#### 启用调试信息

```csharp
public class WorldEditorDebug : MonoBehaviour
{
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showPerformanceStats = true;
    [SerializeField] private bool logDetailedInfo = false;
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("WorldEditor Debug Info");
        
        // 显示系统状态
        GUILayout.Label($"地形生成器: {(FindObjectOfType<AdvancedTerrainGenerator>() != null ? "✓" : "✗")}");
        GUILayout.Label($"智能放置: {(FindObjectOfType<SmartPlacementSystem>() != null ? "✓" : "✗")}");
        GUILayout.Label($"环境系统: {(FindObjectOfType<DynamicEnvironmentSystem>() != null ? "✓" : "✗")}");
        
        // 显示性能信息
        if (showPerformanceStats)
        {
            GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F1}");
            GUILayout.Label($"内存: {(GC.GetTotalMemory(false) / 1024f / 1024f):F1}MB");
        }
        
        GUILayout.EndArea();
    }
}
```

#### 性能分析

```csharp
[MenuItem("世界编辑器/性能分析")]
static void OpenPerformanceAnalysis()
{
    var analyzer = FindObjectOfType<PerformanceProfiler>();
    if (analyzer == null)
    {
        var go = new GameObject("Performance Analyzer");
        analyzer = go.AddComponent<PerformanceProfiler>();
    }
    
    analyzer.StartProfiling();
}
```

### 获取支持

如果遇到无法解决的问题：

1. **检查日志**
   - Unity Console窗口
   - 系统日志文件
   - WorldEditor调试输出

2. **收集信息**
   - Unity版本
   - 系统配置
   - 错误信息截图
   - 重现步骤

3. **寻求帮助**
   - 查看在线文档
   - 搜索已知问题
   - 提交问题报告

---

## 结语

WorldEditor是一个功能强大的世界创建工具，通过合理配置和使用，能够创建出超越现有工具的精美世界。

关键要点：
- 🎯 明确目标，合理规划
- ⚡ 注重性能，持续优化
- 🔄 迭代开发，逐步完善
- 📊 监控指标，数据驱动

希望本指南能帮助您充分发挥WorldEditor的潜力，创造出令人惊叹的虚拟世界！

---

*更新日期：2025年8月18日*  
*版本：v1.0.0*