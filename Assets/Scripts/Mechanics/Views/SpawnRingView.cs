using System;
using UnityEngine;
using UnityEngine.Rendering;
using Gameplay.Rhythm.Views;

// Efecto independiente: permanece en el origen mientras la palabra se acerca al jugador.
public sealed class SpawnRingView : MonoBehaviour
{
    private AudioConductorService _clock;
    private LineRenderer _ring;
    private double _start;
    private double _duration;
    private bool _audiencePulse;
    private Vector2 _radius;
    public AudienceWordEmitter SourceEmitter { get; private set; }
    public bool IsVisible => _ring != null && _ring.enabled;

    public void InitializeAudience(AudioConductorService clock, AudienceWordEmitter emitter, Vector3 player,
        double duration, double? scheduledStart = null)
    {
        if (emitter == null || emitter.WordOrigin == null) throw new ArgumentException("Audience pulse needs a mouth origin.");
        SourceEmitter = emitter;
        _audiencePulse = true;
        transform.SetParent(emitter.WordOrigin, true);
        transform.position = emitter.WordOrigin.position;
        transform.rotation = Quaternion.LookRotation(transform.position - player, Vector3.up);
        Initialize(clock, new Vector2(0.065f, 0.05f), duration, scheduledStart);
    }

    public void Initialize(AudioConductorService clock, Vector2 radius, double duration, double? scheduledStart = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        _duration = duration;
        _start = scheduledStart ?? clock.CurrentSongTime;
        _radius = radius;
        var material = Resources.Load<Material>("Rhythm/TimingCue");
        if (material == null) throw new InvalidOperationException("Falta el material del aro de aparicion.");
        _ring = gameObject.AddComponent<LineRenderer>();
        _ring.sharedMaterial = material;
        _ring.useWorldSpace = false;
        _ring.loop = true;
        _ring.positionCount = 48;
        _ring.widthMultiplier = _audiencePulse ? 0.028f : 0.045f;
        _ring.shadowCastingMode = ShadowCastingMode.Off;
        _ring.receiveShadows = false;
        _ring.lightProbeUsage = LightProbeUsage.Off;
        _ring.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", _audiencePulse ? new Color(0.5f, 1f, 0.95f) : new Color(0.3f, 0.95f, 0.8f));
        _ring.SetPropertyBlock(properties);
        for (int i = 0; i < _ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / _ring.positionCount;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y, 0f));
        }
        RefreshVisual(clock.CurrentSongTime);
    }

    private void Update()
    {
        if (_clock == null || !_clock.IsPlaying) { Cancel(); return; }
        if (_clock.IsPaused || _clock.IsWaitingForTimeline) return;
        if (_audiencePulse && (SourceEmitter == null || SourceEmitter.WordOrigin == null)) { Cancel(); return; }
        RefreshVisual(_clock.CurrentSongTime);
    }

    private void RefreshVisual(double songTime)
    {
        double age = songTime - _start;
        _ring.enabled = age >= 0d;
        if (age < 0d) return;
        if (age >= _duration) { Cancel(); return; }
        float progress = Mathf.Clamp01((float)(age / _duration));
        _ring.widthMultiplier = (_audiencePulse ? 0.028f : 0.045f) * (1f - progress);
        if (_audiencePulse)
        {
            float expansion = Mathf.Lerp(1f, 2.6f, Mathf.SmoothStep(0f, 1f, progress));
            for (int i = 0; i < _ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / _ring.positionCount;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * _radius.x, Mathf.Sin(angle) * _radius.y, 0f) * expansion);
            }
        }
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
