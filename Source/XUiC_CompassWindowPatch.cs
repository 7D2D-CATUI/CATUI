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
				if (__instance.localPlayer != null)
				{
					value = WeatherManager.Instance.spectrumSourceType.ToString();
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
			default:
				return true;
		}
	}
}
