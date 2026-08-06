# CATUI 性能分析与优化方案

## 一、性能基线（现状总览）

**项目规模**：26 个 Harmony 补丁文件（~7,000 行 C#）+ 3 个 XUi XML 配置（~1,900 行）+ 200+ 张 PNG 图集资源。

**整体评估**：代码整体质量良好——**无 LINQ、无反射（Traverse/GetValue）、无 NCalc 热路径问题**（所有自定义绑定均通过 prefix 短路原版求值器）。已有关键优化措施（HUDStatBar 10fps 限频、ItemStack 引用缓存、AnimatedSprite 惰性挂载、Compass 预取局部变量）。瓶颈集中在**少数每帧/每刷新的热路径**与**缓存生命周期管理**。

## 二、问题分类与瓶颈清单

### A 类：每帧执行的热路径（最高优先级）

| # | 位置 | 问题 | 影响 |
|---|---|---|---|
| A1 | `XUiC_TargetBarPatch.cs:79-86` | `EffectManager.GetValue(PhysicalDamageResist)` **每帧未缓存**，对目标做完整被动效果结算 | 战斗时每帧一次全量结算，最重的单点 |
| A2 | `XUiC_TargetBarPatch.cs:32-36` | 每次绑定求值做 **5 次 `FastTags.GetBit("boss/feral/radiated/charged/infernal")` 字符串哈希查表**，未提升为静态字段、无短路 | 每帧 5-6 次字符串哈希；普通丧尸也全部执行 |
| A3 | `XUiC_TargetBarPatch.cs:103-234` | `Target.Buffs.GetBuff("buffXxx")` 字符串查表每帧重复（bleeding/shocked/onFire 各查 2 次） | 每帧多个字符串字典查找 |
| A4 | `XUiC_TargetBarPatch.cs:244-281` | `CATUI_EntityBuffList/Timer` 用 `text +=` 循环拼接（O(n²) 字符拷贝） | 若绑定启用，每帧大量分配 |
| A5 | `XUiC_HUDStatBarPatch.cs:80,100` | `ConditionalWeakTable.GetOrCreateValue` **每帧、每个 stat bar 实例**加锁+哈希查找（10-20 实例） | 每帧 10-20 次 CWT 锁操作 |
| A6 | `XUiC_HUDStatBarPatch.cs:207+239等26处` | `MarkDirtyThrottled` 每绑定执行 2 次（行 207 统一限频 + 每个 case 内重复） | 每刷新约 26 次冗余 CWT 查找 |

### B 类：每刷新执行的高乘数成本（中优先级）

| # | 位置 | 问题 | 乘数 |
|---|---|---|---|
| B1 | `XUiC_SkillEntryPatch.cs` | `CalculatedLevel` 每个条目每次刷新**重复计算最多 4 次**；书本组子节点扫描最多 3 次 | ×30 技能条目（1,020 元素/450 NCalc） |
| B2 | `XUiC_SkillEntryPatch.cs:453` | `GetSkillGroupColor` 每次调用 `string.Format` 分配新字符串 + 全量扫描 | ×30，每刷新 2 次 |
| B3 | `XUiC_SkillCraftingInfoWindowPatch.cs:131` | `SkillChangedPrefix` 仅为读 `.Count` 而**构建整个 DisplayDataList**（每个含新 List） | 技能变更时 2 次全量重建 |
| B4 | `XUiC_SkillListPatch.cs:167` | `GetActiveCount` 每调用全量线性扫描 `currentSkills` | 若被每帧查询则 O(n)/帧 |
| B5 | `XUiC_ItemStackPatch.cs:181,185` | `bool.ToString().ToLower()` 每次求值分配 2 个字符串 | ×135 槽位 |
| B6 | `XUiC_BuffPopoutListPatch.cs:101-112` | 每 0.1s 分配 HashSet + 2 个 List 做清理 | 10fps 稳态分配 |

### C 类：缓存生命周期 / 资源加载问题

