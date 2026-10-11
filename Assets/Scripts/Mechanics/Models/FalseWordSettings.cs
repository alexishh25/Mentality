using System;
using UnityEngine;
namespace Gameplay.Rhythm.Models
{
    public enum WordTargetKind { Normal, Screamer, Bullicio, Glitch }

    [Serializable]
    public sealed class FalseWordSettings
    {
        [Tooltip("Activar cuando la distorsion y los efectos esten listos.")]
        public bool Enabled = false;
        [Min(0.02f)] public float DistortionIntervalSeconds = 0.12f;
        [Range(0f, 1f)] public float QuietProbability = 0.1f;
        [Range(0f, 1f)] public float FrenzyProbability = 0.28f;
        [Min(0f)] public float ScreamerWeight = 1f;
        [Min(0f)] public float BullicioWeight = 1f;
        [Min(0f)] public float GlitchWeight = 1f;
    }
}
