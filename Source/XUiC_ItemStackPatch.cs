using Audio;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[HarmonyPatch]
public class XUiC_ItemStackPatch
{
    private const string UI_CLICK_SOUND_PATH = "@:Sounds/UI/ui_menu_click.wav";
    private static AudioClip _cachedClickSound;
    private const string ALLOW_CLICKLOCK_ATTR = "allow_clicklock";
    private static readonly HashSet<XUiC_ItemStack> _patchedInstances = new HashSet<XUiC_ItemStack>();
    // 定义默认样式常量，提高可维护性
    private const string MODIFICATION_HIGHLIGHTED = "[04FE85]▇[-] ";
    private const string MODIFICATION_DEFAULT = "▇ ";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_ItemStack), "Init")]
    public static void InitPostfixProxy(XUiC_ItemStack __instance)
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
				allowClicklock = StringParsers.ParseBool(_sender.CustomAttributes[ALLOW_CLICKLOCK_ATTR]);
			}
			if (!allowClicklock)
				return;

			var backpackWindow = __instance.xui.GetChildByType<XUiC_BackpackWindow>();
			var lootWindow = __instance.xui.GetChildByType<XUiC_LootWindow>();
			var vehicleContainer = __instance.xui.GetChildByType<XUiC_VehicleContainer>();

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
			PlayClickSound();
		};
    }

    private static void PlayClickSound()
    {
        // 播放缓存音效
        if (_cachedClickSound != null)
        {
            Manager.PlayXUiSound(_cachedClickSound, .75f);
            return;
        }

        // 加载音效，缓存并播放
        LoadManager.LoadAsset<AudioClip>(UI_CLICK_SOUND_PATH, clip =>
        {
            if (clip != null)
            {
                _cachedClickSound = clip;
                Manager.PlayXUiSound(clip, .75f);
            }
            else
            {
                Debug.LogError("<color=#FF0000>CATUI [AltClickPatch] Failed to load UI click sound</color>");
            }
        });
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

            // 道具插槽状态
            case "CATUI_itemStackModifications":
                _value = "";
                if (__instance?.itemStack?.itemValue == null)
                {
                    Debug.Log($"<color=#00FF00>__instance?.itemStack?.itemValue == null </color>");
                    __result = true;
                    return false;
                }

                // 取View上的属性
                __instance.CustomAttributes.TryGetValue("mod_highlight", out string highlightStyle);
                __instance.CustomAttributes.TryGetValue("mod_default", out string defaultStyle);
                highlightStyle ??= MODIFICATION_HIGHLIGHTED;
                defaultStyle ??= MODIFICATION_DEFAULT;

                ItemValue itemValue = __instance.itemStack.itemValue;
                ItemValue[] mods = itemValue.Modifications;
                
                // mods数组空值检查
                if (itemValue.Quality <= 0 || mods == null || mods.Length == 0)
                {
                    __result = true;
                    return false;
                }

                System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
                for (int i = 0; i < mods.Length; i++)
                {
                    if (mods[i] == null) continue;

                    var itemClass = mods[i]?.ItemClass;
                    if (itemClass != null && !string.IsNullOrEmpty(itemClass?.GetItemName()))
                    {
                        textBuilder.Append(highlightStyle);
                    }
                    else
                    {
                        textBuilder.Append(defaultStyle);
                    }
                }
                _value = textBuilder.ToString();
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