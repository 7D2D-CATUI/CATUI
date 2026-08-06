namespace TargetBuffs
{
    /// <summary>
    /// Target buff 运行时状态（每次 Refresh 重新计算）。
    /// 供 UI 查询当前是否激活、层数与剩余倒计时。
    /// </summary>
    public class TargetBuffState
    {
        /// <summary>当前是否激活</summary>
        public bool IsActive;

        /// <summary>当前层数（0 = 无层数或无激活）</summary>
        public float StackCount;

        /// <summary>剩余秒数（&gt;0 时有效；已激活但无倒计时时为 0）</summary>
        public float TimeRemaining;

        /// <summary>本轮刷新相对上一轮是否有变化（供 UI 决定是否需要重绘）</summary>
        public bool HasChanged;

        /// <summary>重置为未激活的默认值（保留 HasChanged 供比较）</summary>
        public void Reset()
        {
            IsActive = false;
            StackCount = 0f;
            TimeRemaining = 0f;
        }
    }
}
