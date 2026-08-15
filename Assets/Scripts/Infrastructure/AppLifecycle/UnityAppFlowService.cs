using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mentality.Infrastructure.AppLifecycle
{
    public class UnityAppFlowService : IAppFlowService
    {
        public void IniciarNuevaPartida(string sceneName = "SampleScene")
        {
            SceneManager.LoadScene(sceneName);
        }

        public void SalirJuego()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
