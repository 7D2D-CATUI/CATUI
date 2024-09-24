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
			// ��ǰ�ȼ� �����������ƣ�ֻչʾIsSkill
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

			// ��ǰ�ȼ�����״̬
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
