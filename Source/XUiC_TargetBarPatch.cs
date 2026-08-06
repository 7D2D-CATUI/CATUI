using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using TargetBuffs;

[HarmonyPatch]
public class XUiC_TargetBarPatch
{
	// 非 buff 类绑定（直接由本补丁处理，不走控制器）
	private static readonly HashSet<string> NonBuffBindings = new HashSet<string>
	{
		"CATUI_fillCurrent",
		"CATUI_EntityType",
		"CATUI_EntityTags",
	};

	/// <summary>
	/// 目标切换后同步到控制器。挂在 XUiC_TargetBar.Update 上，
	/// 在每次目标变化/刷新后重算全部 buff 状态。
	/// </summary>
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_TargetBar), "Update")]
	public static void UpdatePostfix(XUiC_TargetBar __instance)
	{
		if (__instance == null)
		{
			return;
		}
		TargetBuffController.SetTarget(__instance.Target);
		TargetBuffController.Refresh();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_TargetBar), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string bindingName, ref string value, ref bool __result, XUiC_TargetBar __instance)
	{
		EntityAlive Target = __instance.Target;

		// 非 buff 绑定：本补丁自行处理
		if (NonBuffBindings.Contains(bindingName))
		{
			switch (bindingName)
			{
				// 丧尸血量百分比（0-1），瞬时值，与 fill 绑定最终显示值一致（*1.01）
				case "CATUI_fillCurrent":
					value = "0";
					if (Target != null && Target.IsAlive())
					{
						float healthPercent = (float)Target.Health / (float)Target.GetMaxHealth();
						value = (healthPercent * 1.02f).ToString("F7");
					}
					__result = true;
					return false;

				// 获取丧尸类型
				case "CATUI_EntityType":
					value = "normal";
					if (Target != null)
					{
						EntityClass entityClass = EntityClass.list[Target.entityClass];
						bool IsBoss = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("boss"));
						bool IsFeral = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("feral"));
						bool IsRadiated = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("radiated"));
						bool IsCharged = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("charged"));
						bool IsInfernal = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("infernal"));
						bool IsBear = entityClass.entityClassName == "animalBear";
						bool IsZombieBear = entityClass.entityClassName == "animalZombieBear";
						bool IsDireWolf = entityClass.entityClassName == "animalDireWolf";
						// 辐射
						if (IsRadiated) {
							value = "radiated";
						}
						// 带电
						else if(IsCharged)
						{
							value = "charged";
						}
						// 炼狱
						else if(IsInfernal)
						{
							value = "infernal";
						}
						// 凶残
						else if (IsFeral)
						{
							value = "feral";
						}
						// BOSS 猪王，大熊，丧尸熊，恐狼
						else if (IsBoss || IsBear || IsZombieBear || IsDireWolf)
						{
							value = "boss";
						}
					}
					__result = true;
					return false;

				// 获取丧尸类型（原始标签串）
				case "CATUI_EntityTags":
					value = "";
					if (Target != null)
					{
						value = EntityClass.list[Target.entityClass].Tags.ToString();
					}
					__result = true;
					return false;
			}
		}

		// 其余绑定：委托给 TargetBuffController
		if (TargetBuffController.HasBinding(bindingName))
		{
			value = TargetBuffController.GetBindingValue(bindingName);
			__result = true;
			return false;
		}

		// 未识别：交给原版处理
		return true;
	}
}
