# ChromaKey（绿幕）功能使用文档

## 概述

ChromaKey 功能允许在 `<video>` 组件上应用绿幕效果，将视频中的指定颜色（通常是绿色或蓝色）替换为透明，从而实现视频与背景的无缝融合。

## 快速开始

### 基础用法

```xml
<video 
    uri="Video/your_video.webm" 
    chromakey="true" 
    width="800" 
    height="600" />
```

### 自定义参数

```xml
<video 
    uri="Video/your_video.webm" 
    chromakey="true" 
    keycolor="0,255,0" 
    tolerance="0.15" 
    smoothing="0.1"
    width="800" 
    height="600" />
```

## XML 属性说明

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `chromakey` | bool | false | 是否启用绿幕功能 |
| `keycolor` | string | `0,0,0` | 要扣掉的颜色，格式为 `r,g,b` 或 `r,g,b,a`（0-255） |
| `tolerance` | float | 0.15 | 颜色容差，0-1 范围，值越大扣除的颜色范围越广 |
| `smoothing` | float | 0.1 | 边缘平滑度，0-1 范围，值越大边缘越柔和 |

### keycolor 格式说明

```xml
<!-- 纯绿色（标准绿幕） -->
keycolor="0,255,0"

<!-- 纯蓝色 -->
keycolor="0,0,255"

<!-- 深绿色 -->
keycolor="0,128,0"

<!-- 带透明度的颜色（可选） -->
keycolor="0,255,0,255"
```

### tolerance 和 smoothing 说明

- **tolerance（容差）**：控制颜色匹配的严格程度
  - `0`：只完全匹配指定颜色的像素
  - `0.15`：默认值，适用于大多数绿幕场景
  - `0.5`：宽松匹配，适用于光照不均匀的情况

- **smoothing（平滑）**：控制边缘的柔和程度
  - `0`：硬边，可能出现锯齿
  - `0.1`：默认值，提供自然的边缘过渡
  - `0.5`：非常柔和的边缘

## 层级控制（Depth）

ChromaKey 视频支持 NGUI 的 `depth` 属性来控制渲染层级：

```xml
<!-- 背景图片 - 在最底层 -->
<sprite depth="1" sprite="background" ... />

<!-- ChromaKey 视频 - 在中间层 -->
<video depth="2" chromakey="true" uri="Video/demo.webm" ... />

<!-- 文字标签 - 在最顶层（覆盖视频） -->
<label depth="3" text="Hello World" ... />
```

**层级效果**：背景图片 < 视频 < 文字

## 完整示例

### 示例 1：简单绿幕视频

```xml
<video 
    uri="Video/green_screen.webm" 
    chromakey="true" 
    width="640" 
    height="360" />
```

### 示例 2：绿幕视频叠放在背景上

```xml
<!-- 背景图片 -->
<sprite 
    depth="1" 
    sprite="ui_background" 
    width="1920" 
    height="1080" />

<!-- 绿幕视频，keycolor 为纯绿色 -->
<video 
    depth="2" 
    uri="Video/presenter.webm" 
    chromakey="true" 
    keycolor="0,255,0"
    tolerance="0.1"
    smoothing="0.05"
    width="800" 
    height="600"
    color="255,255,255,255" />

<!-- UI 文字覆盖在视频上方 -->
<label 
    depth="3" 
    text="特别呈现" 
    color="255,255,255,255"
    width="400"
    height="60"
    font_size="48" />
```

### 示例 3：蓝色背景扣像

```xml
<video 
    uri="Video/blue_screen.mp4" 
    chromakey="true" 
    keycolor="0,0,255"
    tolerance="0.2"
    smoothing="0.15"
    width="1280" 
    height="720" />
```

## 技术原理

### 渲染流程

1. **Shader 加载**：从 AssetBundle 加载自定义 ChromaKey Shader（`CATUI/ChromaKey`）
2. **材质创建**：为启用 ChromaKey 的视频创建自定义材质
3. **纹理绑定**：将视频纹理绑定到材质的 `_MainTex`
4. **Shader 处理**：每帧渲染时，Shader 检测每个像素与 `_KeyColor` 的差异
5. **颜色替换**：差异小于 `_Tolerance` 的像素被设为透明
6. **边缘平滑**：使用 `_Smoothing` 参数对边缘像素进行平滑过渡

