using System;

namespace Mentality.Gameplay.MG01
{
    // Detiene cancion, proyectiles y temporizadores sin depender de Time.timeScale.
    public interface IPauseService
    {
        // Pausa abierta por el jugador (Escape).
        bool IsPaused { get; }
        // La partida termino (Game Over o Recompensa); solo se reinicia recargando la escena.
        bool IsHalted { get; }
        // Lo que debe consultar el gameplay antes de avanzar o procesar input.
        bool IsGameplayStopped { get; }

        event Action<bool> PausedChanged;

        void SetPaused(bool paused);
        void TogglePause();
        void Halt();
    }
}
