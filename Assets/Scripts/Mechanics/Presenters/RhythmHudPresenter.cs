using System;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Gameplay.Rhythm.Presenters
{
    public class RhythmHudPresenter : IStartable, IPostTickable, IDisposable
    {
        private readonly RhythmGameModel _model;
        private readonly RhythmGamePresenter _game;
        private readonly AudioConductorService _conductor;
        private readonly RhythmHudView _view;
        private readonly PlayerView _playerView;
        private readonly IPlayerInputService _playerInput;
        private readonly RhythmSessionInputService _sessionInput;
        private bool _captured;
        private bool _actionsEnabled;
        private bool _resumeRequested;
        private bool _pausedThisFrame;
        private int _pauseControlsFrame = -1;
        private int _restartFrame = -1;

        public RhythmHudPresenter(RhythmGameModel model, RhythmGameConfig config,
            RhythmGamePresenter game, AudioConductorService conductor, RhythmHudView view,
            PlayerView playerView, IPlayerInputService playerInput, RhythmSessionInputService sessionInput)
        {
            _model = model;
            _game = game;
            _conductor = conductor;
            _view = view;
            _playerView = playerView;
            _playerInput = playerInput;
            _sessionInput = sessionInput;
        }

        public void Start()
        {
            _view.RestartRequested += RestartFromButton;
            InputSystem.onAfterUpdate += ProcessPauseControls;
            Application.focusChanged += OnFocusChanged;
            SetCaptured(true);
            if (!_sessionInput.HasFocus) OnFocusChanged(false);
        }

        private void OnFocusChanged(bool focused)
        {
            if (focused) return;
            _game.PauseSession();
            SetCaptured(false);
        }

        private void ProcessPauseControls()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic || _pauseControlsFrame == Time.frameCount) return;
            _pauseControlsFrame = Time.frameCount;
            _resumeRequested = false;
            _pausedThisFrame = false;
            if (!_sessionInput.HasFocus)
            {
                _game.PauseSession();
                if (_captured) SetCaptured(false);
            }
            else if (_sessionInput.TogglePausePressed && _model.IsPlaying)
            {
                if (_model.IsPaused) _resumeRequested = true;
                else { _game.PauseSession(); SetCaptured(false); _pausedThisFrame = true; }
            }
        }

        // Despues del PlayerPresenter: el clic para recuperar el cursor no dispara una palabra.
        public void PostTick()
        {
            _game.HandleGameOver();
            if (!_sessionInput.HasFocus)
            {
                if (_captured) SetCaptured(false);
            }
            else if (_sessionInput.RestartPressed)
            {
                RestartLevel();
            }
            else if (_model.IsPaused && !_pausedThisFrame && (_resumeRequested || _sessionInput.CapturePressed))
            {
                _game.ResumeSession();
                SetCaptured(true);
            }

            if (!_model.IsPlaying && _captured) SetCaptured(false);
            bool allowActions = _captured && _model.IsPlaying && !_model.IsPaused && !_conductor.IsWaitingForTimeline;
            if (allowActions != _actionsEnabled)
            {
                _actionsEnabled = allowActions;
                if (allowActions) _playerInput.EnableInput();
                else _playerInput.DisableInput();
            }

            double age = _conductor.CurrentSongTime - _model.LastJudgmentSongTime;
            bool showCombo = _model.IsPlaying && !_model.IsPaused && !_conductor.IsWaitingForTimeline
                && _model.LastJudgment != RhythmJudgment.None;
            _view.RenderSanity(_model.Sanity);
            _view.ShowGameOver(_model.IsGameOver);
            _view.ShowVictory(_model.HasWon);
            _view.RenderCombo(_model.Combo, _model.ScoreMultiplier, _model.LastBrokenCombo, age, showCombo, _model.LastPulseBonus);
        }

        private void RestartFromButton()
        {
            if ((!_model.IsGameOver && !_model.HasWon) || !_sessionInput.HasFocus) return;
            RestartLevel();
        }

        private void RestartLevel()
        {
            if (_restartFrame == Time.frameCount) return;
            _restartFrame = Time.frameCount;
            _resumeRequested = false;
            _pausedThisFrame = false;
            _pauseControlsFrame = Time.frameCount;
            SetCaptured(false);
            _game.RestartSession();
            _view.ResetSessionPresentation(_model.Sanity);
            SetCaptured(true);
        }
        private void SetCaptured(bool captured)
        {
            _captured = captured;
            _actionsEnabled = captured && _model.IsPlaying && !_model.IsPaused && !_conductor.IsWaitingForTimeline;
            if (_actionsEnabled) _playerInput.EnableInput();
            else _playerInput.DisableInput();
            _playerView.SetFirstPersonLookEnabled(captured);
            _playerView.BloquearCursor(captured);
            _playerView.SetPointerUIState(captured);
        }

        public void Dispose()
        {
            _view.RestartRequested -= RestartFromButton;
            InputSystem.onAfterUpdate -= ProcessPauseControls;
            Application.focusChanged -= OnFocusChanged;
            _playerInput.DisableInput();
            if (_playerView != null)
            {
                _playerView.BloquearCursor(false);
                _playerView.SetFirstPersonLookEnabled(true);
            }
        }
    }
}

