using UnityEngine;
using UnityEditor;
using System.IO;

public class ChromaKeyShaderBuilder
{
    private const string ShaderSourcePath = "Assets/Shaders/CATUI_ChromaKey.shader";
    private const string AssetBundleFolder = "Assets/AssetBundles";
    private const string BundleName = "CATUI_Shaders";

    [MenuItem("CATUI/Build ChromaKey Shader AssetBundle")]
    public static void BuildShaderBundle()
    {
        Debug.Log("[CATUI] Starting ChromaKey shader build...");

        // Ensure output directory exists
        if (!Directory.Exists(AssetBundleFolder))
        {
            Directory.CreateDirectory(AssetBundleFolder);
        }

        // Load the shader
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderSourcePath);
        if (shader == null)
        {
            Debug.LogError("[CATUI] Failed to load shader at: " + ShaderSourcePath);
            return;
        }

        Debug.Log("[CATUI] Loaded shader: " + shader.name);

        // Mark shader for asset bundle
        var importer = AssetImporter.GetAtPath(ShaderSourcePath);
        importer.assetBundleName = BundleName;
        importer.SaveAndReimport();

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), AssetBundleFolder);

        // Clear old bundles
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);
        }

        // Build the asset bundle using compatible API
        BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        BuildAssetBundleOptions options = BuildAssetBundleOptions.None;

        var manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            options,
            buildTarget
        );

        if (manifest != null)
        {
            Debug.Log("[CATUI] Shader bundle built successfully!");
            Debug.Log("[CATUI] Output directory: " + outputPath);
            Debug.Log("[CATUI] Copy CATUI_Shaders file to: ZZZ_CATUI/Resources/Shaders/");
            
            // List built bundles
            string[] bundles = manifest.GetAllAssetBundles();
            foreach (var bundle in bundles)
            {
                Debug.Log("[CATUI] Built bundle: " + bundle);
            }
        }
        else
        {
            Debug.LogError("[CATUI] Failed to build shader bundle");
        }
    }
}