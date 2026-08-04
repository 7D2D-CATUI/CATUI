using HarmonyLib;
using System;
using UnityEngine;

[HarmonyPatch]
public class XUiC_PartyWindowPatch
{
	// 计算组队窗口的 Y 位置，默认高度和单项高度从 controller="PartyWindow" 标签的属性读取，
	// 改 UI 时只需改 XML，无需重新编译；未配置时回退到这里的默认值
	public static int ComputePositionY(XUiC_PartyWindow window)
	{
		int defaultPartyWindowPositionY = 74;
		int entryHeight = 56;
		ReadAttributeInt(window, "default_PartyWindowPositionY", ref defaultPartyWindowPositionY);
		ReadAttributeInt(window, "entry_height", ref entryHeight);
		EntityPlayer player = window?.xui?.playerUI?.entityPlayer;
		if (player == null)
		{
			return defaultPartyWindowPositionY;
		}
		int y = defaultPartyWindowPositionY;
		if (player.Party != null && player.Party.MemberList != null)
		{
			y += (player.Party.MemberList.Count - 1) * entryHeight;
		}
		if (player.Companions != null)
		{
			y += player.Companions.Count * entryHeight;
		}
		return y;
	}

	static void ReadAttributeInt(XUiC_PartyWindow window, string attributeName, ref int fallback)
	{
		if (window == null)
		{
			return;
		}
		// 属性名解析时被转成小写存储，这里做不区分大小写的查找
		foreach (var pair in window.CustomAttributes)
		{
			if (string.Equals(pair.Key, attributeName, StringComparison.OrdinalIgnoreCase) &&
				pair.Value != null && int.TryParse(pair.Value.ToString(), out int parsed))
			{
				fallback = parsed;
				return;
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_PartyWindow), "GetBindingValueInternal")]
	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_PartyWindow __instance)
	{
		switch (_bindingName)
		{
			// 根据团队数量设置PartyWindow Pos Y
			case "CATUI_PartyWindowPositionY":
				_value = ComputePositionY(__instance).ToString();
				__result = true;
				return false;
			default:
				return true;
		}
	}

	// 队伍列表变化（新成员加入/离开/换队长）时，立即刷新组队窗口位置
	// 原版只在新成员加入时触发 PartyMemberAdded（XUiC_PartyWindow 没订阅），
	// 且 Update 的 1 秒轮询不保证触发，导致窗口整体不上移而错位
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_PartyEntryList), "RefreshPartyList")]
	public static void PartyEntryList_RefreshPartyListPostfix(XUiC_PartyEntryList __instance)
	{
		RefreshPartyWindowPosition(__instance);
	}

	// 伙伴列表变化时同样刷新（修复已知 bug：Companions 变化不触发 bind）
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CompanionEntryList), "RefreshPartyList")]
	public static void CompanionEntryList_RefreshPartyListPostfix(XUiC_CompanionEntryList __instance)
	{
		RefreshPartyWindowPosition(__instance);
	}

	static void RefreshPartyWindowPosition(XUiController source)
	{
		try
		{
			XUiC_PartyWindow window = source?.windowGroup?.Controller as XUiC_PartyWindow;
			if (window?.ViewComponent == null)
			{
				return;
			}
			// 刷新绑定，保持绑定缓存值一致
			window.RefreshBindings();
			// 直接应用位置，避免依赖视图每帧 Update（窗口不在 showing 时不更新）
			Vector2i pos = window.ViewComponent.Position;
			window.ViewComponent.Position = new Vector2i(pos.x, ComputePositionY(window));
			window.ViewComponent.TryUpdatePosition();
		}
		catch
		{
		}
	}
}
