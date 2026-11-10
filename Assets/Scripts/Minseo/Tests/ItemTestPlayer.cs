using UnityEngine;
using UnityEngine.InputSystem;

namespace Minseo.MazeItems.Tests
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerItems))]
    public sealed class ItemTestPlayer : MonoBehaviour, IHealingTarget
    {
        public const int MaxHealth = 100;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        [SerializeField] private Camera view;
        [SerializeField] private Renderer[] mazeSurfaces;

        private CharacterController controller;
        private PlayerItems items;
        private float pitch;

        public Camera View => view;
        public PlayerItems Items => items;
        public int Health { get; private set; } = MaxHealth;
        public bool ScanOn { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            items = GetComponent<PlayerItems>();
            SetScan(true);
        }

        public bool TryHeal(int amount)
        {
            int healed = Mathf.Min(amount, MaxHealth - Health);
            if (healed <= 0)
                return false;

            Health += healed;
            return true;
        }

        public void Damage(int amount) => Health = Mathf.Max(0, Health - amount);

        public void SetScan(bool on)
        {
            ScanOn = on;
            var properties = new MaterialPropertyBlock();
            foreach (Renderer surface in mazeSurfaces)
            {
                surface.GetPropertyBlock(properties);
                properties.SetColor(BaseColor, on ? surface.sharedMaterial.GetColor(BaseColor) : Color.black);
                surface.SetPropertyBlock(properties);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.hKey.wasPressedThisFrame)
                Damage(45);
            if (keyboard.tKey.wasPressedThisFrame)
                SetScan(!ScanOn);

            float horizontal = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float forward = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            Vector3 move = Vector3.ClampMagnitude(new Vector3(horizontal, 0, forward), 1);
            controller.Move((transform.TransformDirection(move) * 3 + Vector3.down * 2) * Time.deltaTime);

            float turn = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
            float tilt = (keyboard.downArrowKey.isPressed ? 1 : 0) - (keyboard.upArrowKey.isPressed ? 1 : 0);
            transform.Rotate(0, turn * 100 * Time.deltaTime, 0);
            pitch = Mathf.Clamp(pitch + tilt * 80 * Time.deltaTime, -65, 65);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 450, 46),
                $"HP {Health}/{MaxHealth}    Beacon {(items.HasBeacon ? 1 : 0)}/1    Scan {(ScanOn ? "ON" : "OFF")}\n" +
                "WASD Move  |  Arrows Look  |  B Place  |  H Damage  |  T Scan");
        }
    }
}
