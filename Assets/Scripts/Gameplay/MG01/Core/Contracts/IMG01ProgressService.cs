using System;

namespace Mentality.Gameplay.MG01
{
    // Recompensa persistente entre partidas (progress.json).
    [Serializable]
    public class MG01ProgressData
    {
        public bool mg01Completed;
        public bool redHandsUnlocked;
        public bool punchUnlocked;
    }

    public interface IMG01ProgressService
    {
        MG01ProgressData Load();
        // Fusiona con el archivo existente: nunca vuelve a bloquear algo ya desbloqueado.
        void SaveMG01Reward();
    }
}
