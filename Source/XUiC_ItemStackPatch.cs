using Audio;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[HarmonyPatch]
public class XUiC_ItemStackPatch
{
    private const string ALLOW_CLICKLOCK_ATTR = "allow_clicklock";
    private static readonly HashSet<XUiC_ItemStack> _patchedInstances = new HashSet<XUiC_ItemStack>();
    private const string MODIFICATION_HIGHLIGHTED = "[04FE85]▇[-] ";
    private const string MODIFICATION_DEFAULT = "▇ ";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "Init")]
    public static void InitPostfix(XUiC_ItemStack __instance)
    {
		if (_patchedInstances.Contains(__instance))
			return;

		_patchedInstances.Add(__instance);
		__instance.OnPress += (XUiController _sender, int _mouseButton) =>
		{
            // 判断alt键是否按下
			if (!InputUtils.AltKeyPressed)
				return;

            // 是否允许栏位锁（配置写在xml上，allow_clicklock="${allow_clicklock}"）
			bool allowClicklock = false;
			if (_sender.CustomAttributes.ContainsKey(ALLOW_CLICKLOCK_ATTR))
			{
				allowClicklock = StringParsers.ParseBool(_sender.CustomAttributes[ALLOW_CLICKLOCK_ATTR].ToString());
			}
			if (!allowClicklock)
				return;

			var backpackWindow = __instance.xui.GetChildByType<XUiC_BackpackWindow>();
			var lootWindow = __instance.xui.GetChildByType<XUiC_LootWindow>();
			var vehicleContainer = __instance.xui.GetChildByType<XUiC_BagContainer>();

            __instance.UserLockedSlot = !__instance.UserLockedSlot;
            __instance.RefreshBindings();

            // 背包 更新栏位锁状态
            if (_sender.Parent.ToString() == "XUiC_Backpack") {
                backpackWindow.UpdateLockedSlots(backpackWindow.standardControls);
            }
            // 箱子 容器窗口是否打开 更新栏位锁状态 2.2+
            else if (_sender.Parent.ToString() == "XUiC_LootContainer" && lootWindow.IsOpen)
            {
                lootWindow.UpdateLockedSlots(lootWindow.standardControls);
            }
            // 载具 容器窗口是否打开 更新栏位锁状态 2.2+
            else if (_sender.Parent.ToString() == "XUiController" && vehicleContainer.IsOpen)
            {
                vehicleContainer.UpdateLockedSlots(vehicleContainer.standardControls);
            }

            // 播放点击音效
            __instance.xui.PlayMenuClickSound();
        };
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "GetBindingValueInternal")]
    public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_ItemStack __instance)
    {
        ItemValue itemValue = __instance.itemStack?.itemValue;
        ItemClass itemClass = itemValue.ItemClass;
        switch (_bindingName)
        {
            // 给道具增加index
            case "CATUI_itemStackSlotIndex":
                _value = "0";
                if (__instance?.SlotNumber != null)
                {
                    _value = (__instance.SlotNumber + 1).ToString();
                }
                __result = true;
                return false;

            // 判断物品是否为全属性Boosted（传奇品质），排除潜行伤害属性
            case "CATUI_itemBoosted":
                _value = "false";
                if (__instance?.itemStack?.itemValue == null)
                {
                    __result = true;
                    return false;
                }

                if (itemValue.HasAnyBoostedStats())
                {
                    _value = "true";
                }
                __result = true;
                return false;

            // 判断物品是否为全属性Boosted（传奇品质）
            case "CATUI_itemLegendary":
                _value = "false";
                if (__instance?.itemStack?.itemValue == null)
                {
                    __result = true;
                    return false;
                }

                if (itemValue.Stats == null || itemValue.Stats.Length == 0)
                {
                    _value = "false";
                }
                else
                {
                    bool allBoosted = true;
                    for (int i = 0; i < itemValue.Stats.Length; i++)
                    {
                        var stat = itemValue.Stats[i];
                        // 如果有任何一个非排除属性没有被Boosted，则不是全Boosted
                        if (!stat.isBoosted)
                        {
                            allBoosted = false;
                            break;
                        }
                    }
                    _value = allBoosted.ToString().ToLower();
                }
                __result = true;
                return false;

            // 调试用
            case "CATUI_itemBoostList":
                _value = "";
                if (itemValue.Stats == null || itemValue.Stats.Length == 0)
                {
                    _value = "";
                }
                else
                {
                    string text = string.Empty;
                    for (int i = 0; i < itemValue.Stats.Length; i++)
                    {
                        var stat = itemValue.Stats[i];
                        text += stat.type;
                        text += ": ";
                        text += stat.isBoosted + " - ";
                        text += stat.value;
                        if (i < itemValue.Stats.Length - 1)
                        {
                            text += ", \n";
                        }
                    }
                    _value = text;
                }
                __result = true;
                return false;

            // 道具插槽状态
            case "CATUI_itemStackModifications":
                _value = "";
                if (__instance?.itemStack?.itemValue == null)
                {
                    Debug.Log($"<color=#00FF00>[CATUI] CATUI_itemStackModifications __instance?.itemStack?.itemValue == null </color>");
                    __result = true;
                    return false;
                }

                // 取View上的属性
                __instance.CustomAttributes.TryGetValue("mod_highlight", out var highlightStyle);
                __instance.CustomAttributes.TryGetValue("mod_default", out var defaultStyle);
                highlightStyle ??= MODIFICATION_HIGHLIGHTED;
                defaultStyle ??= MODIFICATION_DEFAULT;

                ItemValue[] mods = itemValue.Modifications;
                
                // mods数组空值检查
                if (itemValue.Quality <= 0 || mods == null || mods.Length == 0)
                {
                    __result = true;
                    return false;
                }

                System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
                for (int i = 0; i < mods.Length; i++)
                {
                    // 未安装过模组 mods[i] == null，直接设置空槽
                    if (mods[i] == null)
                    {
                        textBuilder.Append(defaultStyle);
                        continue;
                    }

                    var itemClass1 = mods[i]?.ItemClass;
                    if (itemClass1 != null && !string.IsNullOrEmpty(itemClass1?.GetItemName()))
                    {
                        textBuilder.Append(highlightStyle);
                    }
                    else
                    {
                        textBuilder.Append(defaultStyle);
                    }
                }
                _value = textBuilder.ToString();
                __result = true;
                return false;
            default:
                return true;
        }
    }

    // 去掉鼠标hover的icon缩放动画
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "AllowIconGrow", MethodType.Getter)]
    public static void AllowIconGrowPostfix(ref bool __result)
    {
        __result = false;
    }
}