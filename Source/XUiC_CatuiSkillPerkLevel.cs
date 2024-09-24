using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
class XUiC_CATUI_Hover : XUiController
{
	private XUiV_Sprite CATUI_EL_hover_bg;
	private string default_hover_color;
	private string hover_color;

	public override void Init()
    {
        base.Init();
		CATUI_EL_hover_bg = (XUiV_Sprite)base.GetChildById("CATUI_EL_hover_bg").ViewComponent;
	}
	public override bool ParseAttribute(string _name, string _value, XUiController _parent)
	{
		switch (_name)
		{
			case "default_hover_color":
				default_hover_color = _value;
				return true;
			case "hover_color":
				hover_color = _value;
				return true;
			default:
				return base.ParseAttribute(_name, _value, _parent);
		}
	}

	// Hover 换Sprite name=background 的背景颜色
	public override void OnHovered(bool _isOver)
    {
		base.OnHovered(_isOver);

		// 背景变色
		if (CATUI_EL_hover_bg != null)
		{
			if (_isOver)
			{
				CATUI_EL_hover_bg.Color = StringParsers.ParseColor32(hover_color);
			}
			else
			{
				CATUI_EL_hover_bg.Color = StringParsers.ParseColor32(default_hover_color);
			}
		}

	}
}
