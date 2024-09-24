using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_SkillListPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillList), "updateFilteredList")]
	public static bool Prefix(XUiC_SkillList __instance)
	{
		__instance.currentSkills.Clear();
		string text = __instance.Category.Trim();
		bool flag = __instance.filterText != "";
		foreach (ProgressionValue skill in __instance.skills)
		{
			ProgressionClass progressionClass = skill?.ProgressionClass;
			if (progressionClass == null || !progressionClass.ValidDisplay(__instance.DisplayType) || progressionClass.Name == null || progressionClass.IsBook || (flag && !progressionClass.NameKey.ContainsCaseInsensitive(__instance.filterText) && !Localization.Get(progressionClass.NameKey).ContainsCaseInsensitive(__instance.filterText)))
			{
				continue;
			}
			if (text == "" || text.EqualsCaseInsensitive(progressionClass.Name))
			{
				__instance.currentSkills.Add(skill);
				continue;
			}
			ProgressionClass progressionClass2 = progressionClass.Parent;
			// 修改的部分
			bool isDifferentClass = progressionClass2 != null && progressionClass2 != progressionClass;
			bool condition1 = progressionClass.IsCrafting ||(progressionClass.IsSkill && progressionClass.Parent.Name == "LearnByUseName");
			bool condition2 = text.EqualsCaseInsensitive(progressionClass.Parent.Name);
			bool condition3 = progressionClass.IsPerk || progressionClass.IsBookGroup;
			bool condition4 = text.EqualsCaseInsensitive(progressionClass.Parent.Parent.Name);

			if (isDifferentClass && (condition1 && condition2 || condition3 && condition4))
			{
				__instance.currentSkills.Add(skill);
			}
		}
		__instance.currentSkills.Sort(ProgressionClass.ListSortOrderComparer.Instance);
		if (__instance.filterText == "")
		{
			for (int i = 0; i < __instance.currentSkills.Count; i++)
			{
				if (__instance.currentSkills[i].ProgressionClass.IsAttribute)
				{
					for (; i % __instance.skillEntries.Length != 0; i++)
					{
						__instance.currentSkills.Insert(i, null);
					}
				}
			}
		}
		__instance.pagingControl?.SetLastPageByElementsAndPageLength(__instance.currentSkills.Count, __instance.skillEntries.Length);
		if (string.IsNullOrEmpty(__instance.selectName))
		{
			return false;
		}
		for (int j = 0; j < __instance.currentSkills.Count; j++)
		{
			if (__instance.currentSkills[j].Name == __instance.selectName)
			{
				__instance.pagingControl?.SetPage(j / __instance.skillEntries.Length);
				break;
			}
		}

		return false;
	}
}
