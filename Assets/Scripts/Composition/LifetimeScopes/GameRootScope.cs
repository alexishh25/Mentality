using Mentality.Application.Scene;
using Mentality.Infrastructure.AppLifecycle;
using Composition.Interfaces;
using Composition.Services;
using Infrastructure.Audio;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameRootScope : LifetimeScope
{
    [SerializeField] private GameConfigSO gameConfig;
    [SerializeField] private AudioRoot audioRoot;

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. Registro de datos y configuraciones globales
        if (gameConfig != null)
        {
            builder.RegisterInstance(gameConfig);

            if (gameConfig.AudioConfig != null)
            {
                builder.RegisterInstance(gameConfig.AudioConfig);
            }

            if (gameConfig.InputConfig != null)
            {
                builder.RegisterInstance(gameConfig.InputConfig);
            }

            if (gameConfig.SaveConfig != null)
            {
                builder.RegisterInstance(gameConfig.SaveConfig);
            }
        }
        
        // 2. Infraestructura
        if (audioRoot != null)
        {
            if (!audioRoot.gameObject.scene.IsValid() || string.IsNullOrEmpty(audioRoot.gameObject.scene.name))
            {
                builder.RegisterComponentInNewPrefab(audioRoot, Lifetime.Singleton);
            }
            else
            {
                builder.RegisterComponent(audioRoot);
            }
        }

        builder.Register<MusicPlayer>(Lifetime.Singleton).As<IMusicPlayer>().As<ITickable>();
        builder.Register<SFXPlayer>(Lifetime.Singleton).As<ISFXPlayer>();
        builder.Register<UnityAppFlowService>(Lifetime.Singleton).As<IAppFlowService>();

        // 3. Servicios Globales de Aplicación
        builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
        builder.Register<UnitySceneService>(Lifetime.Singleton).As<ISceneService>();
        builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
        builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
    }
}