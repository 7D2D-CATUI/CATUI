using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_ItemInfoWindow))]
public class XUiC_ItemInfoWindowPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_ItemInfoWindow), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_ItemInfoWindow __instance)
	{
		switch (bindingName)
		{
			// 道具耐久
			case "CATUI_ItemUseTimesResidue":
				ItemStack itemStack = __instance.itemStack;
				if (itemStack.IsEmpty())
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
						value = (itemStack.itemValue.MaxUseTimes - itemStack.itemValue.UseTimes).ToString("F0");
					}
				}
				__result = true;
				return false;
			// 道具耐久最大值
			case "CATUI_ItemUseTimesMax":
				ItemStack _itemStack = __instance.itemStack;
				if (_itemStack.IsEmpty())
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
}
