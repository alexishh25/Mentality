using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class RhythmPlacementSettings
    {
        public bool Enabled = true;
        [Range(15f, 90f)] public float MaximumSideAngle = 75f;
        [Range(5f, 45f)] public float LaneSpacingDegrees = 15f;
        [Range(15f, 180f)] public float MaximumTurnDegrees = 90f;
        [Min(15f)] public float MaximumTurnSpeedDegreesPerSecond = 120f;
        [Range(2f, 30f), Tooltip("Ancho angular maximo de la palabra al lanzarse. Conserva su tamano fisico durante el vuelo.")]
        public float TargetAngularWidthDegrees = 10f;
    }
}
