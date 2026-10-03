using System;
using UnityEngine;

namespace Gameplay
{
    public interface ILockerView
    {
        Transform SpawnPoint { get; }
        void OnInteracted(); // Se llama cuando el jugador interactúa con el casillero
        GameObject ConsumablePrefab { get; }
        void SetInteractionListener(Action listener);
        void AnimateOpen(Action onComplete);
        void AnimateClose(Action onComplete);
    }
}