using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    /// <summary>
    /// Scene-flow transition fade controller (UI-03).
    /// Validates Build Profiles before loading and always resets Time.timeScale cleanly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Text statusLabel;
        [SerializeField] private bool fadeInOnStart = true;
        [SerializeField] private float fadeInDuration = 0.55f;
        [SerializeField] private float fadeOutDuration = 0.45f;
        [SerializeField] private bool persistAcrossScenes;

        public static SceneTransitionManager Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        public event Action<string> TransitionStarted;
        public event Action<string> TransitionFailed;

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance == null)
            {
                Instance = this;
            }

            if (fadeGroup != null)
            {
                fadeGroup.alpha = fadeInOnStart ? 1f : 0f;
                fadeGroup.blocksRaycasts = fadeInOnStart;
            }
            if (statusLabel != null)
                statusLabel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            if (fadeInOnStart && fadeGroup != null)
                StartCoroutine(FadeRoutine(0f, fadeInDuration, false));
        }

        public bool CanLoadScene(string sceneNameOrPath)
        {
            return !string.IsNullOrWhiteSpace(sceneNameOrPath) &&
                   Application.CanStreamedLevelBeLoaded(sceneNameOrPath);
        }

        public bool LoadScene(string sceneNameOrPath, string loadingMessage = null, Action<string> onFailed = null)
        {
            if (IsTransitioning)
                return false;

            if (!CanLoadScene(sceneNameOrPath))
            {
                string error = string.IsNullOrWhiteSpace(sceneNameOrPath)
                    ? "이동할 씬 이름이 지정되지 않았습니다."
                    : $"'{sceneNameOrPath}' 씬이 Build Profiles에 아직 등록되지 않았습니다.";
                onFailed?.Invoke(error);
                TransitionFailed?.Invoke(error);
                return false;
            }

            StartCoroutine(LoadSceneRoutine(sceneNameOrPath, loadingMessage, onFailed));
            return true;
        }

        public bool ReloadCurrentScene(string loadingMessage = "다시 시작하는 중…", Action<string> onFailed = null)
        {
            if (IsTransitioning)
                return false;

            Scene active = SceneManager.GetActiveScene();
            if (CanLoadScene(active.name))
                return LoadScene(active.name, loadingMessage, onFailed);
            if (CanLoadScene(active.path))
                return LoadScene(active.path, loadingMessage, onFailed);

#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(active.path))
            {
                StartCoroutine(LoadEditorSceneRoutine(active.path, loadingMessage, onFailed));
                return true;
            }
#endif

            const string error = "현재 씬이 Build Profiles에 등록되지 않아 다시 불러올 수 없습니다.";
            onFailed?.Invoke(error);
            TransitionFailed?.Invoke(error);
            return false;
        }

        private IEnumerator LoadSceneRoutine(string sceneNameOrPath, string loadingMessage, Action<string> onFailed)
        {
            IsTransitioning = true;
            TransitionStarted?.Invoke(sceneNameOrPath);
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (fadeGroup != null)
                fadeGroup.blocksRaycasts = true;
            ShowStatus(loadingMessage);

            yield return FadeRoutine(1f, fadeOutDuration, true);

            AsyncOperation operation = null;
            string error = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneNameOrPath, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (operation == null)
            {
                yield return RecoverFromFailure(error ?? "씬을 불러오지 못했습니다.", onFailed);
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            if (persistAcrossScenes)
            {
                ShowStatus(null);
                yield return FadeRoutine(0f, fadeInDuration, false);
                IsTransitioning = false;
            }
        }

#if UNITY_EDITOR
        private IEnumerator LoadEditorSceneRoutine(string scenePath, string loadingMessage, Action<string> onFailed)
        {
            IsTransitioning = true;
            TransitionStarted?.Invoke(scenePath);
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (fadeGroup != null)
                fadeGroup.blocksRaycasts = true;
            ShowStatus(loadingMessage);

            yield return FadeRoutine(1f, fadeOutDuration, true);

            AsyncOperation operation = null;
            string error = null;
            try
            {
                operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (operation == null)
            {
                yield return RecoverFromFailure(error ?? "테스트 씬을 다시 불러오지 못했습니다.", onFailed);
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }
#endif

        private IEnumerator RecoverFromFailure(string error, Action<string> onFailed)
        {
            ShowStatus(null);
            yield return FadeRoutine(0f, 0.3f, false);
            IsTransitioning = false;
            onFailed?.Invoke(error);
            TransitionFailed?.Invoke(error);
        }

        private void ShowStatus(string message)
        {
            if (statusLabel == null)
                return;
            bool hasMessage = !string.IsNullOrWhiteSpace(message);
            statusLabel.gameObject.SetActive(hasMessage);
            if (hasMessage)
                statusLabel.text = message;
        }

        private IEnumerator FadeRoutine(float target, float duration, bool blockAfter)
        {
            if (fadeGroup == null)
                yield break;

            float start = fadeGroup.alpha;
            if (duration <= 0f)
            {
                fadeGroup.alpha = target;
                fadeGroup.blocksRaycasts = blockAfter;
                yield break;
            }

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                fadeGroup.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            fadeGroup.alpha = target;
            fadeGroup.blocksRaycasts = blockAfter;
        }
    }
}
