using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Yeeun.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class MazePlayerController : MonoBehaviour
    {
        [Header("Connected camera rig: Look > Gait > Impact > Camera")]
        public Transform lookPivot;
        public Transform gaitPivot;
        public Camera viewCamera;
        public AudioSource footsteps;
        public AudioClip[] footstepClips;
        [Header("Metres / seconds")]
        [Min(0.1f)] public float walkSpeed = 2.5f;
        [Min(0.1f)] public float runSpeed = 4.2f;
        [Min(0.1f)] public float acceleration = 20f;
        [Min(0.1f)] public float braking = 38f;
        public float gravity = -20f;
        [Header("Immediate mouse look")]
        [Min(0)] public float mouseSensitivity = 0.10f;
        [Header("Distance-driven footsteps")]
        [Min(0.1f)] public float walkStepDistance = 1.25f;
        [Min(0.1f)] public float runStepDistance = 1.65f;
        [Range(0, 1)] public float walkVolume = 0.32f;
        [Range(0, 1)] public float runVolume = 0.7f;
        [Min(0)] public float walkHearingRadius = 4f;
        [Min(0)] public float runHearingRadius = 9f;
        [Header("Small camera effects (set bob to 0 for comfort)")]
        [Min(0)] public float walkBob = 0.018f;
        [Min(0)] public float runBob = 0.032f;
        [Header("Running camera motion")]
        [Range(0, 4)] public float runRollDegrees = 1.35f;
        [Range(0, 4)] public float turnLeanDegrees = 1.5f;
        [Range(0, 4)] public float strafeLeanDegrees = 1.1f;
        [Range(0, 3)] public float runPitchDegrees = 0.8f;
        [Range(0, 0.08f)] public float runStrideSway = 0.024f;
        [Range(0, 0.06f)] public float runForwardSway = 0.012f;
        [Min(1)] public float cameraSettleSpeed = 10f;
        [Range(40, 100)] public float baseFov = 65f;
        [Range(0, 15)] public float runFovIncrease = 5f;
        public bool ReadPlayerInput { get; set; } = true;
        public float ActualSpeed { get; private set; }
        public bool IsRunning { get; private set; }
        public int StepCount { get; private set; }
        public float LastNoiseRadius { get; private set; }
        public event Action<Vector3, float> Footstep;

        CharacterController body;
        Vector3 velocity, gaitOrigin;
        Quaternion gaitRotationOrigin;
        float verticalSpeed, pitch, stepProgress, gaitPhase, runBlend, smoothedTurn;
        int clipIndex;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            if (gaitPivot) gaitOrigin = gaitPivot.localPosition;
            if (gaitPivot) gaitRotationOrigin = gaitPivot.localRotation;
            if (viewCamera) viewCamera.fieldOfView = baseFov;
        }

        void Update()
        {
            if (!ReadPlayerInput) return;
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) SetCursor(false);
            else if (m != null && m.leftButton.wasPressedThisFrame) SetCursor(true);
            bool captured = Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
            Vector2 input = Vector2.zero;
            bool shift = false;
            if (captured && k != null)
            {
                input = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                shift = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            }
            Simulate(input, captured && m != null ? m.delta.ReadValue() : Vector2.zero, shift, Time.deltaTime);
        }

        // Also used by the personal scene's repeatable movement checks.
        public void Simulate(Vector2 input, Vector2 mouseDelta, bool shift, float dt)
        {
            if (dt <= 0 || !body || !body.enabled) return;
            dt = Mathf.Min(dt, 0.05f); // Avoid a large jump after a stalled frame.
            float yawDelta = mouseDelta.x * mouseSensitivity;
            transform.Rotate(0, yawDelta, 0);
            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -85f, 85f);
            if (lookPivot) lookPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
            input = Vector2.ClampMagnitude(input, 1);
            bool wantsRun = shift && input.y > 0;
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            Vector3 target = direction * (wantsRun ? runSpeed : walkSpeed);
            bool slowing = target.sqrMagnitude < velocity.sqrMagnitude || Vector3.Dot(target, velocity) < 0;
            velocity = Vector3.MoveTowards(velocity, target, (slowing ? braking : acceleration) * dt);
            verticalSpeed = body.isGrounded ? -2f : Mathf.Max(verticalSpeed + gravity * dt, -40f);
            Vector3 before = transform.position;
            body.Move((velocity + Vector3.up * verticalSpeed) * dt);
            Vector3 actual = Vector3.ProjectOnPlane(transform.position - before, Vector3.up);
            ActualSpeed = actual.magnitude / dt;
            // Discard velocity absorbed by a wall instead of accumulating pressure against it.
            velocity = Vector3.ClampMagnitude(actual / dt, velocity.magnitude);
            bool moving = body.isGrounded && ActualSpeed > 0.08f;
            IsRunning = wantsRun && moving && ActualSpeed > walkSpeed + 0.1f;
            float effort = moving && wantsRun ? Mathf.InverseLerp(walkSpeed, runSpeed, ActualSpeed) : 0f;
            // Keep effects tied to measured motion, so a wall cannot produce running camera motion.
            runBlend = Mathf.MoveTowards(runBlend, effort, dt * (effort > runBlend ? 4f : 7f));
            smoothedTurn = Mathf.Lerp(smoothedTurn, Mathf.Clamp(yawDelta / dt, -120f, 120f),
                1f - Mathf.Exp(-9f * dt));
            if (moving)
            {
                float fraction = actual.magnitude / Mathf.Max(0.1f, IsRunning ? runStepDistance : walkStepDistance);
                stepProgress += fraction;
                gaitPhase = Mathf.Repeat(gaitPhase + fraction * Mathf.PI, Mathf.PI * 2);
                while (stepProgress >= 1f) { stepProgress -= 1f; EmitFootstep(); }
            }
            else stepProgress = 0f;
            float blend = 1f - Mathf.Exp(-cameraSettleSpeed * dt);
            if (gaitPivot)
            {
                float size = IsRunning ? runBob : walkBob;
                float stride = Mathf.Sin(gaitPhase);
                Vector3 bob = moving ? new Vector3(stride * size * 0.5f,
                    -Mathf.Cos(gaitPhase * 2f) * size, 0) : Vector3.zero;
                bob += new Vector3(stride * runStrideSway * runBlend, 0,
                    -Mathf.Abs(stride) * runForwardSway * runBlend);
                gaitPivot.localPosition = Vector3.Lerp(gaitPivot.localPosition, gaitOrigin + bob, blend);
                float localStrafe = moving ? Vector3.Dot(actual / dt, transform.right) / Mathf.Max(runSpeed, 0.1f) : 0;
                float roll = -stride * runRollDegrees * runBlend
                    - Mathf.Clamp(smoothedTurn / 120f, -1f, 1f) * turnLeanDegrees * runBlend
                    - localStrafe * strafeLeanDegrees * runBlend;
                float nod = Mathf.Cos(gaitPhase * 2f) * runPitchDegrees * runBlend;
                gaitPivot.localRotation = Quaternion.Slerp(gaitPivot.localRotation,
                    gaitRotationOrigin * Quaternion.Euler(nod, 0, roll), blend);
            }
            if (viewCamera)
            {
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, baseFov + runFovIncrease * runBlend,
                    1f - Mathf.Exp(-7f * dt));
            }
        }

        void EmitFootstep()
        {
            StepCount++;
            LastNoiseRadius = IsRunning ? runHearingRadius : walkHearingRadius;
            if (footsteps && footstepClips != null && footstepClips.Length > 0)
            {
                var clip = footstepClips[clipIndex++ % footstepClips.Length];
                if (clip) footsteps.PlayOneShot(clip, IsRunning ? runVolume : walkVolume);
            }
            Footstep?.Invoke(transform.position, LastNoiseRadius);
            FootstepNoise.Emit(this, transform.position, LastNoiseRadius, IsRunning);
        }

        public void Teleport(Vector3 position)
        {
            if (!body) body = GetComponent<CharacterController>();
            body.enabled = false;
            transform.position = position;
            body.enabled = true;
            velocity = Vector3.zero;
            verticalSpeed = stepProgress = gaitPhase = ActualSpeed = runBlend = smoothedTurn = 0;
            IsRunning = false;
            if (gaitPivot) gaitPivot.localPosition = gaitOrigin;
            if (gaitPivot) gaitPivot.localRotation = gaitRotationOrigin;
        }

        static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        void OnApplicationFocus(bool focused) { if (!focused) { velocity = Vector3.zero; SetCursor(false); } }
        void OnDisable() { velocity = Vector3.zero; SetCursor(false); }
    }
}
