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

        #endregion


        #region 当前状态

        [Header("当前时间状态 (只读)")]
        [SerializeField] private float currentTime = 0.5f;        // 当前一天内时间 0-1
        [SerializeField] private int daysPassed = 0;              // 已经过天数
        [SerializeField] private bool isPaused = false;          // 是否暂停

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
            daysPassed = 0;
            isPaused = false;

            // 同步到环境状态
            SyncToEnvironmentState();

            // 启动时间更新
            isActive = true;
            isInitialized = true;
            lastUpdateTime = Time.time;

            Debug.Log($"[TimeSystem] 时间系统初始化完成 - 开始时间: {GetTimeString()}");
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
            
            // 同步到环境状态
            SyncToEnvironmentState();
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
        /// 同步到环境状态
        /// </summary>
        private void SyncToEnvironmentState()
        {
            if (linkedEnvironmentState != null)
            {
                linkedEnvironmentState.timeOfDay = currentTime;
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
            GUILayout.Label($"时间流速: {timeScale}x");
            GUILayout.Label($"状态: {(isPaused ? "暂停" : "运行")}");
            
            GUILayout.EndArea();
        }

        #endregion
    }
}