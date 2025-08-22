using UnityEngine;

namespace WorldEditor.TerrainSystem
{
    /// <summary>
    /// 地形印章数据 - 存储印章的高度图和属性
    /// </summary>
    [CreateAssetMenu(fileName = "New Stamp", menuName = "WorldEditor/Terrain Stamp")]
    public class Stamp : ScriptableObject
    {
        [Header("印章基本信息")]
        public string stampName = "New Stamp";
        [TextArea(2, 4)]
        public string description = "描述这个印章的用途和特点";
        public Texture2D previewIcon;
        
        [Header("高度数据")]
        [Tooltip("印章的高度图（灰度图）")]
        public Texture2D heightTexture;
        [Tooltip("高度范围调节")]
        public float heightScale = 1f;
        [Tooltip("基础高度偏移")]
        public float baseHeight = 0f;
        
        [Header("印章属性")]
        [Tooltip("默认大小")]
        public float defaultSize = 100f;
        [Tooltip("默认强度")]
        [Range(0f, 2f)]
        public float defaultStrength = 1f;
        [Tooltip("推荐的混合模式")]
        public StampBlendMode recommendedBlendMode = StampBlendMode.Add;
        
        [Header("印章类型")]
        public StampCategory category = StampCategory.Mountain;
        public string[] tags = new string[0];
        
        [Header("使用限制")]
        [Tooltip("最小使用大小")]
        public float minSize = 10f;
        [Tooltip("最大使用大小")]
        public float maxSize = 500f;
        
        /// <summary>
        /// 验证印章数据的有效性
        /// </summary>
        public bool IsValid()
        {
            if (heightTexture == null)
            {
                Debug.LogWarning($"[Stamp] {name}: 缺少高度纹理");
                return false;
            }
            
            if (!IsTextureReadable(heightTexture))
            {
                Debug.LogWarning($"[Stamp] {name}: 高度纹理不可读取，请在导入设置中启用Read/Write");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查纹理是否可读取
        /// </summary>
        bool IsTextureReadable(Texture2D texture)
        {
            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 获取印章在指定位置的高度值
        /// </summary>
        public float GetHeightAtUV(float u, float v)
        {
            if (!IsValid()) return 0f;
            
            // 使用双线性插值采样
            Color sample = heightTexture.GetPixelBilinear(u, v);
            float normalizedHeight = sample.grayscale;
            
            return baseHeight + normalizedHeight * heightScale;
        }
        
        /// <summary>
        /// 获取印章的实际大小范围
        /// </summary>
        public Vector2 GetSizeRange()
        {
            return new Vector2(minSize, maxSize);
        }
        
        /// <summary>
        /// 创建印章的预览纹理
        /// </summary>
        public Texture2D GeneratePreviewTexture(int size = 128)
        {
            if (!IsValid()) return null;
            
            Texture2D preview = new Texture2D(size, size, TextureFormat.RGB24, false);
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float u = (float)x / (size - 1);
                    float v = (float)y / (size - 1);
                    
                    float height = GetHeightAtUV(u, v);
                    float normalizedHeight = Mathf.Clamp01(height);
                    
                    // 创建高度可视化颜色
                    Color color = Color.Lerp(Color.black, Color.white, normalizedHeight);
                    preview.SetPixel(x, y, color);
                }
            }
            
            preview.Apply();
            return preview;
        }
        
        /// <summary>
        /// 复制印章设置到另一个印章
        /// </summary>
        public void CopySettingsTo(Stamp targetStamp)
        {
            if (targetStamp == null) return;
            
            targetStamp.heightScale = this.heightScale;
            targetStamp.baseHeight = this.baseHeight;
            targetStamp.defaultSize = this.defaultSize;
            targetStamp.defaultStrength = this.defaultStrength;
            targetStamp.recommendedBlendMode = this.recommendedBlendMode;
            targetStamp.minSize = this.minSize;
            targetStamp.maxSize = this.maxSize;
        }
    }
    
    /// <summary>
    /// 印章分类
    /// </summary>
    public enum StampCategory
    {
        Mountain,    // 山峰
        Valley,      // 山谷
        Ridge,       // 山脊
        Crater,      // 火山口
        River,       // 河流
        Cliff,       // 悬崖
        Plateau,     // 高原
        Custom       // 自定义
    }
}