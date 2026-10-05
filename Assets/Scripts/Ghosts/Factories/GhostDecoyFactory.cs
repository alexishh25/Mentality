using System;
using System.Collections.Generic;
using Gameplay.Ghosts.Models;
using Gameplay.Ghosts.Presenters;
using Gameplay.Ghosts.Services;
using Gameplay.Ghosts.Views;
using Gameplay.Rhythm.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Ghosts.Factories
{
    public sealed class GhostDecoyFactory : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly GhostFeatureSettings _settings;
        private readonly RhythmGameConfig _rhythmConfig;
        private readonly Transform _playerTarget;
        private readonly RhythmGameModel _session;
        private readonly IGhostConsequences _consequences;
        private readonly List<GhostDecoyView> _active = new List<GhostDecoyView>();

        public GhostDecoyFactory(IObjectResolver resolver, GhostFeatureSettings settings,
            RhythmGameConfig rhythmConfig, Transform playerTarget, RhythmGameModel session,
            IGhostConsequences consequences)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rhythmConfig = rhythmConfig ?? throw new ArgumentNullException(nameof(rhythmConfig));
            _playerTarget = playerTarget != null ? playerTarget : throw new ArgumentNullException(nameof(playerTarget));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _consequences = consequences ?? throw new ArgumentNullException(nameof(consequences));
        }

        public void Create()
        {
            GameObject[] prefabs = _settings.DecoyPrefabs;
            if (!_session.IsPlaying || prefabs == null || prefabs.Length == 0) return;
            GameObject prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
            if (prefab == null) throw new InvalidOperationException("La lista DecoyPrefabs contiene un prefab sin asignar.");
            TextMesh prefabWord = prefab.GetComponentInChildren<TextMesh>(true);
            if (prefabWord == null) throw new InvalidOperationException($"El prefab {prefab.name} necesita un TextMesh.");

            Vector3 forward = Vector3.ProjectOnPlane(_playerTarget.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            float angle = UnityEngine.Random.Range(-60f, 60f);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 position = _playerTarget.position + direction * _rhythmConfig.SpawnDistance;
            Quaternion rotation = Quaternion.LookRotation(_playerTarget.position - position, Vector3.up);
            GameObject instance = _resolver.Instantiate(prefab, position, rotation);
            try
            {
                var view = instance.GetComponent<GhostDecoyView>();
                if (view == null) throw new InvalidOperationException("El prefab de decoy necesita GhostDecoyView.");
                view.Initialize(new GhostDecoyPresenter(view, _playerTarget, _session, _consequences,
                    _rhythmConfig.TravelSpeed, _settings.PassThroughDistance), prefabWord.text);
                instance.SetActive(true);
                _active.RemoveAll(item => item == null);
                _active.Add(view);
            }
            catch
            {
                UnityEngine.Object.Destroy(instance);
                throw;
            }
        }

        public void Clear()
        {
            foreach (GhostDecoyView view in _active)
                if (view != null) view.DestroySelf();
            _active.Clear();
        }

        public void Dispose() => Clear();
    }
}
