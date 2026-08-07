using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_CompassWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_CompassWindow), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(ref string value, string bindingName, ref bool __result, XUiC_CompassWindow __instance)
	{
		// 缓存变量
		var localPlayer = __instance.localPlayer;
		bool hasLocalPlayer = localPlayer != null;
		BiomeDefinition.BiomeType? currentBiomeType = null;
		WeatherManager.BiomeWeather biomeWeather = null;
		int stormLevel = 0;
		int worldTime = 0;

		// 预获取变量
		if (hasLocalPlayer)
		{
			worldTime = WeatherManager.worldTime;
			var biomeStandingOn = localPlayer.biomeStandingOn;
			if (biomeStandingOn != null)
			{
				currentBiomeType = biomeStandingOn.m_BiomeType;
				biomeWeather = WeatherManager.Instance.FindBiomeWeather(currentBiomeType.Value);
			}
			stormLevel = WeatherManager.currentWeather?.biomeDefinition?.currentWeatherGroup?.stormLevel ?? 0;
		}

		switch (bindingName)
		{
			// 人物属性 - 是否被敌人锁定（进入战斗状态）：任一敌方生物把玩家当作攻击目标即 true，不限范围
			case "CATUI_playerAlert":
				value = "false";
				if (__instance.localPlayer != null)
				{
					EntityPlayerLocal player = __instance.localPlayer;
					var worldB = GameManager.Instance.World;
					if (worldB != null && worldB.Entities != null && player.IsAlive())
					{
						foreach (Entity entity in worldB.Entities.list)
						{
							if (entity is EntityAlive alive && alive != player && alive.IsAlive())
							{
								EntityClass eClass = EntityClass.list.ContainsKey(alive.entityClass) ? EntityClass.list[alive.entityClass] : null;
								if (eClass != null && eClass.bIsEnemyEntity && alive.GetAttackTarget() == player)
								{
									value = "true";
									break;
								}
							}
						}
					}
					__instance.IsDirty = true;
				}
				__result = true;
				return false;

			// 距离下次血月的天数（差值，血月当天为 0），XUi 表达式直接比较，无需 day 变量
			case "CATUI_nextBloodMoonDay":
				value = "7";
				if (hasLocalPlayer)
				{
					int currentDay = GameManager.Instance.World != null ? (int)(GameManager.Instance.World.worldTime / 24000) : 1;
					value = (GameStats.GetInt(EnumGameStats.BloodMoonDay) - currentDay).ToString();
				}
				__result = true;
				return false;

			// 当前血月进度条 fill（0-1），从血月当天dusk 到次日dawn 刷怪结束（时长随游戏白昼设置变化）
			// 不依赖 BloodMoonDay（其会在血月激活时已推进到下一个），直接用当前世界时间 + dusk/dawn 推导当前夜窗口
			case "CATUI_bloodMoonProgress":
				value = "0";
				if (hasLocalPlayer && GameManager.Instance.World != null)
				{
					var worldB = GameManager.Instance.World;
					var bmComp = worldB.aiDirector != null ? worldB.aiDirector.BloodMoonComponent : null;
					if (bmComp != null && bmComp.duskHour > 0)
					{
						ulong worldTimeC = worldB.worldTime;
						ulong day = worldTimeC / 24000;
						ulong hour = (worldTimeC % 24000) / 1000;
						int dusk = bmComp.duskHour;
						int dawn = bmComp.dawnHour;
						// 起始夜：若当前已到深夜(dusk及之后)，属于今天之夜的窗口；若在凌晨(dawn前)，属于昨夜窗口
						ulong baseDay = hour >= (ulong)dusk ? day : (day > 0 ? day - 1 : day);
						ulong windowStart = baseDay * 24000 + (ulong)dusk * 1000;
						ulong windowEnd = (baseDay + 1) * 24000 + (ulong)dawn * 1000;
						ulong total = windowEnd - windowStart;
						if (total > 0 && worldTimeC >= windowStart && worldTimeC <= windowEnd)
						{
							double p = (double)(worldTimeC - windowStart) / (double)total;
							value = p.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
						}
					}
				}
				__result = true;
				return false;

			// 空投频率
			case "CATUI_airDropFrequency":
				value = "3";
				if (hasLocalPlayer)
				{
					value = (GameStats.GetInt(EnumGameStats.AirDropFrequency) / 24f).ToString("F1");
				}
				__result = true;
				return false;

			// 当前天气（血月优先返回 BloodMoon；雾浓度超阈值返回 Foggy，且雾优先级最低）
			case "CATUI_currentWeather":
				value = "None";
				if (hasLocalPlayer && currentBiomeType.HasValue && biomeWeather != null && biomeWeather.biomeDefinition != null)
				{
					bool isBloodMoon = false;
					var world = GameManager.Instance.World;
					if (world != null && world.aiDirector != null && world.aiDirector.BloodMoonComponent != null)
					{
						isBloodMoon = world.aiDirector.BloodMoonComponent.BloodMoonActive;
					}
					string spectrum = biomeWeather.biomeDefinition.weatherSpectrum.ToString();
					if (isBloodMoon)
					{
						value = "BloodMoon"; // 血月最高优先级
					}
					else if (spectrum == "Snowy" || spectrum == "Stormy" || spectrum == "Rainy")
					{
						value = spectrum; // 雨/雪/风暴优先于雾
					}
					else if (SkyManager.GetFogDensity() > 15f)
					{
						// 仅当无具体天气(图谱为 Biome/None)且雾浓时才显示 Foggy（雾优先级最低）
						value = "Foggy";
					}
					else
					{
						value = spectrum;
					}
				}
				__result = true;
				return false;

			// 是否白天
			case "CATUI_isDaytime":
				value = "true";
				if (hasLocalPlayer)
				{
					value = GameManager.Instance.World.IsDaytime().ToString();
				}
				__result = true;
				return false;

			/* 风暴逻辑 ================================================================== */
			// 风暴等級：0=无，1=风暴警告，2=风暴中
			case "CATUI_stormLevel":
				value = stormLevel.ToString();
				__result = true;
				return false;

			// 风暴名(风暴icon / 风暴名称本地化) return: "Burnt", "Desert", "Snow", "Wasteland"
			case "CATUI_stormName":
				value = "";
				if (hasLocalPlayer && currentBiomeType.HasValue && stormLevel > 0)
				{
					if (currentBiomeType.Value == BiomeDefinition.BiomeType.burnt_forest)
					{
						value = "Burnt";
					}
					else if (currentBiomeType.Value == BiomeDefinition.BiomeType.Desert ||
							 currentBiomeType.Value == BiomeDefinition.BiomeType.Snow ||
							 currentBiomeType.Value == BiomeDefinition.BiomeType.Wasteland)
					{
						value = currentBiomeType.Value.ToString();
					}
				}
				__result = true;
				return false;

			// 风暴持续真实时间 mm:ss eg. 2分 8秒
			case "CATUI_stormDurationTimeReal":
				value = "0";
				if (hasLocalPlayer && biomeWeather != null && stormLevel > 0)
				{
					int stormRemaining = biomeWeather.stormWorldTime + biomeWeather.stormDuration - worldTime;
					// 持续时间/时间流逝速度
					int stormDurationTimeSec = (stormRemaining / GameStats.GetInt(EnumGameStats.TimeOfDayIncPerSec));
					value = XUiM_PlayerBuffs.ConvertToTimeString(stormDurationTimeSec);
				}
				__result = true;
				return false;

			// 风暴持续时间 eg.3200
			case "CATUI_stormDurationTime":
				value = "0";
				if (hasLocalPlayer && biomeWeather != null)
				{
					value = biomeWeather.stormDuration.ToString();
				}
				__result = true;
				return false;

			// 风暴剩余时间 eg.3000
			case "CATUI_stormRemainingTime":
				value = "0";
				if (hasLocalPlayer && biomeWeather != null && stormLevel > 0)
				{
					int stormRemaining = biomeWeather.stormWorldTime + biomeWeather.stormDuration - worldTime;
					value = stormRemaining.ToString();
				}
				__result = true;
				return false;

			// 风暴开始世界时间 eg.74000（包括当前风暴和下一场风暴开始时间）
			case "CATUI_stormStartWorldTime":
				value = "0";
				if (hasLocalPlayer && biomeWeather != null)
				{
					value = biomeWeather.stormWorldTime.ToString();
				}
				__result = true;
				return false;

			// 风暴结束世界时间 eg.77200
			case "CATUI_stormEndWorldTime":
				value = "0";
				if (hasLocalPlayer && biomeWeather != null)
				{
					value = (biomeWeather.stormWorldTime + biomeWeather.stormDuration).ToString();
				}
				__result = true;
				return false;

			// 天气世界时间 eg.77000
			case "CATUI_worldTime":
				value = "0";
				if (hasLocalPlayer)
				{
					value = worldTime.ToString();
				}
				__result = true;
				return false;

			// 风暴结束进度条 eg.0.432
			case "CATUI_stormFill":
				value = "0.000";
				if (hasLocalPlayer && biomeWeather != null && stormLevel > 0)
				{
					int stormRemaining = biomeWeather.stormWorldTime + biomeWeather.stormDuration - worldTime;
					double fillPercentage = (double)stormRemaining / biomeWeather.stormDuration;
					value = fillPercentage.ToString("F3");
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}

	// 血月进度条需要连续刷新：vanilla Compass 只在天气/时间变化时才 RefreshBindings，
	// 而进度值是连续变化的，故血月激活时定期标脏，保证 CATUI_bloodMoonProgress 被重算。
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiController), "Update")]
	[PublicizedFrom(EAccessModifier.Private)]
	private static void UpdatePostfix(XUiController __instance)
	{
		if (__instance as XUiC_CompassWindow == null)
		{
			return;
		}
		var world = GameManager.Instance.World;
		if (world == null || world.aiDirector == null || world.aiDirector.BloodMoonComponent == null)
		{
			return;
		}
		if (!world.aiDirector.BloodMoonComponent.BloodMoonActive)
		{
			return;
		}
		__instance.IsDirty = true;
	}
}
