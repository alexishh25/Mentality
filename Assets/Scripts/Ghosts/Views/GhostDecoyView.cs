using System;
using Gameplay;
using Gameplay.Ghosts.Presenters;
using UnityEngine;

namespace Gameplay.Ghosts.Views
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GhostDecoyView : MonoBehaviour, ITouchable
    {
        [SerializeField] private TextMesh wordText;
        private GhostDecoyPresenter _presenter;
        private BoxCollider _clickCollider;

        private void Awake()
        {
            if (wordText == null) wordText = GetComponentInChildren<TextMesh>(true);
            _clickCollider = GetComponent<BoxCollider>();
            // The player passes through; the trigger remains raycastable for click penalties.
            if (_clickCollider != null) _clickCollider.isTrigger = true;
        }

        public void Initialize(GhostDecoyPresenter presenter, string word)
        {
            if (_presenter != null) throw new InvalidOperationException("El decoy ya fue inicializado.");
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            if (wordText == null) wordText = GetComponentInChildren<TextMesh>(true);
            if (wordText != null) wordText.text = word;
        }

        public Vector3 Position => transform.position;
        public void SetPosition(Vector3 value) => transform.position = value;

        public void Touch(Vector3 playerPosition) => _presenter?.OnTouched();

        private void Update() => _presenter?.Tick(Time.deltaTime);

        public void DestroySelf()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDestroy() => _presenter?.Dispose();
    }
}
