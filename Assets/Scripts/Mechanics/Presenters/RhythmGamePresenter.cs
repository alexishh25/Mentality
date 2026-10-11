using System;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
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
        private readonly FalseWordEffectService _falseWordEffects;
        private bool _disposed;
        private bool _gameOverHandled;
        public MusicalMapData MusicalMap { get; private set; }

        public RhythmGamePresenter(RhythmGameConfig config, RhythmGameModel model,
            AudioConductorService conductor, RhythmSpawnerPresenter spawner,
            TargetFactory targetFactory, AudioSource musicSource, FalseWordEffectService falseWordEffects)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
            _musicSource = musicSource;
            _falseWordEffects = falseWordEffects ?? throw new ArgumentNullException(nameof(falseWordEffects));
        }

        public void Start() => StartSession();

        public void StartSession()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RhythmGamePresenter));

            // Retirar objetivos anteriores antes de reiniciar los contadores.
            StopSession();
            MusicalMap = null;
            ValidateConfiguration();

            try
            {
                if (_config.AnalyzedSong != null)
                    MusicalMap = _config.AnalyzedSong.LoadValidatedMap();
                if (_musicSource != null)
                {
                    _musicSource.Stop();
                    _musicSource.playOnAwake = false;
                    _musicSource.clip = _config.SessionMusicClip;
                }

                if (MusicalMap != null)
                    _conductor.Initialize(_musicSource, MusicalMap);
                else
                    _conductor.Initialize(_musicSource, _config.Beatmap);
                _model.StartSession();
                _model.ConfigurePulseBonus(MusicalMap, _config.PulseBonus);
                _gameOverHandled = false;
                double leadTime = MusicalMap != null ? _config.TargetLeadTimeSeconds : 0d;
                // Preparar el plan completo antes de acordar el inicio DSP del audio.
                _spawner.ResetBeatTracking(MusicalMap, _config.MinimumSpawnInterval, leadTime, _config.MusicalActivity,
                    _config.Patterns, 0d, _config.Placement, _config.Cadence, _config.AnalyzedSong?.ManualIntensity);
                _conductor.PlayMusic(leadTime);
            }
            catch
            {
                MusicalMap = null;
                StopSession();
                throw;
            }
        }

        public void Tick()
        {
            HandleGameOver();
            if (_disposed || !_model.IsPlaying || _model.IsPaused)
                return;

            try
            {
                if (_conductor.IsAtSongEnd)
                {
                    FinishSong();
                    return;
                }
                _conductor.UpdateTime();
                if (!_conductor.IsPlaying)
                {
                    if (_conductor.HasCompletedSong) FinishSong();
                    else StopSession();
                    return;
                }

                _spawner.CheckAndSpawnTargets();
                HandleGameOver();
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
            try { _conductor.StopMusic(); }
            finally
            {
                try { _targetFactory.ClearTargets(); }
                finally { _falseWordEffects.Clear(); }
            }
        }

        public void RestartSession() => StartSession();

        private void FinishSong()
        {
            // Resolver llegadas pendientes antes de conceder la victoria, incluso en el ultimo frame.
            _targetFactory.ResolveAtSongEnd(_conductor.CurrentSongTime);
            if (_model.IsGameOver) HandleGameOver();
            else
            {
                _model.CompleteSong();
                StopSession();
            }
        }

        // Limpiar fuera de la iteracion de objetivos evita modificar su lista durante una colision.
        public void HandleGameOver()
        {
            if (_disposed || !_model.IsGameOver || _gameOverHandled) return;
            _gameOverHandled = true;
            StopSession();
        }

        public void PauseSession()
        {
            if (_disposed || !_model.IsPlaying || _model.IsPaused) return;
            _conductor.PauseMusic();
            if (!_conductor.IsPlaying)
            {
                if (_conductor.HasCompletedSong) FinishSong();
                else StopSession();
                return;
            }
            _model.PauseSession();
            _falseWordEffects.RefreshPresentation();
        }

        public void ResumeSession()
        {
            if (_disposed || !_model.IsPlaying || !_model.IsPaused) return;
            _conductor.ResumeMusic();
            _model.ResumeSession();
            _falseWordEffects.RefreshPresentation();
        }

        private void ValidateConfiguration()
        {
            if (_config.Beatmap == null)
                throw new InvalidOperationException("Asigna la configuracion de BPM y desfase.");
            if (_config.SessionMusicClip != null && _musicSource == null)
                throw new InvalidOperationException("Asigna un AudioSource para reproducir el clip.");
            if (_config.AnalyzedSong != null && !IsPositiveFinite(_config.MinimumSpawnInterval))
                throw new InvalidOperationException("MinimumSpawnInterval debe ser positivo y finito.");
            if (_config.AnalyzedSong != null)
            {
                if (_config.MusicalActivity != null && _config.MusicalActivity.Enabled)
                    MusicalActivityProfile.ValidateSettings(_config.MusicalActivity, _config.MinimumSpawnInterval);
                if (_config.Patterns != null && _config.Patterns.Enabled)
                    RhythmPatternPlanner.ValidateSettings(_config.Patterns);
                if (_config.Placement != null && _config.Placement.Enabled)
                    RhythmPlacementPlanner.ValidateSettings(_config.Placement);
            }
            if (!IsPositiveFinite(_config.TravelSpeed) || !IsPositiveFinite(_config.ArrivalDistance))
                throw new InvalidOperationException("TravelSpeed y ArrivalDistance deben ser positivos y finitos.");
            if (_config.ShowSpawnRing && !IsPositiveFinite(_config.SpawnRingDurationSeconds))
                throw new InvalidOperationException("La duracion del aro de aparicion debe ser positiva y finita.");
            if (_config.Evasion != null) AimEvasionMotion.Validate(_config.Evasion);
            if (_config.FalseWords != null) FalseWordSelector.Validate(_config.FalseWords);
            if (_config.Bullicio != null) BullicioSessionService.Validate(_config.Bullicio);
            if (_config.Glitch != null) GlitchSignal.Validate(_config.Glitch);
            if (_config.Screamer != null && _config.Screamer.Enabled
                && (!IsPositiveFinite(_config.Screamer.DurationSeconds) || _config.Screamer.DurationSeconds < 0.05f))
                throw new InvalidOperationException("La duracion del screamer debe ser finita y de al menos 0.05 segundos.");
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
