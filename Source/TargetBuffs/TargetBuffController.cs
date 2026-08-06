using System;
using System.Collections.Generic;
using UnityEngine;

namespace TargetBuffs
{
    /// <summary>
    /// Target buff 控制器核心类。
    /// 集中管理目标 buff 的定义、状态与查询，供 XUi 绑定及其他模块调用。
    /// </summary>
    public static class TargetBuffController
    {
        /// <summary>绑定角色：区分同一 buff 的可见性/层数/倒计时绑定</summary>
        private enum BindingRole
        {
            Primary,  // 主绑定：布尔（IsXxx）或值型（护甲）
            Stack,    // 层数绑定（角标）
            Timer,    // 倒计时绑定
        }

        // ==================== 内部存储 ====================

        // 注册顺序即列表展示顺序
        private static readonly List<TargetBuffDef> _definitions = new List<TargetBuffDef>();

        private static readonly Dictionary<string, TargetBuffDef> _byId =
            new Dictionary<string, TargetBuffDef>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, TargetBuffState> _states =
            new Dictionary<string, TargetBuffState>(StringComparer.OrdinalIgnoreCase);

        // 绑定名 -> (定义, 角色)。用于 GetBindingValue 快速反查
        private static readonly Dictionary<string, KeyValuePair<TargetBuffDef, BindingRole>> _bindingMap =
            new Dictionary<string, KeyValuePair<TargetBuffDef, BindingRole>>(StringComparer.OrdinalIgnoreCase);

        // ==================== 目标状态 ====================

        /// <summary>当前目标实体（可为 null）</summary>
        public static EntityAlive CurrentTarget { get; private set; }

        // ==================== 初始化 ====================

        /// <summary>
        /// 使用默认注册表初始化（幂等：重复调用会重置为默认列表）。
        /// 在 Mod 初始化或 XUi loadAsync 时调用一次。
        /// </summary>
        public static void Initialize()
        {
            Initialize(TargetBuffDefinitionRegistry.BuildDefaults());
        }

