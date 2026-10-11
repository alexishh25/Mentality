using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public sealed class PulseBonusProfile
    {
        private readonly double[] _beats;
        private readonly double _window;
        private readonly int _points;

        public PulseBonusProfile(MusicalMapData map, PulseBonusSettings settings)
        {
            MusicalMapLoader.Validate(map);
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (float.IsNaN(settings.WindowSeconds) || float.IsInfinity(settings.WindowSeconds)
                || settings.WindowSeconds < 0.01f || settings.WindowSeconds > 0.2f
                || settings.BonusPoints < 1 || settings.BonusPoints > int.MaxValue / RhythmScoreRules.MaximumMultiplier - RhythmScoreRules.HitPoints)
                throw new ArgumentOutOfRangeException(nameof(settings));
            _beats = settings.Enabled ? (double[])map.Events.Beats.Clone() : Array.Empty<double>();
            _window = settings.WindowSeconds;
            _points = settings.BonusPoints;
        }

        public int PointsAt(double songTime, out double? signedError)
        {
            if (double.IsNaN(songTime) || double.IsInfinity(songTime)) throw new ArgumentOutOfRangeException(nameof(songTime));
            signedError = null;
            if (songTime < 0 || _beats.Length == 0) return 0;
            int index = Array.BinarySearch(_beats, songTime);
            if (index < 0)
            {
                int next = ~index;
                index = next == 0 ? 0 : next == _beats.Length ? next - 1
                    : songTime - _beats[next - 1] <= _beats[next] - songTime ? next - 1 : next;
            }
            signedError = songTime - _beats[index];
            return Math.Abs(signedError.Value) <= _window + 1e-9d ? _points : 0;
        }
    }
}
