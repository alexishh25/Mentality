using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SongBeatmap
{
    public string songName;
    [Min(1f)]
    public float bpm = 120f;

    [Tooltip("Tiempo en segundos del primer beat respecto al inicio de la cancion. Puede ser negativo.")]
    public float offsetSeconds;

    public float SecondsPerBeat => 60f / Mathf.Max(1f, bpm);

    // Se conserva para el presentador existente hasta migrarlo al spawn por BPM.
    // Lista de tiempos en segundos donde debe aparecer un objetivo
    public List<float> targetSpawnTimes = new List<float>();
}
