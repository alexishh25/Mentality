using Cysharp.Threading.Tasks;
using Gameplay.Enemy.Walker.Presenters;
using Gameplay.Enemy.Walker.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Enemy.Walker.Services
{
    public class EnemyFactory
    {
        private readonly IObjectResolver resolver;

        public EnemyFactory(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public EnemyPresenter Create(GameObject enemyPrefab, Vector3 spawnPosition, Quaternion rotation, Transform playerTransform)
        {
            GameObject instance = resolver.Instantiate(enemyPrefab, spawnPosition, rotation);
            var view = instance.GetComponent<EnemyView>();

            var presenter = new EnemyPresenter(view, playerTransform);
            presenter.ExecuteBehaviorAsync().Forget();

            return presenter;
        }
    }
}