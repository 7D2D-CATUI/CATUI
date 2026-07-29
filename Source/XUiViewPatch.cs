using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Views;

[HarmonyPatch]
public class XUiViewPatch
{
    private static readonly Dictionary<XUiView, float> elementScales = new Dictionary<XUiView, float>();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiView), "ParseInitialAttributeValue")]
    public static bool ParseInitialAttributeValuePrefix(XUiView __instance, string _attribute, string _value)
    {
        if (_value.Contains("{"))
        {
            return true;
        }

        if (_attribute == "transform_scale")
        {
            float scaleValue = 1f;
            float.TryParse(_value, out scaleValue);

            // 缩放区间0.1-3
            if (scaleValue < 0.1f) // 最小缩放为10%
                scaleValue = 0.1f;
            if (scaleValue > 3f) // 最大缩放为300%
                scaleValue = 3f;

            elementScales[__instance] = scaleValue;
            __instance.isDirty = true;
            return false;
        }

        return !ParseCatuiAttribute(__instance, _attribute, _value);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "InitView")]
    public static void InitViewPostfix(XUiView __instance)
    {
        if (elementScales.Count == 0) return;
        ApplyScale(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "updateData")]
    public static void UpdateDataPostfix(XUiView __instance)
    {
        if (elementScales.Count == 0) return;
        ApplyScale(__instance);
    }

    // 通过OnClose清理已销毁的视图
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiController), "OnClose")]
    public static void OnClosePostfix(XUiController __instance)
    {
        var keysToRemove = new List<XUiView>();
        foreach (var kvp in elementScales)
        {
            if (kvp.Key.Controller == __instance)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            elementScales.Remove(key);
        }
    }

    private static void ApplyScale(XUiView view)
    {
        if (elementScales.TryGetValue(view, out float scale))
        {
            if (view.UiTransform != null)
            {
                view.UiTransform.localScale = Vector3.one * scale;
            }
        }
    }

    private static bool ParseCatuiAttribute(XUiView view, string attribute, string value)
    {
        switch (view)
        {
            case XUiV_AnimatedSprite animatedSprite:
                return animatedSprite.ParseCatuiAttribute(attribute, value);
            default:
                return false;
        }
    }
}
