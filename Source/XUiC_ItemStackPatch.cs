using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public class XUiC_ItemStackPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(XUiC_ItemStack), "GetBindingValue")]
	public static bool Prefix(string _bindingName, ref string _value, ref bool __result, XUiC_ItemStack __instance)
	{
		switch (_bindingName)
		{
			// ����������index
			case "CATUI_itemStackSlotIndex":
				_value = (__instance.SlotNumber).ToString();
				__result = true;
				return false;
			// ��Ʒ�ʵĵ��� ���״̬ ����ģʽ��bug
			case "CATUI_itemStackModifications":
				_value = "";
				ItemValue itemValue = __instance.itemStack.itemValue;
				ItemValue[] mods = itemValue.Modifications;
				if (itemValue.Quality > 0 && mods != null && mods.Length > 0) {
					string text = "";
					for (int i = 0; i < mods.Length; i++)
					{
						ItemClass itemClass = mods[i].ItemClass;
						if (itemClass != null && itemClass.GetItemName() != "") {
							text += "[FFC300]�~[-] ";
						}
						else
						{
							text += "�~ ";
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
}
