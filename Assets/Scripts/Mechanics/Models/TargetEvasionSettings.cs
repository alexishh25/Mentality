using System;
using UnityEngine;
namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class TargetEvasionSettings
    {
        public bool Enabled = true;
        [Range(0f, 1f)] public float ReactiveFraction = 0.65f;
        [Range(0f, 1f)] public float ActivationFrenzy = 0.3f;
        [Range(1f, 25f)] public float MaximumAngleDegrees = 10f;
        [Range(0.5f, 8f)] public float OscillationHz = 3f;
        [Min(0.01f)] public float ReleaseSeconds = 0.25f;
    }
}
