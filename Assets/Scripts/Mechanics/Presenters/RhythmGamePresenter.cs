using System;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using UnityEngine;
using VContainer.Unity;

namespace Gameplay.Rhythm.Presenters
{
    public class RhythmGamePresenter : IStartable, ITickable, IDisposable
    {
        private readonly RhythmGameConfig _config;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _conductor;
        private readonly RhythmSpawnerPresenter _spawner;
        private readonly TargetFactory _targetFactory;
        private readonly AudioSource _musicSource;
        private bool _disposed;

        public RhythmGamePresenter(RhythmGameConfig config, RhythmGameModel model,
            AudioConductorService conductor, RhythmSpawnerPresenter spawner,
            TargetFactory targetFactory, AudioSource musicSource)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
            _musicSource = musicSource;
        }

        public void Start() => StartSession();

        public void StartSession()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RhythmGamePresenter));

            // Retirar objetivos anteriores antes de reiniciar los contadores.
            StopSession();
            ValidateConfiguration();

            try
            {
                if (_musicSource != null)
                {
                    _musicSource.Stop();
                    _musicSource.playOnAwake = false;
                    _musicSource.clip = _config.MusicClip;
                }

                _conductor.Initialize(_musicSource, _config.Beatmap);
                _model.StartSession();
                _conductor.PlayMusic();
                _spawner.ResetBeatTracking();
            }
            catch
            {
                StopSession();
                throw;
            }
        }

        public void Tick()
        {
            if (_disposed || !_model.IsPlaying)
                return;

            try
            {
                _conductor.UpdateTime();
                if (!_conductor.IsPlaying)
                {
                    StopSession();
                    return;
                }

                _spawner.CheckAndSpawnTargets();
            }
            catch
            {
                // Un fallo de configuracion/instanciacion no debe repetirse cada beat.
                StopSession();
                throw;
            }
        }

        public void StopSession()
        {
            _model.StopSession();
            _conductor.StopMusic();
            _targetFactory.ClearTargets();
        }

        public void RestartSession() => StartSession();

        private void ValidateConfiguration()
        {
            if (_config.Beatmap == null)
                throw new InvalidOperationException("Asigna la configuracion de BPM y desfase.");
            if (_config.MusicClip != null && _musicSource == null)
                throw new InvalidOperationException("Asigna un AudioSource para reproducir el clip.");
            if (!IsPositiveFinite(_config.TravelSpeed) || !IsPositiveFinite(_config.ArrivalDistance))
                throw new InvalidOperationException("TravelSpeed y ArrivalDistance deben ser positivos y finitos.");
            if (!IsPositiveFinite(_config.SpawnDistance) || _config.SpawnDistance <= _config.ArrivalDistance)
                throw new InvalidOperationException("SpawnDistance debe ser mayor que ArrivalDistance y finita.");
            if (_config.WordPrefabs == null || _config.WordPrefabs.Length == 0)
                throw new InvalidOperationException("Asigna al menos un prefab de palabra.");

            foreach (var prefab in _config.WordPrefabs)
            {
                if (prefab == null)
                    throw new InvalidOperationException("La lista de palabras contiene un prefab sin asignar.");

                var view = prefab.GetComponent<TargetView>();
                var collider = prefab.GetComponent<Collider>();
                if (view == null || !view.enabled || collider == null || !collider.enabled)
                    throw new InvalidOperationException("Cada prefab necesita TargetView y Collider habilitados en su raiz.");
            }
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopSession();
        }
    }
}
