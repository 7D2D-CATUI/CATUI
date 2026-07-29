using HarmonyLib;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiTweenDelayPatch
{
    // 存储每个Tween的延迟时间（仅在初始化阶段临时持有，应用后立即清理）
    private static readonly Dictionary<XUiTweenAbs, float> tweenDelays = new Dictionary<XUiTweenAbs, float>();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiTweenAbs), "ParseInitialAttributeValue")]
    public static bool ParseInitialAttributeValuePrefix(XUiTweenAbs __instance, string _attribute, string _value)
    {
        if (_attribute == "delay")
        {
            if (float.TryParse(_value, out float delay))
            {
                tweenDelays[__instance] = delay;
            }
            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiTweenAbs), "setCommonTweenValues")]
    public static void SetCommonTweenValuesPostfix(XUiTweenAbs __instance, UITweener _tween)
    {
        if (tweenDelays.TryGetValue(__instance, out float delay))
        {
            _tween.delay = delay;
            // 应用后立即移除，避免静态集合持有引用
            tweenDelays.Remove(__instance);
        }
    }
}