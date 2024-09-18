using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_TargetBar))]
public class XUiC_TargetBarPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_TargetBar), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_TargetBar __instance)
	{
		EntityAlive Target = __instance.Target;
		switch (bindingName)
		{
			// ��ȡɥʬ����
			case "CATUI_EntityType":
				value = "normal";
				if (Target != null)
				{
					EntityClass entityClass = EntityClass.list[Target.entityClass];
					bool IsBoss = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("boss"));
					bool IsFeral = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("feral"));
					bool IsRadiated = entityClass.Tags.Test_Bit(FastTags<TagGroup.Global>.GetBit("radiated"));
					bool IsBear = entityClass.entityClassName == "animalBear";
					bool IsZombieBear = entityClass.entityClassName == "animalZombieBear";
					bool IsDireWolf = entityClass.entityClassName == "animalDireWolf";
					// ����
					if (IsRadiated) {
						value = "radiated";
					}
					// �ײ�
					else if (IsFeral)
                    {
                        value = "feral";
                    }
					// BOSS ���������ܣ�ɥʬ�ܣ�����
					else if (IsBoss || IsBear || IsZombieBear || IsDireWolf)
                    {
                        value = "boss";
                    }
				}
				__result = true;
				return false;

			// ��ȡɥʬ����
			case "CATUI_EntityTags":
				value = "";
				if (Target != null)
				{
					value = EntityClass.list[Target.entityClass].Tags.ToString();
				}
				__result = true;
				return false;

			// ɥʬ״̬ - ����ֵ
			case "CATUI_EntityArmorRating":
				value = "0";
				if (Target != null)
				{
					value = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, Target).ToString();
				}
				__result = true;
				return false;

			// ɥʬ״̬ - �Ƿ�˯��
			case "CATUI_EntityIsSleeping":
				value = "false";
				if (Target != null)
				{
					value = Target.IsSleeping.ToString();
				}
				__result = true;
				return false;

			// ɥʬ״̬ - �Ƿ���Ѫ
			case "CATUI_EntityIsBleeding":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffInjuryBleeding") != null).ToString();
				}
				__result = true;
				return false;
			// ɥʬ״̬ - ��Ѫ����
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
			// ɥʬ״̬ - ��Ѫ����ʱ
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

            // ɥʬ״̬ - �Ƿ���
            case "CATUI_EntityIsShocked":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffShocked") != null).ToString();
				}
				__result = true;
				return false;
            // ɥʬ״̬ - �������ʱ
            case "CATUI_EntityShockedTimer":
                value = "";
                if (Target != null)
                {
					BuffValue buff = Target.Buffs.GetBuff("buffShocked");
					if (buff != null)
					{
						float Timer = Target.Buffs.GetCustomVar(buff.BuffClass.DisplayValueCVar);
						// ż������ָ���
						if (Timer > 0f)
						{
							value = Timer.ToString();
						}
					}
                }
                __result = true;
                return false;

            // ɥʬ״̬ - �Ƿ��Ż�
            case "CATUI_EntityIsOnFire":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffIsOnFire") != null).ToString();
				}
				__result = true;
				return false;
            // ɥʬ״̬ - �Ż𵹼�ʱ
            case "CATUI_EntityOnFireTimer":
                value = "";
                if (Target != null)
                {
					BuffValue buff = Target.Buffs.GetBuff("buffIsOnFire");
					if (buff != null)
					{
						float Timer = Target.Buffs.GetCustomVar(buff.BuffClass.DisplayValueCVar);
						// ȼ�ռ�����ֵ���ʱΪ0�����
						if (Timer > 0f) {
							value = Timer.ToString();
						}
					}
				}
                __result = true;
                return false;

            // ɥʬ״̬ - �Ƿ��²�
            case "CATUI_EntityIsCrippled":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffInjuryCrippled01") != null).ToString();
				}
				__result = true;
				return false;

			// ɥʬ״̬ - �Ƿ��Ѫ
			case "CATUI_EntityIsRadiatedRegen":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffRadiatedRegen") != null).ToString();
				}
				__result = true;
				return false;

			// ɥʬ״̬ - �Ƿ���ֹ��Ѫ
			case "CATUI_EntityIsRadiatedRegenBlock":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffRadiatedRegenBlock") != null).ToString();
				}
				__result = true;
				return false;

			// ɥʬ״̬ - ��ֹ��Ѫ����ʱ
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

			// ɥʬ״̬ - buff�б�
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
			// ɥʬ״̬ - buff�б���ʱ
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
