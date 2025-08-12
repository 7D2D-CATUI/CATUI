using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_CompassWindow))]
public class XUiC_CompassWindowPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CompassWindow), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_CompassWindow __instance)
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
			// 下次血月时间（对比总天数的第几天）
			case "CATUI_nextBloodMoonDay":
				value = "7";
				if (hasLocalPlayer)
				{
					value = GameStats.GetInt(EnumGameStats.BloodMoonDay).ToString();
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

			// 当前天气
			case "CATUI_currentWeather":
				value = "None";
				if (hasLocalPlayer && currentBiomeType.HasValue && biomeWeather != null && biomeWeather.biomeDefinition != null)
				{
					value = biomeWeather.biomeDefinition.weatherSpectrum.ToString();
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
}
