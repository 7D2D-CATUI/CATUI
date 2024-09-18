using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_PartyEntryPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_PartyEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_PartyEntry __instance)
	{
		switch (bindingName)
		{
			// ping
			case "CATUI_Ping":
				value = "-1";
				if (__instance.Player != null) {
					int _ping = __instance.Player.pingToServer;
					if (_ping > 0)
					{
						value = _ping > 1000 ? ">1000" : _ping.ToString();
					}
				}
				__result = true;
				return false;
			case "CATUI_PingColor":
				value = "0,0,0";
				if (__instance.Player != null)
				{
					int _ping = __instance.Player.pingToServer;
					const string GoodColor = "67, 207, 124";
					const string MediumColor = "255, 195, 0";
					const string PoorColor = "255, 0, 0";
					if (_ping > 0)
					{
						// 利大措挫
						if (_ping <= 150) {
							value = GoodColor;
						}
						// 利大匯違
						else if (_ping <= 500) {
							value = MediumColor;
						}
						// 利大熟餓
						else
						{
							value = PoorColor;
						}
					}
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}
}
