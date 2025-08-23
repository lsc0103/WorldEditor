using UnityEngine;
using WorldEditor.Environment;

namespace WorldEditor.Environment.Data
{
    /// <summary>
    /// 春季环境预设数据
    /// </summary>
    public static class SpringPresetData 
    {
        public static EnvironmentState CreateSpringState()
        {
            var state = new EnvironmentState();
            
            // 时间状态：春天早晨
            state.timeOfDay = 0.33f;  // 8:00 AM
            state.currentSeason = SeasonType.Spring;
            state.seasonProgress = 0.5f;
            state.daysPassed = 30;
            
            // 天气状态：温和多云
            state.currentWeather = WeatherType.Cloudy;
            state.weatherIntensity = 0.6f;
            state.weatherTransition = 0f;
            state.targetWeather = WeatherType.Cloudy;
            
            // 大气环境：温和舒适
            state.temperature = 18f;
            state.humidity = 0.6f;
            state.atmosphericPressure = 1.0f;
            state.airQuality = 0.15f;
            
            // 风力系统：轻柔春风
            state.windDirection = new Vector3(0.7f, 0f, 0.7f);
            state.windStrength = 0.35f;
            state.windVariation = 1.2f;
            
            // 光照状态：柔和晨光
            state.sunColor = new Color(1f, 0.95f, 0.8f);
            state.sunIntensity = 0.8f;
            state.ambientColor = new Color(0.6f, 0.65f, 0.8f);
            state.ambientIntensity = 0.4f;
            state.moonColor = new Color(0.8f, 0.8f, 1f);
            state.moonIntensity = 0.05f;
            
            // 天空状态：清新春空
            state.skyTint = new Color(0.9f, 1f, 1f);
            state.atmosphereThickness = 1.1f;
            state.cloudCoverage = 0.4f;
            state.cloudDensity = 0.3f;
            
            // 水体状态：清澈春水
            state.globalWaterLevel = 0.5f;
            state.waterTemperature = 12f;
            state.waterTurbidity = 0.05f;
            
            // 环境特效：清晰空气
            state.fogDensity = 0.1f;
            state.fogColor = new Color(0.9f, 0.95f, 1f);
            state.particleEnvironmentIntensity = 0.8f;
            
            return state;
        }
    }
    
    /// <summary>
    /// 夏季环境预设数据  
    /// </summary>
    public static class SummerPresetData
    {
        public static EnvironmentState CreateSummerState()
        {
            var state = new EnvironmentState();
            
            // 时间状态：夏天正午
            state.timeOfDay = 0.5f;  // 12:00 PM
            state.currentSeason = SeasonType.Summer;
            state.seasonProgress = 0.5f;
            state.daysPassed = 120;
            
            // 天气状态：晴朗炎热
            state.currentWeather = WeatherType.Clear;
            state.weatherIntensity = 1f;
            state.weatherTransition = 0f;
            state.targetWeather = WeatherType.Clear;
            
            // 大气环境：炎热干燥
            state.temperature = 32f;
            state.humidity = 0.3f;
            state.atmosphericPressure = 0.95f;
            state.airQuality = 0.25f;
            
            // 风力系统：微风
            state.windDirection = new Vector3(1f, 0f, 0f);
            state.windStrength = 0.2f;
            state.windVariation = 0.8f;
            
            // 光照状态：强烈阳光
            state.sunColor = new Color(1f, 0.98f, 0.9f);
            state.sunIntensity = 1.3f;
            state.ambientColor = new Color(0.7f, 0.7f, 0.9f);
            state.ambientIntensity = 0.5f;
            state.moonColor = new Color(0.9f, 0.9f, 1f);
            state.moonIntensity = 0.08f;
            
            // 天空状态：蔚蓝晴空
            state.skyTint = new Color(0.8f, 0.95f, 1f);
            state.atmosphereThickness = 0.8f;
            state.cloudCoverage = 0.1f;
            state.cloudDensity = 0.2f;
            
            // 水体状态：温暖水体
            state.globalWaterLevel = -0.2f;
            state.waterTemperature = 24f;
            state.waterTurbidity = 0.15f;
            
            // 环境特效：热浪效应
            state.fogDensity = 0f;
            state.fogColor = new Color(1f, 0.98f, 0.9f);
            state.particleEnvironmentIntensity = 0.6f;
            
            return state;
        }
    }
    
