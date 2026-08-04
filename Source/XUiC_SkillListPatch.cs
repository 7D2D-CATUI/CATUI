using HarmonyLib;
using System;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiC_SkillListPatch
{
	private const string TAG = "[CATUI]";

	// 读取 controller="SkillList" 的 grid 的 cols 属性（运行时 XUiV_Grid.Columns），取不到默认 5
	private static int GetGridCols(XUiC_SkillList instance)
	{
		int cols = 5;
		try
		{
			if (instance != null && instance.ViewComponent is XUiV_Grid grid && grid.Columns > 0)
			{
				cols = grid.Columns;
			}
		}
		catch (Exception ex)
		{
			Log.Warning("{0} Failed to read SkillList grid cols, defaulting to 5. {1}", TAG, ex.Message);
		}
		return cols;
	}

	// 技能分级：一级=属性层，二级=IsSkill 且有子技能，三级=IsPerk；其余（书/制作/无子技能普通技能）归入普通流
	private static int GetTier(ProgressionValue pv)
	{
		if (pv == null || pv.ProgressionClass == null)
		{
			return 0;
		}
		ProgressionClass pc = pv.ProgressionClass;
		if (pc.IsAttribute)
		{
			return 1;
		}
		if (pc.IsSkill && pc.Children != null && pc.Children.Count > 0)
		{
			return 2;
		}
		if (pc.IsPerk)
		{
			return 3;
		}
		return 0;
	}

	// 在 updateFilteredList 之后，按 cols 行宽重建 currentSkills：
	// 1. 压缩掉原版属性对齐插入的 null
	// 2. 一级技能独占一行（自身 + cols-1 个空白槽）
	// 3. 二级技能强制从行首开始，其后三级技能顺序填充该行
	// 4. 其余技能顺序排满 cols 个/行
	// 5. 末尾不足 cols 补 null，保证总量为 cols 的整数倍
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_SkillList), "updateFilteredList")]
	private static void updateFilteredListPostfix(XUiC_SkillList __instance)
	{
		try
		{
			int cols = GetGridCols(__instance);
			if (cols <= 0)
			{
				cols = 5;
			}
			List<ProgressionValue> source = __instance.currentSkills;
			if (source == null)
			{
				return;
			}

			List<ProgressionValue> clean = new List<ProgressionValue>(source.Count);
			for (int i = 0; i < source.Count; i++)
			{
				if (source[i] != null)
				{
					clean.Add(source[i]);
				}
			}

			int col = 0;
			List<ProgressionValue> padded = new List<ProgressionValue>(clean.Count + 16);
			for (int j = 0; j < clean.Count; j++)
			{
				int tier = GetTier(clean[j]);
				// 一级/二级均强制换行：补满当前行后从行首开始
				if (tier == 1 || tier == 2)
				{
					while (col % cols != 0)
					{
						padded.Add(null);
						col++;
					}
					padded.Add(clean[j]);
					col = (col + 1) % cols;
					// 一级独占一行：补齐本行剩余空白
					if (tier == 1)
					{
						while (col % cols != 0)
						{
							padded.Add(null);
							col++;
						}
					}
				}
				else
				{
					padded.Add(clean[j]);
					col = (col + 1) % cols;
				}
			}
			while (col % cols != 0)
			{
				padded.Add(null);
				col++;
			}

			__instance.currentSkills = padded;

			if (__instance.pagingControl != null && __instance.skillEntries != null && __instance.skillEntries.Length > 0)
			{
				__instance.pagingControl.SetLastPageByElementsAndPageLength(padded.Count, __instance.skillEntries.Length);
			}

			FixSelectNamePage(__instance, padded);
		}
		catch (Exception ex)
		{
			Log.Error("{0} SkillList updateFilteredList postfix failed. {1}", TAG, ex);
		}
	}

	// 原版在填充前用未填充索引 SetPage，这里按填充后列表重定位 selectName 的页
	private static void FixSelectNamePage(XUiC_SkillList instance, List<ProgressionValue> padded)
	{
		if (string.IsNullOrEmpty(instance.selectName) || instance.pagingControl == null)
		{
			return;
		}
		int pageLength = (instance.skillEntries != null && instance.skillEntries.Length > 0) ? instance.skillEntries.Length : 1;
		for (int i = 0; i < padded.Count; i++)
		{
			if (padded[i] != null && padded[i].Name == instance.selectName)
			{
				instance.pagingControl.SetPage(i / pageLength);
				return;
			}
		}
	}

	// 总技能数统计非空白槽，避免补全的 null 撑大数量显示
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_SkillList), "GetActiveCount")]
	private static void GetActiveCountPostfix(XUiC_SkillList __instance, ref int __result)
	{
		try
		{
			List<ProgressionValue> list = __instance.currentSkills;
			if (list == null)
			{
				__result = 0;
				return;
			}
			int count = 0;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != null)
				{
					count++;
				}
			}
			__result = count;
		}
		catch (Exception ex)
		{
			Log.Error("{0} SkillList GetActiveCount postfix failed. {1}", TAG, ex);
		}
	}

	// 页首若为空白槽，原版兜底会取 skillEntries[0]（Skill 可能为 null），改为选中第一个非空条目
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_SkillList), "listSkills")]
	private static void listSkillsPostfix(XUiC_SkillList __instance)
	{
		try
		{
			if (__instance.SelectedEntry != null && __instance.SelectedEntry.Skill != null)
			{
				return;
			}
			XUiC_SkillEntry[] entries = __instance.skillEntries;
			if (entries == null)
			{
				return;
			}
			XUiC_SkillEntry first = null;
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i] != null && entries[i].Skill != null)
				{
					first = entries[i];
					break;
				}
			}
			if (first == null)
			{
				return;
			}
			__instance.SelectedEntry = first;
			first.IsSelected = true;
			first.RefreshBindings();
			if (__instance.WindowGroup != null && __instance.WindowGroup.Controller is XUiC_SkillWindowGroup windowGroup)
			{
				windowGroup.CurrentSkill = first.Skill;
				windowGroup.IsDirty = true;
			}
			__instance.selectName = "";
		}
		catch (Exception ex)
		{
			Log.Error("{0} SkillList listSkills postfix failed. {1}", TAG, ex);
		}
	}
}
