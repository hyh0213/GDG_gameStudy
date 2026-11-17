using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkMaze.Editor
{
    /// <summary>Applies horror typography, lighting/fog contrast, and crimson accents to HYH's personal assets.</summary>
    public static class HorrorThemeUpdater
    {
        private static readonly Color BoneIvory = new Color32(230, 224, 210, 255);
        private static readonly Color ColdAsh = new Color32(182, 188, 180, 255);
        private static readonly Color DriedCrimson = new Color32(174, 58, 58, 255);
        private static readonly Color CrimsonGlow = new Color(0.45f, 0.10f, 0.10f, 0.28f);
        private static readonly Color CrimsonIdle = new Color(0.32f, 0.11f, 0.11f, 0.26f);
        private static readonly Color CrimsonRule = new Color(0.70f, 0.23f, 0.23f, 0.78f);

        [MenuItem("Tools/하윤형/UI/Apply Horror Theme")]
        public static void ApplyHorrorTheme()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before applying the horror theme.");

            Font titleFont = LoadFont("Assets/UI/하윤형/Fonts/tillvictorycomes-Regular.ttf");
            Font serifFont = LoadFont("Assets/UI/하윤형/Fonts/SongMyung-Regular.ttf");
            Font bodyFont = LoadFont("Assets/UI/하윤형/Fonts/GothicA1-Regular.ttf");

            UpdateStartMenuPrefab(titleFont, serifFont, bodyFont);
            UpdateGameplayHudPrefab(titleFont, serifFont, bodyFont);
            UpdateStartMenuSceneAtmosphere();

            AssetDatabase.SaveAssets();
        }

        private static void UpdateStartMenuPrefab(Font titleFont, Font serifFont, Font brushFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StartMenuSceneBuilder.PrefabPath);
            try
            {
                Text gameTitle = FindText(root, "Game title");
                gameTitle.font = titleFont;
                gameTitle.color = BoneIvory;

                Text genre = FindText(root, "Genre label");
                genre.color = DriedCrimson;

                Text tagline = FindText(root, "Tagline");
                tagline.font = brushFont;
                tagline.fontSize = 23;
                tagline.lineSpacing = 1.32f;
                tagline.color = ColdAsh;

                Text location = FindText(root, "Location annotation");
                location.font = brushFont;
                location.fontSize = 15;
                location.color = new Color32(188, 182, 170, 210);

                FindImage(root, "Menu divider").color = new Color(0.65f, 0.22f, 0.22f, 0.45f);
                FindImage(root, "Location marker").color = new Color(0.72f, 0.23f, 0.23f, 0.75f);

                foreach (string buttonName in new[] { "Start Button", "Help Button", "Settings Button", "Quit Button" })
                {
                    Transform btn = FindTransform(root, buttonName);
                    Text label = btn.Find("Label").GetComponent<Text>();
                    label.font = serifFont;
                    label.fontSize = 27;

                    Transform glow = btn.Find("Focus glow");
                    if (glow != null)
                    {
                        glow.GetComponent<Image>().color = CrimsonGlow;
                        Transform edge = glow.Find("Focus edge");
                        if (edge != null) edge.GetComponent<Image>().color = DriedCrimson;
                    }

                    if (buttonName == "Start Button")
                    {
                        btn.GetComponent<Image>().color = CrimsonIdle;
                        btn.Find("Top edge").GetComponent<Image>().color = CrimsonRule;
                        btn.Find("Bottom edge").GetComponent<Image>().color = CrimsonRule;
                    }
                }

                FindText(root, "Dialog title").font = serifFont;
                FindText(root, "Dialog body").font = serifFont;
                FindText(root, "Settings card", "Title").font = serifFont;
                FindText(root, "Confirmation title").font = serifFont;
                FindText(root, "Loading label").font = serifFont;

                // Deepen 3D corridor lighting so darkness swallows the far gate
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.name == "Cold corridor fill")
                    {
                        light.intensity = 0.40f;
                        light.range = 12f;
                        light.color = new Color(0.28f, 0.44f, 0.46f, 1f);
                    }
                    else if (light.name == "Pale light beyond")
                    {
                        light.intensity = 2.2f;
                        light.range = 10f;
                        light.color = new Color(0.40f, 0.58f, 0.52f, 1f);
                    }
                    else if (light.name == "Warm lantern light")
                    {
                        light.intensity = 2.8f;
                        light.range = 7.5f;
                        light.color = new Color(0.95f, 0.46f, 0.20f, 1f);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, StartMenuSceneBuilder.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateGameplayHudPrefab(Font titleFont, Font serifFont, Font brushFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GameplayHudBuilder.PrefabPath);
            try
            {
                Text resultTitle = FindText(root, "Result title");
                resultTitle.font = titleFont;
                resultTitle.fontSize = 48;
                resultTitle.color = BoneIvory;

                Text resultDesc = FindText(root, "Result description");
                resultDesc.font = brushFont;
                resultDesc.fontSize = 21;
                resultDesc.lineSpacing = 1.35f;

                FindText(root, "Pause title").font = serifFont;
                FindText(root, "Settings card", "Title").font = serifFont;
                FindText(root, "Confirmation title").font = serifFont;
                FindText(root, "Loading label").font = serifFont;

                string[] buttons =
                {
                    "Pause resume", "Pause settings", "Pause restart", "Pause main menu",
                    "Result retry", "Result main menu"
                };
                foreach (string buttonName in buttons)
                {
                    Transform btn = FindTransform(root, buttonName);
                    Text label = btn.Find("Label").GetComponent<Text>();
                    label.font = serifFont;
                    label.fontSize = 25;

                    Transform glow = btn.Find("Focus glow");
                    if (glow != null)
                    {
                        glow.GetComponent<Image>().color = CrimsonGlow;
                        Transform edge = glow.Find("Focus edge");
                        if (edge != null) edge.GetComponent<Image>().color = DriedCrimson;
                    }

                    if (buttonName == "Pause resume" || buttonName == "Result retry")
                    {
                        btn.GetComponent<Image>().color = CrimsonIdle;
                        btn.Find("Top edge").GetComponent<Image>().color = CrimsonRule;
                        btn.Find("Bottom edge").GetComponent<Image>().color = CrimsonRule;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, GameplayHudBuilder.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateStartMenuSceneAtmosphere()
        {
            Scene previous = SceneManager.GetActiveScene();
            bool openStart = previous.path != StartMenuSceneBuilder.ScenePath;
            Scene startScene = openStart
                ? EditorSceneManager.OpenScene(StartMenuSceneBuilder.ScenePath, OpenSceneMode.Single)
                : previous;

            RenderSettings.ambientLight = new Color(0.042f, 0.058f, 0.060f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.020f, 0.036f, 0.040f, 1f);
            RenderSettings.fogDensity = 0.068f;
            EditorSceneManager.MarkSceneDirty(startScene);
            EditorSceneManager.SaveScene(startScene);
        }

        private static Font LoadFont(string path)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null) throw new InvalidOperationException("Missing font: " + path);
            return font;
        }

        private static Transform FindTransform(GameObject root, string name)
            => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

        private static Text FindText(GameObject root, string name)
            => root.GetComponentsInChildren<Text>(true).Single(t => t.name == name);

        private static Text FindText(GameObject root, string parentName, string name)
            => root.GetComponentsInChildren<Text>(true).Single(t => t.name == name && t.transform.parent != null && t.transform.parent.name == parentName);

        private static Image FindImage(GameObject root, string name)
            => root.GetComponentsInChildren<Image>(true).Single(i => i.name == name);
    }
}
