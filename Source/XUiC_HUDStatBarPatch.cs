using HarmonyLib;
using UnityEngine;
using System;
using System.Collections;
using System.Text;
using System.Runtime.CompilerServices;

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

	// 帧缓存 - 避免同一帧内重复计算
	private static int _cachedFrame;
	private static float _mobility;
	private static float _physicalDamageResist;
	private static float _runSpeed;
	private static float _barteringBuying;
	private static float _barteringSelling;
	private static float _entityPenetrationCount;
	private static bool _mobilityCached;
	private static bool _resistCached;
	private static bool _runSpeedCached;
	private static bool _buyingCached;
	private static bool _sellingCached;
	private static bool _penetrationCached;

	// 载具缓存
	private static Vehicle _cachedVehicle;
	private static int _vehicleCachedFrame;

	// 任务日志缓存
	private static int _cachedFactionTier = -1;
	private static int _cachedFactionPoints;
	private static int _cachedFactionMax;
	private static bool _questCached;

	// 限频刷新 - 每个实例每 10 帧最多触发一次 IsDirty，绑定值变化时按 ~10fps 刷新界面，避免每帧刷新
	private const int DirtyThrottleFrames = 10;

	// 按实例记录上次限频触发帧。
	// 注意：HUD 上存在多个独立 HUDStatBar 实例（LevelNum/GameStage/SkillPoints/…），
	// 若用全局共享帧号，每 10 帧只会标记到第一个实例，其余实例绑定值变化时不会刷新。
	// ConditionalWeakTable 不持有实例强引用，实例销毁后状态自动回收。
	private static readonly ConditionalWeakTable<XUiC_HUDStatBar, LastDirtyState> _dirtyStates = new ConditionalWeakTable<XUiC_HUDStatBar, LastDirtyState>();

	private sealed class LastDirtyState
	{
		public int lastDirtyFrame = int.MinValue;
	}

	// 限频标记 IsDirty：该实例距上次标记达到 10 帧才触发，避免频繁刷新
	[PublicizedFrom(EAccessModifier.Private)]
	private static void MarkDirtyThrottled(XUiC_HUDStatBar instance)
	{
		if (instance == null)
		{
			return;
		}
		int frame = Time.frameCount;
		LastDirtyState state = _dirtyStates.GetOrCreateValue(instance);
		if (frame - state.lastDirtyFrame >= DirtyThrottleFrames)
		{
			state.lastDirtyFrame = frame;
			instance.IsDirty = true;
		}
	}

	// 限频轮询兜底：原版 stat 无变化时 hasChanged() 为 false，RefreshBindings 不会执行，
	// 自定义绑定值变化便无法刷新；这里按实例每 10 帧标记一次 IsDirty，
	// 保证每个 HUDStatBar 实例的 GetBindingValueInternal 都被定期调用、绑定值持续刷新
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "Update")]
	[PublicizedFrom(EAccessModifier.Private)]
	private static void UpdatePostfix(XUiC_HUDStatBar __instance)
	{
		if (__instance == null || __instance.IsDirty)
		{
			return;
		}
		MarkDirtyThrottled(__instance);
	}

	private static void EnsureCacheFrame()
	{
		int frame = Time.frameCount;
		if (frame != _cachedFrame)
		{
			_cachedFrame = frame;
			_mobilityCached = false;
			_resistCached = false;
			_runSpeedCached = false;
			_buyingCached = false;
			_sellingCached = false;
			_penetrationCached = false;
			_vehicleCachedFrame = -1;
			_questCached = false;
		}
	}

	private static float GetMobility(EntityPlayerLocal player)
	{
		if (!_mobilityCached)
		{
			_mobility = EffectManager.GetValue(PassiveEffects.Mobility, null, 0f, player, null, XUiM_Player.GetPlayer().generalTags, calcEquipment: true, calcHoldingItem: true, calcProgression: true, calcBuffs: true, calcChallenges: true, 1, useMods: true, _useDurability: true);
			_mobilityCached = true;
		}
		return _mobility;
	}

	private static float GetPhysicalDamageResist(EntityPlayerLocal player)
	{
		if (!_resistCached)
		{
			_physicalDamageResist = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, player);
			_resistCached = true;
		}
		return _physicalDamageResist;
	}

	private static float GetRunSpeed(EntityPlayerLocal player)
	{
		if (!_runSpeedCached)
		{
			_runSpeed = EffectManager.GetValue(PassiveEffects.RunSpeed, null, 0f, player);
			_runSpeedCached = true;
		}
		return _runSpeed;
	}

	private static float GetBarteringBuying(EntityPlayerLocal player)
	{
		if (!_buyingCached)
		{
			_barteringBuying = EffectManager.GetValue(PassiveEffects.BarteringBuying, null, 0f, player);
			_buyingCached = true;
		}
		return _barteringBuying;
	}

	private static float GetBarteringSelling(EntityPlayerLocal player)
	{
		if (!_sellingCached)
		{
			_barteringSelling = EffectManager.GetValue(PassiveEffects.BarteringSelling, null, 0f, player);
			_sellingCached = true;
		}
		return _barteringSelling;
	}

	private static float GetEntityPenetrationCount(EntityPlayerLocal player)
	{
		if (!_penetrationCached)
		{
			_entityPenetrationCount = EffectManager.GetValue(PassiveEffects.EntityPenetrationCount, null, 0f, player);
			_penetrationCached = true;
		}
		return _entityPenetrationCount;
	}

	private static Vehicle GetCachedVehicle(XUiC_HUDStatBar instance)
	{
		if (_vehicleCachedFrame != _cachedFrame)
		{
			_cachedVehicle = instance.vehicle?.GetVehicle();
			_vehicleCachedFrame = _cachedFrame;
		}
		return _cachedVehicle;
	}

	private static void EnsureQuestCache(EntityPlayerLocal player)
	{
		if (!_questCached && player != null)
		{
			_cachedFactionTier = player.QuestJournal.GetCurrentFactionTier(1);
			_cachedFactionPoints = player.QuestJournal.GetQuestFactionPoints(1);
			_cachedFactionMax = player.QuestJournal.GetQuestFactionMax(1, _cachedFactionTier);
			_questCached = true;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_HUDStatBar __instance)
	{
		EnsureCacheFrame();
		// 统一限频：所有绑定（含未单独调用的只读绑定，如 CATUI_playerSkillPointsAvailable）每次执行都尝试限频刷新
		MarkDirtyThrottled(__instance);

		switch (_bindingName)
		{
			// 角色名称
			case "CATUI_playerName":
				_value = " ";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.PlayerDisplayName;
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
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 存活时间
			case "CATUI_playerCurrentLife":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetCurrentLife(__instance.localPlayer).ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 护甲等级 (直接用数值，避免 string→int 反解析)
			case "CATUI_playerArmorRating":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float resist = GetPhysicalDamageResist(__instance.localPlayer);
					_value = playerArmorRatingFormatter.Format((int)resist);
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 护甲等级 - 区间 (复用缓存，直接用 float 比较)
			case "CATUI_playerArmorLevel":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					int armor = (int)GetPhysicalDamageResist(__instance.localPlayer);
					_value = armor switch
					{
						0 => "0",
						> 0 and < 20 => "1",
						>= 20 and < 40 => "2",
						>= 40 and < 60 => "3",
						>= 60 and < 80 => "4",
						>= 80 and < 100 => "5",
						_ => "6"
					};
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 游戏阶段
			case "CATUI_playerGameStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.gameStage.ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 搜刮阶段
			case "CATUI_playerLootStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.GetLootStage(0f, 0f).ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 商人阶段
			case "CATUI_playerTraderStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EnsureQuestCache(__instance.localPlayer);
					_value = _cachedFactionTier.ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 商人阶段 进度 当前值
			case "CATUI_playerTraderStageProgressCurrent":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EnsureQuestCache(__instance.localPlayer);
					_value = _cachedFactionPoints.ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 商人阶段 进度 最大值
			case "CATUI_playerTraderStageProgressMax":
				_value = "10";
				if (__instance.localPlayer != null)
				{
					EnsureQuestCache(__instance.localPlayer);
					_value = _cachedFactionMax.ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 旅行距离
			case "CATUI_playerTraveled":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetKMTraveled(__instance.localPlayer).ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 击杀丧尸数
			case "CATUI_playerZombieKills":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetZombieKills(__instance.localPlayer).ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 温度 - 体感
			case "CATUI_coretemp":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetCoreTemp(__instance.localPlayer).ToString();
					MarkDirtyThrottled(__instance);
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
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 温度 - 室外
			case "CATUI_outsidetemp":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetOutsideTemp(__instance.localPlayer).ToString();
					MarkDirtyThrottled(__instance);
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
					MarkDirtyThrottled(__instance);
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
					MarkDirtyThrottled(__instance);
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
						if (_ping <= 150)
						{
							_value = GoodColor;
						}
						else if (_ping <= 500)
						{
							_value = MediumColor;
						}
						else
						{
							_value = PoorColor;
						}
					}
					MarkDirtyThrottled(__instance);
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
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 移动速度
			case "CATUI_playerMoveSpeed":
				_value = "100";
				if (__instance.localPlayer != null)
				{
					float num = GetMobility(__instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 移动速度等级 (复用缓存)
			case "CATUI_playerMoveSpeedLevel":
				_value = "4";
				if (__instance.localPlayer != null)
				{
					float speed = GetMobility(__instance.localPlayer) * 100f;
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
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 奔跑速度
			case "CATUI_playerRunSpeed":
				_value = "110";
				if (__instance.localPlayer != null)
				{
					float num = GetRunSpeed(__instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物属性 - 购物优惠
			case "CATUI_playerBarteringBuying":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float num = GetBarteringBuying(__instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 出售优惠
			case "CATUI_playerBarteringSelling":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float num = GetBarteringSelling(__instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
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
						Color32 color = QualityInfo.GetQualityColor(itemValue.Quality);
						_value = rgbaColorFormatter.Format(color);
					}
					MarkDirtyThrottled(__instance);
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
					MarkDirtyThrottled(__instance);
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
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 人物 - 待使用技能点
			case "CATUI_playerSkillPointsAvailable":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.Progression.SkillPoints.ToString();
				}
				__result = true;
				return false;

			// 人物属性 - 目标穿透
			case "CATUI_playerEntityPenetrationCount":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = playerEntityPenetrationCountFormatter.Format(GetEntityPenetrationCount(__instance.localPlayer));
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 图标
			case "CATUI_VehicleIcon":
				_value = "";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.GetMapIcon();
				}
				__result = true;
				return false;

			// 载具 - 当前速度（米/秒）
			case "CATUI_VehicleCurrentSpeed":
				_value = "0";
				Vehicle v = GetCachedVehicle(__instance);
				if (v != null)
				{
					float currentSpeed = Mathf.Abs(v.CurrentForwardVelocity + 0.001f);
					_value = currentSpeed < 0.01f ? "0" : currentSpeed.ToString("F2");
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 当前速度（百分比）
			case "CATUI_VehicleCurrentSpeedFill":
				_value = "0";
				Vehicle vf = GetCachedVehicle(__instance);
				if (vf != null)
				{
					float MaxTurboSpeed = vf.VelocityMaxTurboForward;
					bool hasEnginePart = vf.HasEnginePart();
					float MaxSpeedPer = vf.EffectVelocityMaxPer;
					float MaxSpeed = hasEnginePart ? MaxTurboSpeed * MaxSpeedPer : MaxTurboSpeed;
					float currentSpeed = Mathf.Abs(vf.CurrentForwardVelocity + 0.001f);
					float SpeedPercent = currentSpeed / MaxSpeed;
					_value = SpeedPercent < 0.01f ? "0" : SpeedPercent.ToString("F3");
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 当前速度（公里/小时）
			case "CATUI_VehicleCurrentSpeedKPH":
				_value = "0";
				Vehicle vk = GetCachedVehicle(__instance);
				if (vk != null)
				{
					float currentSpeed = Mathf.Abs(vk.CurrentForwardVelocity + 0.001f);
					_value = currentSpeed < 0.01f ? "0" : (currentSpeed * 3.6f).ToString("F1");
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 未加速 最大速度（米/秒）
			case "CATUI_VehicleMaxSpeedNotTurbo":
				_value = "0";
				Vehicle vm = GetCachedVehicle(__instance);
				if (vm != null)
				{
					_value = vm.VelocityMaxForward.ToString();
				}
				__result = true;
				return false;

			// 载具 - 最大速度（米/秒）
			case "CATUI_VehicleMaxSpeed":
				_value = "0";
				Vehicle vmx = GetCachedVehicle(__instance);
				if (vmx != null)
				{
					float MaxTurboSpeed = vmx.VelocityMaxTurboForward;
					bool hasEnginePart = vmx.HasEnginePart();
					float MaxSpeedPer = vmx.EffectVelocityMaxPer;
					_value = (hasEnginePart ? MaxTurboSpeed * MaxSpeedPer : MaxTurboSpeed).ToString("0.00");
				}
				__result = true;
				return false;

			// 载具 - 刹车
			case "CATUI_VehicleIsBrake":
				_value = "false";
				Vehicle vb = GetCachedVehicle(__instance);
				if (vb != null)
				{
					_value = vb.CurrentIsBreak.ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 库存最大容量
			case "CATUI_VehicleInventorySlotCount":
				_value = "false";
				if (__instance.vehicle != null && __instance.vehicle.GetVehicle().HasStorage())
				{
					_value = __instance.vehicle.bag.GetSlots().Length.ToString();
				}
				__result = true;
				return false;

			// 载具 - 库存已使用容量
			case "CATUI_VehicleInventoryItemCount":
				_value = "false";
				if (__instance.vehicle != null && __instance.vehicle.GetVehicle().HasStorage())
				{
					_value = __instance.vehicle.bag.GetUsedSlotCount().ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 能否加速
			case "CATUI_VehicleCanTurbo":
				_value = "false";
				Vehicle vct = GetCachedVehicle(__instance);
				if (vct != null)
				{
					_value = vct.CanTurbo.ToString();
				}
				__result = true;
				return false;

			// 载具 - 是否加速
			case "CATUI_VehicleIsTurbo":
				_value = "false";
				Vehicle vt = GetCachedVehicle(__instance);
				if (vt != null)
				{
					_value = vt.IsTurbo.ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 载具 - 是否喇叭
			case "CATUI_VehicleHasHorn":
				_value = "false";
				Vehicle vh = GetCachedVehicle(__instance);
				if (vh != null)
				{
					_value = vh.HasHorn().ToString();
				}
				__result = true;
				return false;

			// 载具 - 是否有大灯
			case "CATUI_VehicleHasLight":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.HasHeadlight().ToString();
				}
				__result = true;
				return false;

			// 载具 - 是否打开大灯
			case "CATUI_VehicleIsLight":
				_value = "false";
				if (__instance.vehicle != null)
				{
					_value = __instance.vehicle.IsHeadlightOn.ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}