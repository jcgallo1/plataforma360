using Ecoaventuras360;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Eco360SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RootName = "Eco360 Scene UI Root";
    private const string SpriteRoot = "Assets/Resources/Ecoaventuras360/Sprites/";

    [InitializeOnLoadMethod]
    private static void BuildWhenSampleSceneIsOpen()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path.Replace("\\", "/") == ScenePath && (GameObject.Find(RootName) == null || GameObject.Find("Hotspot Crear Perfil") == null || GameObject.Find("Hotspot Inicio") == null || GameObject.Find("Hotspot Bosque Protector 360") == null))
            {
                BuildSampleScene();
            }
        };
    }

    [MenuItem("Ecoaventuras 360/Reconstruir SampleScene UI")]
    public static void BuildSampleScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace("\\", "/") != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        DeleteIfExists(RootName);
        EnsureSpriteImports();

        var root = new GameObject(RootName);
        var app = root.AddComponent<Eco360App>();
        app.enabled = true;

        EnsureCamera();
        EnsureLight();
        EnsureEventSystem();

        var canvas = CreateScreenCanvas(root.transform);
        BuildProfileView(canvas.transform);
        BuildCreateProfileView(canvas.transform);
        BuildIconSelectionView(canvas.transform);
        BuildMainMenuView(canvas.transform);
        BuildVideoDetailView(canvas.transform);
        BuildSettingsView(canvas.transform);
        BuildPlayerOverlay(canvas.transform);
        DeleteIfExists("Eco360 Spatial UI Canvas");
        Eco360CanvasScaleFix.ApplyToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Ecoaventuras 360: SampleScene.unity now contains the rebuilt profile, icon selection, and main menu Canvas views.");
    }

    private static Canvas CreateScreenCanvas(Transform parent)
    {
        DeleteIfExists("Eco360 Canvas");
        var canvasGo = new GameObject("Eco360 Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(parent, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Stretch(canvasGo.GetComponent<RectTransform>());
        return canvas;
    }

    private static void BuildProfileView(Transform parent)
    {
        var view = View(parent, "01 Profile Selection View", true);
        FullImage(view, "fondo principal");
        Image(view, "burbuja", new Vector2(0, 270), new Vector2(310, 310));
        Image(view, "logo eco aventuras", new Vector2(0, 272), new Vector2(390, 310));

        Image(view, "burbuja", new Vector2(-765, -178), new Vector2(124, 124));
        Text(view, "<", 60, new Vector2(-765, -174), new Vector2(90, 82), TextAnchor.MiddleCenter).color = new Color(0.28f, 0.32f, 0.34f);
        Image(view, "burbuja", new Vector2(765, -178), new Vector2(124, 124));
        Text(view, ">", 60, new Vector2(765, -174), new Vector2(90, 82), TextAnchor.MiddleCenter).color = new Color(0.28f, 0.32f, 0.34f);

        Image(view, "burbuja", new Vector2(0, -220), new Vector2(250, 250));
        Text(view, "+", 98, new Vector2(0, -190), new Vector2(180, 96), TextAnchor.MiddleCenter).color = new Color(0.18f, 0.32f, 0.34f);
        Text(view, "Nuevo\nPerfil", 30, new Vector2(0, -298), new Vector2(230, 76), TextAnchor.MiddleCenter);

        Button(view, "Hotspot Crear Perfil", new Vector2(0, -220), new Vector2(250, 250));
        Button(view, "Hotspot Flecha Perfil Izquierda", new Vector2(-765, -178), new Vector2(118, 118));
        Button(view, "Hotspot Flecha Perfil Derecha", new Vector2(765, -178), new Vector2(118, 118));
    }

    private static void ProfileBubble(RectTransform view, string label, string icon, Vector2 pos, bool selected)
    {
        Image(view, "burbuja", pos, selected ? new Vector2(242, 242) : new Vector2(226, 226));
        Image(view, icon, pos + new Vector2(0, 14), selected ? new Vector2(172, 172) : new Vector2(160, 160));
        Text(view, label, 28, pos + new Vector2(0, -78), new Vector2(220, 44), TextAnchor.MiddleCenter);
    }

    private static void BuildCreateProfileView(Transform parent)
    {
        var view = View(parent, "01B Create Profile View", false);
        FullImage(view, "fondo principal");
        Image(view, "burbuja", new Vector2(0, 260), new Vector2(300, 300));
        Image(view, "logo eco aventuras", new Vector2(0, 262), new Vector2(380, 302));
        Image(view, "burbuja", new Vector2(-790, -320), new Vector2(124, 124));
        Text(view, "<", 58, new Vector2(-790, -317), new Vector2(82, 80), TextAnchor.MiddleCenter).color = new Color(0.24f, 0.28f, 0.3f);
        Image(view, "burbuja", new Vector2(790, -320), new Vector2(124, 124));
        Text(view, ">", 58, new Vector2(790, -317), new Vector2(82, 80), TextAnchor.MiddleCenter).color = new Color(0.24f, 0.28f, 0.3f);
        Text(view, "Hola, Ingresa tu nombre", 30, new Vector2(-255, -52), new Vector2(690, 48), TextAnchor.MiddleLeft);
        Image(view, "BLOQUE texto", new Vector2(0, -124), new Vector2(930, 84));
        Text(view, "Nombre", 27, new Vector2(-330, -124), new Vector2(220, 48), TextAnchor.MiddleLeft);
        Panel(view, "Continuar Button", new Vector2(0, -305), new Vector2(260, 62), new Color(0.54f, 0.32f, 1f, 0.78f));
        Text(view, "Continuar", 24, new Vector2(0, -305), new Vector2(250, 48), TextAnchor.MiddleCenter);
    }

    private static void BuildIconSelectionView(Transform parent)
    {
        var view = View(parent, "01C Icon Selection View", false);
        FullImage(view, "fondo principal");
        Text(view, "Hola, Username", 31, new Vector2(-720, 365), new Vector2(380, 58), TextAnchor.MiddleLeft);
        Image(view, "usuario icono", new Vector2(-515, 370), new Vector2(92, 92));
        Image(view, "burbuja", new Vector2(0, 392), new Vector2(170, 170));
        Image(view, "logo eco aventuras", new Vector2(0, 394), new Vector2(218, 173));
        Text(view, "Escoge un icono", 33, new Vector2(0, 205), new Vector2(430, 60), TextAnchor.MiddleCenter);

        var positions = new[]
        {
            new Vector2(-360, 10), new Vector2(-120, 10), new Vector2(120, 10), new Vector2(360, 10)
        };
        var labels = new[] { "Perfil", "Lagu", "Perez", "Juan" };
        var icons = new[] { "usuario icono", "--21", "--24", "--22" };
        for (var i = 0; i < positions.Length; i++)
        {
            Image(view, "burbuja", positions[i], new Vector2(210, 210));
            Image(view, icons[i], positions[i] + new Vector2(0, 12), new Vector2(140, 140));
            Text(view, labels[i], 24, positions[i] + new Vector2(0, -76), new Vector2(180, 42), TextAnchor.MiddleCenter);
            Button(view, $"Hotspot Icono {i + 1}", positions[i], new Vector2(220, 220));
        }

        Image(view, "burbuja", new Vector2(795, -330), new Vector2(112, 112));
        Text(view, "✓", 54, new Vector2(795, -330), new Vector2(90, 70), TextAnchor.MiddleCenter).color = new Color(0.12f, 0.16f, 0.15f);
        Button(view, "Hotspot Confirmar Perfil", new Vector2(795, -330), new Vector2(105, 76));
    }

    private static void BuildMainMenuView(Transform parent)
    {
        var view = View(parent, "02 Main Menu Prototype View", false);
        FullImage(view, "fondo principal");
        Text(view, "Hola, Username", 31, new Vector2(-720, 365), new Vector2(380, 58), TextAnchor.MiddleLeft);
        Image(view, "burbuja", new Vector2(-520, 370), new Vector2(104, 104));
        Image(view, "usuario icono", new Vector2(-520, 370), new Vector2(86, 86));
        Image(view, "burbuja", new Vector2(0, 400), new Vector2(185, 185));
        Image(view, "logo eco aventuras", new Vector2(0, 402), new Vector2(238, 189));

        Image(view, "bloque buscar", new Vector2(170, 235), new Vector2(1010, 86));
        Image(view, "buscar icono", new Vector2(-285, 235), new Vector2(50, 50));
        Image(view, "microfono icono", new Vector2(620, 235), new Vector2(46, 46));

        Image(view, "submenu izquierdo", new Vector2(-760, -90), new Vector2(150, 560));
        MenuPreviewButton(view, "Inicio", "menu icono", new Vector2(-760, 95), true);
        MenuPreviewButton(view, "Favoritos", "me gusta icono", new Vector2(-760, -30), false);
        MenuPreviewButton(view, "Ajustes", "boton herramienta", new Vector2(-760, -155), false);

        var spot = new Vector2(80, -35);
        Image(view, "burbuja", spot, new Vector2(255, 255));
        Image(view, "foto LAGU Y ZARIGUELLA", spot + new Vector2(0, 16), new Vector2(178, 136));
        Panel(view, "Sombra titulo Bosque Protector", spot + new Vector2(0, -50), new Vector2(184, 66), new Color(0, 0, 0, 0.24f));
        Text(view, "Bosque Protector", 17, spot + new Vector2(0, -40), new Vector2(170, 42), TextAnchor.MiddleCenter);
        Text(view, "360", 20, spot + new Vector2(0, -78), new Vector2(90, 28), TextAnchor.MiddleCenter);
        Button(view, "Hotspot Bosque Protector 360", spot, new Vector2(230, 230));

        Image(view, "scrolbar", new Vector2(795, -92), new Vector2(76, 500));
        Image(view, "scrolbar agarre", new Vector2(795, -28), new Vector2(72, 190));
    }

    private static void MenuPreviewButton(RectTransform view, string label, string iconName, Vector2 pos, bool selected)
    {
        if (selected)
        {
            Image(view, "selección menú izquierdo", pos, new Vector2(112, 112));
        }

        Image(view, iconName, pos + new Vector2(0, 24), new Vector2(50, 50));
        Text(view, label, 21, pos + new Vector2(0, -34), new Vector2(120, 44), TextAnchor.MiddleCenter);
        Button(view, "Hotspot " + label, pos, new Vector2(116, 116));
    }

    private static void BuildVideoDetailView(Transform parent)
    {
        var view = View(parent, "03 Video Detail Prototype View", false);
        FullImage(view, "referencia letrero");
        Image(view, "boton me gusta", new Vector2(360, 260), new Vector2(96, 96));
        for (var i = 0; i < 5; i++)
        {
            Image(view, "estrella vacia", new Vector2(-302 + i * 53f, -248), new Vector2(48, 48));
        }
        Button(view, "Hotspot Volver", new Vector2(-560, 260), new Vector2(260, 95));
        Button(view, "Hotspot Favorito", new Vector2(360, 260), new Vector2(115, 115));
        Button(view, "Hotspot Iniciar 360", new Vector2(20, -365), new Vector2(370, 110));
    }

    private static void BuildSettingsView(Transform parent)
    {
        var view = View(parent, "04 Settings View", false);
        FullImage(view, "fondo principal");
        Image(view, "logo eco aventuras", new Vector2(0, 350), new Vector2(260, 210));
        Image(view, "boton herramienta", new Vector2(760, 360), new Vector2(105, 105));
        Panel(view, "Glass Settings Panel", new Vector2(0, -60), new Vector2(1050, 650), new Color(0.55f, 0.9f, 1f, 0.35f));
        Text(view, "Ajustes", 48, new Vector2(0, 190), new Vector2(500, 70), TextAnchor.MiddleCenter);
        Text(view, "Idioma", 34, new Vector2(-250, 80), new Vector2(260, 55), TextAnchor.MiddleLeft);
        Text(view, "Espanol   English", 30, new Vector2(140, 80), new Vector2(440, 55), TextAnchor.MiddleCenter);
        Text(view, "Musica", 30, new Vector2(-250, -20), new Vector2(260, 55), TextAnchor.MiddleLeft);
        Text(view, "Efectos", 30, new Vector2(-250, -110), new Vector2(260, 55), TextAnchor.MiddleLeft);
        Text(view, "Icono", 30, new Vector2(-250, -210), new Vector2(260, 55), TextAnchor.MiddleLeft);
    }

    private static void BuildPlayerOverlay(Transform parent)
    {
        var view = View(parent, "05 Player Overlay View", false);
        Panel(view, "Video Control Bar", new Vector2(0, -455), new Vector2(1550, 120), new Color(0, 0, 0, 0.62f));
        Text(view, "Salir", 26, new Vector2(-680, -455), new Vector2(150, 55), TextAnchor.MiddleCenter);
        Text(view, "Pausa", 26, new Vector2(-505, -455), new Vector2(150, 55), TextAnchor.MiddleCenter);
        Panel(view, "Timeline", new Vector2(120, -455), new Vector2(860, 28), new Color(1, 1, 1, 0.35f));
        Text(view, "00:00 / 00:00", 24, new Vector2(620, -455), new Vector2(260, 55), TextAnchor.MiddleCenter);
        Panel(view, "Interaction Prompt", new Vector2(0, 330), new Vector2(1360, 210), new Color(0.03f, 0.13f, 0.1f, 0.75f));
        Text(view, "Interaccion VR", 34, new Vector2(-430, 365), new Vector2(360, 55), TextAnchor.MiddleLeft);
        Text(view, "Picotear, atrapar peces, tocar flor, aletear y clasificar residuos.", 24, new Vector2(40, 315), new Vector2(760, 75), TextAnchor.MiddleLeft);
    }

    private static RectTransform View(Transform parent, string name, bool active)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect);
        go.SetActive(active);
        return rect;
    }

    private static void FullImage(RectTransform parent, string spriteName)
    {
        var image = Image(parent, spriteName, Vector2.zero, new Vector2(1920, 1080));
        Stretch(image.GetComponent<RectTransform>());
        image.preserveAspect = false;
    }

    private static Image Image(RectTransform parent, string spriteName, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(spriteName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = LoadSprite(spriteName);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Text Text(RectTransform parent, string value, int size, Vector2 pos, Vector2 dimensions, TextAnchor anchor)
    {
        var go = new GameObject("Text - " + value, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = dimensions;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Image Panel(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button Button(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0.001f);
        image.raycastTarget = true;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + name + ".png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + name + ".PNG");
    }

    private static void EnsureSpriteImports()
    {
        var names = new[]
        {
            "REFERENCIA.png", "referencia letrero.png", "fondo principal.png", "logo eco aventuras.png",
            "burbuja.png", "boton herramienta.png", "foto LAGU Y ZARIGUELLA.png", "BLOQUE texto.png", "bloque buscar.png",
            "buscar icono.png", "microfono icono.png", "submenu izquierdo.png", "selección menú izquierdo.png",
            "scrolbar.png", "scrolbar agarre.png", "usuario icono.png", "menu icono.png", "me gusta icono.png",
            "boton me gusta.png", "me gusta.png", "estrella vacia.png", "estrella logrado.png", "estrella.png",
            "BOOTON volver.png", "BOTON iniciar.png", "espacio imagen video.png",
            "Fl0r.PNG", "IMG_0326.PNG", "--21.png", "--22.png", "--23.png", "--24.png", "--25.png", "--26.png"
        };

        foreach (var name in names)
        {
            var path = SpriteRoot + name;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }

    private static void EnsureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            camera = cameraGo.GetComponent<Camera>();
        }

        camera.transform.position = new Vector3(0, 1.4f, -4);
        camera.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.08f, 0.07f);
    }

    private static void EnsureLight()
    {
        if (GameObject.Find("Eco360 Directional Light") != null)
        {
            return;
        }

        var lightGo = new GameObject("Eco360 Directional Light", typeof(Light));
        var light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(40, 35, 0);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void DeleteIfExists(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
