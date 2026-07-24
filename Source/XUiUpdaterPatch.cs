using HarmonyLib;
using UnityEngine;
using System;
using UnityEngine.Scripting;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiUpdaterPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiUpdater), "Update")]
    public static void UpdatePostfix()
    {
        // 检测ALT+B按键组合
        if (InputUtils.AltKeyPressed && Input.GetKeyDown(KeyCode.B))
        {
            // 获取本地玩家
            EntityPlayerLocal localPlayer = GameManager.Instance.World.GetPrimaryPlayer() as EntityPlayerLocal;
            if (localPlayer == null)
                return;

            // 查找玩家的无人机
            EntityDrone playerDrone = FindPlayerDrone(localPlayer);
            if (playerDrone != null)
            {
                // 打开无人机背包
                playerDrone.openStorageWindow(localPlayer.playerUI);
            }
        }

        // 检测ALT+RMB按键组合
        // if (InputUtils.AltKeyPressed && Input.GetMouseButtonDown(1))
        // 鼠标侧键返回键触发
        if (Input.GetKeyDown(KeyCode.Mouse3))
        {
            // 获取本地玩家
            EntityPlayerLocal localPlayer = GameManager.Instance.World.GetPrimaryPlayer() as EntityPlayerLocal;
            if (localPlayer == null)
                return;

            // 获取UI系统
            XUi xui = localPlayer.playerUI.xui;
            if (xui == null)
                return;

            // 尝试通过窗口组查找容器控件
            XUiC_ContainerStandardControls containerControls = null;
            XUiC_BackpackWindow backpackWindow = xui.GetChildByType<XUiC_BackpackWindow>();
            XUiC_LootWindow lootWindow = xui.GetChildByType<XUiC_LootWindow>();
            XUiC_BagContainer vehicleContainer = xui.GetChildByType<XUiC_BagContainer>();
            if (backpackWindow != null)
            {
                containerControls = backpackWindow.GetChildByType<XUiC_ContainerStandardControls>();
            }

            // 判断搜刮窗口（搜刮/箱子/无人机等）和载具窗口是否打开
            if (containerControls != null && containerControls?.MoveAllowed != null && (lootWindow.IsOpen || vehicleContainer.IsOpen))
            {
                // Debug.Log("<color=#00FF00>CATUI ContainerControls: " + (containerControls != null ? "Found" : "Not Found") + "</color>");
                // 调用MoveFillAndSmart方法实现自动堆叠
                containerControls.MoveFillAndSmart();
                xui.PlayMenuClickSound();
            }
        }

    }

    // 查找玩家拥有的无人机
    private static EntityDrone FindPlayerDrone(EntityPlayerLocal player)
    {
        // 获取所有活动的无人机
        List<Entity> entities = GameManager.Instance.World.Entities.list;
        foreach (Entity entity in entities)
        {
            if (entity is EntityDrone drone)
            {
                // 检查无人机的所有者是否是当前玩家
                if (drone.Owner?.entityId == player.entityId)
                {
                    return drone;
                }
            }
        }
        return null;
    }
}