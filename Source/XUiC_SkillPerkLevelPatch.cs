using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

[HarmonyPatch]
public class XUiC_SkillPerkLevelPatch
{
    [HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillPerkLevel), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_SkillPerkLevel __instance)
	{
		int level = __instance.level;
		bool flag = __instance.CurrentSkill != null && __instance.CurrentSkill.ProgressionClass.MaxLevel >= __instance.level;
		EntityPlayerLocal entityPlayer = __instance.xui.playerUI.entityPlayer;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		if (flag)
		{
			flag3 = __instance.CurrentSkill.Level >= __instance.level;
			flag2 = __instance.CurrentSkill.Level + 1 == __instance.level && __instance.CurrentSkill.Level + 1 <= __instance.CurrentSkill.CalculatedMaxLevel(entityPlayer);
			flag4 = !flag3 && __instance.CurrentSkill.CalculatedLevel(entityPlayer) >= __instance.level;
			flag5 = flag3 && __instance.CurrentSkill.CalculatedLevel(entityPlayer) < __instance.level;
		}
		switch (_bindingName)
		{
			// 复写 是否展示购买按钮（默认技能消耗为0的情况也展示了，会出现展示NA的情况）
			case "buyvisible":
				_value = flag.ToString();
				if (__instance.CurrentSkill != null)
				{
					int num2 = __instance.CurrentSkill.ProgressionClass.CalculatedCostForLevel(__instance.level);
					if (num2 <= 0)
					{
						_value = "false";

					}
				}
				__result = true;
				return false;

			// 当前等级 是否Buffed
			case "CATUI_IsBuffed":
				_value = "fasle";
				if (__instance.CurrentSkill != null && flag4)
				{
					_value = "true";
				}
				__result = true;
				return false;

			// 当前等级 是否Nerfed
			case "CATUI_IsNerfed":
				_value = "fasle";
                if (__instance.CurrentSkill != null && flag3 && flag5)
                {
					_value = "true";
				}
				__result = true;
				return false;

			// 当前等级 消费技能点
			case "CATUI_BuyCost":
				_value = "0";
				if (__instance.CurrentSkill != null)
				{
					int num = __instance.CurrentSkill.ProgressionClass.CalculatedCostForLevel(level);
					_value = num.ToString();
				}
				__result = true;
				return false;

			// 当前等级技能状态
			case "CATUI_SkillStat":
				// 未学习
				_value = "notbuy";
				// 已学习
				if (flag3)
				{
					_value = "bought";
				}
				// 可学习
				else if (flag2)
				{
					_value = "buy";
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
