using System;
using Gameplay.Rhythm.Models;
using UnityEngine;

public class AudioConductorService : IDisposable
{
    // Margen para que Unity prepare el audio antes del instante DSP acordado.
    private const double StartDelaySeconds = 0.1d;

    private AudioSource _musicSource;
    private double _secondsPerBeat;
    private double _offsetSeconds;
    private double _songPosition;
    private double _dspSongTime;
    private double _dspTimelineTime;
    private double _countInSeconds;
    private double _clipDuration;
    private double _minimumSongTime;
    private bool _initialized;
    private bool _usesAudio;
    private double[] _mappedBeats;

    // Tiempo respecto al inicio del clip; negativo durante la cuenta de entrada.
    public float SongPosition => (float)_songPosition;
    public double SongTime => _songPosition;
    // Lectura DSP sin cambiar el estado: el movimiento y el clic no dependen del orden de Update.
    public double CurrentSongTime
    {
        get
        {
            if (!IsPlaying || IsPaused) return _songPosition;
            double time = Math.Max(_minimumSongTime, AudioSettings.dspTime - _dspSongTime);
            return _usesAudio ? Math.Min(time, _clipDuration) : time;
        }
    }
    public double SecondsPerBeat => _secondsPerBeat;
    public double SongPositionInBeats => _initialized
        ? (_songPosition - _offsetSeconds) / _secondsPerBeat
        : 0d;

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public bool HasCompletedSong { get; private set; }
    public bool IsAtSongEnd => _usesAudio && IsPlaying && !IsPaused && !IsWaitingForTimeline
        && CurrentSongTime >= _clipDuration;
    public bool IsWaitingForTimeline => IsPlaying && !IsPaused && AudioSettings.dspTime < _dspTimelineTime;
    public bool HasStarted { get; private set; }
    public bool HasTimelineStarted { get; private set; }
    public bool IsCountingIn => IsPlaying && !IsPaused && _countInSeconds > 0d && !HasStarted;
    public double CountInRemainingSeconds => IsCountingIn ? Math.Max(0d, -_songPosition) : 0d;

    // -1 indica que todavia no ha ocurrido ningun beat de esta reproduccion.
    // El consumidor debe comprobar IsPlaying antes de generar objetivos.
    public int CurrentBeatIndex
    {
        get
        {
            if (!HasStarted) return -1;
            if (_mappedBeats != null)
            {
                int index = Array.BinarySearch(_mappedBeats, _songPosition);
                return index >= 0 ? index : ~index - 1;
            }
            return SongPositionInBeats < 0d ? -1 : (int)Math.Floor(SongPositionInBeats);
        }
    }

