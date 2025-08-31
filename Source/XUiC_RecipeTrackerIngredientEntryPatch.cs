using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_RecipeTrackerIngredientEntryPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_RecipeTrackerIngredientEntry), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string bindingName, ref string value, ref bool __result, XUiC_RecipeTrackerIngredientEntry __instance)
	{
		ItemStack ingredient = __instance.ingredient;
		bool flag = ingredient != null;
		int currentCount = __instance.currentCount;
		XUiC_RecipeTrackerIngredientsList Owner = __instance.Owner;
		switch (bindingName)
		{
			case "CATUI_IngredientCompleteColor":
				value = "255,255,255";
				if (flag)
				{
					value = ((currentCount >= ingredient.count * Owner.Count) ? Owner.completeColor : Owner.incompleteColor);
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}
}
