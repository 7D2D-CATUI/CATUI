using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_QuestTrackerObjectiveEntry))]
public class XUiC_QuestTrackerObjectiveEntryPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_QuestTrackerObjectiveEntry), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_QuestTrackerObjectiveEntry __instance)
	{
		switch (bindingName)
		{
			// 
			//case "CATUI_001":
			//	value = "None";
			//	if (flag)
			//	{
			//		value = questObjective.ObjectiveValueType.ToString();
			//	}
			//	__result = true;
			//	return false;
			//case "CATUI_002":
			//	value = "None";
			//	if (flag)
			//	{
			//		if (questObjective.OwnerQuest.CurrentState == Quest.QuestState.InProgress && questObjective.ObjectiveState != BaseObjective.ObjectiveStates.Complete) {
			//			value = questObjective.Description + "+++Description";
			//		}
			//	}
			//	__result = true;
			//	return false;
			//case "CATUI_003":
			//	value = "None";
			//	if (flag)
			//	{
			//		if (questObjective.OwnerQuest.CurrentState == Quest.QuestState.InProgress && questObjective.ObjectiveState != BaseObjective.ObjectiveStates.Complete)
			//		{
			//			questObjective.Description = "1/2";
			//			value = "";
			//		}
			//	}
			//	__result = true;
			//	return false;
			//case "CATUI_004":
			//	value = "None";
			//	if (flag)
			//	{
					

			//		if (questObjective.OwnerQuest.CurrentState == Quest.QuestState.InProgress && questObjective.ObjectiveState != BaseObjective.ObjectiveStates.Complete)
			//		{
			//			value = QuestEventManager.Current.SleeperVolumeLocationList.Count.ToString();
			//		}
			//	}
			//	__result = true;
			//	return false;
			//case "CATUI_005":
			//	value = "None";
			//	if (flag)
			//	{
			//		value = (questObjective is ObjectiveClearSleepers).ToString();
			//	}
			//	__result = true;
			//	return false;

			//case "CATUI_001":
		
			//	value = "";

			//	__result = true;
			//	return false;

			default:
				return false;
		}
	}
}
