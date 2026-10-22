using System;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [Serializable]
    public sealed class CreatureSettings
    {
        public bool Enabled;
        [Tooltip("Disparar al completar la emergencia, sin espera de preparacion; retirarse al terminar la rafaga.")]
        public bool ContinuousAttack;
        public GameObject Prefab;
        public AudioClip AppearanceClip;
        [Range(0f, 1f)] public float AppearanceVolume = 0.7f;
        [Min(2f)] public float Distance = 10f;
        [Range(15f, 75f)] public float MaximumAngle = 60f;
        [Min(1)] public int FirstEveryBeats = 8;
        [Min(1)] public int BridgeEveryBeats = 6;
        [Min(1)] public int SecondEveryBeats = 4;
        [Range(1, 8)] public int FirstBurst = 2;
        [Range(1, 8)] public int BridgeBurst = 3;
        [Range(1, 8)] public int SecondBurst = 4;
    }
}
