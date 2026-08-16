using HarmonyLib;
using System.Reflection;
using UnityEngine;

[HarmonyPatch]
public class XUiC_RecipeStackPatch
{
    private static FieldInfo _craftingTimeLeftField;
    private static FieldInfo _oneItemCraftTimeField;
    private static FieldInfo _isCraftingField;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_RecipeStack), "updateRecipeData")]
    public static void UpdateRecipeDataPostfix(XUiC_RecipeStack __instance)
    {
        if (_craftingTimeLeftField == null)
        {
            var type = typeof(XUiC_RecipeStack);
            _craftingTimeLeftField = type.GetField("craftingTimeLeft", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _oneItemCraftTimeField = type.GetField("oneItemCraftTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _isCraftingField = type.GetField("isCrafting", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        bool isCrafting = (bool)_isCraftingField.GetValue(__instance);

        // CATUI_timerFill: 单物品制作进度条
        XUiController timerFillCtrl = __instance.GetChildById("CATUI_timerFill");
        if (timerFillCtrl != null)
        {
            XUiV_Sprite timerFillSprite = timerFillCtrl.ViewComponent as XUiV_Sprite;
            if (timerFillSprite != null)
            {
                if (!isCrafting)
                {
                    timerFillSprite.Fill = 0f;
                }
                else
                {
                    float oneItemCraftTime = (float)_oneItemCraftTimeField.GetValue(__instance);
                    float craftingTimeLeft = (float)_craftingTimeLeftField.GetValue(__instance);
                    if (oneItemCraftTime <= 0f)
                    {
                        timerFillSprite.Fill = 0f;
                    }
                    else
                    {
                        timerFillSprite.Fill = 1f - (craftingTimeLeft / oneItemCraftTime);
                    }
                }
            }
        }

        // CATUI_backgroundIcon: 队列进行中时隐藏，与lockIcon相反
        XUiController bgIconCtrl = __instance.GetChildById("CATUI_backgroundIcon");
        if (bgIconCtrl != null)
        {
            bgIconCtrl.ViewComponent.IsVisible = !isCrafting;
        }
    }
}