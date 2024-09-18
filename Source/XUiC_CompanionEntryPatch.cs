using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[HarmonyPatch]
public class XUiC_CompanionEntryPatch
{
	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterXuiRgbaColor arrowcolorFormatter = new CachedStringFormatterXuiRgbaColor();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CompanionEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_CompanionEntry __instance)
	{
		switch (bindingName)
		{
			// 队友 索引
			case "CATUI_Index":
				value = "0";
				if (__instance.Companion != null)
				{
					int num = __instance.xui.playerUI.entityPlayer.Companions.IndexOf(__instance.Companion);
					value = (num + 1).ToString();
				}
				__result = true;
				return false;

			// 队友 数量
			case "CATUI_ListLength":
				value = "0";
				if (__instance.Companion != null)
				{
					int num = __instance.xui.playerUI.entityPlayer.Companions.Count;
					value = num.ToString();
				}
				__result = true;
				return false;

            default:
				return true;
		}
	}
}
