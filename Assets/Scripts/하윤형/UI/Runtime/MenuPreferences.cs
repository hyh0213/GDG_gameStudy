using UnityEngine;

namespace DarkMaze.UI
{
    /// <summary>Local player preferences only. Never writes Unity project/Input System settings.</summary>
    public static class MenuPreferences
    {
        private const string Key = "DarkMaze.HYH.Settings.";
        public const float DefaultVolume = 0.8f;

        public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(Key + "Volume", DefaultVolume));
        public static float MouseSensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(Key + "Sensitivity", 1f), 0.2f, 3f);
        public static bool InvertY => PlayerPrefs.GetInt(Key + "InvertY", 0) != 0;
        public static bool ReducedMenuMotion => PlayerPrefs.GetInt(Key + "ReducedMotion", 0) != 0;
        public static bool HasDisplayPreference => PlayerPrefs.HasKey(Key + "Width");
        public static int Width => Mathf.Clamp(PlayerPrefs.GetInt(Key + "Width", Screen.width), 640, 16384);
        public static int Height => Mathf.Clamp(PlayerPrefs.GetInt(Key + "Height", Screen.height), 480, 16384);
        public static bool Fullscreen => PlayerPrefs.GetInt(Key + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;

        public static void Save(float volume, float sensitivity, bool invertY, bool reducedMotion,
            int width, int height, bool fullscreen)
        {
            PlayerPrefs.SetFloat(Key + "Volume", Mathf.Clamp01(volume));
            PlayerPrefs.SetFloat(Key + "Sensitivity", Mathf.Clamp(sensitivity, 0.2f, 3f));
            PlayerPrefs.SetInt(Key + "InvertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt(Key + "ReducedMotion", reducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Key + "Width", Mathf.Clamp(width, 640, 16384));
            PlayerPrefs.SetInt(Key + "Height", Mathf.Clamp(height, 480, 16384));
            PlayerPrefs.SetInt(Key + "Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
