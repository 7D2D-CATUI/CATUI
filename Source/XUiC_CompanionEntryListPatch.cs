using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

[HarmonyPatch]
public class XUiC_CompanionEntryListPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_CompanionEntryList), "RefreshPartyList")]
	public static bool Prefix(XUiC_CompanionEntryList __instance)
	{
		// 删掉原来改变view位置的逻辑
		int i = 0;
		EntityPlayer entityPlayer = __instance.xui.playerUI.entityPlayer;
		if (entityPlayer.Companions != null)
		{
			for (int j = 0; j < entityPlayer.Companions.Count; j++)
			{
				EntityAlive companion = entityPlayer.Companions[j];
				if (i >= __instance.entryList.Count)
				{
					break;
				}
				__instance.entryList[i++].SetCompanion(companion);
			}
			for (; i < __instance.entryList.Count; i++)
			{
				__instance.entryList[i].SetCompanion(null);
			}
		}
		else
		{
			for (int k = 0; k < __instance.entryList.Count; k++)
			{
				__instance.entryList[k].SetCompanion(null);
			}
		}

		return false;
	}
}
