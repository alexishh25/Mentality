using System;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace Gameplay.Rhythm.Presenters
{
    public sealed class GlitchPresenter : IPostTickable, IDisposable
    {
        private const int Width = 128, Height = 72;
        private readonly RhythmGameConfig _config;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _clock;
        private readonly FalseWordEffectService _effects;
        private readonly GlitchSignal _signal = new GlitchSignal();
        private readonly byte[] _pixels = new byte[Width * Height * 4];
        private GameObject _overlay;
        private RawImage _darkness, _static;
        private Texture2D _texture;
        private long _lastFrame = -1;
        private bool _disposed;

        public GlitchPresenter(RhythmGameConfig config, RhythmGameModel model,
            AudioConductorService clock, FalseWordEffectService effects)
        {
            _config = config;
            _model = model;
            _clock = clock;
            _effects = effects;
            _effects.Requested += OnRequested;
            _effects.Cleared += Clear;
            _effects.PresentationChanged += PostTick;
        }

        private void OnRequested(WordTargetKind kind, double songTime)
        {
            var settings = _config.Glitch;
            if (_disposed || kind != WordTargetKind.Glitch || settings == null || !settings.Enabled) return;
            GlitchSignal.Validate(settings);
            if (_overlay == null) CreateOverlay();
            _signal.Trigger(songTime, settings.DurationSeconds);
            _lastFrame = -1;
            Render();
        }

        private void CreateOverlay()
        {
            _overlay = new GameObject("Glitch overlay", typeof(RectTransform), typeof(Canvas));
            var canvas = _overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29000; // Debajo del screamer.
            _darkness = CreateLayer("Darkness");
            _darkness.texture = Texture2D.whiteTexture;
            _static = CreateLayer("Static");
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            _texture.name = "Runtime glitch static";
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _static.texture = _texture;
            _overlay.SetActive(false);
        }

        private RawImage CreateLayer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            layer.transform.SetParent(_overlay.transform, false);
            var image = layer.GetComponent<RawImage>();
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        public void PostTick()
        {
            if (_disposed) return;
            if (!_model.IsPlaying || !_clock.IsPlaying) Clear();
            else Render();
        }

        private void Render()
        {
            if (_overlay == null) return;
            var settings = _config.Glitch;
            bool visible = settings != null && settings.Enabled && _model.IsPlaying && !_model.IsPaused
                && _clock.IsPlaying && !_clock.IsPaused && !_clock.IsWaitingForTimeline
                && _signal.IsVisible(_clock.CurrentSongTime);
            if (visible)
            {
                _darkness.color = new Color(0f, 0f, 0f, settings.Darkness);
                _static.color = new Color(1f, 1f, 1f, settings.StaticOpacity);
                long frame = _signal.FrameAt(_clock.CurrentSongTime, settings.StaticFramesPerSecond);
                if (frame != _lastFrame)
                {
                    GlitchSignal.FillStatic(_pixels, Width, Height, frame);
                    _texture.LoadRawTextureData(_pixels);
                    _texture.Apply(false, false);
                    _lastFrame = frame;
                }
            }
            if (_overlay.activeSelf != visible) _overlay.SetActive(visible);
        }

        private void Clear()
        {
            _signal.Clear();
            _lastFrame = -1;
            if (_overlay != null) _overlay.SetActive(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _effects.Requested -= OnRequested;
            _effects.Cleared -= Clear;
            _effects.PresentationChanged -= PostTick;
            Clear();
            if (_overlay != null) UnityEngine.Object.Destroy(_overlay);
            if (_texture != null) UnityEngine.Object.Destroy(_texture);
        }
    }
}
