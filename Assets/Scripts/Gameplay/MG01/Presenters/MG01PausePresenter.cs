using System;
using Mentality.Gameplay.MG01;
using Mentality.UI.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Mentality.UI.Presenters
{
    // Escape abre/cierra la pausa. Accion propia en codigo: no se edita InputSystem_Actions.inputactions.
    public sealed class MG01PausePresenter : IStartable, ITickable, IDisposable
    {
        private readonly IPauseService _pause;
        private readonly UIService _uiService;
        private readonly InputAction _toggleAction = new InputAction("MG01TogglePause", InputActionType.Button, "<Keyboard>/escape");

        private CursorLockMode _cursorLockBeforePause;
        private bool _cursorVisibleBeforePause;

        public MG01PausePresenter(IPauseService pause, UIService uiService)
        {
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        }

        public void Start()
        {
            _pause.PausedChanged += OnPausedChanged;
            _toggleAction.Enable();
        }

        public void Tick()
        {
            if (_toggleAction.WasPressedThisFrame() && !_pause.IsHalted)
                _pause.TogglePause();
        }

        private void OnPausedChanged(bool paused)
        {
            if (paused)
            {
                _cursorLockBeforePause = Cursor.lockState;
                _cursorVisibleBeforePause = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _uiService.ChangeScreen(ScreenType.Pause);
                return;
            }

            _uiService.HideScreen();
            if (_pause.IsHalted) return;

            Cursor.lockState = _cursorLockBeforePause;
            Cursor.visible = _cursorVisibleBeforePause;
        }

        public void Dispose()
        {
            _pause.PausedChanged -= OnPausedChanged;
            _toggleAction.Disable();
            _toggleAction.Dispose();
        }
    }
}
