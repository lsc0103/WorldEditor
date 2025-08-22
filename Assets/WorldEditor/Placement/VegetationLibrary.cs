using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WorldEditor.Core;

namespace WorldEditor.Placement
{
    /// <summary>
    /// 植被库 - 存储所有可用植被类型和预制体
    /// </summary>
    [CreateAssetMenu(fileName = "VegetationLibrary", menuName = "WorldEditor/Vegetation Library")]
    public class VegetationLibrary : ScriptableObject
    {
        [Header("植被数据")]
        public List<VegetationData> vegetationTypes = new List<VegetationData>();
        
        [Header("预设模板")]
        public List<VegetationTemplate> templates = new List<VegetationTemplate>();
        
        /// <summary>
        /// 获取指定类型的植被数据
        /// </summary>
        public VegetationData GetVegetationData(VegetationType type)
        {
            return vegetationTypes.FirstOrDefault(v => v.type == type);
        }
        
        /// <summary>
        /// 获取指定生物群系的植被类型
        /// </summary>
        public List<VegetationData> GetVegetationForBiome(BiomeType biome)
        {
            return vegetationTypes.Where(v => v.preferredBiomes.Contains(biome)).ToList();
        }
        
        /// <summary>
        /// 获取树木类植被
        /// </summary>
        public List<VegetationData> GetTreeVegetation()
        {
            return vegetationTypes.Where(v => IsTreeType(v.type)).ToList();
        }
        
        /// <summary>
        /// 获取灌木类植被
        /// </summary>
        public List<VegetationData> GetBushVegetation()
        {
            return vegetationTypes.Where(v => IsBushType(v.type)).ToList();
        }
        
        /// <summary>
        /// 获取草本植物
        /// </summary>
        public List<VegetationData> GetGrassVegetation()
        {
            return vegetationTypes.Where(v => IsGrassType(v.type)).ToList();
        }
        
        /// <summary>
        /// 初始化默认植被库
        /// </summary>
        public void InitializeDefaultVegetation()
        {
            vegetationTypes.Clear();
            
            // 树木类
            vegetationTypes.Add(CreateVegetationData("北欧云杉", VegetationType.针叶树, 
                new Color(0.1f, 0.4f, 0.1f), 0.8f, 1.5f, BiomeType.Forest, BiomeType.Temperate));
            
            vegetationTypes.Add(CreateVegetationData("橡树", VegetationType.阔叶树, 
                new Color(0.2f, 0.6f, 0.2f), 1.2f, 2.0f, BiomeType.Forest, BiomeType.Grassland));
            
            vegetationTypes.Add(CreateVegetationData("椰子树", VegetationType.棕榈树, 
                new Color(0.3f, 0.7f, 0.3f), 1.5f, 2.5f, BiomeType.Tropical, BiomeType.Tropical));
            
            vegetationTypes.Add(CreateVegetationData("苹果树", VegetationType.果树, 
                new Color(0.4f, 0.6f, 0.3f), 1.0f, 1.8f, BiomeType.Forest));
            
            vegetationTypes.Add(CreateVegetationData("枯木", VegetationType.枯树, 
                new Color(0.3f, 0.2f, 0.1f), 0.8f, 1.5f, BiomeType.Desert, BiomeType.Desert));
            
            // 灌木类
            vegetationTypes.Add(CreateVegetationData("山楂灌木", VegetationType.普通灌木, 
                new Color(0.3f, 0.5f, 0.2f), 0.5f, 1.2f, BiomeType.Forest, BiomeType.Mountain));
            
            vegetationTypes.Add(CreateVegetationData("蓝莓丛", VegetationType.浆果灌木, 
                new Color(0.4f, 0.5f, 0.2f), 0.3f, 0.8f, BiomeType.Temperate, BiomeType.Forest));
            
            vegetationTypes.Add(CreateVegetationData("荆棘", VegetationType.荆棘丛, 
                new Color(0.2f, 0.4f, 0.1f), 0.4f, 1.0f, BiomeType.Forest, BiomeType.Desert));
            
            vegetationTypes.Add(CreateVegetationData("竹林", VegetationType.竹子, 
                new Color(0.5f, 0.7f, 0.3f), 2.0f, 4.0f, BiomeType.Forest, BiomeType.Tropical));
            
            // 草本植物
            vegetationTypes.Add(CreateVegetationData("野草丛", VegetationType.野草, 
                new Color(0.4f, 0.7f, 0.3f), 0.2f, 0.6f, BiomeType.Grassland, BiomeType.Forest));
            
            vegetationTypes.Add(CreateVegetationData("野花", VegetationType.鲜花, 
                new Color(0.8f, 0.4f, 0.6f), 0.1f, 0.4f, BiomeType.Grassland, BiomeType.Mountain));
            
            vegetationTypes.Add(CreateVegetationData("蕨类植物", VegetationType.蕨类, 
                new Color(0.2f, 0.6f, 0.2f), 0.3f, 0.8f, BiomeType.Tropical, BiomeType.Forest));
            
            vegetationTypes.Add(CreateVegetationData("苔藓", VegetationType.苔藓, 
                new Color(0.3f, 0.5f, 0.1f), 0.02f, 0.1f, BiomeType.Tundra, BiomeType.Forest));
            
            // 特殊植物
            vegetationTypes.Add(CreateVegetationData("仙人掌", VegetationType.仙人掌, 
                new Color(0.2f, 0.4f, 0.2f), 0.8f, 2.0f, BiomeType.Desert));
            
            vegetationTypes.Add(CreateVegetationData("蘑菇", VegetationType.蘑菇, 
                new Color(0.8f, 0.6f, 0.4f), 0.05f, 0.3f, BiomeType.Forest, BiomeType.Tropical));
            
            vegetationTypes.Add(CreateVegetationData("藤蔓", VegetationType.藤蔓, 
                new Color(0.3f, 0.6f, 0.2f), 0.1f, 2.0f, BiomeType.Tropical, BiomeType.Forest));
            
            vegetationTypes.Add(CreateVegetationData("水草", VegetationType.水草, 
                new Color(0.2f, 0.5f, 0.3f), 0.1f, 0.5f, BiomeType.Swamp, BiomeType.Swamp));
            
            InitializeDefaultTemplates();
        }
        
