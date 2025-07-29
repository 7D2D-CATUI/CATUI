using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Views;

[HarmonyPatch(typeof(XUiFromXml))]
public class XUiFromXmlPatch
{
    private const string TAG = "XUiFromXmlPatch";

    [HarmonyPrefix]
    [HarmonyPatch("parseViewComponents")]
    public static bool parseByElementName(ref XUiView __result,
        XElement _node, XUiController _parent, XUiWindowGroup _windowGroup,
        string nodeNameOverride = "", Dictionary<string, object> _controlParams = null)
    {
        string localName = _node.Name.LocalName;
        string id = localName;

        if (nodeNameOverride == "" && _node.HasAttribute("name"))
        {
            id = _node.GetAttribute("name");
        }
        else if (nodeNameOverride != "")
        {
            id = nodeNameOverride;
        }

        if (_controlParams != null)
        {
            XUiFromXmlReversePatch.parseControlParams(_node, _controlParams);
        }

        XUiView view = null;

        switch(localName)
        {
            case "CATUI_animatedsprite":
                view = new XUiV_AnimatedSprite(id);
                break;
            case "CATUI_scrollview":
                view = new XUiV_ScrollViewContainer(id);
                break;
            case "CATUI_scrollbar":
                view = new XUiV_ScrollBar(id);
                view.xui = _windowGroup.xui;
                XUiFromXmlReversePatch.setController(_node, view, _parent);
                XUiFromXmlReversePatch.parseAttributes(_node, view, _parent, _controlParams);

                view.Controller.WindowGroup = _windowGroup;
                createScrollBarViewComponents(_node, view as XUiV_ScrollBar, _windowGroup, _controlParams);
                __result = view;
                return false;
        }

        if(view != null)
        {
            view.xui = _windowGroup.xui;
            XUiFromXmlReversePatch.setController(_node, view, _parent);
            XUiFromXmlReversePatch.parseAttributes(_node, view, _parent, _controlParams);

            view.Controller.WindowGroup = _windowGroup;

            foreach (XElement childNode in _node.Elements())
            {
                XUiFromXmlReversePatch.parseViewComponents(childNode, _windowGroup, view.Controller, _controlParams: _controlParams);
            }
            
            __result = view;

            return false;
        }

        return true;
    }

    private static void createScrollBarViewComponents(XElement _node, XUiV_ScrollBar view, XUiWindowGroup _windowGroup, Dictionary<string, object> _controlParams = null)
    {
        if (!view.HasXMLChildren)
        {
            return;
        }

        int childCount = _node.Elements().Count<XElement>();
       

        if (childCount > 2)
        {
            XUiFromXmlReversePatch.logForNode(LogType.Log, _node, "[XUi] XUiFromXml::parseByElementName: Invalid scrollbar child count. Must have zero to two child element.");
        }
        else
        {
            foreach (XElement child in _node.Elements())
            {
                ParseScrollBarViewComponents(child, view.Controller, _windowGroup, _controlParams: _controlParams);
            }
        }
    }

    private static void ParseScrollBarViewComponents(XElement node, XUiController parent, XUiWindowGroup windowGroup,
        string nodeNameOverride = "", Dictionary<string, object> _controlParams = null)
    {
        string name = node.Name.LocalName;
        string id = name;

        if (nodeNameOverride == "" && node.HasAttribute("name"))
        {
            id = node.GetAttribute("name");
        }
        else if (nodeNameOverride != "")
        {
            id = nodeNameOverride;
        }

        if (_controlParams != null)
        {
            XUiFromXmlReversePatch.parseControlParams(node, _controlParams);
        }

        XUiView view = null;

        switch (name)
        {
            case "sprite":
                view = new XUiC_Scrollbar_Sprite(id);
                break;
            case "button":
                view = new XUiC_ScrollBar_Button(id);
                break;
        }

        if (view != null)
        {
            view.xui = windowGroup.xui;
            XUiFromXmlReversePatch.setController(node, view, parent);
            XUiFromXmlReversePatch.parseAttributes(node, view, parent, _controlParams);

            view.Controller.WindowGroup = windowGroup;
        }
    }
}

[HarmonyPatch(typeof(XUiFromXml))]
public class XUiFromXmlReversePatch
{
    private const string TAG = "Error Reverse Patching XUiFromXML method: ";

    [HarmonyReversePatch]
    [HarmonyPatch("parseControlParams")]
    public static void parseControlParams(XElement _node, Dictionary<string, object> _controlParams)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "parseControlParams");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("setController")]
    public static void setController(XElement _node, XUiView _viewComponent, XUiController _parent)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "setController");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("parseAttributes")]
    public static void parseAttributes(XElement _node, XUiView _viewComponent, XUiController _parent,
        Dictionary<string, object> _controlParams = null)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "parseAttributes");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("parseViewComponents")]
    public static XUiView parseViewComponents(XElement _node, XUiWindowGroup _windowGroup, XUiController _parent = null, 
        string nodeNameOverride = "", Dictionary<string, object> _controlParams = null)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "parseViewComponents");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("logForNode")]
    public static void logForNode(LogType _level, XElement _node, string _message)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "logForNode");
    }
}
