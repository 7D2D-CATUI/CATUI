using HarmonyLib;
using UnityEngine;
using System;
using System.Collections;
using System.Text;

[HarmonyPatch]
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
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_HUDStatBar __instance)
	{
		switch (_bindingName)
		{
			// 角色名称
			case "CATUI_playerName":
				_value = " ";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.PlayerDisplayName;
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 弹药最大值
			case "CATUI_AmmoMax":
				_value = "";
				if (__instance.localPlayer != null)
				{
					ItemActionAttack attackAction = __instance.attackAction;
					int currentAmmoCount = __instance.currentAmmoCount;
					int currentSlotIndex = __instance.currentSlotIndex;
					EntityPlayerLocal LocalPlayer = __instance.localPlayer;
					if (attackAction != null && attackAction.IsEditingTool())
					{
						ItemActionData itemActionDataInSlot = LocalPlayer.inventory.GetItemActionDataInSlot(currentSlotIndex, 1);
						_value = attackAction.GetStat(itemActionDataInSlot);
					}
					else
					{
						_value = currentAmmoCount.ToString();
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 存活时间
			case "CATUI_playerCurrentLife":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetCurrentLife(__instance.localPlayer).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 护甲等级
			case "CATUI_playerArmorRating":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = playerArmorRatingFormatter.Format((int)EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, __instance.localPlayer)).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 护甲等级 - 区间
			case "CATUI_playerArmorLevel":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					string playerArmorRating = playerArmorRatingFormatter.Format((int)EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, __instance.localPlayer));
					int Armor = int.Parse(playerArmorRating);
					_value = Armor switch
					{
						0 => "0",
						> 0 and < 20 => "1",
						>= 20 and < 40 => "2",
						>= 40 and < 60 => "3",
						>= 60 and < 80 => "4",
						>= 80 and < 100 => "5",
						_ => "6"
					};
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 世界等级
			case "CATUI_playerGameStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.gameStage.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 搜刮等级
			case "CATUI_playerLootStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.GetLootStage(0f, 0f).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 商人等级
			case "CATUI_playerTraderStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.QuestJournal.GetCurrentFactionTier(1).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 商人等级 进度 当前值
			case "CATUI_playerTraderStageProgressCurrent":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.QuestJournal.GetQuestFactionPoints(1).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 商人等级 进度 最大值
			case "CATUI_playerTraderStageProgressMax":
				_value = "10";
				if (__instance.localPlayer != null)
				{
					int currentFactionTier = __instance.localPlayer.QuestJournal.GetCurrentFactionTier(1);
					_value = __instance.localPlayer.QuestJournal.GetQuestFactionMax(1, currentFactionTier).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 旅行距离
			case "CATUI_playerTraveled":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetKMTraveled(__instance.localPlayer).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 击杀丧尸
			case "CATUI_playerZombieKills":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetZombieKills(__instance.localPlayer).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 温度 - 体感
			case "CATUI_coretemp":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetCoreTemp(__instance.localPlayer).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 温度 - 体感 颜色
			case "CATUI_coretempcolor":
				_value = "255,255,255";
				if (__instance.localPlayer != null)
				{
					float coretemp = __instance.localPlayer.Buffs.GetCustomVar("_coretemp");
					_value = coretemp switch
					{
						<= 32f => "0,153,255",
						> 32f and <= 50f => "0,255,255",
						>= 85f and < 100f => "255,128,0",
						>= 100f => "255,0,0",
						_ => "255,255,255"
					};
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 温度 - 室外
			case "CATUI_outsidetemp":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetOutsideTemp(__instance.localPlayer).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 温度 - 室外 颜色
			case "CATUI_outsidetempcolor":
				_value = "255,255,255";
				if (__instance.localPlayer != null)
				{
					float outsidetemp = __instance.localPlayer.Buffs.GetCustomVar("_outsidetemp");
					_value = outsidetemp switch
					{
						<= 32f => "0,153,255",
						> 32f and <= 50f => "0,255,255",
						>= 85f and < 100f => "255,128,0",
						>= 100f => "255,0,0",
						_ => "255,255,255"
					};
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 网络状态 - ping
			case "CATUI_playerPing":
				_value = "-1";
				if (__instance.localPlayer != null)
				{
					int _ping = __instance.localPlayer.pingToServer;
					if (_ping > 0)
					{
						_value = _ping > 1000 ? ">1000" : _ping.ToString();
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 网络状态 - 颜色
			case "CATUI_playerPingColor":
				_value = "0,0,0";
				if (__instance.localPlayer != null)
				{
					int _ping = __instance.localPlayer.pingToServer;
					const string GoodColor = "67, 207, 124";
					const string MediumColor = "255, 195, 0";
					const string PoorColor = "255, 0, 0";
					if (_ping > 0)
					{
						// 网络良好
						if (_ping <= 150)
						{
							_value = GoodColor;
						}
						// 网络一般
						else if (_ping <= 500)
						{
							_value = MediumColor;
						}
						// 网络较差
						else
						{
							_value = PoorColor;
						}
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 网络状态 - 是否展示
			case "CATUI_playerPingVisible":
				_value = "false";
				if (__instance.localPlayer != null)
				{
					int _ping = __instance.localPlayer.pingToServer;
					if (_ping > 0)
					{
						_value = "true";
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 移动速度
			case "CATUI_playerMoveSpeed":
				_value = "100";
                if (__instance.localPlayer != null)
                {
                    float num = EffectManager.GetValue(PassiveEffects.Mobility, null, 0f, __instance.localPlayer, null, XUiM_Player.GetPlayer().generalTags, calcEquipment: true, calcHoldingItem: true, calcProgression: true, calcBuffs: true, calcChallenges: true, 1, useMods: true, _useDurability: true) * 100f;
                    _value = ((int)num).ToString();
					__instance.IsDirty = true;
				}
                __result = true;
				return false;
			// 人物属性 - 移动速度等级
			case "CATUI_playerMoveSpeedLevel":
				_value = "4";
                if (__instance.localPlayer != null)
                {
                    float speed = EffectManager.GetValue(PassiveEffects.Mobility, null, 0f, __instance.localPlayer, null, XUiM_Player.GetPlayer().generalTags, calcEquipment: true, calcHoldingItem: true, calcProgression: true, calcBuffs: true, calcChallenges: true, 1, useMods: true, _useDurability: true) * 100f;
                    _value = speed switch
                    {
                        >= 0 and < 50 => "0",
                        >= 50 and < 70 => "1",
                        >= 70 and < 80 => "2",
                        >= 80 and < 90 => "3",
                        >= 100 and < 110 => "4",
                        >= 110 and < 120 => "5",
                        _ => "6"
                    };
					__instance.IsDirty = true;
				}
                __result = true;
				return false;

			// 人物属性 - 奔跑速度
			case "CATUI_playerRunSpeed":
				_value = "110";
				if (__instance.localPlayer != null)
				{
					float num = (float)EffectManager.GetValue(PassiveEffects.RunSpeed, null, 0f, __instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 购物优惠
			case "CATUI_playerBarteringBuying":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float num = (float)EffectManager.GetValue(PassiveEffects.BarteringBuying, null, 0f, __instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 出售优惠
			case "CATUI_playerBarteringSelling":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float num = (float)EffectManager.GetValue(PassiveEffects.BarteringSelling, null, 0f, __instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 当前手持武器 - 图标
			case "CATUI_playerActiveItemIcon":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					ItemClass itemClass = itemValue.ItemClass;
					if (itemClass != null) {
						_value = itemClass.GetIconName();
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 当前手持武器 - 名称
			case "CATUI_playerActiveItemName":
				_value = "";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					ItemClass itemClass = itemValue.ItemClass;
					if (itemClass != null)
					{
						_value = itemClass.GetLocalizedItemName();
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 当前手持武器 - 品质
			case "CATUI_playerActiveItemDurabilityColor":
				_value = "255,255,255";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					if (itemValue != null)
					{
						Color32 v = QualityInfo.GetQualityColor(itemValue.Quality);
						_value = rgbaColorFormatter.Format(v); ;
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 当前手持武器 - 耐久 剩余值
			case "CATUI_playerActiveItemUseTimesResidue":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemStack itemStack = inventory.GetItem(__instance.currentSlotIndex);
					if (itemStack.IsEmpty())
					{
						_value = "0";
					}
					else
					{
						if (itemStack.itemValue.MaxUseTimes == 0)
						{
							_value = "1";
						}
						else
						{
							_value = (itemStack.itemValue.MaxUseTimes - itemStack.itemValue.UseTimes).ToString("F0");
						}
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 当前手持武器 - 耐久 最大值
			case "CATUI_playerActiveItemUseTimesMax":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemStack itemStack = inventory.GetItem(__instance.currentSlotIndex);
					if (itemStack.IsEmpty())
					{
						_value = "0";
					}
					else
					{
						if (itemStack.itemValue.MaxUseTimes == 0)
						{
							_value = "1";
						}
						else
						{
							_value = itemStack.itemValue.MaxUseTimes.ToString("F0");
						}
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 潜行伤害加成
			//case "CATUI_playerEntityDamageBonus":
			//    _value = "0";
			//    if (__instance.localPlayer != null)
			//    {
			//        float num = (float)EffectManager.GetValue(PassiveEffects.DamageBonus, null, 0f, __instance.localPlayer);
			//        _value = num.ToString();
			//    }
			//    __result = true;
			//    return false;

			// 人物 - 待使用技能点
			case "CATUI_playerSkillPointsAvailable":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.Progression.SkillPoints.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 人物属性 - 目标穿透
			case "CATUI_playerEntityPenetrationCount":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = playerEntityPenetrationCountFormatter.Format((float)EffectManager.GetValue(PassiveEffects.EntityPenetrationCount, null, 0f, __instance.localPlayer));
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 载具 - 图标
			case "CATUI_VehicleIcon":
				_value = "";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetMapIcon();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 当前速度（米/秒）
			case "CATUI_VehicleCurrentSpeed":
				_value = "0";
				if (__instance.vehicle != null)
				{
					float currentSpeed = Mathf.Abs(__instance.vehicle.GetVehicle().CurrentForwardVelocity + 0.001f);
					_value = currentSpeed < 0.01f ? "0" : currentSpeed.ToString("F2");
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 当前速度（百分比）
			case "CATUI_VehicleCurrentSpeedFill":
				_value = "0";
				if (__instance.vehicle != null)
				{
					Vehicle __Vehicle = __instance.vehicle.GetVehicle();
					// 载具最大速度
					float MaxTurboSpeed = __Vehicle.VelocityMaxTurboForward;
					// 是否有引擎组件
					bool hasEnginePart = __Vehicle.HasEnginePart();
					// 引擎组件 加速系数
					float MaxSpeedPer = __Vehicle.EffectVelocityMaxPer;
					// 当前最大速度
					float MaxSpeed = hasEnginePart ? MaxTurboSpeed * MaxSpeedPer : MaxTurboSpeed;
					// 当前速度
					float currentSpeed = Mathf.Abs(__Vehicle.CurrentForwardVelocity + 0.001f);
					// 计算当前速度百分比
					float SpeedPercent = currentSpeed / MaxSpeed;
					_value = SpeedPercent < 0.01f ? "0" : SpeedPercent.ToString("F3");
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 当前速度（公里/小时）
			case "CATUI_VehicleCurrentSpeedKPH":
				_value = "0";
				if (__instance.vehicle != null)
				{
					float currentSpeed = Mathf.Abs(__instance.vehicle.GetVehicle().CurrentForwardVelocity + 0.001f);
					_value = currentSpeed < 0.01f ? "0" : (currentSpeed * 3.6f).ToString("F1");
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 未加速 最大速度（米/秒）
			case "CATUI_VehicleMaxSpeedNotTurbo":
				_value = "0";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetVehicle().VelocityMaxForward.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 最大速度（米/秒）
			case "CATUI_VehicleMaxSpeed":
				_value = "0";
				if (__instance.vehicle != null)
				{
					// 载具最大速度
					float MaxTurboSpeed = __instance.vehicle.GetVehicle().VelocityMaxTurboForward;
					// 是否有引擎组件
					bool hasEnginePart = __instance.vehicle.GetVehicle().HasEnginePart();
					// 引擎组件 加速系数
					float MaxSpeedPer = __instance.vehicle.GetVehicle().EffectVelocityMaxPer;
					_value = (hasEnginePart ? MaxTurboSpeed * MaxSpeedPer : MaxTurboSpeed).ToString("0.00");
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 刹车
			case "CATUI_VehicleIsBrake":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetVehicle().CurrentIsBreak.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 库存最大容量
			case "CATUI_VehicleInventorySlotCount":
				_value = "false";
				if (__instance.vehicle != null && __instance.vehicle.GetVehicle().HasStorage())
				{
					_value = __instance.vehicle.bag.GetSlots().Length.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 库存已使用容量
			case "CATUI_VehicleInventoryItemCount":
				_value = "false";
				if (__instance.vehicle != null && __instance.vehicle.GetVehicle().HasStorage())
				{
					_value = __instance.vehicle.bag.GetUsedSlotCount().ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 能否加速
			case "CATUI_VehicleCanTurbo":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetVehicle().CanTurbo.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 是否加速
			case "CATUI_VehicleIsTurbo":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetVehicle().IsTurbo.ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 是否喇叭
			case "CATUI_VehicleHasHorn":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetVehicle().HasHorn().ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 是否有大灯
			case "CATUI_VehicleHasLight":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.HasHeadlight().ToString();
					__instance.IsDirty = true;
				}
				__result = true;
				return false;
			// 载具 - 是否打开大灯
			case "CATUI_VehicleIsLight":
				_value = "false";
				if (__instance.vehicle != null)
				{
					//_value = (__instance.vehicle.GetVehicle().FindPart("headlight") as VPHeadlight)?.IsOn().ToString();
					_value = (__instance.vehicle.IsHeadlightOn).ToString();
					__instance.IsDirty = true;
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
		if (__instance.vehicle != null)
		{
			__instance.RefreshBindings();
		}
	}
}
