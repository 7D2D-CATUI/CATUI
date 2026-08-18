using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiC_BuffPopoutListPatch
{
    // 缓存UILabel组件，避免每帧transform.Find
    private static readonly Dictionary<EntityUINotification, UILabel> cachedLabels = new();
    
    // 缓存上一次的文本，用于变化检测
    private static readonly Dictionary<EntityUINotification, string> cachedTexts = new();
    
    // 上次更新时间
    private static float lastUpdateTime = 0f;
    
    // 更新间隔（秒），约10 FPS
    private const float UpdateInterval = 0.1f;

    // 缓存的滚动视图组件，避免重复GetComponent
    private static UIScrollView cachedScrollView;

    // 上次滚动条刷新时间
    private static float lastScrollbarUpdateTime = 0f;

    // 滚动条刷新间隔（秒），内容增删后滑块高度自动纠正
    private const float ScrollbarUpdateInterval = 1f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_BuffPopoutList), "OnOpen")]
    public static void OnOpenPostfix(XUiC_BuffPopoutList __instance)
    {
        // 清理缓存
        cachedLabels.Clear();
        cachedTexts.Clear();
        cachedScrollView = null;
        lastScrollbarUpdateTime = 0f;
        
        if (__instance.items != null && __instance.items.Count > 0)
        {
            UpdateBuffPositions(__instance);
            PreCacheLabels(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_BuffPopoutList), "AddNotification")]
    public static void AddNotificationPostfix(XUiC_BuffPopoutList __instance, EntityUINotification _notification)
    {
        if (__instance.items != null && __instance.items.Count > 0)
        {
            var lastItem = __instance.items[__instance.items.Count - 1];
            if (lastItem != null && lastItem.Notification == _notification)
            {
                // 缓存UILabel组件
                UILabel textName = CacheLabel(lastItem.Item, _notification);
                if (textName != null && _notification.Buff != null)
                {
                    // 使用BindingsManager.ReplaceCVars解析LocalizedName中的{cvar(...)}占位符
                    string newText = BindingsManager.ReplaceCVars(_notification.Buff.BuffClass.LocalizedName);
                    UpdateTextIfChanged(_notification, textName, newText);
                }
                
                UISprite decoration = lastItem.Item.transform.Find("Decoration").GetComponent<UISprite>();
                if (decoration != null)
                {
                    decoration.color = _notification.GetColor();
                }
            }
            UpdateBuffPositions(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_BuffPopoutList), "Update")]
    public static void UpdatePostfix(XUiC_BuffPopoutList __instance)
    {
        float currentTime = Time.time;

        // 按1秒间隔刷新滚动条，纠正内容增删后滑块高度不重算的问题
        if (currentTime - lastScrollbarUpdateTime >= ScrollbarUpdateInterval)
        {
            lastScrollbarUpdateTime = currentTime;
            RefreshScrollbars(__instance);
        }

        // 限制更新频率，避免每帧计算
        if (currentTime - lastUpdateTime < UpdateInterval)
        {
            return;
        }
        lastUpdateTime = currentTime;
        
        if (__instance.items != null && __instance.items.Count > 0)
        {
            foreach (var data in __instance.items)
            {
                if (data.Notification != null && data.Notification.Buff != null && !data.Notification.Buff.Paused)
                {
                    // 使用缓存的UILabel
                    UILabel textName = GetOrCacheLabel(data.Item, data.Notification);
                    if (textName != null)
                    {
                        string newText = BindingsManager.ReplaceCVars(data.Notification.Buff.BuffClass.LocalizedName);
                        UpdateTextIfChanged(data.Notification, textName, newText);
                    }
                }
            }
            
            // 清理已移除的通知缓存
            CleanupRemovedNotifications(__instance);
        }
    }
    
    /// <summary>
    /// 刷新滚动条，使滑块高度与当前内容边界一致。
    /// NGUI 的 UIScrollView.bounds 有 mCalculatedBounds 缓存，
    /// 内容减少后不会自动失效，需要显式调用 UpdateScrollbars() 重算。
    /// </summary>
    private static void RefreshScrollbars(XUiC_BuffPopoutList __instance)
    {
        if (__instance == null || __instance.Parent == null || __instance.Parent.ViewComponent == null)
        {
            return;
        }

        if (cachedScrollView == null)
        {
            cachedScrollView = __instance.Parent.ViewComponent.UiTransform.GetComponent<UIScrollView>();
        }

        if (cachedScrollView != null)
        {
            cachedScrollView.UpdateScrollbars();
        }
    }

    /// <summary>
    /// 清理已移除的通知缓存
    /// </summary>
    private static void CleanupRemovedNotifications(XUiC_BuffPopoutList __instance)
    {
        var activeNotifications = new HashSet<EntityUINotification>();
        foreach (var data in __instance.items)
        {
            if (data.Notification != null)
            {
                activeNotifications.Add(data.Notification);
            }
        }
        
        // 移除不再活跃的通知缓存
        var labelsToRemove = new List<EntityUINotification>();
        var textsToRemove = new List<EntityUINotification>();
        
        foreach (var key in cachedLabels.Keys)
        {
            if (!activeNotifications.Contains(key))
            {
                labelsToRemove.Add(key);
            }
        }
        
        foreach (var key in cachedTexts.Keys)
        {
            if (!activeNotifications.Contains(key))
            {
                textsToRemove.Add(key);
            }
        }
        
        foreach (var key in labelsToRemove)
        {
            cachedLabels.Remove(key);
        }
        
        foreach (var key in textsToRemove)
        {
            cachedTexts.Remove(key);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_BuffPopoutList), "updateEntries")]
    public static bool updateEntriesPrefix(XUiC_BuffPopoutList __instance)
    {
        UpdateBuffPositions(__instance);
        return false;
    }
    
    /// <summary>
    /// 预缓存所有条目的UILabel组件
    /// </summary>
    private static void PreCacheLabels(XUiC_BuffPopoutList __instance)
    {
        foreach (var data in __instance.items)
        {
            if (data.Notification != null)
            {
                CacheLabel(data.Item, data.Notification);
            }
        }
    }
    
    /// <summary>
    /// 缓存UILabel组件
    /// </summary>
    private static UILabel CacheLabel(GameObject item, EntityUINotification notification)
    {
        if (!cachedLabels.TryGetValue(notification, out UILabel label))
        {
            label = item.transform.Find("TextName")?.GetComponent<UILabel>();
            if (label != null)
            {
                cachedLabels[notification] = label;
            }
        }
        return label;
    }
    
    /// <summary>
    /// 获取或缓存UILabel组件
    /// </summary>
    private static UILabel GetOrCacheLabel(GameObject item, EntityUINotification notification)
    {
        if (!cachedLabels.TryGetValue(notification, out UILabel label) || label == null)
        {
            label = item.transform.Find("TextName")?.GetComponent<UILabel>();
            if (label != null)
            {
                cachedLabels[notification] = label;
            }
        }
        return label;
    }
    
    /// <summary>
    /// 仅在文本变化时更新，避免不必要的UI刷新
    /// </summary>
    private static void UpdateTextIfChanged(EntityUINotification notification, UILabel label, string newText)
    {
        if (!cachedTexts.TryGetValue(notification, out string oldText) || oldText != newText)
        {
            label.text = newText;
            cachedTexts[notification] = newText;
        }
    }
    
    /// <summary>
    /// 更新Buff位置
    /// </summary>
    private static void UpdateBuffPositions(XUiC_BuffPopoutList __instance)
    {
        int visibleCount = 0;
        for (int i = 0; i < __instance.items.Count; i++)
        {
            var data = __instance.items[i];
            if (!data.Notification.Buff.Paused)
            {
                TweenPosition component = data.Item.GetComponent<TweenPosition>();
                if ((bool)component)
                {
                    Object.Destroy(component);
                }
                Vector3 currentPos = data.Item.transform.localPosition;
                float targetY = (float)visibleCount * __instance.height + __instance.yOffset;
                data.Item.transform.localPosition = new Vector3(currentPos.x, -targetY, currentPos.z);
                visibleCount++;
            }
        }
    }
}