### 关键特性

- **自动继承渲染队列**：自动获取游戏默认 UI 的 renderQueue，确保 `depth` 属性正常工作
- **材质持久化**：窗口关闭时保留材质，重新打开时无需重建
- **Shader 修复机制**：拦截 `UIDrawCall.UpdateMaterials()` 调用，防止 NGUI 覆盖自定义 Shader
- **动态参数更新**：`tolerance`、`smoothing` 参数可在运行时动态调整

## 调试与故障排除

### 启用调试日志

在游戏日志中搜索 `[CATUI]` 关键字可以查看 ChromaKey 的运行状态：

```
grep "\[CATUI\]" output_log.txt
```

### 常见日志信息

| 日志信息 | 含义 |
|---------|------|
| `ChromaKey enabled for ...` | ChromaKey 属性被正确解析 |
| `Loading shader from: ...` | 开始加载 Shader |
| `Shader loaded: True, supported: True` | Shader 加载成功 |
| `Inherited renderQueue: 3000` | 成功获取默认渲染队列 |
| `Creating NEW ChromaKey material for ...` | 创建新的 ChromaKey 材质 |
| `UpdateMaterials FIX for ...` | 检测到 Shader 被覆盖并自动修复 |

### 常见问题

#### 1. 绿幕效果不生效

**检查清单**：
- 确认 `chromakey="true"` 属性已设置
- 确认 Shader AssetBundle 存在于 `Resources/Shaders/CATUI_Shaders.unity3d`
- 检查日志中是否有 `Shader loaded: True` 信息

#### 2. 视频被其他 UI 元素遮挡

**解决方案**：
- 使用 `depth` 属性调整层级
- 确保视频的 `depth` 小于需要覆盖它的元素

#### 3. 边缘出现锯齿或光晕

**调整建议**：
- 增加 `smoothing` 值（0.1-0.3）来柔化边缘
- 减小 `tolerance` 值（0.05-0.1）来更精确地匹配颜色

#### 4. 部分背景未被扣除

**调整建议**：
- 增大 `tolerance` 值（0.2-0.35）来扩大颜色匹配范围
- 检查视频源的光照均匀性

#### 5. 第二次打开窗口后绿幕失效

**已修复**：最新版本在 `OnClose` 时保留材质，重新打开时会自动恢复

## 资源文件

ChromaKey 功能需要以下资源文件：

```
ZZZ_CATUI/
├── Resources/
│   └── Shaders/
│       ├── CATUI_Shaders.unity3d    # Shader AssetBundle
│       └── CATUI_ChromaKey.shader   # Shader 源文件（Unity 编辑器中使用）
└── CATUI.dll                         # 主插件 DLL
```

## 更新历史

| 版本 | 日期 | 更新内容 |
|------|------|---------|
| 1.0 | 2026-07-30 | 初始版本，支持基础绿幕功能 |
| 1.1 | 2026-07-30 | 修复 renderQueue 继承，支持 depth 属性 |
| 1.2 | 2026-07-30 | 修复 Shader 被覆盖问题（UpdateMaterials 拦截） |
| 1.3 | 2026-07-30 | 修复窗口重开后绿幕失效问题 |

## 相关文件

- 源码：[XUiV_VideoChromaKeyPatch.cs](file:///h:/git/7D2D-CATUI/CATUI/Source/XUiV_VideoChromaKeyPatch.cs)
- Shader：[CATUI_ChromaKey.shader](file:///h:/git/7D2D-CATUI/CATUI/ZZZ_CATUI/Resources/Shaders/CATUI_ChromaKey.shader)
- 视频组件文档：[XUiV_Video_Docs.md](file:///h:/git/7D2D-CATUI/CATUI/Documents/XUiV_Video_Docs.md)
