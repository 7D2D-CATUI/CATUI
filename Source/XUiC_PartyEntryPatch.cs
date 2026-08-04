using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_PartyEntryPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_PartyEntry), "GetBindingValueInternal")]
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
						// 网络良好
						if (_ping <= 150) {
							value = GoodColor;
						}
						// 网络一般
						else if (_ping <= 500) {
							value = MediumColor;
						}
						// 网络较差
						else
						{
							value = PoorColor;
						}
					}
				}
				__result = true;
				return false;

			// 玩家在队伍中的排序索引（MemberList 下标）
			case "CATUI_PartyIndex":
				value = "0";
				if (__instance.Player != null && __instance.xui != null && __instance.xui.playerUI != null && __instance.xui.playerUI.entityPlayer != null)
				{
					EntityPlayerLocal localPlayer = __instance.xui.playerUI.entityPlayer;
					if (localPlayer.Party != null && localPlayer.Party.MemberList != null)
					{
						List<EntityPlayer> memberList = localPlayer.Party.MemberList;
						for (int i = 0; i < memberList.Count; i++)
						{
							if (ReferenceEquals(memberList[i], __instance.Player))
							{
								value = i.ToString();
								break;
							}
						}
					}
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_PartyEntry), "SetPlayer")]
	public static void SetPlayerPostfix(EntityPlayer player, XUiC_PartyEntry __instance)
	{
		UpdateAvatar(__instance, player);
	}

	// 队友头像：有 Steam 头像时显示头像，否则显示占位（黑色底 + 队伍序号）
	private static void UpdateAvatar(XUiC_PartyEntry entry, EntityPlayer player)
	{
		XUiV_Texture avatarTexture = GetChildView<XUiV_Texture>(entry, "avatar");
		if (avatarTexture == null)
		{
			return;
		}
		// 默认显示占位（黑底+序号）
		avatarTexture.Texture = null;
		avatarTexture.IsVisible = false;
		SetChildVisible(entry, "avatarFallback", true);
		SetChildVisible(entry, "avatarFallbackIdx", true);

		ulong steamId = PartyAvatar.GetSteamId(player);
		if (steamId == 0UL)
		{
			return;
		}
		PartyAvatar.RequestAvatar(steamId, delegate(Texture2D texture)
		{
			// 头像加载完成时校验该 entry 仍显示同一玩家
			if (texture == null || !ReferenceEquals(entry.Player, player))
			{
				return;
			}
			avatarTexture.Texture = texture;
			avatarTexture.IsVisible = true;
			SetChildVisible(entry, "avatarFallback", false);
			SetChildVisible(entry, "avatarFallbackIdx", false);
		});
	}

	private static T GetChildView<T>(XUiController controller, string name) where T : class
	{
		try
		{
			XUiController child = controller.GetChildById(name);
			if (child != null)
			{
				return child.ViewComponent as T;
			}
		}
		catch (Exception e)
		{
			Log.Error("[CATUI] GetChildView(" + name + ") error: " + e);
		}
		return null;
	}

	private static void SetChildVisible(XUiController controller, string name, bool visible)
	{
		try
		{
			XUiController child = controller.GetChildById(name);
			if (child != null)
			{
				child.ViewComponent.IsVisible = visible;
			}
		}
		catch
		{
		}
	}
}
