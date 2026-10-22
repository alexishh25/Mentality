using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class WordImpactBlurSettings
    {
        public bool Enabled = true;
        [Range(0.08f, 0.6f)] public float DurationSeconds = 0.28f;
        [Range(0.1f, 1f)] public float Radius = 0.7f;
    }
}
