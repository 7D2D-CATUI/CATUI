using HarmonyLib;
using UnityEngine;
using System.Runtime.CompilerServices;
using Views;

[HarmonyPatch]
public class XUiViewPatch
{
    // ConditionalWeakTable 在视图对象被 GC 时自动移除条目，无需在 OnClose 线性扫描清理
    private class ScaleValue
    {
        public float value;
    }

    private static readonly ConditionalWeakTable<XUiView, ScaleValue> elementScales = new ConditionalWeakTable<XUiView, ScaleValue>();

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

            ScaleValue holder;
            if (!elementScales.TryGetValue(__instance, out holder))
            {
                holder = new ScaleValue();
                elementScales.Add(__instance, holder);
            }
            holder.value = scaleValue;
            __instance.isDirty = true;
            return false;
        }

        return !ParseCatuiAttribute(__instance, _attribute, _value);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "InitView")]
    public static void InitViewPostfix(XUiView __instance)
    {
        ApplyScale(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiView), "updateData")]
    public static void UpdateDataPostfix(XUiView __instance)
    {
        ApplyScale(__instance);
    }

    private static void ApplyScale(XUiView view)
    {
        ScaleValue holder;
        if (!elementScales.TryGetValue(view, out holder))
        {
            return;
        }
        if (view.UiTransform == null)
        {
            return;
        }
        float scale = holder.value;
        Vector3 localScale = view.UiTransform.localScale;
        if (localScale.x != scale || localScale.y != scale || localScale.z != scale)
        {
            view.UiTransform.localScale = Vector3.one * scale;
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
