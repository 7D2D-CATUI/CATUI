using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

[HarmonyPatch]
public class XUiV_VideoChromaKeyPatch
{
    private const string ShaderBundlePath = "#@modfolder(CATUI):Resources/Shaders/CATUI_Shaders.unity3d?";
    private const string ShaderAssetName = "CATUI_ChromaKey";

    private static Shader _chromaKeyShader;
    private static bool _shaderLoadAttempted;
    private static bool _shaderAvailable;

    private static readonly HashSet<XUiV_Video> _chromaEnabled = new HashSet<XUiV_Video>();
    private static readonly Dictionary<XUiV_Video, Color> _keyColors = new Dictionary<XUiV_Video, Color>();
    private static readonly Dictionary<XUiV_Video, float> _tolerances = new Dictionary<XUiV_Video, float>();
    private static readonly Dictionary<XUiV_Video, float> _smoothings = new Dictionary<XUiV_Video, float>();
    private static readonly Dictionary<XUiV_Video, int> _inheritedRenderQueues = new Dictionary<XUiV_Video, int>();
    private static readonly HashSet<XUiV_Video> _debugLogged = new HashSet<XUiV_Video>();
    private static int _frameCount = 0;

    private static void EnsureShader()
    {
        if (_shaderLoadAttempted) return;
        _shaderLoadAttempted = true;

        Debug.Log($"[CATUI] Loading shader from: {ShaderBundlePath}{ShaderAssetName}");

        try
        {
            _chromaKeyShader = DataLoader.LoadAsset<Shader>($"{ShaderBundlePath}{ShaderAssetName}");
            
            Debug.Log($"[CATUI] Shader loaded: {_chromaKeyShader != null}, supported: {_chromaKeyShader?.isSupported}");
            
            if (_chromaKeyShader != null && _chromaKeyShader.isSupported)
            {
                _chromaKeyShader.hideFlags = HideFlags.HideAndDontSave;
                _shaderAvailable = true;
                Debug.Log("[CATUI] ChromaKey shader loaded successfully from AssetBundle");
            }
            else
            {
                Debug.LogWarning("[CATUI] ChromaKey shader not found or not supported");
                _shaderAvailable = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CATUI] Failed to load ChromaKey shader: {e.Message}\n{e.StackTrace}");
            _shaderAvailable = false;
        }
    }

