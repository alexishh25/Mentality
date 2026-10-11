using System;
using Mentality.Gameplay.MG01;
using Mentality.UI.Services;
using UnityEngine;
using VContainer.Unity;

namespace Mentality.UI.Presenters
{
    // Al terminar la partida: guarda la recompensa (si hubo victoria) y muestra Game Over o Recompensa.
    public sealed class MG01ResultsPresenter : IStartable, IDisposable
    {
        private readonly IMG01ResultService _result;
        private readonly IMG01ProgressService _progress;
        private readonly UIService _uiService;

        public MG01ResultsPresenter(IMG01ResultService result, IMG01ProgressService progress, UIService uiService)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        }

        public void Start() => _result.RunEnded += OnRunEnded;

        private void OnRunEnded(MG01Outcome outcome, MG01RunStats stats)
        {
            if (outcome == MG01Outcome.Victory)
                _progress.SaveMG01Reward();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _uiService.ChangeScreen(outcome == MG01Outcome.Victory ? ScreenType.Reward : ScreenType.GameOver);
        }

        public void Dispose() => _result.RunEnded -= OnRunEnded;
    }
}
