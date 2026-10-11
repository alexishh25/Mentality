using System;
using Gameplay;
using Gameplay.Rhythm.Interfaces;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TargetView : MonoBehaviour, ITouchable, ITargetView
{
    [SerializeField] private TextMesh wordText;
    [SerializeField] private WordTargetKind targetKind;
    public WordTargetKind Kind => targetKind;
    private TargetPresenter _presenter;
    private Action _onTouched;
    private FalseWordDistortion _distortion;
    private AudioConductorService _distortionClock;
    private double _distortionStart;
    private double _distortionInterval;

    public Vector3 Position => transform.position;
    public TargetTiming Timing => _presenter?.Timing;
    public bool IsResolved => _presenter == null || _presenter.IsResolved;

    public void RefreshMusicalState()
    {
        if (Timing != null) _presenter?.Tick(0f);
    }

    public void ResolveAtSongEnd(double songTime) => _presenter?.ResolveAtSongEnd(songTime);

    private void Awake()
    {
        if (wordText == null) wordText = GetComponentInChildren<TextMesh>();
    }

    public void Initialize(TargetPresenter presenter)
    {
        if (_presenter != null) throw new InvalidOperationException("El objetivo ya tiene un presentador.");
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        targetKind = presenter.Kind;
    }

    public void SetWord(string word)
    {
        if (wordText != null) wordText.text = word;
        if (_distortion != null)
        {
            _distortion = new FalseWordDistortion(word);
            RefreshDistortion();
        }
    }

    public void ConfigureDistortion(FalseWordSettings settings, AudioConductorService clock)
    {
        FalseWordSelector.Validate(settings);
        if (wordText == null) wordText = GetComponentInChildren<TextMesh>(true);
        if (wordText == null) throw new InvalidOperationException("La palabra falsa necesita TextMesh.");
        _distortionClock = clock ?? throw new ArgumentNullException(nameof(clock));
        _distortionStart = clock.CurrentSongTime;
        _distortionInterval = settings.DistortionIntervalSeconds;
        _distortion = new FalseWordDistortion(wordText.text);
        ApplyDistortedText(_distortion.At(0, _distortionInterval));
    }

    private void RefreshDistortion()
    {
        if (_distortion == null || IsResolved || !_distortionClock.IsPlaying
            || _distortionClock.IsPaused || _distortionClock.IsWaitingForTimeline) return;
        ApplyDistortedText(_distortion.At(_distortionClock.CurrentSongTime - _distortionStart, _distortionInterval));
    }

    private void ApplyDistortedText(string text)
    {
        if (wordText.text == text) return;
        wordText.text = text;
        var renderer = wordText.GetComponent<Renderer>();
        if (renderer == null) return;
        var local = renderer.localBounds;
        var bounds = new Bounds(transform.InverseTransformPoint(renderer.transform.TransformPoint(local.center)), Vector3.zero);
        for (int corner = 0; corner < 8; corner++)
        {
            var offset = new Vector3((corner & 1) == 0 ? -local.extents.x : local.extents.x,
                (corner & 2) == 0 ? -local.extents.y : local.extents.y,
                (corner & 4) == 0 ? -local.extents.z : local.extents.z);
            bounds.Encapsulate(transform.InverseTransformPoint(renderer.transform.TransformPoint(local.center + offset)));
        }
        if (bounds.size.x <= 0 || bounds.size.y <= 0) return;
        var box = GetComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = new Vector3(bounds.size.x + 0.05f, bounds.size.y + 0.05f, Mathf.Max(box.size.z, bounds.size.z));
        Physics.SyncTransforms();
    }

    public void SetPosition(Vector3 position) => transform.position = position;

    public void ConfigureLaunchSize(Vector3 playerPosition, RhythmPlacementSettings settings)
    {
        RhythmPlacementPlanner.ValidateSettings(settings);
        if (wordText == null) wordText = GetComponentInChildren<TextMesh>();
        var box = GetComponent<BoxCollider>();
        double width = box.size.x * Mathf.Abs(transform.lossyScale.x);
        if (wordText != null)
        {
            var renderer = wordText.GetComponent<Renderer>();
            if (renderer != null) width = Math.Max(width, renderer.bounds.size.x);
        }
        double distance = Vector3.Distance(transform.position, playerPosition);
        // Ajustar solo al lanzar. El tamano fisico se conserva durante todo el recorrido.
        transform.localScale *= RhythmPlacementPlanner.ReadabilityScale(distance, distance, width,
            settings.TargetAngularWidthDegrees);
    }

    public void FacePlayer(Vector3 playerPosition)
    {
        Vector3 direction = transform.position - playerPosition;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void SetTouchListener(Action listener) => _onTouched = listener;
    public void Touch(Vector3 playerPosition) => _onTouched?.Invoke();
    public void SetAimed(bool aimed) => _presenter?.SetAimed(aimed);
    private void Update()
    {
        _presenter?.Tick(Time.deltaTime);
        RefreshDistortion();
    }
    public void Cancel() => _presenter?.Cancel();

    public void DestroySelf()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
        _onTouched = null;
    }
}
