using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class ShowSleepersCount3
{
	public static int clearedSleepersCount;

	public static int totalSleepersCount;

	public static void setupSleepersCount(Quest quest)
	{
		clearedSleepersCount = 0;
		totalSleepersCount = 0;
		PrefabInstance prefabFromWorldPos = GameManager.Instance.GetDynamicPrefabDecorator().GetPrefabFromWorldPos((int)quest.GetLocation().x, (int)quest.GetLocation().z);
		if (prefabFromWorldPos == null)
		{
			return;
		}
		totalSleepersCount = prefabFromWorldPos.prefab.SleeperVolumes.Count;
		for (int i = 0; i < prefabFromWorldPos.prefab.SleeperVolumes.Count; i++)
		{
			if (prefabFromWorldPos.prefab.SleeperVolumes[i].isQuestExclude)
			{
				totalSleepersCount--;
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(QuestEventManager), "SleeperVolumePositionRemoved")]
	public static void SleeperVolumePositionRemovedPostfix()
	{
		clearedSleepersCount++;
	}

    //[HarmonyPrefix]
    //[HarmonyPatch()]
    //public static bool ObjectiveValueTypePrefix(ref BaseObjective.ObjectiveValueTypes __result)
    //{
    //    __result = BaseObjective.ObjectiveValueTypes.Number;
    //    return false;
    //}

    //[HarmonyPrefix]
    //[HarmonyPatch()]
    //public static bool StatusTextPrefix(ref string __result, BaseObjective __instance)
    //{
    //	if (__instance is ObjectiveClearSleepers)
    //	{
    //		__result = "";
    //		if (__instance.OwnerQuest.CurrentState == Quest.QuestState.InProgress && __instance.ObjectiveState != BaseObjective.ObjectiveStates.Complete)
    //		{
    //			__result = string.Concat(clearedSleepersCount);
    //			if (totalSleepersCount > 0)
    //			{
    //				__result = __result + " / " + totalSleepersCount;
    //			}
    //		}
    //		return false;
    //	}
    //	return true;
    //}

    [HarmonyPrefix]
    [HarmonyPatch()]
    public static bool postfix(ref string __result, ObjectiveClearSleepers __instance)
    {
        
        return true;
    }



    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetPackagePartyQuestChange), "HandlePlayer")]
    public static void Postfix(EntityPlayerLocal localPlayer, int ___senderEntityID, int ___questCode, byte ___objectiveIndex)
    {
        Quest sharedQuest = localPlayer.QuestJournal.GetSharedQuest(___questCode);
        if (sharedQuest != null)
        {
            setupSleepersCount(sharedQuest);
        }
    }

    [HarmonyPostfix]
	[HarmonyPatch(typeof(ObjectiveRallyPoint), "RallyPointActivate")]
	public static void RallyPointActivatePostfix(ObjectiveRallyPoint __instance)
	{
		setupSleepersCount(__instance.OwnerQuest);
	}
}
