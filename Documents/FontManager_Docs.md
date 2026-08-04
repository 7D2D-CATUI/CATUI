# FontManager 字体功能使用文档

## 概述

FontManager 允许通过 `styles.xml` 定义自定义字体，并在界面 XML 中通过 `font_face` 属性引用。支持三种字体来源：NGUI 位图字体、Unity 动态字体（.ttf/.otf）和系统已安装字体。自定义字体加载完成后会注册到 XUi 字体系统，界面中所有 `font_face` 属性均可直接使用。

**重要**：mod 目录里的原始字体文件（.ttf/.otf/位图字体）无法被引擎直接加载（`@modfolder` 路径最终走 `Resources.Load`，不支持磁盘原始文件）。位图字体和动态字体**必须先打包成 AssetBundle**（.unity3d），系统字体则需先安装到操作系统。

## 文件与集成

| 文件 | 作用 |
|------|------|
| `Source/FontManager.cs` | `FontManager` 静态类，负责字体注册、加载与查询 |
| `Source/XUi_Harmony.cs` | `XUiPatch` 类，通过 Harmony 将字体系统接入原版 XUi |

集成方式：

- **`GetUIFontByName` Prefix**：拦截 `XUi.GetUIFontByName(name)`，优先从 FontManager 注册表查找自定义字体；未命中时回退到原版 `ReferenceFont`。
- **`loadAsync` Postfix**：在 XUi 加载 UI 前执行 `FontManager.LoadFonts(xui)` 协程，确保所有自定义字体先于界面解析完成加载。

## 快速开始

### 1. 打包字体为 AssetBundle

用 Unity 编辑器将字体打成 `.unity3d` 资源包（详见下文「资源打包」章节），放入 mod 目录，例如：

- `ZZZ_CATUI\UIAtlases\Fonts\catui_fonts.unity3d`

### 2. 在 styles.xml 中注册字体

```xml
<append xpath="/styles">
    <style name="Fonts.UnityFonts">
        <style_entry name="AlibabaPuHuiTiMedium" value="#@modfolder(CATUI):UIAtlases/Fonts/catui_fonts.unity3d?AlibabaPuHuiTiMedium" />
        <style_entry name="AmericanCaptain" value="#@modfolder(CATUI):UIAtlases/Fonts/catui_fonts.unity3d?AmericanCaptain" />
    </style>
</append>
```

### 3. 在界面 XML 中使用

```xml
<label font_face="AmericanCaptain" text="Hello World" font_size="40" />
```

## 字体类型说明

| 类型 | styles.xml 样式键 | 加载方式 | 典型用途 |
|------|------------------|----------|----------|
| NGUI 位图字体 | `Fonts.NGUIFonts` | 从 AssetBundle 加载 `NGUIFont` 资源 | 与原版 UI 风格一致的静态字体 |
| Unity 动态字体 | `Fonts.UnityFonts` | 从 AssetBundle 加载 Unity `Font`（.ttf/.otf），包装为动态 `NGUIFont` | 自定义字体、大字号标题 |
| 系统字体 | `Fonts.OSFonts` | 按系统已安装字体名称创建动态字体 | 中文等需要系统字体支持的场景 |

## styles.xml 配置详解

三种字体分别使用不同的样式键，配置格式均为：

```xml
<style name="Fonts.XXX">
    <style_entry name="字体注册名" value="资源路径或系统字体名" />
</style>
```

### Fonts.NGUIFonts（NGUI 位图字体）

```xml
<style name="Fonts.NGUIFonts">
    <style_entry name="MyBitmapFont" value="#@modfolder(CATUI):UIAtlases/Fonts/catui_fonts.unity3d?MyBitmapFont" />
</style>
```

- `name`：字体注册名，用于界面中的 `font_face="MyBitmapFont"`。
- `value`：AssetBundle 引用，`?` 后是 bundle 内 `NGUIFont` 资产的名称（须与打包时资产名一致）。

### Fonts.UnityFonts（Unity 动态字体）

```xml
<style name="Fonts.UnityFonts">
    <style_entry name="AlibabaPuHuiTiMedium" value="#@modfolder(CATUI):UIAtlases/Fonts/catui_fonts.unity3d?AlibabaPuHuiTiMedium" />
    <style_entry name="AmericanCaptain" value="#@modfolder(CATUI):UIAtlases/Fonts/catui_fonts.unity3d?AmericanCaptain" />
</style>
```

- `name`：字体注册名。
- `value`：AssetBundle 引用，`?` 后是 bundle 内 `.ttf`/`.otf` 资产的名称（资产名 = 打包时文件名去掉扩展名）。加载后包装为动态 `NGUIFont`，可随字号缩放。

### Fonts.OSFonts（系统字体）

```xml
<style name="Fonts.OSFonts">
    <style_entry name="ignored" value="Microsoft YaHei" />
</style>
```

**注意**：此类型使用 `value` 作为系统字体名称，`name` 属性会被忽略；注册到字体表时使用的名称即为该系统字体名。界面中直接引用系统字体名：

```xml
<label font_face="Microsoft YaHei" text="你好" font_size="22" />
```

### 路径语法

引用 AssetBundle 内资源的完整语法为：

```
#@modfolder(ModName):Bundle相对路径?资产名
```

- `#`：表示 bundle 资源（引擎据此走 `AssetBundleManager` 加载）。
- `@modfolder(ModName):...`：指向某个 mod 目录下的文件；`@modfolder(CATUI)` 会被替换为该 mod 的绝对路径。
- `?` 后为 bundle **内资产名**（= 打包时文件名去掉扩展名）。

