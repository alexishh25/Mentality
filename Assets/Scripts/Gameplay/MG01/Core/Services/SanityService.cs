using System;
using UnityEngine;

namespace Mentality.Gameplay.MG01
{
    // 100 de cordura, -10 por impacto real, sin regeneracion (GDD v0.2).
    public sealed class SanityService : ISanityService
    {
        private readonly float _damagePerHit;

        public float Current { get; private set; }
        public float Max { get; }
        public float Normalized => Current / Max;
        public bool IsDepleted { get; private set; }

        public event Action<float, float> Changed;
        public event Action Depleted;

        public SanityService(MG01ConfigSO config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Max = Mathf.Max(1f, config.MaxSanity);
            _damagePerHit = Mathf.Max(0f, config.DamagePerHit);
            Current = Max;
        }

        public void ApplyHit()
        {
            if (IsDepleted) return;

            Current = Mathf.Clamp(Current - _damagePerHit, 0f, Max);
            Changed?.Invoke(Current, Max);

            if (Current <= 0f)
            {
                IsDepleted = true;
                Depleted?.Invoke();
            }
        }

        public void Reset()
        {
            Current = Max;
            IsDepleted = false;
            Changed?.Invoke(Current, Max);
        }
    }
}
