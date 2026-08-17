using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3Data(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class EventoToqueObjetoRequest
{
    public string player_id;
    public string object_id;
    public string prefab_name;
    public int touch_count;
    public Vector3Data player_position;
}

[Serializable]
public class ComandoDuplicadoResponse
{
    public string action;
    public string prefab_name;
    public List<Vector3Data> positions;
    public float duration_seconds;
}