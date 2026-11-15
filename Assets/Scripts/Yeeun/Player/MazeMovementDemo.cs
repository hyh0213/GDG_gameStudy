using UnityEngine;
using UnityEngine.InputSystem;

namespace Yeeun.Player
{
    public sealed class MazeMovementDemo : MonoBehaviour
    {
        public MazePlayerController player;
        public PlayerImpactFeedback impacts;
        public FootstepHearingReceiver receiver;
        void Update()
        {
            var k = Keyboard.current;
            if (k == null || !player.ReadPlayerInput) return;
            if (k.hKey.wasPressedThisFrame) impacts.Hit();
            if (k.jKey.wasPressedThisFrame) impacts.HeavyDoor();
            if (k.rKey.wasPressedThisFrame) player.Teleport(new Vector3(0, 0.05f, 0));
        }
        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 530, 125), "Yeeun / Movement & Camera Test");
            GUI.Label(new Rect(24, 40, 510, 90),
                "Click: mouse lock | Esc: release | WASD: move | Shift + W: run\n" +
                "H: hit impact | J: heavy-door impact | R: return to start\n" +
                $"Speed {player.ActualSpeed:F2} m/s | Steps {player.StepCount} | Noise {player.LastNoiseRadius:F1} m\n" +
                $"Hearing probe: {receiver.HeardCount} events | Walk 4 m / Run 9 m");
        }
    }
}
