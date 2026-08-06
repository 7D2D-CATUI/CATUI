# Target Buff 默认配置列表

> 对应 `Source/TargetBuffs/TargetBuffDefinitionRegistry.cs` 的 `BuildDefaults()`。
> 注册顺序即目标血条上的展示顺序。

## 1. 排除规则

| 排除项 | 说明 |
|---|---|
| 增益/特殊状态类 · `buffSetHeldItemJammed` | 武器卡壳（内部机制，不展示） |
| 环境/天气类 全部 buff | 风暴/危害/温湿度等不属目标 buff 展示范围 |

## 2. 默认列表（18 项）

### 前置：硬编码新增（排在最前面）

| # | 主绑定 | 中文名称 | 功能说明 | 施加来源/触发条件 | 层数绑定 | 倒计时绑定 | 图标 | 颜色 |
|---|---|---|---|---|---|---|---|---|
| 1 | `CATUI_EntityArmorRating`（值型） | 护甲 | 目标当前物理护甲值（层数位置展示数值）；激活条件 = 物理抗性 > 0 | 目标自带护甲属性（非 buff） | `CATUI_EntityArmorRating` | — | `catui_icon_shield` | 绿 |
| 2 | `CATUI_EntityIsSleeping` | 睡眠 | 目标正在睡觉（静止状态） | 目标进入睡眠状态（夜间/白天站立休眠） | — | — | `catui_icon_sleep` | 红 |

### 伤害/流血类

| # | 主绑定 | 中文名称 | 功能说明 | 施加来源/触发条件 | 层数绑定 | 倒计时绑定 | 图标 | 颜色 |
|---|---|---|---|---|---|---|---|---|
| 3 | `CATUI_EntityIsBleeding` | 出血 | 持续损失生命（按层数），同时减速；角标显示流血层数 | 利器/钝器暴击、陷阱（铁丝网）、猎刀/小刀类武器 | `CATUI_EntityBleedingCounter`（`bleedCounter` 上限 7） | `CATUI_EntityBleedingTimer`（`$bleedDuration`） | `ui_game_symbol_deep_cuts` | 红 |
| 4 | `CATUI_EntityIsInternalBleeding` | 内出血 | 60s 内按曲线持续大量掉血（1→5→200），无法外部止血 | 特定高伤害暴击、爆炸冲击 | — | buff 时长 | `ui_game_symbol_critical` | 红 |
| 5 | `CATUI_EntityIsConcussed` | 脑震荡 | 长时间持续掉血，再次被攻击会延长；止痛药可移除 | 钝器暴击、爆炸 | — | `.concussionDurationDisplay` | `ui_game_symbol_concussion` | 橙 |

### 控制/限制类

| # | 主绑定 | 中文名称 | 功能说明 | 施加来源/触发条件 | 层数绑定 | 倒计时绑定 | 图标 | 颜色 |
|---|---|---|---|---|---|---|---|---|
| 6 | `CATUI_EntityIsStunned` | 眩晕 | 减速 80% 并随时间恢复、攻击速度下降、屏幕模糊+镜头震动（4 级） | 散弹枪、钝器/拳击暴击、击晕棒 | — | `.stunDisplay` | `ui_game_symbol_stunned` | 橙 |
| 7 | `CATUI_EntityIsKnockedDown` | 击倒/倒地 | 移动完全瘫痪并触发 Ragdoll 倒地（4/6s，有冷却） | `perkSkullCrusher`、`perkBrawler`、棒球棍击倒、金属链狼牙棒 mod | — | `.knockdownDisplay` | `ui_game_symbol_stunned` | 橙 |
| 8 | `CATUI_EntityIsUnconscious` | 昏迷（KO） | 10s 完全瘫痪 + Ragdoll，屏幕全模糊；最强控制 | 拳击精通（`perkPummelPete`）、高暴击拳击 | — | buff 时长 | `ui_game_symbol_critical` | 橙 |
| 9 | `CATUI_EntityIsCrippled` | 致残/跛行 | 仅对 walker/bandit 生效：移动/奔跑减 30%，切换跛行动画 | `perkRangersCripplingShot`、`modGunCrippleEm` 武器 mod | — | buff 时长 | `ui_game_symbol_stunned` | 橙 |
| 10 | `CATUI_EntityIsSlowed` | 减速 | 6s 内移动（跑/走/蹲/跳）速度缓慢递减恢复 | 钝器暴击、弹药减速效果 | — | buff 时长 | `ui_game_symbol_twitch_slow` | 橙 |
| 11 | `CATUI_EntityLegInjured` | 腿部受伤 | 大幅降低移动与跳跃能力（扭伤/骨折） | 高处摔落、被重击 | — | buff 时长 | `ui_game_symbol_brokenbone` | 橙 |
| 12 | `CATUI_EntityArmInjured` | 手臂受伤 | 降低近战攻击力与使用武器能力（扭伤/骨折） | 被重击、摔落 | — | buff 时长 | `ui_game_symbol_broken_arm` | 橙 |

### 元素类

