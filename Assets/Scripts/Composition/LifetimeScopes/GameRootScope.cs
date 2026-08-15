using Mentality.Application.Scene;
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
        // Registro de datos/configuración
        if (gameConfig != null && gameConfig.audioConfig != null)
        {
            builder.RegisterInstance(gameConfig.audioConfig);
        }
        
        // Infrastructure
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
        
        builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();

        builder.Register<UnitySceneService>(Lifetime.Singleton).As<ISceneService>();
        
    }
}