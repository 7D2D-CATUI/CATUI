using HarmonyLib;
using System.Linq;
using UnityEngine;

[HarmonyPatch]
public class ConstantsPatch
{
	[HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_CompanionEntry), "Init")]
    [HarmonyPatch(typeof(XUiC_PartyEntry), "Init")]
    public static void Prefix()
	{
        ModEvents.GameAwake.RegisterHandler(() =>
        {
            Color[] TrackedFriendColors = new Color[8]
            {
                new Color32(255, 173, 31, byte.MaxValue),
                new Color32(4, 254, 133, byte.MaxValue),
                new Color32(1, 239, 255, byte.MaxValue),
                new Color32(255, 235, 59, byte.MaxValue),
                new Color32(89, 167, 255, byte.MaxValue),
                new Color32(231, 92, 255, byte.MaxValue),
                new Color32(255, 82, 82, byte.MaxValue),
                new Color32(153, 110, 255, byte.MaxValue)
            };
            Constants.TrackedFriendColors.ToList().AddRange(TrackedFriendColors);
        });
    }


}