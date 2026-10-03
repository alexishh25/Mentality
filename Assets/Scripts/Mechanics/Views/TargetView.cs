using System;
using Gameplay;
using Gameplay.Rhythm.Interfaces;
using Gameplay.Rhythm.Presenters;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TargetView : MonoBehaviour, ITouchable, ITargetView
{
    [SerializeField] private TextMesh wordText;

    private TargetPresenter _presenter;
    private Action _onTouched;

    public Vector3 Position => transform.position;

    private void Awake()
    {
        if (wordText == null)
            wordText = GetComponentInChildren<TextMesh>();
    }

    public void Initialize(TargetPresenter presenter)
    {
        if (_presenter != null)
            throw new InvalidOperationException("El objetivo ya tiene un presentador.");

        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
    }

    public void SetWord(string word)
    {
        if (wordText != null)
            wordText.text = word;
    }

    public void SetPosition(Vector3 position) => transform.position = position;

    public void FacePlayer(Vector3 playerPosition)
    {
        // El frente de TextMesh se lee desde su eje -Z.
        Vector3 direction = transform.position - playerPosition;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void SetTouchListener(Action listener) => _onTouched = listener;

    public void Touch(Vector3 playerPosition) => _onTouched?.Invoke();

    private void Update() => _presenter?.Tick(Time.deltaTime);

    public void Cancel() => _presenter?.Cancel();

    public void DestroySelf()
    {
        // Destroy es diferido: desactivar evita otro raycast durante este frame.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
        _onTouched = null;
    }
}
