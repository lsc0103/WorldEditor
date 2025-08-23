using UnityEngine;
using System;
using System.Collections;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 时间系统 - 管理游戏内时间、昼夜循环和季节变化
    /// 
    /// 核心功能：
    /// - 24小时昼夜循环系统
    /// - 四季更替和季节进度管理
    /// - 时间流速控制和暂停功能
    /// - 时间事件通知系统
    /// - 与其他环境系统的时间同步
    /// </summary>
    public class TimeSystem : MonoBehaviour
    {
        #region 时间配置参数

        [Header("时间流速配置")]
        [Tooltip("时间流速倍率 (1 = 现实时间, 60 = 1分钟现实时间 = 1小时游戏时间)")]
        [Range(0.1f, 3600f)]
        public float timeScale = 60f;
        
        [Tooltip("是否允许暂停时间")]
        public bool canPause = true;
        
        [Header("初始时间设置")]
        [Tooltip("游戏开始时的时间 (0=午夜, 0.5=正午, 1=午夜)")]
        [Range(0f, 1f)]
        public float startTimeOfDay = 0.5f;
        
        [Tooltip("游戏开始时的季节")]
        public SeasonType startSeason = SeasonType.Spring;

        #endregion

        #region 季节配置参数

        [Header("季节系统配置")]
        [Tooltip("是否启用季节系统")]
        public bool enableSeasons = true;
        
        [Tooltip("每个季节的长度 (游戏内天数)")]
        [Range(1f, 365f)]
        public float seasonLength = 30f;
        
        [Tooltip("季节过渡是否平滑")]
        public bool smoothSeasonTransition = true;

        #endregion

        #region 当前状态

        [Header("当前时间状态 (只读)")]
        [SerializeField] private float currentTime = 0.5f;        // 当前一天内时间 0-1
        [SerializeField] private int daysPassed = 0;              // 已经过天数
        [SerializeField] private SeasonType currentSeason = SeasonType.Spring;
        [SerializeField] private float seasonProgress = 0f;      // 当前季节进度 0-1
        [SerializeField] private bool isPaused = false;          // 是否暂停

        #endregion

        #region 光照控制

        [Header("光照控制")]
        [Tooltip("太阳光源 (Directional Light)")]
        public Light sunLight;
        
        [Tooltip("是否自动控制太阳光")]
        public bool controlSunLight = true;

        #endregion

        #region 运行时状态

        private bool isInitialized = false;
        private bool isActive = false;
        private float lastUpdateTime = 0f;
        private EnvironmentState linkedEnvironmentState;

        #endregion

        #region 事件系统

        /// <summary>时间变化事件 (参数：标准化时间0-1)</summary>
        public event Action<float> OnTimeChanged;
        
        /// <summary>新一天开始事件 (参数：天数)</summary>
        public event Action<int> OnNewDay;
        
        /// <summary>季节变化事件 (参数：新季节, 旧季节)</summary>
        public event Action<SeasonType, SeasonType> OnSeasonChanged;
        
        /// <summary>小时变化事件 (参数：小时0-23)</summary>
        public event Action<int> OnHourChanged;

        #endregion

        #region 公共属性

        /// <summary>当前时间 (0-1, 0=午夜, 0.5=正午)</summary>
        public float CurrentTime => currentTime;
        
        /// <summary>当前小时 (0-23)</summary>
        public float CurrentHour => currentTime * 24f;
        
        /// <summary>已经过天数</summary>
        public int DaysPassed => daysPassed;
        
        /// <summary>当前季节</summary>
        public SeasonType CurrentSeason => currentSeason;
        
        /// <summary>季节进度 (0-1)</summary>
        public float SeasonProgress => seasonProgress;
        
        /// <summary>是否暂停</summary>
        public bool IsPaused => isPaused;
        
        /// <summary>时间系统是否激活</summary>
        public bool IsActive => isActive && isInitialized;
        
        /// <summary>是否为白天 (6:00-18:00)</summary>
        public bool IsDaytime => currentTime > 0.25f && currentTime < 0.75f;
        
        /// <summary>是否为夜晚</summary>
        public bool IsNighttime => !IsDaytime;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化时间系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[TimeSystem] 时间系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[TimeSystem] 开始初始化时间系统...");

            // 链接环境状态
            linkedEnvironmentState = environmentState;

            // 设置初始时间
            currentTime = startTimeOfDay;
            currentSeason = startSeason;
            daysPassed = 0;
            seasonProgress = 0f;
            isPaused = false;

            // 同步到环境状态
            SyncToEnvironmentState();

            // 查找并设置太阳光
            SetupSunLight();

            // 启动时间更新
            isActive = true;
            isInitialized = true;
            lastUpdateTime = Time.time;

            Debug.Log($"[TimeSystem] 时间系统初始化完成 - 开始时间: {GetTimeString()}, 季节: {currentSeason}");
        }

        #endregion

        #region 时间控制方法

        /// <summary>
        /// 设置时间流速
        /// </summary>
        public void SetTimeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.1f, 3600f);
            timeScale = scale;
            Debug.Log($"[TimeSystem] 时间流速设置为: {scale}x");
        }

        /// <summary>
        /// 暂停时间
        /// </summary>
        public void PauseTime()
        {
            if (!canPause) return;
            
            isPaused = true;
            Debug.Log("[TimeSystem] 时间已暂停");
        }

        /// <summary>
        /// 恢复时间
        /// </summary>
        public void ResumeTime()
        {
            isPaused = false;
            lastUpdateTime = Time.time; // 重置更新时间避免跳跃
            Debug.Log("[TimeSystem] 时间已恢复");
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                ResumeTime();
            else
                PauseTime();
        }

        /// <summary>
        /// 设置一天中的时间
        /// </summary>
        public void SetTimeOfDay(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            
            float previousTime = currentTime;
            currentTime = normalizedTime;
            
            // 触发时间变化事件
            OnTimeChanged?.Invoke(currentTime);
            
            // 检查是否跨越了小时边界
            CheckHourBoundary(previousTime, currentTime);
            
            // 同步到环境状态
            SyncToEnvironmentState();
            
            Debug.Log($"[TimeSystem] 时间设置为: {GetTimeString()}");
        }

        /// <summary>
        /// 跳转到指定时间 (小时:分钟)
        /// </summary>
        public void SkipToTime(int hour, int minute = 0)
        {
            hour = Mathf.Clamp(hour, 0, 23);
            minute = Mathf.Clamp(minute, 0, 59);
            
            float normalizedTime = (hour + minute / 60f) / 24f;
            SetTimeOfDay(normalizedTime);
        }

        /// <summary>
        /// 跳转到下一天的指定时间
        /// </summary>
        public void SkipToNextDay(int hour = 6, int minute = 0)
        {
            AdvanceDay();
            SkipToTime(hour, minute);
        }

        #endregion

        #region 季节控制方法

        /// <summary>
        /// 设置季节
        /// </summary>
        public void SetSeason(SeasonType season)
        {
            if (currentSeason == season) return;
            
            SeasonType previousSeason = currentSeason;
            currentSeason = season;
            seasonProgress = 0f; // 重置季节进度
            
            // 触发季节变化事件
            OnSeasonChanged?.Invoke(currentSeason, previousSeason);
            
            // 同步到环境状态
            SyncToEnvironmentState();
            
            Debug.Log($"[TimeSystem] 季节从 {previousSeason} 变更为 {currentSeason}");
        }

        /// <summary>
        /// 跳转到下一个季节
        /// </summary>
        public void AdvanceToNextSeason()
        {
            SeasonType nextSeason = GetNextSeason(currentSeason);
            SetSeason(nextSeason);
        }

        #endregion

        #region 系统更新

        /// <summary>
        /// 更新时间系统 (由EnvironmentManager调用)
        /// </summary>
        public void UpdateSystem()
        {
            if (!isActive || isPaused) return;

            float deltaTime = Time.time - lastUpdateTime;
            UpdateTimeProgression(deltaTime);
            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Unity Update (备用更新方式)
        /// </summary>
        void Update()
        {
            if (!isInitialized) return;
            
            // 如果没有外部管理器调用UpdateSystem，则自动更新
            UpdateSystem();
        }

        /// <summary>
        /// 更新时间进程
        /// </summary>
        private void UpdateTimeProgression(float deltaTime)
        {
            if (deltaTime <= 0) return;

            float previousTime = currentTime;
            int previousHour = Mathf.FloorToInt(previousTime * 24f);
            
            // 计算时间增量
            float timeIncrement = (deltaTime * timeScale) / 86400f; // 86400秒 = 1天
            currentTime += timeIncrement;
            
            // 检查是否进入新一天
            if (currentTime >= 1f)
            {
                int newDays = Mathf.FloorToInt(currentTime);
                currentTime = currentTime - newDays;
                
                for (int i = 0; i < newDays; i++)
                {
                    AdvanceDay();
                }
            }
            
            // 检查小时边界
            int currentHour = Mathf.FloorToInt(currentTime * 24f);
            if (currentHour != previousHour)
            {
                OnHourChanged?.Invoke(currentHour);
            }
            
            // 触发时间变化事件
            OnTimeChanged?.Invoke(currentTime);
            
            // 更新季节进度
            if (enableSeasons)
            {
                UpdateSeasonProgress();
            }
            
            // 同步到环境状态
            SyncToEnvironmentState();
            
            // 更新太阳光
            UpdateSunLight();
        }

        /// <summary>
        /// 设置太阳光引用
        /// </summary>
        private void SetupSunLight()
        {
            if (sunLight == null)
            {
                // 自动查找场景中的Directional Light
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light;
                        Debug.Log($"[TimeSystem] 自动找到太阳光: {sunLight.name}");
                        break;
                    }
                }
            }
            
            if (sunLight == null)
            {
                Debug.LogWarning("[TimeSystem] 未找到Directional Light，无法控制太阳光");
                controlSunLight = false;
            }
        }

        /// <summary>
        /// 更新太阳光照
        /// </summary>
        private void UpdateSunLight()
        {
            if (!controlSunLight || sunLight == null) return;

            // 计算太阳角度 (-90度到90度，0度为正午)
            float sunAngle = GetSunAngle();
            
            // 设置太阳光方向
            Vector3 sunDirection = Quaternion.Euler(sunAngle, 0, 0) * Vector3.forward;
            sunLight.transform.rotation = Quaternion.LookRotation(sunDirection);
            
            // 设置太阳光颜色和强度
            UpdateSunLightColor();
        }

        /// <summary>
        /// 更新太阳光颜色和强度
        /// </summary>
        private void UpdateSunLightColor()
        {
            if (sunLight == null) return;

            Color sunColor = Color.white;
            float intensity = 1f;
            
            // 根据时间调整颜色
            if (currentTime < 0.25f || currentTime > 0.75f) // 夜晚
            {
                intensity = 0.1f;
                sunColor = new Color(0.5f, 0.5f, 0.8f); // 月光色
            }
            else if (currentTime < 0.35f || currentTime > 0.65f) // 日出日落
            {
                intensity = 0.6f;
                sunColor = new Color(1f, 0.7f, 0.4f); // 橙红色
            }
            else // 白天
            {
                intensity = 1f;
                sunColor = new Color(1f, 0.95f, 0.8f); // 温暖白色
            }
            
            // 根据季节微调
            switch (currentSeason)
            {
                case SeasonType.Spring:
                    sunColor = Color.Lerp(sunColor, new Color(1f, 1f, 0.9f), 0.2f);
                    break;
                case SeasonType.Summer:
                    intensity *= 1.1f;
                    break;
                case SeasonType.Autumn:
                    sunColor = Color.Lerp(sunColor, new Color(1f, 0.8f, 0.6f), 0.3f);
                    break;
                case SeasonType.Winter:
                    intensity *= 0.8f;
                    sunColor = Color.Lerp(sunColor, new Color(0.9f, 0.9f, 1f), 0.2f);
                    break;
            }
            
            sunLight.color = sunColor;
            sunLight.intensity = intensity;
        }

        /// <summary>
        /// 推进到新一天
        /// </summary>
        private void AdvanceDay()
        {
            daysPassed++;
            OnNewDay?.Invoke(daysPassed);
            Debug.Log($"[TimeSystem] 新的一天开始 - 第{daysPassed}天");
        }

        /// <summary>
        /// 更新季节进度
        /// </summary>
        private void UpdateSeasonProgress()
        {
            if (!enableSeasons || seasonLength <= 0) return;
            
            // 计算当前季节内的天数
            float daysInCurrentSeason = daysPassed % (seasonLength * 4); // 4个季节
            float daysInThisSeason = daysInCurrentSeason % seasonLength;
            
            // 更新季节进度
            float previousProgress = seasonProgress;
            seasonProgress = daysInThisSeason / seasonLength;
            
            // 检查是否需要切换季节
            SeasonType calculatedSeason = GetSeasonFromDays(daysPassed);
            if (calculatedSeason != currentSeason)
            {
                SetSeason(calculatedSeason);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取时间字符串表示
        /// </summary>
        public string GetTimeString()
        {
            float hours = currentTime * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            return $"{hour:D2}:{minute:D2}";
        }

        /// <summary>
        /// 获取详细时间字符串
        /// </summary>
        public string GetDetailedTimeString()
        {
            return $"{GetTimeString()} - 第{daysPassed}天 - {currentSeason} ({seasonProgress * 100:F0}%)";
        }

        /// <summary>
        /// 检查小时边界
        /// </summary>
        private void CheckHourBoundary(float previousTime, float newTime)
        {
            int previousHour = Mathf.FloorToInt(previousTime * 24f);
            int newHour = Mathf.FloorToInt(newTime * 24f);
            
            if (previousHour != newHour)
            {
                OnHourChanged?.Invoke(newHour);
            }
        }

        /// <summary>
        /// 根据天数计算季节
        /// </summary>
        private SeasonType GetSeasonFromDays(int days)
        {
            if (!enableSeasons) return SeasonType.Spring;
            
            int seasonIndex = Mathf.FloorToInt(days / seasonLength) % 4;
            return (SeasonType)seasonIndex;
        }

        /// <summary>
        /// 获取下一个季节
        /// </summary>
        private SeasonType GetNextSeason(SeasonType currentSeason)
        {
            return (SeasonType)(((int)currentSeason + 1) % 4);
        }

        /// <summary>
        /// 同步到环境状态
        /// </summary>
        private void SyncToEnvironmentState()
        {
            if (linkedEnvironmentState != null)
            {
                linkedEnvironmentState.timeOfDay = currentTime;
                linkedEnvironmentState.currentSeason = currentSeason;
                linkedEnvironmentState.seasonProgress = seasonProgress;
                linkedEnvironmentState.daysPassed = daysPassed;
            }
        }

        #endregion

        #region 调试和信息

        /// <summary>
        /// 获取太阳角度 (度)
        /// </summary>
        public float GetSunAngle()
        {
            return (currentTime - 0.5f) * 180f; // -90 to 90 degrees
        }

        /// <summary>
        /// 获取月亮相位
        /// </summary>
        public float GetMoonPhase()
        {
            return (daysPassed % 29.5f) / 29.5f; // 29.5天月相周期
        }

        void OnGUI()
        {
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            // 显示时间系统调试信息
            GUILayout.BeginArea(new Rect(320, 10, 200, 150));
            GUILayout.Box("时间系统调试");
            
            GUILayout.Label($"时间: {GetTimeString()}");
            GUILayout.Label($"天数: {daysPassed}");
            GUILayout.Label($"季节: {currentSeason}");
            GUILayout.Label($"季节进度: {seasonProgress * 100:F0}%");
            GUILayout.Label($"时间流速: {timeScale}x");
            GUILayout.Label($"状态: {(isPaused ? "暂停" : "运行")}");
            
            GUILayout.EndArea();
        }

        #endregion
    }
}