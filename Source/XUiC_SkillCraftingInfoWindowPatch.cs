using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_SkillCraftingInfoWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillCraftingInfoWindow), "UpdateSkill")]
	public static bool UpdateSkill_Prefix(XUiC_SkillCraftingInfoWindow __instance)
	{
		// Patch变量名重新赋值
		ProgressionValue CurrentSkill = __instance.CurrentSkill;
		XUiC_ItemActionList actionItemList = __instance.actionItemList;
		int skillsPerPage = __instance.skillsPerPage;
		XUiC_Paging pager = __instance.pager;
		XUiWindowGroup windowGroup = __instance.windowGroup;
		List<XUiC_SkillCraftingInfoEntry> levelEntries = __instance.levelEntries;

		if (CurrentSkill != null && actionItemList != null)
		{
			actionItemList.SetCraftingActionList(XUiC_ItemActionList.ItemActionListTypes.Skill, __instance);
		}
		int num = (pager?.GetPage() ?? 0) * skillsPerPage;
		ProgressionClass progressionClass = ((CurrentSkill != null) ? CurrentSkill.ProgressionClass : null);
		if (progressionClass != null && progressionClass.DisplayDataList != null)
		{
			XUiC_SkillEntry entryForSkill = windowGroup.Controller.GetChildByType<XUiC_SkillList>().GetEntryForSkill(CurrentSkill);
			{
                // 重新构建 progressionClass.DisplayDataList
                List<ProgressionClass.DisplayData> newDisplayDataList = new();
                for (int i = 0; i < progressionClass.DisplayDataList.Count; i++)
                {
					// UnlockDataList为空说明没有需要解锁的物品，不执行
					if (progressionClass.DisplayDataList[i].UnlockDataList != null)
                    {
						// 将UnlockDataList（分类下的解锁物品列表）数据设置到DisplayDataList上
						for (int j = 0; j < progressionClass.DisplayDataList[i].UnlockDataList.Count; j++)
						{
							ProgressionClass.DisplayData originalDisplayData = progressionClass.DisplayDataList[i];
							ProgressionClass.DisplayData.UnlockData unlockData = originalDisplayData.UnlockDataList[j];
							// 新DisplayData赋值
							ProgressionClass.DisplayData newDisplayData = new ProgressionClass.DisplayData();
							newDisplayData.CustomHasQuality = originalDisplayData.CustomHasQuality;
							newDisplayData.CustomIcon = originalDisplayData.CustomIcon;
							newDisplayData.CustomIconTint = originalDisplayData.CustomIconTint;
							newDisplayData.item = originalDisplayData.item;
							newDisplayData.Owner = originalDisplayData.Owner;
							newDisplayData.QualityStarts = originalDisplayData.QualityStarts;
							// UnlockDataList只保留1条，避免混淆
							newDisplayData.UnlockDataList = new List<ProgressionClass.DisplayData.UnlockData>();
							newDisplayData.UnlockDataList.Add(unlockData);
							// 将unlockData.ItemName设置为DisplayData.ItemName（基础数据，查询解锁等级icon等数据需要）
							newDisplayData.ItemName = unlockData.ItemName;
							newDisplayDataList.Add(newDisplayData);
						}
					}
                }

				foreach (XUiC_SkillCraftingInfoEntry levelEntry in levelEntries)
				{
					// 应用新DisplayDataList
					ProgressionClass.DisplayData data = (newDisplayDataList.Count > num) ? newDisplayDataList[num] : null;
					levelEntry.Data = data;
					levelEntry.IsDirty = true;
					if (entryForSkill != null)
					{
						levelEntry.ViewComponent.NavLeftTarget = entryForSkill.ViewComponent;
					}
					num++;
				}
				return false;
			}
		}
		foreach (XUiC_SkillCraftingInfoEntry levelEntry2 in levelEntries)
		{
			levelEntry2.Data = null;
			levelEntry2.IsDirty = true;
		}

		return false;
	}

}
