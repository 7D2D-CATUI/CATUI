using System.IO;
using System.Xml.Linq;
using UnityEngine;

/// <summary>
/// CATUI 配置读取器。
///
/// 读取 LocalLoadEnabled 开关，取值优先级：
///   1. Gears 存档 <用户数据>/7DaysToDie/Gears/ModSettings.xml 中
///      Mod[@name='CATUI'] 下的 Setting[@name='CATUI_LocalLoadEnabled'] 的 value
///      （玩家通过 Gears GUI 修改后，Gears 把当前值保存在此处；此时 mod 文件
///       内的 value 属性会被 Gears 移除，只剩 defaultValue）；
///   2. ZZZ_CATUI/ModSettings.xml 中 Switch[@name='CATUI_LocalLoadEnabled'] 的 value
///      （无 Gears 时玩家手动修改此文件，格式与 Quartz/Gears 一致）；
///   3. 都不存在时使用默认值 On。
///
/// 本读取器不依赖 GearsAPI，Gears 缺失时 CATUI 完全正常，仅失去 GUI 入口。
/// </summary>
public static class CatuiSettings
{
    private const string TAG = "[CATUI]";
    private const string SwitchName = "CATUI_LocalLoadEnabled";

    /// <summary>配置文件路径（CATUI mod 根目录下）</summary>
    public static string SettingsFilePath
    {
        get
        {
            string path = null;
            foreach (Mod mod in ModManager.GetLoadedMods())
            {
                if (mod.Name != null && mod.Name.StartsWith("CATUI", System.StringComparison.OrdinalIgnoreCase))
                {
                    path = mod.Path + "/ModSettings.xml";
                    break;
                }
            }
            return path;
        }
    }

    /// <summary>Gears 保存的 mod 当前值存档路径。</summary>
    public static string GearsSavedSettingsFilePath
    {
        get { return GameIO.GetUserGameDataDir() + "/Gears/ModSettings.xml"; }
    }

    /// <summary>
    /// 读取配置并应用。可在 InitMod 与 ModEvents.GameAwake 各调用一次（幂等）。
    /// Gears 的存档在启动早期可能尚未生成，故 GameAwake 时再刷新一次以保证读到 GUI 值。
    /// </summary>
    public static void Load()
    {
        string value = ReadFromGearsSave();
        string source = "Gears save";

        if (value == null)
        {
            value = ReadFromModFile();
            source = "ModSettings.xml";
        }

        if (value == null)
        {
            Log.Out("{0} 'LocalLoadEnabled' not found, using default (On).", TAG);
            return;
        }

        bool enabled = ParseOnOff(value);
        LocalLoadPatch.EnableLocalLoad = enabled;
        Log.Out("{0} ModSettings: LocalLoadEnabled = {1} (from {2})", TAG, enabled ? "On" : "Off", source);
    }

    /// <summary>从 Gears 存档读取当前值（Gears GUI 修改后的权威来源）。</summary>
    private static string ReadFromGearsSave()
    {
        string path = GearsSavedSettingsFilePath;
        if (path == null || !SdFile.Exists(path))
        {
            return null;
        }
        try
        {
            XElement root = XElement.Load(path);
            foreach (XElement mod in root.Elements("Mod"))
            {
                string modName = mod.Attribute("name")?.Value;
                if (modName != null && modName.StartsWith("CATUI", System.StringComparison.OrdinalIgnoreCase))
                {
                    return FindSettingValue(mod, SwitchName);
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Warning("{0} Failed to parse Gears saved settings at '{1}': {2}", TAG, path, e.Message);
        }
        return null;
    }

    /// <summary>从 CATUI mod 文件读取 Switch 的 value（无 Gears 手动编辑场景）。</summary>
    private static string ReadFromModFile()
    {
        string path = SettingsFilePath;
        if (path == null || !SdFile.Exists(path))
        {
            return null;
        }
        try
        {
            XElement root = XElement.Load(path);
            XElement global = root.Element("Global");
            if (global == null)
            {
                return null;
            }
            foreach (XElement tab in global.Elements("Tab"))
            {
                foreach (XElement category in tab.Elements("Category"))
                {
                    foreach (XElement setting in category.Elements())
                    {
                        string name = setting.Attribute("name")?.Value;
                        if (name == SwitchName)
                        {
                            return setting.Attribute("value")?.Value;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Warning("{0} Failed to parse ModSettings.xml at '{1}': {2}", TAG, path, e.Message);
        }
        return null;
    }

    /// <summary>在 Mod 节点下按 name 查找 Setting/Switch 并返回其 value。</summary>
    private static string FindSettingValue(XElement modRoot, string settingName)
    {
        foreach (XElement tab in modRoot.Elements("Tab"))
        {
            foreach (XElement category in tab.Elements("Category"))
            {
                foreach (XElement setting in category.Elements())
                {
                    string name = setting.Attribute("name")?.Value;
                    if (name == settingName)
                    {
                        return setting.Attribute("value")?.Value;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>把 Gears 的 On/Off（或 High/Low、True/False）解析为 bool。</summary>
    private static bool ParseOnOff(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }
        switch (value.Trim().ToLowerInvariant())
        {
            case "off":
            case "low":
            case "false":
            case "0":
            case "disabled":
            case "disable":
                return false;
            default:
                return true;
        }
    }
}
