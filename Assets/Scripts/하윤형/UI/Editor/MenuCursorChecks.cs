using System;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkMaze.Editor
{
    public static class MenuCursorChecks
    {
        public static object VerifyAssets()
        {
            Require(!Application.isPlaying, "Verify saved cursor assets in Edit mode.");
            MenuCursorController cursor = Objects<MenuCursorController>().Single();
            using (var so = new SerializedObject(cursor))
                foreach (string prefix in new[] { "arrow", "select" })
                {
                    var texture = so.FindProperty(prefix + "Texture").objectReferenceValue as Texture2D;
                    Require(texture != null && texture.width == 32 && texture.height == 32, "Cursor texture is missing or not 32x32.");
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                    Require(importer != null && importer.textureType == TextureImporterType.Cursor && importer.isReadable &&
                        !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                        "Incorrect cursor texture import settings.");
                    Vector2 hotspot = so.FindProperty(prefix + "Hotspot").vector2Value;
                    Require(hotspot.x >= 0 && hotspot.y >= 0 && hotspot.x < texture.width && hotspot.y < texture.height,
                        "Cursor hotspot is outside its texture.");
                }
            return new { textures = "passed", readable = "passed", hotspots = "passed", cursorControllers = 1 };
        }

        public static object VerifyMainMenuHover()
        {
            Require(Application.isPlaying, "Run hover checks in Play mode after the menu has rendered.");
            MainMenuController menu = Objects<MainMenuController>().Single();
            Require(!menu.IsDialogOpen && !menu.IsSettingsOpen, "Close dialogs before checking the main menu.");
            MenuCursorController cursor = Objects<MenuCursorController>().Single();
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Start Button", "Help Button", "Settings Button", "Quit Button" })
                Require(cursor.GetCursorKindAt(Center(Find<RectTransform>(name))) == MenuCursorController.CursorKind.Select,
                    name + " did not get a selection cursor.");
            Require(cursor.GetCursorKindAt(Center(Find<RectTransform>("Game title"))) == MenuCursorController.CursorKind.Arrow,
                "Decorative text must not look clickable.");
            Button start = Find<Button>("Start Button");
            bool enabled = start.interactable;
            try
            {
                start.interactable = false;
                Require(cursor.GetCursorKindAt(Center(start.GetComponent<RectTransform>())) == MenuCursorController.CursorKind.Arrow,
                    "A disabled button must not show a selection cursor.");
            }
            finally { start.interactable = enabled; }
            return new { mainButtons = 4, decoration = "arrow", disabledButton = "arrow" };
        }

        public static object VerifyOpenSettingsHover()
        {
            Require(Application.isPlaying && Objects<MainMenuController>().Single().IsSettingsOpen,
                "Open settings and render a frame first.");
            MenuCursorController cursor = Objects<MenuCursorController>().Single();
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Master volume", "Mouse sensitivity", "Invert Y", "Reduced motion", "Settings apply", "Settings cancel" })
                Require(cursor.GetCursorKindAt(Center(Find<RectTransform>(name))) == MenuCursorController.CursorKind.Select,
                    name + " did not get a selection cursor.");
            RectTransform title = Find<RectTransform>("Settings card").Find("Title") as RectTransform;
            Require(cursor.GetCursorKindAt(Center(title)) == MenuCursorController.CursorKind.Arrow,
                "The settings heading must not look clickable.");
            Require(!MenuCursorController.IsInteractive(Find<Button>("Start Button").gameObject),
                "Modal-blocked main buttons must not be considered interactive.");
            return new { sliders = "select", toggles = "select", dialogButtons = "select", blockedMainMenu = "arrow" };
        }

        public static object VerifyOpenDialogHover()
        {
            Require(Application.isPlaying && Objects<MainMenuController>().Single().IsDialogOpen,
                "Open a dialog and render a frame first.");
            MenuCursorController cursor = Objects<MenuCursorController>().Single();
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Dismiss button", "Close dialog", "Confirm quit button" })
            {
                Button button = Find<Button>(name);
                if (!button.gameObject.activeInHierarchy) continue;
                Require(cursor.GetCursorKindAt(Center(button.GetComponent<RectTransform>())) == MenuCursorController.CursorKind.Select,
                    name + " did not get a selection cursor.");
            }
            Require(cursor.GetCursorKindAt(Center(Find<RectTransform>("Start Button"))) == MenuCursorController.CursorKind.Arrow,
                "The dialog backdrop must block selection of the main menu underneath.");
            return new { dialogButtons = "select", backdropBlocksUnderlyingButton = "passed" };
        }

        public static object VerifyCustomTarget()
        {
            // Temporary objects in the personal scene only; removed in finally, never saved.
            Require(Application.isPlaying, "Run this check in Play mode.");
            Objects<MenuCursorController>().Single();
            var target = new GameObject("Temporary HYH Cursor Check", typeof(MenuCursorTarget), typeof(CanvasGroup));
            try
            {
                var marker = target.GetComponent<MenuCursorTarget>();
                Require(MenuCursorController.IsInteractive(target), "Custom click target was not recognized.");
                marker.InteractionEnabled = false;
                Require(!MenuCursorController.IsInteractive(target), "Disabled custom action was still clickable.");
                marker.InteractionEnabled = true;
                target.GetComponent<CanvasGroup>().interactable = false;
                Require(!MenuCursorController.IsInteractive(target), "CanvasGroup did not block the custom action.");
                return new { customTarget = "passed", disabledAction = "passed", canvasGroup = "passed" };
            }
            finally { UnityEngine.Object.DestroyImmediate(target); }
        }

        private static T[] Objects<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            Require(scene.path == StartMenuSceneBuilder.ScenePath, "Only the personal StartMenu scene may be checked.");
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static T Find<T>(string name) where T : Component => Objects<T>().Single(c => c.name == name);
        private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
