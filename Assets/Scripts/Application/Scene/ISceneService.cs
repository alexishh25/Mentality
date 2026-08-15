using UnityEngine.SceneManagement;

namespace Mentality.Application.Scene
{
    public interface ISceneService
    {
        void LoadByName(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        UnityEngine.AsyncOperation LoadByNameAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        string GetActiveSceneName();
    }
}