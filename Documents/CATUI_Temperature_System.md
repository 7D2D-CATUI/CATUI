# CATUI 温度系统文档

记录 CATUI 温度显示相关的数据口径、阈值、零部件与常见问题，
用于日后改温度 UI（数字、颜色、温条）时快速对照。

## 一、数据链路口径（很重要）

游戏温度系统用两个 buff cvar（**0~100，本质是“华氏体感”刻度**）：

- `_coretemp`：体感核心温度（决定玩家热/冷 buff）
- `_outsidetemp`：外部环境温度

### 两种“数值体系”必须分清

| 显示/判定 | 取值 | 说明 |
|---|---|---|
| CATUI 数字显示 | `XUiM_Player.GetCoreTemp()` → `ValueDisplayFormatters.Temperature(_coretemp, -1)` | 把 0~100 **当作华氏换算显示**（摄氏度设置下转 `ToCelsius`）|
| 温度 buff 判定 | 原始 `_coretemp`（华氏 0~100） | buffs.xml 里直接比较 cvar |

> 例：`_coretemp=32`（华氏 32 = 冰点）→ 数字显示 `0°C`（若设摄氏度）；
> 同时 `buffElementFreezing` 触发。所以“数字”和“buff 阈值”是两套量纲，**不是错误**。

## 二、温度 buff 阈值（游戏原版 buffs.xml）

| buff | 触发条件（`_coretemp`） | 图标 |
|---|---|---|
| `buffElementFreezing` | `<= 32` | 冷 |
| `buffElementCold` | `> 32` 且 `< 50` | 冷 |
| `buffElementHot` | `> 85` 且 `< 100` | 热 |
| `buffElementSweltering` | `>= 100` | 热(红) |

## 三、CATUI 内温度相关实现

### 数字与颜色绑定（`XUiC_HUDStatBarPatch.cs` GetBindingValueInternalPrefix）

- `CATUI_coretemp`：`XUiM_Player.GetCoreTemp(...)`（换算后数字）
- `CATUI_coretempcolor`：按 `_coretemp` 原始值分档
  - `<= 32` → 蓝 `0,153,255`
  - `> 32 and <= 50` → 青 `0,255,255`
  - `>= 85 and < 100` → 橙 `255,128,0`
  - `>= 100` → 红 `255,0,0`
  - 其余 → 白 `255,255,255`
- `CATUI_outsidetemp` / `CATUI_outsidetempcolor`：同结构，用 `_outsidetemp`

> 颜色档位与 buff 阈值基本对齐（仅 50 / 85 处边界略有差异）。

### 温度条（TempBar，windows.xml）

- `TempBar` rect：宽 `132`、高 `30`，`controller="HUDStatBar"`，`visible="{statvisible}"`
- 分段子 `sprite`：左冷 → 右热（蓝→青→白→橙→红），居中一根白色竖线 `tempBarMark`
- 竖线由 `XUiC_HUDStatBarPatch.UpdatePostfix` 逐帧驱动：

```csharp
float coretemp = localPlayer.Buffs.GetCustomVar("_coretemp");
// 0~132 活动区，中心 66，低温在左
int x = (int)Mathf.Clamp(coretemp, TempBarLeft, TempBarRight) - (int)TempBarCenter;
_tempBarMark.position = new Vector2i(x, 0);
_tempBarMark.positionDirty = true;
_tempBarMark.TryUpdatePosition();
```

活动区 **固定 0~132、中心 66**，不随父宽度变化；`_coretemp` 最大 100，热端不会顶满右侧。

### HUD 上的核心温度入口（HUDLeftStatBars）

```xml
<rect name="CoreTemp" controller="HUDStatBar" visible="{statvisible}" tooltip_key="xuiBuffStatCoreTemp">
    <sprite sprite="ui_game_symbol_temperature" color="{CATUI_coretempcolor}" />
    <label text="{CATUI_coretemp}" />
</rect>
```

## 四、竖线缓存 / 多 statbar 实例的坑（已修）

- `_tempBarMark` 是 static 缓存：换角色/窗口重建后旧 `uiTransform` 会被销毁
  （Unity 对象 `== null`），需在 `UpdatePostfix` 里用 `_tempBarMark.UiTransform == null` 判定并重新查找。
- 竖线移动与提前 `return` **只允许对真正拥有 tempBarMark 的实例**执行（用 `_tempBarMarkOwner` + `ReferenceEquals` 限定），
  否则其它 statbar（SkillPoints、CoreTemp、Mobility…）会被误导提前 return，跳过限频标脏 → 绑定不刷新。
- `TryUpdatePosition()` 全程 try/catch，异常时清缓存避免刷屏。

## 五、常见问题排查

- **数字与 buff 对不上**：先确认是不是“换算温度（°C）vs 华氏原始 `_coretemp`”两种量纲——多半是正常现象（见第一节）。
- **竖线位置和数字/buff 档位不一致**：检查 `UpdatePostfix` 里的映射公式与 `-center` 偏移。
- **竖线跑到最右端顶满**：`_coretemp` 上限 100，而滑轨宽 132，热端天然留白；如需顶满可改等比映射
  `x = coretemp * (132f/100f) - 66`。