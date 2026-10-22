// PlayerView.cs
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerView : MonoBehaviour
{
    [field: SerializeField] public CharacterController Controller { get; private set; }
    [field: SerializeField] public Transform CameraTransform { get; private set; }
    [field: SerializeField] public Animator Animator { get; private set; }

    [Header("Mesh Settings")]
    [SerializeField] private SkinnedMeshRenderer characterRenderer;

    [Header("Flashlight")]
    [SerializeField] private Light flashlightLight;

    [Header("Cinemachine Cameras")]
    [field: SerializeField] public CinemachineCamera ThirdPersonCamera { get; private set; }
    [field: SerializeField] public CinemachineCamera FirstPersonCamera { get; private set; }

    [Header("Input Controllers")]
    [SerializeField] private CinemachineInputAxisController thirdPersonInputController;

    [Header("Head Bobbing")]
    [SerializeField]
    private CinemachineBasicMultiChannelPerlin fpvNoiseComponent;

    [Header("UI Pointer / Crosshair")]
    [SerializeField] private GameObject pointerUI;

    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Rhythm Interaction")]
    [Min(0.1f)]
    [Tooltip("Alcance del clic en modo ritmico. Debe cubrir la distancia de aparicion de las palabras.")]
    [SerializeField] private float rhythmInteractDistance = 100f;

    private bool _isRhythmMode;
    private bool _savedApplyRootMotion;


    private void Awake()
    {
        SetFlashlightState(false);
    }

    private void Reset()
    {
        Controller = GetComponent<CharacterController>();
        if (Camera.main != null) CameraTransform = Camera.main.transform;
    }

    public void SetRhythmMode(bool active)
    {
        if (_isRhythmMode == active)
            return;

        _isRhythmMode = active;
        if (active)
        {
            // Las animaciones tampoco deben desplazar al jugador fijo.
            if (Animator != null)
            {
                _savedApplyRootMotion = Animator.applyRootMotion;
                Animator.applyRootMotion = false;
            }

            SetHeadBobbingAmplitude(0f);
            SetThirdPersonOrbitState(false);
        }
        else if (Animator != null)
        {
            Animator.applyRootMotion = _savedApplyRootMotion;
        }
    }

    // Método para activar/desactivar la rotación de 3ra persona y el cursor
    public void SetThirdPersonOrbitState(bool active)
    {
        if (thirdPersonInputController != null)
        {
            thirdPersonInputController.enabled = active;
        }
        BloquearCursor(active);
    }


    public void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }

    public void SetFirstPersonLookEnabled(bool active)
    {
        if (FirstPersonCamera != null &&
            FirstPersonCamera.TryGetComponent<CinemachineInputAxisController>(out var inputController))
            inputController.enabled = active;
    }

    public void ActualizarCamaras(CameraPerspective perspectiva)
    {
        if (perspectiva == CameraPerspective.ThirdPerson)
        {
            ThirdPersonCamera.Priority = 10;
            FirstPersonCamera.Priority = 0;
            if (characterRenderer != null) characterRenderer.enabled = true;

            // En 3ra persona iniciamos sin rotar y con cursor visible
            SetPointerUIState(false);
            SetThirdPersonOrbitState(false);
        }
        else
        {
            ThirdPersonCamera.Priority = 0;
            FirstPersonCamera.Priority = 10;
            if (characterRenderer != null) characterRenderer.enabled = false;

            // En 1ra persona siempre bloqueado
            SetPointerUIState(true);
            BloquearCursor(true);
        }
    }

    public void ActualizarVelocidadAnimacion(int idAnimation, float velocidad)
    {
        if (Animator != null) Animator.SetFloat(idAnimation, velocidad, 0.1f, Time.deltaTime);
    }

    public void ActualizarEstadoSueloAnimacion(int idAnimation, bool estaEnSuelo)
    {
        if (Animator != null) Animator.SetBool(idAnimation, estaEnSuelo);
    }

    public void DispararGatilloAnimacion(int idAnimation)
    {
        if (Animator != null) Animator.SetTrigger(idAnimation);
    }

    public void SetFlashlightState(bool isOn)
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isOn;
        }
    }

    public void SetHeadBobbingAmplitude(float amplitude)
    {
        if (fpvNoiseComponent != null)
        {
            fpvNoiseComponent.AmplitudeGain = amplitude;
        }
    }

    public void SetPointerUIState(bool active)
    {
        if (pointerUI != null)
        {
            pointerUI.SetActive(active);
        }
    }

    //public bool PerformFirstPersonRaycast(out RaycastHit hitInfo)
    //{
    //    Ray ray = new Ray(CameraTransform.position, CameraTransform.forward);
    //    return Physics.Raycast(ray, out hitInfo, interactDistance, interactableLayer);
    //}

    public bool PerformFirstPersonRaycast(out RaycastHit hitInfo, bool drawDebug = true)
    {
        Ray ray = new Ray(CameraTransform.position, CameraTransform.forward);
        float distance = _isRhythmMode ? rhythmInteractDistance : interactDistance;

        // Dibujar el rayo en la vista de escena de Unity (durante 2 segundos en verde)
        if (drawDebug) Debug.DrawRay(ray.origin, ray.direction * distance, Color.green, 2f);

        bool result = Physics.Raycast(ray, out hitInfo, distance, interactableLayer,
            _isRhythmMode ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.UseGlobal);
        return result;
    }

    public void PlayActionFeedback(Vector3 point)
    {
        Debug.Log($"[PlayerView] Impacto o interacción en la posición: {point}");
    }
}
