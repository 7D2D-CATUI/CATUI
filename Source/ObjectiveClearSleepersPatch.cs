using HarmonyLib;
using PrefabVolumes;
using UnityEngine;

// 清除丧尸任务(ObjectiveClearSleepers)计数补丁。
// 功能:在任务追踪窗口为"清除丧尸/群聚"类任务显示 "已清理 X / 总数 Y"。
//
// 计数单位:POI 内的"丧尸容积(SleeperVolume,即丧尸群)"数,而非个体丧尸。
// 一个容积被清空的判定是"该容积刷出的丧尸全死且不再刷"——所以表现为"清完一群 +1"(跳跃式),
// 这是原版机制(地图标记、任务完成也按容积数判定),无法做到逐丧尸。
//
// 总数 totalSleepersCount 取两源:
//  - 服务端/单机:直接读游戏自身追踪的 SleeperEventData.SleeperVolumes.Count,
//    与地图标记/任务完成 100% 一致;
//  - 客户端连远程服(本地无该字典):回退 prefab 定义级统计 Used 且非 isQuestExclude 的容积数。
// 已清理 clearedSleepersCount:按"进行中清除任务 POI 包围盒"过滤原版逐容积清除事件,
// 避免其它 POI/共享任务的事件污染计数。
//
// 显示:原版 XUiC_QuestTrackerObjectiveEntry 对 Boolean 型目标会清空 objectivestate,故必须把
// ObjectiveClearSleepers.ObjectiveValueType 强制为 Number,StatusText 才会被渲染成 "X / Y"。
[HarmonyPatch]
public class ObjectiveClearSleepersPatch
{
	// 已清理的丧尸容积数(事件累计,按当前任务 POI 包围盒过滤)
	public static int clearedSleepersCount;

	// 任务 POI 内丧尸容积总数
	public static int totalSleepersCount;

	// 当前进行中清除任务的 POI 包围盒(用于过滤清除事件)
	private static Rect activePoiRect = Rect.zero;

	// 是否存在进行中清除任务(避免无任务时误计数)
	private static bool hasActiveQuest;

	// 计算任务 POI 的丧尸容积总数、记录 POI 包围盒,并重置已清理计数。在任务(共享)激活时调用。
	public static void SetupSleepersCount(Quest quest)
	{
		clearedSleepersCount = 0;
		totalSleepersCount = 0;
		hasActiveQuest = false;
		activePoiRect = Rect.zero;

		Vector3 poiPos = quest.GetLocation();
		if (poiPos == Vector3.zero)
		{
			return;
		}

		// 用 POIPosition+POISize 构造包围盒,对齐原版 SetupZombieCompassBounds 的 Rect 构造方式
		Vector3 poiSize;
		quest.GetPositionData(out poiSize, Quest.PositionDataTypes.POISize);
		activePoiRect = new Rect(poiPos.x, poiPos.z, poiSize.x, poiSize.z);
		hasActiveQuest = true;

		// 优先取游戏自身追踪的容积集合(仅服务端/单机存在该字典),键为 POIPosition,与订阅键一致
		SleeperEventData sleeperData;
		if (QuestEventManager.Current.SleeperVolumeUpdateDictionary.TryGetValue(poiPos, out sleeperData))
		{
			totalSleepersCount = sleeperData.SleeperVolumes.Count;
			return;
		}

		// 兜底(客户端连远程服):按 prefab 定义统计 Used 且非 isQuestExclude 的容积数。
		// isQuestExclude 会被 SleeperVolume.Create 原样拷贝,与运行时一致。
		PrefabInstance prefabFromWorldPos = GameManager.Instance.GetDynamicPrefabDecorator().GetPrefabFromWorldPos((int)poiPos.x, (int)poiPos.z);
		if (prefabFromWorldPos == null)
		{
			return;
		}
		for (int i = 0; i < prefabFromWorldPos.prefab.SleeperVolumeList.Count; i++)
		{
			PrefabSleeperVolume prefabSleeperVolume = prefabFromWorldPos.prefab.SleeperVolumeList[i];
			if (prefabSleeperVolume.Used && !prefabSleeperVolume.isQuestExclude)
			{
				totalSleepersCount++;
			}
		}
	}

