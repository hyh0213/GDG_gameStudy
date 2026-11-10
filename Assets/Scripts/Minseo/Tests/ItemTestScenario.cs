using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Minseo.MazeItems.Tests
{
    public sealed class ItemTestScenario : MonoBehaviour
    {
        [SerializeField] private ItemTestPlayer player;
        [SerializeField] private GameObject firstKit;
        [SerializeField] private GameObject secondKit;
        [SerializeField] private GameObject firstBeacon;
        [SerializeField] private GameObject secondBeacon;

        private bool started;
        private string result = "Running item checks...";

        private void OnGUI()
        {
            var bounds = new Rect(12, 64, 245, 25);
            if (started)
                GUI.Box(bounds, result);
            else if (GUI.Button(bounds, "Run item checks (fresh Play Mode)"))
            {
                started = true;
                StartCoroutine(RunChecks());
            }
        }

        private IEnumerator RunChecks()
        {
            try
            {
                Require(firstKit != null && secondKit != null && firstBeacon != null && secondBeacon != null &&
                    player.Health == ItemTestPlayer.MaxHealth && !player.Items.HasBeacon, "Start in a fresh test scene.");
                player.enabled = false;
                player.Items.enabled = false;

                Vector3 spawn = player.transform.position;
                Vector3 firstKitPosition = FloorPosition(firstKit.transform.position);
                MovePlayer(firstKitPosition, Vector3.forward);
                yield return new WaitForFixedUpdate();
                yield return null;
                Require(firstKit != null && player.Health == 100, "A full-health player keeps the kit on the floor.");

                MovePlayer(spawn, Vector3.forward);
                yield return new WaitForFixedUpdate();
                yield return null;
                player.Damage(45);
                MovePlayer(firstKitPosition, Vector3.forward);
                yield return new WaitForFixedUpdate();
                yield return null;
                Require(firstKit == null && player.Health == 85, "The first kit restores exactly 30 HP and disappears.");

                MovePlayer(FloorPosition(secondKit.transform.position), Vector3.forward);
                yield return new WaitForFixedUpdate();
                yield return null;
                Require(secondKit == null && player.Health == 100, "The second kit caps health at 100 HP.");

                MovePlayer(FloorPosition(firstBeacon.transform.position), Vector3.forward);
                yield return new WaitForFixedUpdate();
                yield return null;
                Require(firstBeacon == null && player.Items.HasBeacon, "The first beacon is collected.");

                MovePlayer(FloorPosition(secondBeacon.transform.position), Vector3.forward);
                yield return new WaitForFixedUpdate();
                yield return null;
                Require(secondBeacon != null && player.Items.HasBeacon, "A second beacon stays on the floor.");

                MovePlayer(new Vector3(0, 2, 0), Vector3.forward);
                Require(!player.Items.TryPlaceBeacon() && player.Items.HasBeacon, "A failed placement keeps the beacon.");

                MovePlayer(Vector3.zero, Vector3.right);
                Keyboard keyboard = Keyboard.current;
                Require(keyboard != null, "A keyboard is required to test placement with B.");
                player.Items.enabled = true;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                player.Items.enabled = false;
                Require(!player.Items.HasBeacon, "Pressing B on the floor places one beacon.");
                Require(!player.Items.TryPlaceBeacon(), "An empty inventory cannot place a beacon.");
                player.SetScan(false);

                CharacterController controller = player.GetComponent<CharacterController>();
                for (int step = 0; step < 12; step++)
                {
                    controller.Move(Vector3.right * 0.5f);
                    yield return null;
                }
                Require(player.transform.position.x > 5, "The east branch was explored.");

                AimAtMarker();
                yield return new WaitForEndOfFrame();
                Require(BeaconPixelVisible(), "The marker is visible from the east branch with scanning off.");
                for (int step = 0; step < 8; step++)
                {
                    controller.Move(Vector3.left * 0.5f);
                    yield return null;
                }
                yield return new WaitForEndOfFrame();
                Require(player.transform.position.x < 3 && BeaconPixelVisible(), "The beacon identifies the junction on return.");

                MovePlayer(new Vector3(0, 0, 4), Vector3.back);
                AimAtMarker();
                yield return new WaitForEndOfFrame();
                Require(!BeaconPixelVisible(), "The opaque north wall hides the beacon with scanning off.");

                MovePlayer(new Vector3(4, 0, 0), Vector3.left);
                result = "PASS - See Console for details";
                Debug.Log("[Minseo Item Checks] PASS: healing, one-beacon limit, placement, return route, wall occlusion.");
            }
            finally
            {
                if (!result.StartsWith("PASS", StringComparison.Ordinal))
                    result = "FAIL - See Console for details";
                player.Items.enabled = true;
                player.enabled = true;
            }
        }

        private static Vector3 FloorPosition(Vector3 position) => new Vector3(position.x, 0, position.z);

        private void MovePlayer(Vector3 position, Vector3 forward)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            controller.enabled = true;
            player.View.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
        }

        private static Vector3 MarkerCenter => new Vector3(0, 0.22f, 0);

        private void AimAtMarker() => player.View.transform.LookAt(MarkerCenter);

        private bool BeaconPixelVisible()
        {
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Vector3 point = player.View.WorldToViewportPoint(MarkerCenter);
                int x = Mathf.FloorToInt(point.x * capture.width);
                int y = Mathf.FloorToInt(point.y * capture.height);
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        Color pixel = capture.GetPixel(x + dx, y + dy);
                        if (pixel.g > 0.6f && pixel.r < 0.35f && pixel.b < 0.6f)
                            return true;
                    }
                }
                return false;
            }
            finally
            {
                Destroy(capture);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("[Minseo Item Checks] FAIL: " + message);
        }
    }
}
