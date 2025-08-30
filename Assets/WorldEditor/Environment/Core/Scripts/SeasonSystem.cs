using UnityEngine;
using System;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 季节系统 - 独立管理四季变化
    /// 
    /// 核心功能：
    /// - 四季状态管理和切换
    /// - 季节进度跟踪
    /// - 季节变化事件通知
    /// - 与时间系统独立运行
    /// </summary>
    public class SeasonSystem : MonoBehaviour
    {
        #region 季节配置参数

        [Header("季节配置")]
        [Tooltip("当前季节")]
        public SeasonType currentSeason = SeasonType.Spring;
        
        [Tooltip("是否启用自动季节变化")]
        public bool enableAutoSeason = false;
        
        [Tooltip("每个季节的长度 (游戏内天数)")]
        [Range(1f, 365f)]
        public float seasonLength = 30f;
        
        [Tooltip("季节过渡是否平滑")]
        public bool smoothSeasonTransition = true;

        #endregion

        #region 当前状态

        [Header("当前季节状态 (只读)")]
        [SerializeField] private float seasonProgress = 0f;      // 当前季节进度 0-1
        [SerializeField] private int daysSinceSeasonStart = 0;   // 当前季节已过天数

        #endregion

        #region 运行时状态

        private bool isInitialized = false;
        private EnvironmentState linkedEnvironmentState;
        private TimeSystem linkedTimeSystem;

        #endregion

        #region 事件系统

        /// <summary>季节变化事件 (参数：新季节, 旧季节)</summary>
        public static event Action<SeasonType, SeasonType> OnSeasonChanged;
        
        /// <summary>季节进度更新事件 (参数：进度0-1)</summary>
        public static event Action<float> OnSeasonProgressUpdated;

        #endregion

        #region 公共属性

        /// <summary>当前季节</summary>
        public SeasonType CurrentSeason => currentSeason;
        
        /// <summary>季节进度 (0-1)</summary>
        public float SeasonProgress => seasonProgress;
        
        /// <summary>当前季节已过天数</summary>
        public int DaysSinceSeasonStart => daysSinceSeasonStart;
        
        /// <summary>系统是否激活</summary>
        public bool IsActive => isInitialized;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化季节系统
        /// </summary>
        public void Initialize(EnvironmentState environmentState = null, TimeSystem timeSystem = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[SeasonSystem] 季节系统已经初始化，跳过重复初始化");
                return;
            }

            Debug.Log("[SeasonSystem] 开始初始化季节系统...");

            // 链接环境状态和时间系统
            linkedEnvironmentState = environmentState;
            linkedTimeSystem = timeSystem;

            // 初始化季节状态
            seasonProgress = 0f;
            daysSinceSeasonStart = 0;

            // 如果启用自动季节变化，订阅时间系统事件
            if (enableAutoSeason && linkedTimeSystem != null)
            {
                linkedTimeSystem.OnNewDay += HandleNewDay;
            }

            // 同步到环境状态
            SyncToEnvironmentState();

            isInitialized = true;
            Debug.Log($"[SeasonSystem] 季节系统初始化完成 - 当前季节: {currentSeason}");
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
            daysSinceSeasonStart = 0; // 重置天数
            
            // 触发季节变化事件
            OnSeasonChanged?.Invoke(currentSeason, previousSeason);
            
            // 同步到环境状态
            SyncToEnvironmentState();
            
            Debug.Log($"[SeasonSystem] 季节从 {previousSeason} 变更为 {currentSeason}");
        }

        /// <summary>
        /// 跳转到下一个季节
        /// </summary>
        public void AdvanceToNextSeason()
        {
            SeasonType nextSeason = GetNextSeason(currentSeason);
            SetSeason(nextSeason);
        }

        /// <summary>
        /// 设置为春天
        /// </summary>
        [ContextMenu("设置春天")]
        public void SetSpring()
        {
            SetSeason(SeasonType.Spring);
        }

        /// <summary>
        /// 设置为夏天
        /// </summary>
        [ContextMenu("设置夏天")]
        public void SetSummer()
        {
            SetSeason(SeasonType.Summer);
        }

        /// <summary>
        /// 设置为秋天
        /// </summary>
        [ContextMenu("设置秋天")]
        public void SetAutumn()
        {
            SetSeason(SeasonType.Autumn);
        }

        /// <summary>
        /// 设置为冬天
        /// </summary>
        [ContextMenu("设置冬天")]
        public void SetWinter()
        {
            SetSeason(SeasonType.Winter);
        }

        /// <summary>
        /// 设置季节进度
        /// </summary>
        public void SetSeasonProgress(float progress)
        {
            seasonProgress = Mathf.Clamp01(progress);
            daysSinceSeasonStart = Mathf.FloorToInt(seasonProgress * seasonLength);
            
            // 触发进度更新事件
            OnSeasonProgressUpdated?.Invoke(seasonProgress);
            
            // 同步到环境状态
            SyncToEnvironmentState();
        }

        #endregion

        #region 自动季节变化

        /// <summary>
        /// 处理新一天事件
        /// </summary>
        private void HandleNewDay(int totalDays)
        {
            if (!enableAutoSeason) return;

            daysSinceSeasonStart++;
            seasonProgress = daysSinceSeasonStart / seasonLength;

            // 检查是否需要切换到下一个季节
            if (daysSinceSeasonStart >= seasonLength)
            {
                AdvanceToNextSeason();
            }
            else
            {
                // 触发进度更新事件
                OnSeasonProgressUpdated?.Invoke(seasonProgress);
                
                // 同步到环境状态
                SyncToEnvironmentState();
            }

            Debug.Log($"[SeasonSystem] 季节进度更新 - {currentSeason}: {seasonProgress:F2} ({daysSinceSeasonStart}/{seasonLength} 天)");
        }

        #endregion

        #region 工具方法

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
                linkedEnvironmentState.currentSeason = currentSeason;
                linkedEnvironmentState.seasonProgress = seasonProgress;
            }
        }

        /// <summary>
        /// 获取季节描述字符串
        /// </summary>
        public string GetSeasonDescription()
        {
            return $"{currentSeason} - 进度: {seasonProgress * 100:F0}% ({daysSinceSeasonStart}/{seasonLength} 天)";
        }

        #endregion

        #region 清理

        void OnDestroy()
        {
            // 取消订阅事件
            if (linkedTimeSystem != null)
            {
                linkedTimeSystem.OnNewDay -= HandleNewDay;
            }
        }

        #endregion

        #region 调试信息

        void OnGUI()
        {
            // 调试面板已禁用
            /*
            if (!isInitialized || !Debug.isDebugBuild) return;
            
            // 显示季节系统调试信息
            GUILayout.BeginArea(new Rect(320, 170, 200, 120));
            GUILayout.Box("季节系统调试");
            
            GUILayout.Label($"当前季节: {currentSeason}");
            GUILayout.Label($"季节进度: {seasonProgress * 100:F0}%");
            GUILayout.Label($"已过天数: {daysSinceSeasonStart}");
            GUILayout.Label($"自动变化: {(enableAutoSeason ? "开启" : "关闭")}");
            
            GUILayout.EndArea();
            */
        }

        #endregion
    }
}