    /// <summary>
    /// 秋季环境预设数据
    /// </summary>
    public static class AutumnPresetData
    {
        public static EnvironmentState CreateAutumnState()
        {
            var state = new EnvironmentState();
            
            // 时间状态：秋天傍晚
            state.timeOfDay = 0.7f;  // 17:00 PM
            state.currentSeason = SeasonType.Autumn;
            state.seasonProgress = 0.6f;
            state.daysPassed = 240;
            
            // 天气状态：多云微雨
            state.currentWeather = WeatherType.Overcast;
            state.weatherIntensity = 0.7f;
            state.weatherTransition = 0f;
            state.targetWeather = WeatherType.Rainy;
            
            // 大气环境：凉爽潮湿
            state.temperature = 12f;
            state.humidity = 0.75f;
            state.atmosphericPressure = 1.05f;
            state.airQuality = 0.1f;
            
            // 风力系统：萧瑟秋风
            state.windDirection = new Vector3(-0.7f, 0f, 0.7f);
            state.windStrength = 0.5f;
            state.windVariation = 1.5f;
            
            // 光照状态：金色夕阳
            state.sunColor = new Color(1f, 0.7f, 0.4f);
            state.sunIntensity = 0.6f;
            state.ambientColor = new Color(0.6f, 0.5f, 0.6f);
            state.ambientIntensity = 0.3f;
            state.moonColor = new Color(0.8f, 0.8f, 1f);
            state.moonIntensity = 0.12f;
            
            // 天空状态：阴沉天空
            state.skyTint = new Color(0.8f, 0.8f, 0.9f);
            state.atmosphereThickness = 1.3f;
            state.cloudCoverage = 0.7f;
            state.cloudDensity = 0.6f;
            
            // 水体状态：凉爽秋水
            state.globalWaterLevel = 0.3f;
            state.waterTemperature = 8f;
            state.waterTurbidity = 0.2f;
            
            // 环境特效：薄雾缭绕
            state.fogDensity = 0.3f;
            state.fogColor = new Color(0.9f, 0.9f, 0.95f);
            state.particleEnvironmentIntensity = 1.2f;
            
            return state;
        }
    }
    
    /// <summary>
    /// 冬季环境预设数据
    /// </summary>
    public static class WinterPresetData
    {
        public static EnvironmentState CreateWinterState()
        {
            var state = new EnvironmentState();
            
            // 时间状态：冬天下午
            state.timeOfDay = 0.6f;  // 14:24 PM (冬日较短)
            state.currentSeason = SeasonType.Winter;
            state.seasonProgress = 0.4f;
            state.daysPassed = 350;
            
            // 天气状态：雪天
            state.currentWeather = WeatherType.Snowy;
            state.weatherIntensity = 0.8f;
            state.weatherTransition = 0f;
            state.targetWeather = WeatherType.Snowy;
            
            // 大气环境：寒冷干燥
            state.temperature = -5f;
            state.humidity = 0.4f;
            state.atmosphericPressure = 1.1f;
            state.airQuality = 0.05f;
            
            // 风力系统：刺骨寒风
            state.windDirection = new Vector3(0f, 0f, -1f);
            state.windStrength = 0.6f;
            state.windVariation = 2f;
            
            // 光照状态：微弱冬阳
            state.sunColor = new Color(0.9f, 0.9f, 1f);
            state.sunIntensity = 0.4f;
            state.ambientColor = new Color(0.7f, 0.8f, 1f);
            state.ambientIntensity = 0.6f;
            state.moonColor = new Color(0.85f, 0.9f, 1f);
            state.moonIntensity = 0.15f;
            
            // 天空状态：灰白雪空
            state.skyTint = new Color(0.9f, 0.9f, 1f);
            state.atmosphereThickness = 1.5f;
            state.cloudCoverage = 0.9f;
            state.cloudDensity = 0.8f;
            
            // 水体状态：冰冷水体
            state.globalWaterLevel = -0.1f;
            state.waterTemperature = 1f;
            state.waterTurbidity = 0.05f;
            
            // 环境特效：雪雾茫茫
            state.fogDensity = 0.2f;
            state.fogColor = new Color(0.95f, 0.95f, 1f);
            state.particleEnvironmentIntensity = 1.5f;
            
            return state;
        }
    }
}