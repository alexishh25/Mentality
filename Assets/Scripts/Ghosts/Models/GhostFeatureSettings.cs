using UnityEngine;

namespace Gameplay.Ghosts.Models
{
    [CreateAssetMenu(menuName = "Mentality/Ghosts/Feature Settings")]
    public sealed class GhostFeatureSettings : ScriptableObject
    {
        [Header("Spawn")]
        public GameObject[] DecoyPrefabs;
        [Range(0f, 1f)] public float OffbeatSpawnChance = 0.18f;
        [Min(0.25f)] public float MinimumSpawnSeparationSeconds = 2.5f;
        [Min(0.1f)] public float MinimumSilenceSeconds = 1.2f;
        [Min(0.1f)] public float PassThroughDistance = 3f;
        public int RandomSeed = 1741;

        [Header("Consequences")]
        [Min(1f)] public float MaximumSanity = 100f;
        [Min(0f)] public float SanityLossPerClick = 12f;
        [Range(0f, 0.6f)] public float MaximumStaticOpacity = 0.4f;
        [Range(0f, 1f)] public float ScreamerChancePerClick = 0.08f;
        [Min(0.05f)] public float ScreamerDurationSeconds = 0.22f;
        public bool EnableRandomScreamer = true;

        [Header("Screen static preview")]
        [Min(16)] public int NoiseTextureWidth = 160;
        [Min(16)] public int NoiseTextureHeight = 90;
        [Min(1f)] public float NoiseRefreshRate = 12f;

        public bool IsValid()
        {
            if (DecoyPrefabs == null || DecoyPrefabs.Length == 0) return false;
            foreach (GameObject prefab in DecoyPrefabs)
                if (prefab == null) return false;

            return MaximumSanity > 0f && IsFinite(MaximumSanity)
                && SanityLossPerClick >= 0f && IsFinite(SanityLossPerClick)
                && IsFinite(OffbeatSpawnChance)
                && MinimumSpawnSeparationSeconds > 0f && IsFinite(MinimumSpawnSeparationSeconds)
                && MinimumSilenceSeconds > 0f && IsFinite(MinimumSilenceSeconds)
                && PassThroughDistance > 0f && IsFinite(PassThroughDistance)
                && MaximumStaticOpacity >= 0f && MaximumStaticOpacity <= 0.6f
                && ScreamerChancePerClick >= 0f && ScreamerChancePerClick <= 1f
                && ScreamerDurationSeconds > 0f && IsFinite(ScreamerDurationSeconds)
                && NoiseTextureWidth >= 16 && NoiseTextureHeight >= 16
                && NoiseRefreshRate > 0f && IsFinite(NoiseRefreshRate);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
