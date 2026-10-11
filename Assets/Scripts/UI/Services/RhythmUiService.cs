using System;
using Mentality.UI.Repositories;
using ScreenType = Mentality.UI.ScreenType;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Mentality.UI.Services
{
    // Muestra Game Over o Victoria por UIDoc y avisa cuando el jugador pide reiniciar.
    public class RhythmUiService
    {
        private readonly RhythmUiRepository _repository;
        private ScreenType? _shown;

        public event Action RestartRequested;

        public RhythmUiService(RhythmUiRepository repository)
        {
            _repository = repository;
        }

        public void RequestRestart() => RestartRequested?.Invoke();

        public void PresentOutcome(bool gameOver, bool victory)
        {
            ScreenType? next = gameOver ? ScreenType.RhythmGameOver
                : victory ? ScreenType.RhythmVictory
                : null;
            if (next == _shown) return;

            if (_shown != null)
                _repository.Hide();

            _shown = next;
            if (next == null) return;

            EnsurePointer();
            _repository.Show(next.Value);
        }

        private static void EnsurePointer()
        {
            if (EventSystem.current != null) return;
            var events = new GameObject("Rhythm UI events");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
