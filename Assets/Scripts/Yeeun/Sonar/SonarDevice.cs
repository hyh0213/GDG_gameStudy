using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Yeeun.Sonar
{
    /// <summary>
    /// The exploration device (탐사 장비).
    /// F turns it on/off. While on, its own hum faintly reveals edges within
    /// <see cref="passiveRadius"/> (1~2 m). Q sends a sound pulse: a wave travels out at
    /// <see cref="waveSpeed"/> up to <see cref="range"/> and lights up edges, which then fade.
    /// Pulses use charges: max <see cref="maxCharges"/>, one comes back every
    /// <see cref="rechargeSeconds"/>, or right away from a charge item (<see cref="AddCharge"/>).
    /// After a pulse there is a <see cref="cooldown"/> to stop spamming.
    /// Each pulse raises <see cref="NoiseEmitted"/> so monsters can react later.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SonarDevice : MonoBehaviour
    {
        static readonly int PulsesId = Shader.PropertyToID("_SonarPulses");
        static readonly int ParamsId = Shader.PropertyToID("_SonarParams");
        static readonly int PassiveId = Shader.PropertyToID("_SonarPassive");
        static readonly int StateId = Shader.PropertyToID("_SonarState");
        const int MaxPulses = 4;

        [Tooltip("Where the sound comes from (the head / camera).")]
        [SerializeField] Transform emitter;
        [SerializeField] bool startOn = true;
        [SerializeField] Key toggleKey = Key.F;
        [SerializeField] Key pulseKey = Key.Q;

        [Header("Pulse")]
        [SerializeField, Range(5f, 30f)] float range = 14f;
        [SerializeField] float waveSpeed = 20f;
        [Tooltip("How long edges stay visible after the wave passes.")]
        [SerializeField] float afterglow = 3.5f;
        [Tooltip("The wave front is extra bright for this long.")]
        [SerializeField] float frontTime = 0.3f;
        [SerializeField] float frontBoost = 1.2f;

        [Header("Passive hum")]
        [SerializeField, Range(0.5f, 3f)] float passiveRadius = 2f;
        [SerializeField, Range(0f, 1f)] float passiveIntensity = 0.22f;

        [Header("Charges")]
        [SerializeField, Min(1)] int maxCharges = 3;
        [SerializeField] float rechargeSeconds = 60f;
        [SerializeField] float cooldown = 1f;

        [Header("Noise (for monsters)")]
        [Tooltip("Pulse noise radius = range x this.")]
        [SerializeField] float noiseRangeMultiplier = 1.5f;

        public UnityEvent onPulse = new UnityEvent();
        [Tooltip("Q pressed but nothing happened (off, empty or cooling down) - e.g. a click sound.")]
        public UnityEvent onPulseDenied = new UnityEvent();
        public UnityEvent<bool> onToggled = new UnityEvent<bool>();

        /// <summary>(position, noise radius) of every pulse. Monster AI can listen to this.</summary>
        public static event Action<Vector3, float> NoiseEmitted;

        public bool IsOn { get; private set; }
        public int Charges { get; private set; }
        public int MaxCharges => maxCharges;
        public bool IsFull => Charges >= maxCharges;
        public float CooldownRemaining { get; private set; }
        /// <summary>0..1 progress of the charge currently refilling (0 when full).</summary>
        public float RechargeProgress => IsFull ? 0f : rechargeTimer / rechargeSeconds;
        public float Range => range;
        public int PulseCount { get; private set; }

        readonly Vector4[] pulses = new Vector4[MaxPulses];
        int nextPulse;
        float rechargeTimer;

        void Awake()
        {
            if (emitter == null) emitter = transform;
            Charges = maxCharges;
            IsOn = startOn;
            for (int i = 0; i < MaxPulses; i++) pulses[i] = new Vector4(0, 0, 0, -1);
            PushGlobals();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (toggleKey != Key.None && keyboard[toggleKey].wasPressedThisFrame) Toggle();
                if (pulseKey != Key.None && keyboard[pulseKey].wasPressedThisFrame) TryPulse();
            }

            if (CooldownRemaining > 0f) CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);

            if (!IsFull)
            {
                rechargeTimer += Time.deltaTime;
                if (rechargeTimer >= rechargeSeconds)
                {
                    rechargeTimer -= rechargeSeconds;
                    Charges++;
                    if (IsFull) rechargeTimer = 0f;
                }
            }

            float lifetime = range / Mathf.Max(waveSpeed, 0.01f) + afterglow;
            for (int i = 0; i < MaxPulses; i++)
                if (pulses[i].w >= 0f && Time.time - pulses[i].w > lifetime) pulses[i].w = -1f;
        }

        void LateUpdate() => PushGlobals();

        void OnDisable() => Shader.SetGlobalVector(StateId, Vector4.zero);

        public void Toggle() => SetOn(!IsOn);

        public void SetOn(bool on)
        {
            if (IsOn == on) return;
            IsOn = on;
            onToggled.Invoke(on);
        }

        /// <summary>Sends a pulse if the device is on, has a charge and is not cooling down.</summary>
        public bool TryPulse()
        {
            if (!IsOn || Charges <= 0 || CooldownRemaining > 0f)
            {
                onPulseDenied.Invoke();
                return false;
            }

            Charges--;
            CooldownRemaining = cooldown;
            PulseCount++;
            Vector3 origin = emitter.position;
            pulses[nextPulse] = new Vector4(origin.x, origin.y, origin.z, Time.time);
            nextPulse = (nextPulse + 1) % MaxPulses;
            onPulse.Invoke();
            NoiseEmitted?.Invoke(origin, range * noiseRangeMultiplier);
            return true;
        }

        /// <summary>Adds charges (from an item). Returns false when already full.</summary>
        public bool AddCharge(int amount = 1)
        {
            if (IsFull || amount <= 0) return false;
            Charges = Mathf.Min(maxCharges, Charges + amount);
            if (IsFull) rechargeTimer = 0f;
            return true;
        }

        void PushGlobals()
        {
            Shader.SetGlobalVectorArray(PulsesId, pulses);
            Shader.SetGlobalVector(ParamsId, new Vector4(waveSpeed, range, afterglow, frontTime));
            Vector3 p = emitter != null ? emitter.position : transform.position;
            Shader.SetGlobalVector(PassiveId, new Vector4(p.x, p.y, p.z, passiveRadius));
            Shader.SetGlobalVector(StateId, new Vector4(IsOn ? 1f : 0f, passiveIntensity, Time.time, frontBoost));
        }
    }
}
