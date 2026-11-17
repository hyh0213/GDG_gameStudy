using UnityEngine;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    public enum ScannerState
    {
        Ready,
        Active,
        Cooldown,
        Disabled
    }

    /// <summary>
    /// Darkness vignette, exploration gear status/cooldown (UI-04),
    /// and optional heal/secondary gear slots (UI-06). Presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DarknessScannerUI : MonoBehaviour
    {
        private static readonly Color Gold = new Color32(184, 160, 120, 255);
        private static readonly Color Jade = new Color32(111, 168, 155, 255);
        private static readonly Color Amber = new Color32(148, 122, 82, 255);
        private static readonly Color Muted = new Color32(108, 118, 114, 255);
        private static readonly Color Crimson = new Color32(178, 56, 56, 255);

        [Header("Screen overlays (UI-04 / UI-02)")]
        [SerializeField] private CanvasGroup darknessOverlay;
        [SerializeField] private Image damageFlashOverlay;
        [SerializeField] private Image scannerPulseOverlay;
        [SerializeField] private float maxDarknessAlpha = 0.82f;

        [Header("Primary exploration gear (UI-04)")]
        [SerializeField] private GameObject scannerRoot;
        [SerializeField] private Text scannerKeyLabel;
        [SerializeField] private Text scannerTitleLabel;
        [SerializeField] private Text scannerStatusLabel;
        [SerializeField] private Image scannerFillBar;
        [SerializeField] private Image scannerAccentRule;
        [SerializeField] private GameObject legendRoot;
        [SerializeField] private Image legendTerrainSwatch;
        [SerializeField] private Image legendPuzzleSwatch;
        [SerializeField] private Image legendHazardSwatch;

        [Header("Recovery item slot (UI-06, optional)")]
        [SerializeField] private GameObject healSlotRoot;
        [SerializeField] private Text healKeyLabel;
        [SerializeField] private Text healNameLabel;
        [SerializeField] private Text healCountLabel;
        [SerializeField] private Image healAccentRule;

        [Header("Secondary gear slot (UI-06, optional)")]
        [SerializeField] private GameObject subGearRoot;
        [SerializeField] private Text subGearKeyLabel;
        [SerializeField] private Text subGearNameLabel;
        [SerializeField] private Text subGearStatusLabel;
        [SerializeField] private Image subGearFillBar;
        [SerializeField] private Image subGearAccentRule;

        private float targetDarkness;
        private float currentDarkness;
        private float damageFlashAlpha;
        private float healFlashAlpha;
        private float scannerPulseAlpha;
        private float lowHealthFactor;

        public ScannerState CurrentScannerState { get; private set; } = ScannerState.Ready;
        public float DarknessLevel => targetDarkness;

        private void Awake()
        {
            SetOverlayAlpha(damageFlashOverlay, 0f);
            SetOverlayAlpha(scannerPulseOverlay, 0f);
            if (darknessOverlay != null)
                darknessOverlay.alpha = 0f;
            SetScannerState(ScannerState.Ready, 1f, 0f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (darknessOverlay != null)
            {
                currentDarkness = Mathf.MoveTowards(currentDarkness, targetDarkness, dt * 2.5f);
                darknessOverlay.alpha = currentDarkness * maxDarknessAlpha;
            }

            if (damageFlashOverlay != null)
            {
                damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, dt * 1.9f);
                healFlashAlpha = Mathf.MoveTowards(healFlashAlpha, 0f, dt * 2.2f);
                float pulse = lowHealthFactor > 0f
                    ? lowHealthFactor * (0.2f + 0.14f * Mathf.Sin(Time.unscaledTime * 4.2f))
                    : 0f;
                if (healFlashAlpha > damageFlashAlpha && healFlashAlpha > pulse)
                {
                    damageFlashOverlay.color = new Color(Jade.r, Jade.g, Jade.b, healFlashAlpha * 0.42f);
                }
                else
                {
                    float alpha = Mathf.Clamp01(Mathf.Max(damageFlashAlpha, pulse));
                    damageFlashOverlay.color = new Color(Crimson.r, Crimson.g, Crimson.b, alpha * 0.58f);
                }
            }

            if (scannerPulseOverlay != null)
            {
                scannerPulseAlpha = Mathf.MoveTowards(scannerPulseAlpha, 0f, dt * 2.1f);
                scannerPulseOverlay.color = new Color(Jade.r, Jade.g, Jade.b, scannerPulseAlpha * 0.35f);
            }
        }

        public void SetDarknessLevel(float normalized)
        {
            targetDarkness = Mathf.Clamp01(normalized);
        }

        public void SetLowHealthRatio(float healthRatio)
        {
            float clamped = Mathf.Clamp01(healthRatio);
            lowHealthFactor = clamped < 0.32f ? Mathf.InverseLerp(0.32f, 0f, clamped) : 0f;
        }

        public void TriggerDamageFlash(float intensity = 0.7f)
        {
            damageFlashAlpha = Mathf.Max(damageFlashAlpha, Mathf.Clamp01(intensity));
        }

        public void TriggerHealFlash(float intensity = 0.5f)
        {
            healFlashAlpha = Mathf.Max(healFlashAlpha, Mathf.Clamp01(intensity));
        }

        public void TriggerScannerPulse(float intensity = 0.55f)
        {
            scannerPulseAlpha = Mathf.Max(scannerPulseAlpha, Mathf.Clamp01(intensity));
        }

        public void SetScannerVisible(bool visible)
        {
            if (scannerRoot != null)
                scannerRoot.SetActive(visible);
        }

        public void SetScannerState(ScannerState state, float progress01 = 1f, float remainingSeconds = 0f, string keyLabel = "F")
        {
            CurrentScannerState = state;
            if (scannerKeyLabel != null && !string.IsNullOrEmpty(keyLabel))
                scannerKeyLabel.text = keyLabel;

            float fill = Mathf.Clamp01(progress01);
            Color accent;
            string status;
            switch (state)
            {
                case ScannerState.Active:
                    accent = Jade;
                    status = remainingSeconds > 0.05f ? $"작동 중 · {remainingSeconds:0.0}s" : "작동 중";
                    break;
                case ScannerState.Cooldown:
                    accent = Amber;
                    status = remainingSeconds > 0.05f ? $"재충전 중 · {remainingSeconds:0.0}s" : "재충전 중";
                    break;
                case ScannerState.Disabled:
                    accent = Muted;
                    fill = 0f;
                    status = "사용 불가";
                    break;
                default:
                    accent = Gold;
                    fill = 1f;
                    status = "준비됨";
                    break;
            }

            if (scannerStatusLabel != null)
            {
                scannerStatusLabel.text = status;
                scannerStatusLabel.color = accent;
            }
            if (scannerAccentRule != null)
                scannerAccentRule.color = accent;
            SetFillBar(scannerFillBar, fill, accent);
        }

        public void SetScannerTitle(string title)
        {
            if (scannerTitleLabel != null && !string.IsNullOrWhiteSpace(title))
                scannerTitleLabel.text = title;
        }

        public void SetLegendVisible(bool visible)
        {
            if (legendRoot != null)
                legendRoot.SetActive(visible);
        }

        public void SetLegendColors(Color terrain, Color puzzle, Color hazard)
        {
            if (legendTerrainSwatch != null) legendTerrainSwatch.color = terrain;
            if (legendPuzzleSwatch != null) legendPuzzleSwatch.color = puzzle;
            if (legendHazardSwatch != null) legendHazardSwatch.color = hazard;
        }

        public void SetHealItemVisible(bool visible)
        {
            if (healSlotRoot != null)
                healSlotRoot.SetActive(visible);
        }

        public void SetHealItem(int count, bool visible = true, string itemName = "회복 아이템", string keyLabel = "Q")
        {
            SetHealItemVisible(visible);
            int clamped = Mathf.Max(0, count);
            if (healKeyLabel != null && !string.IsNullOrEmpty(keyLabel))
                healKeyLabel.text = keyLabel;
            if (healNameLabel != null && !string.IsNullOrWhiteSpace(itemName))
                healNameLabel.text = itemName;
            if (healCountLabel != null)
            {
                healCountLabel.text = $"×{clamped}";
                healCountLabel.color = clamped > 0 ? Gold : Muted;
            }
            if (healAccentRule != null)
                healAccentRule.color = clamped > 0 ? Jade : Muted;
        }

        public void SetSubGearVisible(bool visible)
        {
            if (subGearRoot != null)
                subGearRoot.SetActive(visible);
        }

        public void SetSubGear(string gearName, ScannerState state, float progress01 = 1f, float remainingSeconds = 0f,
            bool visible = true, string keyLabel = "G")
        {
            SetSubGearVisible(visible);
            if (subGearKeyLabel != null && !string.IsNullOrEmpty(keyLabel))
                subGearKeyLabel.text = keyLabel;
            if (subGearNameLabel != null && !string.IsNullOrWhiteSpace(gearName))
                subGearNameLabel.text = gearName;

            float fill = Mathf.Clamp01(progress01);
            Color accent;
            string status;
            switch (state)
            {
                case ScannerState.Active:
                    accent = Jade;
                    status = remainingSeconds > 0.05f ? $"작동 중 · {remainingSeconds:0.0}s" : "작동 중";
                    break;
                case ScannerState.Cooldown:
                    accent = Amber;
                    status = remainingSeconds > 0.05f ? $"재충전 · {remainingSeconds:0.0}s" : "재충전 중";
                    break;
                case ScannerState.Disabled:
                    accent = Muted;
                    fill = 0f;
                    status = "잠김";
                    break;
                default:
                    accent = Gold;
                    fill = 1f;
                    status = "준비됨";
                    break;
            }

            if (subGearStatusLabel != null)
            {
                subGearStatusLabel.text = status;
                subGearStatusLabel.color = accent;
            }
            if (subGearAccentRule != null)
                subGearAccentRule.color = accent;
            SetFillBar(subGearFillBar, fill, accent);
        }

        private static void SetFillBar(Image bar, float normalized, Color color)
        {
            if (bar == null)
                return;
            bar.color = color;
            RectTransform rt = bar.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(normalized), rt.anchorMax.y);
            rt.offsetMax = Vector2.zero;
        }

        private static void SetOverlayAlpha(Image image, float alpha)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
