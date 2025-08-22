using UnityEngine;
using System;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境状态数据结构
    /// 包含所有环境系统的状态信息，支持序列化和持久化
    /// </summary>
    [System.Serializable]
    public class EnvironmentState
    {
        #region 时间状态
        
        [Header("时间状态")]
        [Tooltip("一天中的时间 (0=午夜, 0.25=日出, 0.5=正午, 0.75=日落, 1=午夜)")]
        [Range(0f, 1f)]
        public float timeOfDay = 0.5f;
        
        [Tooltip("当前季节")]
        public SeasonType currentSeason = SeasonType.Spring;
        
        [Tooltip("季节内的进度 (0=季节开始, 1=季节结束)")]
        [Range(0f, 1f)]
        public float seasonProgress = 0f;
        
        [Tooltip("游戏内已经过的天数")]
        public int daysPassed = 0;

        #endregion

        #region 天气状态
        
        [Header("天气状态")]
        [Tooltip("当前天气类型")]
        public WeatherType currentWeather = WeatherType.Clear;
        
        [Tooltip("天气强度 (0=无, 1=最强)")]
        [Range(0f, 1f)]
        public float weatherIntensity = 1f;
        
        [Tooltip("天气过渡进度 (0=起始天气, 1=目标天气)")]
        [Range(0f, 1f)]
        public float weatherTransition = 0f;
        
        [Tooltip("目标天气类型 (用于天气过渡)")]
        public WeatherType targetWeather = WeatherType.Clear;

        #endregion

        #region 大气环境状态
        
        [Header("大气环境")]
        [Tooltip("环境温度 (摄氏度)")]
        [Range(-50f, 50f)]
        public float temperature = 20f;
        
        [Tooltip("相对湿度 (0=干燥, 1=饱和)")]
        [Range(0f, 1f)]
        public float humidity = 0.5f;
        
        [Tooltip("大气压强 (标准大气压的倍数)")]
        [Range(0.5f, 1.5f)]
        public float atmosphericPressure = 1f;
        
        [Tooltip("空气质量指数 (0=优, 1=严重污染)")]
        [Range(0f, 1f)]
        public float airQuality = 0.1f;

        #endregion

        #region 风力系统
        
        [Header("风力系统")]
        [Tooltip("风向 (世界空间方向)")]
        public Vector3 windDirection = Vector3.forward;
        
        [Tooltip("风力强度 (0=无风, 1=强风)")]
        [Range(0f, 1f)]
        public float windStrength = 0.3f;
        
        [Tooltip("风力变化频率 (影响风的随机波动)")]
        [Range(0f, 5f)]
        public float windVariation = 1f;

        #endregion

        #region 光照状态
        
        [Header("光照状态")]
        [Tooltip("太阳光颜色")]
        public Color sunColor = Color.white;
        
        [Tooltip("太阳光强度")]
        [Range(0f, 3f)]
        public float sunIntensity = 1f;
        
        [Tooltip("环境光颜色")]
        public Color ambientColor = new Color(0.5f, 0.5f, 0.7f);
        
        [Tooltip("环境光强度")]
        [Range(0f, 1f)]
        public float ambientIntensity = 0.3f;
        
        [Tooltip("月亮光颜色")]
        public Color moonColor = new Color(0.8f, 0.8f, 1f);
        
        [Tooltip("月亮光强度")]
        [Range(0f, 1f)]
        public float moonIntensity = 0.1f;

        #endregion

        #region 天空状态
        
        [Header("天空状态")]
        [Tooltip("天空色调")]
        public Color skyTint = Color.white;
        
        [Tooltip("大气厚度 (影响大气散射)")]
        [Range(0f, 5f)]
        public float atmosphereThickness = 1f;
        
        [Tooltip("云层覆盖度 (0=无云, 1=完全覆盖)")]
        [Range(0f, 1f)]
        public float cloudCoverage = 0.3f;
        
        [Tooltip("云层密度")]
        [Range(0f, 1f)]
        public float cloudDensity = 0.5f;

        #endregion

        #region 水体状态
        
        [Header("水体状态")]
        [Tooltip("全局水位高度 (世界空间Y坐标)")]
        public float globalWaterLevel = 0f;
        
        [Tooltip("水体温度 (摄氏度)")]
        [Range(-10f, 40f)]
        public float waterTemperature = 15f;
        
        [Tooltip("水体浑浊度 (0=清澈, 1=浑浊)")]
        [Range(0f, 1f)]
        public float waterTurbidity = 0.1f;

        #endregion

        #region 特效状态
        
        [Header("环境特效")]
        [Tooltip("雾气密度 (0=无雾, 1=浓雾)")]
        [Range(0f, 1f)]
        public float fogDensity = 0f;
        
        [Tooltip("雾气颜色")]
        public Color fogColor = Color.gray;
        
        [Tooltip("粒子环境强度 (影响雨雪等粒子效果)")]
        [Range(0f, 1f)]
        public float particleEnvironmentIntensity = 1f;

        #endregion

        #region 构造函数
        
        /// <summary>
        /// 默认构造函数 - 创建春天正午的晴朗环境
        /// </summary>
        public EnvironmentState()
        {
            // 时间状态：春天正午
            timeOfDay = 0.5f;
            currentSeason = SeasonType.Spring;
            seasonProgress = 0.5f;
            daysPassed = 0;
            
            // 天气状态：晴朗天气
            currentWeather = WeatherType.Clear;
            targetWeather = WeatherType.Clear;
            weatherIntensity = 1f;
            weatherTransition = 0f;
            
            // 大气环境：温和舒适
            temperature = 20f;
            humidity = 0.5f;
            atmosphericPressure = 1f;
            airQuality = 0.1f;
            
            // 风力系统：轻微南风
            windDirection = Vector3.forward;
            windStrength = 0.3f;
            windVariation = 1f;
            
            // 光照状态：标准日光
            sunColor = Color.white;
            sunIntensity = 1f;
            ambientColor = new Color(0.5f, 0.5f, 0.7f);
            ambientIntensity = 0.3f;
            moonColor = new Color(0.8f, 0.8f, 1f);
            moonIntensity = 0.1f;
            
            // 天空状态：少云
            skyTint = Color.white;
            atmosphereThickness = 1f;
            cloudCoverage = 0.3f;
            cloudDensity = 0.5f;
            
            // 水体状态：标准海平面
            globalWaterLevel = 0f;
            waterTemperature = 15f;
            waterTurbidity = 0.1f;
            
            // 环境特效：清晰环境
            fogDensity = 0f;
            fogColor = Color.gray;
            particleEnvironmentIntensity = 1f;
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 获取当前时间的小时数 (0-23)
        /// </summary>
        public float GetHourOfDay()
        {
            return timeOfDay * 24f;
        }

        /// <summary>
        /// 获取时间字符串表示
        /// </summary>
        public string GetTimeString()
        {
            float hours = timeOfDay * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            return $"{hour:D2}:{minute:D2}";
        }

        /// <summary>
        /// 判断当前是否为白天
        /// </summary>
        public bool IsDaytime()
        {
            return timeOfDay > 0.25f && timeOfDay < 0.75f;
        }

        /// <summary>
        /// 判断当前是否为夜晚
        /// </summary>
        public bool IsNighttime()
        {
            return !IsDaytime();
        }

        /// <summary>
        /// 获取太阳高度角 (-90度到90度)
        /// </summary>
        public float GetSunElevation()
        {
            // 将时间转换为太阳高度角
            float angle = (timeOfDay - 0.5f) * 180f; // -90 到 90
            return angle;
        }

        /// <summary>
        /// 获取月亮相位 (0=新月, 0.5=满月, 1=新月)
        /// </summary>
        public float GetMoonPhase()
        {
            // 基于天数计算月相 (假设29.5天一个月相周期)
            float cycle = (daysPassed % 29.5f) / 29.5f;
            return cycle;
        }

        /// <summary>
        /// 复制环境状态
        /// </summary>
        public EnvironmentState Clone()
        {
            return JsonUtility.FromJson<EnvironmentState>(JsonUtility.ToJson(this));
        }

        /// <summary>
        /// 线性插值到目标状态
        /// </summary>
        public static EnvironmentState Lerp(EnvironmentState from, EnvironmentState to, float t)
        {
            t = Mathf.Clamp01(t);
            
            EnvironmentState result = new EnvironmentState();
            
            // 插值数值属性
            result.timeOfDay = Mathf.Lerp(from.timeOfDay, to.timeOfDay, t);
            result.seasonProgress = Mathf.Lerp(from.seasonProgress, to.seasonProgress, t);
            result.weatherIntensity = Mathf.Lerp(from.weatherIntensity, to.weatherIntensity, t);
            result.weatherTransition = Mathf.Lerp(from.weatherTransition, to.weatherTransition, t);
            result.temperature = Mathf.Lerp(from.temperature, to.temperature, t);
            result.humidity = Mathf.Lerp(from.humidity, to.humidity, t);
            result.atmosphericPressure = Mathf.Lerp(from.atmosphericPressure, to.atmosphericPressure, t);
            result.airQuality = Mathf.Lerp(from.airQuality, to.airQuality, t);
            result.windStrength = Mathf.Lerp(from.windStrength, to.windStrength, t);
            result.windVariation = Mathf.Lerp(from.windVariation, to.windVariation, t);
            result.sunIntensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t);
            result.ambientIntensity = Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t);
            result.moonIntensity = Mathf.Lerp(from.moonIntensity, to.moonIntensity, t);
            result.atmosphereThickness = Mathf.Lerp(from.atmosphereThickness, to.atmosphereThickness, t);
            result.cloudCoverage = Mathf.Lerp(from.cloudCoverage, to.cloudCoverage, t);
            result.cloudDensity = Mathf.Lerp(from.cloudDensity, to.cloudDensity, t);
            result.globalWaterLevel = Mathf.Lerp(from.globalWaterLevel, to.globalWaterLevel, t);
            result.waterTemperature = Mathf.Lerp(from.waterTemperature, to.waterTemperature, t);
            result.waterTurbidity = Mathf.Lerp(from.waterTurbidity, to.waterTurbidity, t);
            result.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
            result.particleEnvironmentIntensity = Mathf.Lerp(from.particleEnvironmentIntensity, to.particleEnvironmentIntensity, t);
            
            // 插值向量属性
            result.windDirection = Vector3.Slerp(from.windDirection, to.windDirection, t);
            
            // 插值颜色属性
            result.sunColor = Color.Lerp(from.sunColor, to.sunColor, t);
            result.ambientColor = Color.Lerp(from.ambientColor, to.ambientColor, t);
            result.moonColor = Color.Lerp(from.moonColor, to.moonColor, t);
            result.skyTint = Color.Lerp(from.skyTint, to.skyTint, t);
            result.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
            
            // 枚举属性 (按阈值切换)
            result.currentSeason = t < 0.5f ? from.currentSeason : to.currentSeason;
            result.currentWeather = t < 0.5f ? from.currentWeather : to.currentWeather;
            result.targetWeather = to.targetWeather;
            
            // 整数属性
            result.daysPassed = t < 0.5f ? from.daysPassed : to.daysPassed;
            
            return result;
        }

        #endregion

        #region 调试信息

        public override string ToString()
        {
            return $"环境状态 - 时间:{GetTimeString()}, 季节:{currentSeason}, 天气:{currentWeather}, 温度:{temperature:F1}°C";
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 季节类型
    /// </summary>
    public enum SeasonType
    {
        Spring = 0,     // 春季
        Summer = 1,     // 夏季  
        Autumn = 2,     // 秋季
        Winter = 3      // 冬季
    }

    /// <summary>
    /// 天气类型
    /// </summary>
    public enum WeatherType
    {
        Clear = 0,      // 晴天
        Cloudy = 1,     // 多云
        Overcast = 2,   // 阴天
        Rainy = 3,      // 雨天
        Storm = 4,      // 暴风雨
        Snowy = 5,      // 雪天
        Foggy = 6,      // 雾天
        Windy = 7       // 大风
    }

    #endregion
}