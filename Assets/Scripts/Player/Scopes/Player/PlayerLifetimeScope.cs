// PlayerLifetimeScope.cs
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class PlayerLifetimeScope : LifetimeScope
{
    [SerializeField] private PlayerView playerView;
    [SerializeField] private PlayerModel playerModel;

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. Inyectar Instancias de Escena / Configuración
        builder.RegisterInstance(playerView);
        builder.RegisterInstance(playerModel);

        // 2. Registrar Servicios (Input)
        builder.Register<PlayerInputService>(Lifetime.Singleton)
               .AsImplementedInterfaces();

        // 3. Registrar el Presenter en el ciclo Tickable de Unity
        builder.RegisterEntryPoint<PlayerPresenter>(Lifetime.Scoped);
    }
}