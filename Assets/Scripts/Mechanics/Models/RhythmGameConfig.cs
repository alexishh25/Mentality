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
        [Tooltip("Suena al perder la partida y se detiene al reiniciar.")]
        public AudioClip GameOverClip;
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
        [Header("Word Impact Feedback")]
        public WordImpactBlurSettings ImpactBlur = new WordImpactBlurSettings();
        [Header("Creatures")]
        public CreatureSettings Creatures = new CreatureSettings();

        [Header("Word Targets")]
        public GameObject[] WordPrefabs = Array.Empty<GameObject>();
        [Tooltip("Pulso breve en la boca del publico; para otros origenes, aro fijo de lanzamiento.")]
        public bool ShowSpawnRing = true;
        [Min(0.01f)] public float SpawnRingDurationSeconds = 0.35f;

        [Min(0.1f)]
        [Tooltip("Distancia de referencia del mapa. Con publico, el origen real es la boca de cada personaje.")]
        public float SpawnDistance = 12f;

        [Min(0.1f)]
        [Tooltip("Velocidad base de referencia. Junto con SpawnDistance determina la anticipacion musical; el acercamiento desde el publico se adapta al recorrido real.")]
        public float TravelSpeed = 3f;

        [Tooltip("Adapta el acercamiento desde el publico: reserva mas tiempo para el tramo legible sin mover los beats de llegada.")]
        public bool UseDistanceAwareApproach;
        [Tooltip("Permite lanzar palabras a traves de la escenografia y usar todos los emisores del publico.")]
        public bool WordsPassThroughScenery;
        [Tooltip("El tamano de las palabras empieza a crecer desde la salida, sin un tramo inicial de tamano fijo.")]
        public bool ImmediateWordGrowth;
        [Range(0, 2), Tooltip("Espacios reservados para disparos de criatura durante los tramos intensos, dentro del limite total de palabras.")]
        public int CreatureReservedSlots;
        [Range(0.5f, 0.85f), Tooltip("Fraccion del tiempo musical de vuelo reservada para el acercamiento legible desde filas lejanas.")]
        public float ReadableApproachFraction = 0.65f;

        [Min(0.01f)]
        [Tooltip("Distancia al jugador a partir de la cual una palabra cuenta como fallo. Debe ser menor que SpawnDistance.")]
        public float ArrivalDistance = 0.75f;

        // Se deriva del recorrido actual para no tener velocidad y anticipacion contradictorias.
        public double TargetLeadTimeSeconds => ((double)SpawnDistance - ArrivalDistance) / TravelSpeed;
    }
}
