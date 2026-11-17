using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    /// <summary>Menu-only behaviour. No dependency on the gameplay team's scripts.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Gameplay connection")]
        [Tooltip("Name or full asset path of a gameplay scene included in Build Profiles. Leave empty until it exists.")]
        [SerializeField] private string gameplaySceneName = "";

        [Header("Menu")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private MenuSettingsController settings;
        [SerializeField] private Button quitButton;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private CanvasGroup screenFade;
        [SerializeField] private Text loadingLabel;

        [Header("Dialog")]
        [SerializeField] private GameObject dialog;
        [SerializeField] private Text dialogEyebrow;
        [SerializeField] private Text dialogTitle;
        [SerializeField] private Text dialogBody;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button dismissButton;

        private GameObject previousSelection;
        private bool transitioning;
        private InputAction tabAction;
        private InputAction escapeAction;

        public string GameplaySceneName => gameplaySceneName;
        public bool IsDialogOpen => dialog != null && dialog.activeSelf;
        public bool IsSettingsOpen => settings != null && settings.IsOpen;

        private void Awake()
        {
            startButton.onClick.AddListener(StartGame);
            helpButton.onClick.AddListener(ShowHelp);
            if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettings);
            if (settings != null) settings.Closed += RestoreMenu;
            quitButton.onClick.AddListener(ShowQuitConfirmation);
            closeButton.onClick.AddListener(CloseDialog);
            dismissButton.onClick.AddListener(CloseDialog);
            confirmQuitButton.onClick.AddListener(QuitGame);
            dialog.SetActive(false);
            screenFade.alpha = 1f;
            screenFade.blocksRaycasts = true;
            loadingLabel.gameObject.SetActive(false);
            tabAction = new InputAction("Menu Tab", InputActionType.Button, "<Keyboard>/tab");
            escapeAction = new InputAction("Menu Cancel", InputActionType.Button, "<Keyboard>/escape");
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
            if (settings != null) settings.Closed -= RestoreMenu;
            tabAction?.Dispose();
            escapeAction?.Dispose();
        }

        private IEnumerator Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EventSystem.current?.SetSelectedGameObject(startButton.gameObject);
            yield return FadeTo(0f, 0.9f);
            screenFade.blocksRaycasts = false;
        }

        private void OnTab(InputAction.CallbackContext context)
        {
            if (transitioning)
                return;

            if (IsSettingsOpen)
            {
                settings.MoveFocus(Keyboard.current != null && Keyboard.current.shiftKey.isPressed);
                return;
            }

            Button[] choices = IsDialogOpen
                ? (confirmQuitButton.gameObject.activeSelf
                    ? new[] { dismissButton, confirmQuitButton, closeButton }
                    : new[] { dismissButton, closeButton })
                : (settingsButton != null ? new[] { startButton, helpButton, settingsButton, quitButton }
                    : new[] { startButton, helpButton, quitButton });
            GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            int index = System.Array.FindIndex(choices, b => b.gameObject == current);
            int direction = Keyboard.current != null && Keyboard.current.shiftKey.isPressed ? -1 : 1;
            EventSystem.current?.SetSelectedGameObject(choices[(index + direction + choices.Length) % choices.Length].gameObject);
        }

        private void OnEscape(InputAction.CallbackContext context)
        {
            if (transitioning)
                return;

            if (IsSettingsOpen)
                settings.Cancel();
            else if (IsDialogOpen)
                CloseDialog();
            else
                ShowQuitConfirmation();
        }

        public void StartGame()
        {
            if (transitioning || IsDialogOpen || IsSettingsOpen)
                return;

            if (string.IsNullOrWhiteSpace(gameplaySceneName) ||
                !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                OpenDialog("UNDER CONSTRUCTION", "미로를 준비하고 있습니다",
                    "아직 탐험할 미로가 연결되지 않았습니다.\n\n" +
                    "플레이 씬 제작 후 MainMenuController의\n" +
                    "Gameplay Scene Name을 지정하고,\n" +
                    "해당 씬을 Build Profiles에 추가해 주세요.", false);
                return;
            }

            StartCoroutine(LoadGameplay());
        }

        public void ShowHelp()
        {
            if (transitioning || IsSettingsOpen)
                return;

            OpenDialog("EXPLORER'S GUIDE", "어둠 속에서 살아남기",
                "제한된 빛을 따라 길을 찾고, 장치를 풀어 탈출하세요.\n" +
                "미로 안에서는 당신만 움직이고 있는 것이 아닙니다.\n\n" +
                "W A S D     이동\n" +
                "마우스        주변 둘러보기\n" +
                "F                탐사 장비 켜기 / 끄기\n" +
                "E                장치 및 오브젝트 상호작용\n\n" +
                "※ 플레이 조작은 제안안이며 개발 과정에서 변경될 수 있습니다.\n" +
                "메뉴: 방향키 / Tab 이동 · Enter 선택 · Esc 닫기", false);
        }

        public void ShowQuitConfirmation()
        {
            if (!transitioning && !IsSettingsOpen)
                OpenDialog("LEAVE THE DARK", "탐험을 마치시겠습니까?",
                    "미로는 다음 방문에도 이곳에서 기다립니다.\n게임을 종료하시겠습니까?", true);
        }

        public void CloseDialog()
        {
            if (transitioning || !IsDialogOpen)
                return;

            dialog.SetActive(false);
            RestoreMenu();
        }

        public void ShowSettings()
        {
            if (transitioning || IsDialogOpen || IsSettingsOpen || settings == null) return;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            menuGroup.interactable = menuGroup.blocksRaycasts = false;
            settings.Open();
        }

        private void RestoreMenu()
        {
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
            EventSystem.current?.SetSelectedGameObject(previousSelection != null
                ? previousSelection : startButton.gameObject);
        }

        private void OpenDialog(string eyebrow, string title, string body, bool quitting)
        {
            if (!IsDialogOpen)
                previousSelection = EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject : startButton.gameObject;

            dialogEyebrow.text = eyebrow;
            dialogTitle.text = title;
            dialogBody.text = body;
            confirmQuitButton.gameObject.SetActive(quitting);
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;
            dialog.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(dismissButton.gameObject);
        }

        private IEnumerator LoadGameplay()
        {
            transitioning = true;
            menuGroup.interactable = false;
            screenFade.blocksRaycasts = true;
            loadingLabel.gameObject.SetActive(true);
            loadingLabel.text = "미로로 들어가는 중…";
            yield return FadeTo(1f, 0.65f);

            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Unable to open the maze: {exception.Message}", this);
            }

            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
                yield break;
            }

            loadingLabel.gameObject.SetActive(false);
            yield return FadeTo(0f, 0.35f);
            screenFade.blocksRaycasts = false;
            transitioning = false;
            OpenDialog("CONNECTION FAILED", "미로에 진입하지 못했습니다",
                "플레이 씬 이름과 Build Profiles의 씬 목록을 확인해 주세요.", false);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float from = screenFade.alpha;
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                screenFade.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, time / duration));
                yield return null;
            }
            screenFade.alpha = target;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
