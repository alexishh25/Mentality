using System;
using System.Collections;
using UnityEngine;

namespace Gameplay
{
    public class LockerView : MonoBehaviour, ILockerView, ITouchable
    {
        [Header("Settings")]
        [SerializeField] private Transform drawerTransform;
        [SerializeField] private GameObject consumablePrefab;
        [SerializeField] private Vector3 openOffset = new Vector3(1.9f, 0, 0);
        [SerializeField] private float openDuration = 0.5f;

        private Action onInteractAction;
        private Vector3 closedPosition;

        // Devuelve el Transform del cajón para saber desde dónde instanciar
        public Transform SpawnPoint => drawerTransform;
        public GameObject ConsumablePrefab => consumablePrefab;

        private void Awake()
        {
            if (drawerTransform != null)
            {
                closedPosition = drawerTransform.localPosition;
            }
        }

        public void SetInteractionListener(Action listener) => onInteractAction = listener;
        public void OnInteracted() => onInteractAction?.Invoke();
        public void Touch(Vector3 playerPosition) => OnInteracted();

        public void AnimateOpen(Action onComplete) => StartCoroutine(AnimateRoutine(closedPosition + openOffset, onComplete));
        public void AnimateClose(Action onComplete) => StartCoroutine(AnimateRoutine(closedPosition, onComplete));

        private IEnumerator AnimateRoutine(Vector3 targetPosition, Action onComplete)
        {
            Vector3 startPosition = drawerTransform.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < openDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / openDuration);
                drawerTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            drawerTransform.localPosition = targetPosition;
            onComplete?.Invoke();
        }
    }
}