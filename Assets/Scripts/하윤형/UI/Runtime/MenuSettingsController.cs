using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    /// <summary>Draft/apply/cancel settings, with a timed rollback for standalone display changes.</summary>
    public sealed class MenuSettingsController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button defaultsButton;
        [SerializeField] private Text statusLabel;

        [Header("Display")]
        [SerializeField] private Button previousResolution;
        [SerializeField] private Button nextResolution;
        [SerializeField] private Text resolutionLabel;
        [SerializeField] private Button windowModeButton;
        [SerializeField] private Text windowModeLabel;

        [Header("Audio and controls")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeLabel;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityLabel;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private MenuAtmosphere atmosphere;

        [Header("Display safety confirmation")]
        [SerializeField] private GameObject confirmation;
        [SerializeField] private Text countdownLabel;
        [SerializeField] private Button keepDisplayButton;
        [SerializeField] private Button revertDisplayButton;

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private int resolutionIndex;
        private bool draftFullscreen;
        private float confirmDeadline;
        private int oldWidth;
        private int oldHeight;
        private FullScreenMode oldMode;
        private bool initialized;

        public event Action Closed;
        public bool IsOpen => panel != null && panel.activeSelf;
        public bool IsConfirmingDisplay => confirmation != null && confirmation.activeSelf;

        private void Awake()
        {
            closeButton.onClick.AddListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
            applyButton.onClick.AddListener(Apply);
            defaultsButton.onClick.AddListener(RestoreDefaults);
            previousResolution.onClick.AddListener(() => CycleResolution(-1));
            nextResolution.onClick.AddListener(() => CycleResolution(1));
            windowModeButton.onClick.AddListener(() => { draftFullscreen = !draftFullscreen; RefreshLabels(); });
            volumeSlider.onValueChanged.AddListener(_ => RefreshLabels());
            sensitivitySlider.onValueChanged.AddListener(_ => RefreshLabels());
            keepDisplayButton.onClick.AddListener(KeepDisplay);
            revertDisplayButton.onClick.AddListener(RevertDisplay);
            confirmation.SetActive(false);
            panel.SetActive(false);
            initialized = true;
            ApplyRuntimePreferences();
#if UNITY_STANDALONE && !UNITY_EDITOR
            if (MenuPreferences.HasDisplayPreference)
                Screen.SetResolution(MenuPreferences.Width, MenuPreferences.Height,
                    MenuPreferences.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
        }

        private void Update()
        {
            if (!IsConfirmingDisplay) return;
            float remaining = confirmDeadline - Time.realtimeSinceStartup;
            countdownLabel.text = "화면이 잘 보이나요?\n" + Mathf.Max(0, Mathf.CeilToInt(remaining)) + "초 뒤 이전 화면으로 돌아갑니다.";
            if (remaining <= 0f) RevertDisplay();
        }

        private void OnDisable()
        {
            // Never strand the player in an unconfirmed mode when leaving/unloading this menu.
            if (initialized && IsConfirmingDisplay)
                RevertDisplay();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && IsConfirmingDisplay) RevertDisplay();
        }

        public void Open()
        {
            if (IsOpen) return;
            resolutions.Clear();
            resolutions.AddRange(Screen.resolutions.Select(r => new Vector2Int(r.width, r.height))
                .Where(r => r.x >= 640 && r.y >= 480).Distinct().OrderByDescending(r => r.x).ThenByDescending(r => r.y));
            Vector2Int requested = MenuPreferences.HasDisplayPreference
                ? new Vector2Int(MenuPreferences.Width, MenuPreferences.Height)
                : new Vector2Int(Mathf.Max(640, Screen.width), Mathf.Max(480, Screen.height));
            if (!resolutions.Contains(requested)) resolutions.Add(requested);
            resolutionIndex = resolutions.IndexOf(requested);
            draftFullscreen = MenuPreferences.Fullscreen;
            volumeSlider.SetValueWithoutNotify(MenuPreferences.MasterVolume);
            sensitivitySlider.SetValueWithoutNotify(MenuPreferences.MouseSensitivity);
            invertYToggle.SetIsOnWithoutNotify(MenuPreferences.InvertY);
            reducedMotionToggle.SetIsOnWithoutNotify(MenuPreferences.ReducedMenuMotion);
            confirmation.SetActive(false);
            contentGroup.interactable = contentGroup.blocksRaycasts = true;
            panel.SetActive(true);
#if UNITY_EDITOR
            statusLabel.text = "해상도·창 모드는 빌드에서 적용됩니다. Editor에서는 선택값만 저장합니다.";
#elif UNITY_STANDALONE
            statusLabel.text = "화면 변경 후 15초 안에 확인하지 않으면 이전 화면으로 복구됩니다.";
#else
            statusLabel.text = "이 기기에서는 해상도·창 모드 변경을 지원하지 않습니다.";
            previousResolution.interactable = nextResolution.interactable = windowModeButton.interactable = false;
#endif
            RefreshLabels();
            Select(volumeSlider);
        }

        public void Cancel()
        {
            if (!IsOpen) return;
            if (IsConfirmingDisplay) { RevertDisplay(); return; }
            Close(); // No draft values were applied or persisted.
        }

        public void Apply()
        {
            if (!IsOpen || IsConfirmingDisplay) return;
            oldWidth = Screen.width;
            oldHeight = Screen.height;
            oldMode = Screen.fullScreenMode;
            confirmDeadline = Time.realtimeSinceStartup + 15f;
#if UNITY_STANDALONE && !UNITY_EDITOR
            Vector2Int size = resolutions[resolutionIndex];
            FullScreenMode mode = draftFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (size.x != Screen.width || size.y != Screen.height || mode != Screen.fullScreenMode)
            {
                confirmation.SetActive(true);
                contentGroup.interactable = contentGroup.blocksRaycasts = false;
                Screen.SetResolution(size.x, size.y, mode);
                Select(keepDisplayButton);
                return;
            }
#endif
            SaveAndClose();
        }

        public void RestoreDefaults()
        {
            if (!IsOpen || IsConfirmingDisplay) return;
            volumeSlider.value = MenuPreferences.DefaultVolume;
            sensitivitySlider.value = 1f;
            invertYToggle.isOn = reducedMotionToggle.isOn = false;
            // Keep the display draft: reset must never surprise the user with a monitor change.
            statusLabel.text = "음량·조작·움직임을 기본값으로 선택했습니다. ‘적용’ 전에는 저장되지 않습니다.";
            RefreshLabels();
        }

        public void MoveFocus(bool backwards)
        {
            Selectable[] controls = IsConfirmingDisplay
                ? new Selectable[] { keepDisplayButton, revertDisplayButton }
                : new Selectable[] { previousResolution, nextResolution, windowModeButton, volumeSlider,
                    sensitivitySlider, invertYToggle, reducedMotionToggle, defaultsButton, applyButton, cancelButton, closeButton };
            controls = controls.Where(control => control.isActiveAndEnabled && control.IsInteractable()).ToArray();
            if (controls.Length == 0) return;
            GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            int index = Array.FindIndex(controls, control => control.gameObject == current);
            Select(controls[(index + (backwards ? -1 : 1) + controls.Length) % controls.Length]);
        }

        private void CycleResolution(int direction)
        {
            resolutionIndex = (resolutionIndex + direction + resolutions.Count) % resolutions.Count;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (resolutions.Count > 0)
            {
                Vector2Int size = resolutions[resolutionIndex];
                resolutionLabel.text = size.x + " × " + size.y;
            }
            windowModeLabel.text = draftFullscreen ? "전체 화면 (테두리 없음)" : "창 모드";
            volumeLabel.text = Mathf.RoundToInt(volumeSlider.value * 100f) + "%";
            sensitivityLabel.text = sensitivitySlider.value.ToString("0.0") + "×";
        }

        private void KeepDisplay()
        {
            if (!IsConfirmingDisplay) return;
            confirmation.SetActive(false);
            SaveAndClose();
        }

        private void RevertDisplay()
        {
            if (!IsConfirmingDisplay) return;
#if UNITY_STANDALONE && !UNITY_EDITOR
            Screen.SetResolution(oldWidth, oldHeight, oldMode);
#endif
            confirmation.SetActive(false);
            contentGroup.interactable = contentGroup.blocksRaycasts = true;
            var oldSize = new Vector2Int(oldWidth, oldHeight);
            if (oldWidth > 0)
            {
                if (!resolutions.Contains(oldSize)) resolutions.Add(oldSize);
                resolutionIndex = resolutions.IndexOf(oldSize);
            }
            draftFullscreen = oldMode != FullScreenMode.Windowed;
            statusLabel.text = "화면 변경을 취소했습니다. 아직 어떤 설정도 저장하지 않았습니다.";
            RefreshLabels();
            Select(applyButton);
        }

        private void SaveAndClose()
        {
            Vector2Int size = resolutions[resolutionIndex];
            MenuPreferences.Save(volumeSlider.value, sensitivitySlider.value, invertYToggle.isOn,
                reducedMotionToggle.isOn, size.x, size.y, draftFullscreen);
            ApplyRuntimePreferences();
            Close();
        }

        private void ApplyRuntimePreferences()
        {
            AudioListener.volume = MenuPreferences.MasterVolume;
            if (atmosphere != null) atmosphere.SetReducedMotion(MenuPreferences.ReducedMenuMotion);
        }

        private void Close()
        {
            panel.SetActive(false);
            Closed?.Invoke();
        }

        private static void Select(Selectable control) => EventSystem.current?.SetSelectedGameObject(control.gameObject);
    }
}
