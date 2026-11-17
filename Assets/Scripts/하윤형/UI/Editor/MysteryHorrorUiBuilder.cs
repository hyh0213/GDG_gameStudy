using System;
using System.IO;
using System.Linq;
using DarkMaze.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DarkMaze.Editor
{
    /// <summary>
    /// Generates full-screen mystery-horror ink/blood artwork, maze exit POV illustration,
    /// and non-boxed full-screen Result & Pause layouts matching the user's sketches.
    /// </summary>
    public static class MysteryHorrorUiBuilder
    {
        private const string ArtDir = "Assets/UI/하윤형/Art/UI";
        private const string CardPath = ArtDir + "/DossierCard.png";
        private const string BrushPath = ArtDir + "/InkBrushWash.png";
        private const string SplatterPath = ArtDir + "/InkBloodSplatter.png";
        private const string ClearArtPath = ArtDir + "/MazeExitClear.png";
        private const string GameOverArtPath = ArtDir + "/GameOverArtwork.png";
        private const string PauseVeilPath = ArtDir + "/LeftInkVeil.png";

        [MenuItem("Tools/하윤형/UI/Apply Mystery Horror Ink & Exit Theme")]
        public static void ApplyMysteryHorrorUi()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before updating UI assets.");

            if (!Directory.Exists(ArtDir))
                AssetDatabase.CreateFolder("Assets/UI/하윤형/Art", "UI");

            GenerateTextures();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters();

            Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardPath);
            Sprite brushSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BrushPath);
            Sprite splatterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SplatterPath);
            Sprite clearSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ClearArtPath);
            Sprite gameOverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameOverArtPath);
            Sprite pauseVeilSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PauseVeilPath);
            Font bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/하윤형/Fonts/GothicA1-Regular.ttf");

            UpdateStartMenuPrefab(cardSprite, brushSprite, splatterSprite, bodyFont);
            UpdateGameplayHudPrefab(cardSprite, brushSprite, splatterSprite, clearSprite, gameOverSprite, pauseVeilSprite, bodyFont);

            if (File.Exists("Assets/UI/하윤형/Fonts/EastSeaDokdo-Regular.ttf"))
                AssetDatabase.DeleteAsset("Assets/UI/하윤형/Fonts/EastSeaDokdo-Regular.ttf");
            if (File.Exists("Assets/UI/하윤형/Fonts/EastSeaDokdo-OFL.txt"))
                AssetDatabase.DeleteAsset("Assets/UI/하윤형/Fonts/EastSeaDokdo-OFL.txt");

            AssetDatabase.SaveAssets();
        }

        private static void GenerateTextures()
        {
            WritePng(CardPath, CreateDossierCard(512, 512));
            WritePng(BrushPath, CreateInkBrushWash(512, 128));
            WritePng(SplatterPath, CreateInkBloodSplatter(512, 512));
            WritePng(ClearArtPath, CreateMazeExitClear(1280, 720));
            WritePng(GameOverArtPath, CreateGameOverArtwork(1280, 720));
            WritePng(PauseVeilPath, CreateLeftInkVeil(1280, 720));
        }

        private static void ConfigureImporters()
        {
            ConfigureSprite(CardPath, new Vector4(64, 64, 64, 64));
            ConfigureSprite(BrushPath, Vector4.zero);
            ConfigureSprite(SplatterPath, Vector4.zero);
            ConfigureSprite(ClearArtPath, Vector4.zero);
            ConfigureSprite(GameOverArtPath, Vector4.zero);
            ConfigureSprite(PauseVeilPath, Vector4.zero);
        }

        private static void ConfigureSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void UpdateStartMenuPrefab(Sprite cardSprite, Sprite brushSprite, Sprite splatterSprite, Font bodyFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(StartMenuSceneBuilder.PrefabPath);
            try
            {
                Text tagline = FindText(root, "Tagline");
                tagline.font = bodyFont;
                tagline.fontSize = 23;
                tagline.lineSpacing = 1.32f;

                Text location = FindText(root, "Location annotation");
                location.font = bodyFont;
                location.fontSize = 15;

                Transform layout = FindTransform(root, "Menu Layout");
                Image titleStain = EnsureOverlayImage("Title ink stain", layout, splatterSprite,
                    new Color(1f, 1f, 1f, 0.30f), false);
                RectTransform stainRt = titleStain.rectTransform;
                stainRt.anchorMin = stainRt.anchorMax = stainRt.pivot = new Vector2(0, 1);
                stainRt.anchoredPosition = new Vector2(54f, -210f);
                stainRt.sizeDelta = new Vector2(460f, 360f);
                titleStain.transform.SetSiblingIndex(FindTransform(root, "Genre label").GetSiblingIndex());

                foreach (string buttonName in new[] { "Start Button", "Help Button", "Settings Button", "Quit Button" })
                {
                    Transform btn = FindTransform(root, buttonName);
                    Image glowImg = btn.Find("Focus glow").GetComponent<Image>();
                    glowImg.sprite = brushSprite;
                    glowImg.type = Image.Type.Simple;
                    glowImg.color = new Color(0.55f, 0.13f, 0.13f, 0.45f);
                }

                foreach (string modal in new[] { "Dialog card", "Settings card", "Confirmation card" })
                {
                    Transform cardTf = FindTransform(root, modal);
                    StyleCard(cardTf, cardSprite, Color.white);
                    SoftenBorders(cardTf);
                }

                Transform dialogCard = FindTransform(root, "Dialog card");
                Image dialogStain = EnsureOverlayImage("Dialog ink stain", dialogCard, splatterSprite,
                    new Color(1f, 1f, 1f, 0.34f), false);
                StretchCorner(dialogStain.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), 260, 260);
                dialogStain.transform.SetSiblingIndex(1);

                PrefabUtility.SaveAsPrefabAsset(root, StartMenuSceneBuilder.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateGameplayHudPrefab(Sprite cardSprite, Sprite brushSprite, Sprite splatterSprite,
            Sprite clearSprite, Sprite gameOverSprite, Sprite pauseVeilSprite, Font bodyFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GameplayHudBuilder.PrefabPath);
            try
            {
                // Revert Result description back to GothicA1
                Text resultDesc = FindText(root, "Result description");
                resultDesc.font = bodyFont;
                resultDesc.fontSize = 21;
                resultDesc.lineSpacing = 1.35f;

                // Small HUD status indicators keep distressed dark charcoal feel
                foreach (string cardName in new[]
                {
                    "Health Panel", "Objective Panel", "Interaction Prompt",
                    "Outline Legend", "Scanner Card", "Heal Item Slot", "Sub Gear Slot"
                })
                {
                    Transform cardTf = FindTransform(root, cardName);
                    StyleCard(cardTf, cardSprite, new Color32(235, 232, 226, 240));
                    SoftenBorders(cardTf);
                }

                foreach (string modalCard in new[] { "Settings card", "Confirmation card" })
                {
                    Transform cardTf = FindTransform(root, modalCard);
                    StyleCard(cardTf, cardSprite, Color.white);
                    SoftenBorders(cardTf);
                }

                // Brushed ink wash focus on all Pause & Result buttons
                foreach (string buttonName in new[]
                {
                    "Pause resume", "Pause settings", "Pause restart", "Pause main menu",
                    "Result retry", "Result main menu"
                })
                {
                    Transform btn = FindTransform(root, buttonName);
                    Image glowImg = btn.Find("Focus glow").GetComponent<Image>();
                    glowImg.sprite = brushSprite;
                    glowImg.type = Image.Type.Simple;
                    glowImg.color = new Color(0.55f, 0.13f, 0.13f, 0.45f);
                }

                // --- PAUSE OVERLAY: Full-screen non-boxed left-column composition ---
                Transform pauseOverlay = FindTransform(root, "Pause overlay");
                Image pauseOverlayBg = pauseOverlay.GetComponent<Image>();
                pauseOverlayBg.sprite = pauseVeilSprite;
                pauseOverlayBg.color = Color.white;

                Transform pauseCard = FindTransform(root, "Pause card");
                Image pauseCardBg = pauseCard.GetComponent<Image>();
                pauseCardBg.sprite = null;
                pauseCardBg.color = new Color(0f, 0f, 0f, 0f); // No rectangular panel!
                pauseCardBg.raycastTarget = false;
                HideBorders(pauseCard);

                RectTransform pauseRt = pauseCard.GetComponent<RectTransform>();
                pauseRt.anchorMin = pauseRt.anchorMax = new Vector2(0.5f, 0.5f);
                pauseRt.pivot = new Vector2(0.5f, 0.5f);
                pauseRt.anchoredPosition = Vector2.zero;
                pauseRt.sizeDelta = new Vector2(1760f, 920f);

                Image pauseStain = EnsureOverlayImage("Pause ink stain", pauseCard, splatterSprite,
                    new Color(1f, 1f, 1f, 0.34f), false);
                StretchCorner(pauseStain.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-10, 10), 420, 420);
                pauseStain.transform.SetSiblingIndex(0);

                PlaceTopLeft(FindTransform(root, "Pause eyebrow"), 30f, 70f, 460f, 24f);
                PlaceTopLeft(FindTransform(root, "Pause title"), 30f, 104f, 460f, 64f);
                PlaceTopLeft(FindTransform(root, "Pause divider"), 30f, 182f, 420f, 1f);
                ResizeMenuButton(FindTransform(root, "Pause resume"), 30f, 224f, 420f);
                ResizeMenuButton(FindTransform(root, "Pause settings"), 30f, 312f, 420f);
                ResizeMenuButton(FindTransform(root, "Pause restart"), 30f, 400f, 420f);
                ResizeMenuButton(FindTransform(root, "Pause main menu"), 30f, 488f, 420f);
                PlaceTopLeft(FindTransform(root, "Pause status"), 30f, 590f, 460f, 64f);

                // --- RESULT OVERLAY: Full-screen 16:9 composition (No popup box!) ---
                Transform resultOverlay = FindTransform(root, "Result overlay");
                Image overlayBg = resultOverlay.GetComponent<Image>();
                overlayBg.color = new Color(0f, 0f, 0f, 0f);

                Transform resultCard = FindTransform(root, "Result card");
                Image resultCardBg = resultCard.GetComponent<Image>();
                resultCardBg.sprite = null;
                resultCardBg.color = new Color(0f, 0f, 0f, 0f); // Transparent container, NO box!
                resultCardBg.raycastTarget = false;
                HideBorders(resultCard);

                RectTransform resultCardRt = resultCard.GetComponent<RectTransform>();
                resultCardRt.anchorMin = resultCardRt.anchorMax = new Vector2(0.5f, 0.5f);
                resultCardRt.pivot = new Vector2(0.5f, 0.5f);
                resultCardRt.anchoredPosition = Vector2.zero;
                resultCardRt.sizeDelta = new Vector2(1760f, 920f);

                CanvasGroup cardGroup = resultCard.GetComponent<CanvasGroup>();
                if (cardGroup == null) cardGroup = resultCard.gameObject.AddComponent<CanvasGroup>();

                // Clean up old corner stain if it lived inside Result card
                Transform oldStain = resultCard.Find("Result blood ink stain");
                if (oldStain != null) Object.DestroyImmediate(oldStain.gameObject);

                // Layer 0: Full-screen pitch-black blackout
                Image blackoutImg = EnsureOverlayImage("Result blackout", resultOverlay, null, Color.black, true);
                StretchFull(blackoutImg.rectTransform);
                blackoutImg.transform.SetSiblingIndex(0);
                CanvasGroup blackoutGroup = blackoutImg.GetComponent<CanvasGroup>();
                if (blackoutGroup == null) blackoutGroup = blackoutImg.gameObject.AddComponent<CanvasGroup>();

                // Layer 1: Full-screen Game Over ink & blood artwork
                Image gameOverImg = EnsureOverlayImage("Result game over artwork", resultOverlay, gameOverSprite, Color.white, false);
                StretchFull(gameOverImg.rectTransform);
                gameOverImg.preserveAspect = false;
                gameOverImg.transform.SetSiblingIndex(1);
                CanvasGroup gameOverGroup = gameOverImg.GetComponent<CanvasGroup>();
                if (gameOverGroup == null) gameOverGroup = gameOverImg.gameObject.AddComponent<CanvasGroup>();
                gameOverImg.gameObject.SetActive(false);

                // Layer 2: Full-screen Game Clear exit POV illustration
                Image clearImg = EnsureOverlayImage("Result clear artwork", resultOverlay, clearSprite, Color.white, false);
                StretchFull(clearImg.rectTransform);
                clearImg.preserveAspect = false;
                clearImg.transform.SetSiblingIndex(2);
                CanvasGroup clearGroup = clearImg.GetComponent<CanvasGroup>();
                if (clearGroup == null) clearGroup = clearImg.gameObject.AddComponent<CanvasGroup>();
                clearImg.gameObject.SetActive(false);

                // Layer 3: Transparent full-screen editorial typography & lower-left buttons
                resultCard.SetSiblingIndex(3);

                PlaceTopLeft(FindTransform(root, "Result eyebrow"), 30f, 54f, 540f, 24f);
                Transform topBorder = resultCard.Find("Result top border");
                if (topBorder != null) PlaceTopLeft(topBorder, 30f, 36f, 430f, 2f);
                Transform resTitleTf = FindTransform(root, "Result title");
                PlaceTopLeft(resTitleTf, 28f, 90f, 580f, 84f);
                Text resTitleText = resTitleTf.GetComponent<Text>();
                resTitleText.fontSize = 54;

                PlaceTopLeft(FindTransform(root, "Result divider"), 30f, 190f, 430f, 1f);
                PlaceTopLeft(FindTransform(root, "Result description"), 30f, 214f, 540f, 84f);
                PlaceTopLeft(FindTransform(root, "Result stats"), 30f, 314f, 540f, 40f);

                // Two action buttons anchored cleanly in bottom-left over dark foreground wall/void
                ResizeMenuButton(FindTransform(root, "Result retry"), 30f, 718f, 256f);
                ResizeMenuButton(FindTransform(root, "Result main menu"), 306f, 718f, 256f);
                PlaceTopLeft(FindTransform(root, "Result notice"), 30f, 810f, 540f, 56f);

                GameResultController resultController = root.GetComponentInChildren<GameResultController>(true);
                using (var so = new SerializedObject(resultController))
                {
                    so.FindProperty("blackoutGroup").objectReferenceValue = blackoutGroup;
                    so.FindProperty("cardGroup").objectReferenceValue = cardGroup;
                    so.FindProperty("resultCardRect").objectReferenceValue = resultCardRt;
                    so.FindProperty("clearArtworkRoot").objectReferenceValue = clearImg.gameObject;
                    so.FindProperty("clearArtworkGroup").objectReferenceValue = clearGroup;
                    so.FindProperty("gameOverStainRoot").objectReferenceValue = gameOverImg.gameObject;
                    so.FindProperty("gameOverArtworkGroup").objectReferenceValue = gameOverGroup;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, GameplayHudBuilder.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void PlaceTopLeft(Transform tf, float x, float y, float w, float h)
        {
            var rt = tf.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void ResizeMenuButton(Transform btn, float x, float y, float width)
        {
            const float height = 70f;
            PlaceTopLeft(btn, x, y, width, height);

            Transform topEdge = btn.Find("Top edge");
            if (topEdge != null) PlaceTopLeft(topEdge, 0f, 0f, width, 1f);

            Transform bottomEdge = btn.Find("Bottom edge");
            if (bottomEdge != null) PlaceTopLeft(bottomEdge, 0f, height - 1f, width, 1f);

            Transform label = btn.Find("Label");
            if (label != null) PlaceTopLeft(label, 22f, 5f, width - 74f, 38f);

            Transform sub = btn.Find("English caption");
            if (sub != null) PlaceTopLeft(sub, 24f, 43f, width - 74f, 20f);

            Transform arrow = btn.Find("Arrow");
            if (arrow != null) PlaceTopLeft(arrow, width - 44f, 22f, 24f, 24f);
        }

        private static void HideBorders(Transform card)
        {
            foreach (Transform child in card)
            {
                if (child.name.IndexOf("border", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        Color c = img.color;
                        c.a = 0f;
                        img.color = c;
                    }
                }
            }
        }

        private static void SoftenBorders(Transform card)
        {
            foreach (Transform child in card)
            {
                if (child.name.IndexOf("border", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null && child.name.IndexOf("top", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        Color c = img.color;
                        c.a = Mathf.Min(c.a, 0.14f);
                        img.color = c;
                    }
                }
            }
        }

        private static void StyleCard(Transform card, Sprite sprite, Color tint)
        {
            Image img = card.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = tint;
        }

        private static Image EnsureOverlayImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            if (existing == null) go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void StretchCorner(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(w, h);
        }

        // --- Procedural Art Generators ---

        private static Texture2D CreateDossierCard(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float dx = Mathf.Min(x, w - 1 - x);
                    float dy = Mathf.Min(y, h - 1 - y);
                    float edgeDist = Mathf.Min(dx, dy);

                    float grain = Fbm(nx * 14f, ny * 14f, 11.3f) * 0.55f + Fbm(nx * 46f, ny * 46f, 3.7f) * 0.45f;
                    float stain = Mathf.Pow(Fbm(nx * 4.2f, ny * 4.2f, 7.1f), 1.6f);
                    float tornNoise = (Fbm(nx * 24f, ny * 24f, 19.2f) - 0.5f) * 8f;
                    float edgeAlpha = Mathf.Clamp01((edgeDist - 2.5f + tornNoise) / 4.2f);
                    float burn = 1f - SmoothRange(0f, 48f, edgeDist + tornNoise * 1.4f);

                    float baseR = 0.088f + grain * 0.058f - stain * 0.048f - burn * 0.058f;
                    float baseG = 0.104f + grain * 0.058f - stain * 0.052f - burn * 0.066f;
                    float baseB = 0.098f + grain * 0.052f - stain * 0.048f - burn * 0.062f;

                    bool innerLine = (Mathf.Abs(edgeDist - 14f) < 0.9f) && (edgeDist > 12f);
                    if (innerLine)
                    {
                        float lineAlpha = 0.22f + 0.12f * grain;
                        baseR += 0.36f * lineAlpha;
                        baseG += 0.28f * lineAlpha;
                        baseB += 0.19f * lineAlpha;
                    }

                    pixels[y * w + x] = ToColor32(baseR, baseG, baseB, edgeAlpha * 0.97f);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateInkBrushWash(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                float verticalFade = SmoothRange(0f, 0.16f, ny) * SmoothRange(1f, 0.84f, ny);
                float bristles = Mathf.Lerp(0.65f, 1.0f, Mathf.Clamp01(Mathf.PerlinNoise(ny * 38f, 2.7f)));

                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float sweepEnd = 0.82f + (Mathf.Clamp01(Mathf.PerlinNoise(ny * 26f, 8.1f)) - 0.5f) * 0.28f;
                    float rightFade = 1f - SmoothRange(sweepEnd - 0.35f, sweepEnd, nx);
                    float leftBleed = SmoothRange(0f, 0.03f, nx);
                    float inkNoise = Fbm(nx * 10f, ny * 18f, 5.4f);

                    float alpha = verticalFade * rightFade * leftBleed * bristles * Mathf.Lerp(0.55f, 1f, inkNoise);
                    float shade = 0.72f + inkNoise * 0.28f;
                    pixels[y * w + x] = ToColor32(shade, shade * 0.94f, shade * 0.94f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateInkBloodSplatter(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            var rng = new System.Random(20261113);

            for (int i = 0; i < 42; i++)
            {
                bool blood = i % 3 != 0;
                float cx = (float)(0.22 + rng.NextDouble() * 0.62);
                float cy = (float)(0.22 + rng.NextDouble() * 0.62);
                float r = (float)(0.025 + Math.Pow(rng.NextDouble(), 2.2) * 0.17);
                Color col = blood
                    ? new Color(0.32f + (float)rng.NextDouble() * 0.12f, 0.04f, 0.04f, 0.55f)
                    : new Color(0.02f, 0.025f, 0.025f, 0.65f);
                PaintSplatterBlob(pixels, w, h, cx, cy, r, col, (float)rng.NextDouble() * 50f);
            }

            for (int i = 0; i < 95; i++)
            {
                bool blood = i % 2 == 0;
                float cx = (float)(0.08 + rng.NextDouble() * 0.84);
                float cy = (float)(0.08 + rng.NextDouble() * 0.84);
                float r = (float)(0.004 + rng.NextDouble() * 0.016);
                Color col = blood
                    ? new Color(0.38f, 0.05f, 0.05f, 0.68f)
                    : new Color(0.02f, 0.025f, 0.025f, 0.72f);
                PaintSplatterBlob(pixels, w, h, cx, cy, r, col, (float)rng.NextDouble() * 50f);
            }

            var bytes = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                bytes[i] = ToColor32(c.r, c.g, c.b, c.a);
            }
            tex.SetPixels32(bytes);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Full-screen 16:9 Game Over illustration matching the user's sketch:
        /// pitch black void + dark ink haze around upper-left title + organic dried crimson blood pools/splatters on right & bottom-right.
        /// </summary>
        private static Texture2D CreateGameOverArtwork(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var buf = new Color[w * h];
            var rng = new System.Random(44091);

            // Base: deep pitch darkness with subtle ink-wash mist near upper-left title ("먹")
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float inkDist = Vector2.Distance(new Vector2(nx * 1.3f, ny), new Vector2(0.18f * 1.3f, 0.78f));
                    float inkCloud = Mathf.Exp(-inkDist * inkDist * 9f) * Fbm(nx * 10f, ny * 10f, 3.1f);
                    float v = 0.005f + inkCloud * 0.045f;
                    buf[y * w + x] = new Color(v, v * 1.05f, v * 1.08f, 1f);
                }
            }

            // Ink splatter cluster near top-left ("먹")
            for (int i = 0; i < 24; i++)
            {
                float cx = (float)(0.04 + rng.NextDouble() * 0.24);
                float cy = (float)(0.62 + rng.NextDouble() * 0.28);
                float r = (float)(0.015 + Math.Pow(rng.NextDouble(), 2.0) * 0.065);
                PaintSplatterBlob(buf, w, h, cx, cy, r, new Color(0.05f, 0.055f, 0.06f, 0.78f), (float)rng.NextDouble() * 60f);
            }

            // Right & bottom-right bloodstain pools matching the sketch ("핏자국"):
            // 1) Upper-right crimson streak/pool
            PaintBloodCluster(buf, w, h, 0.78f, 0.76f, 0.085f, 18, rng);
            // 2) Mid-right crimson splash + downward drip
            PaintBloodCluster(buf, w, h, 0.80f, 0.48f, 0.075f, 16, rng);
            // 3) Lower-right major pooled bloodstains & secondary pool to its left
            PaintBloodCluster(buf, w, h, 0.75f, 0.21f, 0.14f, 34, rng);
            PaintBloodCluster(buf, w, h, 0.57f, 0.20f, 0.068f, 15, rng);

            // Fine crimson arterial mist droplets scattered on the right half
            for (int i = 0; i < 160; i++)
            {
                float cx = (float)(0.45 + rng.NextDouble() * 0.50);
                float cy = (float)(0.05 + rng.NextDouble() * 0.88);
                float r = (float)(0.0025 + Math.Pow(rng.NextDouble(), 2.8) * 0.014);
                float tone = (float)rng.NextDouble();
                Color col = new Color(0.35f + tone * 0.28f, 0.018f, 0.020f, 0.75f);
                PaintSplatterBlob(buf, w, h, cx, cy, r, col, (float)rng.NextDouble() * 90f);
            }

            var bytes = new Color32[w * h];
            for (int i = 0; i < buf.Length; i++)
                bytes[i] = ToColor32(buf[i].r, buf[i].g, buf[i].b, 1f);

            tex.SetPixels32(bytes);
            tex.Apply();
            return tex;
        }

        private static void PaintBloodCluster(Color[] buf, int w, int h, float cx, float cy, float spread, int count, System.Random rng)
        {
            // Dark clotted core
            PaintSplatterBlob(buf, w, h, cx, cy, spread * 0.75f, new Color(0.18f, 0.012f, 0.014f, 0.88f), (float)rng.NextDouble() * 50f);
            // Rich dried crimson lobes
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float dist = (float)(Math.Pow(rng.NextDouble(), 1.4) * spread);
                float bx = cx + Mathf.Cos(angle) * dist * 1.15f;
                float by = cy + Mathf.Sin(angle) * dist * 0.75f;
                float r = (float)(spread * (0.14 + rng.NextDouble() * 0.42));
                bool darkCore = i % 3 == 0;
                Color col = darkCore
                    ? new Color(0.22f, 0.015f, 0.016f, 0.82f)
                    : new Color(0.46f + (float)rng.NextDouble() * 0.14f, 0.025f, 0.028f, 0.76f);
                PaintSplatterBlob(buf, w, h, bx, by, r, col, (float)rng.NextDouble() * 70f);
            }
        }

        /// <summary>
        /// Full-screen 16:9 Game Clear illustration matching the user's sketch:
        /// Left ~38%: jagged dark stone maze inner wall (natural dark backdrop for left text + bottom-left buttons).
        /// Right ~62%: wide open maze exit revealing sky, layered misty pine trees, and an earth/dirt exit path.
        /// </summary>
        private static Texture2D CreateMazeExitClear(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);

                    float stoneTex = Fbm(nx * 22f, ny * 18f, 4.2f);
                    float fineGrain = Fbm(nx * 60f, ny * 60f, 12.9f);
                    float mist = Fbm(nx * 5.5f, ny * 4.5f, 9.1f);

                    // --- OUTSIDE WORLD (Right 62%: Sky, Trees, Earth/Dirt trail) ---
                    // Sky gradient (cool slate night-dawn -> pale moonlit mist horizon)
                    float skyT = SmoothRange(0.32f, 0.96f, ny);
                    Color skyHorizon = new Color(0.64f, 0.74f, 0.69f);
                    Color skyZenith = new Color(0.14f, 0.22f, 0.25f);
                    Color sky = Color.Lerp(skyHorizon, skyZenith, skyT);
                    sky += new Color(0.08f, 0.10f, 0.09f) * mist;

                    // Soft moon / dawn break glow at upper right (0.76, 0.76)
                    float moonDist = Vector2.Distance(new Vector2((nx - 0.76f) * 1.55f, ny - 0.76f), Vector2.zero);
                    sky += new Color(0.45f, 0.44f, 0.34f) * Mathf.Exp(-moonDist * moonDist * 140f);
                    sky += new Color(0.32f, 0.32f, 0.26f) * SmoothRange(0.055f, 0.028f, moonDist);

                    // Distant misty mountain/forest ridge + irregular organic pine treeline
                    float farRidge = 0.45f + Fbm(nx * 5.2f, 1.4f, 2.2f) * 0.11f;
                    float canopyNoise = Fbm(nx * 19f, 3.1f, 6.1f) * 0.10f + Fbm(nx * 47f, 7.3f, 9.4f) * 0.045f;
                    float pinePeak = Mathf.Pow(Fbm(nx * 34f, 1.8f, 12.7f), 1.4f) * 0.085f;
                    float nearTreeH = 0.31f + canopyNoise + pinePeak;

                    // Ground / Earth ("흙") contour + winding dirt trail narrowing toward the trees
                    float groundH = 0.255f + Fbm(nx * 6.5f, 9.2f, 1.7f) * 0.022f;

                    Color outside;
                    if (ny >= groundH)
                    {
                        outside = sky;
                        if (ny < farRidge)
                            outside = Color.Lerp(outside, new Color(0.15f, 0.22f, 0.22f), 0.72f);
                        if (ny < nearTreeH)
                        {
                            float depthShade = SmoothRange(groundH, nearTreeH, ny);
                            float treeShade = Mathf.Lerp(0.045f, 0.085f, depthShade) + stoneTex * 0.025f;
                            outside = new Color(treeShade * 0.80f, treeShade * 1.12f, treeShade * 1.02f);
                        }
                        // Soft morning/moonlit ground mist hugging the tree line
                        float lowMist = SmoothRange(groundH + 0.14f, groundH, ny) * (0.35f + 0.45f * mist);
                        outside = Color.Lerp(outside, new Color(0.46f, 0.56f, 0.52f), lowMist * 0.52f);
                    }
                    else
                    {
                        // Earth / Dirt path ("흙") widening into foreground with grassy shoulders & pebbles
                        float depthT = SmoothRange(0f, groundH, ny);
                        float pathCenter = 0.67f + Mathf.Sin(depthT * 3.1f) * 0.03f;
                        float pathHalfW = Mathf.Lerp(0.26f, 0.07f, depthT);
                        float dirtNoise = Fbm(nx * 28f, ny * 22f, 15.3f);
                        float pebble = SmoothRange(0.70f, 0.88f, Fbm(nx * 90f, ny * 90f, 21.1f));
                        float pathMask = 1f - SmoothRange(pathHalfW * 0.5f, pathHalfW, Mathf.Abs(nx - pathCenter) + (dirtNoise - 0.5f) * 0.04f);

                        Color grassShoulder = new Color(0.06f, 0.085f, 0.065f) * (0.75f + 0.5f * dirtNoise);
                        Color dirtDark = new Color(0.11f, 0.095f, 0.075f);
                        Color dirtLit = new Color(0.29f, 0.24f, 0.18f);
                        Color dirtCol = Color.Lerp(dirtDark, dirtLit, (0.25f + 0.75f * depthT) * (0.55f + 0.45f * dirtNoise));
                        dirtCol += new Color(0.08f, 0.075f, 0.06f) * (pebble * (0.25f + 0.75f * depthT));

                        outside = Color.Lerp(grassShoulder, dirtCol, pathMask);
                    }

                    // --- FOREGROUND MAZE WALL & PERSPECTIVE CORNER (Left ~38% + bottom-left stone floor lip) ---
                    // Jagged crumbling stone wall edge around x = 0.375
                    float jaggedEdge = 0.378f
                        + (Fbm(0.3f, ny * 15f, 8.8f) - 0.5f) * 0.055f
                        + (Mathf.PerlinNoise(ny * 36f, 3.1f) - 0.5f) * 0.022f;

                    // Diagonal perspective floor/threshold line running from (0.38, 0.22) down-right to (0.60, 0.00)
                    float diagLine = 0.22f - (nx - 0.38f) * 0.95f;
                    bool inLeftWall = nx < jaggedEdge;
                    bool inBottomThreshold = ny < diagLine + (Fbm(nx * 18f, ny * 18f, 5.5f) - 0.5f) * 0.02f;
                    // Bottom-left foreground stone lip keeping bottom-left buttons crisp & grounded
                    bool inBottomLip = ny < 0.18f && nx < 0.44f;

                    Color col;
                    if (inLeftWall || inBottomThreshold || inBottomLip)
                    {
                        // Stone block seams on the dark maze wall
                        float seam = (Mathf.Abs(((ny * 10f) % 1f) - 0.5f) > 0.46f ||
                                      Mathf.Abs((((nx * 8f) + Mathf.Floor(ny * 10f) * 0.5f) % 1f) - 0.5f) > 0.47f)
                            ? 0.65f : 1f;

                        // Moonlight rim-light catching the jagged edge of the maze doorway
                        float distToEdge = Mathf.Min(
                            Mathf.Abs(nx - jaggedEdge),
                            inBottomThreshold ? Mathf.Abs(ny - diagLine) : 1f);
                        float rim = (1f - SmoothRange(0f, 0.045f, distToEdge)) * 0.16f;

                        float wallLight = (0.022f + stoneTex * 0.028f + fineGrain * 0.010f) * seam + rim * (0.6f + 0.4f * fineGrain);
                        // Keep left 35% extra dark & calm for crisp typography readability
                        float leftDarken = SmoothRange(0.05f, 0.36f, nx);
                        wallLight *= Mathf.Lerp(0.55f, 1.0f, leftDarken);

                        col = new Color(wallLight * 0.78f, wallLight * 0.92f, wallLight * 0.90f);
                    }
                    else
                    {
                        col = outside;
                        // Subtle volumetric light mist spilling across the doorway boundary
                        float doorDist = Mathf.Max(0f, nx - jaggedEdge);
                        float spillMist = (1f - SmoothRange(0f, 0.14f, doorDist)) * 0.18f * (0.6f + 0.4f * mist);
                        col += new Color(0.20f, 0.25f, 0.23f) * spillMist;
                    }

                    // Soft dark ink vignette around outer edges
                    float vig = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.58f, 0.50f));
                    float vigMul = 1f - SmoothRange(0.45f, 0.92f, vig);
                    col *= Mathf.Lerp(0.42f, 1f, vigMul);

                    pixels[y * w + x] = ToColor32(col.r, col.g, col.b, 1f);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateLeftInkVeil(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float grain = Fbm(nx * 8f, ny * 8f, 4.4f);
                    float leftWeight = 1f - SmoothRange(0.22f, 0.75f, nx);
                    float alpha = Mathf.Lerp(0.52f, 0.94f, leftWeight) + (grain - 0.5f) * 0.05f;
                    float v = 0.015f + grain * 0.015f;
                    pixels[y * w + x] = ToColor32(v, v * 1.1f, v * 1.1f, Mathf.Clamp01(alpha));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static void PaintSplatterBlob(Color[] pixels, int w, int h, float cx, float cy, float radius,
            Color color, float seed)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((cx - radius * 1.6f) * w));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt((cx + radius * 1.6f) * w));
            int minY = Mathf.Max(0, Mathf.FloorToInt((cy - radius * 1.6f) * h));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt((cy + radius * 1.6f) * h));

            for (int y = minY; y <= maxY; y++)
            {
                float ny = y / (float)h;
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = x / (float)w;
                    float dist = Vector2.Distance(new Vector2(nx, ny), new Vector2(cx, cy));
                    float angle = Mathf.Atan2(ny - cy, nx - cx);
                    float warp = Mathf.Clamp01(Mathf.PerlinNoise(Mathf.Cos(angle) * 1.15f + seed, Mathf.Sin(angle) * 1.15f + seed));
                    float r = radius * (0.72f + warp * 0.48f);
                    if (dist > r) continue;

                    float edge = 1f - SmoothRange(r * 0.38f, r, dist);
                    float a = color.a * edge;
                    int idx = y * w + x;
                    Color dst = pixels[idx];
                    float outA = a + dst.a * (1f - a);
                    if (outA > 0.0001f)
                    {
                        pixels[idx] = new Color(
                            (color.r * a + dst.r * dst.a * (1f - a)) / outA,
                            (color.g * a + dst.g * dst.a * (1f - a)) / outA,
                            (color.b * a + dst.b * dst.a * (1f - a)) / outA,
                            outA);
                    }
                }
            }
        }

        private static float SmoothRange(float edge0, float edge1, float x)
        {
            float denom = edge1 - edge0;
            if (Mathf.Abs(denom) < 0.00001f) return x >= edge0 ? 1f : 0f;
            float t = Mathf.Clamp01((x - edge0) / denom);
            return t * t * (3f - 2f * t);
        }

        private static Color32 ToColor32(float r, float g, float b, float a)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(r) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(g) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(b) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(a) * 255f), 0, 255));
        }

        private static float Fbm(float x, float y, float seed)
        {
            float value = 0f;
            float amp = 0.5f;
            for (int i = 0; i < 4; i++)
            {
                value += amp * Mathf.Clamp01(Mathf.PerlinNoise(x + seed, y + seed * 1.37f));
                x *= 2.03f;
                y *= 2.03f;
                amp *= 0.5f;
            }
            return Mathf.Clamp01(value);
        }

        private static void WritePng(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static Transform FindTransform(GameObject root, string name)
            => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

        private static Text FindText(GameObject root, string name)
            => root.GetComponentsInChildren<Text>(true).Single(t => t.name == name);
    }
}
