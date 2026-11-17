using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    /// <summary>
    /// Game Over & Game Clear screen controller (UI-05).
    /// Reusable standalone or paired with GameplayHudController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameResultController : MonoBehaviour
    {
        private static readonly Color Gold = new Color32(184, 160, 120, 255);
        private static readonly Color Crimson = new Color32(178, 56, 56, 255);

        [Header("Scene connection (UI-03)")]
        [SerializeField] private string mainMenuSceneName = "StartMenu";
        [SerializeField] private SceneTransitionManager transitionManager;
        [SerializeField] private bool pauseTimeOnResult = true;
        [SerializeField] private bool unlockCursorOnResult = true;

        [Header("Result overlay")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup blackoutGroup;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private RectTransform resultCardRect;
        [SerializeField] private GameObject clearArtworkRoot;
        [SerializeField] private CanvasGroup clearArtworkGroup;
        [SerializeField] private GameObject gameOverStainRoot;
        [SerializeField] private CanvasGroup gameOverArtworkGroup;
        [SerializeField] private Image topAccentRule;
        [SerializeField] private Text eyebrowLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Text statusNoticeLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private Coroutine fadeRoutine;
        private readonly List<string> statParts = new List<string>(3);
        private Vector2 defaultCardPosition;
        private bool cardPositionCaptured;

        public bool IsOpen => overlay != null && overlay.activeSelf;
        public bool IsVictory { get; private set; }
        public string MainMenuSceneName
        {
            get => mainMenuSceneName;
            set => mainMenuSceneName = value;
        }

        public event Action<bool> ResultShown;
        public event Action RetryRequested;
        public event Action MainMenuRequested;

        private void Awake()
        {
            if (resultCardRect != null && !cardPositionCaptured)
            {
                defaultCardPosition = resultCardRect.anchoredPosition;
                cardPositionCaptured = true;
            }
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (overlayGroup != null)
                overlayGroup.alpha = 0f;
            if (blackoutGroup != null)
                blackoutGroup.alpha = 0f;
            if (clearArtworkGroup != null)
                clearArtworkGroup.alpha = 0f;
            if (gameOverArtworkGroup != null)
                gameOverArtworkGroup.alpha = 0f;
            if (cardGroup != null)
                cardGroup.alpha = 0f;
            if (clearArtworkRoot != null)
                clearArtworkRoot.SetActive(false);
            if (gameOverStainRoot != null)
                gameOverStainRoot.SetActive(false);
            if (statusNoticeLabel != null)
                statusNoticeLabel.text = string.Empty;
            if (overlay != null)
                overlay.SetActive(false);
        }

        public void ShowGameOver(string reason = null, float elapsedSeconds = -1f,
            int puzzlesSolved = -1, int totalPuzzles = -1)
        {
            IsVictory = false;
            Show(
                eyebrow: "G A M E   O V E R",
                title: "어둠에 삼켜졌습니다",
                description: string.IsNullOrWhiteSpace(reason)
                    ? "미로의 위협으로부터 살아남지 못했습니다."
                    : reason,
                accent: Crimson,
                stats: FormatStats(elapsedSeconds, -1f, puzzlesSolved, totalPuzzles),
                retryText: "다시 시도",
                retrySubtitle: "RETRY");
        }

        public void ShowGameClear(float elapsedSeconds = -1f, float remainingHealthRatio = -1f,
            int puzzlesSolved = -1, int totalPuzzles = -1, string subtitle = null)
        {
            IsVictory = true;
            Show(
                eyebrow: "E S C A P E D",
                title: "미로를 벗어났습니다",
                description: string.IsNullOrWhiteSpace(subtitle)
                    ? "어둠 끝의 출구에 무사히 도달했습니다."
                    : subtitle,
                accent: Gold,
                stats: FormatStats(elapsedSeconds, remainingHealthRatio, puzzlesSolved, totalPuzzles),
                retryText: "다시 도전",
                retrySubtitle: "PLAY AGAIN");
        }

        public void Hide()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            if (overlayGroup != null)
                overlayGroup.alpha = 0f;
            if (blackoutGroup != null)
                blackoutGroup.alpha = 0f;
            if (clearArtworkGroup != null)
                clearArtworkGroup.alpha = 0f;
            if (gameOverArtworkGroup != null)
                gameOverArtworkGroup.alpha = 0f;
            if (cardGroup != null)
                cardGroup.alpha = 0f;
            if (clearArtworkRoot != null)
                clearArtworkRoot.SetActive(false);
            if (gameOverStainRoot != null)
                gameOverStainRoot.SetActive(false);
            if (resultCardRect != null && cardPositionCaptured)
                resultCardRect.anchoredPosition = defaultCardPosition;
            if (statusNoticeLabel != null)
                statusNoticeLabel.text = string.Empty;
            if (overlay != null)
                overlay.SetActive(false);
            if (pauseTimeOnResult && Time.timeScale == 0f)
                Time.timeScale = 1f;
        }

        public void MoveFocus(bool backwards)
        {
            if (!IsOpen)
                return;

            Button[] buttons = { retryButton, mainMenuButton };
            GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            int index = current == mainMenuButton?.gameObject ? 1 : 0;
            int next = (index + (backwards ? -1 : 1) + buttons.Length) % buttons.Length;
            if (buttons[next] != null && buttons[next].IsInteractable())
                EventSystem.current?.SetSelectedGameObject(buttons[next].gameObject);
        }

        private void Show(string eyebrow, string title, string description, Color accent,
            string stats, string retryText, string retrySubtitle)
        {
            if (overlay == null)
                return;

            if (topAccentRule != null) topAccentRule.color = accent;
            if (eyebrowLabel != null)
            {
                eyebrowLabel.text = eyebrow;
                eyebrowLabel.color = accent;
            }
            if (titleLabel != null) titleLabel.text = title;
            if (descriptionLabel != null) descriptionLabel.text = description;
            if (statsLabel != null)
            {
                statsLabel.text = stats;
                statsLabel.gameObject.SetActive(!string.IsNullOrEmpty(stats));
            }
            if (statusNoticeLabel != null) statusNoticeLabel.text = string.Empty;

            if (retryButton != null)
            {
                Transform label = retryButton.transform.Find("Label");
                if (label != null && label.TryGetComponent(out Text labelText))
                    labelText.text = retryText;
                Transform sub = retryButton.transform.Find("English caption");
                if (sub != null && sub.TryGetComponent(out Text subText))
                    subText.text = retrySubtitle;
            }

            if (pauseTimeOnResult)
                Time.timeScale = 0f;
            if (unlockCursorOnResult)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (resultCardRect != null)
            {
                if (!cardPositionCaptured)
                {
                    defaultCardPosition = resultCardRect.anchoredPosition;
                    cardPositionCaptured = true;
                }
                resultCardRect.anchoredPosition = defaultCardPosition;
            }

            if (clearArtworkRoot != null)
                clearArtworkRoot.SetActive(IsVictory);
            if (gameOverStainRoot != null)
                gameOverStainRoot.SetActive(!IsVictory);

            overlay.SetActive(true);
            if (overlayGroup != null)
            {
                overlayGroup.interactable = true;
                overlayGroup.blocksRaycasts = true;
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeInRoutine(IsVictory));
            }

            if (retryButton != null)
                EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);

            ResultShown?.Invoke(IsVictory);
        }

        private IEnumerator FadeInRoutine(bool victory)
        {
            if (blackoutGroup == null && cardGroup == null)
            {
                overlayGroup.alpha = 0f;
                const float fallbackDuration = 0.4f;
                for (float elapsed = 0f; elapsed < fallbackDuration; elapsed += Time.unscaledDeltaTime)
                {
                    overlayGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / fallbackDuration);
                    yield return null;
                }
                overlayGroup.alpha = 1f;
                fadeRoutine = null;
                yield break;
            }

            overlayGroup.alpha = 1f;
            if (blackoutGroup != null) blackoutGroup.alpha = 0f;
            if (clearArtworkGroup != null) clearArtworkGroup.alpha = 0f;
            if (gameOverArtworkGroup != null) gameOverArtworkGroup.alpha = 0f;
            if (cardGroup != null) cardGroup.alpha = 0f;

            if (!victory)
            {
                // 1) Screen blacks out into total darkness first
                const float blackoutDuration = 0.55f;
                for (float elapsed = 0f; elapsed < blackoutDuration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / blackoutDuration);
                    if (blackoutGroup != null) blackoutGroup.alpha = t;
                    yield return null;
                }
                if (blackoutGroup != null) blackoutGroup.alpha = 1f;

                // 2) Then the full-screen Game Over ink/blood artwork & typography emerge from the darkness
                const float revealDuration = 0.5f;
                for (float elapsed = 0f; elapsed < revealDuration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / revealDuration);
                    if (gameOverArtworkGroup != null) gameOverArtworkGroup.alpha = t;
                    if (cardGroup != null) cardGroup.alpha = t;
                    yield return null;
                }
                if (gameOverArtworkGroup != null) gameOverArtworkGroup.alpha = 1f;
                if (cardGroup != null) cardGroup.alpha = 1f;
            }
            else
            {
                // Reveal full-screen exit illustration + escape typography together
                const float clearDuration = 0.65f;
                for (float elapsed = 0f; elapsed < clearDuration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / clearDuration);
                    if (blackoutGroup != null) blackoutGroup.alpha = t;
                    if (clearArtworkGroup != null) clearArtworkGroup.alpha = t;
                    if (cardGroup != null) cardGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.85f));
                    yield return null;
                }
                if (blackoutGroup != null) blackoutGroup.alpha = 1f;
                if (clearArtworkGroup != null) clearArtworkGroup.alpha = 1f;
                if (cardGroup != null) cardGroup.alpha = 1f;
            }

            fadeRoutine = null;
        }

        private void OnRetryClicked()
        {
            if (transitionManager != null && transitionManager.IsTransitioning)
                return;

            if (RetryRequested != null)
            {
                RetryRequested.Invoke();
                return;
            }

            if (transitionManager != null)
            {
                transitionManager.ReloadCurrentScene("다시 시작하는 중…", ShowNotice);
            }
            else
            {
                ShowNotice("SceneTransitionManager가 연결되지 않았습니다.");
            }
        }

        private void OnMainMenuClicked()
        {
            if (transitionManager != null && transitionManager.IsTransitioning)
                return;

            if (MainMenuRequested != null)
            {
                MainMenuRequested.Invoke();
                return;
            }

            if (transitionManager != null)
            {
                transitionManager.LoadScene(mainMenuSceneName, "메인 화면으로 돌아가는 중…", ShowNotice);
            }
            else
            {
                ShowNotice("SceneTransitionManager가 연결되지 않았습니다.");
            }
        }

        private void ShowNotice(string message)
        {
            if (statusNoticeLabel != null)
                statusNoticeLabel.text = message;
        }

        private string FormatStats(float elapsedSeconds, float remainingHealthRatio, int puzzlesSolved, int totalPuzzles)
        {
            statParts.Clear();
            if (elapsedSeconds >= 0f)
            {
                int total = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
                int minutes = total / 60;
                int seconds = total % 60;
                statParts.Add($"{(IsVictory ? "탈출 시간" : "생존 시간")}  {minutes:00}:{seconds:00}");
            }
            if (remainingHealthRatio >= 0f)
            {
                int percent = Mathf.RoundToInt(Mathf.Clamp01(remainingHealthRatio) * 100f);
                statParts.Add($"남은 체력  {percent}%");
            }
            if (puzzlesSolved >= 0 && totalPuzzles > 0)
            {
                statParts.Add($"해제한 장치  {puzzlesSolved} / {totalPuzzles}");
            }
            return string.Join("     ·     ", statParts);
        }
    }
}
