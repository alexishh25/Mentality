using System;
using UnityEngine;
namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class ManualIntensitySettings
    {
        public bool Enabled;
        [Range(0.01f, 2f)] public float TransitionSeconds = 0.5f;
        public MusicalActiveInterval[] Intervals = Array.Empty<MusicalActiveInterval>();
        public ManualDifficultySection[] Sections = Array.Empty<ManualDifficultySection>();
    }
    [Serializable]
    public sealed class ManualDifficultySection
    {
        public double Start, End;
        [Range(1f, 4f)] public float SpeedMultiplier = 2f;
        [Range(0f, 1f)] public float Frenzy = 0.65f;
        [Range(3, 9)] public int MaximumTargets = 6;
        [Range(0f, 1f)] public float FalseProbability = 0.25f;
        [Range(0.15f, 0.3f)] public float MinimumInterval = 0.22f;
        [Range(1, 8)] public int AttackEveryBeats = 4;
    }
}
