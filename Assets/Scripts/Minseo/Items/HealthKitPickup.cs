using UnityEngine;

namespace Minseo.MazeItems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class HealthKitPickup : MonoBehaviour
    {
        private const int HealAmount = 30;
        private bool consumed;

        private void OnTriggerEnter(Collider other)
        {
            if (consumed || !isActiveAndEnabled)
                return;

            PlayerItems player = other.GetComponentInParent<PlayerItems>();
            if (player == null || !player.TryUseHealthKit(HealAmount))
                return;

            consumed = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
