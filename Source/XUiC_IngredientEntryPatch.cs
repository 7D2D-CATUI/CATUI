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
			__instance.xui.playerUI.windowManager.Close("looting");
            XUiC_RecipeList xUiC_RecipeList = __instance.xui.GetChildrenByType<XUiC_RecipeList>()
                .Find(recipeList => recipeList.WindowGroup != null && recipeList.WindowGroup.isShowing);
            if (xUiC_RecipeList == null)
            {
                XUiC_WindowSelector.OpenSelectorAndWindow(__instance.xui.playerUI.entityPlayer, "crafting");
                xUiC_RecipeList = __instance.xui.GetChildByType<XUiC_RecipeList>();
            }
			xUiC_RecipeList.SetRecipeDataByItem(__instance.ingredient.itemValue.ItemClass.Id);
			__instance.IsDirty = true;
		};
    }

    // 清理已销毁的实例
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiController), "OnClose")]
    public static void OnClosePostfix(XUiController __instance)
    {
        if (__instance is XUiC_IngredientEntry entry)
        {
            _patchedInstances.Remove(entry);
        }
    }
}
