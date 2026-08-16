using NCalc;

/// <summary>
/// CATUI 自定义 NCalc 函数注册类。
///
/// 7D2D 原版机制：游戏在首次创建 NCalc binding 时（BindingsManager.CreateBinding
/// → BindingNcalcFunctions.RegisterNcalcFunctions）通过反射扫描所有已加载程序集
/// （Assembly-CSharp + 全部 mod dll）中带 [XuiBindingNcalcFunction] 的类与方法，
/// 自动注册为可在 XUi binding 表达式中调用的函数。
/// 因此只需在本程序集内提供符合规范的方法即可被原版识别，无需额外初始化代码。
/// </summary>
[XuiBindingNcalcFunction]
public static class CatuiNcalcFunctions
{
	// CATUI_numprefix —— 提取字符串开头的数字前缀为整数。
	//
	// 声明说明：
	//   构造参数 (0, 1, "CATUI_numprefix")
	//     - 0            参数求值失败/为空时的兜底返回值（ErrorResult）
	//     - 1            期望参数个数
	//     - "CATUI_numprefix"  函数名（加 CATUI 前缀，避免与 7D2D 原版
	//                          及第三方 mod 的 NCalc 函数同名冲突）
	//
	// 使用方法（XUi XML binding 内调用，如 templates.xml 的制作需求数量比较）：
	//   {# (CATUI_numprefix(havecount) >= CATUI_numprefix(needcount) ? '[43CF7C]' : '[FF5252]') + havecount + '[-]' }/{needcount}
	//
	// 与 NCalc 内置 Floor() 的区别：
	//   Floor("5 个") 会把"5 个"按 double 转换失败并抛异常，导致整条 binding 求值失败、
	//   UI 显示空白（叠加第三方 mod 后 havecount/needcount 常带额外文本，此问题频发）；
	//   CATUI_numprefix("5 个") 则安全返回 5，对空字符串或纯文本返回 0，永不抛异常。
	[XuiBindingNcalcFunction(0, 1, "CATUI_numprefix")]
	public static void CATUI_numprefix(IBindingNcalc _bindingInstance, FunctionArgs _args, object[] _argv)
	{
		// 取参数的字符串形式，null 按空串处理（正常情况下参数已被原版评估为非 null）
		string s = _argv[0]?.ToString() ?? "";
		// 从开头累加连续数字字符，直到遇到第一个非数字；避免 int.Parse 的溢出/格式异常
		int value = 0;
		int i = 0;
		while (i < s.Length && char.IsDigit(s[i]))
		{
			value = value * 10 + (s[i] - '0');
			i++;
		}
		// 没有任何数字前缀（空串/纯文本）时返回 0
		_args.Result = i > 0 ? value : 0;
	}
}
