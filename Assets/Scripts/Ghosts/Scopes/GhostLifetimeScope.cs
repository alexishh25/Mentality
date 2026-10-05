using System;
using Gameplay.Ghosts.Factories;
using Gameplay.Ghosts.Models;
using Gameplay.Ghosts.Presenters;
using Gameplay.Ghosts.Services;
using Gameplay.Ghosts.Views;
using Gameplay.Rhythm.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Ghosts.Scopes
{
    public sealed class GhostLifetimeScope : LifetimeScope
    {
        [SerializeField] private GhostFeatureSettings settings;
        [SerializeField] private Transform playerTarget;

        protected override void Configure(IContainerBuilder builder)
        {
            if (settings == null || !settings.IsValid())
                throw new InvalidOperationException("GhostLifetimeScope necesita GhostFeatureSettings validos.");
            if (playerTarget == null)
                throw new InvalidOperationException("Asigna el punto del jugador en GhostLifetimeScope.");

            builder.RegisterInstance(settings);
            builder.RegisterInstance(playerTarget);
            builder.Register<GhostSanityModel>(Lifetime.Scoped);
            builder.Register<GhostSpawnPlanner>(Lifetime.Scoped);
            builder.Register<GhostEffectsView>(Lifetime.Scoped);
            builder.Register<GhostDecoyFactory>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GhostConsequencesPresenter>(Lifetime.Scoped)
                .AsSelf().AsImplementedInterfaces();
            builder.RegisterEntryPoint<GhostSpawnerPresenter>(Lifetime.Scoped);
        }
    }
}
