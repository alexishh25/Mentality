using System;
using Mentality.Gameplay.MG01;
using Mentality.UI.ViewModels;
using Mentality.UI.Views;
using UnityEngine;
using VContainer.Unity;

namespace Mentality.UI.Presenters
{
    // Avanza el HudViewModel y solo empuja a la vista lo que cambio (sin asignaciones por frame).
    public sealed class HudPresenter : ITickable
    {
        private readonly HudViewModel _viewModel;
        private readonly HudView _view;
        private readonly IMG01ProgressService _progress;

        private bool _bound;
        private float _lastDisplayed = -1f;
        private float _lastGhostStart = -1f;
        private float _lastGhostWidth = -1f;
        private int _lastValue = -1;
        private int _lastBand = -1;
        private bool _lastHit;
        private string _lastStatus;
        private float _lastShake = float.NaN;
        private float _lastPulse = -1f;
        private float _lastProgress = -1f;

        public HudPresenter(HudViewModel viewModel, HudView view, IMG01ProgressService progress)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public void Tick()
        {
            if (!_bound)
            {
                // El UIDocument crea su arbol en OnEnable; se reintenta hasta que exista.
                if (!_view.TryBind()) return;
                _bound = true;
                _view.SetSanityMax(_viewModel.MaxSanityValue);
                _view.SetDamageText(_viewModel.DamageText);
                _view.SetAbilityUnlocked(_progress.Load().punchUnlocked);
            }

            _viewModel.Tick(Time.deltaTime);
            Render();
        }

        private void Render()
        {
            float displayed = _viewModel.DisplayedSanityNormalized;
            float ghostStart = _viewModel.GhostTrailStart;
            float ghostWidth = _viewModel.GhostTrailWidth;
            if (displayed != _lastDisplayed || ghostStart != _lastGhostStart || ghostWidth != _lastGhostWidth)
            {
                _lastDisplayed = displayed;
                _lastGhostStart = ghostStart;
                _lastGhostWidth = ghostWidth;
                _view.SetSanityBar(displayed, ghostStart, ghostWidth);
            }

            int value = _viewModel.DisplayedSanityValue;
            if (value != _lastValue)
            {
                _lastValue = value;
                _view.SetSanityValue(value);
            }

            int band = (int)_viewModel.Band;
            if (band != _lastBand)
            {
                _lastBand = band;
                _view.SetBand(_viewModel.Band);
            }

            bool hit = _viewModel.IsHitActive;
            if (hit != _lastHit)
            {
                _lastHit = hit;
                _view.SetHitActive(hit);
            }

            string status = _viewModel.StatusText;
            if (!ReferenceEquals(status, _lastStatus))
            {
                _lastStatus = status;
                _view.SetStatus(status);
            }

            float shake = _viewModel.ShakeOffset;
            if (shake != _lastShake)
            {
                _lastShake = shake;
                _view.SetShakeOffset(shake);
            }

            float pulse = _viewModel.CriticalPulse;
            if (pulse != _lastPulse)
            {
                _lastPulse = pulse;
                _view.SetCriticalPulse(pulse);
            }

            float progress = _viewModel.ProgressNormalized;
            if (progress != _lastProgress)
            {
                _lastProgress = progress;
                _view.SetProgress(progress);
            }
        }
    }
}
