using UnityEngine;
using VContainer;
using VContainer.Unity;



public class BackendLifetimeScope : LifetimeScope
{

    protected override void Configure(IContainerBuilder builder)
    {


        // Servicios HTTP

        builder.Register<ApiClient>(Lifetime.Singleton).AsImplementedInterfaces();


        // Control del proceso Python

        builder.Register<BackendLauncher>(Lifetime.Singleton).AsImplementedInterfaces();


        // Presenter
        builder.RegisterEntryPoint<BackendPresenter>(Lifetime.Scoped);


    }

}