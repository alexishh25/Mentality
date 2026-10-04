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
    private TargetPresenter _presenter;
    private Action _onTouched;

    public Vector3 Position => transform.position;
    public TargetTiming Timing => _presenter?.Timing;
    public bool IsResolved => _presenter == null || _presenter.IsResolved;

    public void RefreshMusicalState()
    {
        if (Timing != null) _presenter?.Tick(0f);
    }

    private void Awake()
    {
        if (wordText == null) wordText = GetComponentInChildren<TextMesh>();
    }

    public void Initialize(TargetPresenter presenter)
    {
        if (_presenter != null) throw new InvalidOperationException("El objetivo ya tiene un presentador.");
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
    }

    public void SetWord(string word)
    {
        if (wordText != null) wordText.text = word;
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
    private void Update() => _presenter?.Tick(Time.deltaTime);
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
