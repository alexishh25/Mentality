using System;

namespace Mentality.Gameplay.MG01
{
    public enum MG01Outcome { Victory, Defeat }

    // "Estadisticas propuestas" del GDD.
    [Serializable]
    public struct MG01RunStats
    {
        public int wordsDestroyed;
        public int hitsTaken;
        public int ghostClicks;
        public float sanityRemaining;
    }

    // Decide el resultado de la partida y avisa a la UI.
    // Derrota: ISanityService.Depleted. Victoria: ISongProgressSource.Finished con cordura > 0.
    public interface IMG01ResultService
    {
        event Action<MG01Outcome, MG01RunStats> RunEnded;

        bool HasEnded { get; }
        MG01Outcome Outcome { get; }
        MG01RunStats Stats { get; }

        void ReportWordDestroyed();
        void ReportGhostClicked();
    }
}
