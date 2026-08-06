using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameRootScope : LifetimeScope
{
    [SerializeField] private GameConfigSO gameConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        // Registro de datos/configuración
        builder.RegisterInstance(gameConfig);

        // Lógica general (Compositor / GameManager)
        // builder.Register<SaveSystem>(Lifetime.Singleton).As<ISaveSystem>();
        // builder.Register<GameAudioManager>(Lifetime.Singleton);
        // builder.RegisterEntryPoint<GameFlowManager>(); // Lógica de estado global
    }
}