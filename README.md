# WorldEditor - Unity开放世界编辑器

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Development Status](https://img.shields.io/badge/Status-In%20Development-yellow)](PROGRESS_TRACKER.md)

## 项目简介

WorldEditor是一个Unity世界编辑工具，主要用于开放世界游戏开发。在开发过程中经常遇到地形生成、环境设计等需求，所以整理了这个工具集。
B站【World Editor世界编辑器v1.0.0演示】 https://www.bilibili.com/video/BV1cDhrzEETf/?share_source=copy_web&vd_source=7abd7b4f65d293fae38bb38793173ebf

### 核心特性

- **地形系统** - 支持程序化生成与编辑
- **植被系统** - 可按照生态规则分布植被
- **水体系统** - 包含湖泊、河流等水体功能
- **天气系统** - 支持多种天气变化效果
- **时间系统** - 包含昼夜循环和光照控制
- **模块化设计** - 便于后期扩展维护
- **性能优化** - 针对大场景进行了优化

## 当前开发状态

**当前版本:** v0.1.0-alpha  
**开发阶段:** 环境系统开发  
**完成度:** 5%  

查看详细进度: [开发进度跟踪](PROGRESS_TRACKER.md)

## 系统架构

```
WorldEditor/
├── Core/                    # 核心系统
│   ├── TerrainSystem       # 地形系统
│   ├── PlacementSystem     # 智能放置系统
│   └── SaveSystem          # 存储系统
├── Environment/            # 环境系统 (开发中)
│   ├── TimeSystem          # 时间系统
│   ├── LightingSystem      # 光照系统
│   ├── WeatherSystem       # 天气系统
│   └── WaterSystem         # 水体系统
├── Ecosystem/              # 生态系统
│   ├── VegetationSystem    # 植被系统
│   └── WildlifeSystem      # 野生动物系统
└── Tools/                  # 编辑器工具
    ├── UI                  # 用户界面
    └── Editor              # Unity编辑器扩展
```

## 文档导航

### 规划文档
- [环境系统设计蓝图](ENVIRONMENT_SYSTEM_BLUEPRINT.md) - 完整的环境系统设计
- [开发计划](DEVELOPMENT_PLAN.md) - 详细的开发时间表
- [进度跟踪](PROGRESS_TRACKER.md) - 实时开发进度

### 技术文档
- [安装指南](docs/INSTALLATION.md) - 项目安装和配置
- [API参考](docs/API_REFERENCE.md) - 代码API文档
- [使用教程](docs/TUTORIALS.md) - 用户使用指南

### 测试文档
- [测试报告](docs/TEST_REPORTS.md) - 测试结果和分析
- [性能基准](docs/PERFORMANCE.md) - 性能测试数据

## 快速开始

### 系统要求

- **Unity版本:** 2022.3 LTS 或更高
- **渲染管道:** Universal Render Pipeline (URP)
- **平台支持:** Windows, macOS, Linux
- **最低配置:** 8GB RAM, GTX 1060 / RX 580

### 安装步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/lsc0103/WorldEditor.git
   cd WorldEditor
   ```

2. **在Unity中打开项目**
   - 启动Unity Hub
   - 选择"Open" -> 选择WorldEditor文件夹
   - 等待项目加载和导入

3. **验证安装**
   - 打开场景: `Assets/Scenes/WorldEditorDemo.unity`
   - 点击播放按钮测试基础功能

详细安装指南: [安装文档](docs/INSTALLATION.md)

## 开发状态

### 已完成功能
- [x] 基础地形生成和编辑
- [x] 植被放置系统框架
- [x] 项目架构设计
- [x] 开发计划制定

### 开发中功能
- [ ] 环境系统核心架构 (进行中)
- [ ] 时间和昼夜循环系统
- [ ] 基础光照系统

### 计划中功能
- [ ] 天空和大气系统
- [ ] 天气系统
- [ ] 水体系统
- [ ] 高级特效和音效

## 参与贡献

欢迎对项目感兴趣的开发者参与：

### 贡献方式
- [报告Bug](https://github.com/lsc0103/WorldEditor/issues)
- [提出功能建议](https://github.com/lsc0103/WorldEditor/discussions)
- 改进文档
- 提交代码

### 开发流程
1. Fork 项目仓库
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

详细贡献指南: [CONTRIBUTING.md](CONTRIBUTING.md)

## 项目统计

### 代码统计
- **代码行数:** 约13,000行
- **C#脚本:** 76个
- **Shader数量:** 12个
- **文档数量:** 18个

### 开发人员
- **主要开发:** LSC
- **其他贡献者:** 暂无

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 相关链接

- **项目主页:** https://github.com/lsc0103/WorldEditor
- **问题追踪:** https://github.com/lsc0103/WorldEditor/issues
- **讨论区:** https://github.com/lsc0103/WorldEditor/discussions
- **Wiki:** https://github.com/lsc0103/WorldEditor/wiki

## 联系信息

- **维护人员:** LSC
- **问题反馈:** GitHub Issues

## 致谢

感谢相关开源项目和Unity引擎的支持。

---

如果项目对你有用，可以点个Star支持一下。

**最后更新:** 2025-08-22  
**版本:** v0.1.0-alpha
