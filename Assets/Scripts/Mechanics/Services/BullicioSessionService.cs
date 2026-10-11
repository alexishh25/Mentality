using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public sealed class BullicioSessionService : IDisposable
    {
        private readonly BullicioSettings _settings;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _clock;
        private readonly FalseWordEffectService _effects;
        private double _until = double.NegativeInfinity;
        private bool _disposed;
        public event Action Started;
        public event Action Cleared;

        public BullicioSessionService(BullicioSettings settings, RhythmGameModel model,
            AudioConductorService clock, FalseWordEffectService effects)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _effects.Requested += OnRequested;
            _effects.Cleared += Clear;
        }

        public bool IsActive => !_disposed && _settings.Enabled && _settings.Clip != null
            && _model.IsPlaying && _clock.IsPlaying && _clock.CurrentSongTime < _until;

        public double ProbabilityBonus => IsActive && !_model.IsPaused && !_clock.IsPaused
            && !_clock.IsWaitingForTimeline ? _settings.FalseWordProbabilityBonus : 0d;

        private void OnRequested(WordTargetKind kind, double songTime)
        {
            if (_disposed || kind != WordTargetKind.Bullicio || !_settings.Enabled || _settings.Clip == null) return;
            Validate(_settings);
            _until = songTime + _settings.DurationSeconds;
            Started?.Invoke();
        }

        public static void Validate(BullicioSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!Finite(settings.DurationSeconds) || settings.DurationSeconds < 0.1f
                || !Unit(settings.Volume) || !Unit(settings.DistortionLevel) || !Unit(settings.FalseWordProbabilityBonus))
                throw new ArgumentException("Los ajustes de bullicio deben ser finitos y respetar sus rangos.", nameof(settings));
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Unit(double value) => Finite(value) && value >= 0 && value <= 1;

        public void Clear()
        {
            _until = double.NegativeInfinity;
            Cleared?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Clear(); }
            finally
            {
                _effects.Requested -= OnRequested;
                _effects.Cleared -= Clear;
                Started = null;
                Cleared = null;
            }
        }
    }
}
