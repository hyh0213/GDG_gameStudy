using System;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DarkMaze.Editor
{
    /// <summary>Adds settings only to HYH's existing prefab. Never changes shared assets or scenes.</summary>
    public static class MenuSettingsBuilder
    {
        private static readonly Color Ivory = new Color32(225, 223, 210, 255);
        private static readonly Color Gold = new Color32(184, 160, 120, 255);
        private static readonly Color Muted = new Color32(151, 165, 158, 255);
        private static Font font;

        [MenuItem("Tools/하윤형/UI/Add Settings To Menu Prefab")]
        public static void AddSettings()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/하윤형/Fonts/GothicA1-Regular.ttf");
            if (font == null) throw new InvalidOperationException("Personal UI font is missing.");
            GameObject root = PrefabUtility.LoadPrefabContents(StartMenuSceneBuilder.PrefabPath);
            try
            {
                if (root.GetComponentInChildren<MenuSettingsController>(true) != null)
                    throw new InvalidOperationException("Settings already exist; edit the existing prefab instead of rebuilding it.");
                MainMenuController menu = root.GetComponentInChildren<MainMenuController>(true);
                Transform layout = Find(root, "Menu Layout");
                Button start = Find(root, "Start Button").GetComponent<Button>();
                Button help = Find(root, "Help Button").GetComponent<Button>();
                Button quit = Find(root, "Quit Button").GetComponent<Button>();
                GameObject settingsObject = Object.Instantiate(help.gameObject, layout);
                settingsObject.name = "Settings Button";
                settingsObject.transform.Find("Label").GetComponent<Text>().text = "환경 설정";
                settingsObject.transform.Find("English caption").GetComponent<Text>().text = "SETTINGS";
                Button settingsButton = settingsObject.GetComponent<Button>();
                settingsButton.onClick = new Button.ButtonClickedEvent();
                Button[] buttons = { start, help, settingsButton, quit };
                for (int i = 0; i < buttons.Length; i++)
                {
                    ((RectTransform)buttons[i].transform).anchoredPosition = new Vector2(140, -(657 + i * 74));
                    ((RectTransform)buttons[i].transform).sizeDelta = new Vector2(470, 70);
                    // Fit the cloned decorative edges to the slightly tighter four-button layout.
                    Transform edge = buttons[i].transform.Find("Bottom edge");
                    if (edge != null) ((RectTransform)edge).anchoredPosition = new Vector2(0, -69);
                    Transform glowEdge = buttons[i].transform.Find("Focus glow/Focus edge");
                    if (glowEdge != null) ((RectTransform)glowEdge).sizeDelta = new Vector2(3, 70);
                    buttons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                        selectOnUp = buttons[(i + 3) % 4], selectOnDown = buttons[(i + 1) % 4],
                        selectOnLeft = buttons[(i + 3) % 4], selectOnRight = buttons[(i + 1) % 4] };
                }
                MenuSettingsController settings = menu.gameObject.AddComponent<MenuSettingsController>();
                BuildPanel(root, settings);
                Refs(menu, ("settingsButton", settingsButton), ("settings", settings));
                PrefabUtility.SaveAsPrefabAsset(root, StartMenuSceneBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildPanel(GameObject root, MenuSettingsController settings)
        {
            Transform canvas = root.GetComponentInChildren<Canvas>(true).transform;
            int fadeIndex = Find(root, "Screen fade").GetSiblingIndex();
            BuildPanelIntoCanvas(canvas, settings, root.GetComponentInChildren<MenuAtmosphere>(true), fadeIndex);
        }

        internal static void BuildPanelIntoCanvas(Transform canvas, MenuSettingsController settings,
            MenuAtmosphere atmosphere = null, int siblingIndex = -1)
        {
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/하윤형/Fonts/GothicA1-Regular.ttf");
            if (font == null)
                throw new InvalidOperationException("Personal UI font is missing.");

            Image overlay = Image("Settings overlay", canvas, new Color(0.005f, 0.012f, 0.014f, 0.94f), true);
            Stretch(overlay.rectTransform);
            if (siblingIndex >= 0)
                overlay.transform.SetSiblingIndex(siblingIndex);
            RectTransform card = Rect("Settings card", overlay.transform, 1100, 920);
            Center(card);
            card.gameObject.AddComponent<Image>().color = new Color32(17, 28, 29, 255);
            CanvasGroup content = card.gameObject.AddComponent<CanvasGroup>();
            Rule(card, 0, 1100, Gold);
            Label(card, "Eyebrow", "P R E P A R E   F O R   T H E   D A R K", 13, Gold, 60, 35, 900, 28);
            Label(card, "Title", "환경 설정", 39, Ivory, 58, 75, 900, 68);
            Button close = Button(card, "Settings close", "×", 996, 35, 50, 50);

            Section(card, "화면", 168);
            Label(card, "Resolution caption", "해상도", 21, Ivory, 65, 208, 350, 48);
            Button previous = Button(card, "Previous resolution", "‹", 500, 208, 56, 48);
            Text resolution = Label(card, "Resolution value", "1920 × 1080", 22, Ivory, 568, 208, 390, 48, TextAnchor.MiddleCenter);
            Button next = Button(card, "Next resolution", "›", 974, 208, 56, 48);
            Label(card, "Window caption", "화면 모드", 21, Ivory, 65, 272, 350, 48);
            Button windowMode = Button(card, "Window mode", "창 모드", 500, 272, 530, 48);

            Section(card, "소리", 347);
            Label(card, "Volume caption", "전체 음량", 21, Ivory, 65, 386, 350, 48);
            Slider volume = Slider(card, "Master volume", 0f, 1f, 500, 392);
            Text volumeValue = Label(card, "Volume value", "80%", 20, Gold, 920, 386, 110, 48, TextAnchor.MiddleRight);

            Section(card, "조작 및 편의", 465);
            Label(card, "Sensitivity caption", "마우스 감도", 21, Ivory, 65, 504, 350, 48);
            Slider sensitivity = Slider(card, "Mouse sensitivity", 0.2f, 3f, 500, 510);
            Text sensitivityValue = Label(card, "Sensitivity value", "1.0×", 20, Gold, 920, 504, 110, 48, TextAnchor.MiddleRight);
            Toggle invertY = Toggle(card, "Invert Y", "마우스 Y축 반전", 65, 574, 430);
            Toggle reducedMotion = Toggle(card, "Reduced motion", "메뉴 배경 움직임 줄이기", 550, 574, 480);
            Label(card, "Controls integration note", "감도·Y축 반전은 저장만 지원합니다. 실제 카메라 연결은 플레이 담당자가 진행합니다.",
                16, Muted, 65, 641, 970, 52);
            Text status = Label(card, "Settings status", "", 16, Gold, 65, 713, 970, 64);
            Rule(card, 797, 1100, new Color(Gold.r, Gold.g, Gold.b, 0.22f));
            Button defaults = Button(card, "Settings defaults", "기본값", 65, 824, 220, 60);
            Button apply = Button(card, "Settings apply", "적용하고 닫기", 335, 824, 335, 60);
            Button cancel = Button(card, "Settings cancel", "취소", 720, 824, 310, 60);

            Image confirmOverlay = Image("Display confirmation", overlay.transform, new Color(0.005f, 0.012f, 0.014f, 0.96f), true);
            Stretch(confirmOverlay.rectTransform);
            RectTransform confirmCard = Rect("Confirmation card", confirmOverlay.transform, 840, 430);
            Center(confirmCard);
            confirmCard.gameObject.AddComponent<Image>().color = new Color32(17, 28, 29, 255);
            Rule(confirmCard, 0, 840, Gold);
            Label(confirmCard, "Confirmation title", "화면 설정을 유지할까요?", 31, Ivory, 55, 45, 730, 70);
            Text countdown = Label(confirmCard, "Display countdown", "", 23, Ivory, 55, 145, 730, 130);
            Button keep = Button(confirmCard, "Keep display", "유지하기", 55, 310, 340, 64);
            Button revert = Button(confirmCard, "Revert display", "이전 화면으로", 445, 310, 340, 64);
            keep.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = revert, selectOnDown = revert, selectOnLeft = revert, selectOnUp = revert };
            revert.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = keep, selectOnDown = keep, selectOnLeft = keep, selectOnUp = keep };

            Refs(settings, ("panel", overlay.gameObject), ("contentGroup", content), ("closeButton", close),
                ("cancelButton", cancel), ("applyButton", apply), ("defaultsButton", defaults), ("statusLabel", status),
                ("previousResolution", previous), ("nextResolution", next), ("resolutionLabel", resolution),
                ("windowModeButton", windowMode), ("windowModeLabel", windowMode.GetComponentInChildren<Text>()),
                ("volumeSlider", volume), ("volumeLabel", volumeValue), ("sensitivitySlider", sensitivity),
                ("sensitivityLabel", sensitivityValue), ("invertYToggle", invertY), ("reducedMotionToggle", reducedMotion),
                ("atmosphere", atmosphere), ("confirmation", confirmOverlay.gameObject),
                ("countdownLabel", countdown), ("keepDisplayButton", keep), ("revertDisplayButton", revert));
            // Explicit vertical navigation keeps arrow navigation inside the modal.
            Selectable[] controls = { previous, next, windowMode, volume, sensitivity, invertY, reducedMotion, defaults, apply, cancel, close };
            for (int i = 0; i < controls.Length; i++)
            {
                Navigation navigation = controls[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = controls[(i + controls.Length - 1) % controls.Length];
                navigation.selectOnDown = controls[(i + 1) % controls.Length];
                if (controls[i] is Slider)
                    navigation.selectOnLeft = navigation.selectOnRight = null; // left/right change slider values
                else
                {
                    navigation.selectOnLeft = controls[(i + controls.Length - 1) % controls.Length];
                    navigation.selectOnRight = controls[(i + 1) % controls.Length];
                }
                controls[i].navigation = navigation;
            }
            confirmOverlay.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);
        }

        private static Transform Find(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private static void Section(Transform parent, string text, float y)
        {
            Label(parent, text + " section", text, 15, Gold, 65, y, 970, 30);
            Image line = Image(text + " divider", parent, new Color(Gold.r, Gold.g, Gold.b, 0.16f));
            line.rectTransform.anchoredPosition = new Vector2(65, -(y + 32));
            line.rectTransform.sizeDelta = new Vector2(970, 1);
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

        private static Image Image(string name, Transform parent, Color color, bool raycast = false)
        {
            Image image = Rect(name, parent, 100, 100).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text Label(Transform parent, string name, string text, int size, Color color,
            float x, float y, float width, float height, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text label = Rect(name, parent, width, height, x, y).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static void Rule(Transform parent, float y, float width, Color color)
        {
            Image image = Image("Rule", parent, color);
            image.rectTransform.sizeDelta = new Vector2(width, 1);
            image.rectTransform.anchoredPosition = new Vector2(0, -y);
        }

        private static Button Button(Transform parent, string name, string text, float x, float y, float width, float height)
        {
            RectTransform rt = Rect(name, parent, width, height, x, y);
            Image background = rt.gameObject.AddComponent<Image>();
            background.color = new Color(0.65f, 0.56f, 0.4f, 0.22f);
            Button button = rt.gameObject.AddComponent<Button>();
            Style(button, background);
            Rule(rt, 0, width, new Color(Gold.r, Gold.g, Gold.b, 0.45f));
            Label(rt, "Caption", text, 21, Ivory, 0, 0, width, height, TextAnchor.MiddleCenter);
            return button;
        }

        private static Slider Slider(Transform parent, string name, float min, float max, float x, float y)
        {
            RectTransform rt = Rect(name, parent, 390, 36, x, y);
            Image target = rt.gameObject.AddComponent<Image>();
            target.color = new Color(0, 0, 0, 0.02f);
            Image track = Image("Track", rt, new Color(0.35f, 0.4f, 0.36f));
            track.rectTransform.sizeDelta = new Vector2(390, 5);
            track.rectTransform.anchoredPosition = new Vector2(0, -15);
            RectTransform fillArea = Rect("Fill area", rt, 366, 5, 12, 15);
            Image fill = Image("Fill", fillArea, Gold);
            Stretch(fill.rectTransform);
            RectTransform handleArea = Rect("Handle area", rt, 366, 36, 12, 0);
            Image handle = Image("Handle", handleArea, Gold, true);
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(18, 28);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            Slider slider = rt.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            Style(slider, handle);
            return slider;
        }

        private static Toggle Toggle(Transform parent, string name, string text, float x, float y, float width)
        {
            RectTransform rt = Rect(name, parent, width, 48, x, y);
            Image background = rt.gameObject.AddComponent<Image>();
            background.color = new Color(0.65f, 0.56f, 0.4f, 0.13f);
            Image box = Image("Checkbox", rt, new Color(0.45f, 0.48f, 0.4f));
            box.rectTransform.sizeDelta = new Vector2(22, 22);
            box.rectTransform.anchoredPosition = new Vector2(12, -13);
            Image check = Image("Checked", box.transform, Gold);
            check.rectTransform.sizeDelta = new Vector2(14, 14);
            check.rectTransform.anchoredPosition = new Vector2(4, -4);
            Toggle toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.graphic = check;
            Style(toggle, background);
            Label(rt, "Caption", text, 20, Ivory, 48, 0, width - 55, 48);
            return toggle;
        }

        private static void Style(Selectable control, Graphic target)
        {
            control.targetGraphic = target;
            ColorBlock colors = control.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1.65f, 1.65f, 1.6f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.6f);
            control.colors = colors;
        }

        private static void Refs(Object target, params (string name, Object value)[] references)
        {
            using (var so = new SerializedObject(target))
            {
                foreach (var reference in references) so.FindProperty(reference.name).objectReferenceValue = reference.value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
