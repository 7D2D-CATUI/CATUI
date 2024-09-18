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
			// �´�Ѫ��ʱ�䣨�Ա��������ĵڼ��죩
			case "CATUI_nextBloodMoonDay":
				value = "7";
				if (__instance.localPlayer != null)
				{
					value = GameStats.GetInt(EnumGameStats.BloodMoonDay).ToString();
				}
				__result = true;
				return false;

			// ��ͶƵ��
			case "CATUI_airDropFrequency":
				value = "3";
				if (__instance.localPlayer != null)
				{
					value = (GameStats.GetInt(EnumGameStats.AirDropFrequency) / 24f).ToString();
				}
				__result = true;
				return false;

			// ��ǰ����
			case "CATUI_currentWeather":
				value = "None";
				if (__instance.localPlayer != null)
				{
					value = WeatherManager.Instance.spectrumSourceType.ToString();
				}
				__result = true;
				return false;
			// �Ƿ����
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
