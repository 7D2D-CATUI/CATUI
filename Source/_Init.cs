// 引入所需的命名空间
using System.Reflection;
using HarmonyLib;
using UnityEngine;

// 定义一个名为ModStartup的公共类，继承自IModApi接口
public class ModStartup : IModApi
{
    // 初始化方法
    public void InitMod(Mod modInstance)
    {
        // 加载Assembly-CSharp.dll
        Assembly executeAssembly = Assembly.GetExecutingAssembly();

        // 创建Harmony实例
        Harmony harmony = new Harmony(executeAssembly.GetName().Name);

        // 打补丁
        harmony.PatchAll(executeAssembly);

        // 常量补丁
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