using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Ecoaventuras360
{
    public class Eco360App : MonoBehaviour
    {
        private const string ProfilePrefsKey = "eco360_profiles";
        private const string ActiveProfilePrefsKey = "eco360_active_profile";
        private const string SpritePath = "Ecoaventuras360/Sprites/";
        private const string VideoPath = "Ecoaventuras360/Videos";

        private readonly List<Experience> experiences = new List<Experience>();
        private readonly List<Sprite> profileIcons = new List<Sprite>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        private Canvas canvas;
        private RectTransform root;
        private Font font;
        private EcoProfile activeProfile;
        private ProfileStore store;
        private string searchText = string.Empty;
        private string selectedSection = "Inicio";
        private Experience selectedExperience;
        private int profilePage;

        private Camera vrCamera;
        private GameObject sphere;
        private VideoPlayer videoPlayer;
        private RenderTexture videoTexture;
        private GameObject worldLight;
        private Slider playbackSlider;
        private Text playbackTime;
        private bool draggingPlayback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<Eco360App>() != null)
            {
                return;
            }

            var go = new GameObject("Ecoaventuras 360 App");
            DontDestroyOnLoad(go);
            go.AddComponent<Eco360App>();
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildExperienceCatalog();
            BuildProfileIcons();
            LoadProfiles();
            SetupScene();
            ShowProfiles();
        }

        private void Update()
        {
            if (videoPlayer != null && videoPlayer.isPrepared && playbackSlider != null && !draggingPlayback)
            {
                var length = Math.Max(videoPlayer.length, 0.01);
                playbackSlider.value = (float)(videoPlayer.time / length);
                playbackTime.text = $"{FormatTime(videoPlayer.time)} / {FormatTime(videoPlayer.length)}";
            }
        }

        private void BuildExperienceCatalog()
        {
            var clips = Resources.LoadAll<VideoClip>(VideoPath)
                .OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            experiences.Add(new Experience
            {
                Id = "bosque_protector_360",
                Title = "Bosque Protector 360",
                Description = "Unete a Lagu en una aventura por el Bosque Protector la Prosperina para conocer sobre sus animales y ayudarlos con sus problemas.",
                Clip = clips.FirstOrDefault(),
                PreviewSpriteName = "foto LAGU Y ZARIGUELLA",
                Stars = 0
            });
        }

        private void BuildProfileIcons()
        {
            var iconNames = new[] { "usuario icono", "--21", "--24", "--22" };
            foreach (var name in iconNames)
            {
                profileIcons.Add(LoadSprite(name));
            }
        }

        private void LoadProfiles()
        {
            var json = PlayerPrefs.GetString(ProfilePrefsKey, string.Empty);
            store = string.IsNullOrWhiteSpace(json) ? new ProfileStore() : JsonUtility.FromJson<ProfileStore>(json);
            if (store == null)
            {
                store = new ProfileStore();
            }

            store.Profiles.RemoveAll(p => p.Id == "demo_lagu" || p.Id == "demo_perez");

            var activeId = PlayerPrefs.GetString(ActiveProfilePrefsKey, string.Empty);
            activeProfile = store.Profiles.FirstOrDefault(p => p.Id == activeId) ?? store.Profiles.FirstOrDefault();
            SaveProfiles();
        }

        private void SaveProfiles()
        {
            PlayerPrefs.SetString(ProfilePrefsKey, JsonUtility.ToJson(store));
            PlayerPrefs.SetString(ActiveProfilePrefsKey, activeProfile != null ? activeProfile.Id : string.Empty);
            PlayerPrefs.Save();
        }

        private void SetupScene()
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                vrCamera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            vrCamera.transform.position = Vector3.zero;
            vrCamera.transform.rotation = Quaternion.identity;
            vrCamera.clearFlags = CameraClearFlags.SolidColor;
            vrCamera.backgroundColor = new Color(0.02f, 0.08f, 0.07f);

            worldLight = new GameObject("Eco360 World Light");
            var light = worldLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            worldLight.transform.rotation = Quaternion.Euler(40, 35, 0);

            var existingCanvas = GameObject.Find("Eco360 Canvas");
            canvas = existingCanvas != null ? existingCanvas.GetComponent<Canvas>() : null;
            if (canvas == null)
            {
                canvas = new GameObject("Eco360 Canvas").AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvas.gameObject);
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            root = canvas.GetComponent<RectTransform>();

            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystem = eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystemGo);
            }
            else if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private void ClearUi()
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private void ShowProfiles()
        {
            StopVideoMode();
            ClearUi();
            Background("fondo principal");

            Image(root, LoadSprite("burbuja"), new Vector2(0, 270), new Vector2(310, 310), true);
            Image(root, LoadSprite("logo eco aventuras"), new Vector2(0, 272), new Vector2(390, 310), true);
            CircleArrow(new Vector2(-765, -178), "<", PreviousProfilePage, store.Profiles.Count > 2);
            CircleArrow(new Vector2(765, -178), ">", NextProfilePage, store.Profiles.Count > 2);

            var visibleProfiles = store.Profiles.Skip(profilePage * 2).Take(2).ToList();
            var profilePositions = visibleProfiles.Count == 1
                ? new[] { new Vector2(-180, -220) }
                : new[] { new Vector2(-360, -220), new Vector2(0, -220) };
            for (var i = 0; i < visibleProfiles.Count; i++)
            {
                ProfileBubble(visibleProfiles[i], profilePositions[i], i == visibleProfiles.Count - 1);
            }

            NewProfileBubble(store.Profiles.Count == 0 ? new Vector2(0, -220) : new Vector2(365, -220));

        }

        private void CircleArrow(Vector2 pos, string label, UnityEngine.Events.UnityAction action, bool enabled)
        {
            ArrowButton(pos, label, action, enabled);
        }

        private Button ArrowButton(Vector2 pos, string label, UnityEngine.Events.UnityAction action, bool enabled)
        {
            Image(root, LoadSprite("burbuja"), pos, new Vector2(124, 124), true);
            var button = Button("", pos, new Vector2(112, 112), new Color(1f, 1f, 1f, 0.001f), enabled ? action : null);
            button.interactable = enabled;
            Text(root, label, 58, pos + new Vector2(0, 3), new Vector2(82, 80), TextAnchor.MiddleCenter, enabled ? new Color(0.24f, 0.28f, 0.3f) : new Color(0.24f, 0.28f, 0.3f, 0.35f));
            return button;
        }

        private void PreviousProfilePage()
        {
            var maxPage = Mathf.Max(0, Mathf.CeilToInt(store.Profiles.Count / 2f) - 1);
            profilePage = profilePage <= 0 ? maxPage : profilePage - 1;
            ShowProfiles();
        }

        private void NextProfilePage()
        {
            var maxPage = Mathf.Max(0, Mathf.CeilToInt(store.Profiles.Count / 2f) - 1);
            profilePage = profilePage >= maxPage ? 0 : profilePage + 1;
            ShowProfiles();
        }

        private void ProfileBubble(EcoProfile profile, Vector2 pos, bool selected)
        {
            var bubbleSize = selected ? new Vector2(242, 242) : new Vector2(226, 226);
            var iconSize = selected ? new Vector2(172, 172) : new Vector2(160, 160);
            Image(root, LoadSprite("burbuja"), pos, bubbleSize, true);
            Image(root, IconFor(profile.IconIndex), pos + new Vector2(0, 14), iconSize, true);
            Text(root, profile.Name, 28, pos + new Vector2(0, -78), new Vector2(220, 44), TextAnchor.MiddleCenter, Color.white);
            InvisibleButton("Entrar " + profile.Name, pos, new Vector2(238, 238), () =>
            {
                activeProfile = profile;
                selectedSection = "Inicio";
                searchText = string.Empty;
                SaveProfiles();
                ShowMainMenu();
            });
        }

        private void NewProfileBubble(Vector2 pos)
        {
            Image(root, LoadSprite("burbuja"), pos, new Vector2(226, 226), true);
            Text(root, "+", 96, pos + new Vector2(0, 32), new Vector2(170, 92), TextAnchor.MiddleCenter, new Color(0.18f, 0.32f, 0.34f));
            Text(root, "Nuevo\nPerfil", 28, pos + new Vector2(0, -72), new Vector2(210, 74), TextAnchor.MiddleCenter, Color.white);
            InvisibleButton("Crear Perfil", pos, new Vector2(238, 238), ShowCreateProfile);
        }

        private void ShowCreateProfile()
        {
            ClearUi();
            Background("fondo principal");
            Image(root, LoadSprite("burbuja"), new Vector2(0, 260), new Vector2(300, 300), true);
            Image(root, LoadSprite("logo eco aventuras"), new Vector2(0, 262), new Vector2(380, 302), true);
            ArrowButton(new Vector2(-790, -320), "<", ShowProfiles, true);
            ArrowButton(new Vector2(790, -320), ">", () => { }, false);

            Text(root, "Hola, Ingresa tu nombre", 30, new Vector2(-255, -52), new Vector2(690, 48), TextAnchor.MiddleLeft, Color.white);
            Image(root, LoadSprite("BLOQUE texto"), new Vector2(0, -124), new Vector2(930, 84), false);
            var nameInput = Input(null, "", new Vector2(0, -124), new Vector2(830, 62));
            nameInput.GetComponent<Image>().color = new Color(1, 1, 1, 0.02f);
            nameInput.textComponent.color = Color.white;
            nameInput.textComponent.fontSize = 28;
            Text error = Text(root, "", 23, new Vector2(0, -202), new Vector2(760, 42), TextAnchor.MiddleCenter, new Color(1f, 0.28f, 0.22f));

            Button("Continuar", new Vector2(0, -305), new Vector2(260, 62), new Color(0.54f, 0.32f, 1f, 0.78f), () =>
            {
                if (string.IsNullOrWhiteSpace(nameInput.text))
                {
                    error.text = "Ingresa un nombre para continuar.";
                    return;
                }

                ShowCreateProfileIconStep(nameInput.text.Trim());
            });
        }

        private void ShowCreateProfileIconStep(string profileName)
        {
            ClearUi();
            Background("fondo principal");
            Text(root, "Hola, " + profileName, 31, new Vector2(-720, 365), new Vector2(380, 58), TextAnchor.MiddleLeft, Color.white);
            Image(root, LoadSprite("usuario icono"), new Vector2(-515, 370), new Vector2(92, 92), true);
            Image(root, LoadSprite("burbuja"), new Vector2(0, 392), new Vector2(170, 170), true);
            Image(root, LoadSprite("logo eco aventuras"), new Vector2(0, 394), new Vector2(218, 173), true);
            Text(root, "Escoge un icono", 33, new Vector2(0, 205), new Vector2(430, 60), TextAnchor.MiddleCenter, Color.white);
            ArrowButton(new Vector2(-790, -320), "<", ShowCreateProfile, true);

            var positions = new[]
            {
                new Vector2(-360, 10),
                new Vector2(-120, 10),
                new Vector2(120, 10),
                new Vector2(360, 10)
            };
            var labels = new[] { "Perfil", "Lagu", "Perez", "Juan" };

            var selectedIcon = 0;
            var iconRings = new List<RectTransform>();
            for (var i = 0; i < positions.Length; i++)
            {
                var index = i;
                Image(root, LoadSprite("burbuja"), positions[i], new Vector2(210, 210), true);
                var ring = Panel("Icono Perfil " + (i + 1), positions[i], new Vector2(216, 216), i == 0 ? new Color(0.87f, 0.2f, 1f, 0.24f) : new Color(1f, 1f, 1f, 0.08f));
                iconRings.Add(ring);
                Image(root, IconFor(i), positions[i] + new Vector2(0, 12), new Vector2(140, 140), true);
                Text(root, labels[i], 24, positions[i] + new Vector2(0, -76), new Vector2(180, 42), TextAnchor.MiddleCenter, Color.white);
                InvisibleButton("Seleccionar Icono " + (i + 1), positions[i], new Vector2(220, 220), () =>
                {
                    selectedIcon = index;
                    for (var m = 0; m < iconRings.Count; m++)
                    {
                        iconRings[m].GetComponent<Image>().color = m == selectedIcon ? new Color(0.87f, 0.2f, 1f, 0.24f) : new Color(1f, 1f, 1f, 0.08f);
                    }
                });
            }

            Image(root, LoadSprite("burbuja"), new Vector2(795, -330), new Vector2(112, 112), true);
            var confirm = Button("", new Vector2(795, -330), new Vector2(105, 76), new Color(1, 1, 1, 0.001f), () =>
            {
                activeProfile = new EcoProfile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = profileName,
                    IconIndex = selectedIcon,
                    Language = "Espanol",
                    MusicVolume = 0.7f,
                    EffectsVolume = 0.8f
                };
                store.Profiles.Add(activeProfile);
                selectedSection = "Inicio";
                searchText = string.Empty;
                SaveProfiles();
                ShowMainMenu();
            });
            Text(confirm.GetComponent<RectTransform>(), "✓", 54, Vector2.zero, new Vector2(90, 70), TextAnchor.MiddleCenter, new Color(0.12f, 0.16f, 0.15f));
        }

        private void ShowMainMenu()
        {
            if (activeProfile == null)
            {
                ShowProfiles();
                return;
            }

            StopVideoMode();
            ClearUi();
            Background("fondo principal");

            Text(root, "Hola, " + activeProfile.Name, 31, new Vector2(-720, 365), new Vector2(380, 58), TextAnchor.MiddleLeft, Color.white);
            Image(root, LoadSprite("burbuja"), new Vector2(-520, 370), new Vector2(104, 104), true);
            Image(root, IconFor(activeProfile.IconIndex), new Vector2(-520, 370), new Vector2(86, 86), true);
            Image(root, LoadSprite("burbuja"), new Vector2(0, 400), new Vector2(185, 185), true);
            Image(root, LoadSprite("logo eco aventuras"), new Vector2(0, 402), new Vector2(238, 189), true);

            Image(root, LoadSprite("submenu izquierdo"), new Vector2(-760, -90), new Vector2(150, 560), false);
            MenuButton("Inicio", "menu icono", new Vector2(-760, 95), () =>
            {
                selectedSection = "Inicio";
                searchText = string.Empty;
                ShowMainMenu();
            }, selectedSection == "Inicio");
            MenuButton("Favoritos", "me gusta icono", new Vector2(-760, -30), () =>
            {
                selectedSection = "Favoritos";
                searchText = string.Empty;
                ShowMainMenu();
            }, selectedSection == "Favoritos");
            MenuButton("Ajustes", "boton herramienta", new Vector2(-760, -155), ShowSettings, false);

            Image(root, LoadSprite("bloque buscar"), new Vector2(170, 235), new Vector2(1010, 86), false);
            Image(root, LoadSprite("buscar icono"), new Vector2(-285, 235), new Vector2(50, 50), true);
            Image(root, LoadSprite("microfono icono"), new Vector2(620, 235), new Vector2(46, 46), true);
            var input = Input(null, "", new Vector2(170, 235), new Vector2(830, 54));
            input.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            input.textComponent.color = Color.white;
            input.textComponent.fontSize = 27;
            input.text = searchText;
            input.onValueChanged.AddListener(value =>
            {
                searchText = value;
            });
            InvisibleButton("Buscar", new Vector2(620, 235), new Vector2(110, 60), ShowMainMenu);

            Image(root, LoadSprite("scrolbar"), new Vector2(795, -92), new Vector2(76, 500), false);
            Image(root, LoadSprite("scrolbar agarre"), new Vector2(795, -28), new Vector2(72, 190), false);

            if (selectedSection == "Favoritos")
            {
                ShowFavoritesOverlay();
                return;
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                ShowSearchOverlay();
                return;
            }

            ShowExperienceGrid();
        }

        private void MenuButton(string label, string iconName, Vector2 pos, UnityEngine.Events.UnityAction action, bool selected)
        {
            if (selected)
            {
                Image(root, LoadSprite("selección menú izquierdo"), pos, new Vector2(112, 112), false);
            }

            Button("", pos, new Vector2(116, 116), new Color(1, 1, 1, 0.001f), action);
            Image(root, LoadSprite(iconName), pos + new Vector2(0, 24), new Vector2(50, 50), true);
            Text(root, label, 21, pos + new Vector2(0, -34), new Vector2(120, 44), TextAnchor.MiddleCenter, Color.white);
        }

        private void ShowExperienceGrid()
        {
            var gridPositions = new[]
            {
                new Vector2(80, -35)
            };

            for (var i = 0; i < gridPositions.Length && i < experiences.Count; i++)
            {
                ExperienceCard(experiences[i], gridPositions[i]);
            }
        }

        private void ShowFavoritesOverlay()
        {
            var panel = Panel("FavoritesOverlay", new Vector2(270, -50), new Vector2(1020, 580), new Color(0.08f, 0.35f, 0.45f, 0.72f));
            Text(panel, "Favoritos", 42, new Vector2(0, 235), new Vector2(500, 60), TextAnchor.MiddleCenter, Color.white);
            var favorites = experiences.Where(e => activeProfile.IsFavorite(e.Id)).ToList();
            if (favorites.Count == 0)
            {
                Text(panel, "Marca el corazon en una ficha para guardar una experiencia aqui.", 28, new Vector2(0, 30), new Vector2(780, 100), TextAnchor.MiddleCenter, Color.white);
                return;
            }

            for (var i = 0; i < favorites.Count; i++)
            {
                var experience = favorites[i];
                var y = 145 - i * 88;
                Image(panel, LoadSprite("burbuja"), new Vector2(-345, y), new Vector2(72, 72), true);
                Text(panel, experience.Title, 28, new Vector2(-35, y), new Vector2(560, 58), TextAnchor.MiddleLeft, Color.white);
                Button(panel, "Abrir", new Vector2(320, y), new Vector2(150, 52), new Color(0.54f, 0.32f, 1f, 0.85f), () => ShowVideoDetail(experience));
            }
        }

        private void ShowSearchOverlay()
        {
            var matches = experiences.Where(e => e.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var panel = Panel("SearchOverlay", new Vector2(265, 90), new Vector2(760, 310), new Color(0.05f, 0.22f, 0.32f, 0.7f));
            Text(panel, $"Busqueda: {searchText}", 28, new Vector2(0, 115), new Vector2(640, 50), TextAnchor.MiddleLeft, Color.white);
            if (matches.Count == 0)
            {
                Text(panel, "Sin coincidencias.", 24, new Vector2(0, 35), new Vector2(620, 50), TextAnchor.MiddleCenter, Color.white);
                return;
            }

            for (var i = 0; i < Math.Min(matches.Count, 3); i++)
            {
                var experience = matches[i];
                Button(panel, experience.Title, new Vector2(0, 45 - i * 72), new Vector2(620, 54), new Color(1f, 1f, 1f, 0.24f), () => ShowVideoDetail(experience));
            }
        }

        private void ShowAchievementsOverlay()
        {
            var panel = Panel("AchievementsOverlay", new Vector2(270, -50), new Vector2(920, 500), new Color(0.08f, 0.35f, 0.45f, 0.72f));
            Text(panel, "Logros", 42, new Vector2(0, 185), new Vector2(500, 60), TextAnchor.MiddleCenter, Color.white);
            Text(panel, $"{activeProfile.TotalStars()} estrellas ganadas", 32, new Vector2(0, 70), new Vector2(580, 56), TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.24f));
            Text(panel, "Completa interacciones para llenar tu progreso.", 26, new Vector2(0, -10), new Vector2(650, 80), TextAnchor.MiddleCenter, Color.white);
            Button(panel, "Volver", new Vector2(0, -165), new Vector2(210, 58), new Color(1f, 1f, 1f, 0.28f), ShowMainMenu);
        }

        private void ShowUploadOverlay()
        {
            var panel = Panel("UploadOverlay", new Vector2(270, -50), new Vector2(920, 500), new Color(0.08f, 0.35f, 0.45f, 0.72f));
            Text(panel, "Subir 360", 42, new Vector2(0, 185), new Vector2(500, 60), TextAnchor.MiddleCenter, Color.white);
            Text(panel, "Modulo reservado para agregar nuevas experiencias 360 al catalogo.", 26, new Vector2(0, 40), new Vector2(650, 100), TextAnchor.MiddleCenter, Color.white);
            Button(panel, "Volver", new Vector2(0, -165), new Vector2(210, 58), new Color(1f, 1f, 1f, 0.28f), ShowMainMenu);
        }

        private void Sidebar()
        {
            var side = Panel("Sidebar", new Vector2(-810, 0), new Vector2(230, 1080), new Color(0.02f, 0.18f, 0.13f, 0.92f));
            Image(side, IconFor(activeProfile != null ? activeProfile.IconIndex : 0), new Vector2(0, 405), new Vector2(86, 86), true);
            Button(side, "Perfiles", new Vector2(0, 315), new Vector2(170, 52), new Color(0.08f, 0.42f, 0.28f), ShowProfiles);
            NavButton(side, "Inicio", new Vector2(0, 200));
            NavButton(side, "Favoritos", new Vector2(0, 125));
            Button(side, "Ajustes", new Vector2(0, 50), new Vector2(170, 54), new Color(0.07f, 0.34f, 0.24f), ShowSettings);
        }

        private void NavButton(RectTransform parent, string section, Vector2 pos)
        {
            var color = selectedSection == section ? new Color(0.95f, 0.7f, 0.16f) : new Color(0.07f, 0.34f, 0.24f);
            Button(parent, section, pos, new Vector2(170, 54), color, () =>
            {
                selectedSection = section;
                searchText = string.Empty;
                ShowMainMenu();
            });
        }

        private void ExperienceCard(Experience experience, Vector2 pos)
        {
            Image(root, LoadSprite("burbuja"), pos, new Vector2(255, 255), true);
            Image(root, LoadSprite(experience.PreviewSpriteName), pos + new Vector2(0, 16), new Vector2(178, 136), false);
            Panel("Video Label Shade", pos + new Vector2(0, -50), new Vector2(184, 66), new Color(0, 0, 0, 0.24f));
            Text(root, experience.Title, 17, pos + new Vector2(0, -40), new Vector2(170, 42), TextAnchor.MiddleCenter, Color.white);
            Text(root, "360", 20, pos + new Vector2(0, -78), new Vector2(90, 28), TextAnchor.MiddleCenter, Color.white);
            InvisibleButton("Abrir " + experience.Id, pos, new Vector2(230, 230), () => ShowVideoDetail(experience));
            var fav = Button("", pos + new Vector2(86, 82), new Vector2(54, 42), new Color(1f, 1f, 1f, 0.001f), () =>
            {
                if (activeProfile == null)
                {
                    return;
                }

                activeProfile.ToggleFavorite(experience.Id);
                SaveProfiles();
                ShowMainMenu();
            });
            if (activeProfile != null && activeProfile.IsFavorite(experience.Id))
            {
                Image(root, LoadSprite("me gusta icono"), pos + new Vector2(86, 82), new Vector2(42, 42), true);
            }
        }

        private void VideoCard(RectTransform parent, Experience experience)
        {
            var card = Panel(experience.Id, Vector2.zero, new Vector2(395, 230), new Color(1, 1, 1, 0.94f), parent);
            Image(card, LoadSprite(experience.PreviewSpriteName), new Vector2(0, 45), new Vector2(365, 120), false);
            Text(card, experience.Title, 24, new Vector2(-10, -42), new Vector2(330, 40), TextAnchor.MiddleLeft, new Color(0.04f, 0.18f, 0.13f));
            Text(card, StarsFor(experience), 24, new Vector2(-104, -78), new Vector2(160, 34), TextAnchor.MiddleLeft, new Color(0.9f, 0.56f, 0.08f));
            Text(card, activeProfile.IsFavorite(experience.Id) ? "Favorito" : "Disponible", 18, new Vector2(95, -80), new Vector2(150, 32), TextAnchor.MiddleRight, new Color(0.18f, 0.34f, 0.26f));
            Button(card, "Ver ficha", new Vector2(105, -82), new Vector2(150, 44), new Color(0.1f, 0.52f, 0.34f), () => ShowVideoDetail(experience));
        }

        private void ShowSettings()
        {
            ClearUi();
            Background("fondo principal");
            Image(root, LoadSprite("logo eco aventuras"), new Vector2(0, 360), new Vector2(260, 207), true);
            Image(root, LoadSprite("boton herramienta"), new Vector2(760, 360), new Vector2(95, 95), true);
            InvisibleButton("VolverMenuAjustes", new Vector2(-785, 390), new Vector2(170, 90), ShowMainMenu);

            var panel = Panel("Settings", new Vector2(0, -70), new Vector2(1050, 660), new Color(0.55f, 0.9f, 1f, 0.36f));
            Text(panel, "Ajustes", 46, new Vector2(0, 260), new Vector2(420, 70), TextAnchor.MiddleCenter, Color.white);
            Text(panel, "Idioma", 34, new Vector2(-350, 165), new Vector2(260, 58), TextAnchor.MiddleLeft, Color.white);
            Button(panel, activeProfile.Language == "Espanol" ? "Espanol seleccionado" : "Espanol", new Vector2(55, 165), new Vector2(430, 58), new Color(0.54f, 0.32f, 1f, 0.85f), () =>
            {
                activeProfile.Language = "Espanol";
                SaveProfiles();
                ShowSettings();
            });
            Button(panel, activeProfile.Language == "English" ? "English selected" : "English", new Vector2(55, 90), new Vector2(430, 58), new Color(0.25f, 0.65f, 0.95f, 0.82f), () =>
            {
                activeProfile.Language = "English";
                SaveProfiles();
                ShowSettings();
            });

            Text(panel, "Sonido", 34, new Vector2(-350, -5), new Vector2(260, 58), TextAnchor.MiddleLeft, Color.white);
            SliderWithLabel(panel, "Musica", activeProfile.MusicVolume, new Vector2(80, -5), value =>
            {
                activeProfile.MusicVolume = value;
                SaveProfiles();
            });
            SliderWithLabel(panel, "Efectos", activeProfile.EffectsVolume, new Vector2(80, -95), value =>
            {
                activeProfile.EffectsVolume = value;
                SaveProfiles();
            });

            Text(panel, "Icono", 34, new Vector2(-350, -210), new Vector2(260, 58), TextAnchor.MiddleLeft, Color.white);
            for (var i = 0; i < profileIcons.Count; i++)
            {
                var index = i;
                var b = Button(panel, "", new Vector2(-125 + i * 88, -210), new Vector2(72, 72), index == activeProfile.IconIndex ? new Color(0.9f, 0.18f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.35f), () =>
                {
                    activeProfile.IconIndex = index;
                    SaveProfiles();
                    ShowSettings();
                });
                Image(b.GetComponent<RectTransform>(), profileIcons[i], Vector2.zero, new Vector2(58, 58), true);
            }
            Button(panel, "Volver", new Vector2(0, -295), new Vector2(220, 58), new Color(1f, 1f, 1f, 0.28f), ShowMainMenu);
        }

        private void ShowVideoDetail(Experience experience)
        {
            selectedExperience = experience;
            ClearUi();
            Background("referencia letrero");

            var earnedStars = activeProfile != null ? activeProfile.GetStars(experience.Id, experience.Stars) : experience.Stars;
            RenderStars(root, new Vector2(-302, -248), earnedStars, 5, 53f);
            Image(root, LoadSprite("boton me gusta"), new Vector2(360, 260), new Vector2(96, 96), true);
            if (activeProfile != null && activeProfile.IsFavorite(experience.Id))
            {
                Image(root, LoadSprite("me gusta"), new Vector2(360, 260), new Vector2(70, 52), true);
            }

            InvisibleButton("VolverFicha", new Vector2(-560, 260), new Vector2(250, 90), ShowMainMenu);
            InvisibleButton("FavoritoFicha", new Vector2(360, 260), new Vector2(110, 110), () =>
            {
                if (activeProfile == null)
                {
                    return;
                }

                activeProfile.ToggleFavorite(experience.Id);
                SaveProfiles();
                ShowVideoDetail(experience);
            });
            InvisibleButton("IniciarFicha", new Vector2(20, -365), new Vector2(360, 105), () => StartExperience(experience));
        }

        private void RenderStars(RectTransform parent, Vector2 start, int earned, int total, float spacing)
        {
            for (var i = 0; i < total; i++)
            {
                var pos = start + new Vector2(i * spacing, 0);
                Image(parent, LoadSprite("estrella vacia"), pos, new Vector2(48, 48), true);
                if (i < earned)
                {
                    Image(parent, LoadSprite("estrella"), pos, new Vector2(52, 52), true);
                }
            }
        }

        private void StartExperience(Experience experience)
        {
            ClearUi();
            StartVideoMode(experience);
            BuildPlaybackOverlay();
            ShowInteractionSequence(experience);
        }

        private void StartVideoMode(Experience experience)
        {
            vrCamera.transform.position = Vector3.zero;
            vrCamera.transform.rotation = Quaternion.identity;
            vrCamera.clearFlags = CameraClearFlags.Skybox;

            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Eco360 Video Sphere";
            sphere.transform.position = Vector3.zero;
            sphere.transform.localScale = new Vector3(-36, 36, 36);

            videoTexture = new RenderTexture(2048, 1024, 0);
            var material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = videoTexture;
            sphere.GetComponent<Renderer>().material = material;

            videoPlayer = sphere.AddComponent<VideoPlayer>();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            if (experience.Clip != null)
            {
                videoPlayer.clip = experience.Clip;
                videoPlayer.Prepare();
                videoPlayer.prepareCompleted += player => player.Play();
            }
        }

        private void StopVideoMode()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                Destroy(videoPlayer);
                videoPlayer = null;
            }

            if (sphere != null)
            {
                Destroy(sphere);
                sphere = null;
            }

            if (videoTexture != null)
            {
                videoTexture.Release();
                Destroy(videoTexture);
                videoTexture = null;
            }

            if (vrCamera != null)
            {
                vrCamera.clearFlags = CameraClearFlags.SolidColor;
                vrCamera.backgroundColor = new Color(0.02f, 0.08f, 0.07f);
            }
        }

        private void BuildPlaybackOverlay()
        {
            var top = Panel("PlayerControls", new Vector2(0, -460), new Vector2(1550, 120), new Color(0, 0, 0, 0.62f));
            Button(top, "Salir", new Vector2(-680, 0), new Vector2(150, 58), new Color(0.5f, 0.18f, 0.16f), () => ShowVideoDetail(selectedExperience));
            Button(top, "Pausa", new Vector2(-505, 0), new Vector2(150, 58), new Color(0.13f, 0.45f, 0.33f), () =>
            {
                if (videoPlayer == null) return;
                if (videoPlayer.isPlaying) videoPlayer.Pause();
                else videoPlayer.Play();
            });
            playbackSlider = Slider(top, new Vector2(130, 0), new Vector2(860, 32), 0, value =>
            {
                if (videoPlayer != null && videoPlayer.isPrepared)
                {
                    videoPlayer.time = value * videoPlayer.length;
                }
            });
            playbackSlider.onValueChanged.AddListener(_ => { });
            var drag = playbackSlider.gameObject.AddComponent<PlaybackDragState>();
            drag.OnBegin = () => draggingPlayback = true;
            drag.OnEnd = () => draggingPlayback = false;
            playbackTime = Text(top, "00:00 / 00:00", 22, new Vector2(620, 0), new Vector2(280, 45), TextAnchor.MiddleCenter, Color.white);
        }

        private void ShowInteractionSequence(Experience experience)
        {
            var panel = Panel("InteractionPanel", new Vector2(0, 330), new Vector2(1360, 210), new Color(0.03f, 0.13f, 0.1f, 0.75f));
            var steps = new Queue<MiniTask>(new[]
            {
                new MiniTask("Romper el cascaron", "Realiza 3 picoteos para salir del huevo.", "Picotear", 3),
                new MiniTask("Captura de peces", "Atrapa 3 peces para continuar el video.", "Atrapar pez", 3),
                new MiniTask("Tocar venado", "Interactua con el venado cuando aparezca la guia visual.", "Tocar venado", 1),
                new MiniTask("Tocar la flor", "Toca la flor para completar la accion.", "Tocar flor", 1),
                new MiniTask("Semillas helicoptero", "Atrapa 3 semillas antes de que caigan.", "Atrapar semilla", 3),
                new MiniTask("Aleteo", "Aletea 3 veces para volar con Lagu.", "Aletear", 3),
                new MiniTask("Recoger basura", "Encuentra y recoge 5 residuos del entorno.", "Recoger", 5),
                new MiniTask("Clasificar residuos", "Coloca cada residuo en el tacho correcto.", "Clasificar", 4)
            });

            void RenderNext()
            {
                foreach (Transform child in panel)
                {
                    Destroy(child.gameObject);
                }

                if (steps.Count == 0)
                {
                    activeProfile.SetStars(experience.Id, 5);
                    SaveProfiles();
                    Text(panel, "Actividad completada. Ganaste 5 estrellas.", 32, new Vector2(-130, 30), new Vector2(860, 70), TextAnchor.MiddleLeft, Color.white);
                    Button(panel, "Volver a ficha", new Vector2(480, 0), new Vector2(260, 60), new Color(0.1f, 0.56f, 0.32f), () => ShowVideoDetail(experience));
                    return;
                }

                var task = steps.Dequeue();
                RenderTask(panel, task, RenderNext);
            }

            RenderNext();
        }

        private void RenderTask(RectTransform panel, MiniTask task, Action complete)
        {
            var count = 0;
            var title = Text(panel, task.Title, 30, new Vector2(-470, 42), new Vector2(390, 48), TextAnchor.MiddleLeft, Color.white);
            Text(panel, task.Description, 22, new Vector2(-210, -18), new Vector2(780, 78), TextAnchor.UpperLeft, new Color(0.92f, 1f, 0.92f));
            var counter = Text(panel, $"0 / {task.RequiredCount}", 30, new Vector2(300, 35), new Vector2(160, 50), TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.25f));
            Button(panel, task.ActionLabel, new Vector2(500, 20), new Vector2(260, 62), new Color(0.1f, 0.56f, 0.32f), () =>
            {
                count++;
                counter.text = $"{Mathf.Min(count, task.RequiredCount)} / {task.RequiredCount}";
                title.text = count >= task.RequiredCount ? $"{task.Title} listo" : task.Title;
                if (count >= task.RequiredCount)
                {
                    complete();
                }
            });
            Button(panel, "Repetir guia", new Vector2(500, -58), new Vector2(260, 50), new Color(0.23f, 0.38f, 0.58f), () =>
            {
                counter.text = $"{count} / {task.RequiredCount}";
            });
        }

        private string StarsFor(Experience experience)
        {
            var stars = Mathf.Clamp(activeProfile != null ? activeProfile.GetStars(experience.Id, experience.Stars) : experience.Stars, 0, 5);
            return new string('*', stars) + new string('-', 5 - stars);
        }

        private string FormatTime(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time)) return "00:00";
            var total = Mathf.Max(0, Mathf.FloorToInt((float)time));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private Sprite IconFor(int index)
        {
            if (profileIcons.Count == 0)
            {
                return null;
            }

            return profileIcons[Mathf.Abs(index) % profileIcons.Count];
        }

        private Sprite LoadSprite(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            if (spriteCache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(SpritePath + resourceName);
            if (texture == null)
            {
                spriteCache[resourceName] = null;
                return null;
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            spriteCache[resourceName] = sprite;
            return sprite;
        }

        private string NicifyName(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? "Experiencia 360" : raw.Replace("_", " ").Trim();
        }

        private void Background(string spriteName)
        {
            var image = Image(root, LoadSprite(spriteName), Vector2.zero, new Vector2(1920, 1080), false);
            image.color = Color.white;
            image.transform.SetAsFirstSibling();
        }

        private void TopLogo(string title)
        {
            Image(root, LoadSprite("logo eco aventuras"), new Vector2(-650, 455), new Vector2(240, 100), true);
            TextBlock(title, 42, new Vector2(0, 460), new Vector2(820, 70), TextAnchor.MiddleCenter);
        }

        private Text TextBlock(string value, int size, Vector2 pos, Vector2 dimensions, TextAnchor anchor)
        {
            return Text(root, value, size, pos, dimensions, anchor, Color.white);
        }

        private RectTransform Panel(string name, Vector2 anchoredPosition, Vector2 size, Color color, RectTransform parent = null)
        {
            parent = parent == null ? root : parent;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private Image Image(RectTransform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size, bool preserveAspect)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.color = sprite == null ? new Color(0.08f, 0.24f, 0.18f, 0.95f) : Color.white;
            image.raycastTarget = false;
            return image;
        }

        private Text Text(RectTransform parent, string value, int size, Vector2 anchoredPosition, Vector2 dimensions, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = dimensions;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Button InvisibleButton(string name, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var button = Button(root, "", anchoredPosition, size, new Color(1, 1, 1, 0), onClick);
            button.name = name;
            button.GetComponent<Image>().raycastTarget = true;
            return button;
        }

        private Button Button(string label, Vector2 anchoredPosition, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            return Button(root, label, anchoredPosition, size, color, onClick);
        }

        private Button Button(RectTransform parent, string label, Vector2 anchoredPosition, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
            if (!string.IsNullOrEmpty(label))
            {
                Text(rect, label, 24, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
            }
            return button;
        }

        private InputField Input(RectTransform parent, string placeholder, Vector2 anchoredPosition, Vector2 size)
        {
            parent = parent == null ? root : parent;
            var panel = Panel("Input", anchoredPosition, size, Color.white, parent);
            var input = panel.gameObject.AddComponent<InputField>();
            var text = Text(panel, "", 24, Vector2.zero, new Vector2(size.x - 36, size.y - 10), TextAnchor.MiddleLeft, Color.black);
            var place = Text(panel, placeholder, 24, Vector2.zero, new Vector2(size.x - 36, size.y - 10), TextAnchor.MiddleLeft, new Color(0.38f, 0.45f, 0.42f));
            input.textComponent = text;
            input.placeholder = place;
            return input;
        }

        private ScrollRect Scroll(Vector2 anchoredPosition, Vector2 size)
        {
            var viewport = Panel("ScrollViewport", anchoredPosition, size, new Color(0, 0, 0, 0.22f));
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Content").AddComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 1000);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            return scroll;
        }

        private Slider Slider(RectTransform parent, Vector2 anchoredPosition, Vector2 size, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var holder = Panel("Slider", anchoredPosition, size, new Color(1, 1, 1, 0.2f), parent);
            var fill = Panel("Fill", Vector2.zero, size, new Color(0.15f, 0.72f, 0.42f), holder);
            fill.anchorMin = new Vector2(0, 0);
            fill.anchorMax = new Vector2(1, 1);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var handle = Panel("Handle", Vector2.zero, new Vector2(28, size.y + 10), Color.white, holder);

            var slider = holder.gameObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = value;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private void SliderWithLabel(RectTransform parent, string label, float value, Vector2 position, UnityEngine.Events.UnityAction<float> changed)
        {
            Text(parent, label, 24, new Vector2(position.x - 330, position.y), new Vector2(160, 45), TextAnchor.MiddleLeft, new Color(0.08f, 0.22f, 0.16f));
            Slider(parent, position, new Vector2(500, 30), value, changed);
        }

        [Serializable]
        private class ProfileStore
        {
            public List<EcoProfile> Profiles = new List<EcoProfile>();
        }

        [Serializable]
        private class EcoProfile
        {
            public string Id;
            public string Name;
            public int IconIndex;
            public string Language;
            public float MusicVolume;
            public float EffectsVolume;
            public List<string> Favorites = new List<string>();
            public List<VideoProgress> Progress = new List<VideoProgress>();

            public bool IsFavorite(string id)
            {
                return Favorites.Contains(id);
            }

            public void ToggleFavorite(string id)
            {
                if (Favorites.Contains(id)) Favorites.Remove(id);
                else Favorites.Add(id);
            }

            public int GetStars(string id, int fallback)
            {
                var progress = Progress.FirstOrDefault(p => p.VideoId == id);
                return progress == null ? fallback : progress.Stars;
            }

            public void SetStars(string id, int stars)
            {
                var progress = Progress.FirstOrDefault(p => p.VideoId == id);
                if (progress == null)
                {
                    Progress.Add(new VideoProgress { VideoId = id, Stars = stars });
                }
                else
                {
                    progress.Stars = Mathf.Max(progress.Stars, stars);
                }
            }

            public int TotalStars()
            {
                return Progress.Sum(p => p.Stars);
            }
        }

        [Serializable]
        private class VideoProgress
        {
            public string VideoId;
            public int Stars;
        }

        private class Experience
        {
            public string Id;
            public string Title;
            public string Description;
            public string PreviewSpriteName;
            public VideoClip Clip;
            public int Stars;
        }

        private class MiniTask
        {
            public readonly string Title;
            public readonly string Description;
            public readonly string ActionLabel;
            public readonly int RequiredCount;

            public MiniTask(string title, string description, string actionLabel, int requiredCount)
            {
                Title = title;
                Description = description;
                ActionLabel = actionLabel;
                RequiredCount = requiredCount;
            }
        }
    }

    public class PlaybackDragState : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public Action OnBegin;
        public Action OnEnd;

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBegin?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEnd?.Invoke();
        }
    }
}
