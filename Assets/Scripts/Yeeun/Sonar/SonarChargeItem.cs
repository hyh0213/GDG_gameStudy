using UnityEngine;
using Yeeun.SymbolPuzzle;

namespace Yeeun.Sonar
{
    /// <summary>
    /// Charge cell lying in the maze: E (through SymbolInteractor) refills pulse charges
    /// on the player's <see cref="SonarDevice"/>. Cannot be taken while the device is full.
    /// </summary>
    public class SonarChargeItem : MonoBehaviour, ISymbolInteractable
    {
        [SerializeField, Min(1)] int charges = 1;
        [Tooltip("Model that bobs and spins slowly.")]
        [SerializeField] Transform visual;
        [SerializeField] float bobHeight = 0.04f;
        [SerializeField] float spinSpeed = 40f;

        Vector3 visualBase;

        void Awake()
        {
            if (visual != null) visualBase = visual.localPosition;
        }

        void Update()
        {
            if (visual == null) return;
            visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        public string GetPrompt(SymbolInteractor interactor)
        {
            var device = interactor.GetComponentInParent<SonarDevice>();
            if (device == null) return null;
            return device.IsFull ? "음파 충전 셀 (충전 가득 참)" : "[E] 음파 충전 셀 줍기";
        }

        public void Interact(SymbolInteractor interactor)
        {
            var device = interactor.GetComponentInParent<SonarDevice>();
            if (device != null && device.AddCharge(charges)) Destroy(gameObject);
        }
    }
}
