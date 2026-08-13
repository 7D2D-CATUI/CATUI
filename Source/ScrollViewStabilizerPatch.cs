using System.Reflection;
using HarmonyLib;
using UnityEngine;

/// <summary>
/// 稳定滚动面板的裁剪偏移。
///
/// 原理：
/// NGUI 的 UIScrollView 会在每帧更新时按其滚动状态改写其所在 UIPanel 的
/// clipOffset 以及自身 transform 的 localPosition，而 XUi 系统又会周期性调用
/// XUiV_Panel.updateClipping()，把裁剪区域重新写回 UIPanel。当滚动视图同时被
/// 两边驱动时会产生抖动/回弹。
///
/// 该补丁在 updateClipping() 执行前记录面板当前的 clipOffset 与 localPosition，
/// 执行后立即还原，从而屏蔽掉 XUi 这一侧的覆盖，保证滚动偏移稳定。
/// 仅对挂载了 UIScrollView 组件的面板生效。
/// </summary>
[HarmonyPatch(typeof(XUiV_Panel), "updateClipping")]
public static class ScrollViewStabilizerPatch
{
	/// <summary>Prefix 传给 Postfix 的滚动面板状态。</summary>
	public struct ScrollState
	{
		/// <summary>是否得到有效状态（只有可滚动画板才为 true）。</summary>
		public bool Active;

		/// <summary>执行 updateClipping 前的面板裁剪偏移。</summary>
		public Vector2 ClipOffset;

		/// <summary>执行 updateClipping 前的面板自身位置。</summary>
		public Vector3 LocalPosition;
	}

	/// <summary>XUiV_Panel.panel 字段（NGUI UIPanel）的缓存引用。</summary>
	private static readonly FieldInfo PanelField = AccessTools.Field(typeof(XUiV_Panel), "panel");

	/// <summary>XUiView.uiTransform 字段（面板 Transform）的缓存引用。</summary>
	private static readonly FieldInfo UiTransformField = AccessTools.Field(typeof(XUiView), "uiTransform");

	/// <summary>
	/// 在原 updateClipping() 执行前记录滚动面板的裁剪偏移与自身位置。
	/// 仅当面板挂载了 UIScrollView 组件时才记录，否则不启用还原。
	/// </summary>
	[HarmonyPrefix]
	public static void Prefix(XUiV_Panel __instance, out ScrollState __state)
	{
		__state = default(ScrollState);
		if (UiTransformField == null || PanelField == null)
		{
			Log.Warning("[CATUI] ScrollViewStabilizerPatch: panel/uiTransform fields not found, patch disabled");
			return;
		}

		var uiTransform = (Transform)UiTransformField.GetValue(__instance);
		if (uiTransform == null || uiTransform.GetComponent<UIScrollView>() == null)
		{
			return;
		}

		var panel = (UIPanel)PanelField.GetValue(__instance);
		if (panel != null)
		{
			__state = new ScrollState
			{
				Active = true,
				ClipOffset = panel.clipOffset,
				LocalPosition = uiTransform.localPosition
			};
		}
	}

	/// <summary>
	/// 在原 updateClipping() 执行后把裁剪偏移与自身位置还原为记录值，
	/// 抵消 XUi 对该滚动面板的裁剪覆盖。
	/// </summary>
	[HarmonyPostfix]
	public static void Postfix(XUiV_Panel __instance, ScrollState __state)
	{
		if (!__state.Active || UiTransformField == null || PanelField == null)
		{
			return;
		}

		var uiTransform = (Transform)UiTransformField.GetValue(__instance);
		var panel = (UIPanel)PanelField.GetValue(__instance);
		if (uiTransform == null || panel == null)
		{
			return;
		}

		panel.clipOffset = __state.ClipOffset;
		uiTransform.localPosition = __state.LocalPosition;
	}
}