示例：`#@modfolder(CATUI):UIAtlases/Fonts/catui_fonts.unity3d?AmericanCaptain` 表示加载 mod 的 `UIAtlases/Fonts/catui_fonts.unity3d` 包中名为 `AmericanCaptain` 的字体资产。

## 资源打包（AssetBundle）

位图字体与动态字体必须打包为 `.unity3d` 才能在游戏中加载。打包流程：

1. 在 Unity 工程中导入字体文件，并**将文件名（资产名）设为计划在 XML 中使用的名字**（例如 `.ttf` → `AlibabaPuHuiTiMedium`）。
2. 对字体资产设置 AssetBundle 名称（如 `catui_fonts`）。
3. 用 `BuildPipeline.BuildAssetBundles` 以 `StandaloneWindows64` 目标构建。
4. 把生成的 bundle 文件（无扩展名）复制到 mod 目录并改名为 `xxx.unity3d`（如 `catui_fonts.unity3d`）。

本项目已提供一键脚本：`H:\unity demo\CATUI\Assets\Editor\FontBundleBuilder.cs`（菜单 `CATUI/Build Font AssetBundle`），它会自动拷贝字体、设 bundle 名、构建并部署到 `ZZZ_CATUI\UIAtlases\Fonts\catui_fonts.unity3d`。

## XML 引用方式

任意使用字体的 UI 组件（如 `label`、`textfield`）均可通过 `font_face` 属性引用自定义字体：

```xml
<label font_face="MyDynamicFont" text="Custom Font" color="[white]" />
```

查找顺序：

1. FontManager 注册表（原版字体 + 自定义字体）。
2. 未命中时回退到原版 `ReferenceFont`，并在日志中输出警告。

## 技术原理

### 加载流程

1. `loadAsync` Postfix 在 XUi 加载界面之前启动 `FontManager.LoadFonts(xui)` 协程。
2. 等待 `XUiFromXml.HasData()` 完成（样式数据已解析）。
3. 注册 XUi 中所有原版 `NGUIFont`（按名称和 spriteName 两个键），并缓存 `ReferenceFont` 作为回退字体。
4. 依次读取三个样式键：
   - `Fonts.NGUIFonts` → `LoadNGUIFont(name, path)`
   - `Fonts.UnityFonts` → `LoadUnityFont(name, path)`
   - `Fonts.OSFonts` → `LoadOSInstalledFont(value)`
5. 已注册的字体名跳过加载（避免重复）。
6. 界面 XML 中 `font_face` 属性 → `XUiV_LabelBase` 调用 `XUi.GetUIFontByName` → 被 `GetUIFontByName` Prefix 接管，返回 FontManager 中的字体。

### 关键特性

- **先于 UI 加载**：字体在界面解析前完成注册，XML 引用不会出现字体缺失。
- **自动去重**：已注册的字体名不会重复加载。
- **回退机制**：未知字体名自动回退原版 `ReferenceFont`，界面不崩溃。
- **多来源支持**：位图字体、动态字体、系统字体三种方式按需混用。

## 日志与排错

### 常见日志信息

| 日志信息 | 含义 |
|---------|------|
| `Loading Fonts` | 开始加载字体 |
| `Loaded Fonts` | 字体加载完成 |
| `FontManager Font: X loaded` | 自定义字体 X 加载成功 |
| `Unable to load XUi Fonts` | 原版字体注册失败 |
| `Unable to load Font: X` | 自定义字体 X 加载失败 |
| `XUi font not found: X` | XML 引用了未注册的字体名，已回退原版字体 |

### 常见问题

#### 1. 字体不生效

**检查清单**：
- 确认 styles.xml 中样式键拼写正确（`Fonts.NGUIFonts` / `Fonts.UnityFonts` / `Fonts.OSFonts`）。
- 确认资源路径以 `@modfolder(ModName):` 开头且路径正确。
- 检查日志中是否有 `Unable to load Font`，确认字体文件实际存在。
- 确认 XML 中 `font_face` 属性与注册名完全一致。

#### 2. Unity 动态字体显示为默认字体

**排查建议**：
- 确认字体已打包为 `.unity3d` 资源包并放到 mod 目录。
- 确认 bundle 路径与 `?` 后的资产名正确（资产名 = 打包时文件名去扩展名）。
- 确认字体注册名在 `Fonts.UnityFonts` 样式下定义。
- 检查日志 `FontManager Font: X loaded` 是否输出。

#### 3. 系统字体（中文等）无法显示

**排查建议**：
- 确认系统中确实安装了对应字体（如 `Microsoft YaHei`）。
- 确认 `value` 填写的系统字体名称与实际字体名一致。
- 注意 `name` 属性对该类型无效，引用时使用 `value` 中的字体名。

## 相关文件

- 字体管理源码：[FontManager.cs](file:///h:/git/7D2D-CATUI/CATUI/Source/FontManager.cs)
- 集成补丁源码：[XUi_Harmony.cs](file:///h:/git/7D2D-CATUI/CATUI/Source/XUi_Harmony.cs)
- 打包脚本：[FontBundleBuilder.cs](file:///h:/unity%20demo/CATUI/Assets/Editor/FontBundleBuilder.cs)
- 字体解析：[XUiV_LabelBase.cs](file:///h:/git/7D2D-CATUI/CATUI/Assembly-CSharp-Decompiled/XUiV_LabelBase.cs)
- 字体查询：[XUi.cs](file:///h:/git/7D2D-CATUI/CATUI/Assembly-CSharp-Decompiled/XUi.cs)
