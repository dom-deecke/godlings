using System;

namespace Godlings
{
    /// <summary>Shared health rules for Fi, regular enemies, and the boss.</summary>
    public sealed class Health
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsDead => Current == 0;
        public event Action<float> Changed;
        public event Action Died;

        public Health(float maximum)
        {
            if (maximum <= 0 || float.IsNaN(maximum) || float.IsInfinity(maximum))
                throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum = maximum;
            Current = maximum;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsDead) return;
            Current = Math.Max(0, Current - amount);
            Changed?.Invoke(Current);
            if (IsDead) Died?.Invoke();
        }
    }
}
