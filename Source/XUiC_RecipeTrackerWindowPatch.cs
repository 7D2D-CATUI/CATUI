using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_RecipeTrackerWindow))]
public class XUiC_RecipeTrackerWindowPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_RecipeTrackerWindow), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_RecipeTrackerWindow __instance)
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
