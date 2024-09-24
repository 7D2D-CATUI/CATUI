using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_SkillEntryPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_SkillEntry __instance)
	{
		switch (bindingName)
		{
			// 复写 当前skill rowstatecolor
			case "rowstatecolor":
				value = (__instance.IsSelected ? "160,160,160,255" : (__instance.IsHovered ? __instance.hoverColor : __instance.rowColor));
				__result = true;
				return false;

			// 当前skill 是否Disabled
			case "CATUI_SkillEntryDisabled":
				value = ((__instance.currentSkill == null) ? "true" : ((__instance.currentSkill.CalculatedMaxLevel(__instance.xui.playerUI.entityPlayer) == 0) ? "true" : "false"));
				__result = true;
				return false;

			// 当前等级 是否Buffed
			case "CATUI_SkillEntryIsBuffed":
				value = "fasle";
				// 非技能书和制作技能
                if (__instance.currentSkill != null && !(__instance.currentSkill.ProgressionClass.IsBookGroup || __instance.currentSkill.ProgressionClass.IsCrafting))
                {
					// 当前技能等级(+装备后数值)
					int calculatedLevel = __instance.currentSkill.CalculatedLevel(__instance.xui.playerUI.entityPlayer);
					// 实际技能等级
					int Level = __instance.currentSkill.Level;
					if (calculatedLevel > Level) {
						value = "true";
					}
				}

				__result = true;
				return false;

			// 当前skill 是否Nerfed
			case "CATUI_SkillEntryIsNerfed":
				value = "fasle";
				// 非技能书和制作技能
				if (__instance.currentSkill != null && !(__instance.currentSkill.ProgressionClass.IsBookGroup || __instance.currentSkill.ProgressionClass.IsCrafting))
				{
					// 当前技能等级(+装备后数值)
					int calculatedLevel = __instance.currentSkill.CalculatedLevel(__instance.xui.playerUI.entityPlayer);
					// 实际技能等级
					int Level = __instance.currentSkill.Level;
					if (calculatedLevel < Level)
					{
						value = "true";
					}
				}
				__result = true;
				return false;	

			// 当前skill group下perk数量
			case "CATUI_GroupEntryCount":
				value = "0";
				if (__instance.Skill != null && __instance.Skill.ProgressionClass.Parent != null)
				{
					int count = 0;
					foreach (ProgressionClass child2 in __instance.Skill.ProgressionClass.Parent.Children)
					{
						if (!child2.IsSkill)
						{
							count++;
						}
					}
					value = (count).ToString();
				}
				__result = true;
				return false;

			// 技能分组类型 skill=普通技能，book=技能书/收集品，craft=制作技能
			case "CATUI_GroupType":
				value = "skill";
				if (__instance.currentSkill != null)
				{
					ProgressionClass entryClass = __instance.currentSkill.ProgressionClass;
					if (entryClass.IsCrafting) {
						value = "craft";
					}
					else if (entryClass.IsBookGroup)
					{
						value = "book";
					}
				}
				__result = true;
				return false;

			// 技能分组图标
			case "CATUI_GroupIcon":
				value = "";
				if (__instance.Skill != null && __instance.Skill.ProgressionClass.Parent != null)
				{
					value = __instance.Skill.ProgressionClass.Parent.Icon;
				}
				__result = true;
				return false;

			// 技能类型 attribute=玩家属性，skill=技能类型（技能书和制作技能类目下=perk），perk=特性
			case "CATUI_GroupEntryType":
				value = "skill";
				if (__instance.currentSkill != null)
				{
					ProgressionClass entryClass = __instance.currentSkill.ProgressionClass;
					if (entryClass.IsPerk)
					{
						value = "perk";
					}
					else if (entryClass.IsAttribute)
					{
						value = "attribute";
					}
				}
				__result = true;
				return false;

			// 技能 当前等级
			case "CATUI_GroupEntryLevel":
				value = "0";
				if (__instance.currentSkill != null)
				{
					// 技能书
					if (__instance.currentSkill.ProgressionClass.IsBookGroup) {
						int num = 0;
						int num2 = 0;
						for (int i = 0; i < __instance.currentSkill.ProgressionClass.Children.Count; i++)
						{
							num++;
							if (__instance.xui.playerUI.entityPlayer.Progression.GetProgressionValue(__instance.currentSkill.ProgressionClass.Children[i].Name).Level == 1)
							{
								num2++;
							}
						}
						num2 = Mathf.Min(num2, num - 1);
						value = num2.ToString();
					}
					// 制作技能/技能
					else
					{
						value = __instance.currentSkill.CalculatedLevel(__instance.xui.playerUI.entityPlayer).ToString();
					}
				}
				__result = true;
				return false;

			// 技能 最大等级
			case "CATUI_GroupEntryLevelMax":
				value = "0";
				if (__instance.currentSkill != null)
				{
					// 技能书
					if (__instance.currentSkill.ProgressionClass.IsBookGroup)
					{
						int num = 0;
						for (int i = 0; i < __instance.currentSkill.ProgressionClass.Children.Count; i++)
						{
							num++;
						}
						value = (num - 1).ToString();
					}
					// 制作技能/技能
					else
					{
						value = __instance.currentSkill.ProgressionClass.MaxLevel.ToString();
					}
				}
				__result = true;
				return false;

			// 技能 百分比进度Fill
			case "CATUI_GroupEntryLevelFill":
				value = "0";
				if (__instance.currentSkill != null)
				{
					// 技能书
					if (__instance.currentSkill.ProgressionClass.IsBookGroup)
					{
						float num = 0;
						float num2 = 0;
						for (int i = 0; i < __instance.currentSkill.ProgressionClass.Children.Count; i++)
						{
							num++;
							if (__instance.xui.playerUI.entityPlayer.Progression.GetProgressionValue(__instance.currentSkill.ProgressionClass.Children[i].Name).Level == 1)
							{
								num2++;
							}
						}
						num2 = Mathf.Min(num2, num - 1);
						float levelPercent = num2 / (num - 1);
						value = levelPercent < 0.01f ? "0" : levelPercent.ToString("F2");
					}
					// 制作技能/技能
					else
					{
						float Level = __instance.currentSkill.CalculatedLevel(__instance.xui.playerUI.entityPlayer);
						float MaxLevel = __instance.currentSkill.ProgressionClass.MaxLevel;
						// MaxLevel在某些模组内会出现为0的情况
						if (MaxLevel == 0) {
							value = "1";
						} else {
							float levelPercent = Level / MaxLevel;
							value = levelPercent < 0.01f ? "0" : levelPercent.ToString("F2");
						}
					}
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
