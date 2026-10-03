using System;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using VContainer.Unity;

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

        public void Start() => SetCaptured(true);

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
            else if (_sessionInput.ToggleCursorPressed)
            {
                SetCaptured(!_captured && _model.IsPlaying);
            }
            else if (!_captured && _model.IsPlaying && _sessionInput.CapturePressed)
            {
                SetCaptured(true);
            }

            if (!_model.IsPlaying && _captured) SetCaptured(false);

            string status = _model.IsPlaying ? "SESION ACTIVA" : "SESION FINALIZADA";
            string mode = _config.MusicClip == null ? "RELOJ SIN MUSICA" : "MUSICA";
            int beat = _conductor.CurrentBeatIndex;
            string rhythm = $"{_config.Beatmap.bpm:0.#} BPM  /  {mode}  /  BEAT {(beat < 0 ? "--" : (beat + 1).ToString())}";
            string hint = !_model.IsPlaying ? "R para iniciar otra sesion"
                : _captured ? "APUNTA Y HAZ CLIC  |  ESC libera el cursor  |  R reinicia"
                : "CURSOR LIBRE: clic para volver  |  El ritmo continua  |  R reinicia";
            double phase = _conductor.SongPositionInBeats;
            float pulse = _conductor.IsPlaying && beat >= 0
                ? (float)Math.Max(0d, 1d - (phase - Math.Floor(phase)) * 4d) : 0f;
            _view.Render(_model.Hits, _model.Misses, status, rhythm, hint, pulse);
        }

        private void SetCaptured(bool captured)
        {
            _captured = captured;
            if (captured) _playerInput.EnableInput();
            else _playerInput.DisableInput();
            _playerView.SetFirstPersonLookEnabled(captured);
            _playerView.BloquearCursor(captured);
            _playerView.SetPointerUIState(captured);
        }

        public void Dispose()
        {
            _playerInput.DisableInput();
            if (_playerView != null)
            {
                _playerView.BloquearCursor(false);
                _playerView.SetFirstPersonLookEnabled(true);
            }
        }
    }
}
