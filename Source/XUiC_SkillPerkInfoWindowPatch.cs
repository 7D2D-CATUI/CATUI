using HarmonyLib;

[HarmonyPatch]
public class XUiC_SkillPerkInfoWindowPatch
{
	// 技能为6的时候无法翻页的bug修复
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillPerkInfoWindow), "SkillChanged")]
	public static bool SkillChangedPrefix(XUiC_SkillPerkInfoWindow __instance)
	{
		var levelEntries = __instance.levelEntries;
		int skillsPerPage = __instance.skillsPerPage;
		var pager = __instance.pager;

		if (pager == null || levelEntries == null)
			return true;

		if (__instance.CurrentSkill == null)
		{
			pager.SetLastPageByElementsAndPageLength(0, skillsPerPage);
			pager.Reset();
			__instance.IsDirty = true;
			return false;
		}

		int maxLevel = __instance.CurrentSkill.ProgressionClass.MaxLevel;
		int elementCount = (maxLevel > levelEntries.Count) ? maxLevel : 0;

		pager.SetLastPageByElementsAndPageLength(elementCount, skillsPerPage);
		pager.Reset();
		__instance.IsDirty = true;

		return false;
	}
}
