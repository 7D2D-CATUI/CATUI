using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_RecipeTrackerWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_RecipeTrackerWindow), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string bindingName, ref string value, ref bool __result, XUiC_RecipeTrackerWindow __instance)
	{
		XUiC_RecipeTrackerIngredientsList ingredientList = __instance.ingredientList;
		Recipe currentRecipe = __instance.currentRecipe;
		switch (bindingName)
		{
			case "CATUI_ListCount":
				value = "0";
				if (currentRecipe != null)
				{
					value = ingredientList.GetActiveIngredientCount().ToString();
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}
}
