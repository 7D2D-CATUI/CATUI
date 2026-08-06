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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CATUI 自定义字体管理器。
/// 类名带 CATUI 前缀，避免与其他 mod（如 Quartz 的 FontManager）发生类型冲突。
/// </summary>
public static class CATUIFontManager
{
    private static Dictionary<string, NGUIFont> fonts = new Dictionary<string, NGUIFont>();

    private static NGUIFont referenceFont;

    private static bool loaded;

    private const string TAG = "CATUIFontManager";

    public const string styleKeyNGUIFonts = "Fonts.NGUIFonts";
    public const string styleKeyUnityFonts = "Fonts.UnityFonts";
    public const string styleKeyOSFonts = "Fonts.OSFonts";

    public static NGUIFont GetVanillaFont()
    {
        return referenceFont;
    }

    public static IEnumerator LoadFonts(XUi xui)
    {
        yield return null;
        while (!XUiFromXml.HasData())
        {
            yield return null;
        }

        yield return null;

        Debug.Log("Loading Fonts");
        bool loadedXUIFonts = LoadXUiFonts(xui);
        if (!loadedXUIFonts)
        {
            Debug.LogWarning("Unable to load XUi Fonts");
        }

        XUiFromXml.StyleData fontData;
        if (TryGetStyle(styleKeyNGUIFonts, out fontData))
        {
            foreach (XUiFromXml.StyleEntryData fontEntry in fontData.StyleEntries.Values)
            {
                bool success = LoadNGUIFont(fontEntry.Name, fontEntry.Value);

                if (!success)
                {
                    Debug.LogWarning("Unable to load Font: " + fontEntry.Name);
                }
            }
        }

        if (TryGetStyle(styleKeyUnityFonts, out fontData))
        {
            foreach (XUiFromXml.StyleEntryData fontEntry in fontData.StyleEntries.Values)
            {
                bool success = LoadUnityFont(fontEntry.Name, fontEntry.Value);

                if (!success)
                {
                    Debug.LogWarning("Unable to load Font: " + fontEntry.Name);
                }
            }
        }

        if (TryGetStyle(styleKeyOSFonts, out fontData))
        {
            foreach (XUiFromXml.StyleEntryData fontEntry in fontData.StyleEntries.Values)
            {
                bool success = LoadOSInstalledFont(fontEntry.Value);

                if (!success)
                {
                    Debug.LogWarning("Unable to load Font: " + fontEntry.Name);
                }
            }
        }

        Debug.Log("Loaded Fonts");

        yield break;
    }

    private static bool TryGetStyle(string key, out XUiFromXml.StyleData style)
    {
        // 兼容两种 style key 形式：`Fonts.UnityFonts` 与 `.Fonts.UnityFonts`。
        // 某些情况下会出现同名的空 style（无 style_entry），这里优先返回非空的，
        // 避免空 style 遮蔽真正注册了字体的 style。
        style = null;
        XUiFromXml.StyleData candidate = null;

        if (XUiFromXml.styles.TryGetValue(key, out candidate) && candidate.StyleEntries.Count > 0)
        {
            style = candidate;
            return true;
        }

        if (XUiFromXml.styles.TryGetValue("." + key, out candidate) && candidate.StyleEntries.Count > 0)
        {
            style = candidate;
            return true;
        }

        // 两个都为空时，返回第一个命中的（交给上层判定）
        if (XUiFromXml.styles.TryGetValue(key, out candidate))
        {
            style = candidate;
            return true;
        }
        if (XUiFromXml.styles.TryGetValue("." + key, out candidate))
        {
            style = candidate;
            return true;
        }
        return false;
    }

    public static bool LoadXUiFonts(XUi xui)
    {
        if (!loaded)
        {
            foreach (NGUIFont nguiFont in xui.NGUIFonts)
            {
                fonts.Add(nguiFont.name, nguiFont);

                if (nguiFont.name != nguiFont.spriteName)
                {
                    fonts.Add(nguiFont.spriteName, nguiFont);
                }

                if (nguiFont.name == "ReferenceFont")
                {
                    referenceFont = nguiFont;
                }
            }

            loaded = true;
        }

        return loaded;
    }

    public static NGUIFont GetNGUIFontByName(string name)
    {
        NGUIFont font = null;
        fonts.TryGetValue(name, out font);

        return font;
    }