        VegetationData CreateVegetationData(string name, VegetationType type, Color tint, float minScale, float maxScale, params BiomeType[] biomes)
        {
            var data = new VegetationData
            {
                displayName = name,
                type = type,
                tintColor = tint,
                minScale = minScale,
                maxScale = maxScale,
                density = 1.0f,
                canGrowOnSlope = !IsTreeType(type) || type == VegetationType.针叶树,
                heightRange = new Vector2(0, 1),
                preferredBiomes = new List<BiomeType>(biomes)
            };
            
            // 根据植被类型调整高度范围
            switch (type)
            {
                case VegetationType.水草:
                    data.heightRange = new Vector2(0, 0.2f);
                    break;
                case VegetationType.仙人掌:
                    data.heightRange = new Vector2(0.1f, 0.8f);
                    break;
                case VegetationType.针叶树:
                    data.heightRange = new Vector2(0.3f, 1.0f);
                    break;
                case VegetationType.苔藓:
                    data.heightRange = new Vector2(0.6f, 1.0f);
                    break;
            }
            
            return data;
        }
        
        void InitializeDefaultTemplates()
        {
            templates.Clear();
            
            templates.Add(new VegetationTemplate
            {
                templateName = "针叶森林",
                description = "北方针叶林生态系统，以云杉为主",
                primaryTypes = new List<VegetationType> { VegetationType.针叶树 },
                secondaryTypes = new List<VegetationType> { VegetationType.浆果灌木, VegetationType.蕨类, VegetationType.苔藓 },
                density = 0.8f,
                biomeTypes = new List<BiomeType> { BiomeType.Temperate, BiomeType.Forest }
            });
            
            templates.Add(new VegetationTemplate
            {
                templateName = "阔叶森林",
                description = "温带阔叶林，树种丰富多样",
                primaryTypes = new List<VegetationType> { VegetationType.阔叶树, VegetationType.果树 },
                secondaryTypes = new List<VegetationType> { VegetationType.普通灌木, VegetationType.野草, VegetationType.鲜花 },
                density = 0.9f,
                biomeTypes = new List<BiomeType> { BiomeType.Forest }
            });
            
            templates.Add(new VegetationTemplate
            {
                templateName = "热带雨林",
                description = "生物多样性极高的热带雨林",
                primaryTypes = new List<VegetationType> { VegetationType.棕榈树, VegetationType.阔叶树 },
                secondaryTypes = new List<VegetationType> { VegetationType.竹子, VegetationType.蕨类, VegetationType.藤蔓 },
                density = 1.2f,
                biomeTypes = new List<BiomeType> { BiomeType.Tropical }
            });
            
            templates.Add(new VegetationTemplate
            {
                templateName = "温带草原",
                description = "开阔的草原景观，零星分布的树木",
                primaryTypes = new List<VegetationType> { VegetationType.野草, VegetationType.鲜花 },
                secondaryTypes = new List<VegetationType> { VegetationType.阔叶树 },
                density = 0.6f,
                biomeTypes = new List<BiomeType> { BiomeType.Grassland }
            });
            
            templates.Add(new VegetationTemplate
            {
                templateName = "沙漠绿洲",
                description = "沙漠中的绿洲，植被稀疏但生命力强",
                primaryTypes = new List<VegetationType> { VegetationType.仙人掌 },
                secondaryTypes = new List<VegetationType> { VegetationType.枯树, VegetationType.荆棘丛 },
                density = 0.3f,
                biomeTypes = new List<BiomeType> { BiomeType.Desert }
            });
            
            templates.Add(new VegetationTemplate
            {
                templateName = "高山草甸",
                description = "高海拔草甸，适应寒冷气候",
                primaryTypes = new List<VegetationType> { VegetationType.野草, VegetationType.鲜花 },
                secondaryTypes = new List<VegetationType> { VegetationType.苔藓 },
                density = 0.7f,
                biomeTypes = new List<BiomeType> { BiomeType.Mountain }
            });
        }
        
