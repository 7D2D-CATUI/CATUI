using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_IngredientEntryPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_IngredientEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_IngredientEntry __instance)
	{
		bool flag = __instance.ingredient != null;
		switch (bindingName)
		{
			// 是否满足素材数量要求
			case "CATUI_HasComplete":
				int havecount;
				value = "false";
				XUiC_WorkstationMaterialInputGrid childByType3 = __instance.windowGroup.Controller.GetChildByType<XUiC_WorkstationMaterialInputGrid>();
				if (!flag) {
					__result = true;
					return false;
				}
				if (childByType3 != null)
				{
					if (__instance.materialBased)
					{
						havecount = childByType3.GetWeight(__instance.material);
					}
					else
					{
						havecount = __instance.xui.PlayerInventory.GetItemCount(__instance.ingredient.itemValue);
					}
				}
				else
				{
					XUiC_WorkstationInputGrid childByType4 = __instance.windowGroup.Controller.GetChildByType<XUiC_WorkstationInputGrid>();
					if (childByType4 != null)
					{
						havecount = childByType4.GetItemCount(__instance.ingredient.itemValue);
					}
					else
					{
						havecount = __instance.xui.PlayerInventory.GetItemCount(__instance.ingredient.itemValue);
					}
				}
				int needcount = __instance.ingredient.count * __instance.craftCountControl.Count;
				value = (!(havecount < needcount)).ToString();
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
