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
        string _nodeNameOverride = "", Dictionary<string, object> _templateParams = null)
    {
        string localName = _node.Name.LocalName;
        string id = localName;

        if (_nodeNameOverride == "" && _node.HasAttribute("name"))
        {
            id = _node.GetAttribute("name");
        }
        else if (_nodeNameOverride != "")
        {
            id = _nodeNameOverride;
        }

        if (_templateParams != null)
        {
            XUiFromXmlReversePatch.parseControlParams(_node, _parent, _templateParams);
        }

        XUiView view = null;

        switch (localName)
        {
            case "CATUI_animatedsprite":
                view = new XUiV_AnimatedSprite(_windowGroup.xui, id);
                break;
            case "CATUI_roundedtexture":
                view = new XUiV_RoundedTexture(_windowGroup.xui, id);
                break;
        }

        if (view != null)
        {
            SetControllerAndParseAttributes(_node, view, _parent, _windowGroup, _templateParams);

            foreach (XElement childNode in _node.Elements())
            {
                XUiFromXmlReversePatch.parseViewComponents(childNode, _windowGroup, view.Controller, _templateParams: _templateParams);
            }

            __result = view;

            return false;
        }

        return true;
    }

    private static void SetControllerAndParseAttributes(XElement node, XUiView view, XUiController parent, XUiWindowGroup windowGroup, Dictionary<string, object> controlParams)
    {
        view.Controller = XUiFromXmlReversePatch.parseController(node, windowGroup.xui, windowGroup, parent);
        view.SetDefaults(parent);
        XUiFromXmlReversePatch.parseAttributes(node, view, controlParams);
        view.SetPostParsingDefaults(parent);
    }
}

[HarmonyPatch(typeof(XUiFromXml))]
public class XUiFromXmlReversePatch
{
    private const string TAG = "Error Reverse Patching XUiFromXML method: ";

    [HarmonyReversePatch]
    [HarmonyPatch("parseParams")]
    public static void parseControlParams(XElement _node, XUiController _parent, Dictionary<string, object> _templateParams)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "parseControlParams");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("parseAttributes")]
    public static void parseAttributes(XElement _node, XUiView _viewComponent,
        Dictionary<string, object> _templateParams = null)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "parseAttributes");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("parseController")]
    public static XUiController parseController(XElement _node, XUi _xui, XUiWindowGroup _windowGroup, XUiController _parent)
    {
        // its a stub so it has no initial content
        throw new NotImplementedException(TAG + "parseController");
    }

    [HarmonyReversePatch]
    [HarmonyPatch("parseViewComponents")]
    public static XUiView parseViewComponents(XElement _node, XUiWindowGroup _windowGroup, XUiController _parent = null,
        string _nodeNameOverride = "", Dictionary<string, object> _templateParams = null)
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
