using UnityEngine;
using UnityEngine.Events;

namespace Yeeun.Player
{
    public sealed class FootstepHearingReceiver : MonoBehaviour
    {
        [Tooltip("Connect the monster's investigate-position method here. Radius-only; walls do not block sound.")]
        public UnityEvent<Vector3> onHeard = new UnityEvent<Vector3>();
        public int HeardCount { get; private set; }
        public Vector3 LastHeardPosition { get; private set; }
        public float LastHeardTime { get; private set; } = -100f;
        void OnEnable() => FootstepNoise.Emitted += Hear;
        void OnDisable() => FootstepNoise.Emitted -= Hear;
        void Hear(MazePlayerController source, Vector3 position, float radius, bool running)
        {
            if (!source || source.gameObject.scene != gameObject.scene ||
                (transform.position - position).sqrMagnitude > radius * radius) return;
            HeardCount++;
            LastHeardPosition = position;
            LastHeardTime = Time.time;
            onHeard.Invoke(position);
        }
    }
}
