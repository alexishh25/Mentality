using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class BullicioSettings
    {
        public bool Enabled = true;
        public AudioClip Clip;
        [Min(0.1f)] public float DurationSeconds = 6f;
        [Range(0f, 1f)] public float Volume = 0.85f;
        [Range(0f, 1f)] public float DistortionLevel = 0.45f;
        [Range(0f, 1f)]
        [Tooltip("Probabilidad adicional de palabras falsas mientras suena el bullicio; el total se limita a 1.")]
        public float FalseWordProbabilityBonus = 0.18f;
    }
}
