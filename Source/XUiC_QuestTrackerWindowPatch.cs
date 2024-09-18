using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_QuestTrackerWindow))]
public class XUiC_QuestTrackerWindowPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_QuestTrackerWindow), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_QuestTrackerWindow __instance)
	{
		Quest currentQuest = __instance.currentQuest;
		Challenges.Challenge currentChallenge = __instance.currentChallenge;
		switch (bindingName)
		{
			case "CATUI_ListCount":
				value = "0";
				if (currentQuest != null)
				{
					value = currentQuest.ActiveObjectives.ToString();
				}
				else if (currentChallenge != null)
				{
					value = currentChallenge.ActiveObjectives.ToString();
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}
}
