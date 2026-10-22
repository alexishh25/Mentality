using System;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Gameplay.Rhythm.Presenters
{
    public sealed class WordImpactBlurPresenter : IPostTickable, IDisposable
    {
        private const int EffectLayer = 2; // Isolated from the scene's Default-layer volumes.
        private readonly RhythmGameConfig _config;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _clock;
        private readonly FalseWordEffectService _effects;
        private Volume _volume;
        private VolumeProfile _profile;
        private DepthOfField _blur;
        private UniversalAdditionalCameraData _cameraData;
        private bool _previousPostProcessing, _cameraSettingsApplied, _disposed;
        private CameraOverrideOption _previousDepth;
        private LayerMask _previousVolumeMask;
        private double _started = double.NegativeInfinity;
        public float Strength { get; private set; }
        public bool IsActive => _cameraSettingsApplied && Strength > 0f;
        private WordImpactBlurSettings Settings => _config.ImpactBlur;

        public WordImpactBlurPresenter(RhythmGameConfig config, RhythmGameModel model,
            AudioConductorService clock, FalseWordEffectService effects)
        {
            _config = config; _model = model; _clock = clock; _effects = effects;
            _model.WordCollided += OnImpact;
            _effects.Cleared += Clear;
            _effects.PresentationChanged += PostTick;
        }

        private void OnImpact()
        {
            var settings = Settings;
            if (_disposed || settings == null || !settings.Enabled || !_model.IsPlaying || _model.IsPaused || !_clock.IsPlaying) return;
            if (_volume == null && !CreateEffect()) return;
            if (!_cameraSettingsApplied)
            {
                _previousPostProcessing = _cameraData.renderPostProcessing;
                _previousDepth = _cameraData.requiresDepthOption;
                _previousVolumeMask = _cameraData.volumeLayerMask;
                // When postprocessing was off, avoid also introducing unrelated scene grading.
                _cameraData.volumeLayerMask = _previousPostProcessing ? _previousVolumeMask.value | (1 << EffectLayer) : 1 << EffectLayer;
                _cameraData.requiresDepthTexture = true;
                _cameraData.renderPostProcessing = true;
                _cameraSettingsApplied = true;
            }
            _started = _clock.CurrentSongTime;
            _volume.enabled = true;
            Render();
        }

        private bool CreateEffect()
        {
            var camera = Camera.main;
            if (camera == null) return false;
            _cameraData = camera.GetUniversalAdditionalCameraData();
            var go = new GameObject("Word impact blur") { layer = EffectLayer };
            go.transform.SetParent(camera.transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true; _volume.priority = 1000f; _volume.weight = 1f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "Runtime word impact blur";
            _blur = _profile.Add<DepthOfField>(true);
            _blur.mode.Override(DepthOfFieldMode.Gaussian);
            _blur.gaussianStart.Override(0f); _blur.gaussianEnd.Override(1f);
            // URP's editor default starts at radius 0.5; allow a continuous fade to zero.
            _blur.gaussianMaxRadius = new ClampedFloatParameter(0f, 0f, 1f, true);
            _blur.highQualitySampling.Override(false);
            _volume.sharedProfile = _profile;
            return true;
        }

        public void PostTick()
        {
            if (_disposed) return;
            if (!_model.IsPlaying || !_clock.IsPlaying || Settings == null || !Settings.Enabled) Clear();
            else Render();
        }

        private void Render()
        {
            if (!_cameraSettingsApplied || _volume == null) return;
            float duration = Mathf.Max(0.08f, Settings.DurationSeconds);
            float progress = (float)((_clock.CurrentSongTime - _started) / duration);
            if (progress >= 1f || progress < 0f) { Clear(); return; }
            Strength = Mathf.Clamp(Settings.Radius, 0.1f, 1f) * (1f - Mathf.SmoothStep(0f, 1f, progress));
            _blur.gaussianMaxRadius.value = Strength;
            // URP's composited radius has a minimum. Fade the far-blur amount as
            // well so the final part returns smoothly to the sharp camera image.
            _blur.gaussianEnd.value = Mathf.Lerp(120f, 1f, Strength / Mathf.Clamp(Settings.Radius, 0.1f, 1f));
        }

        private void Clear()
        {
            Strength = 0f; _started = double.NegativeInfinity;
            if (_volume != null) _volume.enabled = false;
            if (_cameraSettingsApplied && _cameraData != null)
            {
                _cameraData.renderPostProcessing = _previousPostProcessing;
                _cameraData.requiresDepthOption = _previousDepth;
                _cameraData.volumeLayerMask = _previousVolumeMask;
            }
            _cameraSettingsApplied = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _model.WordCollided -= OnImpact;
            _effects.Cleared -= Clear;
            _effects.PresentationChanged -= PostTick;
            Clear();
            if (_volume != null) Object.Destroy(_volume.gameObject);
            if (_blur != null) Object.Destroy(_blur);
            if (_profile != null) Object.Destroy(_profile);
        }
    }
}
