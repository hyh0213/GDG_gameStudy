using System;
using System.Collections.Generic;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkMaze.Editor
{
    /// <summary>Edit-mode & Play-mode verification for UI-02, UI-03, UI-04, UI-05, UI-06.</summary>
    public static class GameplayHudChecks
    {
        public static object VerifyPrefabAndSceneReferences()
        {
            Require(!Application.isPlaying, "Stop Play mode before verifying prefab/scene links.");
            Scene scene = SceneManager.GetActiveScene();
            Require(scene.path == GameplayHudBuilder.TestScenePath,
                "Open GameplayUITest.unity before verifying HUD references.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayHudBuilder.PrefabPath);
            Require(prefab != null, "GameplayHUD.prefab is missing.");
            Require(prefab.GetComponentsInChildren<Camera>(true).Length == 0,
                "GameplayHUD.prefab must not contain a Camera.");
            Require(prefab.GetComponentsInChildren<AudioListener>(true).Length == 0,
                "GameplayHUD.prefab must not contain an AudioListener.");
            Require(prefab.GetComponentsInChildren<EventSystem>(true).Length == 0,
                "GameplayHUD.prefab must not bake an EventSystem.");

            Transform[] transforms = SceneObjects<Transform>();
            foreach (Transform t in transforms)
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0,
                    "Missing script: " + t.name);

            Component[] components = SceneObjects<Component>().Where(c => c != null).ToArray();
            foreach (Component c in components)
            {
                using (var serialized = new SerializedObject(c))
                {
                    SerializedProperty prop = serialized.GetIterator();
                    while (prop.NextVisible(true))
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                            Require(prop.objectReferenceValue != null || prop.objectReferenceEntityIdValue.Equals(default(EntityId)),
                                "Broken reference: " + c.name + "." + prop.propertyPath);
                }
            }

            GameplayHudController hud = SceneObjects<GameplayHudController>().Single();
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud.gameObject) == GameplayHudBuilder.PrefabPath,
                "GameplayUITest must reference GameplayHUD.prefab.");

            return new
            {
                gameObjects = transforms.Length,
                components = components.Length,
                missingScripts = 0,
                brokenReferences = 0,
                prefabSelfContained = "passed"
            };
        }

        public static object VerifyHudStates()
        {
            Require(Application.isPlaying, "Run HUD state checks in Play mode.");
            Scene scene = SceneManager.GetActiveScene();
            Require(scene.path == GameplayHudBuilder.TestScenePath,
                "Run HUD checks in GameplayUITest.unity.");

            GameplayHudController hud = SceneObjects<GameplayHudController>().Single();
            DarknessScannerUI scanner = SceneObjects<DarknessScannerUI>().Single();
            GameResultController result = SceneObjects<GameResultController>().Single();
            MenuCursorController cursor = SceneObjects<MenuCursorController>().Single();

            // 1. Health & Objective & Interaction Prompt (UI-02)
            hud.SetHealth(25f, 100f);
            Require(Mathf.Approximately(hud.CurrentHealth, 25f), "Health did not update.");
            Require(Find<Text>("Health value").text == "25 / 100", "Health label mismatch.");
            hud.SetObjective("탈출 단서 2 / 3 — 테스트");
            Require(Find<Text>("Objective label").text.Contains("2 / 3"), "Objective text did not update.");
            hud.ShowInteractionPrompt("장치 작동", "E");
            Require(Find<Text>("Prompt action label").text == "장치 작동", "Interaction prompt did not update.");
            hud.HideInteractionPrompt();

            // 2. Darkness & Exploration Gear & Optional UI-06 Slots (UI-04 / UI-06)
            scanner.SetDarknessLevel(0.8f);
            Require(Mathf.Approximately(scanner.DarknessLevel, 0.8f), "Darkness level did not update.");
            scanner.SetScannerState(ScannerState.Active, 0.5f, 2.0f);
            Require(Find<Text>("Scanner status").text.Contains("작동 중"), "Active scanner label mismatch.");
            scanner.SetScannerState(ScannerState.Cooldown, 0.4f, 3.5f);
            Require(Find<Text>("Scanner status").text.Contains("재충전 중"), "Cooldown scanner label mismatch.");
            scanner.SetScannerState(ScannerState.Disabled);
            Require(Find<Text>("Scanner status").text == "사용 불가", "Disabled scanner label mismatch.");
            scanner.SetScannerState(ScannerState.Ready);
            scanner.SetLegendVisible(true);
            Require(Find<RectTransform>("Outline Legend").gameObject.activeSelf, "Outline legend did not show.");

            scanner.SetHealItem(3, true);
            Require(Find<RectTransform>("Heal Item Slot").gameObject.activeSelf && Find<Text>("Heal count").text == "×3",
                "UI-06 heal slot failed.");
            scanner.SetSubGear("광역 탐지기", ScannerState.Ready, 1f, 0f, true);
            Require(Find<RectTransform>("Sub Gear Slot").gameObject.activeSelf, "UI-06 sub gear slot failed.");

            // 3. Pause + Settings + Cursor Restore (UI-02)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            hud.Pause();
            Require(hud.IsPaused && Mathf.Approximately(Time.timeScale, 0f), "Pause did not freeze timeScale.");
            Require(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Pause did not unlock cursor.");
            Require(MenuCursorController.IsInteractive(Find<Button>("Pause resume").gameObject),
                "Pause resume button is not interactive.");
            hud.OpenSettings();
            Require(hud.IsSettingsOpen, "Pause -> Settings did not open.");
            SceneObjects<MenuSettingsController>().Single().Cancel();
            Require(!hud.IsSettingsOpen && hud.IsPaused, "Closing Settings did not return to Pause.");
            hud.Resume();
            Require(!hud.IsPaused && Mathf.Approximately(Time.timeScale, 1f), "Resume did not restore timeScale.");
            Require(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "Resume did not restore cursor state.");

            // Restore visible unlocked cursor for normal test-scene usage after test
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 4. Game Over & Game Clear (UI-05)
            result.ShowGameOver("테스트 사망", 95f, 1, 3);
            Require(result.IsOpen && !result.IsVictory, "Game Over did not open.");
            Require(Find<Text>("Result title").text == "어둠에 삼켜졌습니다", "Game Over title mismatch.");
            Require(Find<Text>("Result stats").text.Contains("01:35") && Find<Text>("Result stats").text.Contains("1 / 3"),
                "Game Over stats mismatch.");
            Require(MenuCursorController.IsInteractive(Find<Button>("Result retry").gameObject),
                "Result retry button is not interactive.");
            Require(!MenuCursorController.IsInteractive(Find<RectTransform>("Result title").gameObject),
                "Result title must not be interactive.");

            result.ShowGameClear(125f, 0.75f, 3, 3);
            Require(result.IsOpen && result.IsVictory, "Game Clear did not open.");
            Require(Find<Text>("Result title").text == "미로를 벗어났습니다", "Game Clear title mismatch.");
            Require(Find<Text>("Result stats").text.Contains("02:05") && Find<Text>("Result stats").text.Contains("75%"),
                "Game Clear stats mismatch.");

            // Test Tab focus cycling on Result overlay
            EventSystem.current.SetSelectedGameObject(Find<Button>("Result retry").gameObject);
            result.MoveFocus(false);
            Require(EventSystem.current.currentSelectedGameObject == Find<Button>("Result main menu").gameObject,
                "Result Tab navigation failed.");

            result.Hide();
            Require(!result.IsOpen && Mathf.Approximately(Time.timeScale, 1f), "Result hide did not reset state.");
            hud.SetHealthImmediate(100f, 100f);
            scanner.SetDarknessLevel(0.25f);
            scanner.SetLegendVisible(false);
            scanner.SetHealItemVisible(false);
            scanner.SetSubGearVisible(false);

            return new
            {
                hudAndHealth = "passed",
                scannerAndDarkness = "passed",
                ui06Slots = "passed",
                pauseAndSettings = "passed",
                gameOverAndClear = "passed",
                cursorIntegration = "passed"
            };
        }

        public static object VerifyPauseHoverAfterRender()
        {
            Require(Application.isPlaying && SceneObjects<GameplayHudController>().Single().IsPaused,
                "Open Pause and render a frame first.");
            MenuCursorController cursor = SceneObjects<MenuCursorController>().Single();
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Pause resume", "Pause settings", "Pause restart", "Pause main menu" })
                Require(cursor.GetCursorKindAt(Center(Find<RectTransform>(name))) == MenuCursorController.CursorKind.Select,
                    name + " did not get selection cursor.");
            Require(cursor.GetCursorKindAt(Center(Find<RectTransform>("Pause title"))) == MenuCursorController.CursorKind.Arrow,
                "Pause title must stay Arrow cursor.");
            Require(cursor.GetCursorKindAt(Center(Find<RectTransform>("Health Panel"))) == MenuCursorController.CursorKind.Arrow,
                "Pause backdrop must block HUD underneath.");
            return new { pauseButtons = 4, pauseTitle = "arrow", backdropBlocksHud = "passed" };
        }

        public static object VerifyResultHoverAfterRender()
        {
            Require(Application.isPlaying && SceneObjects<GameResultController>().Single().IsOpen,
                "Open Result screen and render a frame first.");
            MenuCursorController cursor = SceneObjects<MenuCursorController>().Single();
            Canvas.ForceUpdateCanvases();
            foreach (string name in new[] { "Result retry", "Result main menu" })
                Require(cursor.GetCursorKindAt(Center(Find<RectTransform>(name))) == MenuCursorController.CursorKind.Select,
                    name + " did not get selection cursor.");
            Require(cursor.GetCursorKindAt(Center(Find<RectTransform>("Result title"))) == MenuCursorController.CursorKind.Arrow,
                "Result title must stay Arrow cursor.");
            return new { resultButtons = 2, resultTitle = "arrow" };
        }

        public static object VerifyHudLayout()
        {
            Require(Application.isPlaying, "Run HUD layout checks in Play mode.");
            GameplayHudController hud = SceneObjects<GameplayHudController>().Single();
            DarknessScannerUI scanner = SceneObjects<DarknessScannerUI>().Single();
            GameResultController result = SceneObjects<GameResultController>().Single();

            scanner.SetLegendVisible(true);
            scanner.SetHealItem(2, true);
            scanner.SetSubGear("광역 탐지기", ScannerState.Ready, 1f, 0f, true);
            hud.ShowInteractionPrompt("장치 작동", "E");
            Canvas.ForceUpdateCanvases();

            string[] hudPanels =
            {
                "Health Panel", "Objective Panel", "Interaction Prompt",
                "Scanner Card", "Outline Legend", "Heal Item Slot", "Sub Gear Slot"
            };
            CheckCorners(hudPanels);

            hud.Pause();
            Canvas.ForceUpdateCanvases();
            CheckCorners(new[] { "Pause card", "Pause resume", "Pause main menu" });
            hud.Resume();

            result.ShowGameOver("레이아웃 점검", 60f, 1, 3);
            Canvas.ForceUpdateCanvases();
            CheckCorners(new[] { "Result card", "Result retry", "Result main menu" });
            result.Hide();

            hud.HideInteractionPrompt();
            scanner.SetLegendVisible(false);
            scanner.SetHealItemVisible(false);
            scanner.SetSubGearVisible(false);

            return new { width = Screen.width, height = Screen.height, safeArea = "passed" };
        }

        private static void CheckCorners(string[] names)
        {
            foreach (string name in names)
            {
                RectTransform rect = Find<RectTransform>(name);
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Require(corners.All(c => c.x >= -1f && c.y >= -1f && c.x <= Screen.width + 1f && c.y <= Screen.height + 1f),
                    name + " is clipped at " + Screen.width + "x" + Screen.height);
            }
        }

        private static T[] SceneObjects<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            Require(scene.path == GameplayHudBuilder.TestScenePath, "Only the personal GameplayUITest scene may be checked.");
            return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static T Find<T>(string name) where T : Component => SceneObjects<T>().Single(c => c.name == name);
        private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
