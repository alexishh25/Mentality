using System;
using UnityEngine;

namespace Gameplay.Doors.Models
{
    [Serializable]
    public class DoorConfig
    {
        public Transform IsolationRoomPoint; // Punto destino en la sala vacía
        public float ProximityDistance = 3f;   // Distancia para que la puerta "se mueva"
        public float ImmobilizeDuration = 4f;  // Tiempo de castigo sin moverse
    }
}