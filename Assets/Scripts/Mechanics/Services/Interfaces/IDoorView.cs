using System;
using UnityEngine;

namespace Gameplay.Doors.Interfaces
{
    public interface IDoorView
    {
        Transform Transform { get; }
        bool IsActive { get; }
        void SetActiveState(bool active);
        void SetProximityListener(Transform player, float triggerDistance, Action onPlayerNear);
    }
}