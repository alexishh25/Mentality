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
        [Tooltip("Opcional. Usa el mapa del audio para generar objetivos. Sin mapa se usa el BPM de Beatmap.")]
        public MusicalSongAsset AnalyzedSong;

        public AudioClip SessionMusicClip => AnalyzedSong != null ? AnalyzedSong.MusicClip : MusicClip;

        [Header("Musical Events")]
        [Min(0.01f)]
        [Tooltip("Separacion minima en segundos entre apariciones del mapa. Los beats tienen prioridad sobre ataques cercanos.")]
        public float MinimumSpawnInterval = 0.3f;
        public MusicalActivitySettings MusicalActivity = new MusicalActivitySettings();
        [Header("Beat Cadence")]
        public BeatCadenceSettings Cadence = new BeatCadenceSettings();
        public PulseBonusSettings PulseBonus = new PulseBonusSettings();
        public RhythmPatternSettings Patterns = new RhythmPatternSettings();
        public RhythmPlacementSettings Placement = new RhythmPlacementSettings();
        public TargetEvasionSettings Evasion = new TargetEvasionSettings();
        public FalseWordSettings FalseWords = new FalseWordSettings();
        public ScreamerSettings Screamer = new ScreamerSettings();
        public BullicioSettings Bullicio = new BullicioSettings();
        public GlitchSettings Glitch = new GlitchSettings();
        [Header("Creatures")]
        public CreatureSettings Creatures = new CreatureSettings();

        [Header("Word Targets")]
        public GameObject[] WordPrefabs = Array.Empty<GameObject>();
        [Tooltip("Aro breve y fijo en el lugar de lanzamiento; no sigue al objeto.")]
        public bool ShowSpawnRing = true;
        [Min(0.01f)] public float SpawnRingDurationSeconds = 0.35f;

        [Min(0.1f)]
        [Tooltip("Distancia desde el jugador donde aparecen las palabras, en unidades de Unity.")]
        public float SpawnDistance = 12f;

        [Min(0.1f)]
        [Tooltip("Velocidad de acercamiento en unidades por segundo. El recorrido y esta velocidad determinan la anticipacion del mapa musical.")]
        public float TravelSpeed = 3f;

        [Min(0.01f)]
        [Tooltip("Distancia al jugador a partir de la cual una palabra cuenta como fallo. Debe ser menor que SpawnDistance.")]
        public float ArrivalDistance = 0.75f;

        // Se deriva del recorrido actual para no tener velocidad y anticipacion contradictorias.
        public double TargetLeadTimeSeconds => ((double)SpawnDistance - ArrivalDistance) / TravelSpeed;
    }
}
