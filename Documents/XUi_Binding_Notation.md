# 7D2D XUi 绑定语法速查（`#` 与 `%`）

7D2D 的 XUi 绑定（Binding）通过 `{...}` 包裹区块，前缀字符决定绑定类型与结果的“用法”。
本文聚焦 `#` 与 `%` 的核心区别，并附完整前缀总览与常见坑。

## 一、前缀总览

| 写法 | 类型 | 结果 | 典型属性 |
|---|---|---|---|
| `{绑定名}` | 普通绑定 | 绑定源返回的值（字符串） | 文本、显示值 |
| `{@paramName}` | 参数绑定 | 窗口/模板参数 | 文本、布局 |
| `{cvar(名字)}` | cvar 绑定 | 某 cvar 的值 | 文本、可见性 |
| `{# 表达式}` | **字符串表达式绑定** | NCalc 求值 → **字符串** | 文本、数值、颜色 |
| `{% 表达式}` | **布尔/条件绑定** | NCalc 求值 → **布尔** | `visible`、`enabled` 等开关 |

> NCalc 表达式示例：`{# int(x) + 1 }`、`{@ param > 0 }`、`{# localization('xuiFoo') }`。

## 二、`#` 与 `%` 的核心区别

二者都是 NCalc 表达式，求值结果相同，**区别在于结果如何交给属性**：

- **`#`（字符串表达式绑定）**
  表达式求值后**先转成字符串**，再把字符串交给属性的目标类型转换器（布尔/数字/颜色…）去解析。

- **`%`（布尔/条件绑定）**
  表达式求值结果**直接作为布尔**使用，不经过字符串往返，专用于需要 `true/false` 的属性（`visible`、`enabled`、`require`…）。

### 关键点
- `visible="{% has_entry or is_separator }"`：表达式结果就是布尔，直接判断。
- `visible="{# has_entry or is_separator }"`：会先把结果转成 `"True"/"False"` 字符串，再转回布尔。行为基本等价，但多了一次 string↔bool 转换。

## 三、使用建议

- **开关类属性**（`visible`、`enabled`、`require`）：优先用 `%`，语义更准，少一层转换。
- **展示类属性**（文本、数字、颜色、百分比）：用 `#`，或普通绑定 `{name}` / `{cvar(...)}`。
- 条件组合常用：`and` / `or` / `not`，cvar 比较如 `{cvar(_crouching) == 1}`。

### 示例
```xml
<!-- 布尔条件绑定：满足任一条件才显示 -->
<label visible="{% has_entry or is_separator }">…</label>
<!-- 数字计算（字符串）绑定 -->
<label text="{# int(x) + round(int(y)/2, 2) }">…</label>
```

## 四、常见坑：`visible` 值解析失败刷屏

字面量或字符串类 `visible` 在解析时，若目标类型（`Boolean`）转换失败，会触发原生
`XUiView.set_IsVisible` 的 `NullReferenceException`，在日志里形如：

```
Can not parse input ('false') into target type System.Boolean for attribute 'visible' on XUiV_Rect
EXC Object reference not set ... at XUiView.set_IsVisible ...
```

**规避思路**：这类布尔开关属性尽量用 `{% 表达式 }`（布尔直通），少用字面量 `"false"` 或 `#` 字符串往返，
可显著降低解析阶段报错概率。