| # | 位置 | 问题 | 风险 |
|---|---|---|---|
| C1 | `PartyAvatar.cs:9,167` | `cache` **永不清理**，Texture2D 按 SteamID 无限增长 | 长会话内存泄漏（跨世界保留） |
| C2 | `PartyAvatar.cs:11,112` | `pending` 仅在回调送达时移除，**无超时**；未送达则闭包持有 XUiC_PartyEntry/GameObject | UI 对象泄漏 |
| C3 | `XUiV_VideoChromaKeyPatch.cs:18-23` | 以 `XUiV_Video` 为 key 的字典/集合仅靠 `Cleanup` 清理，`OnClose` 有意不清理；`_frameCount` 无界增长 | 长溢出 + 潜在悬挂引用 |
| C4 | `XUiV_AnimatedSprite.cs:23` | `spriteFrameCache` **死代码**（声明但从未引用，实际用的是行 261 `spriteNameCache`） | 清理 |
| C5 | `XUiV_AnimatedSprite.cs:261` | `spriteNameCache` 图集重载后**不失效** | 低风险（图集固定） |
| C6 | `XUiV_RoundedTexture.cs` | 重新生成纹理时**不销毁旧 Texture2D** | GPU 纹理孤儿 |
| C7 | `FontManager.cs:262` | 每个新字体名都重新枚举 `GetOSInstalledFontNames()` | 仅 OS 字体路径，可缓存列表 |
| C8 | `XUiV_VideoChromaKeyPatch.cs:35` | shader 首次使用时**主线程同步磁盘读取** | 游戏中途一次性卡顿 |

### D 类：XML 模板复杂度（加载/刷新基线的放大器）

| 模板 | CATUI 节点 | 原版节点 | CATUI NCalc | 原版 NCalc | 实例数 | 汇总 |
|---|---|---|---|---|---|---|
| **item_stack** | 28 | 21 | 17 | 8 | 20-135 | **HUD 340 + 背包/箱子 2,295 NCalc** |
| **skill_entry** | 34 | 10 | 15 | 0 | 30 | 1,020 元素/450 NCalc |
| skill_perk_level | 26 | 7 | 13 | 0 | 10-15 | 260-390 元素 |
| **party_entry** | 29 | 15 | 6 | 1 | 7 | 203 元素（常驻 HUD） |
| windowCompass | 55 | 8 | 25 | 1 | 1 | **常驻 25 NCalc** |

**关键结论**：`item_stack` 较原版 +33% 节点 / +112% NCalc，且被背包+箱子网格放大至 **2,295 个 NCalc 表达式**——这是此前"打开背包卡顿数秒"的根因（已部分缓解，见 E 类），但仍是最重的聚合成本。

### E 类：已完成的优化（验证有效，无需重复）

- `XUiC_ItemStackPatch` 绑定缓存（按 ItemStack 引用）——已验证修复卡顿
- `XUiV_AnimatedSprite` 惰性 AddComponent + RebuildSpriteList 静态缓存
- `XUiViewPatch` ConditionalWeakTable（替代线性扫描）
- 崩溃修复（动态字体材质链 + UIDrawCall 渲染防御补丁）

## 三、优化建议与实施计划

按「收益/风险/工作量」排序。所有改动均为纯增量补丁，可独立回滚。

### 阶段 1：高收益低风险（每帧热路径缓存化）

**1.1 TargetBar 每帧缓存**（`XUiC_TargetBarPatch.cs`）
- 方案：仿照 HUDStatBar 的 `EnsureCacheFrame()`，引入 `(int frame, EntityAlive target)` 缓存键，同一帧内：
  - 缓存 `EffectManager.GetValue(PhysicalDamageResist)` 结果
  - 缓存 `EntityClass.list[Target.entityClass].Tags` 及 5 个 `FastTags.GetBit` 位掩码（提升为 `static readonly FastTags` 一次性获取）
  - 缓存 `GetBuff("buffXxx")` 结果（每帧按 buff 名缓存，重复绑定共用）
- 预期：战斗期间 TargetBar 每帧成本降 **60-80%**（消除 EffectManager 全量结算 + 字符串哈希查表 + ToString 分配）

**1.2 HUDStatBar 消除冗余 CWT 调用**（`XUiC_HUDStatBarPatch.cs`）
- 方案：删除各 case 内 26 处重复 `MarkDirtyThrottled`（行 207 已统一覆盖）；`UpdatePostfix` 保持
- 预期：每刷新 CWT 调用减半（约 -26 次锁查找）；改动极小、零风险

**1.3 TargetBar BuffList 改用 StringBuilder + 按需重建**
- 方案：`CATUI_EntityBuffList/Timer` 用 `StringBuilder` 单次构建；仅在 buff 集合变化时重建（缓存上次拼接结果 + buff 集合引用比较）
- 预期：若绑定启用，消除每帧 O(n²) 字符串拷贝

