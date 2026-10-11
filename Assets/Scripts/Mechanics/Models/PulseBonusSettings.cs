using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class PulseBonusSettings
    {
        public bool Enabled;
        [Range(0.01f, 0.2f), Tooltip("Margen antes y despues de un beat real para premiar el clic.")]
        public float WindowSeconds = 0.08f;
        [Min(1), Tooltip("Puntos extra por pulso, multiplicados por el combo actual.")]
        public int BonusPoints = 50;
    }
}
