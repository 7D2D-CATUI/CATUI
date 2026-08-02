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
}
