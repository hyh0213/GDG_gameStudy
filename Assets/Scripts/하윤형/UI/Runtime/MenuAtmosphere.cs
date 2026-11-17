using UnityEngine;

namespace DarkMaze.UI
{
    /// <summary>Subtle, cosmetic motion only; does not touch gameplay or global audio settings.</summary>
    public sealed class MenuAtmosphere : MonoBehaviour
    {
        [SerializeField] private Transform menuCamera;
        [SerializeField] private Light corridorLamp;
        [SerializeField] private RectTransform driftingMap;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;
        private Vector2 mapPosition;
        private float lampIntensity;
        private bool reducedMotion;
        private bool initialized;

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (!initialized || !value) return;
            if (menuCamera != null) menuCamera.SetLocalPositionAndRotation(cameraPosition, cameraRotation);
            if (driftingMap != null) driftingMap.anchoredPosition = mapPosition;
            if (corridorLamp != null) corridorLamp.intensity = lampIntensity;
        }

        private void Awake()
        {
            if (menuCamera != null)
            {
                cameraPosition = menuCamera.localPosition;
                cameraRotation = menuCamera.localRotation;
            }
            if (corridorLamp != null)
                lampIntensity = corridorLamp.intensity;
            if (driftingMap != null)
                mapPosition = driftingMap.anchoredPosition;
            initialized = true;
            SetReducedMotion(MenuPreferences.ReducedMenuMotion);
        }

        private void Update()
        {
            if (reducedMotion) return;
            float time = Time.unscaledTime;
            if (menuCamera != null)
            {
                menuCamera.localPosition = cameraPosition + new Vector3(Mathf.Sin(time * 0.19f) * 0.018f,
                    Mathf.Sin(time * 0.27f) * 0.012f, 0f);
                menuCamera.localRotation = cameraRotation * Quaternion.Euler(
                    Mathf.Sin(time * 0.18f) * 0.08f, Mathf.Sin(time * 0.14f) * 0.12f, 0f);
            }
            if (corridorLamp != null)
            {
                float noise = Mathf.PerlinNoise(time * 1.45f, 1.3f);
                float sputter = Mathf.PerlinNoise(time * 6.8f, 4.7f) > 0.82f ? 0.72f : 1f;
                corridorLamp.intensity = lampIntensity * (0.82f + noise * 0.26f) * sputter;
            }
            if (driftingMap != null)
                driftingMap.anchoredPosition = mapPosition + new Vector2(0f, Mathf.Sin(time * 0.17f) * 3f);
        }
    }
}
