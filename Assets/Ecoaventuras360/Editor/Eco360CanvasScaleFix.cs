using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Eco360CanvasScaleFix
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Ecoaventuras 360/Reducir Canvas VR")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace("\\", "/") != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        ApplyToOpenScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Ecoaventuras 360: escala de canvas VR reducida.");
    }

    public static void ApplyToOpenScene()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            var rect = canvas.GetComponent<RectTransform>();
            if (rect == null)
            {
                continue;
            }

            if (canvas.name == "Eco360 Spatial UI Canvas")
            {
                rect.localPosition = new Vector3(0f, 1.12f, 1.9f);
                rect.localScale = Vector3.one * 0.00045f;
                rect.sizeDelta = new Vector2(640f, 300f);
                EditorUtility.SetDirty(rect);
                continue;
            }

            if (canvas.name == "Spatial UI")
            {
                rect.localScale = Vector3.one * 0.00035f;
                rect.sizeDelta = new Vector2(420f, 190f);
                EditorUtility.SetDirty(rect);
            }
        }

        EditorSceneManager.MarkAllScenesDirty();
    }
}
