using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_BuffPopoutListPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_BuffPopoutList), "OnOpen")]
	public static void OnOpenPostfix(XUiC_BuffPopoutList __instance)
	{
		if (__instance.items != null && __instance.items.Count > 0)
		{
			UpdateBuffPositions(__instance);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_BuffPopoutList), "AddNotification")]
	public static void AddNotificationPostfix(XUiC_BuffPopoutList __instance, EntityUINotification _notification)
	{
		if (__instance.items != null && __instance.items.Count > 0)
		{
			var lastItem = __instance.items[__instance.items.Count - 1];
			if (lastItem != null && lastItem.Notification == _notification)
			{
				UILabel textName = lastItem.Item.transform.Find("TextName").GetComponent<UILabel>();
				if (textName != null && _notification.Buff != null)
				{
					// 使用BindingsManager.ReplaceCVars解析LocalizedName中的{cvar(...)}占位符
					textName.text = BindingsManager.ReplaceCVars(_notification.Buff.BuffClass.LocalizedName);
				}
				UISprite decoration = lastItem.Item.transform.Find("Decoration").GetComponent<UISprite>();
				if (decoration != null)
				{
					decoration.color = _notification.GetColor();
				}
			}
			UpdateBuffPositions(__instance);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_BuffPopoutList), "Update")]
	public static void UpdatePostfix(XUiC_BuffPopoutList __instance)
	{
		if (__instance.items != null && __instance.items.Count > 0)
		{
			foreach (var data in __instance.items)
			{
				if (data.Notification != null && data.Notification.Buff != null && !data.Notification.Buff.Paused)
				{
					UILabel textName = data.Item.transform.Find("TextName").GetComponent<UILabel>();
					if (textName != null)
					{
						textName.text = BindingsManager.ReplaceCVars(data.Notification.Buff.BuffClass.LocalizedName);
					}
				}
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_BuffPopoutList), "updateEntries")]
	public static bool updateEntriesPrefix(XUiC_BuffPopoutList __instance)
	{
		UpdateBuffPositions(__instance);
		return false;
	}

	private static void UpdateBuffPositions(XUiC_BuffPopoutList __instance)
	{
		int visibleCount = 0;
		for (int i = 0; i < __instance.items.Count; i++)
		{
			var data = __instance.items[i];
			if (!data.Notification.Buff.Paused)
			{
				TweenPosition component = data.Item.GetComponent<TweenPosition>();
				if ((bool)component)
				{
					Object.Destroy(component);
				}
				Vector3 currentPos = data.Item.transform.localPosition;
				float targetY = (float)visibleCount * __instance.height + __instance.yOffset;
				data.Item.transform.localPosition = new Vector3(currentPos.x, -targetY, currentPos.z);
				visibleCount++;
			}
		}
	}
}