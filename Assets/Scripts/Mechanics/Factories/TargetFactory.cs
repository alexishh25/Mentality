using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Services;
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
        private readonly AudioConductorService _conductor;
        private readonly List<TargetView> _targets = new List<TargetView>();
        private readonly List<SpawnRingView> _spawnRings = new List<SpawnRingView>();

        public TargetFactory(IObjectResolver resolver, RhythmGameConfig config,
            RhythmGameModel model, Transform playerTarget, AudioConductorService conductor)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (playerTarget == null)
                throw new ArgumentNullException(nameof(playerTarget));
            _playerTarget = playerTarget;
            _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
        }

        public TargetView CreateRandomPosition(TargetTiming timing = null)
        {
            return CreateAtAngle(UnityEngine.Random.Range(-135f, 135f), timing);
        }

        public int ActiveTargetCount
        {
            get
            {
                foreach (var target in _targets)
                    if (target != null) target.RefreshMusicalState();
                _targets.RemoveAll(target => target == null || target.IsResolved);
                return _targets.Count;
            }
        }

        public TargetView CreateAtAngle(float angleDegrees, TargetTiming timing = null)
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
            if (float.IsNaN(angleDegrees) || float.IsInfinity(angleDegrees) || Mathf.Abs(angleDegrees) > 135f)
                throw new ArgumentOutOfRangeException(nameof(angleDegrees));

            int index = UnityEngine.Random.Range(0, _config.WordPrefabs.Length);
            // Arco de 270 grados centrado en el norte (+Z) de la arena.
            // Excluye los 90 grados del sur (-Z), independientemente del giro de la camara.
            float angle = angleDegrees * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            return Create(_config.WordPrefabs[index], _playerTarget.position + direction * _config.SpawnDistance, timing);
        }

        public TargetView Create(GameObject prefab, Vector3 position, TargetTiming timing = null)
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
                if (timing != null && _config.Placement != null && _config.Placement.Enabled)
                    view.ConfigureLaunchSize(_playerTarget.position, _config.Placement);
                var presenter = new TargetPresenter(view, _model, _playerTarget,
                    _config.TravelSpeed, _config.ArrivalDistance, timing, _conductor,
                    timing == null || _config.Evasion == null ? null : new AimEvasionMotion(_config.Evasion, timing.HitTime));
                view.Initialize(presenter);
                instance.SetActive(true);
                if (_config.ShowSpawnRing) CreateSpawnRing(view, position);
                _targets.RemoveAll(target => target == null || target.IsResolved);
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
            foreach (var ring in _spawnRings)
                if (ring != null) ring.Cancel();
            _spawnRings.Clear();
        }

        private void CreateSpawnRing(TargetView view, Vector3 origin)
        {
            var effect = new GameObject("Spawn ring");
            try
            {
                Vector3 direction = origin - _playerTarget.position;
                effect.transform.SetPositionAndRotation(origin, direction.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity);
                var box = view.GetComponent<BoxCollider>();
                Vector3 scale = view.transform.lossyScale;
                var radius = new Vector2(box.size.x * Mathf.Abs(scale.x) * 0.5f + 0.12f,
                    box.size.y * Mathf.Abs(scale.y) * 0.5f + 0.12f);
                var ring = effect.AddComponent<SpawnRingView>();
                ring.Initialize(_conductor, radius, _config.SpawnRingDurationSeconds);
                _spawnRings.RemoveAll(item => item == null);
                _spawnRings.Add(ring);
            }
            catch
            {
                UnityEngine.Object.Destroy(effect);
                throw;
            }
        }

        public void Dispose() => ClearTargets();
    }
}