        /// <summary>使用自定义列表初始化（覆盖默认列表）。</summary>
        public static void Initialize(IEnumerable<TargetBuffDef> defs)
        {
            _definitions.Clear();
            _byId.Clear();
            _states.Clear();
            _bindingMap.Clear();

            if (defs == null)
            {
                return;
            }

            foreach (TargetBuffDef def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.BindingId))
                {
                    continue;
                }
                _definitions.Add(def);
                // 后注册的同名定义覆盖前者
                _byId[def.BindingId] = def;
                _states[def.BindingId] = new TargetBuffState();
                MapBinding(def, def.BindingId, BindingRole.Primary);
                if (!string.IsNullOrEmpty(def.StackBindingId))
                {
                    MapBinding(def, def.StackBindingId, BindingRole.Stack);
                }
                if (!string.IsNullOrEmpty(def.TimerBindingId))
                {
                    MapBinding(def, def.TimerBindingId, BindingRole.Timer);
                }
            }
        }

        private static void MapBinding(TargetBuffDef def, string bindingId, BindingRole role)
        {
            if (string.IsNullOrEmpty(bindingId))
            {
                return;
            }
            // 绑定名冲突时后者覆盖（多 buff 共用绑定名时以后注册为准）
            _bindingMap[bindingId] = new KeyValuePair<TargetBuffDef, BindingRole>(def, role);
        }

        // ==================== 注册接口 ====================

        /// <summary>动态注册单个 buff 定义（后注册覆盖同名）。</summary>
        public static void Register(TargetBuffDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.BindingId))
            {
                return;
            }
            if (!_byId.ContainsKey(def.BindingId))
            {
                _definitions.Add(def);
                _states[def.BindingId] = new TargetBuffState();
            }
            _byId[def.BindingId] = def;
            MapBinding(def, def.BindingId, BindingRole.Primary);
            if (!string.IsNullOrEmpty(def.StackBindingId))
            {
                MapBinding(def, def.StackBindingId, BindingRole.Stack);
            }
            if (!string.IsNullOrEmpty(def.TimerBindingId))
            {
                MapBinding(def, def.TimerBindingId, BindingRole.Timer);
            }
        }

        /// <summary>取消注册指定 buff（含其全部绑定）。</summary>
        public static void Unregister(string bindingId)
        {
            if (string.IsNullOrEmpty(bindingId))
            {
                return;
            }
            if (!_byId.TryGetValue(bindingId, out TargetBuffDef def))
            {
                return;
            }
            _definitions.Remove(def);
            _byId.Remove(bindingId);
            _states.Remove(bindingId);
            _bindingMap.Remove(bindingId);
            if (!string.IsNullOrEmpty(def.StackBindingId))
            {
                _bindingMap.Remove(def.StackBindingId);
            }
            if (!string.IsNullOrEmpty(def.TimerBindingId))
            {
                _bindingMap.Remove(def.TimerBindingId);
            }
        }

        // ==================== 目标切换与刷新 ====================

        /// <summary>
        /// 切换当前目标。目标变化时清空全部状态。
        /// 目标为 null 或已死亡时，所有 buff 置为未激活。
        /// </summary>
        public static void SetTarget(EntityAlive target)
        {
            bool changed = !ReferenceEquals(CurrentTarget, target);
            CurrentTarget = target;

            if (!changed)
            {
                return;
            }

            foreach (TargetBuffState state in _states.Values)
            {
                state.Reset();
            }
        }

        /// <summary>
        /// 重算所有 buff 状态。建议由 UI 控制器按帧（或限频）调用。
        /// </summary>
        public static void Refresh()
        {
            EntityAlive target = CurrentTarget;
            bool hasValidTarget = target != null && target.IsAlive();

            foreach (TargetBuffDef def in _definitions)
            {
                TargetBuffState state = _states[def.BindingId];

                bool wasActive = state.IsActive;
                float oldStack = state.StackCount;
                float oldTime = state.TimeRemaining;

                if (!hasValidTarget || !IsTargetEligible(def, target))
                {
                    state.Reset();
                }
                else
                {
                    Evaluate(def, target, state);
                }

                state.HasChanged =
                    state.IsActive != wasActive ||
                    state.StackCount != oldStack ||
                    state.TimeRemaining != oldTime;
            }
        }

        // ==================== 查询接口 ====================

        /// <summary>是否存在指定绑定（主/层数/倒计时任一）。</summary>
        public static bool HasBinding(string bindingId)
        {
            return !string.IsNullOrEmpty(bindingId) && _bindingMap.ContainsKey(bindingId);
        }

        /// <summary>主绑定是否已定义且当前激活。</summary>
        public static bool IsActive(string bindingId)
        {
            return TryGetState(bindingId, out TargetBuffState state) && state.IsActive;
        }

        /// <summary>获取指定主绑定的运行时状态（未定义返回 null）。</summary>
        public static TargetBuffState GetState(string bindingId)
        {
            return TryGetState(bindingId, out TargetBuffState state) ? state : null;
        }

        /// <summary>按注册顺序返回全部定义。</summary>
        public static IReadOnlyList<TargetBuffDef> GetDefinitions()
        {
            return _definitions;
        }

        /// <summary>按注册顺序返回当前激活的 buff 定义列表（供 UI 遍历渲染）。</summary>
        public static List<TargetBuffDef> GetActiveBuffs()
        {
            var result = new List<TargetBuffDef>();
            foreach (TargetBuffDef def in _definitions)
            {
                if (_states.TryGetValue(def.BindingId, out TargetBuffState state) && state.IsActive)
                {
                    result.Add(def);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取绑定值（供 XUiC_TargetBarPatch 委托调用）。
        /// 规则：
        /// - 主绑定（布尔型）：激活返回 "True"，否则 "False"；
        /// - 主绑定（值型，如护甲）：返回数值（向上取整）；
        /// - 层数绑定：激活时返回层数（向上取整），否则 "0"；
        /// - 倒计时绑定：激活时返回剩余秒数（向上取整，避免显示 0），否则 "0"。
        /// </summary>
        public static string GetBindingValue(string bindingId)
        {
            if (string.IsNullOrEmpty(bindingId) ||
                !_bindingMap.TryGetValue(bindingId, out KeyValuePair<TargetBuffDef, BindingRole> entry))
            {
                return null; // 非本控制器管理的绑定，交由原 switch 处理
            }

            TargetBuffDef def = entry.Key;
            TargetBuffState state = _states[def.BindingId];
            bool active = state.IsActive;

            switch (entry.Value)
            {
                case BindingRole.Primary:
                    if (def.IsValueBinding)
                    {
                        return active ? Mathf.CeilToInt(state.StackCount).ToString() : "0";
                    }
                    return active ? "True" : "False";

                case BindingRole.Stack:
                    return active ? Mathf.CeilToInt(state.StackCount).ToString() : "0";

                case BindingRole.Timer:
                    return active ? Mathf.CeilToInt(state.TimeRemaining).ToString() : "0";

                default:
                    return "False";
            }
        }

        // ==================== 内部实现 ====================

        private static bool TryGetState(string bindingId, out TargetBuffState state)
        {
            if (string.IsNullOrEmpty(bindingId) || !_byId.TryGetValue(bindingId, out TargetBuffDef def))
            {
                state = null;
                return false;
            }
            return _states.TryGetValue(def.BindingId, out state);
        }

        /// <summary>标签过滤：def 指定了 RequiredTags 时，目标需命中任一标签才激活。</summary>
        private static bool IsTargetEligible(TargetBuffDef def, EntityAlive target)
        {
            if (def.RequiredTags == null || def.RequiredTags.Length == 0)
            {
                return true;
            }
            if (!EntityClass.list.TryGetValue(target.entityClass, out EntityClass entityClass))
            {
                return false;
            }
            for (int i = 0; i < def.RequiredTags.Length; i++)
            {
                if (entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit(def.RequiredTags[i])))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>计算单个 buff 的激活状态、层数与倒计时。</summary>
        private static void Evaluate(TargetBuffDef def, EntityAlive target, TargetBuffState state)
        {
            bool active = IsBuffActive(def, target);
            state.Reset();
            if (!active)
            {
                return;
            }

            state.IsActive = true;
            state.StackCount = GetStack(def, target);
            if (def.HasTimer)
            {
                state.TimeRemaining = GetTimer(def, target);
            }
        }

        /// <summary>激活判定：优先自定义 ActiveCheck，否则任一 SourceBuff 存在即激活。</summary>
        private static bool IsBuffActive(TargetBuffDef def, EntityAlive target)
        {
            if (def.ActiveCheck != null)
            {
                return def.ActiveCheck(target);
            }
            if (def.SourceBuffNames != null)
            {
                for (int i = 0; i < def.SourceBuffNames.Length; i++)
                {
                    if (target.Buffs.GetBuff(def.SourceBuffNames[i]) != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>层数解析：优先自定义 StackReader，否则读 StackCvar（缺失返回 0），再按 StackMax 钳制。</summary>
        private static float GetStack(TargetBuffDef def, EntityAlive target)
        {
            float count = 0f;
            if (def.StackReader != null)
            {
                count = def.StackReader(target);
            }
            else if (!string.IsNullOrEmpty(def.StackCvar))
            {
                count = target.Buffs.GetCustomVar(def.StackCvar);
            }

            if (def.StackMax > 0f && count > def.StackMax)
            {
                count = def.StackMax;
            }
            if (count < 0f)
            {
                count = 0f;
            }
            return count;
        }

        /// <summary>倒计时解析：优先自定义 TimerReader，否则 TimerCvar，最后回退到 buff 时长。</summary>
        private static float GetTimer(TargetBuffDef def, EntityAlive target)
        {
            float timer = 0f;

            if (def.TimerReader != null)
            {
                timer = def.TimerReader(target);
            }
            else if (!string.IsNullOrEmpty(def.TimerCvar))
            {
                timer = target.Buffs.GetCustomVar(def.TimerCvar);
            }

            if (timer <= 0f && def.UseBuffDuration)
            {
                timer = GetLongestBuffRemaining(def, target);
            }

            if (timer < 0f)
            {
                timer = 0f;
            }
            return timer;
        }

        /// <summary>取所有来源 buff 中最长的剩余时长。</summary>
        private static float GetLongestBuffRemaining(TargetBuffDef def, EntityAlive target)
        {
            if (def.SourceBuffNames == null)
            {
                return 0f;
            }
            float max = 0f;
            for (int i = 0; i < def.SourceBuffNames.Length; i++)
            {
                BuffValue buff = target.Buffs.GetBuff(def.SourceBuffNames[i]);
                if (buff == null)
                {
                    continue;
                }
                float remaining = buff.BuffClass.DurationMax - buff.DurationInSeconds;
                if (remaining > max)
                {
                    max = remaining;
                }
            }
            return max;
        }
    }
}
