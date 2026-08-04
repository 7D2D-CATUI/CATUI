using HarmonyLib;
using System;
using System.Collections.Generic;

[HarmonyPatch]
public class BindingNcalcSafetyPatch
{
	private const string TAG = "[CATUI]";

	private static readonly HashSet<string> loggedKeys = new HashSet<string>(StringComparer.Ordinal);

	// BindingItemNcalc.evaluateExpression 在两种情况下都会返回空字符串：
	//  1) 表达式"合法地"返回空结果，例如 slotLockIcon 使用的
	//     {# !defined('userlockmode') ? '' : (userlockedslot ? ... : ...) }
	//     当 userlockmode 未定义时返回 ''，这是设计好的"无图标"含义，必须原样保留；
	//  2) NCalc 求值失败(抛异常)，例如 {# CATUI_GroupEntryLevelFill * 0.8 }
	//     在参数取到非法值/空值时报错，空结果流入 fill 等数值属性解析时
	//     会触发 "Can not parse input ('') into target type System.Single" 刷屏。
	// 这里通过再次探测 Evaluate() 是否抛异常来区分两种情况：
	// 仅当求值失败(抛异常)时把空结果替换为 "0" 实现优雅降级；合法空结果保持不变。
	[HarmonyPatch(typeof(BindingItemNcalc), "evaluateExpression")]
	[HarmonyPostfix]
	public static void EvaluateExpressionPostfix(ref string __result, BindingItemNcalc __instance)
	{
		if (!string.IsNullOrEmpty(__result))
		{
			return;
		}
		bool failed;
		try
		{
			__instance.expression.Evaluate();
			failed = false;
		}
		catch (Exception)
		{
			failed = true;
		}
		if (failed)
		{
			__result = "0";
			LogOnce(__instance);
		}
	}

	private static void LogOnce(BindingItemNcalc binding)
	{
		try
		{
			string hierarchy = "unknown";
			try
			{
				hierarchy = binding.Parent.View.GetXuiHierarchy();
			}
			catch
			{
			}
			string sourceText = binding.SourceText ?? "";
			if (loggedKeys.Add(sourceText + "|" + hierarchy))
			{
				Log.Warning("{0} NCalc binding '{{{1}}}' on '{2}' failed to evaluate (expression error, usually caused by missing or incomplete server XML data). Using '0' as a safe default.", TAG, sourceText, hierarchy);
			}
		}
		catch
		{
		}
	}
}
