using HarmonyLib;
using UnityEngine;
using System;
using System.Collections;
using System.Text;
using System.Reflection;
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

	// 空投组件缓存（按 World 引用失效，换世界后重取）
	private static World _cachedAirdropWorld;
	private static AIDirectorAirDropComponent _cachedAirdropComponent;
	// 空投私有字段缓存（真实程序集中为 private，需反射读取）
	private static FieldInfo _airDropNextField;
	private static FieldInfo _airDropLastCheckField;

	// 限频刷新 - 普通统计绑定每 10 帧标脏一次 IsDirty；车速仪表盘值连续变化，每帧标脏
	private const int DirtyThrottleFrames = 10;
	private const int VehicleDirtyThrottleFrames = 1;

	// 温度状态条 - 竖线固定按 0~132 活动区滑动（低温左侧），居中偏移 66，不随父宽度变化
	private const float TempBarLeft = 0f;
	private const float TempBarRight = 132f;
	private const float TempBarCenter = 66f;
	private static XUiV_Sprite _tempBarMark;
	// 竖线归属的 statbar 实例（只有它每帧移动竖线并提前返回，其余实例必须照常走限频标脏刷新绑定）
	private static XUiC_HUDStatBar _tempBarMarkOwner;

	// 按实例记录限频状态（HUD 有多个 HUDStatBar 实例，不能共用全局帧号）
	private static readonly ConditionalWeakTable<XUiC_HUDStatBar, LastDirtyState> _dirtyStates = new ConditionalWeakTable<XUiC_HUDStatBar, LastDirtyState>();

	private sealed class LastDirtyState
	{
		// 统计与车速两个计数槽分开，避免同实例其它 10 帧绑定污染车速的快速限频
		public int lastDirtyFrame = -DirtyThrottleFrames;
		public int lastVehicleDirtyFrame = -VehicleDirtyThrottleFrames;
	}

	// 距上次标脏达到 intervalFrames 帧才标记 IsDirty
	[PublicizedFrom(EAccessModifier.Private)]
	private static void MarkDirtyThrottled(XUiC_HUDStatBar instance, int intervalFrames)
	{
		if (instance == null)
		{
			return;
		}
		int frame = Time.frameCount;
		LastDirtyState state = _dirtyStates.GetOrCreateValue(instance);
		if (intervalFrames == VehicleDirtyThrottleFrames)
		{
			if (frame - state.lastVehicleDirtyFrame >= intervalFrames)
			{
				state.lastVehicleDirtyFrame = frame;
				instance.IsDirty = true;
			}
		}
		else if (frame - state.lastDirtyFrame >= intervalFrames)
		{
			state.lastDirtyFrame = frame;
			instance.IsDirty = true;
		}
	}

	// 默认限频（~10fps），用于一般统计绑定
	[PublicizedFrom(EAccessModifier.Private)]
	private static void MarkDirtyThrottled(XUiC_HUDStatBar instance)
	{
		MarkDirtyThrottled(instance, DirtyThrottleFrames);
	}

	// 兜底：原版 stat 无变化时不会 RefreshBindings，这里定期标脏保证绑定被求值。
	// 载具实例（车速仪表盘）每帧标脏维持丝滑刷新，其余实例 10 帧限频
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "Update")]
	[PublicizedFrom(EAccessModifier.Private)]
	private static void UpdatePostfix(XUiC_HUDStatBar __instance)
	{
		if (__instance == null)
		{
			return;
		}
		// 温度状态条竖线：逐帧把 tempBarMark 移到 _coretemp 对应的 x 位置（0~132 clamp，居中偏移 66）
		// 缓存失效判定：换角色/窗口重建后旧 uiTransform 已被销毁（Unity 对象 == null），需重新查找
		if (_tempBarMark == null || _tempBarMark.UiTransform == null)
		{
			_tempBarMark = null;
			_tempBarMarkOwner = null;
			XUiController markController = __instance.GetChildById("tempBarMark");
			if (markController != null)
			{
				_tempBarMark = markController.ViewComponent as XUiV_Sprite;
				_tempBarMarkOwner = __instance;
			}
		}
		// 只对真正拥有竖线的实例移动并提前返回；其他 statbar 实例照常走下面的限频标脏刷新绑定
		if (ReferenceEquals(_tempBarMarkOwner, __instance) && _tempBarMark != null && __instance.localPlayer != null)
		{
			try
			{
				float coretemp = __instance.localPlayer.Buffs.GetCustomVar("_coretemp");
				int x = (int)Mathf.Clamp(coretemp, TempBarLeft, TempBarRight) - (int)TempBarCenter;
				_tempBarMark.position = new Vector2i(x, 0);
				_tempBarMark.positionDirty = true;
				_tempBarMark.TryUpdatePosition();
			}
			catch (Exception)
			{
				_tempBarMark = null;
				_tempBarMarkOwner = null;
			}
			return;
		}
		if (__instance.IsDirty)
		{
			return;
		}
		if (__instance.statGroup == HUDStatGroups.Vehicle)
		{
			__instance.IsDirty = true;
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

	// 载具实际可达到的最大速度：优先用引擎收敛后的 velocityMax（已含 mod/BUFF 速度倍率），
	// 为 0（未初始化/停驶）时回退到 涡轮上限×倍率 的物理公式
	private static float GetEffectiveMaxSpeed(EntityVehicle vehicle)
	{
		if (vehicle != null)
		{
			float vm = vehicle.velocityMax;
			if (vm > 0f)
			{
				return vm;
			}
			Vehicle v = vehicle.GetVehicle();
			if (v != null)
			{
				return v.VelocityMaxTurboForward * v.EffectVelocityMaxPer;
			}
		}
		return 0f;
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

	// 空投倒计时数据源：仅服务器权威侧有效（单机/房主）；纯客户端本地计算与服务器不同流，不权威，返回 null
	private static AIDirectorAirDropComponent GetAirdropComponent()
	{
		if (ConnectionManager.Instance == null || !ConnectionManager.Instance.IsServer)
		{
			return null;
		}
		World world = GameManager.Instance?.World;
		if (world == null)
		{
			return null;
		}
		if (_cachedAirdropComponent == null || !ReferenceEquals(_cachedAirdropWorld, world))
		{
			_cachedAirdropWorld = world;
			_cachedAirdropComponent = world.aiDirector?.GetComponent<AIDirectorAirDropComponent>();
		}
		return _cachedAirdropComponent;
	}

	// 反射读取空投组件私有字段（FieldInfo 静态缓存，Instance|Public|NonPublic 双保险）
	private static ulong GetAirdropField(AIDirectorAirDropComponent comp, ref FieldInfo field, string fieldName)
	{
		if (comp == null)
		{
			return 0;
		}
		if (field == null)
		{
			field = typeof(AIDirectorAirDropComponent).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		object raw = field?.GetValue(comp);
		return raw is ulong value ? value : 0;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_HUDStatBar), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_HUDStatBar __instance)
	{
		EnsureCacheFrame();
		MarkDirtyThrottled(__instance);

		switch (_bindingName)
		{
			// 角色姓名
			case "CATUI_playerName":
				_value = " ";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.PlayerDisplayName;
				}
				__result = true;
				return false;

			// 弹药上限
			case "CATUI_AmmoMax":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = __instance.currentAmmoCount.ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 存活时间
			case "CATUI_playerCurrentLife":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetCurrentLife(__instance.localPlayer).ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 护甲数值
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

			// 护甲等级区间 (0-6)
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

			// 游戏阶段
			case "CATUI_playerGameStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.gameStage.ToString();
				}
				__result = true;
				return false;

			// 搜刮阶段
			case "CATUI_playerLootStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.GetLootStage(0f, 0f).ToString();
				}
				__result = true;
				return false;

			// 商人阶段
			case "CATUI_playerTraderStage":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EnsureQuestCache(__instance.localPlayer);
					_value = _cachedFactionTier.ToString();
				}
				__result = true;
				return false;

			// 是否服务器权威侧（单机/房主 true；连他人服务器的纯客户端 false），可用于空投等服务器权威数据的展示控制
			case "CATUI_isServer":
				_value = (ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer).ToString().ToLower();
				__result = true;
				return false;

			// 空投 - 倒计时（xx时，仅小时避免 HUD 频繁跳动）
			case "CATUI_airDropTimeLeft":
				_value = "";
				{
					AIDirectorAirDropComponent airDrop = GetAirdropComponent();
					if (airDrop != null && AIDirectorAirDropComponent.MaxDayCount > 0)
					{
						ulong next = GetAirdropField(airDrop, ref _airDropNextField, "nextAirDropTime");
						World world = GameManager.Instance?.World;
						if (next > 0 && world != null)
						{
							ulong now = world.worldTime;
							ulong remaining = next > now ? next - now : 0;
							// 1000 tick = 1 小时（不含余量，避免跳动）；不足 1 小时显示 "<1"
							ulong hours = remaining / 1000;
							_value = hours > 0 ? $"{hours}{Localization.Get("timeAbbreviationHours")}" : $"<1{Localization.Get("timeAbbreviationHours")}";
							MarkDirtyThrottled(__instance);
						}
					}
				}
				__result = true;
				return false;

			// 空投 - 剩余进度（0-1 递增，填满式）
			case "CATUI_airDropProgress":
				_value = "0";
				{
					AIDirectorAirDropComponent airDrop = GetAirdropComponent();
					if (airDrop != null && AIDirectorAirDropComponent.MaxDayCount > 0)
					{
						ulong next = GetAirdropField(airDrop, ref _airDropNextField, "nextAirDropTime");
						ulong last = GetAirdropField(airDrop, ref _airDropLastCheckField, "lastAirdropCheckTime");
						World world = GameManager.Instance?.World;
						if (next > 0 && world != null)
						{
							ulong now = world.worldTime;
							ulong remaining = next > now ? next - now : 0;
							// 总周期：会话内用真实 next-last；读档后 lastAirdropCheckTime 未恢复为 0，回退配置天数
							ulong total;
							if (last != 0 && next > last)
							{
								total = next - last;
							}
							else
							{
								total = (ulong)Math.Max(1, AIDirectorAirDropComponent.MaxDayCount) * 24000uL;
							}
							float progress = total > 0 ? 1f - Mathf.Clamp01((float)remaining / (float)total) : 0f;
							_value = progress.ToString("F3");
							MarkDirtyThrottled(__instance);
						}
					}
				}
				__result = true;
				return false;

			// 商人阶段进度 - 当前值
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

			// 商人阶段进度 - 最大值
			case "CATUI_playerTraderStageProgressMax":
				_value = "10";
				if (__instance.localPlayer != null)
				{
					EnsureQuestCache(__instance.localPlayer);
					_value = _cachedFactionMax.ToString();
				}
				__result = true;
				return false;

			// 旅行距离
			case "CATUI_playerTraveled":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetKMTraveled(__instance.localPlayer).ToString();
				}
				__result = true;
				return false;

			// 击杀丧尸数
			case "CATUI_playerZombieKills":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetZombieKills(__instance.localPlayer).ToString();
				}
				__result = true;
				return false;

			// 温度 - 体感
			case "CATUI_coretemp":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetCoreTemp(__instance.localPlayer).ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 温度 - 体感 颜色
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

			// 温度 - 室外
			case "CATUI_outsidetemp":
				_value = "";
				if (__instance.localPlayer != null)
				{
					_value = XUiM_Player.GetOutsideTemp(__instance.localPlayer).ToString();
					MarkDirtyThrottled(__instance);
				}
				__result = true;
				return false;

			// 温度 - 室外 颜色
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

			// 网络 - ping
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

			// 网络 - ping 颜色（绿/黄/红，>150/>500）
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

			// 网络 - 是否展示 ping
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

			// 移动能力（百分比）
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

			// 移动能力等级 (0-6)
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

			// 奔跑能力（百分比）
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

			// 商人优惠 - 买（百分比）
			case "CATUI_playerBarteringBuying":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float num = GetBarteringBuying(__instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
				}
				__result = true;
				return false;

			// 商人优惠 - 卖（百分比）
			case "CATUI_playerBarteringSelling":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					float num = GetBarteringSelling(__instance.localPlayer) * 100f;
					_value = ((int)num).ToString();
				}
				__result = true;
				return false;

			// 当前手持 - 图标
			case "CATUI_playerActiveItemIcon":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					Inventory inventory = localPlayer.inventory;
					ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
					ItemClass itemClass = itemValue.ItemClass;
					if (itemClass != null)
					{
						_value = itemClass.GetIconName();
					}
				}
				__result = true;
				return false;

			// 当前手物 - 名称
			case "CATUI_playerActiveItemName":
				_value = "";
				if (__instance.localPlayer != null)
				{
					EntityPlayer localPlayer = __instance.localPlayer;
					ItemActionAttack attackAction = __instance.attackAction;
					// 编辑工具（画笔/方块涂改等）展示当前编辑目标，而非物品名
					if (attackAction != null && attackAction.IsEditingTool())
					{
						ItemActionData itemActionDataInSlot = localPlayer.inventory.GetItemActionDataInSlot(__instance.currentSlotIndex, 1);
						_value = attackAction.GetStat(itemActionDataInSlot);
					}
					else
					{
						Inventory inventory = localPlayer.inventory;
						ItemValue itemValue = inventory.GetItem(__instance.currentSlotIndex).itemValue;
						ItemClass itemClass = itemValue.ItemClass;
						if (itemClass != null)
						{
							_value = itemClass.GetLocalizedItemName();
						}
					}
				}
				__result = true;
				return false;

			// 当前持有物 - 品质颜色
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

			// 当前手持物 - 耐久 剩余值
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

			// 当前手持物 - 耐久 最大值
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

			// 可用技能点
			case "CATUI_playerSkillPointsAvailable":
				_value = "1";
				if (__instance.localPlayer != null)
				{
					_value = __instance.localPlayer.Progression.SkillPoints.ToString();
				}
				__result = true;
				return false;

			// 目标穿透
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

			// 载具 - 当前速度填充条（满格 = 基础+加速达到的涡轮上限）
			case "CATUI_VehicleCurrentSpeedFill":
				_value = "0";
				if (__instance.vehicle != null)
				{
					Vehicle vf = __instance.vehicle.GetVehicle();
					if (vf != null)
					{
						float maxSpeed = vf.VelocityMaxTurboForward * vf.EffectVelocityMaxPer;
						float currentSpeed = Mathf.Abs(vf.CurrentForwardVelocity);
						float SpeedPercent = maxSpeed > 0f ? currentSpeed / maxSpeed : 0f;
						SpeedPercent = Mathf.Clamp01(SpeedPercent);
						_value = SpeedPercent < 0.01f ? "0" : SpeedPercent.ToString("F3");
						MarkDirtyThrottled(__instance, VehicleDirtyThrottleFrames);
					}
				}
				__result = true;
				return false;

			// 载具 - 涡轮区间占比（基础档最高速度/涡轮档最高速度，0-1），用于仪表盘涡轮区间背景起点
			case "CATUI_VehicleTurboRatio":
				_value = "0";
				if (__instance.vehicle != null)
				{
					Vehicle vr = __instance.vehicle.GetVehicle();
					if (vr != null)
					{
						float turboMax = vr.VelocityMaxTurboForward * vr.EffectVelocityMaxPer;
						float baseMax = vr.VelocityMaxForward * vr.EffectVelocityMaxPer;
						float ratio = turboMax > 0f ? baseMax / turboMax : 0f;
						ratio = Mathf.Clamp01(ratio);
						_value = ratio.ToString("F3");
						MarkDirtyThrottled(__instance, VehicleDirtyThrottleFrames);
					}
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
					MarkDirtyThrottled(__instance, VehicleDirtyThrottleFrames);
				}
				__result = true;
				return false;

			// 载具 - 未加速 最大速度
			case "CATUI_VehicleMaxSpeedNotTurbo":
				_value = "0";
				if (__instance.vehicle != null)
				{
					_value = GetEffectiveMaxSpeed(__instance.vehicle).ToString("0.00");
				}
				__result = true;
				return false;

			// 载具 - 最大速度
			case "CATUI_VehicleMaxSpeed":
				_value = "0";
				if (__instance.vehicle != null)
				{
					_value = GetEffectiveMaxSpeed(__instance.vehicle).ToString("0.00");
				}
				__result = true;
				return false;

			// 载具 - 是否刹车
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

			// 载具 - 库存容量
			case "CATUI_VehicleInventorySlotCount":
				_value = "false";
				if (__instance.vehicle != null && __instance.vehicle.GetVehicle().HasStorage())
				{
					_value = __instance.vehicle.bag.GetSlots().Length.ToString();
				}
				__result = true;
				return false;

			// 载具 - 库存已用
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

			// 载具 - 是否加速中
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

			// 载具 - 是否开大灯
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