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
			// �����Ŷ���������PartyWindow Pos Y
			// ��֪bug��Companions���������ӣ��仯�󲻻ᴥ����bind
			case "CATUI_PartyWindowPositionY":
				// Ĭ�ϼ��
				int defaultHeight = 130;
				// ����߶�
				int entryHeight = 56;
				value = defaultHeight.ToString();
				// ���
				if (__instance.player != null && __instance.player.Party != null)
				{
					int entryListCount = __instance.player.party.MemberList.Count;
					if (entryListCount > 0)
					{
						int PositionY = defaultHeight + (entryListCount - 1) * entryHeight;
						value = PositionY.ToString();
					}
				}
				// ���
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
