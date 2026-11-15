using System;
using UnityEngine;
using UnityEngine.Events;
using Minseo.MazeItems;

namespace Yeeun.GasMask
{
    /// <summary>
    /// Player health with no number on screen: the gas mask shows it as lens cracks,
    /// breathing and heartbeat (<see cref="GasMaskHUD"/>, <see cref="GasMaskAudio"/>).
    /// Implements Minseo's <see cref="IHealingTarget"/> so the team's health kit works.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IHealingTarget
    {
        [SerializeField, Min(1)] int maxHealth = 100;

        public UnityEvent<int> onDamaged = new UnityEvent<int>();
        public UnityEvent<int> onHealed = new UnityEvent<int>();
        public UnityEvent onDied = new UnityEvent();

        /// <summary>(this, change). Negative change = damage.</summary>
        public event Action<PlayerHealth, int> Changed;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        /// <summary>0 = full health, 1 = dead.</summary>
        public float Damage01 => 1f - (float)Current / maxHealth;

        void Awake() => Current = maxHealth;

        public void Damage(int amount)
        {
            if (IsDead || amount <= 0) return;
            int before = Current;
            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(this, Current - before);
            onDamaged.Invoke(before - Current);
            if (IsDead) onDied.Invoke();
        }

        public bool TryHeal(int amount)
        {
            if (IsDead || amount <= 0 || Current >= maxHealth) return false;
            int before = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(this, Current - before);
            onHealed.Invoke(Current - before);
            return true;
        }

        /// <summary>UnityEvent-friendly heal.</summary>
        public void Heal(int amount) => TryHeal(amount);
    }
}
