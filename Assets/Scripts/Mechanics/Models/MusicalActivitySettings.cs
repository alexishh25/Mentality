using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class MusicalActivitySettings
    {
        public bool Enabled = true;
        [Min(0.01f), Tooltip("Suavizado temporal de intensidad y transiciones, en segundos.")]
        public float SmoothingSeconds = 0.5f;
        [Min(0f), Tooltip("Unir pausas menores o iguales a este tiempo para evitar cortes breves.")]
        public float MergeSilenceGapsSeconds = 0.25f;
        [Min(0.01f), Tooltip("Separacion entre objetivos en tramos tranquilos. Nunca menor que MinimumSpawnInterval.")]
        public float QuietSpawnInterval = 1.2f;
        [Range(0f, 1f), Tooltip("Intensidad normalizada por debajo de la cual se usa la densidad tranquila.")]
        public float QuietIntensity = 0.08f;
        [Range(0f, 1f), Tooltip("Intensidad normalizada necesaria para alcanzar la densidad maxima.")]
        public float FullDensityIntensity = 0.6f;
        [Range(0f, 1f), Tooltip("Reduccion de actividad cuando aumenta la curva de transiciones. No identifica versos ni estribillos.")]
        public float TransitionRelief = 0.35f;
        public bool EnableFrenzy = true;
        [Range(0f, 1f)] public float FrenzyStartIntensity = 0.45f;
        [Range(0f, 1f)] public float FullFrenzyIntensity = 0.65f;
        [Range(1f, 4f)] public float MaximumSpeedMultiplier = 4f;
        [Range(0.25f, 1f)] public float FrenzyIntervalMultiplier = 0.25f;
        [Range(0, 4)] public int FrenzyExtraTargets = 4;
    }
}
