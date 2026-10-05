using Gameplay.Ghosts.Models;
using Gameplay.Ghosts.Services;
using Gameplay.Ghosts.Views;
using System;
using UnityEngine;
using VContainer.Unity;

namespace Gameplay.Ghosts.Presenters
{
    public sealed class GhostConsequencesPresenter : IStartable, ITickable, IDisposable,
        IGhostConsequences, IScreamerEffect
    {
        private readonly GhostFeatureSettings _settings;
        private readonly GhostSanityModel _sanity;
        private readonly GhostEffectsView _view;
        private float _screamerRemaining;

        public GhostConsequencesPresenter(GhostFeatureSettings settings, GhostSanityModel sanity,
            GhostEffectsView view)
        {
            _settings = settings;
            _sanity = sanity;
            _view = view;
        }

        public void Start() => RenderState();

        public void OnGhostClicked()
        {
            _sanity.Lose(_settings.SanityLossPerClick);
            _view.TriggerStaticBurst();
            RenderState();
            if (_settings.EnableRandomScreamer) TryTriggerScreamer();
        }

        public void ResetForSession()
        {
            _sanity.Reset();
            _screamerRemaining = 0f;
            _view.SetScreamerVisible(false);
            RenderState();
        }

        public void TriggerScreamer()
        {
            _screamerRemaining = _settings.ScreamerDurationSeconds;
            _view.SetScreamerVisible(true);
        }

        public bool TryTriggerScreamer()
        {
            if (UnityEngine.Random.value > _settings.ScreamerChancePerClick) return false;
            TriggerScreamer();
            return true;
        }

        public void Tick()
        {
            _view.Tick(Time.unscaledDeltaTime);
            if (_screamerRemaining <= 0f) return;
            _screamerRemaining -= Time.unscaledDeltaTime;
            if (_screamerRemaining <= 0f) _view.SetScreamerVisible(false);
        }

        private void RenderState()
        {
            _view.SetSanity(_sanity.Normalized, _sanity.Current, _sanity.Maximum);
            // Opacity grows gradually as sanity is lost; even one penalty creates a visible trace.
            float damage = 1f - _sanity.Normalized;
            float opacity = damage <= 0f ? 0f : _settings.MaximumStaticOpacity * Mathf.Sqrt(damage);
            _view.SetStaticOpacity(opacity);
        }

        public void Dispose()
        {
            _view.SetScreamerVisible(false);
        }
    }
}
