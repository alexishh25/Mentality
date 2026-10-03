using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Rhythm.Factories
{
    public class TargetFactory : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly RhythmGameConfig _config;
        private readonly RhythmGameModel _model;
        private readonly Transform _playerTarget;
        private readonly List<TargetView> _targets = new List<TargetView>();

        public TargetFactory(IObjectResolver resolver, RhythmGameConfig config,
            RhythmGameModel model, Transform playerTarget)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (playerTarget == null)
                throw new ArgumentNullException(nameof(playerTarget));
            _playerTarget = playerTarget;
        }

        public TargetView CreateRandomPosition()
        {
            if (!_model.IsPlaying)
                return null;

            if (_playerTarget == null)
                throw new InvalidOperationException("No hay un punto de destino para las palabras.");
            if (_config.WordPrefabs == null || _config.WordPrefabs.Length == 0)
                throw new InvalidOperationException("Asigna al menos un prefab de palabra.");
            if (float.IsNaN(_config.SpawnDistance) || float.IsInfinity(_config.SpawnDistance)
                || _config.SpawnDistance <= _config.ArrivalDistance)
                throw new InvalidOperationException("SpawnDistance debe ser finita y mayor que ArrivalDistance.");

            int index = UnityEngine.Random.Range(0, _config.WordPrefabs.Length);
            // Arco de 270 grados centrado en el norte (+Z) de la arena.
            // Excluye los 90 grados del sur (-Z), independientemente del giro de la camara.
            float angle = UnityEngine.Random.Range(-135f, 135f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            return Create(_config.WordPrefabs[index], _playerTarget.position + direction * _config.SpawnDistance);
        }

        public TargetView Create(GameObject prefab, Vector3 position)
        {
            if (!_model.IsPlaying)
                return null;

            if (prefab == null || prefab.GetComponent<TargetView>() == null
                || prefab.GetComponent<Collider>() == null)
                throw new InvalidOperationException("El prefab necesita TargetView y un Collider en su raiz.");

            GameObject instance = _resolver.Instantiate(prefab, position, Quaternion.identity);
            try
            {
                var view = instance.GetComponent<TargetView>();
                var presenter = new TargetPresenter(view, _model, _playerTarget,
                    _config.TravelSpeed, _config.ArrivalDistance);
                view.Initialize(presenter);
                instance.SetActive(true);
                _targets.RemoveAll(target => target == null);
                _targets.Add(view);
                return view;
            }
            catch
            {
                instance.SetActive(false);
                UnityEngine.Object.Destroy(instance);
                throw;
            }
        }

        public void ClearTargets()
        {
            foreach (var target in _targets)
            {
                if (target != null)
                    target.Cancel();
            }
            _targets.Clear();
        }

        public void Dispose() => ClearTargets();
    }
}
