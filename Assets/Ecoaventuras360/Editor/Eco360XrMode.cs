using UnityEditor;
using UnityEngine;

public static class Eco360XrMode
{
    private const string XrSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    private const string StandaloneSettingsName = "Standalone Settings";

    [MenuItem("Ecoaventuras 360/Modo UI sin XR Simulator")]
    public static void DisableStandaloneXr()
    {
        SetStandaloneXrInit(false);
        Debug.Log("Ecoaventuras 360: XR Standalone desactivado para probar la interfaz sin Meta XR Simulator.");
    }

    [MenuItem("Ecoaventuras 360/Modo Meta XR Simulator")]
    public static void EnableStandaloneXr()
    {
        SetStandaloneXrInit(true);
        Debug.Log("Ecoaventuras 360: XR Standalone activado para Meta XR Simulator.");
    }

    private static void SetStandaloneXrInit(bool enabled)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(XrSettingsPath);
        foreach (var asset in assets)
        {
            if (asset == null || asset.name != StandaloneSettingsName)
            {
                continue;
            }

            var serialized = new SerializedObject(asset);
            var initOnStart = serialized.FindProperty("m_InitManagerOnStart");
            if (initOnStart == null)
            {
                Debug.LogWarning("Ecoaventuras 360: no se pudo encontrar m_InitManagerOnStart en Standalone Settings.");
                return;
            }

            initOnStart.boolValue = enabled;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return;
        }

        Debug.LogWarning("Ecoaventuras 360: no se encontro Standalone Settings para cambiar el modo XR.");
    }
}
