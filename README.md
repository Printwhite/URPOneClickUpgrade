# URP 一键升级工具

## 📋 简介

这是一个 Unity 编辑器插件，用于将项目从**内置渲染管线 (Built-in Render Pipeline)** 一键升级到 **URP (Universal Render Pipeline)**。

## ✨ 功能特点

### 主工具 (URP 一键升级工具)
- ✅ 自动检测并安装 URP 包
- ✅ 自动创建 URP Asset 和 Renderer
- ✅ 自动配置 Graphics Settings 和 Quality Settings
- ✅ 批量升级材质 (Standard → URP/Lit)
- ✅ 自动为相机添加 UniversalAdditionalCameraData
- ✅ 自动为灯光添加 UniversalAdditionalLightData
- ✅ 详细的升级日志
- ✅ 支持还原到内置管线

### 辅助工具
- 🔍 **预检查工具**: 升级前扫描项目，列出需要处理的内容
- 🔄 **着色器批量替换**: 自定义源/目标着色器进行批量替换
- 🛠️ **快速设置**: 一键修复粉红色材质、快速打开设置面板

## 📦 安装方法

1. 将 `URPUpgradeTool` 文件夹复制到你的 Unity 项目的 `Assets` 目录下
2. 等待 Unity 编译完成
3. 在菜单栏找到 `Tools > URP 一键升级工具`

## 🚀 使用方法

### 完整升级流程

1. **打开工具**: `Tools > URP 一键升级工具`

2. **安装 URP 包** (如果尚未安装):
   - 点击 "安装 URP 包" 按钮
   - 等待安装完成

3. **选择升级选项**:
   - 创建 URP Asset ✓
   - 配置 Graphics Settings ✓
   - 升级材质 ✓
   - 升级相机 ✓
   - 升级灯光 ✓

4. **执行升级**:
   - 点击 "⚡ 一键完整升级" 按钮
   - 确认对话框后开始升级

5. **检查结果**:
   - 查看升级日志
   - 在场景中检查效果

### 单独执行某一步

如果你只想执行某一步操作，可以使用 "单独执行" 区域的按钮：

- **创建 URP Asset**: 只创建 URP 配置文件
- **配置 Graphics**: 只设置渲染管线
- **升级场景材质**: 只升级当前场景中的材质
- **升级项目材质**: 只升级 Assets 文件夹中的材质
- **升级相机**: 只为相机添加 URP 组件
- **升级灯光**: 只为灯光添加 URP 组件

### 预检查

建议在升级前使用预检查工具：

1. `Tools > URP 升级助手 > 预检查工具`
2. 点击 "扫描项目"
3. 查看警告和材质列表
4. 处理自定义着色器

## 📝 材质着色器映射

| 原着色器 | 目标 URP 着色器 |
|---------|----------------|
| Standard | Universal Render Pipeline/Lit |
| Standard (Specular setup) | Universal Render Pipeline/Lit |
| Unlit/* | Universal Render Pipeline/Unlit |
| Particles/* | Universal Render Pipeline/Particles/Lit |
| Legacy Shaders/* | Universal Render Pipeline/Lit |
| Mobile/* | Universal Render Pipeline/Lit |

## ⚠️ 注意事项

1. **备份项目**: 升级前强烈建议备份整个项目

2. **自定义着色器**: 
   - 工具无法自动转换自定义着色器
   - 需要手动重写为 URP 兼容版本或使用 Shader Graph

3. **Post Processing**:
   - URP 使用 Volume 系统而非 Post Processing Stack
   - 需要手动迁移后处理效果

4. **第三方插件**:
   - 检查第三方插件是否支持 URP
   - 可能需要更新或更换插件

5. **Color Space**:
   - 建议将 Color Space 设置为 Linear
   - `Edit > Project Settings > Player > Other Settings > Color Space`

## 🔧 常见问题

### Q: 升级后材质变成粉红色？
A: 使用 `Tools > URP 升级助手 > 快速设置 > 修复粉红色材质`

### Q: Graphics Settings 在哪里设置？
A: `Edit > Project Settings > Graphics` 或使用工具的 "配置 Graphics" 按钮

### Q: 如何还原到内置管线？
A: 点击 "清除所有 URP 设置" 按钮，但材质着色器不会自动还原

### Q: 支持哪些 Unity 版本？
A: 建议 Unity 2020.3 LTS 或更高版本

## 📁 文件结构

```
URPUpgradeTool/
├── Editor/
│   ├── URPOneClickUpgrade.cs    # 主升级工具
│   └── URPUpgradeHelper.cs      # 辅助工具集
└── README.md                     # 说明文档
```

## 📄 License

MIT License - 可自由使用和修改

## 🤝 反馈

如有问题或建议，欢迎反馈！
