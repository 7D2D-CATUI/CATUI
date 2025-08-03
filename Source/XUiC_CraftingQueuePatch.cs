using HarmonyLib;
using UnityEngine;
using System.Linq;

using Audio;

[HarmonyPatch]
public class XUiC_CraftingQueuePatch
{
	// 制作
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_CraftingQueue), "AddRecipeToCraft")]
	public static bool AddRecipeToCraftPrefix(XUiC_CraftingQueue __instance, ref Recipe _recipe, ref int _count, ref float craftTime, ref bool isCrafting, ref float _oneItemCraftingTime, ref bool __result)
	{
		if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303))
		{
			AddToStartOfQueue(__instance, ref _recipe, ref _count, ref craftTime, ref isCrafting, ref _oneItemCraftingTime, ref __result);
			return false;
		}
		for (int num = __instance.queueItems.Length - 1; num >= 0; num--)
		{
			if (__instance.AddRecipeToCraftAtIndex(num, _recipe, _count, craftTime, isCrafting, recipeModification: false, -1, -1, _oneItemCraftingTime))
			{
				__result = true;
				return false;
			}
		}
		return false;
	}

	private static void AddToStartOfQueue(XUiC_CraftingQueue inst, ref Recipe _recipe, ref int _count, ref float craftTime, ref bool isCrafting, ref float _oneItemCraftingTime, ref bool __result)
	{
		__result = false;
		if (inst.queueItems.Cast<XUiC_RecipeStack>().All((XUiC_RecipeStack a) => a.HasRecipe()))
		{
			Manager.PlayInsidePlayerHead("ui_denied");
			return;
		}
		for (int i = 1; i < inst.queueItems.Length; i++)
		{
			XUiC_RecipeStack xUiC_RecipeStack = (XUiC_RecipeStack)inst.queueItems[i];
			if (xUiC_RecipeStack.HasRecipe())
			{
				xUiC_RecipeStack.IsCrafting = false;
				xUiC_RecipeStack.CopyTo((XUiC_RecipeStack)inst.queueItems[i - 1]);
				xUiC_RecipeStack.IsDirty = true;
			}
		}
		XUiController[] queueItems = inst.queueItems;
		XUiC_RecipeStack obj = (XUiC_RecipeStack)queueItems[queueItems.Length - 1];
		obj.SetRecipe(null, 0, 0f, recipeModification: true);
		obj.IsCrafting = false;
		obj.SetRecipe(_recipe, _count, craftTime, recipeModification: false, -1, -1, _oneItemCraftingTime);
		obj.IsDirty = true;
		__result = true;
	}

	//修理&分解
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_CraftingQueue), "AddItemToRepair")]
	public static bool AddItemToRepairPrefix(XUiC_CraftingQueue __instance, ref float _repairTimeLeft, ref ItemValue _itemToRepair, ref int _amountToRepair, ref bool __result, ref XUiController[] ___queueItems)
	{
		if (!Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303))
		{
			return true;
		}
		if (___queueItems.Cast<XUiC_RecipeStack>().All((XUiC_RecipeStack a) => a.HasRecipe()))
		{
			Manager.PlayInsidePlayerHead("ui_denied");
			return false;
		}
		for (int i = 1; i < ___queueItems.Length; i++)
		{
			XUiC_RecipeStack xUiC_RecipeStack = (XUiC_RecipeStack)___queueItems[i];
			if (xUiC_RecipeStack.HasRecipe())
			{
				xUiC_RecipeStack.IsCrafting = false;
				xUiC_RecipeStack.CopyTo((XUiC_RecipeStack)___queueItems[i - 1]);
				___queueItems[i - 1].IsDirty = true;
			}
		}
		XUiController[] obj = ___queueItems;
		XUiC_RecipeStack xUiC_RecipeStack2 = (XUiC_RecipeStack)obj[obj.Length - 1];
		xUiC_RecipeStack2.IsCrafting = false;
		xUiC_RecipeStack2.SetRecipe(null, 0, 0f, recipeModification: true);
		if (xUiC_RecipeStack2.SetRepairRecipe(_repairTimeLeft, _itemToRepair, _amountToRepair))
		{
			xUiC_RecipeStack2.IsCrafting = true;
			xUiC_RecipeStack2.IsDirty = true;
			__result = true;
			return false;
		}
		__result = false;
		return false;
	}
}
