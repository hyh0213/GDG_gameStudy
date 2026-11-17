using System;
using System.Collections.Generic;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DarkMaze.Editor
{
    /// <summary>Non-destructive checks callable via Unity CLI's live C# evaluation.</summary>
    public static class StartMenuChecks
    {
        public static object VerifyButtons()
        {
            Require(Application.isPlaying, "Run these checks in Play mode.");
            MainMenuController menu = SceneObjects<MainMenuController>().SingleOrDefault();
            Require(menu != null, "Menu controller is missing.");
            menu.CloseDialog();

            Click("Help Button");
            Require(menu.IsDialogOpen, "The Help button did not open a dialog.");
            Require(Label("Dialog title").text == "어둠 속에서 살아남기", "Wrong Help title.");
            Require(!Button("Confirm quit button").gameObject.activeSelf, "Help must not show Quit confirmation.");
            Require(EventSystem.current.currentSelectedGameObject == Button("Dismiss button").gameObject,
                "Dialog did not receive keyboard focus.");
            Click("Dismiss button");
            Require(!menu.IsDialogOpen, "Dismiss did not close Help.");

            if (string.IsNullOrWhiteSpace(menu.GameplaySceneName))
            {
                Click("Start Button");
                Require(menu.IsDialogOpen, "Unconfigured gameplay should show a helpful dialog.");
                Require(Label("Dialog title").text == "미로를 준비하고 있습니다", "Wrong missing-scene message.");
                Click("Close dialog");
                Require(!menu.IsDialogOpen, "The close icon did not close the dialog.");
            }

            Click("Quit Button");
            Require(menu.IsDialogOpen && Button("Confirm quit button").gameObject.activeInHierarchy,
                "Quit must ask for confirmation.");
            Click("Dismiss button");
            Require(!menu.IsDialogOpen, "Cancelling Quit must return to the menu.");

            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Start Button", "Help Button", "Settings Button", "Quit Button" })
            {
                Button button = Button(name);
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null,
                    button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
                    name + " is blocked by another graphic.");
            }

            var serializedMenu = new SerializedObject(menu);
            SerializedProperty property = serializedMenu.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                    Require(property.objectReferenceValue != null, "Missing reference: " + property.name);

            EventSystem.current.SetSelectedGameObject(Button("Start Button").gameObject);
            return new { help = "passed", missingScene = "passed", quitCancel = "passed",
                keyboardFocus = "passed", buttonRaycasts = "passed", serializedReferences = "passed" };
        }

        public static object VerifyLayout()
        {
            Canvas.ForceUpdateCanvases();
            string[] names = { "Start Button", "Help Button", "Settings Button", "Quit Button", "Game title", "Tagline", "Header", "Footer" };
            var rects = names.Select(name => SceneObjects<RectTransform>().Single(rt => rt.name == name)).ToArray();
            foreach (RectTransform rect in rects)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Require(corners.All(c => c.x >= -1f && c.y >= -1f && c.x <= Screen.width + 1f && c.y <= Screen.height + 1f),
                    rect.name + " is clipped at " + Screen.width + "x" + Screen.height);
            }
            return new { width = Screen.width, height = Screen.height, visibleElements = names.Length,
                safeArea = "passed", frame = Time.frameCount };
        }

        public static object VerifySceneReferences()
        {
            Require(!Application.isPlaying,
                "Stop Play mode before checking persistent prefab instance links.");
            Transform[] transforms = SceneObjects<Transform>();
            foreach (Transform transform in transforms)
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    "Missing script: " + transform.name);

            Component[] components = SceneObjects<Component>().Where(component => component != null).ToArray();
            foreach (Component component in components)
            {
                using (var serialized = new SerializedObject(component))
                {
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Require(property.objectReferenceValue != null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),
                                "Broken reference: " + component.name + "." + property.propertyPath);
                }
            }
            foreach (Renderer renderer in SceneObjects<Renderer>())
                foreach (Material material in renderer.sharedMaterials)
                    Require(material != null && material.shader != null && material.shader.name != "Hidden/InternalErrorShader",
                        "Missing material/shader: " + renderer.name);

            GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().Single();
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == StartMenuSceneBuilder.PrefabPath,
                "The scene must reference its personal StartMenu prefab.");
            var cameraData = SceneObjects<UniversalAdditionalCameraData>().Single();
            var cameraSettings = new SerializedObject(cameraData);
            int rendererIndex = cameraSettings.FindProperty("m_RendererIndex").intValue;
            var pipeline = new SerializedObject(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
            SerializedProperty renderers = pipeline.FindProperty("m_RendererDataList");
            var expected = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/UI/하윤형/Art/Menu3DRenderer.asset");
            Require(expected != null && rendererIndex >= 0 && rendererIndex < renderers.arraySize &&
                renderers.GetArrayElementAtIndex(rendererIndex).objectReferenceValue == expected,
                "The menu camera lost its approved 3D renderer reference.");

            return new { gameObjects = transforms.Length, components = components.Length,
                missingScripts = 0, brokenReferences = 0, prefab = "passed", menuRenderer = "passed" };
        }

        public static object VerifySettings()
        {
            Require(Application.isPlaying, "Run settings interaction checks in Play mode.");
            MainMenuController menu = SceneObjects<MainMenuController>().Single();
            MenuSettingsController settings = SceneObjects<MenuSettingsController>().Single();
            Require(!menu.IsDialogOpen && !menu.IsSettingsOpen, "Close open dialogs before checking settings.");
            float volume = AudioListener.volume;
            float savedSensitivity = MenuPreferences.MouseSensitivity;
            float savedVolume = MenuPreferences.MasterVolume;
            bool savedInvert = MenuPreferences.InvertY;
            menu.ShowSettings();
            Require(menu.IsSettingsOpen, "Settings did not open.");
            Require(EventSystem.current.currentSelectedGameObject.name == "Master volume", "Settings did not receive focus.");
            Slider volumeControl = SceneObjects<Slider>().Single(s => s.name == "Master volume");
            Slider sensitivity = SceneObjects<Slider>().Single(s => s.name == "Mouse sensitivity");
            Toggle invert = SceneObjects<Toggle>().Single(t => t.name == "Invert Y");
            volumeControl.value = 0.23f;
            sensitivity.value = 2.3f;
            invert.isOn = !savedInvert;
            Require(Mathf.Approximately(AudioListener.volume, volume), "A draft changed live audio before Apply.");
            settings.MoveFocus(false);
            Require(EventSystem.current.currentSelectedGameObject == sensitivity.gameObject, "Settings Tab order is broken.");

            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Settings card", "Settings apply", "Settings cancel", "Settings defaults" })
            {
                RectTransform rect = SceneObjects<RectTransform>().Single(t => t.name == name);
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Require(corners.All(c => c.x >= -1 && c.y >= -1 && c.x <= Screen.width + 1 && c.y <= Screen.height + 1),
                    name + " is clipped.");
            }
            settings.Cancel();
            Require(!menu.IsSettingsOpen, "Cancel did not close settings.");
            Require(MenuPreferences.MouseSensitivity == savedSensitivity && MenuPreferences.MasterVolume == savedVolume &&
                MenuPreferences.InvertY == savedInvert, "Cancel persisted draft values.");
            menu.ShowSettings();
            Require(Mathf.Approximately(volumeControl.value, savedVolume) && Mathf.Approximately(sensitivity.value, savedSensitivity)
                && invert.isOn == savedInvert, "Reopening did not discard the cancelled draft.");
            settings.RestoreDefaults();
            Require(Mathf.Approximately(volumeControl.value, MenuPreferences.DefaultVolume) &&
                Mathf.Approximately(sensitivity.value, 1f) && !invert.isOn, "Default draft values are wrong.");
            settings.Cancel();
            return new { opening = "passed", draftCancel = "passed", defaultsDraft = "passed", focus = "passed",
                layout = "passed", preferencesWritten = false,
                displayRollback = "requires standalone build" };
        }

        public static object VerifyOpenSettingsRaycasts()
        {
            Require(Application.isPlaying && SceneObjects<MenuSettingsController>().Single().IsOpen,
                "Open settings and let the Game view render a frame before checking raycasts.");
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Settings apply", "Settings cancel", "Settings defaults", "Settings close" })
            {
                Button button = Button(name);
                RectTransform rect = button.GetComponent<RectTransform>();
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                    { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) }, hits);
                Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
                    name + " is blocked or has not rendered yet.");
            }
            return new { settingsButtonRaycasts = "passed" };
        }

        public static object VerifySettingsPersistence()
        {
            Require(Application.isPlaying, "Run this check in Editor Play mode.");
            var menu = SceneObjects<MainMenuController>().Single();
            var settings = SceneObjects<MenuSettingsController>().Single();
            Require(!menu.IsSettingsOpen && !menu.IsDialogOpen, "Close dialogs before checking persistence.");
            const string prefix = "DarkMaze.HYH.Settings.";
            string[] floatKeys = { prefix + "Volume", prefix + "Sensitivity" };
            string[] intKeys = { prefix + "InvertY", prefix + "ReducedMotion", prefix + "Width", prefix + "Height", prefix + "Fullscreen" };
            var floatValues = floatKeys.ToDictionary(key => key, key => PlayerPrefs.GetFloat(key));
            var intValues = intKeys.ToDictionary(key => key, key => PlayerPrefs.GetInt(key));
            var existed = floatKeys.Concat(intKeys).ToDictionary(key => key, PlayerPrefs.HasKey);
            float originalVolume = AudioListener.volume;
            try
            {
                menu.ShowSettings();
                SceneObjects<Slider>().Single(s => s.name == "Master volume").value = 0.37f;
                SceneObjects<Slider>().Single(s => s.name == "Mouse sensitivity").value = 1.7f;
                SceneObjects<Toggle>().Single(t => t.name == "Invert Y").isOn = true;
                Button("Settings apply").onClick.Invoke();
                Require(!menu.IsSettingsOpen, "Apply did not close settings in Editor.");
                Require(Mathf.Approximately(MenuPreferences.MasterVolume, 0.37f) &&
                    Mathf.Approximately(AudioListener.volume, 0.37f) &&
                    Mathf.Approximately(MenuPreferences.MouseSensitivity, 1.7f) && MenuPreferences.InvertY,
                    "Apply did not update preferences and live audio.");
                menu.ShowSettings();
                Require(Mathf.Approximately(SceneObjects<Slider>().Single(s => s.name == "Master volume").value, 0.37f),
                    "Reopen did not load persisted settings.");
                settings.Cancel();
                return new { saveAndReload = "passed", masterVolume = "passed", existingPreferences = "restored after test",
                    displayApplication = "Editor intentionally skips; standalone unverified" };
            }
            finally
            {
                if (settings.IsOpen) settings.Cancel();
                foreach (string key in floatKeys)
                    if (existed[key]) PlayerPrefs.SetFloat(key, floatValues[key]); else PlayerPrefs.DeleteKey(key);
                foreach (string key in intKeys)
                    if (existed[key]) PlayerPrefs.SetInt(key, intValues[key]); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                AudioListener.volume = originalVolume;
                SceneObjects<MenuAtmosphere>().Single().SetReducedMotion(MenuPreferences.ReducedMenuMotion);
            }
        }

        private static T[] SceneObjects<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            Require(scene.path == StartMenuSceneBuilder.ScenePath,
                "Checks only run against the personal StartMenu scene; other scenes are never modified.");
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static Button Button(string name) => SceneObjects<Button>().Single(b => b.name == name);

        private static Text Label(string name) => SceneObjects<Text>().Single(t => t.name == name);

        private static void Click(string name) => Button(name).onClick.Invoke();

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
