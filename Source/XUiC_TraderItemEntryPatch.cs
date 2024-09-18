using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_CompassWindow))]
public class XUiC_TraderItemEntryPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_TraderItemEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_TraderItemEntry __instance)
	{
		switch (bindingName)
		{
			// 商品名称
			case "CATUI_ItemName":
				value = "";
				if (__instance.item != null)
				{
					value = __instance.itemClass.GetLocalizedItemName();
				}

				__result = true;
				return false;

			// 商品库存
			case "CATUI_ItemCount":
				value = "1";
				if (__instance.item != null)
				{
					value = __instance.item.count.ToString();
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
