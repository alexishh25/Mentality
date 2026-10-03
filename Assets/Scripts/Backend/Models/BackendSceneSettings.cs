using UnityEngine;

// Configuracion opcional por escena. Las escenas sin este componente conservan el backend.
[DisallowMultipleComponent]
public sealed class BackendSceneSettings : MonoBehaviour
{
    [SerializeField] private bool enableBackend = true;
    public bool EnableBackend => enableBackend;
}
