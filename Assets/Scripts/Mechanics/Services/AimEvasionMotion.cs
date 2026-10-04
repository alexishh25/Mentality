using System;
using Gameplay.Rhythm.Models;
namespace Gameplay.Rhythm.Services
{
    public sealed class AimEvasionMotion
    {
        private readonly double _angle, _frequency, _release, _threshold, _sign;
        private readonly bool _reactive;
        private bool _aimed;
        private double _start, _releaseStart, _releaseAngle;
        public bool IsReactive => _reactive;

        public static void Validate(TargetEvasionSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!Finite(settings.ReactiveFraction) || settings.ReactiveFraction < 0 || settings.ReactiveFraction > 1
                || !Finite(settings.ActivationFrenzy) || settings.ActivationFrenzy < 0 || settings.ActivationFrenzy > 1
                || !Finite(settings.MaximumAngleDegrees) || settings.MaximumAngleDegrees < 1 || settings.MaximumAngleDegrees > 25
                || !Finite(settings.OscillationHz) || settings.OscillationHz < 0.5 || settings.OscillationHz > 8
                || !Finite(settings.ReleaseSeconds) || settings.ReleaseSeconds <= 0)
                throw new ArgumentException("Configuracion de evasion invalida.", nameof(settings));
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public AimEvasionMotion(TargetEvasionSettings settings, double seed)
        {
            Validate(settings);
            if (!Finite(seed)) throw new ArgumentOutOfRangeException(nameof(seed));
            uint hash = unchecked((uint)Math.Round(seed * 1000d));
            hash ^= hash >> 16; hash *= 0x7feb352d; hash ^= hash >> 15;
            _reactive = settings.Enabled && (hash % 10000) / 10000d < settings.ReactiveFraction;
            _sign = (hash & 1) == 0 ? -1d : 1d;
            _angle = settings.MaximumAngleDegrees; _frequency = settings.OscillationHz;
            _release = settings.ReleaseSeconds; _threshold = settings.ActivationFrenzy;
            _releaseStart = double.NegativeInfinity;
        }

        public void SetAimed(bool aimed, double time, double frenzy)
        {
            aimed = aimed && _reactive && frenzy >= _threshold;
            if (aimed == _aimed) return;
            if (aimed) _start = time;
            else { _releaseAngle = AngleAt(time, frenzy); _releaseStart = time; }
            _aimed = aimed;
        }

        public double AngleAt(double time, double frenzy)
        {
            if (!_reactive || frenzy < _threshold) return 0d;
            double strength = Math.Min(1d, Math.Max(0d, frenzy));
            if (_aimed)
                return _sign * _angle * strength * (0.45d + 0.55d * Math.Sin((time - _start) * 2d * Math.PI * _frequency));
            double fade = Math.Max(0d, 1d - (time - _releaseStart) / _release);
            return _releaseAngle * fade;
        }
    }
}
