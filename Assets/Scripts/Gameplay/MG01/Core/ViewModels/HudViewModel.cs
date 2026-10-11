using System;
using Mentality.Gameplay.MG01;
using UnityEngine;

namespace Mentality.UI.ViewModels
{
    // Estado del HUD de MG01. C# puro: sin asignaciones por frame (los textos se construyen una vez).
    public sealed class HudViewModel : IDisposable
    {
        private const float SnapThreshold = 0.001f;
        private const int ShakeCycles = 3;

        private readonly ISanityService _sanity;
        private readonly ISongProgressSource _song;
        private readonly float _lerpSpeed;
        private readonly float _hitDuration;
        private readonly float _ghostDuration;
        private readonly float _shakeDuration;
        private readonly float _shakeAmplitude;
        private readonly float _pulsePeriod;
        private readonly string _hitStatusText;

        private float _lastTarget;
        private float _hitRemaining;
        private float _hitElapsed;
        private float _ghostRemaining;
        private float _ghostStartWidth;
        private float _pulseTime;

        public float TargetSanityNormalized => Mathf.Clamp01(_sanity.Normalized);
        public float DisplayedSanityNormalized { get; private set; }
        public float ProgressNormalized => Mathf.Clamp01(_song.Normalized);

        public int DisplayedSanityValue => Mathf.RoundToInt(DisplayedSanityNormalized * _sanity.Max);
        public int MaxSanityValue => Mathf.RoundToInt(_sanity.Max);
        public SanityBand Band => SanityBands.FromNormalized(TargetSanityNormalized);

        public bool IsHitActive => _hitRemaining > 0f;
        public string DamageText { get; }
        public string StatusText => IsHitActive ? _hitStatusText : SanityBands.StatusText(Band);

        // Estela fantasma: bloque que nace al final del nuevo relleno y colapsa a 0.
        public float GhostTrailStart => TargetSanityNormalized;
        public float GhostTrailWidth => _ghostDuration <= 0f ? 0f : _ghostStartWidth * (_ghostRemaining / _ghostDuration);

        // Sacudida horizontal del panel en px (3 ciclos amortiguados).
        public float ShakeOffset
        {
            get
            {
                if (!IsHitActive || _shakeDuration <= 0f || _hitElapsed >= _shakeDuration) return 0f;
                float t = _hitElapsed / _shakeDuration;
                return _shakeAmplitude * Mathf.Sin(t * ShakeCycles * 2f * Mathf.PI) * (1f - t);
            }
        }

        // 0..1 solo en estado critico; 0 en el resto.
        public float CriticalPulse => Band == SanityBand.Critical
            ? 0.5f - 0.5f * Mathf.Cos(_pulseTime / _pulsePeriod * 2f * Mathf.PI)
            : 0f;

        public HudViewModel(ISanityService sanity, ISongProgressSource song, MG01ConfigSO config)
        {
            _sanity = sanity ?? throw new ArgumentNullException(nameof(sanity));
            _song = song ?? throw new ArgumentNullException(nameof(song));
            if (config == null) throw new ArgumentNullException(nameof(config));

            _lerpSpeed = Mathf.Max(0.01f, config.HudLerpSpeed);
            _hitDuration = Mathf.Max(0f, config.HitFeedbackDuration);
            _ghostDuration = Mathf.Max(0f, config.GhostTrailDuration);
            _shakeDuration = Mathf.Max(0f, config.HitShakeDuration);
            _shakeAmplitude = Mathf.Max(0f, config.HitShakeAmplitude);
            _pulsePeriod = Mathf.Max(0.01f, config.CriticalPulsePeriod);

            int damage = Mathf.RoundToInt(config.DamagePerHit);
            DamageText = string.Format(MG01Texts.DamageFormat, damage);
            _hitStatusText = string.Format(MG01Texts.StatusHitFormat, damage);

            _lastTarget = TargetSanityNormalized;
            DisplayedSanityNormalized = _lastTarget;
            _sanity.Changed += OnSanityChanged;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            float target = TargetSanityNormalized;
            float displayed = Mathf.Lerp(DisplayedSanityNormalized, target, 1f - Mathf.Exp(-_lerpSpeed * deltaTime));
            if (Mathf.Abs(displayed - target) < SnapThreshold) displayed = target;
            DisplayedSanityNormalized = Mathf.Clamp01(displayed);

            if (_hitRemaining > 0f)
            {
                _hitRemaining = Mathf.Max(0f, _hitRemaining - deltaTime);
                _hitElapsed += deltaTime;
            }

            if (_ghostRemaining > 0f)
                _ghostRemaining = Mathf.Max(0f, _ghostRemaining - deltaTime);

            _pulseTime = (_pulseTime + deltaTime) % _pulsePeriod;
        }

        private void OnSanityChanged(float current, float max)
        {
            float target = TargetSanityNormalized;
            if (target < _lastTarget)
            {
                _hitRemaining = _hitDuration;
                _hitElapsed = 0f;
                _ghostStartWidth = _lastTarget - target;
                _ghostRemaining = _ghostDuration;
            }
            _lastTarget = target;
        }

        public void Dispose()
        {
            _sanity.Changed -= OnSanityChanged;
        }
    }
}
