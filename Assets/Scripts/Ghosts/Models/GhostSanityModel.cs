using System;

namespace Gameplay.Ghosts.Models
{
    public sealed class GhostSanityModel
    {
        private readonly float _maximum;

        public float Current { get; private set; }
        public float Maximum => _maximum;
        public float Normalized => _maximum <= 0f ? 0f : Current / _maximum;

        public GhostSanityModel(GhostFeatureSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _maximum = settings.MaximumSanity;
            Current = _maximum;
        }

        public void Lose(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Current = Math.Max(0f, Current - amount);
        }

        public void Reset() => Current = _maximum;
    }
}
