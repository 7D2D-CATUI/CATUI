using HarmonyLib;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiTweenDelayPatch
{
    // 存储每个Tween的延迟时间
    private static readonly Dictionary<XUiTweenAbs, float> tweenDelays = new Dictionary<XUiTweenAbs, float>();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiTweenAbs), "ParseInitialAttributeValue")]
    public static bool ParseInitialAttributeValuePrefix(XUiTweenAbs __instance, string _attribute, string _value)
    {
        // 设置动画延迟时间
        if (_attribute == "delay")
        {
            if (float.TryParse(_value, out float delay))
            {
                if (tweenDelays.ContainsKey(__instance))
                    tweenDelays[__instance] = delay;
                else
                    tweenDelays.Add(__instance, delay);
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
        }
    }
}