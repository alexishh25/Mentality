using Gameplay;

using UnityEngine;
using VContainer;
using VContainer.Unity;



public class BackendLifetimeScope : LifetimeScope
{

    protected override void Configure(IContainerBuilder builder)
    {
        // Una escena de prueba offline puede usar el contenedor raiz sin iniciar Python/Ollama.
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var settings = root.GetComponentInChildren<BackendSceneSettings>(true);
            if (settings != null && !settings.EnableBackend)
                return;
        }


        // Servicios HTTP

        builder.Register<ApiClient>(Lifetime.Singleton).AsImplementedInterfaces();


        // Control del proceso Python

        builder.Register<BackendLauncher>(Lifetime.Singleton).AsImplementedInterfaces();
        //builder.RegisterComponentInHierarchy<TouchableObject>();


        // Presenter
        builder.RegisterEntryPoint<BackendPresenter>(Lifetime.Scoped);


    }

}
