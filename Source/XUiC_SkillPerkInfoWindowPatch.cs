using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

[HarmonyPatch]
public class XUiC_SkillPerkInfoWindowPatch
{
    [HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillPerkInfoWindow), "GetBindingValue")]
	public static bool Prefix(string _bindingName, ref string _value, ref bool __result, XUiC_SkillPerkInfoWindow __instance)
	{
		switch (_bindingName)
		{
			// 当前等级 父级技能名称，只展示IsSkill
			case "CATUI_SkillGroupName":
				_value = "";
				if (__instance.CurrentSkill != null)
				{
					if (__instance.CurrentSkill.ProgressionClass.Parent.IsSkill) {
						_value = Localization.Get(__instance.CurrentSkill.ProgressionClass.Parent.NameKey);
					}
				}
				__result = true;
				return false;

			// 当前等级技能状态
			case "CATUI_MaxSkillLevel":
				_value = "0";
				if (__instance.CurrentSkill != null) {
					_value = __instance.CurrentSkill.ProgressionClass.MaxLevel.ToString();
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}
}