	// 每个丧尸容积被清空时原版会触发 SleeperVolumePositionRemoved,这里按"进行中清除任务的 POI 包围盒"
	// 过滤后累计,避免其它 POI/共享任务的事件污染计数。任务结束的批量取消订阅事件虽在同一包围盒内,
	// 但此时显示已被 InProgress/Complete 条件挡住,且下次激活会重置,不影响。
	[HarmonyPostfix]
	[HarmonyPatch(typeof(QuestEventManager), "SleeperVolumePositionRemoved")]
	public static void SleeperVolumePositionRemovedPostfix(Vector3 pos)
	{
		if (hasActiveQuest && activePoiRect.Contains(new Vector2(pos.x, pos.z)))
		{
			clearedSleepersCount++;
		}
	}

	// 把清除丧尸目标的数值类型从 Boolean 强制改为 Number。
	// 原版 XUiC_QuestTrackerObjectiveEntry 对 Boolean 目标(isBool)会清空 objectivestate 文本(仅显示勾选框),
	// 改成 Number 后才会渲染 StatusText 的 "X / Y"。
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ObjectiveClearSleepers), "get_ObjectiveValueType")]
	public static bool ObjectiveValueTypePrefix(ref BaseObjective.ObjectiveValueTypes __result)
	{
		__result = BaseObjective.ObjectiveValueTypes.Number;
		return false;
	}

	// 仅对清除丧尸目标重写 StatusText 为 "已清理 / 总数",其余目标放行走原版逻辑。
	[HarmonyPrefix]
	[HarmonyPatch(typeof(BaseObjective), "get_StatusText")]
	public static bool StatusTextPrefix(ref string __result, BaseObjective __instance)
	{
		if (__instance is ObjectiveClearSleepers)
		{
			__result = "";
			// 只在任务进行中且未完成时显示;完成/失败后留空,避免显示陈旧的数字
			if (__instance.OwnerQuest.CurrentState == Quest.QuestState.InProgress && __instance.ObjectiveState != BaseObjective.ObjectiveStates.Complete)
			{
				__result = string.Concat(clearedSleepersCount);
				if (totalSleepersCount > 0)
				{
					__result = __result + "/" + totalSleepersCount;
				}
			}
			return false;
		}
		return true;
	}

	// 队伍共享任务:仅当变更的目标就是"清除丧尸"目标时才同步重算,避免队友其它目标
	// (如取回/电力)完成时把本队的清除计数清零。
	// ___xxx 前缀参数为原版私有字段(senderEntityID/questCode/objectiveIndex),程序集已 publicize 可直接读取。
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetPackagePartyQuestChange), "HandlePlayer")]
	public static void HandlePlayerPostfix(EntityPlayerLocal localPlayer, int ___senderEntityID, int ___questCode, byte ___objectiveIndex)
	{
		Quest sharedQuest = localPlayer.QuestJournal.GetSharedQuest(___questCode);
		if (sharedQuest != null && ___objectiveIndex < sharedQuest.Objectives.Count && sharedQuest.Objectives[___objectiveIndex] is ObjectiveClearSleepers)
		{
			SetupSleepersCount(sharedQuest);
		}
	}

	// 进入任务 POI(激活集合点)时重算总数并清零已清理数。
	// 注意:RallyPointActivate 在激活(activate=true)与 POI 锁定/重置(activate=false)时都会被调用,
	// 需用 activate 参数守卫,避免在非激活时机把计数清零。
	[HarmonyPostfix]
	[HarmonyPatch(typeof(ObjectiveRallyPoint), "RallyPointActivate")]
	public static void RallyPointActivatePostfix(ObjectiveRallyPoint __instance, bool activate)
	{
		if (activate)
		{
			SetupSleepersCount(__instance.OwnerQuest);
		}
	}
}