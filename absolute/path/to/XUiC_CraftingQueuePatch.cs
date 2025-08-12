using HarmonyLib;
using UnityEngine;
using System.Linq;
using Audio;

[HarmonyPatch]
public class XUiC_CraftingQueuePatch
{
    // 常量定义，替换魔法数字
    private const KeyCode LEFT_CONTROL = KeyCode.LeftControl;
    private const KeyCode RIGHT_CONTROL = KeyCode.RightControl;

    // 制作
    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_CraftingQueue), "AddRecipeToCraft")]
    public static bool AddRecipeToCraftPrefix(XUiC_CraftingQueue __instance, ref Recipe _recipe, ref int _count, ref float craftTime, ref bool isCrafting, ref float _oneItemCraftingTime, ref bool __result)
    {
        // 验证输入参数
        if (_recipe == null || _count <= 0)
        {
            __result = false;
            return false;
        }

        // 检查是否按下了修饰键
        if (Input.GetKey(LEFT_CONTROL) || Input.GetKey(RIGHT_CONTROL))
        {
            AddToStartOfQueue(__instance, ref _recipe, ref _count, ref craftTime, ref isCrafting, ref _oneItemCraftingTime, ref __result);
            return false;
        }

        // 缓存队列长度
        int queueLength = __instance.queueItems.Length;

        for (int num = queueLength - 1; num >= 0; num--)
        {
            if (__instance.AddRecipeToCraftAtIndex(num, _recipe, _count, craftTime, isCrafting, recipeModification: false, -1, -1, _oneItemCraftingTime))
            {
                __result = true;
                return false;
            }
        }

        __result = false;
        return false;
    }

    private static void AddToStartOfQueue(XUiC_CraftingQueue inst, ref Recipe _recipe, ref int _count, ref float craftTime, ref bool isCrafting, ref float _oneItemCraftingTime, ref bool __result)
    {
        __result = false;

        // 验证输入参数
        if (_recipe == null || _count <= 0)
        {
            Manager.PlayInsidePlayerHead("ui_denied");
            return;
        }

        XUiController[] queueItems = inst.queueItems;

        // 检查队列是否已满
        if (queueItems.Cast<XUiC_RecipeStack>().All(a => a.HasRecipe()))
        {
            Manager.PlayInsidePlayerHead("ui_denied");
            return;
        }

        // 移动队列项
        ShiftQueueItems(queueItems);

        // 设置新的队列项
        XUiC_RecipeStack lastItem = (XUiC_RecipeStack)queueItems[queueItems.Length - 1];
        lastItem.SetRecipe(_recipe, _count, craftTime, recipeModification: false, -1, -1, _oneItemCraftingTime);
        lastItem.IsDirty = true;

        __result = true;
    }

    // 修理&分解
    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_CraftingQueue), "AddItemToRepair")]
    public static bool AddItemToRepairPrefix(XUiC_CraftingQueue __instance, ref float _repairTimeLeft, ref ItemValue _itemToRepair, ref int _amountToRepair, ref bool __result, ref XUiController[] ___queueItems)
    {
        // 检查是否按下了修饰键
        if (!Input.GetKey(LEFT_CONTROL) && !Input.GetKey(RIGHT_CONTROL))
        {
            return true;
        }

        // 验证输入参数
        if (_itemToRepair == null || _amountToRepair <= 0)
        {
            Manager.PlayInsidePlayerHead("ui_denied");
            __result = false;
            return false;
        }

        // 检查队列是否已满
        if (___queueItems.Cast<XUiC_RecipeStack>().All(a => a.HasRecipe()))
        {
            Manager.PlayInsidePlayerHead("ui_denied");
            __result = false;
            return false;
        }

        // 移动队列项
        ShiftQueueItems(___queueItems);

        // 设置修复配方
        XUiC_RecipeStack lastItem = (XUiC_RecipeStack)___queueItems[___queueItems.Length - 1];
        if (lastItem.SetRepairRecipe(_repairTimeLeft, _itemToRepair, _amountToRepair))
        {
            lastItem.IsCrafting = true;
            lastItem.IsDirty = true;
            __result = true;
        }
        else
        {
            __result = false;
        }

        return false;
    }

    // 提取重复代码为通用方法
    private static void ShiftQueueItems(XUiController[] queueItems)
    {
        if (queueItems == null || queueItems.Length <= 1)
            return;

        // 缓存队列长度
        int queueLength = queueItems.Length;

        for (int i = 1; i < queueLength; i++)
        {
            XUiC_RecipeStack currentItem = (XUiC_RecipeStack)queueItems[i];
            if (currentItem.HasRecipe())
            {
                currentItem.IsCrafting = false;
                currentItem.CopyTo((XUiC_RecipeStack)queueItems[i - 1]);
                queueItems[i - 1].IsDirty = true;
            }
        }

        // 获取最后一个队列项
        XUiC_RecipeStack lastItem = (XUiC_RecipeStack)queueItems[queueLength - 1];
        lastItem.IsCrafting = false;
        lastItem.SetRecipe(null, 0, 0f, recipeModification: true);
    }
}