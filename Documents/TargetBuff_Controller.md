# Target Buff 控制器 — 接口文档与使用示例

> 集中管理游戏中所有目标（Target）buff 的列表状态，供 XUi 绑定与其他模块查询展示。

## 1. 架构总览

```
┌──────────────────────────────────────────────────────┐
│             TargetBuffController（核心静态类）          │
│  - 定义列表  List<TargetBuffDef>   （注册顺序=展示顺序）│
│  - 状态表    Dictionary<BindingId, TargetBuffState>   │
│  - 绑定反查  bindingName → (def, role)                 │
└──────────────────────┬───────────────────────────────┘
                       │
      ┌────────────────┼───────────────────┐
      │                │                   │
┌─────▼─────┐   ┌──────▼──────┐   ┌────────▼────────┐
│ TargetBuffDef │  TargetBuffState │  XUiC_TargetBarPatch │
│ (数据模型)     │  (运行时状态)     │  (Harmony 委托)      │
└───────────┘   └─────────────┘   └───────────────────┘
```

## 2. 文件清单

| 文件 | 作用 |
|---|---|
| `Source/TargetBuffs/TargetBuffDef.cs` | buff 定义数据模型（不可变配置） |
| `Source/TargetBuffs/TargetBuffState.cs` | buff 运行时状态（激活/层数/倒计时） |
| `Source/TargetBuffs/TargetBuffController.cs` | 核心控制器（注册/目标/刷新/取值） |
| `Source/TargetBuffs/TargetBuffDefinitionRegistry.cs` | 默认 buff 注册表（集中维护） |
| `Source/XUiC_TargetBarPatch.cs` | 已重构：非 buff 绑定自处理，buff 绑定委托控制器 |

## 3. 数据模型

### 3.1 `TargetBuffDef`（定义）

| 成员 | 类型 | 说明 |
|---|---|---|
| `BindingId` | string | 主绑定名（布尔型返回 True/False；值型返回数值） |
| `IsValueBinding` | bool | 值型绑定标记（当前仅护甲） |
| `StackBindingId` | string | 层数绑定名（角标位置），可空 |
| `TimerBindingId` | string | 倒计时绑定名（图标下方），可空 |
| `DisplayName` | string | 展示名（本地化预留） |
| `Icon` | string | 图标 sprite 名 |
| `Color` | Color32 | 图标颜色 |
| `HasStack` / `StackCvar` / `StackMax` | — | 层数展示开关 / 来源 cvar / 上限（0=不限） |
| `StackReader` | Func<EntityAlive,float> | 自定义层数读取（优先） |
| `HasTimer` / `TimerCvar` / `UseBuffDuration` | — | 倒计时开关 / 来源 cvar / 回退 buff 时长 |
| `TimerReader` | Func<EntityAlive,float> | 自定义倒计时读取（优先） |
| `SourceBuffNames` | string[] | 关联原版 buff 名（任一存在即激活） |
| `ActiveCheck` | Func<EntityAlive,bool> | 自定义激活判定（优先） |
| `RequiredTags` | string[] | 可选标签过滤（命中任一才激活） |

### 3.2 `TargetBuffState`（运行时状态）

| 成员 | 类型 | 说明 |
|---|---|---|
| `IsActive` | bool | 当前是否激活 |
| `StackCount` | float | 当前层数 |
| `TimeRemaining` | float | 剩余秒数（>0 有效） |
| `HasChanged` | bool | 本轮相对上轮是否有变化 |

## 4. 控制器接口

### 4.1 初始化与注册

| 方法 | 说明 |
|---|---|
| `TargetBuffController.Initialize()` | 用默认注册表初始化（幂等） |
| `Initialize(IEnumerable<TargetBuffDef>)` | 用自定义列表初始化 |
| `Register(TargetBuffDef)` | 动态注册单个（后注册覆盖同名） |
| `Unregister(string bindingId)` | 取消注册 |

### 4.2 目标与刷新

| 方法 | 说明 |
|---|---|
| `SetTarget(EntityAlive target)` | 切换目标；null/死亡时全部置未激活 |
| `Refresh()` | 重算所有 buff 状态（建议每帧/限频调用） |

### 4.3 查询

