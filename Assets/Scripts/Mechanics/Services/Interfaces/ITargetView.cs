using System;
using UnityEngine;

namespace Gameplay.Rhythm.Interfaces
{
    public interface ITargetView
    {
        Vector3 Position { get; }
        void SetPosition(Vector3 position);
        void FacePlayer(Vector3 playerPosition);
        void SetTouchListener(Action listener);
        void DestroySelf();
    }
}
