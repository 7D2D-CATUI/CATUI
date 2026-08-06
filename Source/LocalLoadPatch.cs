using System;
using System.Collections.Generic;
using System.Xml.Linq;
using HarmonyLib;
using UnityEngine;

/// <summary>
/// CATUI 联机配置叠加模块（由原独立组件 CATUI_local_load 集成而来）。
///
/// 联机进入服务器时：
/// - 服务器正常下发 XUi/qualityinfo 配置 → 保留服务器内容，并把本地 CATUI 系列补丁叠加上去，
///   同时修复服务器配置里常见的双重转义实体问题；
/// - 服务器下发空(0字节)/缺失(null) 配置 → 自动回退到本地文件加载。
///
/// ============================================================================
/// 【功能开关】
/// ============================================================================
/// 默认开启。若您需要关闭本功能（完全恢复原版加载行为），请把下面这一行改为：
///
///     public static readonly bool EnableLocalLoad = false;
///
/// 修改后重新编译即可，无需改动其他代码。
/// ============================================================================
/// </summary>
[HarmonyPatch]
public class LocalLoadPatch
{
	private const string TAG = "[CATUI]";

	// ========================================================================
	// 【功能开关】
	// 默认开启。运行时会从 ZZZ_CATUI/ModSettings.xml 读取 LocalLoadEnabled 覆盖此值：
	//   - 有 Gears 模组时：可通过游戏内 Gears 设置界面（GUI）开关；
	//   - 无 Gears 模组时：可手动修改 ZZZ_CATUI/ModSettings.xml 中的 LocalLoadEnabled。
	// 若您希望永久关闭（无视配置文件），把下面的 true 改为 false 后重新编译即可。
	// ========================================================================
	public static bool EnableLocalLoad = true;

	// 记录因服务器未下发/下发损坏而回退到本地加载的配置文件名
	public static readonly List<string> LocallyLoadedConfigs = new List<string>();

	private static bool summaryLogged;

	// 本次连接中从服务器正常收到、需要叠加本地 CATUI 补丁的配置名
	private static readonly HashSet<string> receivedOverlayConfigs = new HashSet<string>();

	[HarmonyPatch(typeof(WorldStaticData))]
	public class LocalLoadPatch_WorldStaticData
	{
		[HarmonyPostfix]
		[HarmonyPatch("ReceivedConfigFile")]
		public static void ReceivedConfigFile(string _name, byte[] _data)
		{
			if (!EnableLocalLoad)
			{
				return;
			}
			if (_name == null || !(_name.Contains("XUi") || _name.Contains("qualityinfo")))
			{
				return;
			}

			// 服务器下发了空数据：直接回退到本地加载，避免加载到空 UI 配置
			if (_data != null && _data.Length == 0)
			{
				Log.Warning("{0} Server sent an EMPTY config for '{1}'. This usually means the server's XML config is missing or incomplete. Falling back to local file.", TAG, _name);
				TrackLocalLoad(_name);
				SetLoadLocal(_name);
				return;
			}

			// 服务器未下发该配置（_data == null）：回退到本地加载
			if (_data == null)
			{
				Log.Warning("{0} Server did NOT provide config '{1}'. Falling back to local file.", TAG, _name);
				TrackLocalLoad(_name);
				SetLoadLocal(_name);
				return;
			}

			// 正常下发：保留服务器内容，标记为待叠加本地 CATUI 补丁
			receivedOverlayConfigs.Add(_name);
			Debug.Log("<color=#00FF00>CATUI local load xml name: </color>" + _name);
		}

		// 所有配置接收完成后，汇总输出一份"服务器缺失/回退本地"清单，便于定位问题
		[HarmonyPostfix]
		[HarmonyPatch("AllConfigsReceivedAndLoaded")]
		public static void AllConfigsReceivedAndLoaded(ref bool __result)
		{
			if (!EnableLocalLoad)
			{
				return;
			}
			if (__result && !summaryLogged && LocallyLoadedConfigs.Count > 0)
			{
				summaryLogged = true;
				Log.Warning("{0} XML fallback summary: the following server configs were missing/incomplete and were loaded from local files instead: {1}", TAG, string.Join(", ", LocallyLoadedConfigs.ToArray()));
			}
		}

