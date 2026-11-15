using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yeeun.Sonar;

namespace Yeeun.GasMask
{
    /// <summary>
    /// First-person gas mask view, built at runtime (no art assets needed yet):
    /// - lens frame that hides the top, bottom and corners, with a soft edge
    /// - three pulse charge lights on the bottom rim (the refilling one fills up)
    /// - lens cracks that grow with damage (no health number or bar)
    /// The canvas is in Screen Space - Camera mode so the camera's post-processing
    /// (lens distortion / vignette) bends the frame like a real visor.
    /// </summary>
    public class GasMaskHUD : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] SonarDevice device;
        [SerializeField] PlayerHealth health;

        [Header("Lens shape (screen = -1..1)")]
        [SerializeField] float lensWidth = 1.08f;
        [SerializeField] float lensHeight = 0.8f;
        [Tooltip("Higher = squarer lens.")]
        [SerializeField] float lensPower = 3f;
        [Tooltip("Width of the soft, darkening edge inside the frame.")]
        [SerializeField, Range(0.02f, 0.6f)] float edgeSoftness = 0.28f;
        [SerializeField, Range(0f, 1f)] float rimHighlight = 0.18f;

        [Header("Charge lights")]
        [SerializeField] Color lightOn = new Color(0.7f, 0.95f, 1f, 1f);
        [SerializeField] Color lightCharging = new Color(0.35f, 0.55f, 0.65f, 1f);
        [SerializeField] Color lightOff = new Color(0.35f, 0.35f, 0.35f, 0.35f);
        [SerializeField] float lightSize = 24f;
        [SerializeField] float lightSpacing = 44f;
        [SerializeField] float lightBottom = 52f;

        [Header("Cracks")]
        [Tooltip("Number of crack marks at zero health.")]
        [SerializeField, Min(1)] int maxCracks = 6;
        [Tooltip("Kept dimmer and greyer than sonar lines so the two do not get confused.")]
        [SerializeField] Color crackColor = new Color(0.6f, 0.65f, 0.72f, 0.5f);

        [Header("Help")]
        [SerializeField] bool showHelp = true;
        [SerializeField, TextArea] string helpText = "F 장비 켜기/끄기   Q 음파   E 상호작용   H/J 피해/회복 (테스트)";

        public int CrackCount => crackSeeds.Count;

        Canvas canvas;
        RawImage maskImage, crackImage;
        readonly List<Image> lightRings = new List<Image>();
        readonly List<Image> lightFills = new List<Image>();
        Text statusText;
        Texture2D maskTex, crackTex;
        Vector2Int maskScreen;
        readonly List<int> crackSeeds = new List<int>();
        int crackSeedCounter = 1234;

        void Start()
        {
            Build();
            RebuildMask();
            if (health != null)
            {
                health.Changed += OnHealthChanged;
                SyncCracks();
            }
        }

        void OnDestroy()
        {
            if (health != null) health.Changed -= OnHealthChanged;
            if (maskTex != null) Destroy(maskTex);
            if (crackTex != null) Destroy(crackTex);
        }

        void Update()
        {
            if (Screen.width != maskScreen.x || Screen.height != maskScreen.y) RebuildMask();
            UpdateLights();
        }

        // ------------------------------------------------------------------ build

        void Build()
        {
            var canvasGo = new GameObject("GasMaskCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            if (viewCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = viewCamera;
                canvas.planeDistance = viewCamera.nearClipPlane + 0.05f;
            }
            else canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            crackImage = FullScreenRaw("LensCracks");
            crackImage.enabled = false; // no texture yet: an empty RawImage would draw solid white
            maskImage = FullScreenRaw("LensFrame");

            int count = device != null ? device.MaxCharges : 3;
            var disc = MakeCircleSprite(64, 0f);
            var ring = MakeCircleSprite(64, 0.2f);
            for (int i = 0; i < count; i++)
            {
                var ringImg = UIObject($"ChargeLight_{i + 1}").AddComponent<Image>();
                ringImg.sprite = ring;
                ringImg.raycastTarget = false;
                var rt = ringImg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.sizeDelta = Vector2.one * lightSize;
                rt.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * lightSpacing, lightBottom);

                var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
                fill.transform.SetParent(rt, false);
                fill.sprite = disc;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.raycastTarget = false;
                var frt = fill.rectTransform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.one * 5f;
                frt.offsetMax = -Vector2.one * 5f;
                lightRings.Add(ringImg);
                lightFills.Add(fill);
            }

            statusText = MakeText("DeviceStatus", 18, TextAnchor.MiddleCenter);
            var srt = statusText.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(400, 24);
            srt.anchoredPosition = new Vector2(0, lightBottom - 30f);

            if (showHelp)
            {
                var help = MakeText("Help", 20, TextAnchor.MiddleCenter);
                help.text = helpText;
                help.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
                var hrt = help.rectTransform;
                hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 1f);
                hrt.sizeDelta = new Vector2(1400, 30);
                hrt.anchoredPosition = new Vector2(0, -40);
            }
        }

        GameObject UIObject(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            return go;
        }

        RawImage FullScreenRaw(string name)
        {
            var img = UIObject(name).AddComponent<RawImage>();
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        Text MakeText(string name, int size, TextAnchor anchor)
        {
            var t = UIObject(name).AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            t.color = new Color(0.75f, 0.85f, 0.9f, 0.9f);
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        static Sprite MakeCircleSprite(int size, float ringThickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * size * 0.5f);
                float outer = Mathf.Clamp01(r - d + 0.5f);
                float inner = ringThickness > 0f ? Mathf.Clamp01(d - r * (1f - ringThickness) + 0.5f) : 1f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * outer * inner));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
        }

        // ------------------------------------------------------------------ lens frame

        void RebuildMask()
        {
            maskScreen = new Vector2Int(Screen.width, Screen.height);
            int w = 512;
            int h = Mathf.Clamp(Mathf.RoundToInt(w * (float)Screen.height / Mathf.Max(1, Screen.width)), 64, 1024);
            if (maskTex == null || maskTex.width != w || maskTex.height != h)
            {
                if (maskTex != null) Destroy(maskTex);
                maskTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            }

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f;
                    float s = Mathf.Pow(Mathf.Abs(u) / lensWidth, lensPower) + Mathf.Pow(Mathf.Abs(v) / lensHeight, lensPower);
                    float frame = Smooth(1f - edgeSoftness, 1f, s);
                    float rim = Mathf.Exp(-Mathf.Pow((s - 1.0f) / 0.03f, 2f)) * rimHighlight;
                    float alpha = Mathf.Max(frame, rim);
                    byte grey = (byte)(255f * rim * 0.6f);
                    px[y * w + x] = new Color32(grey, grey, grey, (byte)(255f * Mathf.Clamp01(alpha)));
                }
            }
            maskTex.SetPixels32(px);
            maskTex.Apply();
            maskImage.texture = maskTex;
        }

        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ charge lights

        void UpdateLights()
        {
            if (device == null) return;
            bool on = device.IsOn;
            for (int i = 0; i < lightFills.Count; i++)
            {
                float fill;
                Color color;
                if (i < device.Charges) { fill = 1f; color = on ? lightOn : lightOff; }
                else if (i == device.Charges) { fill = device.RechargeProgress; color = on ? lightCharging : lightOff; }
                else { fill = 0f; color = lightOff; }
                lightFills[i].fillAmount = fill;
                lightFills[i].color = color;
                lightRings[i].color = on ? new Color(lightOn.r, lightOn.g, lightOn.b, 0.45f) : lightOff;
            }
            statusText.text = on ? "SONAR" : "SONAR OFF";
            statusText.color = on ? new Color(0.7f, 0.9f, 1f, 0.7f) : new Color(0.5f, 0.5f, 0.5f, 0.6f);
        }

        // ------------------------------------------------------------------ cracks

        void OnHealthChanged(PlayerHealth h, int change) => SyncCracks();

        void SyncCracks()
        {
            int wanted = health == null ? 0 : Mathf.CeilToInt(health.Damage01 * maxCracks - 0.0001f);
            wanted = Mathf.Clamp(wanted, 0, maxCracks);
            if (wanted == crackSeeds.Count) return;
            while (crackSeeds.Count < wanted) crackSeeds.Add(crackSeedCounter++);
            while (crackSeeds.Count > wanted) crackSeeds.RemoveAt(crackSeeds.Count - 1);
            RedrawCracks();
        }

        void RedrawCracks()
        {
            const int W = 960, H = 540;
            if (crackTex == null)
                crackTex = new Texture2D(W, H, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            foreach (int seed in crackSeeds) DrawCrack(px, W, H, seed);
            crackTex.SetPixels32(px);
            crackTex.Apply();
            crackImage.texture = crackTex;
            crackImage.enabled = crackSeeds.Count > 0;
        }

        void DrawCrack(Color32[] px, int w, int h, int seed)
        {
            var rng = new System.Random(seed);
            float R() => (float)rng.NextDouble();
            var center = new Vector2(w * (0.18f + 0.64f * R()), h * (0.25f + 0.5f * R()));

            // Small impact ring.
            int ringPts = 7;
            float ringR = 5f + 5f * R();
            for (int i = 0; i < ringPts; i++)
            {
                float a0 = i * Mathf.PI * 2f / ringPts, a1 = (i + 1) * Mathf.PI * 2f / ringPts;
                Line(px, w, h, center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * ringR,
                    center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * ringR, 1f);
            }

            int rays = 5 + rng.Next(4);
            for (int r = 0; r < rays; r++)
            {
                float angle = r * Mathf.PI * 2f / rays + (R() - 0.5f) * 0.6f;
                Vector2 p = center;
                int segments = 3 + rng.Next(5);
                for (int s = 0; s < segments; s++)
                {
                    angle += (R() - 0.5f) * 0.7f;
                    float len = 12f + 26f * R();
                    Vector2 next = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * len;
                    float fade = 1f - (float)s / segments * 0.6f;
                    Line(px, w, h, p, next, fade);
                    if (R() < 0.3f)
                    {
                        float b = angle + (R() < 0.5f ? 0.8f : -0.8f);
                        Line(px, w, h, next, next + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * len * 0.7f, fade * 0.7f);
                    }
                    p = next;
                }
            }
        }

        void Line(Color32[] px, int w, int h, Vector2 a, Vector2 b, float strength)
        {
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)));
            byte alpha = (byte)(255f * crackColor.a * Mathf.Clamp01(strength));
            var c = new Color32((byte)(255 * crackColor.r), (byte)(255 * crackColor.g), (byte)(255 * crackColor.b), alpha);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, steps == 0 ? 0f : (float)i / steps);
                int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
                Plot(px, w, h, x, y, c);
                Plot(px, w, h, x + 1, y, c);
            }
        }

        static void Plot(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            if (px[i].a < c.a) px[i] = c;
        }
    }
}
