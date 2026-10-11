using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace Gameplay.Rhythm.Presenters
{
    public sealed class ScreamerPresenter : IPostTickable, IDisposable
    {
        private readonly RhythmGameConfig _config;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _clock;
        private readonly FalseWordEffectService _effects;
        private readonly ScreamerTimeline _timeline = new ScreamerTimeline();
        private GameObject _overlay;
        private RawImage _image;
        private GameObject _audioObject;
        private AudioSource _source;
        private bool _audioPaused;
        private Texture2D _previousImage;
        private readonly List<Texture2D> _candidates = new List<Texture2D>();
        private bool _disposed;

        public ScreamerPresenter(RhythmGameConfig config, RhythmGameModel model,
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
            var settings = _config.Screamer;
            if (_disposed || kind != WordTargetKind.Screamer || settings == null
                || !settings.Enabled) return;
            Texture2D selected = SelectImage(settings);
            if (selected == null) return;
            if (_overlay == null) CreateOverlay();
            _image.texture = selected;
            _previousImage = selected;
            _timeline.Trigger(songTime, settings.DurationSeconds);
            Render();
            PlaySound(settings);
        }

        private void PlaySound(ScreamerSettings settings)
        {
            if (_source != null) _source.Stop();
            _audioPaused = false;
            if (settings.Clip == null) return;
            // Independent of the overlay: the clip finishes after the image disappears.
            if (_audioObject == null)
            {
                _audioObject = new GameObject("Screamer audio");
                _source = _audioObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.loop = false;
                _source.spatialBlend = 0f;
            }
            _source.clip = settings.Clip;
            _source.volume = Mathf.Clamp01(settings.Volume);
            _source.Play();
        }

        private Texture2D SelectImage(ScreamerSettings settings)
        {
            _candidates.Clear();
            if (settings.Images != null)
                foreach (var candidate in settings.Images)
                    if (candidate != null && !_candidates.Contains(candidate)) _candidates.Add(candidate);
            if (_candidates.Count == 0) return settings.Image;
            if (_candidates.Count > 1) _candidates.Remove(_previousImage);
            return _candidates[UnityEngine.Random.Range(0, _candidates.Count)];
        }

        private void CreateOverlay()
        {
            _overlay = new GameObject("Screamer overlay", typeof(RectTransform), typeof(Canvas));
            var canvas = _overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var imageObject = new GameObject("Screamer image", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(_overlay.transform, false);
            _image = imageObject.GetComponent<RawImage>();
            _image.raycastTarget = false;
            var rect = _image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _overlay.SetActive(false);
        }

        public void PostTick()
        {
            if (_disposed) return;
            if (!_model.IsPlaying || !_clock.IsPlaying || _config.Screamer == null || !_config.Screamer.Enabled) Clear();
            else
            {
                Render();
                if (_source == null) return;
                bool paused = _model.IsPaused || _clock.IsPaused || _clock.IsWaitingForTimeline;
                if (paused && !_audioPaused && _source.isPlaying)
                {
                    _source.Pause();
                    _audioPaused = true;
                }
                else if (!paused && _audioPaused)
                {
                    _source.UnPause();
                    _audioPaused = false;
                }
                _source.volume = Mathf.Clamp01(_config.Screamer.Volume);
            }
        }

        private void Render()
        {
            if (_overlay == null) return;
            var settings = _config.Screamer;
            bool visible = settings != null && settings.Enabled && _image.texture != null
                && _model.IsPlaying && !_model.IsPaused && _clock.IsPlaying && !_clock.IsPaused
                && !_clock.IsWaitingForTimeline && _timeline.IsVisible(_clock.CurrentSongTime);
            // Recorte centrado para llenar la pantalla sin deformar la imagen.
            if (visible && Screen.height > 0)
            {
                float screenAspect = (float)Screen.width / Screen.height;
                float imageAspect = (float)_image.texture.width / _image.texture.height;
                float width = Mathf.Min(1f, screenAspect / imageAspect);
                float height = Mathf.Min(1f, imageAspect / screenAspect);
                _image.uvRect = new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);
            }
            if (_overlay.activeSelf != visible) _overlay.SetActive(visible);
        }

        private void Clear()
        {
            _timeline.Clear();
            if (_source != null) _source.Stop();
            _audioPaused = false;
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
            if (_audioObject != null) UnityEngine.Object.Destroy(_audioObject);
        }
    }
}
