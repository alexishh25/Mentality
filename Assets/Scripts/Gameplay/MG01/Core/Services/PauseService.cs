using System;

namespace Mentality.Gameplay.MG01
{
    public sealed class PauseService : IPauseService
    {
        public bool IsPaused { get; private set; }
        public bool IsHalted { get; private set; }
        public bool IsGameplayStopped => IsPaused || IsHalted;

        public event Action<bool> PausedChanged;

        public void SetPaused(bool paused)
        {
            if (paused && IsHalted) return;
            if (IsPaused == paused) return;

            IsPaused = paused;
            PausedChanged?.Invoke(IsPaused);
        }

        public void TogglePause() => SetPaused(!IsPaused);

        public void Halt()
        {
            IsHalted = true;
            SetPaused(false);
        }
    }
}
