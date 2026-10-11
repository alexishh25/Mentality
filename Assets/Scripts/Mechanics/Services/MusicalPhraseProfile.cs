using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public enum MusicalPhrasePressure { Recovery, Normal, Pressure }

    // Frases jugables de beats detectados; no pretende identificar compases o estribillos.
    public sealed class MusicalPhraseProfile
    {
        private readonly double[] _beats;
        private readonly MusicalPhrasePressure[] _levels;
        private readonly int _phraseBeats;
        public bool ChallengingPatterns { get; }
        private readonly ManualIntensityProfile _manual;

        public MusicalPhraseProfile(MusicalMapData map, MusicalActivityProfile activity, BeatCadenceSettings settings)
        {
            if (settings.PhraseBeats < 4 || settings.PhraseBeats > 16
                || settings.PressureExtraBeatEvery < 2 || settings.PressureExtraBeatEvery > 8
                || !Unit(settings.PressureIntensity) || !Unit(settings.PressureReleaseIntensity)
                || settings.PressureReleaseIntensity >= settings.PressureIntensity || !Unit(settings.RecoveryTransition)
                || !Unit(settings.MinimumAttackIntensity))
                throw new ArgumentOutOfRangeException(nameof(settings), "Umbrales de frase musical invalidos.");
            _beats = map.Events.Beats;
            _manual = activity.ManualIntensity;
            ChallengingPatterns = settings.ChallengingPressurePatterns;
            _phraseBeats = settings.PhraseBeats;
            _levels = new MusicalPhrasePressure[(_beats.Length + _phraseBeats - 1) / _phraseBeats];
            bool pressure = false;
            for (int phrase = 0; phrase < _levels.Length; phrase++)
            {
                int start = phrase * _phraseBeats;
                int end = Math.Min(_beats.Length, start + _phraseBeats);
                double intensity = 0d, transition = 0d;
                bool active = false;
                for (int i = start; i < end; i++)
                {
                    intensity += activity.IntensityAt(_beats[i]);
                    transition = Math.Max(transition, activity.TransitionAt(_beats[i]));
                    active |= activity.IsActive(_beats[i]);
                }
                intensity /= end - start;
                bool recovery = !active || intensity < settings.MinimumAttackIntensity || transition >= settings.RecoveryTransition;
                if (start > 0 && _beats[start] - _beats[start - 1] > 2d) pressure = false;
                pressure = !recovery && intensity >= (pressure ? settings.PressureReleaseIntensity : settings.PressureIntensity);
                _levels[phrase] = recovery ? MusicalPhrasePressure.Recovery
                    : pressure ? MusicalPhrasePressure.Pressure : MusicalPhrasePressure.Normal;
            }
        }

        private static bool Unit(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;

        public int PhraseAt(double time)
        {
            int index = Array.BinarySearch(_beats, time);
            if (index < 0) index = ~index - 1;
            return index < 0 ? -1 : index / _phraseBeats;
        }

        public MusicalPhrasePressure At(double time)
        {
            if (_manual != null && _manual.Contains(time)) return MusicalPhrasePressure.Pressure;
            int phrase = PhraseAt(time);
            var level = phrase < 0 || phrase >= _levels.Length ? MusicalPhrasePressure.Recovery : _levels[phrase];
            return _manual != null && level == MusicalPhrasePressure.Pressure ? MusicalPhrasePressure.Normal : level;
        }
    }
}
