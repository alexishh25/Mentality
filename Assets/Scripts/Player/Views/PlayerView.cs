// PlayerView.cs
using Unity.Cinemachine;
using UnityEngine;
using Mentality.Domain.Player;

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


    private void Awake()
    {
        SetFlashlightState(false);
    }

    private void Reset()
    {
        Controller = GetComponent<CharacterController>();
        if (Camera.main != null) CameraTransform = Camera.main.transform;
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

    public void ActualizarCamaras(CameraPerspective perspectiva)
    {
        if (perspectiva == CameraPerspective.ThirdPerson)
        {
            ThirdPersonCamera.Priority = 10;
            FirstPersonCamera.Priority = 0;
            if (characterRenderer != null) characterRenderer.enabled = true;

            // En 3ra persona iniciamos sin rotar y con cursor visible
            SetThirdPersonOrbitState(false);
        }
        else
        {
            ThirdPersonCamera.Priority = 0;
            FirstPersonCamera.Priority = 10;
            if (characterRenderer != null) characterRenderer.enabled = false;

            // En 1ra persona siempre bloqueado
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
}