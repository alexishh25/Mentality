using System;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using Mentality.UI;
using Mentality.UI.Repositories;
using Mentality.UI.Services;
using Mentality.UI.ViewModels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Rhythm.Scopes
{
    public class RhythmLifetimeScope : LifetimeScope
    {
        [Header("Rhythm Settings")]
        [SerializeField] private RhythmGameConfig config = new RhythmGameConfig();

        [Header("Scene References")]
        [Tooltip("Punto de llegada y centro del circulo de aparicion; usar la posicion de la camara del jugador fijo.")]
        [SerializeField] private Transform playerTarget;

        [Tooltip("AudioSource dedicado. Puede quedar vacio cuando no se usa musica.")]
        [SerializeField] private AudioSource musicSource;

        [Header("Optional Test HUD")]
        [SerializeField] private RhythmHudView hudView;
        [SerializeField] private UIDoc screensDoc;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config == null)
                throw new InvalidOperationException("RhythmLifetimeScope necesita una configuracion.");
            if (playerTarget == null)
                throw new InvalidOperationException("Asigna el punto del jugador en RhythmLifetimeScope.");

            builder.RegisterInstance(config);
            builder.RegisterInstance(playerTarget);
            builder.Register<RhythmGameModel>(Lifetime.Scoped);
            builder.Register<AudioConductorService>(Lifetime.Scoped);
            builder.Register<FalseWordEffectService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<ScreamerPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GlitchPresenter>(Lifetime.Scoped);
            builder.RegisterInstance(config.Bullicio ?? new BullicioSettings { Enabled = false });
            builder.Register<BullicioSessionService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<BullicioPresenter>(Lifetime.Scoped);
            builder.Register<TargetFactory>(Lifetime.Scoped);
            builder.Register<RhythmSpawnerPresenter>(Lifetime.Scoped);

            // Solo este presentador actualiza reloj y spawn, en ese orden.
            // El parametro explicito permite inyectar null en las pruebas sin musica.
            builder.RegisterEntryPoint<RhythmGamePresenter>(Lifetime.Scoped)
                .AsSelf()
                .WithParameter<AudioSource>(musicSource);

            if (hudView != null)
            {
                builder.RegisterComponent(hudView);
                builder.Register<UIRepository>(Lifetime.Singleton);
                builder.Register<UIService>(Lifetime.Singleton);
                builder.Register<ButtonRepository>(Lifetime.Singleton);
                builder.Register<ButtonService>(Lifetime.Singleton);
                builder.Register<RhythmUiRepository>(Lifetime.Singleton);
                builder.Register<RhythmUiService>(Lifetime.Singleton);
                builder.Register<RhythmResultViewModel>(Lifetime.Singleton);
                if (screensDoc != null)
                    builder.RegisterComponent(screensDoc);
                builder.Register<RhythmSessionInputService>(Lifetime.Scoped);
                builder.RegisterEntryPoint<RhythmHudPresenter>(Lifetime.Scoped);
            }
        }
    }
}
