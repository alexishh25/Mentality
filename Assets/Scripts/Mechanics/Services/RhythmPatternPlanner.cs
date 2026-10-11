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
            RhythmPatternSettings settings, MusicalPhraseProfile musicalPhrases = null)
        {
            ValidateSettings(settings);
            var difficulty = RhythmDifficultyProfile.For(settings.Difficulty);
            var events = new MusicalSpawnEvent[times.Count];
            int step = 0;
            int phrase = 0;
            RhythmPattern pattern = RhythmPattern.CenterPulse;
            int previousMusicalPhrase = -2;
            MusicalPhrasePressure previousPressure = MusicalPhrasePressure.Normal;
            for (int i = 0; i < times.Count; i++)
            {
                int musicalPhrase = musicalPhrases == null ? -1 : musicalPhrases.PhraseAt(times[i]);
                if (i == 0 || (musicalPhrases == null ? step >= settings.PhraseLength : musicalPhrase != previousMusicalPhrase)
                    || (musicalPhrases != null && musicalPhrases.At(times[i]) != previousPressure)
                    || times[i] - times[i - 1] > settings.PhraseResetGapSeconds)
                {
                    step = 0;
                    double intensity = activity == null ? 0.4d : activity.IntensityAt(times[i]);
                    double transition = activity == null ? 0d : activity.TransitionAt(times[i]);
                    pattern = intensity < 0.25d || transition >= 0.3d ? RhythmPattern.CenterPulse
                        : settings.Difficulty == RhythmDifficulty.Easy ? RhythmPattern.Alternating
                        : intensity >= 0.55d ? (settings.Difficulty == RhythmDifficulty.Hard ? RhythmPattern.Mirror : RhythmPattern.Sweep)
                        : phrase % 2 == 0 ? RhythmPattern.Alternating : RhythmPattern.Sweep;
                    if (musicalPhrases != null)
                    {
                        var pressure = musicalPhrases.At(times[i]);
                        if (pressure == MusicalPhrasePressure.Recovery) pattern = RhythmPattern.CenterPulse;
                        else if (pressure == MusicalPhrasePressure.Pressure)
                            pattern = musicalPhrase % 2 == 0 ? RhythmPattern.Alternating
                                : musicalPhrases.ChallengingPatterns ? RhythmPattern.Mirror : RhythmPattern.Sweep;
                    }
                    phrase++;
                }
                int[] sequence = pattern == RhythmPattern.CenterPulse ? Center
                    : pattern == RhythmPattern.Alternating ? Alternating : pattern == RhythmPattern.Sweep ? Sweep : Mirror;
                float reach = musicalPhrases != null && musicalPhrases.At(times[i]) == MusicalPhrasePressure.Recovery ? 0.5f : 1f;
                if (musicalPhrases != null && musicalPhrases.ChallengingPatterns
                    && musicalPhrases.At(times[i]) == MusicalPhrasePressure.Pressure) reach = 1.25f;
                events[i] = new MusicalSpawnEvent(times[i], sequence[step % sequence.Length] * difficulty.SideAngleDegrees * reach, pattern);
                previousMusicalPhrase = musicalPhrase;
                if (musicalPhrases != null) previousPressure = musicalPhrases.At(times[i]);
                step++;
            }
            return events;
        }
    }
}