### 阶段 2：中收益中风险（高乘数模板）

**2.1 SkillEntry 每刷新缓存**（`XUiC_SkillEntryPatch.cs`）
- 方案：引入 `RefreshBindings` 级缓存（key = 实例 + 刷新代际号）：
  - `CalculatedLevel` 每条目每刷新最多计算 1 次（当前 4 次）
  - `GetBookGroupLevel` / `CountChildren` 子节点扫描合并为 1 次
  - `GetSkillGroupColor` 的 `string.Format` 改为预拼接/缓存
- 预期：技能窗口（×30 条目）每刷新成本降 **50-70%**，技能界面滚动/悬停更流畅

**2.2 SkillCraftingInfoWindow 避免双重重建**（行 131）
- 方案：`SkillChangedPrefix` 仅统计 `elementCount`（计数循环），不构建完整 `DisplayDataList`；`UpdateSkill` 再真正重建
- 预期：技能变更时分配量减半

**2.3 ItemStack 预缓存布尔字符串**（`XUiC_ItemStackPatch.cs`）
- 方案：`BindingCache` 内直接存 `"true"/"false"` 常量，消除每次求值的 `ToString().ToLower()` 分配
- 预期：×135 槽位每次刷新省 270 次小分配

### 阶段 3：内存生命周期（防泄漏）

**3.1 PartyAvatar 缓存治理**
- `cache`：改为按**会话/世界切换** `Clear()`（挂 `World.UnloadEvent` 或 ModEvents），或 LRU 上限（如 64 项）
- `pending`：加超时（如 30s 后移除并丢弃闭包），防止 Steam 回调未送达泄漏 UI 引用

**3.2 ChromaKey 集合清理**：`CleanupChromaKey` 已在 `Cleanup` postfix 调用；补充在 `OnClose` 也清理纯状态（`_debugLogged`/`_frameCount` 归零），或确认视频视图生命周期保证

**3.3 死代码清理**：删除 `XUiV_AnimatedSprite.cs:23` 的 `spriteFrameCache`；`XUiV_RoundedTexture` 重建前 `Destroy(oldTexture)`

**3.4 FontManager**：缓存 `GetOSInstalledFontNames()` 列表（一次性枚举）

### 阶段 4：XML 模板精简（可选，收益大但需验证视觉）

- **方案**：对高乘数模板（item_stack/skill_entry/party_entry）合并冗余 NCalc：
  - `item_stack` 中 `rectSlotLock` 的 3 元条件表达式去重（行 208-209 计算两次）
  - 合并多个 `visible` 门控为单个控制器绑定
- **预期**：item_stack NCalc 17→12、skill_entry 15→10；背包打开时 NCalc 求值总量降约 30%
- **风险**：需逐项截图对比视觉，建议作为独立迭代，不与其他阶段混用

## 四、预期性能提升汇总

| 场景 | 当前 | 优化后 | 手段 |
|---|---|---|---|
| 战斗时 TargetBar 每帧成本 | 高（EffectManager+字符串查表） | **-60~80%** | 阶段 1.1/1.3 |
| HUDStatBar 每刷新 CWT 调用 | ~26 次/实例 | **~1 次/实例** | 阶段 1.2 |
| 技能窗口打开/滚动/悬停 | 4×CalculatedLevel + 3×扫描 | **1×计算 + 1×扫描** | 阶段 2.1 |
| 背包/箱子打开 NCalc 总量 | 2,295 表达式 | **-30%**（阶段 4） | XML 精简 |
| 长会话内存 | 头像/待定闭包/纹理无界增长 | **有界** | 阶段 3.1/3.2 |

## 五、验证方案

1. **每个阶段完成后**：`dotnet build`（0 错误）+ 部署 + 游戏内跑 30 分钟验证（对照日志 `[CATUI]` 无新报错、无 NRE 刷屏）
2. **性能对比**：优化前后录制同场景（打开 90 格大箱子、战斗 3 分钟、技能窗口滚动）的 `Frame Debugger`/FPS 数据
3. **回归检查**：传奇动画、组队头像、天气罗盘、风暴倒计时逐项确认视觉无回归

## 六、待决策项

1. **阶段 4 的 XML 模板精简**是否纳入本次执行（涉及视觉验证成本）
2. **PartyAvatar 缓存清理**倾向「会话结束 Clear」还是「LRU 上限」
