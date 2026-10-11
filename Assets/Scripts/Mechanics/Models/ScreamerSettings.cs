using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class ScreamerSettings
    {
        public bool Enabled = true;
        public Texture2D Image;
        public Texture2D[] Images = Array.Empty<Texture2D>();
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 0.85f;
        [Min(0.05f)] public float DurationSeconds = 0.55f;
    }
}
