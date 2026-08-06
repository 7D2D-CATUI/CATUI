using HarmonyLib;
using UnityEngine;

/// <summary>
/// NGUI UIDrawCall 渲染防御补丁。
/// 解决"进入游戏后画面粉紫 + 卡死"问题：
/// 当某个 UIWidget 的材质存在但 shader 为 null 时，NGUI 的
/// UIDrawCall.CreateMaterial()（UIDrawCall.cs:364）访问
/// mMaterial.shader.name 会抛 NullReferenceException，
/// 导致 UIPanel 渲染循环每帧刷 NRE、画面无法正常绘制（呈粉紫色）。
///
/// 本补丁在 CreateMaterial 执行前把空 shader 的材质修复为默认文本着色器，
/// 避免渲染循环崩溃，保证界面正常显示。
/// </summary>
[HarmonyPatch(typeof(UIDrawCall))]
public static class UIDrawCallDefensePatch
{
    private static Shader _fallbackShader;

    private static Shader GetFallbackShader()
    {
        if (_fallbackShader != null)
        {
            return _fallbackShader;
        }
        _fallbackShader = Shader.Find("Unlit/Text");
        if (_fallbackShader == null)
        {
            _fallbackShader = Shader.Find("GUI/Text Shader");
        }
        if (_fallbackShader == null)
        {
            _fallbackShader = Shader.Find("Unlit/Transparent Colored");
        }
        return _fallbackShader;
    }

    /// <summary>
    /// 在 CreateMaterial 前修复空 shader 的 baseMaterial，避免渲染循环 NRE。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("CreateMaterial")]
    public static void CreateMaterialPrefix(UIDrawCall __instance)
    {
        try
        {
            Material baseMat = __instance.baseMaterial;
            if (baseMat == null)
            {
                return;
            }
            if (baseMat.shader != null)
            {
                return;
            }

            // baseMaterial 存在但 shader 为 null：替换为默认文本着色器，避免 NRE 与粉屏
            Shader fallback = GetFallbackShader();
            if (fallback == null)
            {
                return;
            }
            baseMat.shader = fallback;
            Debug.LogWarning("[CATUI] UIDrawCall baseMaterial had null shader, repaired to " + fallback.name);
        }
        catch (System.Exception e)
        {
            // 防御性补丁：任何异常都不应影响主流程
            Debug.LogWarning("[CATUI] CreateMaterialPrefix error: " + e.Message);
        }
    }
}
