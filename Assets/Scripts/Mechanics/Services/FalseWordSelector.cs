using System;
using Gameplay.Rhythm.Models;
namespace Gameplay.Rhythm.Services
{
    public static class FalseWordSelector
    {
        public static void Validate(FalseWordSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!Finite(settings.DistortionIntervalSeconds) || settings.DistortionIntervalSeconds < 0.02f
                || !Finite(settings.QuietProbability) || settings.QuietProbability < 0 || settings.QuietProbability > 1
                || !Finite(settings.FrenzyProbability) || settings.FrenzyProbability < settings.QuietProbability || settings.FrenzyProbability > 1
                || !Finite(settings.ScreamerWeight) || settings.ScreamerWeight < 0
                || !Finite(settings.BullicioWeight) || settings.BullicioWeight < 0
                || !Finite(settings.GlitchWeight) || settings.GlitchWeight < 0
                || (double)settings.ScreamerWeight + settings.BullicioWeight + settings.GlitchWeight <= 0)
                throw new ArgumentException("Probabilidades o pesos de palabras falsas invalidos.", nameof(settings));
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double RandomValue(uint seed)
        {
            unchecked { seed ^= seed >> 16; seed *= 0x7feb352d; seed ^= seed >> 15; seed *= 0x846ca68b; seed ^= seed >> 16; }
            return seed / 4294967296d;
        }
        public static WordTargetKind Select(FalseWordSettings settings, double seedTime, double frenzy,
            double probabilityBonus = 0d, double? probabilityOverride = null)
        {
            Validate(settings);
            if (!Finite(seedTime) || !Finite(frenzy) || frenzy < 0 || frenzy > 1
                || !Finite(probabilityBonus) || probabilityBonus < 0 || probabilityBonus > 1)
                throw new ArgumentOutOfRangeException(nameof(frenzy));
            if (!settings.Enabled) return WordTargetKind.Normal;
            if(probabilityOverride.HasValue && (!Finite(probabilityOverride.Value) || probabilityOverride<0 || probabilityOverride>1))throw new ArgumentOutOfRangeException(nameof(probabilityOverride));
            uint seed = unchecked((uint)Math.Round(seedTime * 1000d));
            double probability = Math.Min(1d, (probabilityOverride ?? (settings.QuietProbability
                + (settings.FrenzyProbability - settings.QuietProbability) * frenzy)) + probabilityBonus);
            if (RandomValue(seed) >= probability) return WordTargetKind.Normal;
            double total = (double)settings.ScreamerWeight + settings.BullicioWeight + settings.GlitchWeight;
            double choice = RandomValue(seed ^ 0x9e3779b9u) * total;
            if (choice < settings.ScreamerWeight) return WordTargetKind.Screamer;
            if (choice < (double)settings.ScreamerWeight + settings.BullicioWeight) return WordTargetKind.Bullicio;
            return WordTargetKind.Glitch;
        }
    }
}
