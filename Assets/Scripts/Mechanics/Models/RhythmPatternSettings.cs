using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    public enum RhythmDifficulty { Easy, Normal, Hard }
    public enum RhythmPattern { Random, CenterPulse, Alternating, Sweep, Mirror }

    [Serializable]
    public sealed class RhythmPatternSettings
    {
        public bool Enabled = true;
        public RhythmDifficulty Difficulty = RhythmDifficulty.Normal;
        [Min(4), Tooltip("Cantidad de objetivos por frase jugable. No representa un compas detectado.")]
        public int PhraseLength = 8;
        [Min(0.01f), Tooltip("Una pausa mayor a este tiempo reinicia la secuencia espacial.")]
        public float PhraseResetGapSeconds = 2f;
    }

    public sealed class RhythmDifficultyProfile
    {
        public double IntervalMultiplier { get; }
        public int MaximumActiveTargets { get; }
        public float SideAngleDegrees { get; }

        private RhythmDifficultyProfile(double multiplier, int maximum, float angle)
        { IntervalMultiplier = multiplier; MaximumActiveTargets = maximum; SideAngleDegrees = angle; }

        public static RhythmDifficultyProfile For(RhythmDifficulty difficulty)
        {
            switch (difficulty)
            {
                case RhythmDifficulty.Easy: return new RhythmDifficultyProfile(1.5d, 3, 30f);
                case RhythmDifficulty.Normal: return new RhythmDifficultyProfile(1d, 5, 45f);
                case RhythmDifficulty.Hard: return new RhythmDifficultyProfile(0.8d, 7, 60f);
                default: throw new ArgumentOutOfRangeException(nameof(difficulty));
            }
        }
    }
}
