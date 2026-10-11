using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mentality.Gameplay.MG01
{
    public sealed class MG01SceneNavigator : IMG01SceneNavigator
    {
        private readonly MG01ConfigSO _config;

        public MG01SceneNavigator(MG01ConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void ReloadLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMenu()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(_config.MenuSceneName);
        }
    }
}
