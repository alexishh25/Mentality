using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class GlitchSettings
    {
        public bool Enabled = true;
        [Min(0.1f)] public float DurationSeconds = 4f;
        [Range(0f, 1f)] public float Darkness = 0.22f;
        [Range(0f, 1f)] public float StaticOpacity = 0.18f;
        [Range(1f, 30f)] public float StaticFramesPerSecond = 12f;
    }
}
