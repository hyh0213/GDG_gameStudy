using System;
using System.IO;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DarkMaze.Editor
{
    /// <summary>
    /// Creates HYH's personal GameplayHUD.prefab and GameplayUITest.unity without touching shared settings or StartMenu.
    /// </summary>
    public static class GameplayHudBuilder
    {
        public const string PrefabPath = "Assets/UI/하윤형/Prefabs/GameplayHUD.prefab";
        public const string TestScenePath = "Assets/Scenes/하윤형/GameplayUITest.unity";

        private static readonly Color Ivory = Hex("E1DFD2");
        private static readonly Color Gold = Hex("B8A078");
        private static readonly Color Muted = Hex("8C9692");
        private static readonly Color CardBg = new Color32(17, 28, 29, 232);
        private static readonly Color SolidCardBg = new Color32(17, 28, 29, 255);
        private static readonly Color TerrainSwatch = new Color32(118, 138, 132, 255);
        private static readonly Color PuzzleSwatch = new Color32(184, 160, 120, 255);
        private static readonly Color HazardSwatch = new Color32(194, 74, 74, 255);

        private static Font bodyFont;
        private static Font titleFont;

        [MenuItem("Tools/하윤형/UI/Create Gameplay HUD")]
        public static void CreateGameplayHud()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before creating the gameplay HUD.");
            if (File.Exists(PrefabPath))
                throw new InvalidOperationException("GameplayHUD.prefab already exists and will not be overwritten.");
            if (File.Exists(TestScenePath))
                throw new InvalidOperationException("GameplayUITest.unity already exists and will not be overwritten.");

            bool untitled = Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path));
            if (untitled)
                throw new InvalidOperationException("Save your untitled scene before creating the personal HUD test scene.");
            bool anyDirty = Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => SceneManager.GetSceneAt(i).isDirty);
            if (anyDirty)
                throw new InvalidOperationException("Save your modified scene before creating the personal HUD test scene.");

            LoadRequiredAssets(out Sprite vignette, out Texture2D arrowCursor, out Texture2D selectCursor);

            if (!Directory.Exists("Assets/UI/하윤형/Prefabs"))
                AssetDatabase.CreateFolder("Assets/UI/하윤형", "Prefabs");
            if (!Directory.Exists("Assets/Scenes/하윤형"))
                AssetDatabase.CreateFolder("Assets/Scenes", "하윤형");

            GameObject prefabRoot = BuildHudPrefabHierarchy(vignette, arrowCursor, selectCursor);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(prefabRoot);
            }

            CreateTestScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Dark Maze Gameplay HUD created: " + PrefabPath + " and " + TestScenePath);
        }

        private static void LoadRequiredAssets(out Sprite vignette, out Texture2D arrowCursor, out Texture2D selectCursor)
        {
            bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/하윤형/Fonts/GothicA1-Regular.ttf");
            titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/하윤형/Fonts/GowunBatang-Regular.ttf");
            vignette = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/하윤형/Art/Vignette.png");
            arrowCursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/하윤형/Art/Cursors/MenuArrow.png");
            selectCursor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/하윤형/Art/Cursors/MenuSelect.png");

            if (bodyFont == null || titleFont == null)
                throw new InvalidOperationException("Personal OFL fonts are missing under Assets/UI/하윤형/Fonts.");
            if (vignette == null)
                throw new InvalidOperationException("Assets/UI/하윤형/Art/Vignette.png is missing.");
            if (arrowCursor == null || selectCursor == null)
                throw new InvalidOperationException("Cursor textures are missing under Assets/UI/하윤형/Art/Cursors.");
        }

        private static GameObject BuildHudPrefabHierarchy(Sprite vignette, Texture2D arrowCursor, Texture2D selectCursor)
        {
            var root = new GameObject("GameplayHUD");
            var canvasGo = new GameObject("Gameplay HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform full = canvasGo.GetComponent<RectTransform>();

            var controllers = new GameObject("HUD Controllers");
            controllers.transform.SetParent(root.transform, false);
            var hudController = controllers.AddComponent<GameplayHudController>();
            var scannerUi = controllers.AddComponent<DarknessScannerUI>();
            var resultController = controllers.AddComponent<GameResultController>();
            var settingsController = controllers.AddComponent<MenuSettingsController>();
            var transitionManager = controllers.AddComponent<SceneTransitionManager>();
            var cursorController = controllers.AddComponent<MenuCursorController>();

            // 1. Screen Darkness / Damage / Scanner Pulse overlays (UI-04 / UI-02, behind HUD panels)
            Image darknessImg = Image("Darkness overlay", full, new Color(0.006f, 0.01f, 0.012f, 1f), vignette, false);
            Stretch(darknessImg.rectTransform);
            CanvasGroup darknessGroup = darknessImg.gameObject.AddComponent<CanvasGroup>();
            darknessGroup.alpha = 0f;
            darknessGroup.blocksRaycasts = false;

            Image damageImg = Image("Damage flash overlay", full, new Color(0.7f, 0.2f, 0.2f, 0f), vignette, false);
            Stretch(damageImg.rectTransform);

            Image pulseImg = Image("Scanner pulse overlay", full, new Color(0.43f, 0.66f, 0.6f, 0f), vignette, false);
            Stretch(pulseImg.rectTransform);

            // 2. Active Gameplay HUD layer
            RectTransform hudLayer = Rect("HUD Layer", full, 0, 0);
            Stretch(hudLayer);
            CanvasGroup hudGroup = hudLayer.gameObject.AddComponent<CanvasGroup>();
            hudGroup.blocksRaycasts = false;

            BuildHealthPanel(hudLayer, hudController);
            BuildObjectivePanel(hudLayer, hudController);
            BuildInteractionPrompt(hudLayer, hudController);
            BuildScannerAndExtraSlots(hudLayer, scannerUi, darknessGroup, damageImg, pulseImg);

            // 3. Pause Menu Overlay (UI-02)
            BuildPauseOverlay(full, hudController);

            // 4. Settings Overlay (reuses MenuSettingsBuilder cleanly)
            MenuSettingsBuilder.BuildPanelIntoCanvas(full, settingsController);

            // 5. Game Over / Clear Overlay (UI-05)
            BuildResultOverlay(full, resultController);

            // 6. Screen Transition Fade Overlay (UI-03, topmost)
            Image fadeImg = Image("Screen fade", full, new Color(0.012f, 0.02f, 0.021f, 1f), null, true);
            Stretch(fadeImg.rectTransform);
            CanvasGroup fadeGroup = fadeImg.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            Text loadingLabel = Label("Loading label", fadeImg.transform, "이동하는 중…", 24, Ivory, 0, 0, 900, 70, TextAnchor.MiddleCenter);
            Center(loadingLabel.rectTransform);
            loadingLabel.gameObject.SetActive(false);

            SetRefs(transitionManager, ("fadeGroup", fadeGroup), ("statusLabel", loadingLabel));
            SetRefs(hudController,
                ("transitionManager", transitionManager),
                ("resultController", resultController),
                ("settingsController", settingsController),
                ("darknessScannerUI", scannerUi),
                ("hudGroup", hudGroup));
            SetRefs(resultController, ("transitionManager", transitionManager));

            using (var so = new SerializedObject(cursorController))
            {
                so.FindProperty("arrowTexture").objectReferenceValue = arrowCursor;
                so.FindProperty("selectTexture").objectReferenceValue = selectCursor;
                so.FindProperty("arrowHotspot").vector2Value = new Vector2(4f, 2f);
                so.FindProperty("selectHotspot").vector2Value = new Vector2(12f, 3f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return root;
        }

        private static void BuildHealthPanel(Transform parent, GameplayHudController hud)
        {
            RectTransform panel = AnchorPanel("Health Panel", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -42), 360, 88);
            panel.gameObject.AddComponent<Image>().color = CardBg;
            panel.GetComponent<Image>().raycastTarget = false;
            Image accent = Line("Health accent rule", panel, 0, 0, 360, 2, Gold);

            Label("Health eyebrow", panel, "V I T A L I T Y", 10, Gold, 20, 10, 160, 18);
            Label("Health title", panel, "체력", 18, Ivory, 20, 28, 120, 24);
            Text value = Label("Health value", panel, "100 / 100", 17, Ivory, 170, 28, 170, 24, TextAnchor.MiddleRight);

            RectTransform track = Rect("Health bar track", panel, 320, 10, 20, 62);
            track.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.11f, 0.95f);
            track.GetComponent<Image>().raycastTarget = false;

            Image trail = FillImage("Health trail bar", track, new Color(Ivory.r, Ivory.g, Ivory.b, 0.45f));
            Image fill = FillImage("Health fill bar", track, Gold);

            SetRefs(hud, ("healthFillBar", fill), ("healthTrailBar", trail), ("healthValueLabel", value), ("healthAccentRule", accent));
        }

        private static void BuildObjectivePanel(Transform parent, GameplayHudController hud)
        {
            RectTransform panel = AnchorPanel("Objective Panel", parent, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-48, -42), 450, 76);
            panel.gameObject.AddComponent<Image>().color = CardBg;
            panel.GetComponent<Image>().raycastTarget = false;
            Line("Objective rule", panel, 0, 0, 450, 1, Alpha(Gold, 0.65f));

            Label("Objective eyebrow", panel, "O B J E C T I V E", 10, Gold, 20, 10, 410, 18);
            Text objective = Label("Objective label", panel, "탈출 단서 0 / 3 — 미로의 장치를 해제하세요", 16, Ivory, 20, 32, 410, 34);

            SetRefs(hud, ("objectiveRoot", panel.gameObject), ("objectiveLabel", objective));
        }

        private static void BuildInteractionPrompt(Transform parent, GameplayHudController hud)
        {
            RectTransform prompt = AnchorPanel("Interaction Prompt", parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 155), 390, 52);
            prompt.gameObject.AddComponent<Image>().color = new Color32(17, 28, 29, 240);
            prompt.GetComponent<Image>().raycastTarget = false;
            CanvasGroup group = prompt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Line("Prompt top rule", prompt, 0, 0, 390, 1, Alpha(Gold, 0.7f));
            Line("Prompt bottom rule", prompt, 0, 51, 390, 1, Alpha(Gold, 0.25f));

            RectTransform keyBox = Rect("Prompt key box", prompt, 34, 34, 14, 9);
            keyBox.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.2f);
            keyBox.GetComponent<Image>().raycastTarget = false;
            Line("Key box rule", keyBox, 0, 0, 34, 1, Gold);
            Text keyLabel = Label("Prompt key label", keyBox, "E", 17, Gold, 0, 0, 34, 34, TextAnchor.MiddleCenter);
            Text actionLabel = Label("Prompt action label", prompt, "장치 작동하기", 18, Ivory, 62, 0, 312, 52);

            SetRefs(hud, ("promptGroup", group), ("promptKeyLabel", keyLabel), ("promptActionLabel", actionLabel));
        }

        private static void BuildScannerAndExtraSlots(Transform parent, DarknessScannerUI scanner,
            CanvasGroup darknessGroup, Image damageImg, Image pulseImg)
        {
            // Bottom-Right: Primary Exploration Gear (UI-04) + optional Outline Legend
            RectTransform scannerGroup = AnchorPanel("Scanner Panel", parent, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48, 42), 360, 186);

            RectTransform legend = AnchorPanel("Outline Legend", scannerGroup, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 118), 360, 56);
            legend.gameObject.AddComponent<Image>().color = new Color32(17, 28, 29, 210);
            legend.GetComponent<Image>().raycastTarget = false;
            Line("Legend rule", legend, 0, 0, 360, 1, Alpha(Gold, 0.35f));
            Label("Legend title", legend, "O U T L I N E", 10, Gold, 16, 6, 120, 16);
            Image terrainSwatch = LegendSwatch(legend, "Terrain swatch", "지형", TerrainSwatch, 16, 26);
            Image puzzleSwatch = LegendSwatch(legend, "Puzzle swatch", "장치", PuzzleSwatch, 128, 26);
            Image hazardSwatch = LegendSwatch(legend, "Hazard swatch", "위험", HazardSwatch, 240, 26);
            legend.gameObject.SetActive(false);

            RectTransform card = AnchorPanel("Scanner Card", scannerGroup, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, 360, 104);
            card.gameObject.AddComponent<Image>().color = CardBg;
            card.GetComponent<Image>().raycastTarget = false;
            Image scannerAccent = Line("Scanner accent rule", card, 0, 0, 360, 2, Gold);

            RectTransform keyBox = Rect("Scanner key box", card, 38, 38, 18, 18);
            keyBox.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.18f);
            keyBox.GetComponent<Image>().raycastTarget = false;
            Line("Scanner key rule", keyBox, 0, 0, 38, 1, Gold);
            Text scannerKey = Label("Scanner key label", keyBox, "F", 18, Gold, 0, 0, 38, 38, TextAnchor.MiddleCenter);

            Label("Scanner eyebrow", card, "E X P L O R A T I O N   G E A R", 10, Alpha(Gold, 0.85f), 70, 14, 270, 16);
            Text scannerTitle = Label("Scanner title", card, "탐사 장비", 19, Ivory, 70, 32, 130, 28);
            Text scannerStatus = Label("Scanner status", card, "준비됨", 15, Gold, 195, 34, 147, 24, TextAnchor.MiddleRight);

            RectTransform track = Rect("Scanner bar track", card, 324, 8, 18, 78);
            track.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.11f, 0.95f);
            track.GetComponent<Image>().raycastTarget = false;
            Image scannerFill = FillImage("Scanner fill bar", track, Gold);

            // Bottom-Left: UI-06 Optional Heal Item & Secondary Gear Slots (hidden until activated by gameplay)
            RectTransform extraGroup = AnchorPanel("Extra Slots Panel", parent, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 42), 340, 166);

            RectTransform healCard = AnchorPanel("Heal Item Slot", extraGroup, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, 300, 68);
            healCard.gameObject.AddComponent<Image>().color = CardBg;
            healCard.GetComponent<Image>().raycastTarget = false;
            Image healAccent = Line("Heal accent rule", healCard, 0, 0, 300, 2, Gold);
            RectTransform healKeyBox = Rect("Heal key box", healCard, 34, 34, 16, 17);
            healKeyBox.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.18f);
            healKeyBox.GetComponent<Image>().raycastTarget = false;
            Line("Heal key rule", healKeyBox, 0, 0, 34, 1, Gold);
            Text healKey = Label("Heal key label", healKeyBox, "Q", 16, Gold, 0, 0, 34, 34, TextAnchor.MiddleCenter);
            Text healName = Label("Heal name", healCard, "회복 아이템", 17, Ivory, 64, 16, 160, 36);
            Text healCount = Label("Heal count", healCard, "×2", 20, Gold, 224, 16, 60, 36, TextAnchor.MiddleRight);
            healCard.gameObject.SetActive(false);

            RectTransform subCard = AnchorPanel("Sub Gear Slot", extraGroup, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 80), 300, 78);
            subCard.gameObject.AddComponent<Image>().color = CardBg;
            subCard.GetComponent<Image>().raycastTarget = false;
            Image subAccent = Line("Sub gear accent rule", subCard, 0, 0, 300, 2, Gold);
            RectTransform subKeyBox = Rect("Sub gear key box", subCard, 34, 34, 16, 14);
            subKeyBox.gameObject.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.18f);
            subKeyBox.GetComponent<Image>().raycastTarget = false;
            Line("Sub key rule", subKeyBox, 0, 0, 34, 1, Gold);
            Text subKey = Label("Sub gear key label", subKeyBox, "G", 16, Gold, 0, 0, 34, 34, TextAnchor.MiddleCenter);
            Text subName = Label("Sub gear name", subCard, "추가 탐사 장비", 16, Ivory, 64, 12, 130, 30);
            Text subStatus = Label("Sub gear status", subCard, "준비됨", 14, Gold, 188, 12, 96, 30, TextAnchor.MiddleRight);
            RectTransform subTrack = Rect("Sub gear bar track", subCard, 268, 6, 16, 58);
            subTrack.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.11f, 0.95f);
            subTrack.GetComponent<Image>().raycastTarget = false;
            Image subFill = FillImage("Sub gear fill bar", subTrack, Gold);
            subCard.gameObject.SetActive(false);

            SetRefs(scanner,
                ("darknessOverlay", darknessGroup),
                ("damageFlashOverlay", damageImg),
                ("scannerPulseOverlay", pulseImg),
                ("scannerRoot", card.gameObject),
                ("scannerKeyLabel", scannerKey),
                ("scannerTitleLabel", scannerTitle),
                ("scannerStatusLabel", scannerStatus),
                ("scannerFillBar", scannerFill),
                ("scannerAccentRule", scannerAccent),
                ("legendRoot", legend.gameObject),
                ("legendTerrainSwatch", terrainSwatch),
                ("legendPuzzleSwatch", puzzleSwatch),
                ("legendHazardSwatch", hazardSwatch),
                ("healSlotRoot", healCard.gameObject),
                ("healKeyLabel", healKey),
                ("healNameLabel", healName),
                ("healCountLabel", healCount),
                ("healAccentRule", healAccent),
                ("subGearRoot", subCard.gameObject),
                ("subGearKeyLabel", subKey),
                ("subGearNameLabel", subName),
                ("subGearStatusLabel", subStatus),
                ("subGearFillBar", subFill),
                ("subGearAccentRule", subAccent));
        }

        private static void BuildPauseOverlay(Transform canvas, GameplayHudController hud)
        {
            Image overlay = Image("Pause overlay", canvas, new Color(0.005f, 0.012f, 0.014f, 0.88f), null, true);
            Stretch(overlay.rectTransform);

            RectTransform card = Rect("Pause card", overlay.transform, 540, 620);
            Center(card);
            card.gameObject.AddComponent<Image>().color = SolidCardBg;
            CanvasGroup contentGroup = card.gameObject.AddComponent<CanvasGroup>();
            Line("Pause top border", card, 0, 0, 540, 2, Gold);
            Line("Pause bottom border", card, 0, 619, 540, 1, Alpha(Gold, 0.25f));

            Label("Pause eyebrow", card, "P A U S E D", 13, Gold, 50, 38, 440, 24);
            Label("Pause title", card, "일시 정지", 42, Ivory, 48, 68, 440, 64, TextAnchor.MiddleLeft, titleFont);
            Line("Pause divider", card, 50, 146, 440, 1, Alpha(Gold, 0.22f));

            Button resume = MenuStyleButton(card, "Pause resume", "계속하기", "RESUME", 50, 174, 440, true);
            Button settings = MenuStyleButton(card, "Pause settings", "환경 설정", "SETTINGS", 50, 260, 440, false);
            Button restart = MenuStyleButton(card, "Pause restart", "다시 시작", "RESTART", 50, 346, 440, false);
            Button mainMenu = MenuStyleButton(card, "Pause main menu", "메인 화면으로", "MAIN MENU", 50, 432, 440, false);
            LinkVerticalNavigation(resume, settings, restart, mainMenu);

            Text status = Label("Pause status", card, "", 15, Gold, 50, 524, 440, 64, TextAnchor.MiddleLeft);

            SetRefs(hud,
                ("pauseOverlay", overlay.gameObject),
                ("pauseContentGroup", contentGroup),
                ("resumeButton", resume),
                ("settingsButton", settings),
                ("restartButton", restart),
                ("mainMenuButton", mainMenu),
                ("pauseStatusLabel", status));

            overlay.gameObject.SetActive(false);
        }

        private static void BuildResultOverlay(Transform canvas, GameResultController result)
        {
            Image overlay = Image("Result overlay", canvas, new Color(0.005f, 0.012f, 0.014f, 0.92f), null, true);
            Stretch(overlay.rectTransform);
            CanvasGroup overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = Rect("Result card", overlay.transform, 820, 530);
            Center(card);
            card.gameObject.AddComponent<Image>().color = SolidCardBg;
            Image topRule = Line("Result top border", card, 0, 0, 820, 2, Gold);
            Line("Result bottom border", card, 0, 529, 820, 1, Alpha(Gold, 0.25f));

            Text eyebrow = Label("Result eyebrow", card, "G A M E   O V E R", 13, Gold, 58, 42, 704, 24);
            Text title = Label("Result title", card, "어둠에 삼켜졌습니다", 46, Ivory, 56, 76, 704, 72, TextAnchor.MiddleLeft, titleFont);
            Line("Result divider", card, 58, 162, 704, 1, Alpha(Gold, 0.22f));
            Text description = Label("Result description", card, "미로의 위협으로부터 살아남지 못했습니다.", 21, Hex("B8C0B8"), 58, 184, 704, 76, TextAnchor.UpperLeft);
            description.lineSpacing = 1.35f;
            Text stats = Label("Result stats", card, "", 18, Gold, 58, 274, 704, 40);

            Button retry = MenuStyleButton(card, "Result retry", "다시 시도", "RETRY", 58, 344, 336, true);
            Button mainMenu = MenuStyleButton(card, "Result main menu", "메인 화면으로", "MAIN MENU", 426, 344, 336, false);
            LinkHorizontalNavigation(retry, mainMenu);

            Text notice = Label("Result notice", card, "", 15, Gold, 58, 434, 704, 64);

            SetRefs(result,
                ("overlay", overlay.gameObject),
                ("overlayGroup", overlayGroup),
                ("topAccentRule", topRule),
                ("eyebrowLabel", eyebrow),
                ("titleLabel", title),
                ("descriptionLabel", description),
                ("statsLabel", stats),
                ("statusNoticeLabel", notice),
                ("retryButton", retry),
                ("mainMenuButton", mainMenu));

            overlay.gameObject.SetActive(false);
        }

        private static void CreateTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var cameraGo = new GameObject("Test UI Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            Camera cam = cameraGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("091213");

            var events = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);

            var harnessGo = new GameObject("HUD Demo Harness");
            var harness = harnessGo.AddComponent<GameplayHudDemoHarness>();
            SetRefs(harness,
                ("hud", hudInstance.GetComponentInChildren<GameplayHudController>(true)),
                ("scanner", hudInstance.GetComponentInChildren<DarknessScannerUI>(true)),
                ("result", hudInstance.GetComponentInChildren<GameResultController>(true)));

            // Test-scene-only shortcut legend at top center so evaluators know the demo keys.
            var hintCanvasGo = new GameObject("Test Legend Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hintCanvasGo.transform.SetParent(harnessGo.transform, false);
            Canvas hintCanvas = hintCanvasGo.GetComponent<Canvas>();
            hintCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hintCanvas.sortingOrder = 105;
            CanvasScaler scaler = hintCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform legend = AnchorPanel("Test Shortcut Legend", hintCanvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), 880, 32);
            legend.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.05f, 0.72f);
            legend.GetComponent<Image>().raycastTarget = false;
            Label("Legend text", legend,
                "테스트: [1]피해  [2]회복  [3]상호작용  [4]탐사장비  [5]암전  [6]회복·추가장비(UI-06)  [7]게임오버  [8]클리어  [9]단서  [Esc]일시정지",
                13, Alpha(Ivory, 0.75f), 0, 0, 880, 32, TextAnchor.MiddleCenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TestScenePath);
        }

        private static Button MenuStyleButton(Transform parent, string name, string title, string subtitle,
            float x, float y, float width, bool primary)
        {
            const float height = 70f;
            RectTransform rt = Rect(name, parent, width, height, x, y);
            Image background = rt.gameObject.AddComponent<Image>();
            background.color = primary ? new Color(0.48f, 0.42f, 0.3f, 0.18f) : new Color(0f, 0f, 0f, 0.04f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            Image hover = Image("Focus glow", rt, new Color(0.56f, 0.48f, 0.34f, 0.18f), null, false);
            Stretch(hover.rectTransform);
            CanvasGroup glow = hover.gameObject.AddComponent<CanvasGroup>();
            glow.alpha = 0f;
            glow.blocksRaycasts = false;
            Line("Focus edge", hover.transform, 0, 0, 3, height, Gold);
            Line("Top edge", rt, 0, 0, width, 1, Alpha(Gold, primary ? 0.7f : 0.2f));
            Line("Bottom edge", rt, 0, height - 1, width, 1, Alpha(Gold, primary ? 0.7f : 0.2f));
            Label("Label", rt, title, 23, primary ? Ivory : Hex("BDC3BC"), 24, 5, width - 90, 38);
            Label("English caption", rt, subtitle, 10, primary ? Gold : Alpha(Ivory, 0.45f), 26, 43, width - 90, 20);

            RectTransform arrow = Rect("Arrow", rt, 24, 24, width - 48, 22);
            Line("Arrow stem", arrow, 0, 11, 20, 1, Gold);
            RectTransform upper = Line("Arrow upper", arrow, 14, 8, 8, 1, Gold).rectTransform;
            upper.localRotation = Quaternion.Euler(0f, 0f, -40f);
            RectTransform lower = Line("Arrow lower", arrow, 14, 14, 8, 1, Gold).rectTransform;
            lower.localRotation = Quaternion.Euler(0f, 0f, 40f);

            MenuButtonFeedback feedback = rt.gameObject.AddComponent<MenuButtonFeedback>();
            SetRefs(feedback, ("highlight", glow), ("arrow", arrow));
            return button;
        }

        private static Image LegendSwatch(Transform parent, string name, string label, Color color, float x, float y)
        {
            Image swatch = Image(name, parent, color, null, false);
            swatch.rectTransform.sizeDelta = new Vector2(12, 12);
            swatch.rectTransform.anchoredPosition = new Vector2(x, -(y + 4));
            Label(name + " label", parent, label, 13, Ivory, x + 18, y - 2, 84, 24);
            return swatch;
        }

        private static void LinkVerticalNavigation(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = nav.selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length];
                nav.selectOnDown = nav.selectOnRight = buttons[(i + 1) % buttons.Length];
                buttons[i].navigation = nav;
            }
        }

        private static void LinkHorizontalNavigation(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = nav.selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length];
                nav.selectOnRight = nav.selectOnDown = buttons[(i + 1) % buttons.Length];
                buttons[i].navigation = nav;
            }
        }

        private static RectTransform AnchorPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, float width, float height)
        {
            RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = anchoredPos;
            return rt;
        }

        private static RectTransform Rect(string name, Transform parent, float width, float height, float x = 0, float y = 0)
        {
            RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -y);
            return rt;
        }

        private static void Center(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Image Image(string name, Transform parent, Color color, Sprite sprite, bool raycast)
        {
            Image image = Rect(name, parent, 100, 100).gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image FillImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            Stretch(image.rectTransform);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image Line(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image line = Rect(name, parent, width, height, x, y).gameObject.AddComponent<Image>();
            line.color = color;
            line.raycastTarget = false;
            return line;
        }

        private static Text Label(string name, Transform parent, string text, int size, Color color,
            float x, float y, float width, float height, TextAnchor alignment = TextAnchor.MiddleLeft, Font font = null)
        {
            Text label = Rect(name, parent, width, height, x, y).gameObject.AddComponent<Text>();
            label.font = font != null ? font : bodyFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        private static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static void SetRefs(Object target, params (string name, Object value)[] references)
        {
            using (var so = new SerializedObject(target))
            {
                foreach (var reference in references)
                    so.FindProperty(reference.name).objectReferenceValue = reference.value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
