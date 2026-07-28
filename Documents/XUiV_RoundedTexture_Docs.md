# XUiV_RoundedTexture 说明文档

## 概述

`XUiV_RoundedTexture` 是基于 `XUiV_Texture` 扩展的自定义 UI 视图组件，用于动态生成圆角矩形纹理，支持自定义圆角半径、填充颜色和边框样式。

## XML 使用方式

```xml
<CATUI_roundedtexture 
    corner_radius="10" 
    fill_color="0.2,0.2,0.2,1" 
    border_color="0.6,0.6,0.6,1" 
    border_width="2" 
    width="200" 
    height="100" />
```

## 属性列表

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `corner_radius` | int | 8 | 圆角半径（0-100） |
| `fill_color` | string | 0.2,0.2,0.2,1 | 填充颜色（r,g,b,a） |
| `border_color` | string | 0.6,0.6,0.6,1 | 边框颜色（r,g,b,a） |
| `border_width` | int | 0 | 边框宽度（0-10） |

## 继承属性

继承自 `XUiV_Texture`，支持以下属性：

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `width` | int | 宽度 |
| `height` | int | 高度 |

## 使用示例

### 基础圆角面板

```xml
<CATUI_roundedtexture 
    corner_radius="12" 
    fill_color="0.15,0.15,0.15,0.9" 
    width="300" 
    height="200" />
```

### 带边框的圆角面板

```xml
<CATUI_roundedtexture 
    corner_radius="8" 
    fill_color="0.25,0.25,0.25,1" 
    border_color="0.5,0.8,0.5,1" 
    border_width="1" 
    width="400" 
    height="300" />
```

### 无圆角（矩形）

```xml
<CATUI_roundedtexture 
    corner_radius="0" 
    fill_color="0.3,0.3,0.3,1" 
    width="100" 
    height="50" />
```

### 圆形（宽高相等）

```xml
<CATUI_roundedtexture 
    corner_radius="50" 
    fill_color="1,0,0,1" 
    width="100" 
    height="100" />
```

## 工作原理

1. 在 `InitView` 中调用 `GenerateRoundedTexture()` 生成纹理
2. 使用 2x 分辨率缩放生成纹理（抗锯齿）
3. 通过计算像素到角点的距离实现圆角效果
4. 边框通过判断像素位置和距离实现
5. 属性变化时自动重新生成纹理

## 纹理生成流程

```
1. 获取尺寸和圆角参数
2. 计算实际圆角半径（不超过宽高的一半）
3. 创建 2x 分辨率的 Texture2D
4. 遍历每个像素，计算圆角透明度和边框
5. 设置纹理像素并应用
6. 赋值给 Texture 属性
```

## 注意事项

1. `corner_radius` 超过宽高一半时，会自动限制为宽高的一半
2. `border_width` 超过 `corner_radius` 时可能产生视觉异常
3. 纹理在属性变化时重新生成，频繁修改可能影响性能
4. 宽高必须大于 0，否则不会生成纹理
5. 抗锯齿通过 2x 缩放实现，纹理质量较高但占用更多内存
