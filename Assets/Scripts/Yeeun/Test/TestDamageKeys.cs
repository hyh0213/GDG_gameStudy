using UnityEngine;
using UnityEngine.InputSystem;
using Yeeun.GasMask;

namespace Yeeun.Test
{
    /// <summary>Test-only keys until monsters and traps deal damage: H = hurt, J = heal.</summary>
    public class TestDamageKeys : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] int amount = 20;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || health == null) return;
            if (keyboard.hKey.wasPressedThisFrame) health.Damage(amount);
            if (keyboard.jKey.wasPressedThisFrame) health.Heal(amount);
        }
    }
}
