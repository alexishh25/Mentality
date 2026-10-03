
using Gameplay.Enemy.Walker.Models;
using Gameplay.Enemy.Walker.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Enemy.Walker.Scopes
{
    public class EnemyLifetimeScope : LifetimeScope
    {
        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;

        [Header("Enemy Spawner Settings")]
        [SerializeField] private EnemySpawnerConfig spawnerConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            // Models & References
            builder.RegisterInstance(spawnerConfig);
            builder.RegisterInstance(playerTransform);

            // Services & Factories
            builder.Register<EnemyFactory>(Lifetime.Scoped);
            builder.Register<EnemySpawnerService>(Lifetime.Scoped);

            // Inicia automáticamente el ciclo de spawn cuando el Scope se inicializa
            builder.RegisterBuildCallback(container =>
            {
                var spawnerService = container.Resolve<EnemySpawnerService>();
                spawnerService.StartSpawning();
            });
        }
    }
}