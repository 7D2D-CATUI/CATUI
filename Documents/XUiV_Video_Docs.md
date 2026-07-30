# XUiV_Video 说明文档

## 概述

`XUiV_Video` 是七日杀内置的视频播放 UI 组件，继承自 `XUiV_TextureBased`。它基于 Unity 的 `VideoPlayer` 组件实现视频播放，视频渲染到 `RenderTexture` 后通过 `UITexture` 显示在界面上。

## XML 使用方式

```xml
<video 
    uri="Video/intro.webm" 
    loop="true" 
    videoaspect="FitInside" 
    width="400" 
    height="300" />
```

## 属性列表

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `uri` | string | 空 | 视频文件路径（支持多种格式，详见下方） |
| `loop` | bool | false | 是否循环播放 |
| `videoaspect` | VideoAspectRatio | FitInside | 视频画面适配方式 |

### videoaspect 可选值

| 值 | 说明 |
|----|------|
| `FitInside` | 视频等比缩放适应控件，保持完整显示（可能有黑边） |
| `FitOutside` | 视频等比缩放覆盖控件，保持完整覆盖（可能被裁切） |
| `Stretch` | 视频拉伸填充控件，不保持宽高比 |
| `AspectFit` | 视频等比缩放适应控件，保持完整显示 |
| `AspectCrop` | 视频等比缩放覆盖控件，保持居中裁切 |

### uri 路径格式

`uri` 属性支持以下几种路径格式：

| 格式 | 说明 | 示例 |
|------|------|------|
| 相对路径 | 相对于 `StreamingAssets` 目录 | `Video/intro.webm` |
| Mod 资源路径 | 通过 Mod 管理器加载 Mod 内的视频 | 自动识别 Mod 路径 |
| 本地文件路径 | 以 `@` 开头指向本地文件 | `@file:///C:/Videos/intro.webm` |
| 网络 URL | 以 `@` 开头的网络地址 | `@https://example.com/video.mp4` |

**自动扩展名补全：** 若未指定扩展名，会自动尝试 `.webm` 和 `.mp4`（PS5 平台相反）。

## 继承属性

继承自 `XUiV_TextureBased` → `XUiV_ImageBased` → `XUiView`，支持以下常用属性：

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `width` | int | 控件宽度（像素） |
| `height` | int | 控件高度（像素） |
| `color` | string | 颜色/透明度（r,g,b,a） |
| `material` | string | 自定义材质 |
| `rect_offset` | Vector2 | UV 矩形偏移 |
| `rect_size` | Vector2 | UV 矩形尺寸 |
| `sourceaspectratiorespectpivot` | bool | 源宽高比是否遵循轴心点 |

## 使用示例

### 基础播放

```xml
<video 
    uri="Video/intro.webm" 
    width="480" 
    height="270" />
```

### 循环播放视频

```xml
<video 
    uri="Video/background.mp4" 
    loop="true" 
    videoaspect="Stretch"
    width="800" 
    height="600" />
```

### Mod 内视频

将视频文件放在 Mod 的 `StreamingAssets/Video/` 目录下：

```xml
<video 
    uri="Video/myvideo.webm" 
    loop="false"
    width="640" 
    height="360" />
```

### 作为背景视频

```xml
<video 
    uri="Video/bg.webm" 
    loop="true" 
    videoaspect="AspectCrop"
    width="1920" 
    height="1080"
    color="255,255,255,180" />
```

## 事件回调

| 事件 | 委托类型 | 说明 |
|------|---------|------|
| `VideoError` | `VideoErrorDelegate` | 视频加载或播放出错时触发 |
| `VideoFinished` | `VideoFinishedDelegate` | 视频播放完成时触发（非循环模式） |

## 代码访问示例

```csharp
// 获取视频视图
XUiV_Video videoView = xui.GetView("myVideo") as XUiV_Video;

// 控制播放
videoView.Playing = true;   // 播放
videoView.Playing = false;  // 暂停

// 跳转到指定时间
videoView.CurrentTime = 5.0;  // 跳转到第5秒

// 监听事件
videoView.VideoError += (sender) => {
    Log.Error("Video error: " + sender.VideoUri);
};
videoView.VideoFinished += (sender) => {
    Log.Out("Video finished: " + sender.VideoUri);
};

// 修改音量设置
videoView.VolumeSetting = EnumGamePrefs.OptionsMenuMasterVolumeLevel;
```

## 工作原理

1. **组件创建：** 在 `createComponents` 中自动添加 `VideoPlayer` 组件
2. **初始化：** 设置 `renderMode = RenderTexture`，绑定 `prepareCompleted`、`loopPointReached`、`errorReceived` 事件
3. **自动播放：** 窗口打开时（`OnOpen`），等待一帧后自动开始加载视频
4. **渲染纹理：** 根据控件尺寸动态创建 `RenderTexture`，将视频画面渲染到 `UITexture` 上
5. **音量同步：** 视频音量跟随游戏设置中的音量选项实时同步
6. **资源释放：** 窗口关闭或清理时销毁 `RenderTexture`，停止视频播放

## 支持的视频格式

- **PC/主机平台：** `.webm`（优先）、`.mp4`
- **PS5 平台：** `.mp4`（优先）、`.webm`
- 支持 Unity `VideoPlayer` 支持的所有格式

## 注意事项

1. **音频设置：** 默认跟随 `OptionsMenuMusicVolumeLevel`（音乐音量），可通过代码修改 `VolumeSetting` 属性改为其他音量设置
2. **尺寸自适应：** `RenderTexture` 尺寸根据控件 `width`/`height` 自动创建，修改尺寸后自动重建
3. **性能开销：** 视频解码和渲染需要 GPU 资源，建议仅在需要时使用，不建议同时播放多个视频
4. **窗口关闭：** 窗口关闭时视频会自动停止，`RenderTexture` 会被释放
5. **路径大小写：** URI 路径在 Windows 上不区分大小写，但在其他平台可能需要精确匹配
6. **Mod 路径：** Mod 内的视频文件会自动通过 `ModManager.TryPatchModPathString` 解析，无需特殊前缀
7. **延迟启动：** 视频在 `OnOpen` 后通过协程延迟一帧再开始加载，确保 UI 布局已完成