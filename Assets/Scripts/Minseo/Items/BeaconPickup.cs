using UnityEngine;

namespace Minseo.MazeItems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class BeaconPickup : MonoBehaviour
    {
        [SerializeField] private GameObject placedMarkerPrefab;
        private bool consumed;

        private void OnTriggerEnter(Collider other)
        {
            if (consumed || !isActiveAndEnabled)
                return;

            PlayerItems player = other.GetComponentInParent<PlayerItems>();
            if (player == null || !player.TryCollectBeacon(placedMarkerPrefab))
                return;

            consumed = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
