using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境预设管理器 (简化版)
    /// 负责加载和应用环境预设，专注于核心功能
    /// </summary>
    public class EnvironmentPresetManager : MonoBehaviour
    {
        #region 配置

        [Header("预设配置")]
        [Tooltip("内置预设列表")]
        public EnvironmentPreset[] builtInPresets = new EnvironmentPreset[4];
        
        [Tooltip("默认预设索引")]
        public int defaultPresetIndex = 0;

        #endregion

        #region 运行时状态

        private EnvironmentPreset currentPreset;
        private EnvironmentManager environmentManager;

        #endregion

        #region 公共属性

        /// <summary>当前预设</summary>
        public EnvironmentPreset CurrentPreset => currentPreset;
        
        /// <summary>可用预设数量</summary>
        public int PresetCount => builtInPresets?.Length ?? 0;

        #endregion

        #region 初始化

        void Start()
        {
            environmentManager = EnvironmentManager.Instance;
            ApplyDefaultPreset();
        }

        /// <summary>
        /// 应用默认预设
        /// </summary>
        private void ApplyDefaultPreset()
        {
            if (builtInPresets != null && defaultPresetIndex < builtInPresets.Length)
            {
                ApplyPreset(defaultPresetIndex, false);
            }
        }

        #endregion

        #region 预设操作

        /// <summary>
        /// 应用指定索引的预设
        /// </summary>
        public bool ApplyPreset(int presetIndex, bool useTransition = true)
        {
            if (builtInPresets == null || presetIndex < 0 || presetIndex >= builtInPresets.Length)
            {
                Debug.LogWarning($"[EnvironmentPresetManager] 预设索引超出范围: {presetIndex}");
                return false;
            }

            return ApplyPreset(builtInPresets[presetIndex], useTransition);
        }

        /// <summary>
        /// 应用预设
        /// </summary>
        public bool ApplyPreset(EnvironmentPreset preset, bool useTransition = true)
        {
            if (preset == null || environmentManager == null) return false;

            currentPreset = preset;
            preset.ApplyToEnvironmentManager(environmentManager, useTransition);
            
            Debug.Log($"[EnvironmentPresetManager] 应用预设: {preset.GetDisplayName()}");
            return true;
        }

        /// <summary>
        /// 按名称应用预设
        /// </summary>
        public bool ApplyPresetByName(string presetName, bool useTransition = true)
        {
            var preset = builtInPresets?.FirstOrDefault(p => p != null && p.GetDisplayName() == presetName);
            return preset != null && ApplyPreset(preset, useTransition);
        }

        /// <summary>
        /// 切换到下一个预设
        /// </summary>
        public void NextPreset()
        {
            if (builtInPresets == null || builtInPresets.Length == 0) return;
            
            int currentIndex = Array.IndexOf(builtInPresets, currentPreset);
            int nextIndex = (currentIndex + 1) % builtInPresets.Length;
            ApplyPreset(nextIndex);
        }

        /// <summary>
        /// 获取预设名称列表
        /// </summary>
        public string[] GetPresetNames()
        {
            if (builtInPresets == null) return new string[0];
            return builtInPresets.Where(p => p != null).Select(p => p.GetDisplayName()).ToArray();
        }

        #endregion
    }
}