using HarmonyLib;

[HarmonyPatch]
public class XUiC_SkillPerkInfoWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillPerkInfoWindow), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_SkillPerkInfoWindow __instance)
	{
		switch (_bindingName)
		{
			// 当前等级 父级技能名称，只展示IsSkill
			case "CATUI_SkillGroupName":
				_value = "";
				if (__instance.CurrentSkill != null)
				{
					if (__instance.CurrentSkill.ProgressionClass.Parent.IsSkill)
					{
						_value = Localization.Get(__instance.CurrentSkill.ProgressionClass.Parent.NameKey);
					}
				}
				__result = true;
				return false;

			// 当前等级技能状态
			case "CATUI_MaxSkillLevel":
				_value = "0";
				if (__instance.CurrentSkill != null)
				{
					_value = __instance.CurrentSkill.ProgressionClass.MaxLevel.ToString();
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}

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
