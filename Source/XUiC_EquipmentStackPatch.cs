using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_EquipmentStackPatch
{
	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterXuiRgbaColor durabilityColorFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterFloat durabilityFillFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterFloat durabilityRemoveFillFormatter = new();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiController), "GetBindingValueInternal")]

	public static bool GetBindingValueInternalPrefix(string _bindingName, ref string _value, ref bool __result, XUiController __instance)
	{
		if (__instance is not XUiC_EquipmentStack itemStack)
			return true;

		switch (_bindingName)
		{
			case "CATUI_durabilityColor":
				_value = "0,0,0,0";
				if (itemStack != null && itemStack.itemValue != null)
				{
					Color32 v = QualityInfo.GetQualityColor(itemStack.itemValue.Quality);
					_value = durabilityColorFormatter.Format(v);
				}
				__result = true;
				return false;

			case "CATUI_durabilityFill":
				_value = "0";
				if (itemStack != null)
				{
					_value = itemStack?.itemValue == null ? "0.0" : itemStack.itemValue.MaxUseTimes == 0 ? "1" : durabilityFillFormatter.Format((float)(itemStack.itemValue.MaxUseTimes - itemStack.itemValue.UseTimes) / itemStack.itemValue.MaxUseTimesUI);
				}
				__result = true;
				return false;

			case "CATUI_durabilityRemoveFill":
				_value = "1";
				if (itemStack != null && itemStack.itemValue != null)
				{
					_value = durabilityRemoveFillFormatter.Format(itemStack.itemValue.MaxDurabilityModifier);
				}
				__result = true;
				return false;

			case "CATUI_itemType":
				_value = "0";
				if (itemStack != null && itemStack.itemValue != null)
				{
					_value = itemStack.SlotNumber.ToString();
				}
				__result = true;
				return false;

			case "CATUI_hasQuality":
				_value = "false";
				if (itemStack != null && itemStack.itemValue != null)
				{
					_value = itemStack.itemValue.HasQuality.ToString();
				}
				__result = true;
				return false;

			default:
				return true;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_EquipmentStack), "Update")]
	public static void Prefix(XUiC_EquipmentStack __instance)
	{
		if (__instance.IsDirty)
		{
			__instance.RefreshBindings();
		}
	}

}
