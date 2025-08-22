using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 印章库 - 管理所有可用的地形印章
    /// </summary>
    [CreateAssetMenu(fileName = "New Stamp Library", menuName = "WorldEditor/Stamp Library")]
    public class StampLibrary : ScriptableObject
    {
        [Header("印章库信息")]
        public string libraryName = "Default Stamp Library";
        [TextArea(2, 4)]
        public string description = "默认印章库，包含常用的地形印章";
        
        [Header("印章集合")]
        [SerializeField] private List<Stamp> stamps = new List<Stamp>();
        
        [Header("分类设置")]
        [SerializeField] private bool groupByCategory = true;
        [SerializeField] private bool showPreviewIcons = true;
        
        /// <summary>
        /// 初始化默认印章
        /// </summary>
        public void InitializeDefaultStamps()
        {
            Debug.Log("[StampLibrary] 初始化默认印章库");
            
            // 这里可以添加一些程序化生成的默认印章
            // 或者从Resources目录加载预制的印章
            CreateProceduralStamps();
        }
        
        /// <summary>
        /// 强制重新创建所有印章（用于更新印章）
        /// </summary>
        public void ForceRecreateStamps()
        {
            Debug.Log("[StampLibrary] 强制重新创建所有印章");
            stamps.Clear();
            CreateProceduralStamps();
        }
        
        /// <summary>
        /// 创建程序化印章
        /// </summary>
        void CreateProceduralStamps()
        {
            // 创建简单的山峰印章
            var mountainStamp = CreateProceduralMountainStamp();
            if (mountainStamp != null)
            {
                AddStamp(mountainStamp);
            }
            
            // 创建简单的火山口印章
            var craterStamp = CreateProceduralCraterStamp();
            if (craterStamp != null)
            {
                AddStamp(craterStamp);
            }
            
            // 创建简单的山谷印章
            var valleyStamp = CreateProceduralValleyStamp();
            if (valleyStamp != null)
            {
                AddStamp(valleyStamp);
            }
            
            Debug.Log($"[StampLibrary] 创建了 {stamps.Count} 个程序化印章");
        }
        
        /// <summary>
        /// 创建程序化山峰印章（带侵蚀效果）
        /// </summary>
        Stamp CreateProceduralMountainStamp()
        {
            int resolution = 128;
            Texture2D heightTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            
            // 生成带侵蚀效果的自然山峰高度图
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);
                    
                    // 计算到中心的距离
                    float centerU = u - 0.5f;
                    float centerV = v - 0.5f;
                    float distance = Mathf.Sqrt(centerU * centerU + centerV * centerV);
                    
                    float height = 0f;
                    if (distance <= 0.5f) // 在圆形范围内
                    {
                        float normalizedDistance = distance / 0.5f; // 0到1
                        
                        // 基础山峰形状（不规则的山脊）
                        float angle = Mathf.Atan2(centerV, centerU); // 径向角度
                        
                        // 创建不规则的山脊线条
                        float ridgeNoise = 0f;
                        ridgeNoise += Mathf.PerlinNoise(angle * 3f, distance * 8f) * 0.3f; // 主要脊线
                        ridgeNoise += Mathf.PerlinNoise(angle * 8f, distance * 15f) * 0.15f; // 细节脊线
                        ridgeNoise += Mathf.PerlinNoise(u * 12f, v * 12f) * 0.1f; // 表面纹理
                        
                        // 基础高度：创建不对称的山峰
                        float asymmetry = Mathf.PerlinNoise(u * 2f, v * 2f) * 0.3f;
                        float baseHeight = Mathf.Pow(1f - normalizedDistance, 2.5f + asymmetry);
                        
                        // 添加侵蚀沟壑效果
                        float erosionChannels = 0f;
                        for (int i = 0; i < 6; i++) // 创建6条主要的侵蚀沟壑
                        {
                            float channelAngle = (i * 60f + ridgeNoise * 30f) * Mathf.Deg2Rad;
                            float channelU = Mathf.Cos(channelAngle);
                            float channelV = Mathf.Sin(channelAngle);
                            
                            // 计算到沟壑中心线的距离
                            float channelDist = Mathf.Abs(centerU * channelV - centerV * channelU);
                            
                            // 沟壑效果：距离中心越远，沟壑越深
                            float channelDepth = Mathf.Exp(-channelDist * 15f) * distance * 0.4f;
                            erosionChannels = Mathf.Max(erosionChannels, channelDepth);
                        }
                        
                        // 组合所有效果
                        height = baseHeight;
                        height += ridgeNoise * (1f - normalizedDistance) * 0.5f; // 脊线增强，靠近中心更明显
                        height -= erosionChannels; // 减去侵蚀沟壑
                        
                        // 添加细节噪声（模拟岩石纹理）
                        float rockTexture = Mathf.PerlinNoise(u * 25f, v * 25f) * 0.03f;
                        rockTexture += Mathf.PerlinNoise(u * 50f, v * 50f) * 0.015f;
                        height += rockTexture;
                        
                        // 边缘软化
                        if (normalizedDistance > 0.85f)
                        {
                            float edgeFade = (1f - normalizedDistance) / 0.15f;
                            height *= Mathf.SmoothStep(0f, 1f, edgeFade);
                        }
                        
                        // 确保山峰顶部有一定的平台感（真实山峰特征）
                        if (normalizedDistance < 0.1f)
                        {
                            float peakPlateau = Mathf.SmoothStep(0f, 1f, (0.1f - normalizedDistance) / 0.1f);
                            height = Mathf.Lerp(height, 0.95f, peakPlateau * 0.3f);
                        }
                    }
                    
                    height = Mathf.Clamp01(height);
                    
                    Color color = new Color(height, height, height, 1f);
                    heightTexture.SetPixel(x, y, color);
                }
            }
            
            heightTexture.Apply();
            
            // 创建印章对象
            var stamp = CreateInstance<Stamp>();
            stamp.name = "Procedural Mountain";
            stamp.stampName = "程序化山峰";
            stamp.description = "程序生成的自然侵蚀山峰印章";
            stamp.heightTexture = heightTexture;
            stamp.heightScale = 18f; // 18米高的山峰
            stamp.baseHeight = 0f;
            stamp.defaultSize = 150f;
            stamp.category = StampCategory.Mountain;
            stamp.recommendedBlendMode = StampBlendMode.Max;
            
            return stamp;
        }
        
        /// <summary>
        /// 创建程序化火山口印章
        /// </summary>
        Stamp CreateProceduralCraterStamp()
        {
            int resolution = 128;
            Texture2D heightTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            
            // 生成更明显的火山口高度图
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);
                    
                    float centerU = u - 0.5f;
                    float centerV = v - 0.5f;
                    float distance = Mathf.Sqrt(centerU * centerU + centerV * centerV);
                    
                    float height = 0f;
                    
                    if (distance <= 0.5f) // 在范围内
                    {
                        // 创建明显的火山口轮廓
                        if (distance <= 0.15f) // 内部凹陷区域
                        {
                            // 中心深坑：使用更大的负值创建深度凹陷
                            float craterDepth = 1f - (distance / 0.15f);
                            height = -1.5f * craterDepth; // 增加挖掘深度
                        }
                        else if (distance <= 0.35f) // 火山口边缘
                        {
                            // 火山口边缘：高耸的环形边缘
                            float edgeDistance = distance - 0.15f;
                            float edgeWidth = 0.35f - 0.15f;
                            float edgeProfile = 1f - (edgeDistance / edgeWidth);
                            height = 0.9f * Mathf.Pow(edgeProfile, 1.5f); // 高耸的边缘
                        }
                        else // 外部斜坡区域
                        {
                            // 外部斜坡：从边缘平滑过渡到周围地形
                            float slopeDistance = distance - 0.35f;
                            float slopeWidth = 0.5f - 0.35f;
                            float slopeProfile = 1f - (slopeDistance / slopeWidth);
                            height = 0.3f * Mathf.SmoothStep(0f, 1f, slopeProfile);
                        }
                        
                        // 添加一些边缘的不规则性
                        float irregularity = Mathf.PerlinNoise(u * 8f, v * 8f) * 0.1f;
                        height += irregularity * (1f - distance);
                    }
                    
                    // 将高度值映射到0-1范围（0.5为基准，向上向下都有变化）
                    height = 0.5f + height * 0.5f;
                    height = Mathf.Clamp01(height);
                    
                    Color color = new Color(height, height, height, 1f);
                    heightTexture.SetPixel(x, y, color);
                }
            }
            
            heightTexture.Apply();
            
            var stamp = CreateInstance<Stamp>();
            stamp.name = "Procedural Crater";
            stamp.stampName = "程序化火山口";
            stamp.description = "程序生成的火山口印章";
            stamp.heightTexture = heightTexture;
            stamp.heightScale = 20f; // 增加高度范围，适应更深的坑
            stamp.baseHeight = -10f; // 基准高度进一步下移，允许更深的挖掘
            stamp.defaultSize = 200f;
            stamp.category = StampCategory.Crater;
            stamp.recommendedBlendMode = StampBlendMode.Set; // 改为Set模式，直接设置高度
            
            return stamp;
        }
        
        /// <summary>
        /// 创建程序化山谷印章（自然河道式）
        /// </summary>
        Stamp CreateProceduralValleyStamp()
        {
            int resolution = 128;
            Texture2D heightTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            
            // 生成自然蜿蜒的山谷高度图
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);
                    
                    // 创建蜿蜒的河道中心线
                    float valleyProgress = u; // 沿X方向延伸
                    
                    // 添加蜿蜒曲线
                    float meander1 = Mathf.Sin(valleyProgress * Mathf.PI * 3f) * 0.15f; // 主要弯曲
                    float meander2 = Mathf.Sin(valleyProgress * Mathf.PI * 7f) * 0.08f; // 次要弯曲
                    float valleyCenterY = 0.5f + meander1 + meander2;
                    
                    // 计算到河道中心线的距离
                    float distanceToCenter = Mathf.Abs(v - valleyCenterY);
                    
                    float height = 0.5f; // 基准高度
                    
                    // 创建自然的U型山谷剖面
                    if (distanceToCenter <= 0.4f) // 山谷影响范围
                    {
                        float normalizedDist = distanceToCenter / 0.4f; // 0到1
                        
                        // 河道底部区域
                        if (distanceToCenter <= 0.1f) // 河床 - 进一步扩大河床区域
                        {
                            // 极深的河床
                            float riverBed = 1f - (distanceToCenter / 0.1f);
                            height = 0.02f + riverBed * 0.01f; // 极深挖掘
                        }
                        else if (distanceToCenter <= 0.25f) // 河岸 - 更大的河岸区域
                        {
                            // 极陡峭的河岸（悬崖式）
                            float bankDistance = distanceToCenter - 0.1f;
                            float bankWidth = 0.25f - 0.1f;
                            float bankProfile = bankDistance / bankWidth;
                            height = 0.03f + Mathf.Pow(bankProfile, 0.5f) * 0.35f; // 悬崖式陡坡
                        }
                        else // 山谷侧坡
                        {
                            // 戏剧性的山谷侧坡
                            float slopeDistance = distanceToCenter - 0.25f;
                            float slopeWidth = 0.4f - 0.25f;
                            float slopeProfile = slopeDistance / slopeWidth;
                            
                            // 强烈的凹形坡面
                            height = 0.38f + Mathf.Pow(slopeProfile, 0.5f) * 0.12f;
                        }
                        
                        // 添加河道沉积物和侵蚀细节
                        float sedimentNoise = Mathf.PerlinNoise(u * 15f, v * 8f) * 0.02f;
                        float erosionDetail = Mathf.PerlinNoise(u * 25f, v * 20f) * 0.015f;
                        
                        // 河道区域添加沉积物，山坡区域添加侵蚀纹理
                        if (distanceToCenter <= 0.1f)
                        {
                            height += sedimentNoise; // 河道沉积
                        }
                        else
                        {
                            height += erosionDetail; // 山坡侵蚀纹理
                        }
                        
                        // 添加岩石露头（偶发特征）
                        float rockOutcrop = Mathf.PerlinNoise(u * 8f, v * 6f);
                        if (rockOutcrop > 0.8f && distanceToCenter > 0.2f)
                        {
                            height += 0.03f; // 小的岩石露头
                        }
                        
                        // 山谷边缘的自然过渡
                        if (normalizedDist > 0.8f)
                        {
                            float edgeFade = (1f - normalizedDist) / 0.2f;
                            height = Mathf.Lerp(0.5f, height, Mathf.SmoothStep(0f, 1f, edgeFade));
                        }
                    }
                    
                    height = Mathf.Clamp01(height);
                    
                    Color color = new Color(height, height, height, 1f);
                    heightTexture.SetPixel(x, y, color);
                }
            }
            
            heightTexture.Apply();
            
            var stamp = CreateInstance<Stamp>();
            stamp.name = "Procedural Valley";
            stamp.stampName = "程序化山谷";
            stamp.description = "程序生成的自然蜿蜒山谷印章";
            stamp.heightTexture = heightTexture;
            stamp.heightScale = 25f; // 极大的高度范围，创造峡谷效果
            stamp.baseHeight = -18f; // 极低的基准高度，允许极深挖掘
            stamp.defaultSize = 200f; // 更大的尺寸适合山谷
            stamp.category = StampCategory.Valley;
            stamp.recommendedBlendMode = StampBlendMode.Set; // 直接设置高度，更可控
            
            return stamp;
        }
        
        /// <summary>
        /// 添加印章到库中
        /// </summary>
        public void AddStamp(Stamp stamp)
        {
            if (stamp == null) return;
            
            if (!stamps.Contains(stamp))
            {
                stamps.Add(stamp);
                Debug.Log($"[StampLibrary] 添加印章: {stamp.stampName}");
            }
        }
        
        /// <summary>
        /// 从库中移除印章
        /// </summary>
        public void RemoveStamp(Stamp stamp)
        {
            if (stamps.Remove(stamp))
            {
                Debug.Log($"[StampLibrary] 移除印章: {stamp.stampName}");
            }
        }
        
        /// <summary>
        /// 获取所有印章
        /// </summary>
        public List<Stamp> GetAllStamps()
        {
            return new List<Stamp>(stamps.Where(s => s != null));
        }
        
        /// <summary>
        /// 按分类获取印章
        /// </summary>
        public List<Stamp> GetStampsByCategory(StampCategory category)
        {
            return stamps.Where(s => s != null && s.category == category).ToList();
        }
        
        /// <summary>
        /// 按标签搜索印章
        /// </summary>
        public List<Stamp> SearchStampsByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return GetAllStamps();
            
            return stamps.Where(s => s != null && 
                                s.tags != null && 
                                s.tags.Any(t => t.ToLower().Contains(tag.ToLower())))
                         .ToList();
        }
        
        /// <summary>
        /// 按名称搜索印章
        /// </summary>
        public List<Stamp> SearchStampsByName(string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return GetAllStamps();
            
            searchText = searchText.ToLower();
            return stamps.Where(s => s != null && 
                               s.stampName.ToLower().Contains(searchText))
                         .ToList();
        }
        
        /// <summary>
        /// 获取分类列表
        /// </summary>
        public StampCategory[] GetAvailableCategories()
        {
            return stamps.Where(s => s != null)
                        .Select(s => s.category)
                        .Distinct()
                        .ToArray();
        }
        
        /// <summary>
        /// 验证库中所有印章
        /// </summary>
        public void ValidateAllStamps()
        {
            int validCount = 0;
            int invalidCount = 0;
            
            foreach (var stamp in stamps.ToList())
            {
                if (stamp == null)
                {
                    stamps.Remove(stamp);
                    invalidCount++;
                    continue;
                }
                
                if (stamp.IsValid())
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogWarning($"[StampLibrary] 无效印章: {stamp.name}");
                }
            }
            
            Debug.Log($"[StampLibrary] 印章验证完成 - 有效: {validCount}, 无效: {invalidCount}");
        }
        
        /// <summary>
        /// 获取印章数量
        /// </summary>
        public int GetStampCount()
        {
            return stamps.Count(s => s != null);
        }
    }
}