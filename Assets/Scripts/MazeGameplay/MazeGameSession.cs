using DarkMaze.UI;
using Minseo.MazeItems;
using UnityEngine;
using Yeeun.GasMask;
using Yeeun.Player;
using Yeeun.Sonar;
using Yeeun.SymbolPuzzle;

namespace DarkMaze.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MazeGameSession : MonoBehaviour
    {
        [SerializeField] private CharacterController player;
        [SerializeField] private MazePlayerController movement;
        [SerializeField] private SonarDevice sonar;
        [SerializeField] private SymbolInteractor interactor;
        [SerializeField] private PlayerItems items;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private SymbolSequencePuzzle exitPuzzle;
        [SerializeField] private GameResultController result;
        [SerializeField] private GameplayHudController pauseMenu;

        private float startTime;
        private bool finished;
        private bool restoreInputAfterFrame;

        private void OnEnable()
        {
            health.onDied.AddListener(ShowGameOver);
            if (pauseMenu != null)
                pauseMenu.PauseChanged += OnPauseChanged;
        }

        private void Start()
        {
            startTime = Time.time;
            if (Application.isFocused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            health.onDied.RemoveListener(ShowGameOver);
            if (pauseMenu != null)
                pauseMenu.PauseChanged -= OnPauseChanged;
            if (!finished)
                return;

            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void OnPauseChanged(bool paused)
        {
            if (finished)
                return;

            restoreInputAfterFrame = !paused;
            if (paused)
                SetGameplayInput(false);
        }

        private void LateUpdate()
        {
            if (!restoreInputAfterFrame)
                return;

            restoreInputAfterFrame = false;
            // Let the UI consume this frame's Escape key or resume click before gameplay reads it.
            SetGameplayInput(true);
        }

        private void SetGameplayInput(bool enabled)
        {
            movement.ReadPlayerInput = enabled;
            sonar.ReadPlayerInput = enabled;
            interactor.enabled = enabled;
            items.enabled = enabled;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != player || !exitPuzzle.IsSolved || health.IsDead || finished)
                return;

            EndRun();
            result.ShowGameClear(Time.time - startTime, (float)health.Current / health.Max, 1, 1);
        }

        private void ShowGameOver()
        {
            if (finished)
                return;

            EndRun();
            result.ShowGameOver(elapsedSeconds: Time.time - startTime,
                puzzlesSolved: exitPuzzle.IsSolved ? 1 : 0, totalPuzzles: 1);
        }

        private void EndRun()
        {
            finished = true;
            restoreInputAfterFrame = false;
            SetGameplayInput(false);
            movement.enabled = false;
            sonar.enabled = false;
            AudioListener.pause = true;
        }
    }
}