    public void Initialize(AudioSource source, MusicalMapData map)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        Initialize(source, (float)map.Metadata.TempoBpm, 0f);
        _mappedBeats = map.Events.Beats;
    }

    public double CurrentBeatTime => CurrentBeatIndex < 0 ? -1d
        : _mappedBeats != null ? _mappedBeats[CurrentBeatIndex]
        : _offsetSeconds + CurrentBeatIndex * _secondsPerBeat;

    public void Initialize(AudioSource source, SongBeatmap beatmap)
    {
        if (beatmap == null)
            throw new ArgumentNullException(nameof(beatmap));

        Initialize(source, beatmap.bpm, beatmap.offsetSeconds);
    }

    // Conserva la llamada anterior Initialize(source, bpm).
    // source puede ser null para probar el ritmo sin musica.
    public void Initialize(AudioSource source, float bpm, float offsetSeconds = 0f)
    {
        if (float.IsNaN(bpm) || float.IsInfinity(bpm) || bpm < 1f)
            throw new ArgumentOutOfRangeException(nameof(bpm), "El BPM debe ser finito y mayor o igual a 1.");

        if (float.IsNaN(offsetSeconds) || float.IsInfinity(offsetSeconds))
            throw new ArgumentOutOfRangeException(nameof(offsetSeconds), "El desfase debe ser finito.");

        StopMusic();
        _musicSource = source;
        _mappedBeats = null;
        _secondsPerBeat = 60d / bpm;
        _offsetSeconds = offsetSeconds;
        _songPosition = 0d;
        HasStarted = false;
        HasTimelineStarted = false;
        _countInSeconds = 0d;
        _initialized = true;
        HasCompletedSong = false;
    }

    // Inicia una nueva reproduccion desde cero; no reanuda una sesion detenida.
    public void PlayMusic(double countInSeconds = 0d)
    {
        if (!_initialized)
            throw new InvalidOperationException("Inicializa el reloj antes de reproducir.");
        if (double.IsNaN(countInSeconds) || double.IsInfinity(countInSeconds) || countInSeconds < 0d)
            throw new ArgumentOutOfRangeException(nameof(countInSeconds));

        StopMusic();
        _countInSeconds = countInSeconds;
        HasCompletedSong = false;
        _songPosition = -countInSeconds;
        _minimumSongTime = _songPosition;
        HasStarted = false;
        HasTimelineStarted = false;
        _usesAudio = _musicSource != null && _musicSource.clip != null;
        _dspTimelineTime = AudioSettings.dspTime + StartDelaySeconds;
        _dspSongTime = _dspTimelineTime + countInSeconds;

        if (_usesAudio)
        {
            // Requiere un AudioSource dedicado, sin cambios externos de velocidad.
            _musicSource.Stop();
            _musicSource.playOnAwake = false;
            _musicSource.loop = false;
            _musicSource.pitch = 1f;
            _musicSource.dopplerLevel = 0f;
            _musicSource.timeSamples = 0;
            _clipDuration = (double)_musicSource.clip.samples / _musicSource.clip.frequency;
            _musicSource.PlayScheduled(_dspSongTime);
        }

        IsPlaying = true;
    }

    public void RestartMusic() => PlayMusic(_countInSeconds);

    public void PauseMusic()
    {
        if (!IsPlaying || IsPaused) return;
        UpdateTime();
        if (!IsPlaying) return;
        _songPosition = CurrentSongTime;
        IsPaused = true;
        // Cancelar tambien un PlayScheduled pendiente durante la cuenta de entrada.
        if (_usesAudio && _musicSource != null) _musicSource.Stop();
    }

    public void ResumeMusic()
    {
        if (!IsPlaying || !IsPaused) return;
        _minimumSongTime = _songPosition;
        _dspTimelineTime = AudioSettings.dspTime + StartDelaySeconds;
        _dspSongTime = _dspTimelineTime - _songPosition;
        HasTimelineStarted = false;
        if (_usesAudio && _musicSource != null)
        {
            _musicSource.Stop();
            int sample = (int)Math.Max(0d, Math.Min(_musicSource.clip.samples - 1d,
                Math.Round(Math.Max(0d, _songPosition) * _musicSource.clip.frequency)));
            _musicSource.timeSamples = sample;
            _musicSource.PlayScheduled(_songPosition < 0d ? _dspSongTime : _dspTimelineTime);
        }
        IsPaused = false;
    }

    public void StopMusic()
    {
        if (IsPlaying)
            UpdateTime();

        IsPlaying = false;
        IsPaused = false;
        if (_usesAudio && _musicSource != null)
            _musicSource.Stop();

        _usesAudio = false;
    }

    // El futuro presentador llamara a este metodo antes de consultar los beats.
    public void UpdateTime()
    {
        if (!IsPlaying || IsPaused)
            return;

        double dspNow = AudioSettings.dspTime;
        if (dspNow < _dspTimelineTime) return;

        HasTimelineStarted = true;
        _songPosition = dspNow - _dspSongTime;
        HasStarted = _songPosition >= 0d;

        if (_usesAudio && _songPosition >= _clipDuration)
        {
            _songPosition = _clipDuration;
            HasCompletedSong = true;
            IsPlaying = false;
            if (_musicSource != null)
                _musicSource.Stop();
        }
    }

    public void Dispose() => StopMusic();
}
