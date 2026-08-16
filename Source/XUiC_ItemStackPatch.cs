using Audio;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[HarmonyPatch]
public class XUiC_ItemStackPatch
{
    private const string ALLOW_CLICKLOCK_ATTR = "allow_clicklock";
    private static readonly HashSet<XUiC_ItemStack> _patchedInstances = new HashSet<XUiC_ItemStack>();
    private const string MODIFICATION_HIGHLIGHTED = "[04FE85]▇[-] ";
    private const string MODIFICATION_DEFAULT = "▇ ";
    private static FieldInfo _lockSpriteField;

    private class BindingCache
    {
        public ItemStack itemStack;
        public bool boosted;
        public bool legendary;
        public string modifications;
    }

    // 绑定结果缓存：itemStack 引用变化即物品变化，引用比较廉价且准确
    private static readonly Dictionary<XUiC_ItemStack, BindingCache> _bindingCache = new Dictionary<XUiC_ItemStack, BindingCache>();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "Init")]
    public static void InitPostfix(XUiC_ItemStack __instance)
    {
		if (_patchedInstances.Contains(__instance))
			return;

		_patchedInstances.Add(__instance);
		__instance.OnPress += (XUiController _sender, int _mouseButton) =>
		{
            // 判断alt键是否按下
			if (!InputUtils.AltKeyPressed)
				return;

            // 是否允许栏位锁（配置写在xml上，allow_clicklock="${allow_clicklock}"）
			bool allowClicklock = false;
			if (_sender.CustomAttributes.ContainsKey(ALLOW_CLICKLOCK_ATTR))
			{
				allowClicklock = StringParsers.ParseBool(_sender.CustomAttributes[ALLOW_CLICKLOCK_ATTR].ToString());
			}
			if (!allowClicklock)
				return;

			var backpackWindow = __instance.xui.GetChildByType<XUiC_BackpackWindow>();
			var lootWindow = __instance.xui.GetChildByType<XUiC_LootWindow>();
			var vehicleContainer = __instance.xui.GetChildByType<XUiC_BagContainer>();

			__instance.UserLockedSlot = !__instance.UserLockedSlot;
			__instance.RefreshBindings();

			// 背包 更新栏位锁状态
			if (_sender.Parent.ToString() == "XUiC_Backpack") {
				backpackWindow.UpdateLockedSlots(backpackWindow.standardControls);
			}
			// 箱子 容器窗口是否打开 更新栏位锁状态 2.2+
			else if (_sender.Parent.ToString() == "XUiC_LootContainer" && lootWindow.IsOpen)
			{
				lootWindow.UpdateLockedSlots(lootWindow.standardControls);
			}
			// 载具 容器窗口是否打开 更新栏位锁状态 2.2+
			else if (_sender.Parent.ToString() == "XUiController" && vehicleContainer.IsOpen)
			{
				vehicleContainer.UpdateLockedSlots(vehicleContainer.standardControls);
			}

			// 播放点击音效
			__instance.xui.PlayMenuClickSound();
		};
    }

    // 清理已销毁的实例
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiController), "OnClose")]
    public static void OnClosePostfix(XUiController __instance)
    {
        if (__instance is XUiC_ItemStack stack)
        {
            _patchedInstances.Remove(stack);
            _bindingCache.Remove(stack);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "GetBindingValueInternal")]
    public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiC_ItemStack __instance)
    {
        switch (_bindingName)
        {
            // 给道具增加index
            case "CATUI_itemStackSlotIndex":
                _value = "0";
                if (__instance?.SlotNumber != null)
                {
                    _value = (__instance.SlotNumber + 1).ToString();
                }
                __result = true;
                return false;

            // 缓存热路径绑定，避免每次刷新重复遍历 Stats/Modifications
            case "CATUI_itemBoosted":
            case "CATUI_itemLegendary":
            case "CATUI_itemStackModifications":
            {
                ItemStack stack = __instance?.itemStack ?? ItemStack.Empty;
                BindingCache cache;
                if (!_bindingCache.TryGetValue(__instance, out cache) || cache.itemStack != stack)
                {
                    cache = new BindingCache();
                    cache.itemStack = stack;

                    bool isEmpty = stack.IsEmpty();
                    cache.boosted = false;
                    cache.legendary = false;
                    cache.modifications = "";

                    if (!isEmpty)
                    {
                        ItemValue itemValue = stack.itemValue;
                        if (itemValue != null)
                        {
                            if (itemValue.HasAnyBoostedStats())
                            {
                                cache.boosted = true;
                            }

                            if (itemValue.Stats != null && itemValue.Stats.Length > 0)
                            {
                                bool allBoosted = true;
                                for (int i = 0; i < itemValue.Stats.Length; i++)
                                {
                                    if (!itemValue.Stats[i].isBoosted)
                                    {
                                        allBoosted = false;
                                        break;
                                    }
                                }
                                cache.legendary = allBoosted;
                            }

                            // 模组插槽文本
                            if (itemValue.Quality > 0 && itemValue.Modifications != null && itemValue.Modifications.Length > 0)
                            {
                                __instance.CustomAttributes.TryGetValue("mod_highlight", out var highlightStyle);
                                __instance.CustomAttributes.TryGetValue("mod_default", out var defaultStyle);
                                highlightStyle ??= MODIFICATION_HIGHLIGHTED;
                                defaultStyle ??= MODIFICATION_DEFAULT;

                                System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
                                for (int i = 0; i < itemValue.Modifications.Length; i++)
                                {
                                    if (itemValue.Modifications[i] == null)
                                    {
                                        textBuilder.Append(defaultStyle);
                                        continue;
                                    }
                                    var itemClass1 = itemValue.Modifications[i]?.ItemClass;
                                    if (itemClass1 != null && !string.IsNullOrEmpty(itemClass1?.GetItemName()))
                                    {
                                        textBuilder.Append(highlightStyle);
                                    }
                                    else
                                    {
                                        textBuilder.Append(defaultStyle);
                                    }
                                }
                                cache.modifications = textBuilder.ToString();
                            }
                        }
                    }

                    _bindingCache[__instance] = cache;
                }

                if (_bindingName == "CATUI_itemBoosted")
                {
                    _value = cache.boosted.ToString().ToLower();
                }
                else if (_bindingName == "CATUI_itemLegendary")
                {
                    _value = cache.legendary.ToString().ToLower();
                }
                else
                {
                    _value = cache.modifications;
                }
                __result = true;
                return false;
            }
            // 是否显示耐久度：hasdurability && MaxUseTimes>1 && 非模组 && 非篝火/熔炉
            case "CATUI_itemShowDurability":
            {
                _value = "false";

                ItemStack stack = __instance?.itemStack;
                if (stack == null || stack.IsEmpty() || stack.itemValue == null)
                {
                    __result = true;
                    return false;
                }

                if (!__instance.ShowDurability)
                {
                    __result = true;
                    return false;
                }

                if (stack.itemValue.MaxUseTimes <= 1)
                {
                    __result = true;
                    return false;
                }

                if (_lockSpriteField == null)
                {
                    _lockSpriteField = typeof(XUiC_ItemStack).GetField("lockSprite", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                string lockSprite = (string)_lockSpriteField?.GetValue(__instance) ?? "";
                if (lockSprite == "ui_game_symbol_assemble")
                {
                    __result = true;
                    return false;
                }

                ItemClass itemClass = stack.itemValue.ItemClassOrMissing;
                if (itemClass == null)
                {
                    __result = true;
                    return false;
                }

                string itemTypeIcon;
                if (itemClass.IsBlock() && stack.itemValue.TextureFullArray.IsDefault)
                {
                    itemTypeIcon = Block.list[stack.itemValue.type].ItemTypeIcon;
                }
                else if (itemClass.AltItemTypeIcon != null && itemClass.Unlocks != ""
                    && XUiM_ItemStack.CheckKnown(__instance.xui.playerUI.entityPlayer, itemClass, stack.itemValue))
                {
                    itemTypeIcon = itemClass.AltItemTypeIcon;
                }
                else
                {
                    itemTypeIcon = itemClass.ItemTypeIcon;
                }

                if (itemTypeIcon == "campfire" || itemTypeIcon == "forge")
                {
                    __result = true;
                    return false;
                }

                _value = "true";
                __result = true;
                return false;
            }

            // 调试用
            case "CATUI_itemBoostList":
                _value = "";
                if (__instance?.itemStack?.itemValue == null)
                {
                    __result = true;
                    return false;
                }
                ItemValue itemValue2 = __instance.itemStack.itemValue;
                if (itemValue2.Stats == null || itemValue2.Stats.Length == 0)
                {
                    _value = "";
                }
                else
                {
                    string text = string.Empty;
                    for (int i = 0; i < itemValue2.Stats.Length; i++)
                    {
                        var stat = itemValue2.Stats[i];
                        text += stat.type;
                        text += ": ";
                        text += stat.isBoosted + " - ";
                        text += stat.value;
                        if (i < itemValue2.Stats.Length - 1)
                        {
                            text += ", \n";
                        }
                    }
                    _value = text;
                }
                __result = true;
                return false;

            default:
                return true;
        }
    }

    // 去掉鼠标hover的icon缩放动画
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "AllowIconGrow", MethodType.Getter)]
    public static void AllowIconGrowPostfix(ref bool __result)
    {
        __result = false;
    }
}
