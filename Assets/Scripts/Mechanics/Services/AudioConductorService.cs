using System;
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
    private double _clipDuration;
    private bool _initialized;
    private bool _usesAudio;

    // Tiempo desde el inicio del clip, sin descontar el desfase.
    public float SongPosition => (float)_songPosition;
    public double SecondsPerBeat => _secondsPerBeat;
    public double SongPositionInBeats => _initialized
        ? (_songPosition - _offsetSeconds) / _secondsPerBeat
        : 0d;

    public bool IsPlaying { get; private set; }
    public bool HasStarted { get; private set; }

    // -1 indica que todavia no ha ocurrido ningun beat de esta reproduccion.
    // El consumidor debe comprobar IsPlaying antes de generar objetivos.
    public int CurrentBeatIndex => !HasStarted || SongPositionInBeats < 0d
        ? -1
        : (int)Math.Floor(SongPositionInBeats);

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
        _secondsPerBeat = 60d / bpm;
        _offsetSeconds = offsetSeconds;
        _songPosition = 0d;
        HasStarted = false;
        _initialized = true;
    }

    // Inicia una nueva reproduccion desde cero; no reanuda una sesion detenida.
    public void PlayMusic()
    {
        if (!_initialized)
            throw new InvalidOperationException("Inicializa el reloj antes de reproducir.");

        StopMusic();
        _songPosition = 0d;
        HasStarted = false;
        _usesAudio = _musicSource != null && _musicSource.clip != null;
        _dspSongTime = AudioSettings.dspTime + StartDelaySeconds;

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

    public void RestartMusic() => PlayMusic();

    public void StopMusic()
    {
        if (IsPlaying)
            UpdateTime();

        IsPlaying = false;
        if (_usesAudio && _musicSource != null)
            _musicSource.Stop();

        _usesAudio = false;
    }

    // El futuro presentador llamara a este metodo antes de consultar los beats.
    public void UpdateTime()
    {
        if (!IsPlaying)
            return;

        double elapsed = AudioSettings.dspTime - _dspSongTime;
        if (elapsed < 0d)
            return;

        HasStarted = true;
        _songPosition = elapsed;

        if (_usesAudio && _songPosition >= _clipDuration)
        {
            _songPosition = _clipDuration;
            IsPlaying = false;
            if (_musicSource != null)
                _musicSource.Stop();
        }
    }

    public void Dispose() => StopMusic();
}