    /// <summary>
    /// 校验字体是否真正可用（容错：字体资源未加载成功时返回 false，供上层回退到原版字体）。
    /// - 动态字体：必须有非空 dynamicFont（纹理由 NGUI 按需生成，不在此强校验）；
    /// - 位图字体：必须有非空 atlas 与材质。
    /// </summary>
    public static bool IsFontUsable(NGUIFont font)
    {
        if (font == null)
        {
            return false;
        }
        try
        {
            if (font.isDynamic)
            {
                // 动态字体：资源未加载成功则不可用
                return font.dynamicFont != null;
            }
            // 位图/引用字体：有 atlas 与材质即视为可用
            if (font.atlas != null)
            {
                Material mat = font.material;
                return mat != null && mat.mainTexture != null;
            }
            return font.material != null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(TAG + "IsFontUsable failed for " + font?.name + ": " + e.Message);
            return false;
        }
    }

    // 确保动态字体拥有非空且带有效 shader 的材质。
    // NGUI 的 UIDrawCall.CreateMaterial() 会访问 mMaterial.shader.name，
    // 若材质为空 shader 会导致渲染循环每次 UpdateMaterials 抛 NullReferenceException。
    private static void EnsureFontMaterial(NGUIFont font, Font dynamicFont)
    {
        try
        {
            Material fontMaterial = font.material;
            if (fontMaterial != null && fontMaterial.shader != null)
            {
                return;
            }

            if (dynamicFont != null && dynamicFont.material != null && dynamicFont.material.shader != null)
            {
                return;
            }

            // 兜底：用文本 shader 创建一个新材质挂到字体上，避免空材质/空 shader
            Shader textShader = Shader.Find("Unlit/Text");
            if (textShader == null)
            {
                textShader = Shader.Find("GUI/Text Shader");
            }
            if (textShader == null)
            {
                textShader = Shader.Find("Unlit/Transparent Colored");
            }

            if (textShader != null)
            {
                Material mat = new Material(textShader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                if (dynamicFont != null && dynamicFont.material != null && dynamicFont.material.mainTexture != null)
                {
                    mat.mainTexture = dynamicFont.material.mainTexture;
                }
                font.material = mat;
                Debug.LogWarning(TAG + "Font: " + font.name + " missing material, created fallback");
            }
            else
            {
                Debug.LogWarning(TAG + "Font: " + font.name + " no text shader available");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(TAG + "EnsureFontMaterial failed for " + font.name + ": " + e.Message);
        }
    }

    public static bool LoadUnityFont(string fontName, string path)
    {
        if (fonts.ContainsKey(fontName))
        {
            return true;
        }

        NGUIFont font = null;

        if (path.Contains("@modfolder("))
        {
            Debug.Log($"<color=#00FF00>[CATUI] fontName: " + fontName + " </color>");
            Debug.Log($"<color=#00FF00>[CATUI] fontName: " + path + " </color>");

            Font loadedFont = DataLoader.LoadAsset<Font>(path);

            if (loadedFont != null)
            {
                font = ScriptableObject.CreateInstance<NGUIFont>();
                font.name = fontName;
                font.dynamicFont = loadedFont;

                // 确保动态字体有可用的材质，避免 drawCall 拿到空 shader 的材质导致渲染循环 NRE
                EnsureFontMaterial(font, loadedFont);

                // 容错：材质/纹理仍不可用则放弃注册，让 GetUIFontByName 回退到原版字体
                if (IsFontUsable(font))
                {
                    fonts.Add(fontName, font);
                    Debug.Log(TAG + "Font: " + fontName + " loaded");
                }
                else
                {
                    Debug.LogWarning(TAG + "Font: " + fontName + " loaded but unusable, skipping registration");
                    Object.Destroy(font);
                    font = null;
                }
            }
        }

        return font != null;
    }

    public static bool LoadNGUIFont(string fontName, string path)
    {
        if (fonts.ContainsKey(fontName))
        {
            return true;
        }

        NGUIFont font = null;

        if (path.Contains("@modfolder("))
        {
            font = DataLoader.LoadAsset<NGUIFont>(path);

            if (font != null)
            {
                fonts.Add(fontName, font);
                Debug.Log(TAG + "Font: " + fontName + " loaded");
            }
        }

        return font != null;
    }

    public static bool LoadOSInstalledFont(string fontName)
    {
        if (fonts.ContainsKey(fontName))
        {
            return true;
        }

        string[] osFonts = Font.GetOSInstalledFontNames();
        Font loadedFont = null;
        NGUIFont font = null;

        foreach (string osFont in osFonts)
        {
            if (osFont == fontName)
            {
                loadedFont = Font.CreateDynamicFontFromOSFont(osFont, 30);
            }
        }

        if (loadedFont != null)
        {
            font = ScriptableObject.CreateInstance<NGUIFont>();
            font.name = fontName;
            font.dynamicFont = loadedFont;

            fonts.Add(fontName, font);
            Debug.Log(TAG + "Font: " + fontName + " loaded");
        }

        return font != null;

    }
}