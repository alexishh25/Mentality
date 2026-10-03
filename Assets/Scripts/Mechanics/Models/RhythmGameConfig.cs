using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public class RhythmGameConfig
    {
        [Header("Music")]
        [Tooltip("Opcional. Sin clip, la prueba utilizara un reloj de beats.")]
        public AudioClip MusicClip;
        public SongBeatmap Beatmap = new SongBeatmap();

        [Header("Word Targets")]
        public GameObject[] WordPrefabs = Array.Empty<GameObject>();

        [Min(0.1f)]
        [Tooltip("Distancia desde el jugador donde aparecen las palabras, en unidades de Unity.")]
        public float SpawnDistance = 12f;

        [Min(0.1f)]
        [Tooltip("Velocidad de acercamiento en unidades por segundo, independiente del BPM.")]
        public float TravelSpeed = 3f;

        [Min(0.01f)]
        [Tooltip("Distancia al jugador a partir de la cual una palabra cuenta como fallo. Debe ser menor que SpawnDistance.")]
        public float ArrivalDistance = 0.75f;
    }
}
