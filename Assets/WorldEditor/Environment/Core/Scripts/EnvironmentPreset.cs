using UnityEngine;
using System;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境预设 - 可序列化的环境状态配置
    /// 用于存储和管理预定义的环境配置（如春夏秋冬、不同天气状态等）
    /// </summary>
    [CreateAssetMenu(fileName = "New Environment Preset", menuName = "WorldEditor/Environment/Environment Preset")]
    public class EnvironmentPreset : ScriptableObject
    {
        #region 预设信息
        
        [Header("预设信息")]
        [Tooltip("预设名称")]
        public string presetName = "New Environment Preset";
        
        [Tooltip("预设描述")]
        [TextArea(2, 4)]
        public string description = "环境预设描述";
        
        [Tooltip("预设分类")]
        public EnvironmentPresetCategory category = EnvironmentPresetCategory.Season;
        
        [Tooltip("预设图标")]
        public Texture2D presetIcon;

        #endregion

        #region 环境配置

        [Header("环境配置")]
        [Tooltip("环境状态配置")]
        public EnvironmentState environmentState;

        #endregion

        #region 预设特殊属性

        [Header("预设特殊属性")]
        [Tooltip("是否为默认预设")]
        public bool isDefaultPreset = false;
        
        [Tooltip("预设优先级 (用于排序)")]
        [Range(0, 100)]
        public int priority = 50;
        
        [Tooltip("是否允许运行时修改")]
        public bool allowRuntimeModification = true;

        #endregion

        #region 过渡配置

        [Header("过渡配置")]
        [Tooltip("切换到此预设时的过渡时间 (秒)")]
        [Range(0f, 60f)]
        public float transitionDuration = 5f;
        
        [Tooltip("过渡曲线")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region 初始化

        void OnEnable()
        {
            // 如果环境状态为空，创建默认状态
            if (environmentState == null)
            {
                environmentState = new EnvironmentState();
            }
        }

        void Reset()
        {
            // 在Inspector中重置时设置默认值
            presetName = name.Replace("Environment_Preset_", "").Replace("_", " ");
            environmentState = new EnvironmentState();
            transitionDuration = 5f;
            transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 应用此预设到环境管理器
        /// </summary>
        public void ApplyToEnvironmentManager(EnvironmentManager manager, bool useTransition = true)
        {
            if (manager == null || environmentState == null)
            {
                Debug.LogWarning($"[EnvironmentPreset] 无法应用预设 {presetName}：管理器或环境状态为空");
                return;
            }

            if (useTransition && transitionDuration > 0)
            {
                // 使用过渡应用预设
                manager.StartCoroutine(ApplyWithTransitionCoroutine(manager));
            }
            else
            {
                // 立即应用预设
                ApplyImmediately(manager);
            }
        }

        /// <summary>
        /// 立即应用预设
        /// </summary>
        public void ApplyImmediately(EnvironmentManager manager)
        {
            if (manager == null || environmentState == null) return;

            // 复制环境状态
            var targetState = environmentState.Clone();
            
            // 设置时间
            manager.SetTimeOfDay(targetState.timeOfDay);
            
            // 设置季节
            manager.SetSeason(targetState.currentSeason);
            
            // 设置天气
            manager.SetWeather(targetState.currentWeather, targetState.weatherIntensity);
            
            Debug.Log($"[EnvironmentPreset] 已立即应用预设: {presetName}");
        }

        /// <summary>
        /// 从当前环境状态创建预设
        /// </summary>
        public void CaptureFromEnvironmentManager(EnvironmentManager manager)
        {
            if (manager == null || manager.CurrentState == null)
            {
                Debug.LogWarning($"[EnvironmentPreset] 无法捕获环境状态：管理器或状态为空");
                return;
            }

            environmentState = manager.CurrentState.Clone();
            Debug.Log($"[EnvironmentPreset] 已从当前环境捕获状态到预设: {presetName}");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        /// <summary>
        /// 获取预设的显示名称
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(presetName) ? name : presetName;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 带过渡的应用协程
        /// </summary>
        private System.Collections.IEnumerator ApplyWithTransitionCoroutine(EnvironmentManager manager)
        {
            var startState = manager.CurrentState.Clone();
            var targetState = environmentState.Clone();
            
            float elapsedTime = 0f;
            
            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);
                
                var currentState = EnvironmentState.Lerp(startState, targetState, t);
                
                // 应用插值后的状态
                manager.SetTimeOfDay(currentState.timeOfDay);
                manager.SetSeason(currentState.currentSeason);
                manager.SetWeather(currentState.currentWeather, currentState.weatherIntensity);
                
                yield return null;
            }
            
            // 确保最终状态完全应用
            ApplyImmediately(manager);
            
            Debug.Log($"[EnvironmentPreset] 已完成预设过渡: {presetName} ({transitionDuration:F1}s)");
        }

        #endregion

        #region 调试信息

        public override string ToString()
        {
            return $"EnvironmentPreset: {GetDisplayName()} ({category})";
        }

        #endregion
    }

    /// <summary>
    /// 环境预设分类
    /// </summary>
    public enum EnvironmentPresetCategory
    {
        Season,     // 季节
        Weather,    // 天气
        Time,       // 时间
        Mood,       // 氛围
        Custom      // 自定义
    }
}