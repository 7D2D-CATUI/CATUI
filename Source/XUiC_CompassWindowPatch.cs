using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_CompassWindow))]
public class XUiC_CompassWindowPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CompassWindow), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_CompassWindow __instance)
	{
		switch (bindingName)
		{
			// 下次血月时间（对比总天数的第几天）
			case "CATUI_nextBloodMoonDay":
				value = "7";
				if (__instance.localPlayer != null)
				{
					value = GameStats.GetInt(EnumGameStats.BloodMoonDay).ToString();
				}
				__result = true;
				return false;

			// 空投频率
			case "CATUI_airDropFrequency":
				value = "3";
				if (__instance.localPlayer != null)
				{
					value = (GameStats.GetInt(EnumGameStats.AirDropFrequency) / 24f).ToString();
				}
				__result = true;
				return false;

			// 当前天气
			case "CATUI_currentWeather":
				value = "None";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					// value = WeatherManager.Instance.spectrumSourceType.ToString();
					// value = WeatherManager.currentWeather.biomeDefinition.weatherSpectrum.ToString();
					WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance.FindBiomeWeather(__instance.localPlayer.biomeStandingOn.m_BiomeType);
					value = biomeWeather.biomeDefinition.weatherSpectrum.ToString();
				}
				__result = true;
				return false;
			// 是否白天
			case "CATUI_isDaytime":
				value = "true";
				if (__instance.localPlayer != null)
				{
					value = GameManager.Instance.World.IsDaytime().ToString();
				}
				__result = true;
				return false;
			
			/* 风暴逻辑 ================================================================== */
			// 风暴等級：0=无，1=风暴警告，2=风暴中
			case "CATUI_stormLevel":
				value = "0";
				if (__instance.localPlayer != null)
				{
					int stormLevel = (int)(WeatherManager.currentWeather?.biomeDefinition?.currentWeatherGroup?.stormLevel); // 风暴等级
					value = stormLevel.ToString();
				}
				__result = true;
				return false;

			// 风暴名(风暴icon / 风暴名称本地化) return: "Burnt", "Desert", "Snow", "Wasteland"
			case "CATUI_stormName":
				value = "";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					int stormLevel = (int)(WeatherManager.currentWeather?.biomeDefinition?.currentWeatherGroup?.stormLevel); // 风暴等级
					BiomeDefinition.BiomeType currentBiomeType = __instance.localPlayer.biomeStandingOn.m_BiomeType;
					// BiomeDefinition.WeatherGroup currentWeatherGroup = WeatherManager.currentWeather.biomeDefinition.currentWeatherGroup;
					// BiomeDefinition.BiomeType biomeType = (BiomeDefinition.BiomeType)(WeatherManager.currentWeather?.biomeDefinition.m_BiomeType);
					/*string buffName = currentWeatherGroup.buffName; // 二阶段风暴伤害buff
					string name = currentWeatherGroup.name; // 返回值：storm
					string spectrum = currentWeatherGroup.spectrum.ToString();  // 当前天气
					value = $"currentBiomeType: {currentBiomeType}, biomeType: {biomeType}, buffName: {buffName},";*/
					// 火烧地特殊处理
					if (stormLevel > 0 && currentBiomeType == BiomeDefinition.BiomeType.burnt_forest) {
						value = "Burnt";
					} 
					else if (stormLevel > 0 && (currentBiomeType == BiomeDefinition.BiomeType.Desert || currentBiomeType == BiomeDefinition.BiomeType.Snow || currentBiomeType == BiomeDefinition.BiomeType.Wasteland)) {
						value = currentBiomeType.ToString();
					}
				}
				__result = true;
				return false;

			// 风暴持续时间 eg.3200
			case "CATUI_stormDurationTime":
				value = "0";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					// value = WeatherManager.currentWeather.biomeDefinition.WeatherGetDuration("stormbuild").ToString();
					WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance.FindBiomeWeather(__instance.localPlayer.biomeStandingOn.m_BiomeType);
					int stormDuration = biomeWeather.stormDuration; // 持续时间
					value = stormDuration.ToString();
				}
				__result = true;
				return false;

			// 风暴剩余时间 eg.3000
			case "CATUI_stormRemainingTime":
				value = "0";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					int stormLevel = (int)(WeatherManager.currentWeather?.biomeDefinition?.currentWeatherGroup?.stormLevel); // 风暴等级
					WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance.FindBiomeWeather(__instance.localPlayer.biomeStandingOn.m_BiomeType);
					int worldTime = WeatherManager.worldTime; // 世界时间
					int stormWorldTime = biomeWeather.stormWorldTime; // 风暴开始世界时间
					int stormDuration = biomeWeather.stormDuration; // 风暴持续时间
					int stormRemaining = stormWorldTime + stormDuration - worldTime; // 开始世界时间74000 + 持续时间3200 - 当前世界时间74200（风暴中结果必定大于0）
					value = (stormLevel > 0 ? stormRemaining : 0).ToString(); // 风暴中给出结果
				}
				__result = true;
				return false;

			// 风暴开始世界时间 eg.74000（包括当前风暴和下一场风暴开始时间）
			case "CATUI_stormStartWorldTime":
				value = "0";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance.FindBiomeWeather(__instance.localPlayer.biomeStandingOn.m_BiomeType);
					int stormWorldTime = biomeWeather.stormWorldTime; // 风暴开始世界时间
					value = stormWorldTime.ToString();
				}
				__result = true;
				return false;

			// 风暴结束世界时间 eg.77200
			case "CATUI_stormEndWorldTime":
				value = "0";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance.FindBiomeWeather(__instance.localPlayer.biomeStandingOn.m_BiomeType);
					int stormWorldTime = biomeWeather.stormWorldTime; // 风暴开始世界时间
					int stormDuration = biomeWeather.stormDuration; // 风暴持续时间
					value = (stormWorldTime + stormDuration).ToString();
				}
				__result = true;
				return false;

			// 天气世界时间 eg.77000
			case "CATUI_worldTime":
				value = "0";
				if (__instance.localPlayer != null)
				{
					int worldTime = WeatherManager.worldTime; // 世界时间
					value = worldTime.ToString();
				}
				__result = true;
				return false;

			// 风暴结束进度条 eg.0.432
			case "CATUI_stormFill":
				value = "0";
				if (__instance.localPlayer != null && __instance.localPlayer.biomeStandingOn != null)
				{
					int stormLevel = (int)(WeatherManager.currentWeather?.biomeDefinition?.currentWeatherGroup?.stormLevel); // 风暴等级
					WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance.FindBiomeWeather(__instance.localPlayer.biomeStandingOn.m_BiomeType);
					int worldTime = WeatherManager.worldTime; // 世界时间
					int stormWorldTime = biomeWeather.stormWorldTime; // 风暴开始世界时间
					int stormDuration = biomeWeather.stormDuration; // 风暴持续时间
					int stormRemaining = stormWorldTime + stormDuration - worldTime; // 开始世界时间74000 + 持续时间3200 - 当前世界时间74200（风暴中结果必定大于0）
					value = (stormLevel > 0 ? ((double)stormRemaining / stormDuration) : 0).ToString("F3"); // 风暴中给出结果
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}
}
