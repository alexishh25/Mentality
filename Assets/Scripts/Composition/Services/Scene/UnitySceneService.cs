using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mentality.Application.Scene
{
    public class UnitySceneService : ISceneService
    {
        public void LoadByName(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[UnitySceneService] No se puede cargar una escena con nombre nulo o vacío.");
                return;
            }

            SceneManager.LoadScene(sceneName, mode);
        }

        public AsyncOperation LoadByNameAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[UnitySceneService] No se puede cargar una escena de forma asíncrona con nombre nulo o vacío.");
                return null;
            }

            return SceneManager.LoadSceneAsync(sceneName, mode);
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}