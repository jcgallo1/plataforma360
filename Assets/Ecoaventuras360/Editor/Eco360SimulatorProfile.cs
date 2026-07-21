using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class Eco360SimulatorProfile
{
    private static readonly string[] LargeUiTextures =
    {
        "Assets/Resources/Ecoaventuras360/Sprites/REFERENCIA.png",
        "Assets/Resources/Ecoaventuras360/Sprites/fondo.png",
        "Assets/Resources/Ecoaventuras360/Sprites/fondo principal.png",
        "Assets/Resources/Ecoaventuras360/Sprites/referencia letrero.png",
        "Assets/Resources/Ecoaventuras360/Sprites/Fl0r.PNG",
        "Assets/Resources/Ecoaventuras360/Sprites/IMG_0326.PNG"
    };

    [MenuItem("Ecoaventuras 360/Aplicar perfil ligero para Meta XR Simulator")]
    public static void ApplySimulatorProfile()
    {
        ConfigurePlayerGraphics();
        ConfigureLargeTextures();
        ConfigureUrpAsset("Assets/Settings/Mobile_RPAsset.asset", 0.8f);
        ConfigureUrpAsset("Assets/Settings/PC_RPAsset.asset", 0.8f);
        AssetDatabase.SaveAssets();
        Debug.Log("Ecoaventuras 360: perfil ligero aplicado para Meta XR Simulator.");
    }

    private static void ConfigurePlayerGraphics()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
    }

    private static void ConfigureLargeTextures()
    {
        foreach (var path in LargeUiTextures)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureUrpAsset(string path, float renderScale)
    {
        var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
        if (asset == null)
        {
            return;
        }

        SetProperty(asset, "renderScale", renderScale);
        SetProperty(asset, "msaaSampleCount", 1);
        SetProperty(asset, "supportsHDR", false);
        SetProperty(asset, "supportsCameraDepthTexture", false);
        SetProperty(asset, "supportsCameraOpaqueTexture", false);
        SetProperty(asset, "supportsMainLightShadows", false);
        SetProperty(asset, "supportsAdditionalLightShadows", false);
        SetProperty(asset, "shadowDistance", 0f);
        EditorUtility.SetDirty(asset);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null || !property.CanWrite)
        {
            return;
        }

        try
        {
            property.SetValue(target, value);
        }
        catch (Exception)
        {
            // Some URP versions expose read-only compatibility properties. Skip those.
        }
    }
}
