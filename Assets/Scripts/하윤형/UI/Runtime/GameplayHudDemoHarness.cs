using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkMaze.UI
{
    /// <summary>
    /// Personal test-scene harness only (not included in GameplayHUD.prefab).
    /// Lets UI developers preview every HUD, scanner, blackout, and result state without gameplay scripts.
    /// </summary>
    public sealed class GameplayHudDemoHarness : MonoBehaviour
    {
        [SerializeField] private GameplayHudController hud;
        [SerializeField] private DarknessScannerUI scanner;
        [SerializeField] private GameResultController result;

        private float health = 100f;
        private const float MaxHealth = 100f;
        private bool promptShown;
        private int darknessStep;
        private bool extraSlotsVisible;
        private int healCount = 2;
        private int puzzleStep;
        private float scannerTimer;
        private ScannerState scannerMode = ScannerState.Ready;
        private float elapsedSeconds;
        private const float ActiveDuration = 4f;
        private const float CooldownDuration = 6f;

        private void Start()
        {
            hud?.SetHealthImmediate(health, MaxHealth);
            hud?.SetObjective("탈출 단서 0 / 3 — 미로의 장치를 해제하세요");
            scanner?.SetDarknessLevel(0.25f);
            scanner?.SetScannerState(ScannerState.Ready, 1f, 0f);
            scanner?.SetLegendVisible(false);
            scanner?.SetHealItemVisible(false);
            scanner?.SetSubGearVisible(false);
        }

        private void Update()
        {
            if (hud != null && !hud.IsAnyOverlayOpen)
                elapsedSeconds += Time.unscaledDeltaTime;

            UpdateScannerCycle();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || (hud != null && (hud.IsSettingsOpen || hud.IsPaused)))
                return;

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                health = Mathf.Max(0f, health - 20f);
                hud?.SetHealth(health, MaxHealth);
                if (health <= 0f)
                    result?.ShowGameOver("체력이 모두 소진되었습니다. (테스트)", elapsedSeconds, puzzleStep, 3);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                if (extraSlotsVisible && healCount > 0 && health < MaxHealth)
                {
                    healCount--;
                    scanner?.SetHealItem(healCount, true);
                }
                health = Mathf.Min(MaxHealth, health + 20f);
                hud?.SetHealth(health, MaxHealth);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                promptShown = !promptShown;
                if (promptShown)
                    hud?.ShowInteractionPrompt("고대 장치 작동하기", "E");
                else
                    hud?.HideInteractionPrompt();
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                if (scannerMode == ScannerState.Ready)
                {
                    scannerMode = ScannerState.Active;
                    scannerTimer = ActiveDuration;
                    scanner?.TriggerScannerPulse();
                }
                else if (scannerMode == ScannerState.Active)
                {
                    scannerMode = ScannerState.Cooldown;
                    scannerTimer = CooldownDuration;
                }
                else
                {
                    scannerMode = ScannerState.Ready;
                    scannerTimer = 0f;
                    scanner?.SetScannerState(ScannerState.Ready, 1f, 0f);
                }
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                darknessStep = (darknessStep + 1) % 3;
                float level = darknessStep == 0 ? 0.2f : (darknessStep == 1 ? 0.55f : 0.9f);
                scanner?.SetDarknessLevel(level);
            }
            else if (keyboard.digit6Key.wasPressedThisFrame)
            {
                extraSlotsVisible = !extraSlotsVisible;
                scanner?.SetLegendVisible(extraSlotsVisible);
                scanner?.SetHealItem(healCount, extraSlotsVisible);
                scanner?.SetSubGear("광역 탐지기", extraSlotsVisible ? ScannerState.Ready : ScannerState.Disabled,
                    1f, 0f, extraSlotsVisible);
            }
            else if (keyboard.digit7Key.wasPressedThisFrame)
            {
                result?.ShowGameOver("어둠 속 추적자에게 붙잡혔습니다. (테스트)", elapsedSeconds, puzzleStep, 3);
            }
            else if (keyboard.digit8Key.wasPressedThisFrame)
            {
                result?.ShowGameClear(elapsedSeconds, health / MaxHealth, puzzleStep, 3);
            }
            else if (keyboard.digit9Key.wasPressedThisFrame)
            {
                puzzleStep = (puzzleStep + 1) % 4;
                hud?.SetObjective(puzzleStep >= 3
                    ? "모든 장치를 해제했습니다 — 출구로 이동하세요"
                    : $"탈출 단서 {puzzleStep} / 3 — 미로의 장치를 해제하세요");
            }
        }

        private void UpdateScannerCycle()
        {
            if (scanner == null || (hud != null && hud.IsAnyOverlayOpen))
                return;

            if (scannerMode == ScannerState.Active)
            {
                scannerTimer -= Time.unscaledDeltaTime;
                if (scannerTimer <= 0f)
                {
                    scannerMode = ScannerState.Cooldown;
                    scannerTimer = CooldownDuration;
                }
                else
                {
                    scanner.SetScannerState(ScannerState.Active, scannerTimer / ActiveDuration, scannerTimer);
                }
            }
            else if (scannerMode == ScannerState.Cooldown)
            {
                scannerTimer -= Time.unscaledDeltaTime;
                if (scannerTimer <= 0f)
                {
                    scannerMode = ScannerState.Ready;
                    scannerTimer = 0f;
                    scanner.SetScannerState(ScannerState.Ready, 1f, 0f);
                }
                else
                {
                    scanner.SetScannerState(ScannerState.Cooldown, 1f - scannerTimer / CooldownDuration, scannerTimer);
                }
            }
        }
    }
}
