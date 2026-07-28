# XUiV_AnimatedSprite 说明文档

## 概述

`XUiV_AnimatedSprite` 是基于 `XUiV_Sprite` 扩展的自定义 UI 视图组件，用于支持精灵动画播放功能。它通过添加 `UISpriteAnimation` 组件实现帧动画效果。

## XML 使用方式

```xml
<CATUI_animatedsprite 
    spriteprefix="anim_" 
    loop="true" 
    framerate="30" 
    width="64" 
    height="64" />
```

## 属性列表

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `spriteprefix` | string | 空 | 精灵帧名称前缀，动画帧需命名为 `前缀_001`, `前缀_002`... |
| `loop` | bool | true | 是否循环播放动画 |
| `framerate` | int | 30 | 帧率（帧/秒） |

## 继承属性

继承自 `XUiV_Sprite`，支持以下属性：

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `width` | int | 宽度 |
| `height` | int | 高度 |
| `sprite` | string | 精灵名称 |
| `color` | string | 颜色（r,g,b,a） |
| `atlas` | string | 图集名称 |

## 使用示例

### 基础动画

```xml
<CATUI_animatedsprite 
    spriteprefix="fire_" 
    loop="true" 
    framerate="15" 
    width="128" 
    height="128" />
```

### 非循环动画

```xml
<CATUI_animatedsprite 
    spriteprefix="explosion_" 
    loop="false" 
    framerate="30" 
    width="256" 
    height="256" />
```

## 方法

| 方法名 | 说明 |
|--------|------|
| `PlayAnimation()` | 播放动画 |
| `PauseAnimation()` | 暂停动画 |
| `ResetAnimation()` | 重置动画到开始帧 |

## 工作原理

1. 在 `createComponents` 中自动添加 `UISpriteAnimation` 组件
2. 在 `updateData` 中设置动画参数（前缀、帧率、循环）
3. 当 `spriteprefix`、`loop` 属性变化时，自动重置并播放动画
4. 使用 Harmony 的 `Traverse` 禁用 `mSnap` 属性，避免动画卡顿

## 注意事项

1. 精灵帧图片必须在同一图集内
2. 帧图片命名格式为：`前缀_001`, `前缀_002`, ..., `前缀_099`
3. 图集加载完成后动画才会开始播放
4. 不支持自定义播放速度，通过调整 `framerate` 控制速度
