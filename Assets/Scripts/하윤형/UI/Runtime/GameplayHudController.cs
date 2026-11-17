using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    /// <summary>
    /// In-game HUD & Pause controller (UI-02).
    /// Zero dependency on gameplay scripts; teammates drive it via public methods or UnityEvents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayHudController : MonoBehaviour
    {
        private static readonly Color Ivory = new Color32(225, 223, 210, 255);
        private static readonly Color Gold = new Color32(184, 160, 120, 255);
        private static readonly Color Crimson = new Color32(186, 62, 62, 255);

        [Header("Scene connection (UI-03)")]
        [SerializeField] private string mainMenuSceneName = "StartMenu";
        [SerializeField] private SceneTransitionManager transitionManager;
        [SerializeField] private GameResultController resultController;
        [SerializeField] private MenuSettingsController settingsController;
        [SerializeField] private DarknessScannerUI darknessScannerUI;

        [Header("Health (UI-02)")]
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private Image healthFillBar;
        [SerializeField] private Image healthTrailBar;
        [SerializeField] private Text healthValueLabel;
        [SerializeField] private Image healthAccentRule;

        [Header("Interaction & objective (UI-02)")]
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private Text promptKeyLabel;
        [SerializeField] private Text promptActionLabel;
        [SerializeField] private GameObject objectiveRoot;
        [SerializeField] private Text objectiveLabel;

        [Header("Pause menu (UI-02)")]
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private CanvasGroup pauseContentGroup;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Text pauseStatusLabel;

        [Header("Behavior")]
        [SerializeField] private bool allowPauseInput = true;
        [SerializeField] private bool pauseTimeScale = true;
        [SerializeField] private bool manageCursorOnOverlay = true;
        [SerializeField] private float hudAlphaDuringOverlay = 0.22f;

        private InputAction tabAction;
        private InputAction escapeAction;
        private float targetHealthRatio = 1f;
        private float trailHealthRatio = 1f;
        private bool promptVisible;
        private bool cursorSaved;
        private CursorLockMode savedLockMode;
        private bool savedCursorVisible;

        public bool IsPaused => pauseOverlay != null && pauseOverlay.activeSelf;
        public bool IsSettingsOpen => settingsController != null && settingsController.IsOpen;
        public bool IsResultOpen => resultController != null && resultController.IsOpen;
        public bool IsAnyOverlayOpen => IsPaused || IsSettingsOpen || IsResultOpen;
        public float CurrentHealth { get; private set; } = 100f;
        public float MaxHealth { get; private set; } = 100f;
        public bool AllowPauseInput
        {
            get => allowPauseInput;
            set => allowPauseInput = value;
        }

        public event Action<bool> PauseChanged;

        private void Awake()
        {
            EnsureEventSystem();

            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (settingsController != null)
                settingsController.Closed += OnSettingsClosed;
            if (resultController != null)
            {
                resultController.MainMenuSceneName = mainMenuSceneName;
                resultController.ResultShown += OnResultShown;
            }

            if (pauseOverlay != null)
                pauseOverlay.SetActive(false);
            if (pauseStatusLabel != null)
                pauseStatusLabel.text = string.Empty;
            if (promptGroup != null)
            {
                promptGroup.alpha = 0f;
                promptGroup.blocksRaycasts = false;
            }

            SetHealthImmediate(CurrentHealth, MaxHealth);

            tabAction = new InputAction("HUD Tab", InputActionType.Button, "<Keyboard>/tab");
            escapeAction = new InputAction("HUD Cancel", InputActionType.Button, "<Keyboard>/escape");
            tabAction.performed += OnTab;
            escapeAction.performed += OnEscape;
        }

        private void OnEnable()
        {
            tabAction?.Enable();
            escapeAction?.Enable();
        }

        private void OnDisable()
        {
            tabAction?.Disable();
            escapeAction?.Disable();
        }

        private void OnDestroy()
        {
            if (settingsController != null)
                settingsController.Closed -= OnSettingsClosed;
            if (resultController != null)
                resultController.ResultShown -= OnResultShown;
            if (IsPaused && pauseTimeScale && Time.timeScale == 0f)
                Time.timeScale = 1f;
            tabAction?.Dispose();
            escapeAction?.Dispose();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (healthTrailBar != null)
            {
                trailHealthRatio = Mathf.MoveTowards(trailHealthRatio, targetHealthRatio, dt * 0.45f);
                SetBarAnchor(healthTrailBar, trailHealthRatio);
            }

            if (promptGroup != null)
            {
                float targetAlpha = promptVisible && !IsAnyOverlayOpen ? 1f : 0f;
                promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, targetAlpha, dt * 8f);
            }
        }

        public void SetHealth(float current, float max)
        {
            float clampedMax = Mathf.Max(1f, max);
            float clampedCurrent = Mathf.Clamp(current, 0f, clampedMax);
            float previous = CurrentHealth;
            CurrentHealth = clampedCurrent;
            MaxHealth = clampedMax;
            targetHealthRatio = clampedCurrent / clampedMax;

            if (clampedCurrent < previous - 0.01f)
            {
                darknessScannerUI?.TriggerDamageFlash();
            }
            else if (clampedCurrent > previous + 0.01f)
            {
                trailHealthRatio = Mathf.Max(trailHealthRatio, targetHealthRatio);
                darknessScannerUI?.TriggerHealFlash();
            }

            ApplyHealthVisuals();
        }

        public void SetHealthImmediate(float current, float max)
        {
            float clampedMax = Mathf.Max(1f, max);
            CurrentHealth = Mathf.Clamp(current, 0f, clampedMax);
            MaxHealth = clampedMax;
            targetHealthRatio = trailHealthRatio = CurrentHealth / MaxHealth;
            SetBarAnchor(healthTrailBar, trailHealthRatio);
            ApplyHealthVisuals();
        }

        public void ShowInteractionPrompt(string actionText, string keyLabel = "E")
        {
            if (promptKeyLabel != null)
                promptKeyLabel.text = string.IsNullOrEmpty(keyLabel) ? "E" : keyLabel;
            if (promptActionLabel != null)
                promptActionLabel.text = actionText ?? string.Empty;
            promptVisible = !string.IsNullOrWhiteSpace(actionText);
        }

        public void HideInteractionPrompt()
        {
            promptVisible = false;
        }

        public void SetObjective(string text)
        {
            if (objectiveLabel != null)
                objectiveLabel.text = text ?? string.Empty;
            SetObjectiveVisible(!string.IsNullOrWhiteSpace(text));
        }

        public void SetObjectiveVisible(bool visible)
        {
            if (objectiveRoot != null)
                objectiveRoot.SetActive(visible);
        }

        public void TogglePause()
        {
            if (IsResultOpen || (transitionManager != null && transitionManager.IsTransitioning))
                return;

            if (IsSettingsOpen)
            {
                settingsController.Cancel();
                return;
            }

            if (IsPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (IsPaused || IsResultOpen || (transitionManager != null && transitionManager.IsTransitioning))
                return;

            SaveAndUnlockCursor();
            if (pauseTimeScale)
                Time.timeScale = 0f;
            if (pauseStatusLabel != null)
                pauseStatusLabel.text = string.Empty;
            if (pauseContentGroup != null)
                pauseContentGroup.interactable = pauseContentGroup.blocksRaycasts = true;
            if (pauseOverlay != null)
                pauseOverlay.SetActive(true);
            if (hudGroup != null)
                hudGroup.alpha = hudAlphaDuringOverlay;
            if (resumeButton != null)
                EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);

            PauseChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            if (IsSettingsOpen)
                settingsController.Cancel();
            if (pauseOverlay != null)
                pauseOverlay.SetActive(false);
            if (pauseStatusLabel != null)
                pauseStatusLabel.text = string.Empty;
            if (hudGroup != null)
                hudGroup.alpha = 1f;
            if (pauseTimeScale)
                Time.timeScale = 1f;

            RestoreSavedCursor();
            EventSystem.current?.SetSelectedGameObject(null);
            PauseChanged?.Invoke(false);
        }

        public void OpenSettings()
        {
            if (!IsPaused || IsSettingsOpen || settingsController == null)
                return;

            if (pauseStatusLabel != null)
                pauseStatusLabel.text = string.Empty;
            if (pauseContentGroup != null)
                pauseContentGroup.interactable = pauseContentGroup.blocksRaycasts = false;
            settingsController.Open();
        }

        public void RestartLevel()
        {
            if (transitionManager != null && transitionManager.IsTransitioning)
                return;

            if (transitionManager != null)
            {
                transitionManager.ReloadCurrentScene("다시 시작하는 중…", ShowPauseNotice);
            }
            else
            {
                ShowPauseNotice("SceneTransitionManager가 연결되지 않았습니다.");
            }
        }

        public void ReturnToMainMenu()
        {
            if (transitionManager != null && transitionManager.IsTransitioning)
                return;

            if (transitionManager != null)
            {
                transitionManager.LoadScene(mainMenuSceneName, "메인 화면으로 돌아가는 중…", ShowPauseNotice);
            }
            else
            {
                ShowPauseNotice("SceneTransitionManager가 연결되지 않았습니다.");
            }
        }

        private void OnSettingsClosed()
        {
            if (IsResultOpen)
                return;

            if (pauseContentGroup != null)
                pauseContentGroup.interactable = pauseContentGroup.blocksRaycasts = true;
            if (IsPaused)
            {
                Button target = settingsButton != null ? settingsButton : resumeButton;
                if (target != null)
                    EventSystem.current?.SetSelectedGameObject(target.gameObject);
            }
        }

        private void OnResultShown(bool victory)
        {
            if (IsSettingsOpen)
                settingsController.Cancel();
            if (pauseOverlay != null)
                pauseOverlay.SetActive(false);
            if (hudGroup != null)
                hudGroup.alpha = hudAlphaDuringOverlay;
            SaveAndUnlockCursor();
        }

        private void OnEscape(InputAction.CallbackContext context)
        {
            if (!allowPauseInput || IsResultOpen || (transitionManager != null && transitionManager.IsTransitioning))
                return;

            if (IsSettingsOpen)
            {
                settingsController.Cancel();
                return;
            }

            TogglePause();
        }

        private void OnTab(InputAction.CallbackContext context)
        {
            if (transitionManager != null && transitionManager.IsTransitioning)
                return;

            bool backwards = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

            if (IsSettingsOpen)
            {
                settingsController.MoveFocus(backwards);
                return;
            }

            if (IsResultOpen)
            {
                resultController.MoveFocus(backwards);
                return;
            }

            if (!IsPaused)
                return;

            Button[] buttons = { resumeButton, settingsButton, restartButton, mainMenuButton };
            GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            int index = Array.FindIndex(buttons, b => b != null && b.gameObject == current);
            for (int step = 1; step <= buttons.Length; step++)
            {
                int next = (index + (backwards ? -step : step) + buttons.Length * 2) % buttons.Length;
                if (buttons[next] != null && buttons[next].IsInteractable() && buttons[next].gameObject.activeInHierarchy)
                {
                    EventSystem.current?.SetSelectedGameObject(buttons[next].gameObject);
                    break;
                }
            }
        }

        private void ApplyHealthVisuals()
        {
            bool lowHealth = targetHealthRatio <= 0.30f;
            if (healthFillBar != null)
            {
                SetBarAnchor(healthFillBar, targetHealthRatio);
                healthFillBar.color = lowHealth ? Crimson : Gold;
            }
            if (healthAccentRule != null)
                healthAccentRule.color = lowHealth ? Crimson : Gold;
            if (healthValueLabel != null)
            {
                healthValueLabel.text = $"{Mathf.CeilToInt(CurrentHealth)} / {Mathf.CeilToInt(MaxHealth)}";
                healthValueLabel.color = lowHealth ? Crimson : Ivory;
            }
            darknessScannerUI?.SetLowHealthRatio(targetHealthRatio);
        }

        private void ShowPauseNotice(string message)
        {
            if (pauseStatusLabel != null)
                pauseStatusLabel.text = message;
        }

        private void SaveAndUnlockCursor()
        {
            if (!manageCursorOnOverlay)
                return;
            if (!cursorSaved)
            {
                savedLockMode = Cursor.lockState;
                savedCursorVisible = Cursor.visible;
                cursorSaved = true;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreSavedCursor()
        {
            if (!manageCursorOnOverlay || !cursorSaved)
                return;
            Cursor.lockState = savedLockMode;
            Cursor.visible = savedCursorVisible;
            cursorSaved = false;
        }

        private static void SetBarAnchor(Image bar, float normalized)
        {
            if (bar == null)
                return;
            RectTransform rt = bar.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(normalized), rt.anchorMax.y);
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
                return;

            var events = new GameObject("HUD EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
