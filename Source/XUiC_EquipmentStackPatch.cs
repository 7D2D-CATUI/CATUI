using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_EquipmentStackPatch
{
	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterXuiRgbaColor durabilityColorFormatter = new();

	[PublicizedFrom(EAccessModifier.Private)]
	public static CachedStringFormatterFloat durabilityFillFormatter = new();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiController), "GetBindingValue")]
	public static bool Prefix(string _bindingName, ref string _value, ref bool __result, XUiController __instance)
	{
		XUiC_EquipmentStack itemStack = __instance as XUiC_EquipmentStack;
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
				if (itemStack != null && itemStack.itemValue != null)
				{
					_value = durabilityFillFormatter.Format(itemStack.itemValue.PercentUsesLeft);
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
	public static void Prefix(XUiController __instance, bool ___isDirty)
	{
		if (___isDirty)
		{
			__instance.RefreshBindings();
		}
	}

}
