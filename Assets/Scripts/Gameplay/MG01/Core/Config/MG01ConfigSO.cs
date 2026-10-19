using UnityEngine;

namespace Mentality.Gameplay.MG01
{
    [CreateAssetMenu(fileName = "MG01Config", menuName = "Scriptable Objects/MG01 Config")]
    public class MG01ConfigSO : ScriptableObject
    {
        [Header("Cordura (GDD v0.2)")]
        [SerializeField, Min(1f)] private float maxSanity = 100f;
        [SerializeField, Min(0f)] private float damagePerHit = 10f;

        [Header("HUD")]
        [Tooltip("Velocidad del suavizado (Mathf.Lerp exponencial). 10 asienta la barra en ~0.5 s.")]
        [SerializeField, Min(0.01f)] private float hudLerpSpeed = 10f;
        [Tooltip("Duracion del estado Golpe (borde rojo, -10, estado IMPACTO).")]
        [SerializeField, Min(0f)] private float hitFeedbackDuration = 0.6f;
        [Tooltip("Tiempo en que la estela fantasma colapsa hasta el nuevo relleno.")]
        [SerializeField, Min(0f)] private float ghostTrailDuration = 0.4f;
        [SerializeField, Min(0f)] private float hitShakeDuration = 0.18f;
        [SerializeField, Min(0f)] private float hitShakeAmplitude = 4f;
        [SerializeField, Min(0.01f)] private float criticalPulsePeriod = 1.2f;

        [Header("Cancion (sustituto temporal del conductor ritmico)")]
        [SerializeField, Min(0.1f)] private float debugSongDuration = 60f;

        [Header("Escenas (deben estar en Build Settings)")]
        [SerializeField] private string mg01SceneName = "Nivel-1_Rhythm";
        [SerializeField] private string menuSceneName = "UIScene";

        [Header("Guardado")]
        [SerializeField] private string progressFileName = "progress.json";

        public float MaxSanity => maxSanity;
        public float DamagePerHit => damagePerHit;
        public float HudLerpSpeed => hudLerpSpeed;
        public float HitFeedbackDuration => hitFeedbackDuration;
        public float GhostTrailDuration => ghostTrailDuration;
        public float HitShakeDuration => hitShakeDuration;
        public float HitShakeAmplitude => hitShakeAmplitude;
        public float CriticalPulsePeriod => criticalPulsePeriod;
        public float DebugSongDuration => debugSongDuration;
        public string MG01SceneName => mg01SceneName;
        public string MenuSceneName => menuSceneName;
        public string ProgressFileName => progressFileName;

        // Valores por defecto cuando una escena no tiene asset asignado.
        public static MG01ConfigSO CreateDefault()
        {
            var config = CreateInstance<MG01ConfigSO>();
            config.name = "MG01Config (default)";
            return config;
        }
    }
}
