using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_TargetBarPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_TargetBar), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string bindingName, ref string value, ref bool __result, XUiC_TargetBar __instance)
	{
		EntityAlive Target = __instance.Target;
		switch (bindingName)
		{
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

			// 获取丧尸类型
			case "CATUI_EntityTags":
				value = "";
				if (Target != null)
				{
					value = EntityClass.list[Target.entityClass].Tags.ToString();
				}
				__result = true;
				return false;

			// 丧尸状态 - 护甲值
			case "CATUI_EntityArmorRating":
				value = "0";
				if (Target != null)
				{
					value = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, Target).ToString("F0");
				}
				__result = true;
				return false;

			// 丧尸状态 - 是否睡眠
			case "CATUI_EntityIsSleeping":
				value = "false";
				if (Target != null)
				{
					value = Target.IsSleeping.ToString();
				}
				__result = true;
				return false;

			// 丧尸状态 - 是否流血
			case "CATUI_EntityIsBleeding":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffInjuryBleeding") != null).ToString();
				}
				__result = true;
				return false;
			// 丧尸状态 - 流血层数
			case "CATUI_EntityBleedingCounter":
				value = "false";
				if (Target != null)
				{
					BuffValue buff = Target.Buffs.GetBuff("buffInjuryBleeding");
					if (buff != null)
					{
						float Counter = Target.Buffs.GetCustomVar("bleedCounter");
						if (Counter > 0f)
						{
							value = Counter.ToString();
						}
					}
				}
				__result = true;
				return false;
			// 丧尸状态 - 流血倒计时
			case "CATUI_EntityBleedingTimer":
                value = "";
                if (Target != null)
                {
					BuffValue buff = Target.Buffs.GetBuff("buffInjuryBleeding");
					if (buff != null) {
						float Timer = Target.Buffs.GetCustomVar(buff.BuffClass.DisplayValueCVar);
						if (Timer > 0f)
						{
							value = Timer.ToString();
						}
					}
				}
				__result = true;
                return false;

            // 丧尸状态 - 是否电击
            case "CATUI_EntityIsShocked":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffShocked") != null).ToString();
				}
				__result = true;
				return false;
            // 丧尸状态 - 电击倒计时
            case "CATUI_EntityShockedTimer":
                value = "";
                if (Target != null)
                {
					EntityClass entityClass = EntityClass.list[Target.entityClass];
					bool IsCharged = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("charged"));
					BuffValue buff = Target.Buffs.GetBuff("buffShocked");
					if (buff != null)
					{
						// 带电丧尸时间减半（If Charged zombie, reduce time again ==> buffs.xml line:6470）
						float timer = Mathf.CeilToInt((IsCharged ? buff.BuffClass.DurationMax / 2 : buff.BuffClass.DurationMax) - buff.DurationInSeconds);
						// 偶尔会出现负数
						if (timer > 0f)
						{
							value = timer.ToString();
						}
					}
                }
                __result = true;
                return false;

            // 丧尸状态 - 是否着火
            case "CATUI_EntityIsOnFire":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffIsOnFire") != null).ToString();
				}
				__result = true;
				return false;
            // 丧尸状态 - 着火倒计时
            case "CATUI_EntityOnFireTimer":
                value = "";
                if (Target != null)
                {
					BuffValue buff = Target.Buffs.GetBuff("buffIsOnFire");
					if (buff != null)
					{
						float timer = Target.Buffs.GetCustomVar(buff.BuffClass.DisplayValueCVar);
						// BUG: 燃烧箭&燃烧弩箭 会出现倒计时为0的情况(buffBurningFlamingArrow), 不知道啥原因
						if (timer > 0f) {
							value = Mathf.CeilToInt(timer).ToString();
						}
					}
				}
                __result = true;
                return false;

            // 丧尸状态 - 是否致残
            case "CATUI_EntityIsCrippled":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffInjuryCrippled01") != null).ToString();
				}
				__result = true;
				return false;

			// 丧尸状态 - 是否回血
			case "CATUI_EntityIsRadiatedRegen":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffRadiatedRegen") != null).ToString();
				}
				__result = true;
				return false;

			// 丧尸状态 - 是否阻止回血
			case "CATUI_EntityIsRadiatedRegenBlock":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffRadiatedRegenBlock") != null).ToString();
				}
				__result = true;
				return false;

			// 丧尸状态 - 阻止回血倒计时
			case "CATUI_EntityRadiatedRegenBlockTimer":
				value = "0";
				if (Target != null)
				{
					BuffValue buff = Target.Buffs.GetBuff("buffRadiatedRegenBlock");
					if (buff != null)
					{
						value = (buff.BuffClass.DurationMax - Mathf.FloorToInt(buff.DurationInSeconds)).ToString();
                    }
                }
				__result = true;
				return false;

			// 丧尸状态 - buff列表
			case "CATUI_EntityBuffList":
				value = "";
				if (Target != null)
				{
					string text = string.Empty;
					List<BuffValue> buffs = Target.Buffs.ActiveBuffs;
					for (int i = 0; i < buffs.Count; i++)
					{
						text += buffs[i].buffName;
						if (i < buffs.Count - 1)
						{
							text += ", ";
						}
					}
					value = text;
				}
				__result = true;
				return false;
			// 丧尸状态 - buff列表倒计时
			case "CATUI_EntityBuffListTimer":
				value = "";
				if (Target != null)
				{
					string text = string.Empty;
					List<BuffValue> buffs = Target.Buffs.ActiveBuffs;
					for (int i = 0; i < buffs.Count; i++)
					{
						string DisplayValueCVar = buffs[i].BuffClass.DisplayValueCVar != null ? buffs[i].BuffClass.DisplayValueCVar : "";
						text += Target.Buffs.GetCustomVar(DisplayValueCVar);
						if (i < buffs.Count - 1)
						{
							text += ", ";
						}
					}
					value = text;
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
