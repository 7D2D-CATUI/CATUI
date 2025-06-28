using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

[HarmonyPatch(typeof(XUiC_SkillCraftingInfoEntry))]
public class XUiC_SkillCraftingInfoEntryPatch
{
    [HarmonyPrefix]
	[HarmonyPatch("GetBindingValue")]
	public static bool Prefix(string _bindingName, ref string _value, ref bool __result, XUiC_SkillCraftingInfoEntry __instance)
	{
		bool flag = __instance.data != null;
		EntityPlayerLocal entityPlayer = __instance.xui.playerUI.entityPlayer;
		ProgressionClass.DisplayData data = __instance.data;
        switch (_bindingName)
		{
            // 复写 物品是否已解锁
            case "showlock":
                _value = "false";
                if (flag)
                {
                    _value = data.GetUnlockItemLocked(entityPlayer, 0).ToString();
                }
                __result = true;
                return false;

            // 复写 图片atlas路径
            case "iconatlas":
                _value = "ItemIconAtlas";
                if (flag)
                {
                    _value = data.GetUnlockItemIconAtlas(entityPlayer, 0).ToString();
                }
                __result = true;
                return false;

            // 当前制作技能等级
            case "CATUI_SkillLevel":
                _value = "";
                if (flag)
                {
                    _value = entityPlayer.Progression.GetProgressionValue(data.Owner.Name).Level.ToString();
                }
                __result = true;
                return false;

            // 物品解锁所需制作技能等级
            case "CATUI_ItemUnlockLevel":
                _value = "";
                if (flag)
                {
                    ProgressionClass.DisplayData.UnlockData unlockData = data.GetUnlockData(0);
                    if (unlockData != null)
                    {
                        _value = data.QualityStarts[unlockData.UnlockTier].ToString();
                    }
                }
                __result = true;
                return false;

            // 物品等阶 - 下一级进度条
            case "CATUI_QualityNextLevelFill":
                _value = "0";
                if (flag)
                {
                    float SkillLevel = entityPlayer.Progression.GetProgressionValue(data.Owner.Name).Level;
                    float QualityNextLevel = data.GetNextPoints(entityPlayer.Progression.GetProgressionValue(data.Owner.Name).Level);
                    // 满级后QualityNextLevel为0
                    if (QualityNextLevel > 0)
                    {
                        float percent = SkillLevel / QualityNextLevel;
                        _value = percent < 0.01f ? "0" : percent.ToString("F3");
                    }
                }
                __result = true;
                return false;

            default:
				return true;
		}
	}

/*    [HarmonyPatch("Init")]
    [HarmonyPostfix]
    private static void InitPostfixProxy(XUiC_SkillCraftingInfoEntry __instance)
    {
        Debug.Log($"<color=#00FF00>InitPostfixProxy ----------------- </color>");

        bool flag = __instance.Data != null;
        EntityPlayerLocal entityPlayer = __instance.xui.playerUI.entityPlayer;
        string _value = (flag ? __instance.Data.GetName(entityPlayer.Progression.GetProgressionValue(__instance.Data.Owner.Name).Level) : "");
        if (flag) {
            Debug.Log($"<color=#00FF00>Data.CustomName: {__instance.Data.GetIcon(1)}</color>");
        }
    }*/

    /* public void Image_OnPress(XUiC_SkillCraftingInfoEntry __instance, int _mouseButton)
     {
         ProgressionClass.DisplayData CurrentData = __instance.Data;
         XUi xUi = __instance.xui;
         if (CurrentData == null || CurrentData.GetUnlockItemRecipes(0) == null)
         {
             return;
         }
         xUi.playerUI.windowManager.CloseIfOpen("looting");
         List<XUiC_RecipeList> childrenByType = xUi.GetChildrenByType<XUiC_RecipeList>();
         XUiC_RecipeList xUiC_RecipeList = null;
         for (int i = 0; i < childrenByType.Count; i++)
         {
             if (childrenByType[i].WindowGroup != null && childrenByType[i].WindowGroup.isShowing)
             {
                 xUiC_RecipeList = childrenByType[i];
                 break;
             }
         }
         if (xUiC_RecipeList == null)
         {
             XUiC_WindowSelector.OpenSelectorAndWindow(xUi.playerUI.entityPlayer, "crafting");
             xUiC_RecipeList = xUi.GetChildByType<XUiC_RecipeList>();
         }
         xUiC_RecipeList?.SetRecipeDataByItems(CurrentData.GetUnlockItemRecipes(0));
     }*/

}
