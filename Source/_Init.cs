// ��������������ռ�
using System.Reflection;
using HarmonyLib;
using UnityEngine;

// ����һ����ΪModStartup�Ĺ����࣬�̳���IModApi�ӿ�
public class ModStartup : IModApi
{
    // ��ʼ������
    public void InitMod(Mod modInstance)
    {
        // ����Assembly-CSharp.dll
        Assembly executeAssembly = Assembly.GetExecutingAssembly();

        // ����Harmonyʵ��
        Harmony harmony = new Harmony(executeAssembly.GetName().Name);

        // �򲹶�
        harmony.PatchAll(executeAssembly);

        // ��������
        ModEvents.GameAwake.RegisterHandler(() =>
        {
            Constants.TrackedFriendColors = new Color[8]
            {
                new Color32(255, 173, 31, byte.MaxValue),
                new Color32(4, 254, 133, byte.MaxValue),
                new Color32(1, 239, 255, byte.MaxValue),
                new Color32(255, 82, 82, byte.MaxValue),
                new Color32(89, 167, 255, byte.MaxValue),
                new Color32(231, 92, 255, byte.MaxValue),
                new Color32(255, 235, 59, byte.MaxValue),
                new Color32(153, 110, 255, byte.MaxValue)
            };
        });
    }
}