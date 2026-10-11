using System;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;
using VContainer.Unity;

namespace Gameplay.Rhythm.Presenters
{
    public sealed class BullicioPresenter : IPostTickable, IDisposable
    {
        private readonly BullicioSettings _settings;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _clock;
        private readonly BullicioSessionService _session;
        private readonly FalseWordEffectService _effects;
        private GameObject _audioObject;
        private AudioSource _source;
        private AudioDistortionFilter _distortion;
        private bool _paused;
        private bool _started;
        private bool _disposed;

        public BullicioPresenter(BullicioSettings settings, RhythmGameModel model,
            AudioConductorService clock, BullicioSessionService session, FalseWordEffectService effects)
        {
            _settings = settings;
            _model = model;
            _clock = clock;
            _session = session;
            _effects = effects;
            _session.Started += StartAudio;
            _session.Cleared += StopAudio;
            _effects.PresentationChanged += PostTick;
        }

        private void StartAudio()
        {
            if (_disposed || !_session.IsActive) return;
            if (_audioObject == null)
            {
                _audioObject = new GameObject("Bullicio audio");
                _source = _audioObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
                _source.loop = true;
                _distortion = _audioObject.AddComponent<AudioDistortionFilter>();
            }
            _source.volume = _settings.Volume;
            _distortion.enabled = true;
            _distortion.distortionLevel = _settings.DistortionLevel;
            // Nuevas colisiones prolongan un unico sonido, sin sumar fuentes ni reiniciar el fragmento.
            if (!_started || _source.clip != _settings.Clip)
            {
                _source.Stop();
                _source.clip = _settings.Clip;
                _source.time = 0f;
                _source.Play();
                _started = true;
                _paused = false;
            }
        }

        public void PostTick()
        {
            if (_disposed) return;
            if (!_session.IsActive) { StopAudio(); return; }
            if (!_started) StartAudio();
            if (_source == null) return;
            bool paused = _model.IsPaused || _clock.IsPaused || _clock.IsWaitingForTimeline;
            if (paused && !_paused) { _source.Pause(); _paused = true; }
            else if (!paused && _paused) { _source.UnPause(); _paused = false; }
            _source.volume = _settings.Volume;
            _distortion.distortionLevel = _settings.DistortionLevel;
        }

        private void StopAudio()
        {
            if (_source != null && _started) _source.Stop();
            if (_distortion != null) _distortion.enabled = false;
            _paused = false;
            _started = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _session.Started -= StartAudio;
            _session.Cleared -= StopAudio;
            _effects.PresentationChanged -= PostTick;
            StopAudio();
            if (_audioObject != null) UnityEngine.Object.Destroy(_audioObject);
        }
    }
}
