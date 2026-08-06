using UnityEngine;

namespace TargetBuffs
{
    /// <summary>
    /// 默认 Target buff 注册表。
    /// 集中管理所有目标 buff 定义，新增/变更 buff 只需在此处维护。
    ///
    /// 排除规则：
    /// - 排除「增益/特殊状态类」中的 buffSetHeldItemJammed
    /// - 排除「环境/天气类」全部 buff
    /// </summary>
    public static class TargetBuffDefinitionRegistry
    {
        /// <summary>
        /// 构建默认 buff 列表（注册顺序即列表展示顺序）。
        /// 前置两项为硬编码新增：护甲值、睡眠。
        /// </summary>
        public static TargetBuffDef[] BuildDefaults()
        {
            return new TargetBuffDef[]
            {
                // ============ 前置：硬编码新增 ============

                // 护甲值：值型绑定，激活条件 = 物理抗性 > 0；数值本身放入层数（角标）展示
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityArmorRating",
                    IsValueBinding = true,
                    DisplayName = "护甲",
                    Icon = "catui_icon_shield",
                    Color = new Color32(0, 255, 0, 255),
                    HasStack = true,
                    StackBindingId = "CATUI_EntityArmorRating",
                    StackReader = (target) => target != null ? EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, target) : 0f,
                    ActiveCheck = (target) => target != null && EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, target) > 0f,
                    SourceBuffNames = null,
                },

