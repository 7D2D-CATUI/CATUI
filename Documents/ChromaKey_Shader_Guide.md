# CATUI 绿幕（ChromaKey）Shader 使用指南

## 概述

CATUI 绿幕功能通过自定义 Shader 实现视频背景的透明化处理。本指南将帮助你在 Unity 编辑器中编译 Shader 并打包成 AssetBundle，供游戏运行时加载。

---

## 第一步：创建 Unity 项目

1. 打开 Unity Hub，创建一个新项目（建议使用与七日杀相同的 Unity 版本：**Unity 2022.3 LTS**）
2. 项目模板选择 **3D (Built-in Render Pipeline)**

---

## 第二步：导入 Shader 源文件

1. 将 `ZZZ_CATUI/Resources/Shaders/CATUI_ChromaKey.shader` 复制到 Unity 项目的 `Assets/Shaders/` 目录下
2. Unity 会自动编译 Shader，等待编译完成
3. 在 Project 窗口中确认 `CATUI_ChromaKey.shader` 出现且无错误

---

## 第三步：创建 AssetBundle 构建脚本

1. 在 Unity 项目中创建 `Assets/Editor/` 目录（如不存在）
2. 将 `Documents/ChromaKeyShaderBuilder.cs` 复制到 `Assets/Editor/` 目录下
3. Unity 会自动编译该脚本

---

## 第四步：构建 AssetBundle

1. 在 Unity 菜单栏点击 **CATUI → Build ChromaKey Shader AssetBundle**
2. 等待构建完成，Console 窗口会显示：
   ```
   [CATUI] Starting ChromaKey shader build...
   [CATUI] Loaded shader: CATUI/ChromaKey
   [CATUI] Shader bundle built successfully!
   [CATUI] Built bundle: CATUI_Shaders
   ```
3. 构建完成后，在 Unity 项目的 `Assets/AssetBundles/` 目录下会生成：
   - `CATUI_Shaders`（无扩展名的 AssetBundle 文件）
   - `CATUI_Shaders.manifest`（清单文件）

---

## 第五步：部署 AssetBundle

1. 将生成的 `CATUI_Shaders` 文件（无扩展名）从 Unity 项目的 `Assets/AssetBundles/` 目录复制到
   `ZZZ_CATUI/Resources/Shaders/` 目录，并重命名为 `CATUI_Shaders.unity3d`
2. 最终目录结构：
   ```
   ZZZ_CATUI/
   ├── Resources/
   │   └── Shaders/
   │       ├── CATUI_ChromaKey.shader      (源文件，可选保留)
   │       └── CATUI_Shaders.unity3d      (编译后的 AssetBundle)
   └── CATUI.dll
   ```

---

## 第六步：游戏内使用

### XML 配置示例

```xml
<!-- 基础用法：去除绿色背景 -->
<video 
    name="myVideo" 
    url="@video/intro.mp4" 
    chromakey="true" />

<!-- 自定义蓝色背景去除 -->
<video 
    name="myVideo" 
    url="@video/intro.mp4" 
    chromakey="true" 
    keycolor="0,0,1" 
    tolerance="0.08" 
    smoothing="0.05" />

<!-- 完全自定义 -->
<video 
    name="myVideo" 
    url="@video/chroma_test.mp4" 
    chromakey="true" 
    keycolor="0,1,0" 
    tolerance="0.15" 
    smoothing="0.1" />
```

### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `chromakey` | bool | - | 启用绿幕效果 |
| `keycolor` | r,g,b[,a] | 0,1,0 | 要去除的颜色（默认绿色），取值范围 0-1 |
| `tolerance` | float | 0.1 | 颜色容差，值越大去除范围越广 |
| `smoothing` | float | 0.1 | 边缘平滑度，避免锯齿 |

### 参数调优建议

1. **绿色背景**：`keycolor="0,1,0"`, `tolerance="0.1"`, `smoothing="0.1"`
2. **蓝色背景**：`keycolor="0,0,1"`, `tolerance="0.08"`, `smoothing="0.05"`
3. **复杂背景**：减小 tolerance 和 smoothing 值
4. **硬边缘效果**：减小 smoothing 值（接近 0）

---

## 常见问题

### Q: 日志显示 "ChromaKey shader not found in AssetBundle"
**A**: 确保 `CATUI_Shaders.unity3d` 文件已正确复制到 `ZZZ_CATUI/Resources/Shaders/` 目录。

### Q: 视频完全透明或看不到
**A**: 减小 `tolerance` 值（如 0.05），或检查 `keycolor` 是否正确。

### Q: 边缘有锯齿
**A**: 增大 `smoothing` 值（如 0.15-0.2）。

### Q: 绿幕效果不生效
**A**: 检查 XML 中是否设置了 `chromakey="true"`，并查看日志中是否有 `[CATUI] ChromaKey enabled` 输出。

### Q: 游戏卡顿
**A**: GPU Shader 方案通常性能良好。如果视频分辨率过高，可考虑降低视频分辨率。

---

## 技术架构

```
XML 配置 → Harmony 补丁解析 → DataLoader.LoadAsset 加载 Shader → 应用到 UITexture.material
                                                                    ↓
                                                          GPU 实时处理绿幕效果
```

**AssetBundle 路径格式**：`#@modfolder(CATUI):Resources/Shaders/CATUI_Shaders.unity3d?`

---

## 文件清单

| 文件 | 说明 |
|------|------|
| `ZZZ_CATUI/Resources/Shaders/CATUI_ChromaKey.shader` | Shader 源文件（需在 Unity 中编译） |
| `ZZZ_CATUI/Resources/Shaders/CATUI_Shaders.unity3d` | 编译后的 AssetBundle（游戏运行时加载） |
| `Source/XUiV_VideoChromaKeyPatch.cs` | Harmony 补丁，加载 Shader 并应用到视频组件 |
| `Documents/ChromaKeyShaderBuilder.cs` | Unity Editor 构建脚本 |