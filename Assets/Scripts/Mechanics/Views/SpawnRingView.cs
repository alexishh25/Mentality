using System;
using UnityEngine;
using UnityEngine.Rendering;

// Efecto independiente: permanece en el origen mientras la palabra se acerca al jugador.
public sealed class SpawnRingView : MonoBehaviour
{
    private AudioConductorService _clock;
    private LineRenderer _ring;
    private double _start;
    private double _duration;

    public void Initialize(AudioConductorService clock, Vector2 radius, double duration)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        _duration = duration;
        _start = clock.CurrentSongTime;
        var material = Resources.Load<Material>("Rhythm/TimingCue");
        if (material == null) throw new InvalidOperationException("Falta el material del aro de aparicion.");
        _ring = gameObject.AddComponent<LineRenderer>();
        _ring.sharedMaterial = material;
        _ring.useWorldSpace = false;
        _ring.loop = true;
        _ring.positionCount = 48;
        _ring.widthMultiplier = 0.045f;
        _ring.shadowCastingMode = ShadowCastingMode.Off;
        _ring.receiveShadows = false;
        _ring.lightProbeUsage = LightProbeUsage.Off;
        _ring.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", new Color(0.3f, 0.95f, 0.8f));
        _ring.SetPropertyBlock(properties);
        for (int i = 0; i < _ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / _ring.positionCount;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y, 0f));
        }
    }

    private void Update()
    {
        if (_clock == null || !_clock.IsPlaying) { Cancel(); return; }
        if (_clock.IsPaused || _clock.IsWaitingForTimeline) return;
        double age = _clock.CurrentSongTime - _start;
        if (age >= _duration) { Cancel(); return; }
        _ring.widthMultiplier = 0.045f * (1f - Mathf.Clamp01((float)(age / _duration)));
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