        public bool IsTreeType(VegetationType type)
        {
            return type <= VegetationType.枯树;
        }
        
        public bool IsBushType(VegetationType type)
        {
            return type >= VegetationType.普通灌木 && type <= VegetationType.竹子;
        }
        
        public bool IsGrassType(VegetationType type)
        {
            return type >= VegetationType.野草 && type <= VegetationType.苔藓;
        }
        
        public bool IsSpecialType(VegetationType type)
        {
            return type >= VegetationType.仙人掌;
        }
        
        /// <summary>
        /// 获取植被类型的显示分类
        /// </summary>
        public string GetVegetationCategory(VegetationType type)
        {
            if (IsTreeType(type)) return "🌳 树木类";
            if (IsBushType(type)) return "🌿 灌木类";
            if (IsGrassType(type)) return "🌱 草本植物";
            return "🌵 特殊植物";
        }
        
        /// <summary>
        /// 获取植被类型的表情符号
        /// </summary>
        public string GetVegetationEmoji(VegetationType type)
        {
            switch (type)
            {
                case VegetationType.针叶树: return "🌲";
                case VegetationType.阔叶树: return "🌳";
                case VegetationType.棕榈树: return "🌴";
                case VegetationType.果树: return "🍎";
                case VegetationType.枯树: return "🪴";
                
                case VegetationType.普通灌木: return "🌿";
                case VegetationType.浆果灌木: return "🫐";
                case VegetationType.荆棘丛: return "🌹";
                case VegetationType.竹子: return "🎋";
                
                case VegetationType.野草: return "🌾";
                case VegetationType.鲜花: return "🌸";
                case VegetationType.蕨类: return "🪴";
                case VegetationType.苔藓: return "🍀";
                
                case VegetationType.仙人掌: return "🌵";
                case VegetationType.蘑菇: return "🍄";
                case VegetationType.藤蔓: return "🍃";
                case VegetationType.水草: return "🌿";
                
                default: return "🌱";
            }
        }
    }
    
    /// <summary>
    /// 植被模板 - 预定义的植被组合
    /// </summary>
    [System.Serializable]
    public class VegetationTemplate
    {
        [Header("模板信息")]
        public string templateName;
        public string description;
        public Texture2D previewImage;
        
        [Header("植被配置")]
        public List<VegetationType> primaryTypes = new List<VegetationType>();
        public List<VegetationType> secondaryTypes = new List<VegetationType>();
        public float density = 1.0f;
        
        [Header("环境适配")]
        public List<BiomeType> biomeTypes = new List<BiomeType>();
        public Vector2 heightRange = new Vector2(0, 1);
        public float slopeLimit = 45f;
        
        [Header("分布参数")]
        public float primaryDensity = 1.0f;
        public float secondaryDensity = 0.5f;
        public float minDistance = 1.0f;
        public bool enableClustering = true;
    }
    
}