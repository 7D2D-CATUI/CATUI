using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_RecipeTrackerIngredientEntry))]
public class XUiC_RecipeTrackerIngredientEntryPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_RecipeTrackerIngredientEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_RecipeTrackerIngredientEntry __instance)
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
