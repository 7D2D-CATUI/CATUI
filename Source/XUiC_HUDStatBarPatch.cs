using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Text;

[HarmonyPatch(typeof(XUiC_HUDStatBar))]
public class XUiC_HUDStatBarPatch
{
	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterXuiRgbaColor rgbaColorFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterInt playerStatCurrentHealthMaxFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterInt playerStatCurrentStaminaMaxFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterFloat playerEntityPenetrationCountFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterInt playerArmorRatingFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterFloat playerRunSpeedFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterInt playerCurrencyAmountFormatter = new();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_HUDStatBar __instance)
	{
		switch (bindingName)
		{
			// ʳ���ˮȡ���ֵ
			case "CATUI_statCurrentWithMax":
				value = "100/100";
				if (__instance.LocalPlayer != null)
				{
					if (__instance.statType == HUDStatTypes.Food) {
						int Max = Mathf.RoundToInt(__instance.LocalPlayer.Stats.Food.Max);
						int Value = Mathf.RoundToInt(__instance.LocalPlayer.Stats.Food.Value);
						value = Value.ToString() + "/" + Max.ToString();
					}
					else if (__instance.statType == HUDStatTypes.Water) {
						int Max = Mathf.RoundToInt(__instance.LocalPlayer.Stats.Water.Max);
						int Value = Mathf.RoundToInt(__instance.LocalPlayer.Stats.Water.Value);
						value = Value.ToString() + "/" + Max.ToString();
					}
				}
				__result = true;
				return false;
			// ��ɫ����
			case "CATUI_playerName":
				value = " ";
				if (__instance.LocalPlayer != null)
				{
					value = __instance.LocalPlayer.PlayerDisplayName;
				}
				__result = true;
				return false;

			// ��ҩ���ֵ
			case "CATUI_AmmoMax":
				value = "";
				if (__instance.LocalPlayer != null)
				{
					ItemActionAttack attackAction = __instance.attackAction;
					int currentAmmoCount = __instance.currentAmmoCount;
					int currentSlotIndex = __instance.currentSlotIndex;
					EntityPlayerLocal LocalPlayer = __instance.LocalPlayer;
					if (attackAction != null && attackAction.IsEditingTool())
					{
						ItemActionData itemActionDataInSlot = LocalPlayer.inventory.GetItemActionDataInSlot(currentSlotIndex, 1);
						value = attackAction.GetStat(itemActionDataInSlot);
					}
					else
					{
						value = currentAmmoCount.ToString();
					}
				}
				__result = true;
				return false;

			// �������� - �������ֵ
			case "CATUI_playerHealthMax":
				value = "100";
				if (__instance.LocalPlayer != null)
				{
					value = playerStatCurrentHealthMaxFormatter.Format((int)__instance.LocalPlayer.Stats.Health.Max).ToString();
				}
				__result = true;
				return false;

			// �������� - �������ֵ
			case "CATUI_playerStaminaMax":
				value = "100";
				if (__instance.LocalPlayer != null)
				{
					value = playerStatCurrentStaminaMaxFormatter.Format((int)__instance.LocalPlayer.Stats.Stamina.Max).ToString();
				}
				__result = true;
				return false;

			// �������� - ���ʱ��
			case "CATUI_playerCurrentLife":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = XUiM_Player.GetCurrentLife(__instance.LocalPlayer).ToString();
				}
				__result = true;
				return false;

			// �������� - ���׵ȼ�
			case "CATUI_playerArmorRating":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = playerArmorRatingFormatter.Format((int)EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, __instance.LocalPlayer)).ToString();
				}
				__result = true;
				return false;

			// �������� - ���׵ȼ� - ����
			case "CATUI_playerArmorLevel":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					string playerArmorRating = playerArmorRatingFormatter.Format((int)EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, __instance.LocalPlayer));
					int Armor = int.Parse(playerArmorRating);
					value = Armor switch
					{
						0 => "0",
						> 0 and < 20 => "1",
						>= 20 and < 40 => "2",
						>= 40 and < 60 => "3",
						>= 60 and < 80 => "4",
						>= 80 and < 100 => "5",
						_ => "6"
					};
				}
				__result = true;
				return false;

			// �������� - ����ȼ�
			case "CATUI_playerGameStage":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = __instance.LocalPlayer.gameStage.ToString();
				}
				__result = true;
				return false;

			// �������� - �ѹεȼ�
			case "CATUI_playerLootStage":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = __instance.LocalPlayer.GetHighestPartyLootStage(0f, 0f).ToString();
				}
				__result = true;
				return false;

			// �������� - ���˵ȼ�
			case "CATUI_playerTraderStage":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = __instance.LocalPlayer.QuestJournal.GetCurrentFactionTier(1).ToString();
				}
				__result = true;
				return false;

			// �������� - ���˵ȼ� ���� ��ǰֵ
			case "CATUI_playerTraderStageProgressCurrent":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = __instance.LocalPlayer.QuestJournal.GetQuestFactionPoints(1).ToString();
				}
				__result = true;
				return false;

			// �������� - ���˵ȼ� ���� ���ֵ
			case "CATUI_playerTraderStageProgressMax":
				value = "10";
				if (__instance.LocalPlayer != null)
				{
					int currentFactionTier = __instance.LocalPlayer.QuestJournal.GetCurrentFactionTier(1);
					value = __instance.LocalPlayer.QuestJournal.GetQuestFactionMax(1, currentFactionTier).ToString();
				}
				__result = true;
				return false;

			// �������� - ���о���
			case "CATUI_playerTraveled":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = XUiM_Player.GetKMTraveled(__instance.LocalPlayer).ToString();
				}
				__result = true;
				return false;

			// �������� - ��ɱɥʬ
			case "CATUI_playerZombieKills":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = XUiM_Player.GetZombieKills(__instance.LocalPlayer).ToString();
				}
				__result = true;
				return false;

			// ����״̬ - ping
			case "CATUI_playerPing":
				value = "-1";
				if (__instance.LocalPlayer != null)
				{
					int _ping = __instance.LocalPlayer.pingToServer;
					if (_ping > 0)
					{
						value = _ping > 1000 ? ">1000" : _ping.ToString();
					}
				}
				__result = true;
				return false;

			// ����״̬ - ��ɫ
			case "CATUI_playerPingColor":
				value = "0,0,0";
				if (__instance.LocalPlayer != null)
				{
					int _ping = __instance.LocalPlayer.pingToServer;
					const string GoodColor = "67, 207, 124";
					const string MediumColor = "255, 195, 0";
					const string PoorColor = "255, 0, 0";
					if (_ping > 0)
					{
						// ��������
						if (_ping <= 150)
						{
							value = GoodColor;
						}
						// ����һ��
						else if (_ping <= 500)
						{
							value = MediumColor;
						}
						// ����ϲ�
						else
						{
							value = PoorColor;
						}
					}
				}
				__result = true;
				return false;

			// ����״̬ - �Ƿ�չʾ
			case "CATUI_playerPingVisible":
				value = "false";
				if (__instance.LocalPlayer != null)
				{
					int _ping = __instance.LocalPlayer.pingToServer;
					if (_ping > 0)
					{
						value = "true";
					}
				}
				__result = true;
				return false;

			// �������� - �ƶ��ٶ�
			case "CATUI_playerMoveSpeed":
				value = "100";
				if (__instance.LocalPlayer != null)
				{
					float num = EffectManager.GetValue(PassiveEffects.Mobility, null, 0f, __instance.LocalPlayer, null, XUiM_Player.playerFastTags, calcEquipment: true, calcHoldingItem: true, calcProgression: true, calcBuffs: true, calcChallenges: true, 1, useMods: true, _useDurability: true) * 100f;
					value = ((int)num).ToString();
				}
				__result = true;
				return false;
			// �������� - �ƶ��ٶȵȼ�
			case "CATUI_playerMoveSpeedLevel":
				value = "4";
				if (__instance.LocalPlayer != null)
				{
					float num = EffectManager.GetValue(PassiveEffects.Mobility, null, 0f, __instance.LocalPlayer, null, XUiM_Player.playerFastTags, calcEquipment: true, calcHoldingItem: true, calcProgression: true, calcBuffs: true, calcChallenges: true, 1, useMods: true, _useDurability: true) * 100f;
					value = ((int)num).ToString();
					int speed = (int)num;
					value = speed switch
					{
						>= 0 and < 50 => "0",
						>= 50 and < 70 => "1",
						>= 70 and < 80 => "2",
						>= 80 and < 90 => "3",
						>= 100 and < 110 => "4",
						>= 110 and < 120 => "5",
						_ => "6"
					};
				}
				__result = true;
				return false;

			// �������� - �����ٶ�
			case "CATUI_playerRunSpeed":
				value = "110";
				if (__instance.LocalPlayer != null)
				{
					float num = (float)EffectManager.GetValue(PassiveEffects.RunSpeed, null, 0f, __instance.LocalPlayer) * 100f;
					value = ((int)num).ToString();
				}
				__result = true;
				return false;

			// �������� - �����Ż�
			case "CATUI_playerBarteringBuying":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					float num = (float)EffectManager.GetValue(PassiveEffects.BarteringBuying, null, 0f, __instance.LocalPlayer) * 100f;
					value = ((int)num).ToString();
				}
				__result = true;
				return false;

			// �������� - �����Ż�
			case "CATUI_playerBarteringSelling":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					float num = (float)EffectManager.GetValue(PassiveEffects.BarteringSelling, null, 0f, __instance.LocalPlayer) * 100f;
					value = ((int)num).ToString();
				}
				__result = true;
				return false;

			// ��ǰ�ֳ����� - ͼ��
			case "CATUI_playerActiveItemIcon":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					EntityPlayer localPlayer = __instance.LocalPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					ItemClass itemClass = itemValue.ItemClass;
					if (itemClass != null) {
						value = itemClass.GetIconName();
					}
				}
				__result = true;
				return false;

			// ��ǰ�ֳ����� - ����
			case "CATUI_playerActiveItemName":
				value = "";
				if (__instance.LocalPlayer != null)
				{
					EntityPlayer localPlayer = __instance.LocalPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					ItemClass itemClass = itemValue.ItemClass;
					if (itemClass != null)
					{
						value = itemClass.GetLocalizedItemName();
					}
				}
				__result = true;
				return false;

			// ��ǰ�ֳ����� - Ʒ��
			case "CATUI_playerActiveItemDurabilityColor":
				value = "255,255,255";
				if (__instance.LocalPlayer != null)
				{
					EntityPlayer localPlayer = __instance.LocalPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					if (itemValue != null)
					{
						Color32 v = QualityInfo.GetQualityColor(itemValue.Quality);
						value = rgbaColorFormatter.Format(v); ;
					}
				}
				__result = true;
				return false;

			// ��ǰ�ֳ����� - �;� ʣ��ֵ
			case "CATUI_playerActiveItemUseTimesResidue":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					EntityPlayer localPlayer = __instance.LocalPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemStack itemStack = inventory.GetItem(__instance.currentSlotIndex);
					if (itemStack.IsEmpty())
					{
						value = "0";
					}
					else
					{
						if (itemStack.itemValue.MaxUseTimes == 0)
						{
							value = "1";
						}
						else
						{
							value = (itemStack.itemValue.MaxUseTimes - itemStack.itemValue.UseTimes).ToString("F0");
						}
					}
				}
				__result = true;
				return false;
			// ��ǰ�ֳ����� - �;� ���ֵ
			case "CATUI_playerActiveItemUseTimesMax":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					EntityPlayer localPlayer = __instance.LocalPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemStack itemStack = inventory.GetItem(__instance.currentSlotIndex);
					if (itemStack.IsEmpty())
					{
						value = "0";
					}
					else
					{
						if (itemStack.itemValue.MaxUseTimes == 0)
						{
							value = "1";
						}
						else
						{
							value = itemStack.itemValue.MaxUseTimes.ToString("F0");
						}
					}
				}
				__result = true;
				return false;

			// �������� - Ǳ���˺��ӳ�
			//case "CATUI_playerEntityDamageBonus":
			//    value = "0";
			//    if (__instance.LocalPlayer != null)
			//    {
			//        float num = (float)EffectManager.GetValue(PassiveEffects.DamageBonus, null, 0f, __instance.LocalPlayer);
			//        value = num.ToString();
			//    }
			//    __result = true;
			//    return false;

			// ���� - ��ʹ�ü��ܵ�
			case "CATUI_playerSkillPointsAvailable":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = __instance.LocalPlayer.Progression.SkillPoints.ToString();
				}
				__result = true;
				return false;

			// �������� - Ŀ�괩͸
			case "CATUI_playerEntityPenetrationCount":
				value = "1";
				if (__instance.LocalPlayer != null)
				{
					value = playerEntityPenetrationCountFormatter.Format((float)EffectManager.GetValue(PassiveEffects.EntityPenetrationCount, null, 0f, __instance.LocalPlayer));
				}
				__result = true;
				return false;

			// �ؾ� - ͼ��
			case "CATUI_VehicleIcon":
				value = "";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.GetMapIcon();
				}
				__result = true;
				return false;
			// �ؾ� - ��ǰ�ٶȣ���/�룩
			case "CATUI_VehicleCurrentSpeed":
				value = "0";
				if (__instance.Vehicle != null)
				{
					float currentSpeed = Mathf.Abs(__instance.Vehicle.GetVehicle().CurrentForwardVelocity + 0.001f);
					value = currentSpeed < 0.01f ? "0" : currentSpeed.ToString("F2");
				}
				__result = true;
				return false;
			// �ؾ� - ��ǰ�ٶȣ��ٷֱȣ�
			case "CATUI_VehicleCurrentSpeedFill":
				value = "0";
				if (__instance.Vehicle != null)
				{
					Vehicle __Vehicle = __instance.Vehicle.GetVehicle();
					// �ؾ�����ٶ�
					float MaxTurboSpeed = __Vehicle.VelocityMaxTurboForward;
					// �Ƿ����������
					bool hasEnginePart = __Vehicle.HasEnginePart();
					// ������� ����ϵ��
					float MaxSpeedPer = __Vehicle.EffectVelocityMaxPer;
					// ��ǰ����ٶ�
					float MaxSpeed = hasEnginePart ? MaxTurboSpeed * MaxSpeedPer : MaxTurboSpeed;
					// ��ǰ�ٶ�
					float currentSpeed = Mathf.Abs(__Vehicle.CurrentForwardVelocity + 0.001f);
					// ���㵱ǰ�ٶȰٷֱ�
					float SpeedPercent = currentSpeed / MaxSpeed;
					value = SpeedPercent < 0.01f ? "0" : SpeedPercent.ToString("F3");
				}
				__result = true;
				return false;
			// �ؾ� - ��ǰ�ٶȣ�����/Сʱ��
			case "CATUI_VehicleCurrentSpeedKPH":
				value = "0";
				if (__instance.Vehicle != null)
				{
					float currentSpeed = Mathf.Abs(__instance.Vehicle.GetVehicle().CurrentForwardVelocity + 0.001f);
					value = currentSpeed < 0.01f ? "0" : (currentSpeed * 3.6f).ToString("F1");
				}
				__result = true;
				return false;
			// �ؾ� - δ���� ����ٶȣ���/�룩
			case "CATUI_VehicleMaxSpeedNotTurbo":
				value = "0";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.GetVehicle().VelocityMaxForward.ToString();
				}
				__result = true;
				return false;
			// �ؾ� - ����ٶȣ���/�룩
			case "CATUI_VehicleMaxSpeed":
				value = "0";
				if (__instance.Vehicle != null)
				{
					// �ؾ�����ٶ�
					float MaxTurboSpeed = __instance.Vehicle.GetVehicle().VelocityMaxTurboForward;
					// �Ƿ����������
					bool hasEnginePart = __instance.Vehicle.GetVehicle().HasEnginePart();
					// ������� ����ϵ��
					float MaxSpeedPer = __instance.Vehicle.GetVehicle().EffectVelocityMaxPer;
					value = (hasEnginePart ? MaxTurboSpeed * MaxSpeedPer : MaxTurboSpeed).ToString("0.00");
				}
				__result = true;
				return false;
			// �ؾ� - ɲ��
			case "CATUI_VehicleIsBrake":
				value = "false";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.GetVehicle().CurrentIsBreak.ToString();
				}
				__result = true;
				return false;
			// �ؾ� - ����������
			case "CATUI_VehicleInventorySlotCount":
				value = "false";
				if (__instance.Vehicle != null && __instance.Vehicle.GetVehicle().HasStorage())
				{
					value = __instance.Vehicle.bag.GetSlots().Length.ToString();
				}
				__result = true;
				return false;
			// �ؾ� - �����ʹ������
			case "CATUI_VehicleInventoryItemCount":
				value = "false";
				if (__instance.Vehicle != null && __instance.Vehicle.GetVehicle().HasStorage())
				{
					value = __instance.Vehicle.bag.GetUsedSlotCount().ToString();
				}
				__result = true;
				return false;
			// �ؾ� - �ܷ����
			case "CATUI_VehicleCanTurbo":
				value = "false";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.GetVehicle().CanTurbo.ToString();
				}
				__result = true;
				return false;
			// �ؾ� - �Ƿ����
			case "CATUI_VehicleIsTurbo":
				value = "false";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.GetVehicle().IsTurbo.ToString();
				}
				__result = true;
				return false;
			// �ؾ� - �Ƿ�����
			case "CATUI_VehicleHasHorn":
				value = "false";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.GetVehicle().HasHorn().ToString();
				}
				__result = true;
				return false;
			// �ؾ� - �Ƿ��д��
			case "CATUI_VehicleHasLight":
				value = "false";
				if (__instance.Vehicle != null)
				{
					value = __instance.Vehicle.HasHeadlight().ToString();
				}
				__result = true;
				return false;
			// �ؾ� - �Ƿ�򿪴��
			case "CATUI_VehicleIsLight":
				value = "false";
				if (__instance.Vehicle != null)
				{
					value = (__instance.Vehicle.GetVehicle().FindPart("headlight") as VPHeadlight)?.IsOn().ToString();
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "Update")]

	public static void Prefix(XUiC_HUDStatBar __instance)
	{
		if (__instance.Vehicle != null)
		{
			__instance.RefreshBindings(_forceAll: true);
		}
	}
}