                // 睡眠：激活条件 = 目标正在睡觉
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsSleeping",
                    DisplayName = "睡眠",
                    Icon = "catui_icon_sleep",
                    Color = new Color32(255, 0, 0, 255),
                    HasStack = false,
                    HasTimer = false,
                    ActiveCheck = (target) => target != null && target.IsSleeping,
                    SourceBuffNames = null,
                },

                // ============ 伤害/流血类 ============

                // 流血：层数 = bleedCounter，倒计时 = $bleedDuration
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsBleeding",
                    DisplayName = "出血",
                    Icon = "ui_game_symbol_deep_cuts",
                    Color = new Color32(255, 0, 0, 255),
                    HasStack = true,
                    StackBindingId = "CATUI_EntityBleedingCounter",
                    StackCvar = "bleedCounter",
                    StackMax = 7f,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityBleedingTimer",
                    TimerCvar = "$bleedDuration",
                    SourceBuffNames = new[] { "buffInjuryBleeding" },
                },

                // 内出血：60s 大量掉血，倒计时用 buff 时长
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsInternalBleeding",
                    DisplayName = "内出血",
                    Icon = "ui_game_symbol_critical",
                    Color = new Color32(255, 0, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityInternalBleedingTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffInternalBleeding" },
                },

                // 脑震荡：长时间掉血，倒计时用 cvar
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsConcussed",
                    DisplayName = "脑震荡",
                    Icon = "ui_game_symbol_concussion",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityConcussedTimer",
                    TimerCvar = ".concussionDurationDisplay",
                    SourceBuffNames = new[] { "buffInjuryConcussion" },
                },

                // ============ 控制/限制类 ============

                // 眩晕：激活条件 = 眩晕 buff 存在 或 引擎踉跄硬直（Stumble）。
                // 注意：普通近战击退/踉跄走 EntityAlive.bodyDamage.CurrentStun（Stumble*），
                //       不产生 buffInjuryStunned00-03；buff 仅由散弹枪/拳击/狙击/配重锤头施加。
                // Prone/Kneel（跪地/倒地）归 CATUI_EntityIsKnockedDown 处理，避免两处重复显示。
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsStunned",
                    DisplayName = "眩晕",
                    Icon = "ui_game_symbol_stunned",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityStunnedTimer",
                    TimerCvar = ".stunDisplay",
                    ActiveCheck = (target) =>
                    {
                        if (target == null) return false;
                        // 眩晕 buff
                        if (target.Buffs.GetBuff("buffInjuryStunned00") != null ||
                            target.Buffs.GetBuff("buffInjuryStunned01") != null ||
                            target.Buffs.GetBuff("buffInjuryStunned02") != null ||
                            target.Buffs.GetBuff("buffInjuryStunned03") != null)
                        {
                            return true;
                        }
                        // 引擎踉跄硬直（不含 Prone/Kneel，那些归倒地）
                        EnumEntityStunType stun = target.bodyDamage.CurrentStun;
                        return stun == EnumEntityStunType.Stumble ||
                               stun == EnumEntityStunType.StumbleBreakThrough ||
                               stun == EnumEntityStunType.StumbleBreakThroughRagdoll;
                    },
                    TimerReader = (target) =>
                    {
                        if (target == null) return 0f;
                        // 仅 buff 有精确倒计时（散弹枪/拳击等明确来源）；
                        // 引擎踉跄硬直无精确剩余时间（StunDuration 固定为 1），不显示计时
                        return target.Buffs.GetCustomVar(".stunDisplay");
                    },
                    SourceBuffNames = new[] { "buffInjuryStunned00", "buffInjuryStunned01", "buffInjuryStunned02", "buffInjuryStunned03" },
                },

                // 击倒/倒地（两级）：激活条件 = 引擎击倒硬直（Prone/Kneel）或击倒 buff。
                // 注意：普通近战/爆炸的倒地走 EntityAlive.bodyDamage.CurrentStun（Prone/Kneel），
                //       并不产生 buffInjuryKnockdown01/02；buff 仅由大锤/棒球棍/金属链等专门来源施加。
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsKnockedDown",
                    DisplayName = "击倒",
                    Icon = "ui_game_symbol_stunned",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityKnockedDownTimer",
                    TimerCvar = ".knockdownDisplay",
                    ActiveCheck = (target) =>
                    {
                        if (target == null) return false;
                        // 引擎击倒硬直（跪地/趴下）
                        EnumEntityStunType stun = target.bodyDamage.CurrentStun;
                        if (stun == EnumEntityStunType.Kneel || stun == EnumEntityStunType.Prone)
                        {
                            return true;
                        }
                        // 击倒 buff
                        return target.Buffs.GetBuff("buffInjuryKnockdown01") != null ||
                               target.Buffs.GetBuff("buffInjuryKnockdown02") != null;
                    },
                    TimerReader = (target) =>
                    {
                        if (target == null) return 0f;
                        // 优先 buff 倒计时（大锤/棒球棍等明确来源）
                        float buffTimer = target.Buffs.GetCustomVar(".knockdownDisplay");
                        if (buffTimer > 0f)
                        {
                            return buffTimer;
                        }
                        // 引擎倒地：优先用 ragdoll 剩余时间（ragdollDuration - ragdollTime）
                        if (target.emodel != null && target.emodel.IsRagdollActive)
                        {
                            float remaining = target.emodel.ragdollDuration - target.emodel.ragdollTime;
                            if (remaining > 0f)
                            {
                                return remaining;
                            }
                        }
                        // 非 ragdoll 的跪地/趴地硬直：用 StunDuration（ragdoll 时该值固定为 1，仅作兜底）
                        EnumEntityStunType stun = target.bodyDamage.CurrentStun;
                        if (stun == EnumEntityStunType.Kneel || stun == EnumEntityStunType.Prone)
                        {
                            return target.bodyDamage.StunDuration;
                        }
                        return 0f;
                    },
                    SourceBuffNames = new[] { "buffInjuryKnockdown01", "buffInjuryKnockdown02" },
                },

                // 昏迷（KO）
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsUnconscious",
                    DisplayName = "昏迷",
                    Icon = "ui_game_symbol_critical",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityUnconsciousTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffInjuryUnconscious" },
                },

                // 致残/跛行（walker/bandit 专用）
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsCrippled",
                    DisplayName = "致残",
                    Icon = "ui_game_symbol_stunned",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityCrippledTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffInjuryCrippled01" },
                },

                // 减速
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsSlowed",
                    DisplayName = "减速",
                    Icon = "ui_game_symbol_twitch_slow",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntitySlowedTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffInjurySlow" },
                },

                // 腿部骨折/扭伤
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityLegInjured",
                    DisplayName = "腿部受伤",
                    Icon = "ui_game_symbol_brokenbone",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityLegInjuredTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffLegSprained", "buffLegBroken" },
                },

                // 手臂骨折/扭伤
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityArmInjured",
                    DisplayName = "手臂受伤",
                    Icon = "ui_game_symbol_broken_arm",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityArmInjuredTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffArmSprained", "buffArmBroken" },
                },

                // ============ 元素类（着火/电击） ============

                // 着火：多火源取最大倒计时
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsOnFire",
                    DisplayName = "着火",
                    Icon = "ui_game_symbol_fire",
                    Color = new Color32(255, 0, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityOnFireTimer",
                    TimerReader = (target) =>
                    {
                        if (target == null) return 0f;
                        float arrow = target.Buffs.GetCustomVar("$buffBurningFlamingArrowDuration");
                        float molotov = target.Buffs.GetCustomVar("%buffBurningMolotovDuration");
                        float element = target.Buffs.GetCustomVar("$buffBurningElementDuration");
                        float hazard = target.Buffs.GetCustomVar("$buffHazardBurningElementDuration");
                        return Mathf.Max(arrow, Mathf.Max(molotov, Mathf.Max(element, hazard)));
                    },
                    SourceBuffNames = new[] { "buffIsOnFire" },
                },

                // 电击：带电僵尸时长减半
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsShocked",
                    DisplayName = "电击",
                    Icon = "ui_game_symbol_electric_power",
                    Color = new Color32(255, 0, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityShockedTimer",
                    TimerReader = (target) =>
                    {
                        if (target == null) return 0f;
                        BuffValue buff = target.Buffs.GetBuff("buffShocked");
                        if (buff == null) return 0f;
                        bool isCharged = target.entityClass != 0 && EntityClass.list.TryGetValue(target.entityClass, out var entityClass)
                            && entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("charged"));
                        float total = isCharged ? buff.BuffClass.DurationMax / 2f : buff.BuffClass.DurationMax;
                        return total - buff.DurationInSeconds;
                    },
                    SourceBuffNames = new[] { "buffShocked" },
                },

                // ============ 增益/特殊状态类 ============

                // 辐射回复
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsRadiatedRegen",
                    DisplayName = "辐射回复",
                    Icon = "ui_game_symbol_radiation",
                    Color = new Color32(255, 0, 0, 255),
                    HasStack = false,
                    HasTimer = false,
                    SourceBuffNames = new[] { "buffRadiatedRegen" },
                },

                // 辐射回复阻断（15/30/45/60/75/90 分级）
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsRadiatedRegenBlock",
                    DisplayName = "辐射回复阻断",
                    Icon = "ui_game_symbol_radiation",
                    Color = new Color32(255, 255, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityRadiatedRegenBlockTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[]
                    {
                        "buffRadiatedRegenBlock15", "buffRadiatedRegenBlock30", "buffRadiatedRegenBlock45",
                        "buffRadiatedRegenBlock60", "buffRadiatedRegenBlock75", "buffRadiatedRegenBlock90",
                    },
                },

                // 传奇 Boss（血量/伤害×3）
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsLegendary",
                    DisplayName = "传奇",
                    Icon = "ui_game_symbol_skull",
                    Color = new Color32(132, 0, 155, 255),
                    HasStack = false,
                    HasTimer = false,
                    SourceBuffNames = new[] { "buffLegendaryBoss" },
                },

                // 狂暴
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsBerserk",
                    DisplayName = "狂暴",
                    Icon = "ui_game_symbol_berserker",
                    Color = new Color32(255, 255, 0, 255),
                    HasStack = false,
                    HasTimer = false,
                    SourceBuffNames = new[] { "buffBerserker" },
                },

                // 护甲撕裂（可叠加）
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsArmorShredded",
                    DisplayName = "护甲撕裂",
                    Icon = "ui_game_symbol_armor_iron",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = true,
                    StackBindingId = "CATUI_EntityArmorShreddedCounter",
                    StackCvar = ".ArmorShreddingCounter",
                    StackMax = 0f,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityArmorShreddedTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffArmorShredding" },
                },

                // 士气低落（大锤三段）
                new TargetBuffDef
                {
                    BindingId = "CATUI_EntityIsCrippledMorale",
                    DisplayName = "士气低落",
                    Icon = "ui_game_symbol_sledge",
                    Color = new Color32(255, 128, 0, 255),
                    HasStack = false,
                    HasTimer = true,
                    TimerBindingId = "CATUI_EntityCrippledMoraleTimer",
                    UseBuffDuration = true,
                    SourceBuffNames = new[] { "buffCrippledMorale" },
                },
            };
        }
    }
}
