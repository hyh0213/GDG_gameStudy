using UnityEngine;
using Yeeun.Effects;

namespace Yeeun.Player
{
    public sealed class PlayerImpactFeedback : MonoBehaviour
    {
        public CameraShaker impactPivot;
        [Range(0, 1)] public float effectScale = 1f;
        public void Hit() { if (impactPivot) impactPivot.Shake(0.055f * effectScale, 0.24f); }
        public void HeavyDoor() { if (impactPivot) impactPivot.Shake(0.035f * effectScale, 0.32f); }
    }
}
