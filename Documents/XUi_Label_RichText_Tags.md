# 7D2D XUi Label 富文本标签速查

7D2D 的 `label` 文本富文本由 **NGUI 符号系统** 解析（`NGUIText.ParseSymbol`），并叠加了 7D2D 自研扩展。
所有标签在 UILabel 渲染文本时生效，**binding 表达式（`{# ... }`）拼出的字符串同样适用**。

核心特点：颜色使用**栈式**结构（`[-]` 逐层回退），样式开关成对出现。

## 一、颜色

| 标签 | 作用 |
|---|---|
| `[RRGGBB]` | 设置颜色并压入颜色栈（如 `[43CF7C]`） |
| `[RRGGBBAA]` | 设置带 Alpha 的颜色（RRGGBB + AA 共 8 位十六进制） |
| `[HH]` | 仅设置透明度（两位十六进制） |
| `[-]` | 弹出颜色栈，恢复上一个颜色 |

注意：**没有** `[c=...]` 写法（`[c]` 是"忽略颜色"开关，不是 color=）。

### 颜色示例

```
[43CF7C]材料充足[-] / [FF5252]材料不足[-]
{# (CATUI_numprefix(havecount) >= CATUI_numprefix(needcount) ? '[43CF7C]' : '[FF5252]') + havecount + '[-]' }/{needcount}
```

嵌套：颜色可多层嵌套，`[-]` 每次回退一层：

```
[FF0000]红[A0A0A0]灰[-]回到红[-]默认
```

## 二、文本样式（成对开关）

| 标签 | 作用 |
|---|---|
| `[b]` / `[/b]` | 加粗 |
| `[i]` / `[/i]` | 斜体 |
| `[u]` / `[/u]` | 下划线 |
| `[s]` / `[/s]` | 删除线 |
| `[sub]` | 下标 |
| `[sup]` | 上标 |
| `[/sub]` / `[/sup]` | 恢复正体 |
| `[c]` / `[/c]` | 忽略颜色（渲染时跳过染色，多用于 url 内部） |
| `[t]` / `[/t]` | 强制使用 sprite 自身颜色 |

## 三、可点击链接

```
[url=...]文本[/url]
```

- 需要 label 开启 `support_urls="true"`（XML 属性 `support_urls`，类型白名单逗号分隔，如 `support_urls="HTTP,DiscordMessageButton"`）。
- 7D2D 用函数式参数拼 URL（`LabelUrlUtils.BuildUrlFunctionString`），形如 `[url=Type=HTTP&MessageId=xxx]...[/url]`。

## 四、动作标记（7D2D 特有）

- label `parse_actions="true"` 时由 `XUiUtils.ParseActionsMarkup` 解析 `[link=...]` 类标记，常用于手柄按键提示与交互。

## 五、内联图标 / 符号（7D2D 特有，最常用）

```
[sp=符号名]
```

内联渲染字体符号表（NGUI BMSymbol）中的图标。常见示例：

| 写法 | 显示 |
|---|---|
| `[sp=ui_stat]` | 星星（属性加强标记，`XUiM_ItemStack.cs:336` 使用） |
| `[sp=XB_Button_A]` / `[sp=PS5_Button_X]` | 手柄按键图标 |
| `[sp=Mouse_LeftButton_Large]` | 鼠标按键图标 |
| `[sp=ui_game_symbol_external_link]` | 外链图标 |

## 六、注意事项

1. **颜色栈式**：`[-]` 只回退一层，需嵌套时逐层闭合。
2. **字面 `[`**：若 `[` 后内容拼不出合法符号，则按普通字符渲染，不会报错。
3. **动态字体 vs 位图字体**：加粗/斜体等样式在位图字体下由符号开关控制；动态字体（DynamicFont）可能走 fontStyle，行为略有差异。
4. **去符号**：`NGUIText.StripSymbols` 可去除全部符号还原纯文本（聊天输入等场景使用）。
5. **换行**用 `\n`，不属于标签。

## 七、相关源码位置

| 文件 | 内容 |
|---|---|
| `NGUI-Decompiled\NGUIText.cs` | `ParseSymbol` 符号解析、`StripSymbols` 去符号 |
| `Assembly-CSharp-Decompiled\XUiV_LabelBase.cs` | `support_urls` / `parse_actions` 属性、URL 点击处理 |
| `Assembly-CSharp-Decompiled\LabelUrlUtils.cs` | URL 函数式构造与点击分发 |
| `Assembly-CSharp-Decompiled\XUiUtils.cs` | `ParseActionsMarkup` 动作标记解析 |
