# 📤 Git仓库上传指南

## 🎯 首次上传步骤

### 1. 初始化本地Git仓库
```bash
cd "D:\u3d-projects\WorldEditor"
git init
```

### 2. 添加远程仓库
```bash
git remote add origin https://github.com/lsc0103/WorldEditor.git
```

### 3. 创建.gitignore文件
```bash
# Unity generated files
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/

# Visual Studio cache/options
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Unity3D generated file on crash reports
sysinfo.txt

# Mac
.DS_Store

# Windows
Thumbs.db
ehthumbs.db
Desktop.ini
```

### 4. 添加所有文件到暂存区
```bash
git add .
```

### 5. 创建初始提交
```bash
git commit -m "feat: initial commit with environment system blueprint

- Add complete environment system design blueprint
- Add detailed 10-week development plan
- Add progress tracking system
- Add project documentation and README
- Establish project structure and architecture

Related: Environment System Development Phase 1"
```

### 6. 推送到GitHub
```bash
git branch -M main
git push -u origin main
```

## 📁 建议的文件上传顺序

### 第一次提交 (文档和规划)
```bash
git add README.md
git add ENVIRONMENT_SYSTEM_BLUEPRINT.md
git add DEVELOPMENT_PLAN.md
git add PROGRESS_TRACKER.md
git add GIT_UPLOAD_GUIDE.md
git add .gitignore
git commit -m "docs: add project documentation and development plan"
```

### 第二次提交 (现有代码)
```bash
git add Assets/
git add ProjectSettings/
git add Packages/
git commit -m "feat: add existing WorldEditor core systems

- Add terrain generation and editing system
- Add smart placement system framework
- Add vegetation system foundation
- Add basic UI and editor tools
- Add project settings and dependencies"
```

## 🔄 日常开发提交规范

### 提交信息格式
```
<类型>(<范围>): <简短描述>

[详细描述 - 可选]

[相关问题 - 可选]
```

### 类型说明
- `feat`: 新功能
- `fix`: Bug修复
- `docs`: 文档更新
- `style`: 代码格式(不影响代码运行)
- `refactor`: 重构(既不是新功能也不是bug修复)
- `test`: 添加测试
- `chore`: 构建过程或辅助工具的变动
- `perf`: 性能优化

### 范围说明
- `environment`: 环境系统相关
- `water`: 水体系统相关
- `weather`: 天气系统相关
- `lighting`: 光照系统相关
- `time`: 时间系统相关
- `ui`: 用户界面相关
- `editor`: 编辑器工具相关
- `core`: 核心系统相关

### 提交示例
```bash
# 新功能
git commit -m "feat(environment): add core environment manager

- Implement EnvironmentManager singleton
- Add environment state management
- Add configuration system
- Add basic editor integration

Closes #123"

# Bug修复
git commit -m "fix(lighting): correct sun position calculation

- Fix sun angle calculation in TimeSystem
- Resolve shadow flickering issue
- Update light intensity curve

Fixes #456"

# 文档更新
git commit -m "docs(progress): update development progress

- Mark Day 1 tasks as completed
- Add Day 2 planning details
- Update milestone progress to 5%"
```

## 📋 每日提交检查清单

### 提交前检查
- [ ] 代码编译无错误
- [ ] 运行基础测试通过
- [ ] 更新相关文档
- [ ] 检查代码格式和注释
- [ ] 更新进度跟踪文件

### 提交命令序列
```bash
# 1. 检查状态
git status

# 2. 查看变更
git diff

# 3. 添加文件
git add <files>

# 4. 提交
git commit -m "提交信息"

# 5. 推送
git push origin main

# 6. 更新进度文档
# 编辑 PROGRESS_TRACKER.md
git add PROGRESS_TRACKER.md
git commit -m "docs(progress): update daily progress tracking"
git push origin main
```

## 🌿 分支管理策略

### 主要分支
- `main`: 稳定版本分支
- `develop`: 开发分支
- `feature/*`: 功能开发分支
- `hotfix/*`: 紧急修复分支

### 分支工作流
```bash
# 创建功能分支
git checkout -b feature/environment-core

# 开发完成后合并到develop
git checkout develop
git merge feature/environment-core

# 测试稳定后合并到main
git checkout main
git merge develop
git tag v1.0.0
```

## 📊 提交质量标准

### 代码质量
- 编译无警告
- 运行无错误
- 性能无明显下降
- 内存无泄漏

### 提交质量
- 单次提交功能完整
- 提交信息清晰明确
- 包含必要的测试
- 文档同步更新

## 🚨 紧急情况处理

### 撤销最后一次提交
```bash
git reset --soft HEAD~1  # 保留文件变更
git reset --hard HEAD~1  # 丢弃文件变更
```

### 修改最后一次提交信息
```bash
git commit --amend -m "新的提交信息"
```

### 强制推送(谨慎使用)
```bash
git push --force-with-lease origin main
```

---

**注意:** 请严格按照这个指南进行Git操作，确保代码仓库的整洁和开发历史的清晰！

**创建日期:** 2024-08-22  
**最后更新:** 2024-08-22