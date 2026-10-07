using System;
using UnityEngine;

namespace Gameplay.Enemy.Walker.Interfaces
{
    public interface IEnemyView
    {
        Transform Transform { get; }
        void MoveTo(Vector3 direction, float speed);
        void SetProximityListener(Transform player, float distanceThreshold, Action onCloseEnough);
        void DestroySelf();
    }
}