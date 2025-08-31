using HarmonyLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[HarmonyPatch]
public class XUiC_IngredientEntryPatch
{
	private static readonly HashSet<XUiC_IngredientEntry> _patchedInstances = new HashSet<XUiC_IngredientEntry>();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_IngredientEntry), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string bindingName, ref string value, ref bool __result, XUiC_IngredientEntry __instance)
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

			// 素材是否有配方
			case "CATUI_InventoryHasRecipe":
				value = "false";
				if (flag)
				{				
					// FilterRecipesByID 是 xUiC_RecipeList.SetRecipeDataByItem	内的方法
					List<Recipe> ingredientRecipes = XUiM_Recipes.FilterRecipesByID(__instance.ingredient.itemValue.ItemClass.Id, XUiM_Recipes.GetRecipes());
					if (ingredientRecipes != null && ingredientRecipes.Count > 0)
					{
						value = "true";
					}
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}

	[HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_IngredientEntry), "Init")]
    public static void InitPostfixProxy(XUiC_IngredientEntry __instance)
    {
		if (_patchedInstances.Contains(__instance))
			return;

		_patchedInstances.Add(__instance);
		XUiController btnInventoryRecipe = __instance.GetChildById("btnInventoryRecipe");
		if (btnInventoryRecipe == null)
		{
			return;
		}
		btnInventoryRecipe.OnPress += (XUiController _sender, int _mouseButton) =>
		{
			__instance.xui.playerUI.windowManager.CloseIfOpen("looting");
            XUiC_RecipeList xUiC_RecipeList = __instance.xui.GetChildrenByType<XUiC_RecipeList>()
                .Find(recipeList => recipeList.WindowGroup != null && recipeList.WindowGroup.isShowing);
            if (xUiC_RecipeList == null)
            {
                XUiC_WindowSelector.OpenSelectorAndWindow(__instance.xui.playerUI.entityPlayer, "crafting");
                xUiC_RecipeList = __instance.xui.GetChildByType<XUiC_RecipeList>();
            }
			xUiC_RecipeList.SetRecipeDataByItem(__instance.ingredient.itemValue.ItemClass.Id);
			__instance.isDirty = true;
        };
    }
}