| 方法 | 说明 |
|---|---|
| `HasBinding(string)` | 是否存在该绑定（主/层数/倒计时任一） |
| `IsActive(string)` | 主绑定是否激活 |
| `GetState(string)` | 获取运行时状态（未定义返回 null） |
| `GetDefinitions()` | 按注册顺序返回全部定义 |
| `GetActiveBuffs()` | 按注册顺序返回当前激活的定义列表 |
| `GetBindingValue(string)` | 取绑定值字符串（供 XUi 委托） |

### 4.4 绑定值返回规则（`GetBindingValue`）

| 绑定角色 | 激活时 | 未激活时 |
|---|---|---|
| 主绑定·布尔型 | `"True"` | `"False"` |
| 主绑定·值型（护甲） | 数值（向上取整） | `"0"` |
| 层数绑定 | 层数（向上取整） | `"0"` |
| 倒计时绑定 | 剩余秒数（向上取整，避免显示 0） | `"0"` |

## 5. 集成方式

### 5.1 初始化（已接入）

`XUi_Harmony.cs` 的 `loadAsync` postfix 在 XUi 启动时调用：
```csharp
TargetBuffController.Initialize();
```

### 5.2 目标切换与刷新（已接入）

`XUiC_TargetBarPatch.UpdatePostfix` 挂在 `XUiC_TargetBar.Update`，每次目标变化后同步：
```csharp
TargetBuffController.SetTarget(__instance.Target);
TargetBuffController.Refresh();
```

### 5.3 绑定委托（已接入）

`XUiC_TargetBarPatch.GetBindingValueInternalPrefix`：非 buff 绑定（`CATUI_fillCurrent`/`CATUI_EntityType`/`CATUI_EntityTags`）自行处理；其余走控制器。

## 6. 新增 buff 使用示例

### 6.1 在注册表新增（推荐）

在 `TargetBuffDefinitionRegistry.BuildDefaults()` 数组内追加一项：
```csharp
new TargetBuffDef
{
    BindingId = "CATUI_EntityIsExample",        // 主绑定（True/False）
    TimerBindingId = "CATUI_EntityExampleTimer",// 倒计时绑定（可空）
    Icon = "ui_game_symbol_example",
    Color = new Color32(255, 128, 0, 255),
    HasTimer = true,
    UseBuffDuration = true,                     // 用 buff 剩余时长做倒计时
    SourceBuffNames = new[] { "buffExample" },  // 关联原版 buff
},
```

### 6.2 运行时动态注册

```csharp
TargetBuffController.Register(new TargetBuffDef
{
    BindingId = "CATUI_EntityIsMyBuff",
    Icon = "catui_icon_shield",
    Color = new Color32(0, 255, 0, 255),
    ActiveCheck = (target) => target?.Buffs.GetBuff("buffMyBuff") != null,
});
```

### 6.3 查询展示

```csharp
// 遍历当前激活的 buff
foreach (var def in TargetBuffController.GetActiveBuffs())
{
    var state = TargetBuffController.GetState(def.BindingId);
    // 渲染 def.Icon / def.Color / state.StackCount / state.TimeRemaining
}

// 直接取绑定值
string v = TargetBuffController.GetBindingValue("CATUI_EntityIsBleeding"); // "True"/"False"
```

### 6.4 XML 中使用

```xml
<CATUI_entity_buff_item width="26" height="26" buff_color="255,0,0"
    buff_icon="ui_game_symbol_deep_cuts"
    buff_corner_text="{CATUI_EntityBleedingCounter}"
    buff_timer="{CATUI_EntityBleedingTimer}"
    visible="{CATUI_EntityIsBleeding}" />
```

## 7. 错误处理与边界条件

| 场景 | 处理 |
|---|---|
| `Target == null` / 已死亡 | 全部 buff 置未激活，返回安全默认值 |
| `GetBuff` 返回 null | 视为不激活，不抛异常 |
| cvar 缺失 | `GetCustomVar` 返回 0（原版行为），层数/计时为空 |
| 重复注册同名 BindingId | 后者覆盖前者 |
| 层数负值/超上限 | clamp 到 `[0, StackMax]` |
| 倒计时负数 | clamp 到 0，不显示 |
| 未识别的绑定名 | `GetBindingValue` 返回 null，交由原版处理 |
