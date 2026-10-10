using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Mentality.UI.Services
{
    /// <summary>
    /// Orquesta pausa de gameplay: UI Toolkit (Pause), timeScale e input del jugador.
    /// Requiere que <see cref="UILifetimeScope"/> sea hijo de <see cref="PlayerLifetimeScope"/>
    /// para resolver <see cref="IPlayerInputService"/>.
    /// </summary>
    public class GameplayPauseService : IGameplayPauseService, IStartable, ITickable, IDisposable
    {
        public const string MainMenuSceneName = "UIScene";

        private readonly UIService _uiService;
        private readonly IPlayerInputService _playerInput;

        public bool IsPaused { get; private set; }

        public GameplayPauseService(UIService uiService, IPlayerInputService playerInput)
        {
            _uiService = uiService;
            _playerInput = playerInput;
        }

        public void Start()
        {
            // UIDoc registra pantallas en Start; ocultamos el overlay hasta que el jugador pause.
            _uiService.DisableUI();
            IsPaused = false;
            Time.timeScale = 1f;
        }

        public void Tick()
        {
            if (!_playerInput.PausePressed) return;

            if (!IsPaused)
            {
                Pause();
                return;
            }

            // Escape en Ajustes vuelve a Pause; Escape en Pause reanuda.
            if (_uiService.CurrentScreen == ScreenType.Options)
                _uiService.GoBack(ScreenType.Pause);
            else
                Resume();
        }

        public void Pause()
        {
            if (IsPaused) return;

            IsPaused = true;
            Time.timeScale = 0f;
            _playerInput.DisableInput();
            _uiService.EnableUI();
            _uiService.ChangeScreen(ScreenType.Pause, pushHistory: false);
        }

        public void Resume()
        {
            if (!IsPaused) return;

            IsPaused = false;
            _uiService.DisableUI();
            _playerInput.EnableInput();
            Time.timeScale = 1f;
        }

        public void ExitToMainMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            _playerInput.EnableInput();
            SceneManager.LoadScene(MainMenuSceneName);
        }

        public void Dispose()
        {
            if (Time.timeScale != 1f)
                Time.timeScale = 1f;
        }
    }
}
