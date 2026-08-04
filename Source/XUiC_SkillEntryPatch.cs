using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_SkillEntryPatch
{
	private const string TAG = "[CATUI]";

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_SkillEntry), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string bindingName, ref string value, ref bool __result, XUiC_SkillEntry __instance)
	{
		try
		{
			return GetBindingValueInternalSafe(bindingName, ref value, ref __result, __instance);
		}
		catch (Exception ex)
		{
			// 服务器 XML 配置缺失/不完整时（例如 progression.xml 中缺少某个技能定义），
			// currentSkill.ProgressionClass 可能为 null，导致绑定求值抛异常。
			// 这里兜底：记录详细日志并返回该绑定类型的合理默认值，避免 UI 崩溃或 NCalc 报错刷屏。
			Log.Error("{0} Binding '{1}' evaluation failed on skill entry. This usually means the server's progression.xml is missing or incomplete. Skill: '{2}', hierarchy: {3}",
				TAG, bindingName, GetSkillName(__instance), GetHierarchy(__instance));
			Log.Exception(ex);
			value = GetSafeDefault(bindingName);
			__result = true;
			return false;
		}
	}

	private static bool GetBindingValueInternalSafe(string bindingName, ref string value, ref bool __result, XUiC_SkillEntry __instance)
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
				value = "false";
				if (__instance.currentSkill != null && GetMaxLevel(__instance) == 0)
				{
					value = "true";
				}
				__result = true;
				return false;

			// 当前等级 是否Buffed
			case "CATUI_SkillEntryIsBuffed":
				value = "false";
				// 非技能书和制作技能
                if (__instance.currentSkill != null && !IsBookGroup(__instance) && !IsCrafting(__instance))
                {
					// 当前技能等级(+装备后数值)
					int calculatedLevel = GetCalculatedLevel(__instance);
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
				value = "false";
				// 非技能书和制作技能
				if (__instance.currentSkill != null && !IsBookGroup(__instance) && !IsCrafting(__instance))
				{
					// 当前技能等级(+装备后数值)
					int calculatedLevel = GetCalculatedLevel(__instance);
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
				if (__instance.Skill != null && __instance.Skill.ProgressionClass != null && __instance.Skill.ProgressionClass.Parent != null)
				{
					int count = 0;
					IEnumerable<ProgressionClass> children = __instance.Skill.ProgressionClass.Parent.Children;
					if (children != null)
					{
						foreach (ProgressionClass child2 in children)
						{
							if (child2 != null && !child2.IsSkill)
							{
								count++;
							}
						}
					}
					value = (count).ToString();
				}
				__result = true;
				return false;

			// 技能分组类型 skill=普通技能，book=技能书/收集品，craft=制作技能
			case "CATUI_GroupType":
				value = "skill";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null)
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

			// 二级技能项颜色条：IsSkill 且有子技能才着色，同组内所有 IsSkill 兄弟计序，取不到用白色
			case "CATUI_GroupEntryColor":
				value = GetSkillGroupColor((__instance.currentSkill != null) ? __instance.currentSkill.ProgressionClass : null);
				__result = true;
				return false;

			// 三级技能角标：父级(二级技能)的颜色
			case "CATUI_ParentEntryColor":
				value = "255,255,255,255";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null
					&& __instance.currentSkill.ProgressionClass.IsPerk
					&& __instance.currentSkill.ProgressionClass.Parent != null)
				{
					value = GetSkillGroupColor(__instance.currentSkill.ProgressionClass.Parent);
				}
				__result = true;
				return false;

			// 三级技能角标：父级(二级技能)的图标
			case "CATUI_ParentEntryIcon":
				value = "";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null
					&& __instance.currentSkill.ProgressionClass.IsPerk
					&& __instance.currentSkill.ProgressionClass.Parent != null)
				{
					value = __instance.currentSkill.ProgressionClass.Parent.Icon ?? "";
				}
				__result = true;
				return false;

			// 三级技能角标：父级(二级技能)的名称
			case "CATUI_ParentEntryName":
				value = "";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null
					&& __instance.currentSkill.ProgressionClass.IsPerk
					&& __instance.currentSkill.ProgressionClass.Parent != null)
				{
					value = Localization.Get(__instance.currentSkill.ProgressionClass.Parent.NameKey);
				}
				__result = true;
				return false;

			// 技能分组图标
			case "CATUI_GroupIcon":
				value = "";
				if (__instance.Skill != null && __instance.Skill.ProgressionClass != null && __instance.Skill.ProgressionClass.Parent != null)
				{
					value = __instance.Skill.ProgressionClass.Parent.Icon;
				}
				__result = true;
				return false;

			// 技能类型 attribute=玩家属性，skill=技能类型（技能书和制作技能类目下=perk），perk=特性
			case "CATUI_GroupEntryType":
				value = "skill";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null)
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
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null)
				{
					// 技能书
					if (__instance.currentSkill.ProgressionClass.IsBookGroup) {
						value = GetBookGroupLevel(__instance).ToString();
					}
					// 制作技能/技能
					else
					{
						value = GetCalculatedLevel(__instance).ToString();
					}
				}
				__result = true;
				return false;

			// 技能 最大等级
			case "CATUI_GroupEntryLevelMax":
				value = "0";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null)
				{
					// 技能书
					if (__instance.currentSkill.ProgressionClass.IsBookGroup)
					{
						int num = CountChildren(__instance.currentSkill.ProgressionClass);
						value = Mathf.Max(0, num - 1).ToString();
					}
					// 制作技能/技能
					else
					{
						value = GetMaxLevel(__instance).ToString();
					}
				}
				__result = true;
				return false;

			// 技能 百分比进度Fill
			case "CATUI_GroupEntryLevelFill":
				value = "0";
				if (__instance.currentSkill != null && __instance.currentSkill.ProgressionClass != null)
				{
					// 技能书
					if (__instance.currentSkill.ProgressionClass.IsBookGroup)
					{
						float num = CountChildren(__instance.currentSkill.ProgressionClass);
						float num2 = GetBookGroupLevel(__instance);
						num2 = Mathf.Min(num2, num - 1);
						float levelPercent = (num > 1) ? (num2 / (num - 1)) : 0f;
						value = levelPercent < 0.01f ? "0" : levelPercent.ToString("F2");
					}
					// 制作技能/技能
					else
					{
						float Level = GetCalculatedLevel(__instance);
						float MaxLevel = GetMaxLevel(__instance);
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

	// ---- 安全访问辅助方法（服务器 XML 缺失/不完整时避免 NRE） ----

	private static bool IsBookGroup(XUiC_SkillEntry instance)
	{
		return instance.currentSkill != null && instance.currentSkill.ProgressionClass != null && instance.currentSkill.ProgressionClass.IsBookGroup;
	}

	private static bool IsCrafting(XUiC_SkillEntry instance)
	{
		return instance.currentSkill != null && instance.currentSkill.ProgressionClass != null && instance.currentSkill.ProgressionClass.IsCrafting;
	}

	// 计算等级（含装备加成），entityPlayer/Progression 可能为 null
	private static int GetCalculatedLevel(XUiC_SkillEntry instance)
	{
		EntityPlayerLocal entityPlayer = GetLocalPlayer(instance);
		if (instance.currentSkill == null || instance.currentSkill.ProgressionClass == null || entityPlayer == null)
		{
			return 0;
		}
		return instance.currentSkill.CalculatedLevel(entityPlayer);
	}

	private static int GetMaxLevel(XUiC_SkillEntry instance)
	{
		if (instance.currentSkill == null || instance.currentSkill.ProgressionClass == null)
		{
			return 0;
		}
		return instance.currentSkill.ProgressionClass.MaxLevel;
	}

	private static EntityPlayerLocal GetLocalPlayer(XUiC_SkillEntry instance)
	{
		try
		{
			if (instance == null || instance.xui == null || instance.xui.playerUI == null)
			{
				return null;
			}
			return instance.xui.playerUI.entityPlayer;
		}
		catch
		{
			return null;
		}
	}

	// 技能书已收集数量（含空值保护）
	private static int GetBookGroupLevel(XUiC_SkillEntry instance)
	{
		if (instance.currentSkill == null || instance.currentSkill.ProgressionClass == null)
		{
			return 0;
		}
		EntityPlayerLocal entityPlayer = GetLocalPlayer(instance);
		if (entityPlayer == null || entityPlayer.Progression == null)
		{
			return 0;
		}
		IList<ProgressionClass> children = instance.currentSkill.ProgressionClass.Children;
		if (children == null || children.Count == 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < children.Count; i++)
		{
			ProgressionClass child = children[i];
			if (child == null || string.IsNullOrEmpty(child.Name))
			{
				continue;
			}
			num++;
			ProgressionValue pv = entityPlayer.Progression.GetProgressionValue(child.Name);
			if (pv != null && pv.Level == 1)
			{
				num2++;
			}
		}
		return Mathf.Min(num2, num - 1);
	}

	private static int CountChildren(ProgressionClass progressionClass)
	{
		if (progressionClass == null || progressionClass.Children == null)
		{
			return 0;
		}
		int num = 0;
		foreach (ProgressionClass child in progressionClass.Children)
		{
			if (child != null)
			{
				num++;
			}
		}
		return num;
	}

	private static string GetSkillName(XUiC_SkillEntry instance)
	{
		try
		{
			if (instance != null && instance.currentSkill != null && instance.currentSkill.ProgressionClass != null)
			{
				return instance.currentSkill.Name;
			}
		}
		catch
		{
		}
		return "null";
	}

	private static string GetHierarchy(XUiC_SkillEntry instance)
	{
		try
		{
			if (instance != null)
			{
				return instance.GetXuiHierarchy();
			}
		}
		catch
		{
		}
		return "unknown";
	}

	// 异常时的合理默认值：数值类绑定返回 0，颜色/字符串类返回原有默认
	private static string GetSafeDefault(string bindingName)
	{
		switch (bindingName)
		{
			case "CATUI_GroupEntryCount":
			case "CATUI_GroupEntryLevel":
			case "CATUI_GroupEntryLevelMax":
			case "CATUI_GroupEntryLevelFill":
				return "0";
			case "CATUI_SkillEntryDisabled":
			case "CATUI_SkillEntryIsBuffed":
			case "CATUI_SkillEntryIsNerfed":
				return "false";
			case "rowstatecolor":
			case "CATUI_GroupEntryColor":
			case "CATUI_ParentEntryColor":
				return "255,255,255,255";
			case "CATUI_GroupType":
			case "CATUI_GroupEntryType":
				return "skill";
			default:
				return "";
		}
	}

	// 二级技能颜色：同组(同一属性)内所有 IsSkill 兄弟计序，循环取 TrackedFriendColors，取不到用白色
	private static string GetSkillGroupColor(ProgressionClass groupClass)
	{
		if (groupClass == null || !groupClass.IsSkill || groupClass.Children == null || groupClass.Children.Count == 0)
		{
			return "255,255,255,255";
		}
		ProgressionClass parent = groupClass.Parent;
		if (parent == null || parent.Children == null)
		{
			return "255,255,255,255";
		}
		int idx = 0;
		foreach (ProgressionClass child in parent.Children)
		{
			if (child == null || !child.IsSkill)
			{
				continue;
			}
			if (child == groupClass)
			{
				if (Constants.TrackedFriendColors.Length > 0)
				{
					Color32 color = Constants.TrackedFriendColors[idx % Constants.TrackedFriendColors.Length];
					return string.Format("{0},{1},{2},{3}", color.r, color.g, color.b, color.a);
				}
				return "255,255,255,255";
			}
			idx++;
		}
		return "255,255,255,255";
	}
}
