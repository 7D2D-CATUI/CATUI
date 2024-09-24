using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

[HarmonyPatch]
public class XUiC_SkillBookLevelPatch
{
    [HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillBookLevel), "GetBindingValue")]
	public static bool Prefix(string _bindingName, ref string _value, ref bool __result, XUiC_SkillBookLevel __instance)
	{
		bool flag = __instance.CurrentSkill != null && __instance.perk != null;
		EntityPlayerLocal entityPlayer = __instance.xui.playerUI.entityPlayer;
		bool flag2 = false;
		if (flag)
		{
			flag2 = __instance.perk != null && __instance.perk.Level > 0;
		}
		switch (_bindingName)
		{
			// 当前等级技能状态
			case "CATUI_SkillStat":
				// 未学习
				_value = "notbuy";
				// 已学习
				if (flag2)
				{
					_value = "bought";
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
