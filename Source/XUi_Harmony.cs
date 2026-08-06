/*Copyright 2022 Christopher Beda

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.*/

using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TargetBuffs;

/// <summary>
/// CATUI 字体注入补丁。
/// 类名 CATUI 专属，避免与其他 mod（如 Quartz 的 XUiPatch）发生类型冲突。
/// </summary>
[HarmonyPatch(typeof(XUi))]
public static class XUiFontPatch
{
    private const string TAG = "Error Reverse Patching XUiController method: ";

    // 低优先级：让本 prefix 最后执行。这样当其他 mod（如 Quartz）也挂载了
    // GetUIFontByName prefix 时，若我们持有可用字体，最终结果以我们为准；
    // 若不持有，则保留其他 mod/原版的结果。
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch("GetUIFontByName")]
    public static bool GetUIFontByName(ref NGUIFont __result, string _name, bool _showWarning = true)
    {
        NGUIFont catuiFont = CATUIFontManager.GetNGUIFontByName(_name);

        // 容错：字体不存在 或 资源未加载成功（动态字体缺 dynamicFont/材质/纹理）时，回退到原版字体
        if (CATUIFontManager.IsFontUsable(catuiFont))
        {
            __result = catuiFont;
            return false;
        }

        // 我们没有该字体的可用版本：交给原版/其他 mod 处理（避免覆盖其他 mod 的字体解析结果）
        if (_showWarning)
        {
            Log.Warning("CATUI font not found or unusable: " + _name + ", from: " + StackTraceUtility.ExtractStackTrace());
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("loadAsync")]
    public static IEnumerator LoadAsync(IEnumerator __result, XUi __instance)
    {
        // 初始化 Target buff 控制器（默认注册表）
        TargetBuffController.Initialize();

        Dictionary<string, XUiFromXml.StyleData> styles = XUiFromXml.styles;
        yield return CATUIFontManager.LoadFonts(__instance);
        yield return __result;
        yield break;
    }
}