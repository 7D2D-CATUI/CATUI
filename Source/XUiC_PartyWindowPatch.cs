using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_PartyWindowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_PartyWindow), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_PartyWindow __instance)
	{
		switch (_bindingName)
		{
			// 根据团队数量设置PartyWindow Pos Y
			// 已知bug：Companions（非玩家组队）变化后不会触发此bind
			case "CATUI_PartyWindowPositionY":
				// 默认间距
				int defaultHeight = 130;
				// 单项高度
				int entryHeight = 56;
				_value = defaultHeight.ToString();
				// 玩家实例
				var player = __instance.xui.playerUI.entityPlayer;
                // 组队
                if (player != null && player.Party != null && player.Party.MemberList != null)
                {
                    int entryListCount = player.Party.MemberList.Count;
                    if (entryListCount > 0)
                    {
                        int PositionY = defaultHeight + (entryListCount - 1) * entryHeight;
                        _value = PositionY.ToString();
                    }
                }
                // 伙伴
                else if (player != null && player.Party != null && player.Companions != null)
                {
                    int entryListCount = player.Companions.Count;
                    if (entryListCount > 0)
                    {
                        int PositionY = defaultHeight + entryListCount * entryHeight;
                        _value = PositionY.ToString();
                    }
                }
                __result = true;
				return false;
			default:
				return true;
		}
	}
}
