using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    // Se prepara antes de reproducir. Se consulta en hitTime, no en el instante anticipado del spawn.
    public sealed class MusicalActivityProfile
    {
        private readonly MusicalCurve _intensity;
        private readonly MusicalCurve _transitions;
        private readonly double[] _activeStarts;
        private readonly double[] _activeEnds;
        private readonly double _minimumInterval;
        private readonly double _quietInterval;
        private readonly double _quietIntensity;
        private readonly double _fullIntensity;
        private readonly double _transitionRelief;
        private readonly bool _frenzy;
        private readonly double _frenzyStart, _frenzyFull, _maximumFrenzy, _maximumSpeed, _frenzyInterval;
        private readonly MusicalCurve _speed;
        private readonly double[] _motion;
        public int ExtraTargets { get; }
        public int ActiveIntervalCount => _activeStarts.Length;
        public ManualIntensityProfile ManualIntensity { get; }

        public MusicalActivityProfile(MusicalMapData map, double minimumInterval, MusicalActivitySettings settings,
            ManualIntensitySettings manualSettings = null)
        {
            MusicalMapLoader.Validate(map);
            ValidateSettings(settings, minimumInterval);
            if (manualSettings != null && manualSettings.Enabled)
                ManualIntensity = new ManualIntensityProfile(map.Metadata.DurationSeconds, manualSettings);
            _minimumInterval = minimumInterval;
            _quietInterval = settings.QuietSpawnInterval;
            _quietIntensity = settings.QuietIntensity;
            _fullIntensity = settings.FullDensityIntensity;
            _transitionRelief = settings.TransitionRelief;
            _frenzy = settings.EnableFrenzy;
            _frenzyStart = settings.FrenzyStartIntensity;
            _frenzyFull = settings.FullFrenzyIntensity;
            _maximumFrenzy = settings.MaximumFrenzy;
            _maximumSpeed = settings.MaximumSpeedMultiplier;
            _frenzyInterval = settings.FrenzyIntervalMultiplier;
            ExtraTargets = _frenzy ? settings.FrenzyExtraTargets : 0;
            _intensity = Smooth(map.Curves.Intensity, settings.SmoothingSeconds);
            _transitions = Smooth(map.Curves.Transitions, settings.SmoothingSeconds);
            var speedTimes = new List<double> { 0d };
            foreach (double time in _intensity.Times) if (time > 0d) speedTimes.Add(time);
            if (ManualIntensity != null)
            {
                var speedIntervals = new List<MusicalActiveInterval>(manualSettings.Intervals);
                foreach(var section in manualSettings.Sections) speedIntervals.Add(new MusicalActiveInterval {Start=section.Start,End=section.End});
                foreach (var interval in speedIntervals)
                {
                    speedTimes.Add(interval.Start); speedTimes.Add(interval.End);
                    double ramp = Math.Min(manualSettings.TransitionSeconds, (interval.End - interval.Start) / 2d);
                    for (int step = 1; step <= 20; step++)
                    {
                        speedTimes.Add(interval.Start + ramp * step / 20d);
                        speedTimes.Add(interval.End - ramp * step / 20d);
                    }
                }
                speedTimes.Sort();
                for (int i = speedTimes.Count - 1; i > 0; i--) if (speedTimes[i] == speedTimes[i - 1]) speedTimes.RemoveAt(i);
            }
            var speedValues = new double[speedTimes.Count];
            _motion = new double[speedTimes.Count];
            for (int i = 0; i < speedValues.Length; i++)
            {
                speedValues[i] = _frenzy && ManualIntensity != null && ManualIntensity.HasSections
                    ? ManualIntensity.SpeedAt(speedTimes[i]) : 1d + (_maximumSpeed - 1d) * FrenzyAt(speedTimes[i]);
                if (i > 0) _motion[i] = _motion[i - 1]
                    + (speedTimes[i] - speedTimes[i - 1]) * (speedValues[i] + speedValues[i - 1]) * 0.5d;
            }
            _speed = new MusicalCurve { Times = speedTimes.ToArray(), Values = speedValues };
            var starts = new List<double>();
            var ends = new List<double>();
            foreach (var interval in map.ActiveIntervals)
            {
                int last = ends.Count - 1;
                if (last >= 0 && interval.Start - ends[last] <= settings.MergeSilenceGapsSeconds)
                    ends[last] = interval.End;
                else { starts.Add(interval.Start); ends.Add(interval.End); }
            }
            _activeStarts = starts.ToArray();
            _activeEnds = ends.ToArray();
        }

        public static void ValidateSettings(MusicalActivitySettings settings, double minimumInterval)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!IsFinite(minimumInterval) || minimumInterval <= 0
                || !IsFinite(settings.SmoothingSeconds) || settings.SmoothingSeconds <= 0
                || !IsFinite(settings.MergeSilenceGapsSeconds) || settings.MergeSilenceGapsSeconds < 0
                || !IsFinite(settings.QuietSpawnInterval) || settings.QuietSpawnInterval < minimumInterval
                || !IsFinite(settings.QuietIntensity) || settings.QuietIntensity < 0
                || !IsFinite(settings.FullDensityIntensity) || settings.FullDensityIntensity > 1
                || settings.FullDensityIntensity <= settings.QuietIntensity
                || !IsFinite(settings.TransitionRelief) || settings.TransitionRelief < 0 || settings.TransitionRelief > 1
                || !IsFinite(settings.FrenzyStartIntensity) || !IsFinite(settings.FullFrenzyIntensity)
                || settings.FrenzyStartIntensity < 0 || settings.FullFrenzyIntensity > 1 || settings.FullFrenzyIntensity <= settings.FrenzyStartIntensity
                || !IsFinite(settings.MaximumFrenzy) || settings.MaximumFrenzy < 0 || settings.MaximumFrenzy > 1
                || !IsFinite(settings.MaximumSpeedMultiplier) || settings.MaximumSpeedMultiplier < 1 || settings.MaximumSpeedMultiplier > 4
                || !IsFinite(settings.FrenzyIntervalMultiplier) || settings.FrenzyIntervalMultiplier < 0.25 || settings.FrenzyIntervalMultiplier > 1
                || settings.FrenzyExtraTargets < 0 || settings.FrenzyExtraTargets > 4)
                throw new ArgumentException("Configuracion de actividad musical invalida: revisa suavizado, pausas, intensidades e intervalos.", nameof(settings));
        }

        public bool IsActive(double songTime)
        {
            if (!IsFinite(songTime) || songTime < 0 || _activeStarts.Length == 0) return false;
            int index = Array.BinarySearch(_activeStarts, songTime);
            if (index < 0) index = ~index - 1;
            return index >= 0 && songTime <= _activeEnds[index];
        }

        public double IntensityAt(double songTime) => Sample(_intensity, songTime);
        public double TransitionAt(double songTime) => Sample(_transitions, songTime);

        public double FrenzyAt(double time)
        {
            if (!_frenzy || time < 0) return 0d;
            if (ManualIntensity != null) return ManualIntensity.HasSections ? ManualIntensity.FrenzyAt(time) : _maximumFrenzy * ManualIntensity.AmountAt(time);
            double amount = Clamp01((IntensityAt(time) - _frenzyStart) / (_frenzyFull - _frenzyStart));
            return Math.Min(_maximumFrenzy, amount * amount * (3d - 2d * amount));
        }

        public double DensityMultiplierAt(double time) => 1d + (_frenzyInterval - 1d) * FrenzyAt(time);
        public double MinimumIntervalMultiplier => _frenzy ? _frenzyInterval : 1d;
        public double SpeedMultiplierAt(double time) => time < 0 ? 1d : Sample(_speed, time);

        // Integral de velocidad: todos los objetos aceleran juntos con el tiempo musical actual.
        public double MotionTimeAt(double time)
        {
            if (!IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
            if (time <= 0) return time;
            int index = Array.BinarySearch(_speed.Times, time);
            if (index >= 0) return _motion[index];
            int next = ~index;
            int previous = next - 1;
            if (next >= _speed.Times.Length)
                return _motion[previous] + (time - _speed.Times[previous]) * _speed.Values[previous];
            double dt = time - _speed.Times[previous];
            double slope = (_speed.Values[next] - _speed.Values[previous]) / (_speed.Times[next] - _speed.Times[previous]);
            return _motion[previous] + dt * _speed.Values[previous] + 0.5d * slope * dt * dt;
        }

        public double SpawnTimeFor(double arrivalTime, double baseLead)
        {
            if (!IsFinite(arrivalTime) || arrivalTime < 0 || !IsFinite(baseLead) || baseLead < 0)
                throw new ArgumentOutOfRangeException(nameof(arrivalTime));
            double target = MotionTimeAt(arrivalTime) - baseLead;
            if (target <= 0) return target;
            double low = 0d, high = arrivalTime;
            for (int i = 0; i < 48; i++)
            {
                double middle = (low + high) * 0.5d;
                if (MotionTimeAt(middle) < target) low = middle; else high = middle;
            }
            return (low + high) * 0.5d;
        }

        public double RequiredIntervalAt(double songTime)
        {
            double activity = Clamp01((IntensityAt(songTime) - _quietIntensity) / (_fullIntensity - _quietIntensity));
            // Smoothstep evita cambios de pendiente en los extremos de la densidad.
            activity = activity * activity * (3d - 2d * activity);
            activity *= 1d - TransitionAt(songTime) * _transitionRelief;
            return Math.Max(_minimumInterval, _quietInterval + (_minimumInterval - _quietInterval) * activity);
        }

        private static MusicalCurve Smooth(MusicalCurve source, double seconds)
        {
            var values = new double[source.Values.Length];
            values[0] = source.Values[0];
            for (int i = 1; i < values.Length; i++)
            {
                double weight = 1d - Math.Exp(-(source.Times[i] - source.Times[i - 1]) / seconds);
                values[i] = values[i - 1] + weight * (source.Values[i] - values[i - 1]);
            }
            return new MusicalCurve { Times = (double[])source.Times.Clone(), Values = values };
        }

        private static double Sample(MusicalCurve curve, double time)
        {
            if (!IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
            if (time <= curve.Times[0]) return curve.Values[0];
            int last = curve.Times.Length - 1;
            if (time >= curve.Times[last]) return curve.Values[last];
            int index = Array.BinarySearch(curve.Times, time);
            if (index >= 0) return curve.Values[index];
            int next = ~index;
            double amount = (time - curve.Times[next - 1]) / (curve.Times[next] - curve.Times[next - 1]);
            return curve.Values[next - 1] + amount * (curve.Values[next] - curve.Values[next - 1]);
        }

        private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
