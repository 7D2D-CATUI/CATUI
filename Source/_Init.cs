// 引入所需的命名空间
using System.Reflection;
using HarmonyLib;
using UnityEngine;

// ============================================================================
// 注意：CATUI_local_load 联机配置叠加功能已集成到本程序集（LocalLoadPatch.cs）。
// 如果旧版本还安装了独立的 ZZZ_CATUI_local_load 模组目录，请务必删除它，
// 否则会出现"同一逻辑被 Harmony 重复打补丁"的问题（叠加逻辑重复执行）。
// 该功能的启用/关闭开关见 LocalLoadPatch.cs 顶部的 EnableLocalLoad 字段。
// ============================================================================

// 定义一个名为ModStartup的公共类，继承自IModApi接口
public class ModStartup : IModApi
{
    // 初始化方法
    public void InitMod(Mod modInstance)
    {
        // 加载 Assembly-CSharp.dll
        Assembly executeAssembly = Assembly.GetExecutingAssembly();

        // 读取 ModSettings.xml 配置（在打补丁前应用，LocalLoad 开关从这里获取）
        CatuiSettings.Load();

        // 创建Harmony实例
        Harmony harmony = new Harmony(executeAssembly.GetName().Name);

        // 打补丁
        harmony.PatchAll(executeAssembly);

        Debug.Log("<color=#00FF00>CATUI Applied.</color>");

        // 常量补丁(组队队友的颜色)
        ModEvents.GameAwake.RegisterHandler((ref ModEvents.SGameAwakeData _data) =>
        {
            // 刷新 ModSettings.xml 配置（Gears 的存档在启动早期可能尚未生成，这里再读一次）
            CatuiSettings.Load();

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
