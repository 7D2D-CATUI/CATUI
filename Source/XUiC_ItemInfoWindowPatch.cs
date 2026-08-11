using HarmonyLib;
using System;
using UnityEngine;

[HarmonyPatch]
public class XUiC_ItemInfoWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_ItemInfoWindow), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(ref string value, string bindingName, ref bool __result, XUiC_ItemInfoWindow __instance)
	{
		try
		{
			switch (bindingName)
			{
				// 道具耐久
				case "CATUI_ItemUseTimesResidue":
					ItemStack itemStack = __instance?.itemStack;
					if (itemStack == null || itemStack.IsEmpty())
					{
						value = "0";
					}
					else
					{
						if (itemStack.itemValue.MaxUseTimes == 0)
						{
							value = "1";
						}
						else
						{
							value = (itemStack.itemValue.MaxUseTimes - Convert.ToInt32(itemStack.itemValue.UseTimes)).ToString("F0");
						}
					}
					__result = true;
					return false;
				// 道具耐久最大值
				case "CATUI_ItemUseTimesMax":
					ItemStack _itemStack = __instance?.itemStack;
					if (_itemStack == null || _itemStack.IsEmpty())
					{
						value = "0";
					}
					else
					{
						if (_itemStack.itemValue.MaxUseTimes == 0)
						{
							value = "1";
						}
						else
						{
							value = _itemStack.itemValue.MaxUseTimes.ToString("F0");
						}
					}
					__result = true;
					return false;
				default:
					return true;
			}
		}
		catch (Exception e)
		{
			Log.Out("CATUI: GetBindingValueInternalPrefix error for binding '" + bindingName + "': " + e);
			value = "1";
			__result = true;
			return false;
		}
	}

	// 增强属性前的 [sp=ui_stat] 是 NGUI iconfont（星星，渲染成正常字号两倍），替换成普通文本 ★，字号与数值一致
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiM_ItemStack), "GetStatItemValueTextWithModColoring")]
	public static void GetStatItemValueTextWithModColoringPostfix(ref string __result)
	{
		try
		{
			if (__result != null && __result.StartsWith("[sp=ui_stat]"))
			{
				__result = "★ " + __result.Substring("[sp=ui_stat]".Length);
			}
		}
		catch
		{
		}
	}
}
