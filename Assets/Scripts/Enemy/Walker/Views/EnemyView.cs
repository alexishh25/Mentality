
using Gameplay.Enemy.Walker.Interfaces;
using System;
using UnityEngine;

namespace Gameplay.Enemy.Walker.Views
{
    public class EnemyView : MonoBehaviour, IEnemyView
    {
        private Transform playerTarget;
        private float threshold;
        private Action onReachedPlayer;
        private bool isTracking;

        public Transform Transform => transform;

        public void MoveTo(Vector3 direction, float speed)
        {
            transform.position += direction * speed * Time.deltaTime;
            transform.forward = direction;
        }

        public void SetProximityListener(Transform player, float distanceThreshold, Action onCloseEnough)
        {
            playerTarget = player;
            threshold = distanceThreshold;
            onReachedPlayer = onCloseEnough;
            isTracking = true;
        }

        private void Update()
        {
            if (!isTracking || playerTarget == null) return;

            if (Vector3.Distance(transform.position, playerTarget.position) <= threshold)
            {
                isTracking = false;
                onReachedPlayer?.Invoke();
            }
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}