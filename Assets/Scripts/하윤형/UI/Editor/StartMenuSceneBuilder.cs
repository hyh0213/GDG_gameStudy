using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DarkMaze.Editor
{
    /// <summary>Creates real, editable scene objects through the Editor, never runtime-only UI.</summary>
    public static class StartMenuSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/하윤형/StartMenu.unity";
        public const string PrefabPath = "Assets/UI/하윤형/Prefabs/StartMenu.prefab";
        private const string Root = "Assets/UI/하윤형/";
        private static readonly Color Ivory = Hex("E1DFD2");
        private static readonly Color Gold = Hex("B8A078");
        private static readonly Color Muted = Hex("8C9692");
        private static Font bodyFont;
        private static Font titleFont;

        [MenuItem("Tools/하윤형/UI/Create Start Menu")]
        public static void Create()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before creating the start menu.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("StartMenu already exists. Open it to edit it; the builder will not overwrite your work.");

            int rendererIndex = FindExisting3DRenderer();
            // Never overwrite existing personal art or close another contributor's scene.
            var reusableFiles = new HashSet<string>
            {
                Root + "Art/Menu3DRenderer.asset", Root + "Art/Menu3DRenderer.asset.meta"
            };
            foreach (string folder in new[] { Root + "Art", Root + "Materials", Root + "Prefabs" })
                if (Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                    .Select(path => path.Replace('\\', '/')).Any(path => !reusableFiles.Contains(path)))
                    throw new InvalidOperationException("Existing menu resources will not be overwritten: " + folder);
            bool untitled = Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path));
            if (untitled)
                throw new InvalidOperationException("Save your untitled scene before creating a separate menu scene.");

            EnsureFolder("Assets/Scenes/하윤형");
            EnsureFolder(Root + "Art");
            EnsureFolder(Root + "Materials");
            AssetDatabase.Refresh();
            bodyFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/GothicA1-Regular.ttf");
            titleFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/GowunBatang-Regular.ttf");
            if (bodyFont == null || titleFont == null)
                throw new InvalidOperationException("The OFL fonts must be imported before building the menu.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            Camera camera = CreateCorridor(rendererIndex, out Light lamp);
            Canvas canvas = CreateCanvas();
            RectTransform full = canvas.transform as RectTransform;

            Sprite leftShade = TextureSprite("LeftShade", 512, 2, (x, y) =>
            {
                float t = x / 511f;
                return new Color(0.018f, 0.027f, 0.029f, Mathf.Lerp(0.99f, 0f, Mathf.SmoothStep(0.18f, 0.92f, t)));
            });
            Stretch(Image("Left cinematic shade", full, Color.white, leftShade));
            Sprite vignette = TextureSprite("Vignette", 512, 288, (x, y) =>
            {
                float dx = (x / 511f - 0.52f) * 1.45f;
                float dy = (y / 287f - 0.5f) * 1.7f;
                float a = Mathf.SmoothStep(0f, 0.82f, Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.2f) / 0.8f));
                return new Color(0.008f, 0.016f, 0.017f, a);
            });
            Stretch(Image("Soft edge vignette", full, Color.white, vignette));

            RectTransform layout = Rect("Menu Layout", full, 1920f, 1080f);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(0.5f, 0.5f);
            layout.anchoredPosition = Vector2.zero;
            CanvasGroup menu = layout.gameObject.AddComponent<CanvasGroup>();

            RectTransform header = Rect("Header", layout, 1720, 50, 100, 63);
            DrawMazeMark(header, 0, 0, 42, Gold);
            Label("Studio signature", header, "D A R K   L A B Y R I N T H", 14, Gold, 490, 32, 64, 5);
            Label("Headphone recommendation", header, "헤드폰 착용을 권장합니다", 16, Muted, 320, 35, 1400, 4, TextAnchor.MiddleRight);
            Line("Header divider", layout, 100, 133, 1720, 1, Alpha(Ivory, 0.12f));

            Label("Genre label", layout, "S U R V I V A L   /   E X P L O R A T I O N", 13, Gold, 660, 30, 141, 248);
            Label("Game title", layout, "암흑 미로", 112, Ivory, 760, 160, 126, 293, TextAnchor.MiddleLeft, titleFont);
            Label("English title", layout, "T H E   D A R K   L A B Y R I N T H", 15, Alpha(Ivory, 0.6f), 700, 36, 142, 448);
            Label("Tagline", layout, "빛이 닿지 않는 곳,\n출구는 당신을 기다리지 않는다.", 24, Hex("B9C0BA"), 680, 90, 140, 511);
            Line("Menu divider", layout, 142, 637, 460, 1, Alpha(Gold, 0.28f));

            Button start = MenuButton(layout, "Start Button", "게임 시작", "ENTER THE MAZE", 140, 667, true);
            Button help = MenuButton(layout, "Help Button", "조작 안내", "HOW TO SURVIVE", 140, 757, false);
            Button quit = MenuButton(layout, "Quit Button", "게임 종료", "LEAVE THE DARK", 140, 843, false);
            LinkNavigation(start, help, quit);

            // Quiet cartographic detail, not another competing information panel.
            RectTransform map = Rect("Drifting maze annotation", layout, 220, 220, 1560, 190);
            DrawMazeMark(map, 0, 0, 208, Alpha(Gold, 0.13f));
            Label("Map caption", layout, "S E C T O R   0 1\nUNKNOWN DEPTH", 11, Alpha(Ivory, 0.38f), 250, 46, 1562, 421);
            Line("Location marker", layout, 1288, 772, 1, 52, Alpha(Gold, 0.65f));
            Label("Location annotation", layout, "01  /  입구\n돌아갈 길을 기억하세요.", 15, Alpha(Ivory, 0.68f), 420, 66, 1310, 773);

            Line("Footer divider", layout, 100, 983, 1720, 1, Alpha(Ivory, 0.12f));
            Label("Footer", layout, "GDGoC  /  GAME STUDY", 12, Alpha(Ivory, 0.46f), 450, 35, 100, 1001);
            Label("Keyboard hint", layout, "방향키 / Tab  이동     Enter  선택", 13, Alpha(Ivory, 0.5f), 600, 35, 660, 1001, TextAnchor.MiddleCenter);
            Label("Version", layout, "PROTOTYPE   /   v0.1", 12, Alpha(Ivory, 0.46f), 360, 35, 1460, 1001, TextAnchor.MiddleRight);

            MainMenuController controller = new GameObject("Main Menu Controller").AddComponent<MainMenuController>();
            BuildDialog(full, controller);
            Image fadeImage = Image("Screen fade", full, new Color(0.012f, 0.02f, 0.021f, 1f));
            Stretch(fadeImage);
            fadeImage.raycastTarget = true;
            CanvasGroup fade = fadeImage.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;
            Text loading = Label("Loading label", fadeImage.transform, "미로로 들어가는 중…", 24, Ivory, 1000, 70, 0, 0, TextAnchor.MiddleCenter);
            loading.rectTransform.anchorMin = loading.rectTransform.anchorMax = loading.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            loading.rectTransform.anchoredPosition = Vector2.zero;
            loading.gameObject.SetActive(false);
            SetRefs(controller, ("startButton", start), ("helpButton", help), ("quitButton", quit),
                ("menuGroup", menu), ("screenFade", fade), ("loadingLabel", loading));

            MenuAtmosphere atmosphere = controller.gameObject.AddComponent<MenuAtmosphere>();
            SetRefs(atmosphere, ("menuCamera", camera.transform), ("corridorLamp", lamp), ("driftingMap", map));

            var events = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.GetComponent<EventSystem>().firstSelectedGameObject = start.gameObject;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            CreatePrefabForCurrentScene();
            MenuSettingsBuilder.AddSettings();
            MenuCursorBuilder.AddCursor();
            // Save only our own generated assets, never unrelated dirty project settings.
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Root.TrimEnd('/') }))
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    AssetDatabase.SaveAssetIfDirty(asset);
            }
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Dark Maze start menu created: " + ScenePath);
        }

        [MenuItem("Tools/하윤형/UI/Create Menu Prefab")]
        public static void CreatePrefabForCurrentScene()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before creating the menu prefab.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open the personal StartMenu scene. Shared scenes will not be modified.");
            if (File.Exists(PrefabPath))
                throw new InvalidOperationException("The menu prefab already exists and will not be overwritten.");

            EnsureFolder(Root + "Prefabs");
            GameObject[] roots = scene.GetRootGameObjects();
            var menuRoot = new GameObject("HYH Start Menu");
            Undo.RegisterCreatedObjectUndo(menuRoot, "Group personal start menu");
            foreach (GameObject child in roots)
                Undo.SetTransformParent(child.transform, menuRoot.transform, "Group personal start menu");
            PrefabUtility.SaveAsPrefabAssetAndConnect(menuRoot, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene);
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Start Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Expand the safe composition on wider/taller screens rather than cropping buttons.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvas;
        }

        private static Button MenuButton(Transform parent, string name, string title, string subtitle,
            float x, float y, bool primary, float width = 470f)
        {
            RectTransform rt = Rect(name, parent, width, 76f, x, y);
            Image background = rt.gameObject.AddComponent<Image>();
            background.color = primary ? new Color(0.48f, 0.42f, 0.3f, 0.16f) : new Color(0, 0, 0, 0.01f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            Image hover = Image("Focus glow", rt, new Color(0.56f, 0.48f, 0.34f, 0.16f));
            Stretch(hover);
            CanvasGroup glow = hover.gameObject.AddComponent<CanvasGroup>();
            glow.alpha = 0f;
            glow.blocksRaycasts = false;
            Line("Focus edge", hover.transform, 0, 0, 3, 76, Gold);
            Line("Top edge", rt, 0, 0, width, 1, Alpha(Gold, primary ? 0.7f : 0.15f));
            Line("Bottom edge", rt, 0, 75, width, 1, Alpha(Gold, primary ? 0.7f : 0.15f));
            Label("Label", rt, title, 25, primary ? Ivory : Hex("BDC3BC"), width - 110, 42, 26, 5);
            Label("English caption", rt, subtitle, 10, primary ? Gold : Alpha(Ivory, 0.42f), width - 110, 22, 28, 46);
            RectTransform arrow = Rect("Arrow", rt, 28, 28, width - 57, 24);
            Line("Arrow stem", arrow, 0, 13, 22, 1, Gold);
            RectTransform upper = Line("Arrow upper", arrow, 16, 10, 8, 1, Gold);
            upper.localRotation = Quaternion.Euler(0f, 0f, -40f);
            RectTransform lower = Line("Arrow lower", arrow, 16, 16, 8, 1, Gold);
            lower.localRotation = Quaternion.Euler(0f, 0f, 40f);
            MenuButtonFeedback feedback = rt.gameObject.AddComponent<MenuButtonFeedback>();
            SetRefs(feedback, ("highlight", glow), ("arrow", arrow));
            return button;
        }

        private static void BuildDialog(Transform canvas, MainMenuController controller)
        {
            Image dim = Image("Dialog overlay", canvas, new Color(0.005f, 0.012f, 0.014f, 0.88f));
            Stretch(dim);
            dim.raycastTarget = true;
            RectTransform card = Rect("Dialog card", dim.transform, 900, 780);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.gameObject.AddComponent<Image>().color = Hex("111C1D");
            Line("Gold top border", card, 0, 0, 900, 2, Gold);
            Line("Bottom border", card, 0, 779, 900, 1, Alpha(Gold, 0.3f));
            Text eyebrow = Label("Dialog eyebrow", card, "EXPLORER'S GUIDE", 13, Gold, 700, 30, 65, 48);
            Text title = Label("Dialog title", card, "어둠 속에서 살아남기", 39, Ivory, 760, 82, 60, 95, TextAnchor.MiddleLeft, titleFont);
            Line("Dialog divider", card, 65, 190, 770, 1, Alpha(Gold, 0.22f));
            Text body = Label("Dialog body", card, "", 21, Hex("B8C0B8"), 770, 390, 65, 219, TextAnchor.UpperLeft);
            body.lineSpacing = 1.35f;
            Button close = SmallButton(card, "Close dialog", "×", 790, 42, 56, 50);
            Button dismiss = SmallButton(card, "Dismiss button", "돌아가기", 65, 662, 360, 64);
            Button confirm = SmallButton(card, "Confirm quit button", "종료하기", 475, 662, 360, 64);
            // Automatic navigation skips the hidden confirmation button in help/info dialogs.
            SetRefs(controller, ("dialog", dim.gameObject), ("dialogEyebrow", eyebrow), ("dialogTitle", title),
                ("dialogBody", body), ("closeButton", close), ("dismissButton", dismiss), ("confirmQuitButton", confirm));
            dim.gameObject.SetActive(false);
        }

        private static Button SmallButton(Transform parent, string name, string title, float x, float y, float width, float height)
        {
            RectTransform rt = Rect(name, parent, width, height, x, y);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.65f, 0.56f, 0.4f, 0.17f);
            Button button = rt.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.5f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.65f);
            button.colors = colors;
            button.targetGraphic = image;
            Line("Button border", rt, 0, 0, width, 1, Alpha(Gold, 0.5f));
            Label("Label", rt, title, title == "×" ? 30 : 21, Ivory, width, height, 0, 0, TextAnchor.MiddleCenter);
            return button;
        }

        private static void LinkNavigation(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = nav.selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length];
                nav.selectOnDown = nav.selectOnRight = buttons[(i + 1) % buttons.Length];
                buttons[i].navigation = nav;
            }
        }

        private static Camera CreateCorridor(int rendererIndex, out Light warmLamp)
        {
            var set = new GameObject("3D Menu Backdrop (cosmetic only)");
            var cameraGO = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.transform.SetParent(set.transform);
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0f, 1.75f, -8f);
            cameraGO.transform.rotation = Quaternion.Euler(1.2f, -16f, 0f);
            Camera camera = cameraGO.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("0C1718");
            camera.fieldOfView = 58;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80;
            var cameraData = cameraGO.AddComponent<UniversalAdditionalCameraData>();
            cameraData.SetRenderer(rendererIndex);
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.21f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = Hex("132123");
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.032f;

            var volumeGO = new GameObject("Menu atmosphere volume", typeof(Volume));
            volumeGO.transform.SetParent(set.transform);
            Volume volume = volumeGO.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var grading = profile.Add<ColorAdjustments>(true);
            grading.postExposure.Override(0.35f);
            grading.contrast.Override(15f);
            grading.saturation.Override(-23f);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.4f);
            bloom.scatter.Override(0.65f);
            AssetDatabase.CreateAsset(profile, Root + "Art/MenuAtmosphere.asset");
            volume.sharedProfile = profile;

            Texture2D stone = StoneTexture();
            Material[] stones = new Material[5];
            for (int i = 0; i < stones.Length; i++)
            {
                stones[i] = Material("Stone " + i, new Color(0.24f + i * 0.018f, 0.27f + i * 0.017f, 0.25f + i * 0.017f), 0.12f);
                stones[i].SetTexture("_BaseMap", stone);
                stones[i].SetTextureScale("_BaseMap", new Vector2(1.1f, 0.6f));
            }
            Material dark = Material("Iron and mortar", Hex("171F1D"), 0.3f);
            Material floor = Material("Wet flagstone", Hex("35413B"), 0.42f);
            floor.SetTexture("_BaseMap", stone);
            floor.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            Material trim = Material("Worn arch stone", Hex("404840"), 0.14f);
            trim.SetTexture("_BaseMap", stone);
            Material glow = Material("Lantern glass", Hex("8D7750"), 0.1f, new Color(2.7f, 1.55f, 0.55f));
            Material exitGlow = Material("Beyond the door", Hex("B5C5AC"), 0f, new Color(1.5f, 2.3f, 1.75f));

            var masonry = new GameObject("Stone corridor");
            masonry.transform.SetParent(set.transform);
            var rng = new System.Random(27);
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Mortar backing", masonry.transform, new Vector3(side * 3.1f, 2.2f, 8f), new Vector3(0.55f, 4.8f, 35f), dark);
                for (int row = 0; row < 9; row++)
                for (int column = 0; column < 24; column++)
                {
                    float z = -9.5f + column * 1.48f + (row % 2 == 0 ? 0f : 0.74f);
                    Cube("Hand-cut stone", masonry.transform, new Vector3(side * (2.92f + (float)rng.NextDouble() * 0.07f), row * 0.5f + 0.25f, z),
                        new Vector3(0.38f, 0.472f, 1.43f), stones[rng.Next(5)]);
                }
            }
            for (int z = -10; z < 26; z += 2)
            for (int x = 0; x < 4; x++)
                Cube("Floor flagstone", masonry.transform, new Vector3(-2.23f + x * 1.49f, -0.1f, z), new Vector3(1.47f, 0.18f, 1.98f), floor);
            Cube("Ceiling darkness", masonry.transform, new Vector3(0, 4.6f, 8), new Vector3(6.6f, 0.3f, 36), dark);

            for (int z = -3; z <= 21; z += 6)
            {
                var arch = new GameObject("Archway " + z);
                arch.transform.SetParent(set.transform);
                for (int side = -1; side <= 1; side += 2)
                {
                    Cube("Arch pillar", arch.transform, new Vector3(side * 2.64f, 1.52f, z), new Vector3(0.42f, 3.04f, 0.7f), trim);
                    Cube("Pillar base", arch.transform, new Vector3(side * 2.64f, 0.14f, z), new Vector3(0.62f, 0.28f, 0.95f), trim);
                    Cube("Pillar capital", arch.transform, new Vector3(side * 2.64f, 3f, z), new Vector3(0.64f, 0.2f, 0.9f), trim);
                }
                const int segments = 13;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (i + 0.5f) / segments * Mathf.PI;
                    GameObject block = Cube("Arch voussoir", arch.transform,
                        new Vector3(Mathf.Cos(angle) * 2.64f, 3.03f + Mathf.Sin(angle) * 1.18f, z), new Vector3(0.68f, 0.36f, 0.72f), trim);
                    block.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(1.18f * Mathf.Cos(angle), -2.64f * Mathf.Sin(angle)) * Mathf.Rad2Deg);
                }
            }

            // A distant, slightly open iron gate draws the eye into the maze.
            Cube("End wall left", set.transform, new Vector3(-2f, 2.1f, 24), new Vector3(2, 4.2f, 0.5f), trim);
            Cube("End wall right", set.transform, new Vector3(2f, 2.1f, 24), new Vector3(2, 4.2f, 0.5f), trim);
            Cube("Door lintel", set.transform, new Vector3(0, 3.9f, 24), new Vector3(2.3f, 0.7f, 0.6f), trim);
            Cube("Light beyond door", set.transform, new Vector3(0, 1.5f, 25.5f), new Vector3(2, 3, 0.08f), exitGlow);
            for (int i = 0; i < 9; i++)
                Cube("Iron gate bar", set.transform, new Vector3(-0.9f + i * 0.22f, 1.65f, 24), new Vector3(0.065f, 3.3f, 0.08f), dark);
            Cube("Gate crossbar", set.transform, new Vector3(0, 1.45f, 24), new Vector3(2.1f, 0.09f, 0.12f), dark);
            AddLight("Pale light beyond", set.transform, new Vector3(0f, 2.3f, 22.5f), new Color(0.57f, 0.79f, 0.69f), 4f, 13f);
            AddLight("Cold corridor fill", set.transform, new Vector3(-1.5f, 2.8f, 5f), new Color(0.4f, 0.62f, 0.64f), 2f, 15f);

            warmLamp = null;
            for (int i = 0; i < 3; i++)
            {
                float z = 0.8f + i * 9f;
                Cube("Lantern bracket", set.transform, new Vector3(2.65f, 2.75f, z), new Vector3(0.58f, 0.08f, 0.08f), dark);
                Cube("Lantern glass", set.transform, new Vector3(2.36f, 2.47f, z), new Vector3(0.2f, 0.37f, 0.2f), glow);
                Cube("Lantern cap", set.transform, new Vector3(2.36f, 2.69f, z), new Vector3(0.3f, 0.07f, 0.3f), dark);
                Cube("Lantern foot", set.transform, new Vector3(2.36f, 2.25f, z), new Vector3(0.3f, 0.07f, 0.3f), dark);
                for (int a = -1; a <= 1; a += 2)
                for (int b = -1; b <= 1; b += 2)
                    Cube("Lantern cage", set.transform, new Vector3(2.36f + a * 0.112f, 2.47f, z + b * 0.112f), new Vector3(0.025f, 0.42f, 0.025f), dark);
                Light light = AddLight("Warm lantern light", set.transform, new Vector3(2.08f, 2.5f, z), new Color(1f, 0.65f, 0.3f), 4.2f, 10f);
                if (i == 0) warmLamp = light;
            }
            MergeMasonry(masonry);
            return camera;
        }

        private static int FindExisting3DRenderer()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
                throw new InvalidOperationException("This menu requires the project's Universal Render Pipeline.");
            var settings = new SerializedObject(pipeline);
            SerializedProperty renderers = settings.FindProperty("m_RendererDataList");
            var approvedRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "Art/Menu3DRenderer.asset");
            for (int i = 0; i < renderers.arraySize; i++)
                if (approvedRenderer != null && renderers.GetArrayElementAtIndex(i).objectReferenceValue == approvedRenderer)
                    return i;

            // Read-only precondition: shared pipeline assets require a separate human approval.
            throw new InvalidOperationException("The approved menu renderer is not registered. Ask the project owner; this tool will not modify the shared pipeline.");
        }

        private static void MergeMasonry(GameObject masonry)
        {
            // Keep the arches editable; merge hundreds of wall blocks into one mesh per material.
            MeshFilter[] filters = masonry.GetComponentsInChildren<MeshFilter>();
            foreach (var group in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                CombineInstance[] combines = group.Select(f => new CombineInstance
                {
                    mesh = f.sharedMesh, transform = f.transform.localToWorldMatrix
                }).ToArray();
                var mesh = new Mesh { name = group.Key.name + " masonry" };
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(combines);
                AssetDatabase.CreateAsset(mesh, Root + "Art/" + mesh.name + ".asset");
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(masonry.transform);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
                go.isStatic = true;
            }
            foreach (MeshFilter filter in filters)
                Object.DestroyImmediate(filter.gameObject);
        }

        private static Texture2D StoneTexture()
        {
            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.048f, y * 0.048f) * 0.25f +
                    Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.13f + Mathf.PerlinNoise(x * 1.5f, y * 1.5f) * 0.14f;
                float vein = Mathf.Pow(Mathf.Abs(Mathf.Sin((x * 0.017f + Mathf.PerlinNoise(x * 0.015f, y * 0.02f) * 3f) * 10f)), 14f) * 0.06f;
                texture.SetPixel(x, y, new Color(0.42f + noise - vein, 0.45f + noise - vein, 0.42f + noise - vein));
            }
            texture.Apply();
            string path = Root + "Art/StoneGrain.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Material(string name, Color color, float smoothness, Color? emission = null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            AssetDatabase.CreateAsset(material, Root + "Materials/" + name + ".mat");
            return material;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        private static Light AddLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent);
            go.transform.position = position;
            Light light = go.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.shadowBias = 0.025f;
            return light;
        }

        private static RectTransform Rect(string name, Transform parent, float width, float height, float x = 0, float y = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -y);
            return rt;
        }

        private static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var image = Rect(name, parent, 100, 100).gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(Image image)
        {
            RectTransform rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Text Label(string name, Transform parent, string text, int size, Color color,
            float width, float height, float x, float y, TextAnchor alignment = TextAnchor.MiddleLeft, Font font = null)
        {
            Text label = Rect(name, parent, width, height, x, y).gameObject.AddComponent<Text>();
            label.font = font != null ? font : bodyFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.supportRichText = false;
            return label;
        }

        private static RectTransform Line(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = Image(name, parent, color);
            image.rectTransform.sizeDelta = new Vector2(width, height);
            image.rectTransform.anchoredPosition = new Vector2(x, -y);
            return image.rectTransform;
        }

        private static void DrawMazeMark(Transform parent, float x, float y, float size, Color color)
        {
            // An intentionally incomplete labyrinth: an entrance, a turn, no obvious exit.
            float u = size / 10f;
            float stroke = Mathf.Max(1f, size / 140f);
            float[,] segments = {
                { 0, 0, 10, 0 }, { 0, 0, 0, 10 }, { 0, 10, 7, 10 }, { 10, 0, 10, 10 },
                { 2, 2, 8, 2 }, { 2, 2, 2, 8 }, { 2, 8, 8, 8 }, { 8, 2, 8, 6 },
                { 4, 4, 8, 4 }, { 4, 4, 4, 6 }, { 4, 6, 6, 6 }, { 6, 6, 6, 8 }
            };
            for (int i = 0; i < segments.GetLength(0); i++)
                Line("Maze line", parent, x + segments[i, 0] * u, y + segments[i, 1] * u,
                    Mathf.Max(stroke, (segments[i, 2] - segments[i, 0]) * u),
                    Mathf.Max(stroke, (segments[i, 3] - segments[i, 1]) * u), color);
        }

        private static Sprite TextureSprite(string name, int width, int height, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            string path = Root + "Art/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetRefs(Object target, params (string name, Object value)[] references)
        {
            var so = new SerializedObject(target);
            foreach (var reference in references)
                so.FindProperty(reference.name).objectReferenceValue = reference.value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }
    }
}