    private static void ApplyChromaKey(XUiV_Video video)
    {
        if (!_chromaEnabled.Contains(video)) return;

        EnsureShader();

        if (!_shaderAvailable || _chromaKeyShader == null || video.uiTexture == null)
        {
            if (!_debugLogged.Contains(video))
            {
                _debugLogged.Add(video);
                Debug.LogWarning($"[CATUI] ApplyChromaKey SKIP for {video.id}: " +
                    $"shaderAvail={_shaderAvailable}, shaderNull={_chromaKeyShader == null}, " +
                    $"uiTexNull={video.uiTexture == null}");
            }
            return;
        }

        var keyColor = _keyColors.TryGetValue(video, out var kc) ? kc : new Color(0f, 0f, 0f);
        var tolerance = _tolerances.TryGetValue(video, out var t) ? t : 0.15f;
        var smoothing = _smoothings.TryGetValue(video, out var s) ? s : 0.1f;

        var uiTexture = video.uiTexture;
        var videoTexture = video.Texture;

        // 第一步：继承 renderQueue（仅执行一次）
        if (!_inheritedRenderQueues.ContainsKey(video))
        {
            int targetRq = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (uiTexture.material != null)
            {
                targetRq = uiTexture.material.renderQueue;
            }
            else if (uiTexture.transform.parent != null)
            {
                var panel = uiTexture.GetComponentInParent<UIPanel>();
                if (panel != null && panel.widgets != null)
                {
                    foreach (var widget in panel.widgets)
                    {
                        if (widget != null && widget != uiTexture && widget.drawCall != null && widget.drawCall.baseMaterial != null)
                        {
                            targetRq = widget.drawCall.baseMaterial.renderQueue;
                            break;
                        }
                    }
                }
            }

            _inheritedRenderQueues[video] = targetRq;
            Debug.Log($"[CATUI] Inherited renderQueue: {targetRq}");
        }

        int inheritedRq = _inheritedRenderQueues[video];

        // 第二步：创建或更新 base material
        bool needNewMaterial = uiTexture.material == null || uiTexture.material.shader != _chromaKeyShader;
        
        if (needNewMaterial)
        {
            if (!_debugLogged.Contains(video))
            {
                _debugLogged.Add(video);
                Debug.Log($"[CATUI] Creating NEW ChromaKey material for {video.id}: " +
                    $"currentMatNull={uiTexture.material == null}, " +
                    $"currentShader={uiTexture.material?.shader?.name ?? "null"}, " +
                    $"videoTextureNull={videoTexture == null}");
            }

            // 销毁旧材质
            if (uiTexture.material != null && uiTexture.material != _chromaKeyShader)
            {
                Object.Destroy(uiTexture.material);
            }

            // 创建新材质
            var mat = new Material(_chromaKeyShader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.renderQueue = inheritedRq;

            mat.SetColor("_KeyColor", keyColor);
            mat.SetFloat("_Tolerance", tolerance);
            mat.SetFloat("_Smoothing", smoothing);

            // 将视频纹理绑定到材质
            if (videoTexture != null)
            {
                mat.mainTexture = videoTexture;
            }

            // 设置材质到 UITexture (这会成为 drawCall.baseMaterial)
            uiTexture.material = mat;

            // 同时设置 UITexture.mainTexture，确保 NGUI 的 MaterialPropertyBlock 传递视频纹理
            uiTexture.mainTexture = videoTexture;

            Debug.Log($"[CATUI] Base material SET for {video.id}: " +
                $"matShader={uiTexture.material?.shader?.name ?? "null"}, " +
                $"matRq={uiTexture.material?.renderQueue}");
        }
        else
        {
            // 更新参数
            uiTexture.material.SetColor("_KeyColor", keyColor);
            uiTexture.material.SetFloat("_Tolerance", tolerance);
            uiTexture.material.SetFloat("_Smoothing", smoothing);
            
            // 确保视频纹理被正确传递
            if (videoTexture != null && uiTexture.mainTexture != videoTexture)
            {
                uiTexture.mainTexture = videoTexture;
            }
            if (videoTexture != null && uiTexture.material.mainTexture != videoTexture)
            {
                uiTexture.material.mainTexture = videoTexture;
            }
        }

        // 第三步：关键修复 - 强制修复 drawCall 的 dynamicMaterial shader
        // NGUI 的 CreateMaterial() 会通过 Shader.Find() 查找 shader，
        // 并用找到的 shader 覆盖我们自定义材质的 shader
        // 这里强制恢复我们的 ChromaKey shader
        if (uiTexture.drawCall != null && uiTexture.drawCall.dynamicMaterial != null)
        {
            var dc = uiTexture.drawCall;
            var dynMat = dc.dynamicMaterial;
            
            if (dynMat.shader != _chromaKeyShader)
            {
                if (_debugLogged.Contains(video))
                {
                    Debug.Log($"[CATUI] Fixing drawCall dynamicMaterial shader for {video.id}: " +
                        $"oldShader={dynMat.shader?.name ?? "null"}, " +
                        $"newShader={_chromaKeyShader.name}");
                }

                // 强制设置 dynamicMaterial 的 shader 为我们的 ChromaKey shader
                dynMat.shader = _chromaKeyShader;
                dynMat.renderQueue = inheritedRq;
                dynMat.SetColor("_KeyColor", keyColor);
                dynMat.SetFloat("_Tolerance", tolerance);
                dynMat.SetFloat("_Smoothing", smoothing);
                
                // 确保视频纹理也传递给 dynamicMaterial
                if (videoTexture != null)
                {
                    dynMat.mainTexture = videoTexture;
                }
            }
        }

        // 定期输出状态（仅用于调试）
        _frameCount++;
        if (_frameCount % 300 == 0 && _debugLogged.Contains(video))
        {
            var dc = uiTexture.drawCall;
            var dynMat = dc?.dynamicMaterial;
            Debug.Log($"[CATUI] ChromaKey STATUS for {video.id}: " +
                $"baseMatShader={uiTexture.material?.shader?.name ?? "null"}, " +
                $"dynMatShader={dynMat?.shader?.name ?? "null"}, " +
                $"dynMatRq={dynMat?.renderQueue}, " +
                $"dynMatMainTexNull={dynMat?.mainTexture == null}, " +
                $"uiTexMainTexNull={uiTexture.mainTexture == null}");
        }
    }

    private static void CleanupChromaKey(XUiV_Video video, bool keepState = false)
    {
        // 清理材质（但保留配置状态以便下次打开时重新应用）
        if (video.uiTexture != null && video.uiTexture.material != null && video.uiTexture.material.shader == _chromaKeyShader)
        {
            Object.Destroy(video.uiTexture.material);
            video.uiTexture.material = null;
        }

        if (!keepState)
        {
            _chromaEnabled.Remove(video);
            _keyColors.Remove(video);
            _tolerances.Remove(video);
            _smoothings.Remove(video);
            _inheritedRenderQueues.Remove(video);
            _debugLogged.Remove(video);
        }
    }

    private static Color ParseColor(string value)
    {
        var parts = value.Split(',');
        if (parts.Length >= 3 &&
            float.TryParse(parts[0], out float r) &&
            float.TryParse(parts[1], out float g) &&
            float.TryParse(parts[2], out float b))
        {
            float a = 1f;
            if (parts.Length >= 4)
            {
                float.TryParse(parts[3], out a);
            }
            return new Color(r, g, b, a);
        }
        return new Color(0f, 0f, 0f);
    }

    // ============ Harmony Patches ============

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiView), "ParseInitialAttributeValue")]
    public static bool ParseInitialAttributeValuePrefix(XUiView __instance, string _attribute, string _value)
    {
        if (__instance is not XUiV_Video video) return true;

        switch (_attribute)
        {
            case "chromakey":
                if (!_value.Contains("{"))
                {
                    if (StringParsers.ParseBool(_value))
                    {
                        _chromaEnabled.Add(video);
                        Debug.Log($"[CATUI] ChromaKey enabled for {video.id}");
                    }
                }
                return false;

            case "keycolor":
                if (!_value.Contains("{"))
                {
                    _keyColors[video] = ParseColor(_value);
                }
                return false;

            case "tolerance":
                if (!_value.Contains("{"))
                {
                    if (float.TryParse(_value, out float tol))
                    {
                        _tolerances[video] = Mathf.Clamp(tol, 0f, 1f);
                    }
                }
                return false;

            case "smoothing":
                if (!_value.Contains("{"))
                {
                    if (float.TryParse(_value, out float smooth))
                    {
                        _smoothings[video] = Mathf.Clamp(smooth, 0f, 1f);
                    }
                }
                return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiV_Video), "InitView")]
    public static void InitViewPostfix(XUiV_Video __instance)
    {
        if (_chromaEnabled.Contains(__instance))
        {
            Debug.Log($"[CATUI] ChromaKey InitView for {__instance.id}");
            ApplyChromaKey(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiV_Video), "updateData")]
    public static void UpdateDataPostfix(XUiV_Video __instance)
    {
        if (!_chromaEnabled.Contains(__instance)) return;

        if (__instance.uiTexture == null)
        {
            if (_debugLogged.Contains(__instance))
            {
                Debug.LogWarning($"[CATUI] updateData but uiTexture is NULL for {__instance.id}");
            }
            return;
        }

        ApplyChromaKey(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiV_Video), "Cleanup")]
    public static void CleanupPostfix(XUiV_Video __instance)
    {
        CleanupChromaKey(__instance, keepState: false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiV_Video), "OnClose")]
    public static void OnClosePostfix(XUiV_Video __instance)
    {
        // 不清理材质！保留材质以便下次打开时继续使用
        // 材质会在 Cleanup（对象销毁）时才清理
    }

    // 关键修复：拦截 UIDrawCall.UpdateMaterials()，在材质重建后恢复 ChromaKey shader
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIDrawCall), "UpdateMaterials")]
    public static void UpdateMaterialsPostfix(UIDrawCall __instance)
    {
        if (__instance == null || __instance.dynamicMaterial == null) return;

        // 检查这个 drawCall 是否属于我们的 chromaKey 视频
        foreach (var video in _chromaEnabled)
        {
            if (video.uiTexture != null && video.uiTexture.drawCall == __instance)
            {
                var dynMat = __instance.dynamicMaterial;
                if (dynMat.shader != _chromaKeyShader)
                {
                    if (_debugLogged.Contains(video))
                    {
                        Debug.Log($"[CATUI] UpdateMaterials FIX for {video.id}: " +
                            $"oldShader={dynMat.shader?.name ?? "null"}");
                    }

                    var keyColor = _keyColors.TryGetValue(video, out var kc) ? kc : new Color(0f, 0f, 0f);
                    var tolerance = _tolerances.TryGetValue(video, out var t) ? t : 0.15f;
                    var smoothing = _smoothings.TryGetValue(video, out var s) ? s : 0.1f;
                    var videoTexture = video.Texture;
                    var inheritedRq = _inheritedRenderQueues.TryGetValue(video, out var rq) ? rq : (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    dynMat.shader = _chromaKeyShader;
                    dynMat.renderQueue = inheritedRq;
                    dynMat.SetColor("_KeyColor", keyColor);
                    dynMat.SetFloat("_Tolerance", tolerance);
                    dynMat.SetFloat("_Smoothing", smoothing);
                    
                    if (videoTexture != null)
                    {
                        dynMat.mainTexture = videoTexture;
                    }
                }
                break;
            }
        }
    }
}
