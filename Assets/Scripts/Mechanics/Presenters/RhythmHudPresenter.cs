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
        private readonly RhythmGameConfig _config;
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

        public RhythmHudPresenter(RhythmGameModel model, RhythmGameConfig config,
            RhythmGamePresenter game, AudioConductorService conductor, RhythmHudView view,
            PlayerView playerView, IPlayerInputService playerInput, RhythmSessionInputService sessionInput)
        {
            _model = model;
            _config = config;
            _game = game;
            _conductor = conductor;
            _view = view;
            _playerView = playerView;
            _playerInput = playerInput;
            _sessionInput = sessionInput;
        }

        public void Start()
        {
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
            if (!_sessionInput.HasFocus)
            {
                if (_captured) SetCaptured(false);
            }
            else if (_sessionInput.RestartPressed)
            {
                _game.RestartSession();
                SetCaptured(true);
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

            string status = _model.IsPlaying ? "SESION ACTIVA" : "SESION FINALIZADA";
            if (_conductor.IsCountingIn)
                status = $"PREPARATE  {Math.Ceiling(_conductor.CountInRemainingSeconds):0}";
            string mode = _config.SessionMusicClip == null ? "RELOJ SIN MUSICA" : "MUSICA";
            int beat = _conductor.CurrentBeatIndex;
            double bpm = _game.MusicalMap != null ? _game.MusicalMap.Metadata.TempoBpm : _config.Beatmap.bpm;
            string source = _game.MusicalMap != null ? "MAPA MUSICAL" : mode;
            string rhythm = $"{bpm:0.#} BPM  /  {source}  /  BEAT {(beat < 0 ? "--" : (beat + 1).ToString())}";
            string hint = !_model.IsPlaying ? "R para iniciar otra sesion"
                : _captured ? "APUNTA Y HAZ CLIC  |  ESC / P pausa  |  R reinicia"
                : "PAUSA: clic, ESC o P para volver  |  R reinicia";
            if (_conductor.IsCountingIn && _captured)
                hint = "PREPARATE: las palabras ya se acercan  |  ESC / P pausa  |  R reinicia";
            else if (_model.IsPlaying && _captured && _game.MusicalMap != null)
                hint = "DISPARA A LAS PALABRAS ANTES DE QUE LLEGUEN  |  ESC / P pausa  |  R reinicia";
            if (_model.IsPaused)
            {
                status = "EN PAUSA";
                hint = "MUSICA Y OBJETIVOS EN PAUSA  |  Clic, ESC o P para volver  |  R reinicia";
            }
            else if (_conductor.IsWaitingForTimeline && _conductor.HasStarted)
                status = "REANUDANDO";
            float pulse = _conductor.IsPlaying && beat >= 0
                ? (float)Math.Max(0d, 1d - (_conductor.SongTime - _conductor.CurrentBeatTime)
                    / (_conductor.SecondsPerBeat * 0.25d)) : 0f;
            RhythmJudgment shownJudgment = RhythmJudgment.None;
            double resultAge = _conductor.CurrentSongTime - _model.LastJudgmentSongTime;
            if (_model.IsPlaying && !_model.IsPaused && !_conductor.IsWaitingForTimeline && _model.LastJudgment != RhythmJudgment.None
                && resultAge >= 0d && resultAge <= RhythmFeedbackFormatter.DurationSeconds)
            {
                shownJudgment = _model.LastJudgment;
                status = RhythmFeedbackFormatter.Status(shownJudgment, _model.LastTimingErrorSeconds);
            }
            _view.Render(_model.Hits, _model.Misses, status, rhythm, hint, pulse, shownJudgment);
            _view.RenderScore(_model.Score, _model.Combo, _model.BestCombo, _model.ScoreMultiplier,
                shownJudgment == RhythmJudgment.None ? "" : RhythmFeedbackFormatter.Result(_model),
                shownJudgment, resultAge);
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
