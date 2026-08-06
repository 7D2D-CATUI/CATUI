using System;
using UnityEngine;

namespace TargetBuffs
{
    /// <summary>
    /// Target buff 定义（不可变配置）。
    /// 描述一个目标 buff 的展示属性、来源 buff、层数与倒计时解析规则，
    /// 以及其在 XUi 中对应的绑定名（主绑定/层数绑定/倒计时绑定）。
    /// </summary>
    public class TargetBuffDef
    {
        /// <summary>主绑定名（XUi 绑定 key）。
        /// 布尔型 buff 返回 "True"/"False"；值型 buff（如护甲）返回数值。</summary>
        public string BindingId;

        /// <summary>是否为值型绑定：主绑定直接返回数值而非布尔（当前仅护甲）。</summary>
        public bool IsValueBinding;

        /// <summary>层数绑定名（角标位置），如 "CATUI_EntityBleedingCounter"；无层数可留空。</summary>
        public string StackBindingId;

        /// <summary>倒计时绑定名（图标下方），如 "CATUI_EntityBleedingTimer"；无倒计时可留空。</summary>
        public string TimerBindingId;

        /// <summary>展示名（预留本地化，当前可为空）</summary>
        public string DisplayName;

        /// <summary>图标 sprite 名（来自 UIAtlas，如 "ui_game_symbol_deep_cuts"）</summary>
        public string Icon;

        /// <summary>图标颜色（RGBA）</summary>
        public Color32 Color;

        /// <summary>是否展示叠加层数（角标位置）</summary>
        public bool HasStack;

        /// <summary>层数来源 cvar 名（如 "bleedCounter"）；为 null 时若无 StackReader 则层数=0</summary>
        public string StackCvar;

        /// <summary>层数上限（0 = 不设限）</summary>
        public float StackMax;

        /// <summary>自定义层数读取（优先于 StackCvar）；null 则回退到 StackCvar</summary>
        public Func<EntityAlive, float> StackReader;

        /// <summary>是否展示倒计时</summary>
        public bool HasTimer;

        /// <summary>倒计时来源 cvar 名（如 "$buffBurningFlamingArrowDuration"）；为 null 时用 UseBuffDuration</summary>
        public string TimerCvar;

        /// <summary>用 buff.DurationMax - DurationInSeconds 计算倒计时（当 TimerCvar 为 null 或读数为 0 时回退）</summary>
        public bool UseBuffDuration;

        /// <summary>关联的原版 buff 名（任一存在即视为激活）</summary>
        public string[] SourceBuffNames;

        /// <summary>自定义激活判定（优先于 SourceBuffNames）；null 则回退到 SourceBuffNames</summary>
        public Func<EntityAlive, bool> ActiveCheck;

        /// <summary>可选：仅对带指定标签的实体生效（任一满足即通过）</summary>
        public string[] RequiredTags;

        /// <summary>自定义倒计时读取（优先于 TimerCvar/UseBuffDuration）；null 则按标准规则</summary>
        public Func<EntityAlive, float> TimerReader;
    }
}
