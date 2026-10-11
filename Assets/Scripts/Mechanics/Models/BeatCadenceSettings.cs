using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class BeatCadenceSettings
    {
        public bool Enabled;
        [Range(1, 4), Tooltip("Un objetivo cada N beats detectados. Los limites de simultaneidad y colocacion siguen activos.")]
        public int BeatsPerTarget = 2;
        public bool AttackVariationsEnabled;
        [Range(4, 16), Tooltip("Como maximo un ataque adicional por bloque de N beats.")]
        public int AttackEveryBeats = 8;
        [Range(0f, 1f), Tooltip("Intensidad musical minima para permitir variaciones entre beats.")]
        public float MinimumAttackIntensity = 0.25f;
        public bool MusicalPhrasesEnabled;
        [Tooltip("En frases intensas, usar patrones espejo y direcciones mas amplias.")]
        public bool ChallengingPressurePatterns;
        [Range(2, 8), Tooltip("Beats entre pulsos adicionales en frases intensas. Con cadencia base de 2, un valor de 2 permite un objetivo en cada beat; los limites pueden rechazar extras.")]
        public int PressureExtraBeatEvery = 4;
        [Range(4, 16)] public int PhraseBeats = 8;
        [Range(0f, 1f)] public float PressureIntensity = 0.45f;
        [Range(0f, 1f)] public float PressureReleaseIntensity = 0.38f;
        [Range(0f, 1f)] public float RecoveryTransition = 0.3f;
    }
}