| # | 主绑定 | 中文名称 | 功能说明 | 施加来源/触发条件 | 层数绑定 | 倒计时绑定 | 图标 | 颜色 |
|---|---|---|---|---|---|---|---|---|
| 13 | `CATUI_EntityIsOnFire` | 着火 | 持续掉血 + 火焰粒子；多火源取最长倒计时 | 燃烧箭/燃烧弩箭、燃烧瓶、武器燃烧模组、火焰陷阱/元素燃烧 | — | `CATUI_EntityOnFireTimer`（多火源 cvar 取最大） | `ui_game_symbol_fire` | 红 |
| 14 | `CATUI_EntityIsShocked` | 电击 | 持续掉血 + 减速；带电僵尸时长减半 | 电击棒、电击陷阱、无人机电击 mod | — | `CATUI_EntityShockedTimer`（buff 时长，带电减半） | `ui_game_symbol_electric_power` | 红 |

### 增益/特殊状态类

| # | 主绑定 | 中文名称 | 功能说明 | 施加来源/触发条件 | 层数绑定 | 倒计时绑定 | 图标 | 颜色 |
|---|---|---|---|---|---|---|---|---|
| 15 | `CATUI_EntityIsRadiatedRegen` | 辐射回复 | 辐射类敌人持续回血，生命 ≥80% 时停止 | 辐射标签实体自带（出生附加） | — | — | `ui_game_symbol_radiation` | 红 |
| 16 | `CATUI_EntityIsRadiatedRegenBlock` | 辐射回复阻断 | 分级（15-90s）禁止辐射回复，压制辐射僵尸回血 | 辐射移除器 mod（`modGunMeleeRadRemover`） | — | `CATUI_EntityRadiatedRegenBlockTimer`（buff 时长） | `ui_game_symbol_radiation` | 黄 |
| 17 | `CATUI_EntityIsLegendary` | 传奇 Boss | 生命上限×3、伤害×3，持续存在 | 传奇 BOSS 级实体（血月/任务 Boss） | — | — | `ui_game_symbol_skull` | 紫 |
| 18 | `CATUI_EntityIsBerserk` | 狂暴 | 提升攻击/移动（狂暴化实体） | 狂暴标签实体、特定僵尸变种 | — | — | `ui_game_symbol_berserker` | 黄 |
| 19 | `CATUI_EntityIsArmorShredded` | 护甲撕裂 | 20s 内按命中叠加降低物理抗性（可叠加） | 金属尖刺陷阱/尖锐武器连续命中 | `.ArmorShreddingCounter` | buff 时长 | `ui_game_symbol_armor_iron` | 橙 |
| 20 | `CATUI_EntityIsCrippledMorale` | 士气低落 | 10s 内目标伤害 -20%、移动大幅降低 | 大锤三段技能（`buffSledgeSaga3CrippledMorale`） | — | buff 时长 | `ui_game_symbol_sledge` | 橙 |

> 注：表内共 20 个展示条目 —— 前置硬编码新增 2 项（护甲、睡眠）+ 核心 buff 18 项（伤害 3 + 控制 7 + 元素 2 + 增益特殊 6）。

## 3. 关联的原版 buff 映射

| CATUI 绑定 | 原版 buff（SourceBuffNames） |
|---|---|
| `CATUI_EntityIsBleeding` | `buffInjuryBleeding` |
| `CATUI_EntityIsInternalBleeding` | `buffInternalBleeding` |
| `CATUI_EntityIsConcussed` | `buffInjuryConcussion` |
| `CATUI_EntityIsStunned` | `buffInjuryStunned00/01/02/03` |
| `CATUI_EntityIsKnockedDown` | `buffInjuryKnockdown01/02` |
| `CATUI_EntityIsUnconscious` | `buffInjuryUnconscious` |
| `CATUI_EntityIsCrippled` | `buffInjuryCrippled01` |
| `CATUI_EntityIsSlowed` | `buffInjurySlow` |
| `CATUI_EntityLegInjured` | `buffLegSprained` / `buffLegBroken` |
| `CATUI_EntityArmInjured` | `buffArmSprained` / `buffArmBroken` |
| `CATUI_EntityIsOnFire` | `buffIsOnFire` |
| `CATUI_EntityIsShocked` | `buffShocked` |
| `CATUI_EntityIsRadiatedRegen` | `buffRadiatedRegen` |
| `CATUI_EntityIsRadiatedRegenBlock` | `buffRadiatedRegenBlock15/30/45/60/75/90` |
| `CATUI_EntityIsLegendary` | `buffLegendaryBoss` |
| `CATUI_EntityIsBerserk` | `buffBerserker` |
| `CATUI_EntityIsArmorShredded` | `buffArmorShredding` |
| `CATUI_EntityIsCrippledMorale` | `buffCrippledMorale` |

## 4. 颜色惯例

| 颜色 | 含义 |
|---|---|
| `255,0,0` 红 | 伤害/致命（流血、内出血、着火、电击、辐射） |
| `255,128,0` 橙 | 控制类（眩晕、击倒、致残、减速、骨折、护甲撕裂、士气低落、脑震荡） |
| `255,255,0` 黄 | 增益/状态阻断（辐射回复阻断、狂暴） |
| `0,255,0` 绿 | 防御/增益（护甲） |
| `132,0,155` 紫 | 特殊（传奇 Boss） |
