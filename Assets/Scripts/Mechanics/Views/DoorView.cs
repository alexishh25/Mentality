using System;
using Gameplay.Doors.Interfaces;
using UnityEngine;

namespace Gameplay.Doors.Views
{
    public class DoorView : MonoBehaviour, IDoorView, ITouchable
    {
        [SerializeField] private GameObject visualObject;

        private Transform playerTransform;
        private float distanceThreshold;
        private Action onNearAction;
        private Action onTouchAction;
        private bool isTrackingProximity;

        public Transform Transform => transform;
        public bool IsActive { get; private set; } = true;

        public void SetActiveState(bool active)
        {
            IsActive = active;
            if (visualObject != null) visualObject.SetActive(active);
            else gameObject.SetActive(active);
        }

        public void SetProximityListener(Transform player, float triggerDistance, Action onPlayerNear)
        {
            playerTransform = player;
            distanceThreshold = triggerDistance;
            onNearAction = onPlayerNear;
            isTrackingProximity = true;
        }

        public void SetTouchListener(Action onTouch)
        {
            onTouchAction = onTouch;
        }

        public void Touch(Vector3 playerPosition)
        {
            if (IsActive)
            {
                onTouchAction?.Invoke();
            }
        }

        private void Update()
        {
            if (!isTrackingProximity || !IsActive || playerTransform == null) return;

            if (Vector3.Distance(transform.position, playerTransform.position) <= distanceThreshold)
            {
                isTrackingProximity = false;
                onNearAction?.Invoke();
            }
        }
    }
}