using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiViewPatch
{
    private static readonly Dictionary<XUiView, float> elementScales = new Dictionary<XUiView, float>();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "ParseAttribute")]
    public static void ParseAttributePostfix(XUiView __instance, string _attribute, string _value)
    {
        if (_attribute == "transform_scale")
        {
            float scaleValue = 1f;
            float.TryParse(_value, out scaleValue);

            // 缩放区间0.1-3
            if (scaleValue < 0.1f) // 最小缩放为10%
                scaleValue = 0.1f;
            if (scaleValue > 3f) // 最大缩放为300%
                scaleValue = 3f;

            // 存储缩放值
            if (elementScales.ContainsKey(__instance))
                elementScales[__instance] = scaleValue;
            else
                elementScales.Add(__instance, scaleValue);

            __instance.IsDirty = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "InitView")]
    public static void InitViewPostfix(XUiView __instance)
    {
        ApplyScale(__instance);
    }

    // Grid等组件需要UpdateData重新缩放
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "UpdateData")]
    public static void UpdateDataPostfix(XUiView __instance)
    {
        ApplyScale(__instance);
    }

    // 缩放方法
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
}