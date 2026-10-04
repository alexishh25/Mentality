using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public static class RhythmPatternPlanner
    {
        private static readonly int[] Center = { 0, -1, 0, 1 };
        private static readonly int[] Alternating = { -1, 1, -1, 1 };
        private static readonly int[] Sweep = { -1, 0, 1, 0 };
        private static readonly int[] Mirror = { -1, 0, 1, 0, 1, 0, -1, 0 };

        public static void ValidateSettings(RhythmPatternSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _ = RhythmDifficultyProfile.For(settings.Difficulty);
            if (settings.PhraseLength < 4 || float.IsNaN(settings.PhraseResetGapSeconds)
                || float.IsInfinity(settings.PhraseResetGapSeconds) || settings.PhraseResetGapSeconds <= 0)
                throw new ArgumentException("La frase necesita al menos 4 objetivos y una pausa de reinicio positiva.", nameof(settings));
        }

        public static MusicalSpawnEvent[] Build(IReadOnlyList<double> times, MusicalActivityProfile activity,
            RhythmPatternSettings settings)
        {
            ValidateSettings(settings);
            var difficulty = RhythmDifficultyProfile.For(settings.Difficulty);
            var events = new MusicalSpawnEvent[times.Count];
            int step = 0;
            int phrase = 0;
            RhythmPattern pattern = RhythmPattern.CenterPulse;
            for (int i = 0; i < times.Count; i++)
            {
                if (i == 0 || step >= settings.PhraseLength || times[i] - times[i - 1] > settings.PhraseResetGapSeconds)
                {
                    step = 0;
                    double intensity = activity == null ? 0.4d : activity.IntensityAt(times[i]);
                    double transition = activity == null ? 0d : activity.TransitionAt(times[i]);
                    pattern = intensity < 0.25d || transition >= 0.3d ? RhythmPattern.CenterPulse
                        : settings.Difficulty == RhythmDifficulty.Easy ? RhythmPattern.Alternating
                        : intensity >= 0.55d ? (settings.Difficulty == RhythmDifficulty.Hard ? RhythmPattern.Mirror : RhythmPattern.Sweep)
                        : phrase % 2 == 0 ? RhythmPattern.Alternating : RhythmPattern.Sweep;
                    phrase++;
                }
                int[] sequence = pattern == RhythmPattern.CenterPulse ? Center
                    : pattern == RhythmPattern.Alternating ? Alternating : pattern == RhythmPattern.Sweep ? Sweep : Mirror;
                events[i] = new MusicalSpawnEvent(times[i], sequence[step % sequence.Length] * difficulty.SideAngleDegrees, pattern);
                step++;
            }
            return events;
        }
    }
}
