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
			// »ñÈ¡É¥Ê¬ÀàÐÍ
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
					// ·øÉä
					if (IsRadiated) {
						value = "radiated";
					}
					// Ð×²Ð
					else if (IsFeral)
                    {
                        value = "feral";
                    }
					// BOSS ÖíÍõ£¬´óÐÜ£¬É¥Ê¬ÐÜ£¬¿ÖÀÇ
					else if (IsBoss || IsBear || IsZombieBear || IsDireWolf)
                    {
                        value = "boss";
                    }
				}
				__result = true;
				return false;

			// »ñÈ¡É¥Ê¬ÀàÐÍ
			case "CATUI_EntityTags":
				value = "";
				if (Target != null)
				{
					value = EntityClass.list[Target.entityClass].Tags.ToString();
				}
				__result = true;
				return false;

			// É¥Ê¬×´Ì¬ - »¤¼×Öµ
			case "CATUI_EntityArmorRating":
				value = "0";
				if (Target != null)
				{
					value = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, Target).ToString();
				}
				__result = true;
				return false;

			// É¥Ê¬×´Ì¬ - ÊÇ·ñË¯Ãß
			case "CATUI_EntityIsSleeping":
				value = "false";
				if (Target != null)
				{
					value = Target.IsSleeping.ToString();
				}
				__result = true;
				return false;

			// É¥Ê¬×´Ì¬ - ÊÇ·ñÁ÷Ñª
			case "CATUI_EntityIsBleeding":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffInjuryBleeding") != null).ToString();
				}
				__result = true;
				return false;
			// É¥Ê¬×´Ì¬ - Á÷Ñª²ãÊý
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
			// É¥Ê¬×´Ì¬ - Á÷Ñªµ¹¼ÆÊ±
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

            // É¥Ê¬×´Ì¬ - ÊÇ·ñµç»÷
            case "CATUI_EntityIsShocked":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffShocked") != null).ToString();
				}
				__result = true;
				return false;
            // É¥Ê¬×´Ì¬ - µç»÷µ¹¼ÆÊ±
            case "CATUI_EntityShockedTimer":
                value = "";
                if (Target != null)
                {
					BuffValue buff = Target.Buffs.GetBuff("buffShocked");
					if (buff != null)
					{
						float Timer = Target.Buffs.GetCustomVar(buff.BuffClass.DisplayValueCVar);
						// Å¼¶û»á³öÏÖ¸ºÊý
						if (Timer > 0f)
						{
							value = Timer.ToString();
						}
					}
                }
                __result = true;
                return false;

            // É¥Ê¬×´Ì¬ - ÊÇ·ñ×Å»ð
            case "CATUI_EntityIsOnFire":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffIsOnFire") != null).ToString();
				}
				__result = true;
				return false;
            // É¥Ê¬×´Ì¬ - ×Å»ðµ¹¼ÆÊ±
            case "CATUI_EntityOnFireTimer":
                value = "";
                if (Target != null)
                {
					BuffValue buff = Target.Buffs.GetBuff("buffIsOnFire");
					if (buff != null)
					{
						float Timer = Target.Buffs.GetCustomVar(buff.BuffClass.DisplayValueCVar);
						// È¼ÉÕ¼ý»á³öÏÖµ¹¼ÆÊ±Îª0µÄÇé¿ö
						if (Timer > 0f) {
							value = Timer.ToString();
						}
					}
				}
                __result = true;
                return false;

            // É¥Ê¬×´Ì¬ - ÊÇ·ñÖÂ²Ð
            case "CATUI_EntityIsCrippled":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffInjuryCrippled01") != null).ToString();
				}
				__result = true;
				return false;

			// É¥Ê¬×´Ì¬ - ÊÇ·ñ»ØÑª
			case "CATUI_EntityIsRadiatedRegen":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffRadiatedRegen") != null).ToString();
				}
				__result = true;
				return false;

			// É¥Ê¬×´Ì¬ - ÊÇ·ñ×èÖ¹»ØÑª
			case "CATUI_EntityIsRadiatedRegenBlock":
				value = "false";
				if (Target != null)
				{
					value = (Target.Buffs.GetBuff("buffRadiatedRegenBlock") != null).ToString();
				}
				__result = true;
				return false;

			// É¥Ê¬×´Ì¬ - ×èÖ¹»ØÑªµ¹¼ÆÊ±
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

			// É¥Ê¬×´Ì¬ - buffÁÐ±í
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
			// É¥Ê¬×´Ì¬ - buffÁÐ±íµ¹¼ÆÊ±
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