		// 服务器配置加载管线：在收到配置、执行 LoadMethod 之前，把本地 CATUI 补丁叠加到服务器下发的文档上。
		// 既保留服务器端其他 UI 定制，又保证客户端本地 CATUI 界面完整。
		// 该方法是普通方法（内部仅 yield break），可被 Harmony 直接打补丁；两条加载路径都会经过它，
		// 通过 receivedOverlayConfigs 一次性消费来区分"服务器下发"与"本地加载"。
		[HarmonyPrefix]
		[HarmonyPatch(typeof(XmlPatcher), "ApplyConditionalXmlBlocks")]
		public static bool ApplyConditionalXmlBlocks(string _xmlName, XmlFile _xmlFile, XmlPatcher.EEvaluator _evaluator)
		{
			if (!EnableLocalLoad)
			{
				return true;
			}
			if (_evaluator != XmlPatcher.EEvaluator.Client)
			{
				return true;
			}
			if (_xmlName == null || (!_xmlName.Contains("XUi") && !_xmlName.Contains("qualityinfo")))
			{
				return true;
			}
			// 只处理本次从服务器收到的配置（Remove 一次性消费），本地加载路径不受影响
			if (!receivedOverlayConfigs.Remove(_xmlName))
			{
				return true;
			}
			try
			{
				// 修复服务器配置里的双重转义实体（如 &amp;gt; → 字面 &gt;），
				// 避免 NCalc 把 &gt; 解析成"& 运算符 + gt 标识符"导致求值报错
				SanitizeBindingEntities(_xmlFile);
				ApplyLocalCatuiPatch(_xmlName, _xmlFile);
			}
			catch (Exception e)
			{
				Log.Error("{0} Failed to overlay local CATUI patch for '{1}': {2}", TAG, _xmlName, e);
			}
			return true;
		}

		// 修复服务器 XML 中把比较运算符写成双重转义实体的问题：
		// 服务器文件里写 visible="... &amp;gt;= ..." 时，解析后属性值仍是字面 "&gt;"，
		// NCalc 会把它解析成 "& 运算符 + gt 标识符"，导致 "Parameter was not defined: gt"。
		// 这里对含 '{' 的绑定属性值做实体还原，最多 3 轮以处理多层转义。
		private static void SanitizeBindingEntities(XmlFile _xmlFile)
		{
			if (_xmlFile?.XmlDoc?.Root == null)
			{
				return;
			}
			foreach (XElement element in _xmlFile.XmlDoc.Root.DescendantsAndSelf())
			{
				foreach (XAttribute attribute in element.Attributes())
				{
					string value = attribute.Value;
					if (string.IsNullOrEmpty(value) || value.IndexOf('{') < 0)
					{
						continue;
					}
					string cleaned = value;
					for (int pass = 0; pass < 3; pass++)
					{
						string next = cleaned.Replace("&gt;", ">")
							.Replace("&lt;", "<")
							.Replace("&quot;", "\"")
							.Replace("&apos;", "'")
							.Replace("&amp;", "&");
						if (next == cleaned)
						{
							break;
						}
						cleaned = next;
					}
					if (cleaned != value)
					{
						attribute.Value = cleaned;
					}
				}
			}
		}

		private static void ApplyLocalCatuiPatch(string _xmlName, XmlFile _xmlFile)
		{
			foreach (Mod loadedMod in ModManager.GetLoadedMods())
			{
				if (!IsCatuiMod(loadedMod))
				{
					continue;
				}
				string text = loadedMod.Path + "/Config/" + _xmlName + ".xml";
				if (!SdFile.Exists(text))
				{
					continue;
				}
				XmlFile patch = XmlPatcher.ReadPatchXmlWithFixedModFolders(loadedMod, text);
				if (patch == null)
				{
					continue;
				}
				Log.Out("{0} Overlaying local CATUI config '{1}' from mod '{2}' onto server config.", TAG, _xmlName, loadedMod.Name);
				XmlPatcher.PatchXml(_xmlFile, patch.XmlDoc.Root, patch, loadedMod);
			}
		}

		// 只叠加 CATUI 系列模组（含 CATUI_backpack_91slot、CATUI_toolbelt_more_slot 等配套），
		// 避免把客户端与服务器共用的其他 Mod 重复叠加导致重复插入
		private static bool IsCatuiMod(Mod _mod)
		{
			return (_mod.Name != null && _mod.Name.StartsWith("CATUI", StringComparison.OrdinalIgnoreCase)) ||
				(_mod.DisplayName != null && _mod.DisplayName.StartsWith("CATUI", StringComparison.OrdinalIgnoreCase));
		}

		private static void SetLoadLocal(string _name)
		{
			WorldStaticData.XmlLoadInfo[] xmlsToLoad = WorldStaticData.xmlsToLoad;
			foreach (WorldStaticData.XmlLoadInfo xmlLoadInfo in xmlsToLoad)
			{
				if (xmlLoadInfo.XmlName.Equals(_name))
				{
					xmlLoadInfo.WasReceivedFromServer = WorldStaticData.EClientFileState.LoadLocal;
					xmlLoadInfo.CompressedXmlData = null;
					break;
				}
			}
		}

		private static void TrackLocalLoad(string _name)
		{
			if (!LocallyLoadedConfigs.Contains(_name))
			{
				LocallyLoadedConfigs.Add(_name);
			}
		}
	}
}
