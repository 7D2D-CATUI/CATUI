using System;
using Audio;
using HarmonyLib;
using UnityEngine;
using System.Xml.Linq;

[HarmonyPatch]
public class XUiC_ItemStackPatch
{
	private const string UI_CLICK_SOUND_PATH = "@:Sounds/UI/ui_menu_click.wav";
	private static AudioClip _cachedClickSound;
 	private const string ALLOW_CLICKLOCK_ATTR = "allow_clicklock";

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_ItemStack), "GetBindingValue")]
	public static bool Prefix(string _bindingName, ref string _value, ref bool __result, XUiC_ItemStack __instance)
	{
		switch (_bindingName)
		{
			// 给道具增加index
			case "CATUI_itemStackSlotIndex":
				_value = (__instance.SlotNumber).ToString();
				__result = true;
				return false;
			// 有品质的道具 插槽状态 创造模式有bug
			/*case "CATUI_itemStackModifications":
				_value = "";
				ItemValue itemValue = __instance.itemStack.itemValue;
				ItemValue[] mods = itemValue.Modifications;
				if (itemValue.Quality > 0 && mods != null && mods.Length > 0) {
					string text = "";
					for (int i = 0; i < mods.Length; i++)
					{
						ItemClass itemClass = mods[i].ItemClass;
						if (itemClass != null && itemClass.GetItemName() != "") {
							text += "[FFC300]▇[-] ";
						}
						else
						{
							text += "▇ ";
						}
					}
					_value = text;
				}
				__result = true;
				return false;*/
			default:
				return true;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(XUiC_ItemStack), "Init")]
	public static void InitPostfixProxy(XUiC_ItemStack __instance)
    {
		try
		{
			__instance.OnPress += (XUiController _sender, int _mouseButton) =>
			{
				bool allowClicklock = false;
				if (_sender.CustomAttributes.ContainsKey(ALLOW_CLICKLOCK_ATTR)) {
					allowClicklock = StringParsers.ParseBool(_sender.CustomAttributes[ALLOW_CLICKLOCK_ATTR]);
				}
				// 是否允许栏位锁（配置写在xml上，allow_clicklock="${allow_clicklock}"）
				if (!allowClicklock) return;
				// 是否ALT+点击触发
				if (!InputUtils.AltKeyPressed) return;

				var backpackWindow = __instance.xui.GetChildByType<XUiC_BackpackWindow>();
                var lootWindow = __instance.xui.GetChildByType<XUiC_LootWindow>();
                var vehicleContainer = __instance.xui.GetChildByType<XUiC_VehicleContainer>();
				if (backpackWindow != null || lootWindow != null || vehicleContainer != null)
                {
					__instance.UserLockedSlot = !__instance.UserLockedSlot;
                    __instance.RefreshBindings();
                    // 背包 更新栏位锁状态
                    backpackWindow.UpdateLockedSlots(backpackWindow.standardControls);
					// 箱子 容器窗口是否打开 更新栏位锁状态 2.2+
					if (lootWindow.IsOpen && lootWindow.standardControls != null) {
						lootWindow.UpdateLockedSlots(lootWindow.standardControls);
					}
					// 载具 容器窗口是否打开 更新栏位锁状态 2.2+
					if (vehicleContainer.IsOpen && vehicleContainer.standardControls != null)
					{
						vehicleContainer.UpdateLockedSlots(vehicleContainer.standardControls);
					}
					PlayClickSound();
                }
            };
		}
		catch (System.Exception ex)
		{
			Debug.Log("<color=#FF9900> CATUI [AltClickPatch] Failed to add event handler: " + ex.Message + "</color>");
		}
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
}
