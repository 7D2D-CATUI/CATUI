using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_CollectedItemListPatch
{
	// 按名称给条目内的两个标签赋值:ItemName=名称,TextContent=数量。
	// 标签不存在则跳过,不报错。
	private static void SetLabels(GameObject item, string nameText, string countText)
	{
		if (item == null)
		{
			return;
		}
		Transform t = item.transform.Find("ItemName");
		if (t != null)
		{
			UILabel label = t.GetComponent<UILabel>();
			if (label != null)
			{
				label.text = nameText;
			}
		}
		t = item.transform.Find("TextContent");
		if (t != null)
		{
			UILabel label = t.GetComponent<UILabel>();
			if (label != null)
			{
				label.text = countText;
			}
		}
	}

	// 设置图标品质色:参照 XUiC_ItemStack 的 durabilitycolor 逻辑——
	// 仅当物品有品质栏(ItemClass.ShowQualityBar)时用 QualityInfo.GetQualityColor(品质) 上色,否则透明
	private static void SetDurabilityColor(GameObject item, ItemValue itemValue)
	{
		if (item == null)
		{
			return;
		}
		Transform t = item.transform.Find("ItemDurabilityColor");
		if (t == null)
		{
			return;
		}
		UISprite sprite = t.GetComponent<UISprite>();
		if (sprite == null)
		{
			return;
		}
		if (itemValue != null && itemValue.ItemClass != null && itemValue.ItemClass.ShowQualityBar)
		{
			sprite.color = QualityInfo.GetQualityColor(itemValue.Quality);
		}
		else
		{
			sprite.color = new Color(1f, 1f, 1f, 0f);
		}
	}

	// 获得/丢弃物品条目:ItemName=物品名,TextContent=只显示加减数量(去掉总数)。
	// 注意:游戏原版把数字文本写入模板里"第一个"UILabel,模板新增 ItemName 后会写错标签,
	// 故这里按名称重写两个标签。negative 区分获得(true 为丢弃条目,数字为负)。
	private static void FillItemText(XUiC_CollectedItemList instance, ItemStack _is, bool negative)
	{
		if (_is == null || _is.itemValue == null)
		{
			return;
		}
		// 从末尾向前找本次实际被更新的条目:同 itemValue.type 且同正负(与游戏聚合逻辑一致)
		for (int i = instance.items.Count - 1; i >= 0; i--)
		{
			var data = instance.items[i];
			if (data.ItemStack == null || data.ItemStack.itemValue.type != _is.itemValue.type || data.isNegative != negative)
			{
				continue;
			}
			// 素材不足提示(count==0)也显示品质色,但跳过文字
			SetDurabilityColor(data.Item, data.ItemStack.itemValue);
			if (data.isMissingNotifier)
			{
				return;
			}
			int count = data.ItemStack.count;
			string countText = negative
				? ((count >= 0) ? ("-" + count) : count.ToString())
				: ((count >= 1) ? ("+" + count) : count.ToString());
			SetLabels(data.Item, data.ItemStack.itemValue.ItemClass.GetLocalizedItemName(), countText);
			return;
		}
	}

	// 获得物品(AddItemStack)后重写标签
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CollectedItemList), "AddItemStack")]
	public static void AddItemStackPostfix(XUiC_CollectedItemList __instance, ItemStack _is)
	{
		FillItemText(__instance, _is, _is != null && _is.count < 0);
	}

	// 丢弃物品(RemoveItemStack)后重写标签,丢弃条目标记为负(isNegative=true)
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CollectedItemList), "RemoveItemStack")]
	public static void RemoveItemStackPostfix(XUiC_CollectedItemList __instance, ItemStack _is)
	{
		FillItemText(__instance, _is, true);
	}

	// 图标提示(如技能/统计图标):ItemName 置空,TextContent 显示累计数量,无品质色
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CollectedItemList), "AddIconNotification")]
	public static void AddIconNotificationPostfix(XUiC_CollectedItemList __instance, string iconNotifier)
	{
		for (int i = __instance.items.Count - 1; i >= 0; i--)
		{
			var data = __instance.items[i];
			if (data.ItemStack != null || data.uiAtlasIcon == null || !data.uiAtlasIcon.Equals(iconNotifier, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int c = data.count;
			SetDurabilityColor(data.Item, null);
			SetLabels(data.Item, "", (c > 0) ? ("+" + c) : c.ToString());
			return;
		}
	}

	// 制作技能升级提示:ItemName 置空,TextContent 显示 "等级/最大等级",无品质色
	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_CollectedItemList), "AddCraftingSkillNotification")]
	public static void AddCraftingSkillNotificationPostfix(XUiC_CollectedItemList __instance, ProgressionValue craftingSkill)
	{
		for (int i = __instance.items.Count - 1; i >= 0; i--)
		{
			var data = __instance.items[i];
			if (data.CraftingSkill == null || data.CraftingSkill != craftingSkill)
			{
				continue;
			}
			SetDurabilityColor(data.Item, null);
			SetLabels(data.Item, "", $"{craftingSkill.Level}/{craftingSkill.ProgressionClass.MaxLevel}");
			return;
		}
	}
}