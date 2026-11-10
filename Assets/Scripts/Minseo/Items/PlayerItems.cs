using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Minseo.MazeItems
{
    public interface IHealingTarget
    {
        // Return true only when health actually increased, capped at maximum health.
        bool TryHeal(int amount);
    }

    [DisallowMultipleComponent]
    public sealed class PlayerItems : MonoBehaviour
    {
        [Tooltip("A component that implements IHealingTarget.")]
        [SerializeField] private MonoBehaviour healingTarget;
        [Tooltip("Assign the point at the player's feet. Defaults to this object's origin.")]
        [SerializeField] private Transform feet;
        [SerializeField] private LayerMask floorLayers = Physics.DefaultRaycastLayers;
        [Tooltip("Set to None when the player controller calls TryPlaceBeacon through its own input action.")]
        [SerializeField] private Key placementKey = Key.B;

        private GameObject heldMarkerPrefab;

        public bool HasBeacon => heldMarkerPrefab != null;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (HasBeacon && keyboard != null && placementKey != Key.None && keyboard[placementKey].wasPressedThisFrame)
                TryPlaceBeacon();
        }

        public bool TryUseHealthKit(int amount)
        {
            return healingTarget != null && healingTarget is IHealingTarget target && target.TryHeal(amount);
        }

        public bool TryCollectBeacon(GameObject markerPrefab)
        {
            if (HasBeacon || markerPrefab == null)
                return false;

            heldMarkerPrefab = markerPrefab;
            return true;
        }

        public bool TryPlaceBeacon()
        {
            if (!HasBeacon)
                return false;

            Vector3 footPosition = feet != null ? feet.position : transform.position;
            Vector3 origin = footPosition + Vector3.up * 0.15f;
            if (!gameObject.scene.GetPhysicsScene().Raycast(origin, Vector3.down, out RaycastHit hit, 0.45f,
                    floorLayers, QueryTriggerInteraction.Ignore) ||
                hit.normal.y < 0.7f || hit.collider.transform.IsChildOf(transform))
                return false;

            GameObject marker = Instantiate(heldMarkerPrefab, hit.point + Vector3.up * 0.02f, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(marker, gameObject.scene);
            heldMarkerPrefab = null;
            return true;
        }
    }
}
