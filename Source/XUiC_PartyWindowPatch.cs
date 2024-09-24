using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(XUiC_ItemInfoWindow))]
public class XUiC_PartyWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_PartyWindow), "GetBindingValue")]
	public static bool Prefix(string bindingName, ref string value, ref bool __result, XUiC_PartyWindow __instance)
	{
		switch (bindingName)
		{
			// 根据团队数量设置PartyWindow Pos Y
			// 已知bug：Companions（非玩家组队）变化后不会触发此bind
			case "CATUI_PartyWindowPositionY":
				// 默认间距
				int defaultHeight = 130;
				// 单项高度
				int entryHeight = 56;
				value = defaultHeight.ToString();
				// 组队
				if (__instance.player != null && __instance.player.Party != null)
				{
					int entryListCount = __instance.player.party.MemberList.Count;
					if (entryListCount > 0)
					{
						int PositionY = defaultHeight + (entryListCount - 1) * entryHeight;
						value = PositionY.ToString();
					}
				}
				// 伙伴
				else if(__instance.player != null && __instance.player.Companions != null)
				{
					int entryListCount = __instance.player.Companions.Count;
					if (entryListCount > 0)
					{
						int PositionY = defaultHeight + entryListCount * entryHeight;
						value = PositionY.ToString();
					}
				}
				__result = true;
				return false;
			default:
				return true;
		}
	}
}
