using System;

namespace Gameplay.Rhythm.Services
{
    public sealed class ScreamerTimeline
    {
        private double _until = double.NegativeInfinity;
        public void Trigger(double songTime, double duration)
        {
            if (double.IsNaN(songTime) || double.IsInfinity(songTime)
                || double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            // Una nueva colision renueva el mismo overlay, sin acumular pantallas.
            _until = songTime + duration;
        }
        public bool IsVisible(double songTime) => songTime < _until;
        public void Clear() => _until = double.NegativeInfinity;
    }
}
