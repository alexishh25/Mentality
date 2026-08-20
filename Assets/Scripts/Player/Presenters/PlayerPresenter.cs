// PlayerPresenter.cs
using UnityEngine;
using VContainer.Unity;
using Mentality.Domain.Player;

public class PlayerPresenter : ITickable
{
    private readonly IPlayerInputService _inputService;
    private readonly PlayerModel _model;
    private readonly PlayerView _view;
    private readonly PlayerState _state;

    public PlayerPresenter(IPlayerInputService inputService, PlayerModel model, PlayerView view)
    {
        _inputService = inputService;
        _model = model;
        _view = view;
        _state = model.GetOrCreateState();
    }

    public void Start()
    {
        _view.ActualizarCamaras(_state.CurrentPerspective);
    }

    public void Tick()
    {
        ProcesarCambioCamara();
        ActualizarEstadoSuelo();
        ProcesarOrbitTerceraPersona();
        ProcesarLinterna();

        Vector3 velocidadHorizontal = CalcularMovimientoHorizontal();
        CalcularMovimientoVertical();

        // Aplicamos el movimiento físico
        Vector3 velocidadFinal = velocidadHorizontal + Vector3.up * _model.Velocity.y;
        _view.Controller.Move(velocidadFinal * Time.deltaTime);

        // --- ACTUALIZAMOS LAS ANIMACIONES ---
        float velocidadNormalizada = velocidadHorizontal.magnitude / _state.MoveSpeed;
        ProcesarHeadBobbing(velocidadNormalizada);
        ActualizarAnimaciones(velocidadNormalizada);
    }

    private void ActualizarEstadoSuelo()
    {
        _state.IsGrounded = _view.Controller.isGrounded;

        if (_state.IsGrounded && _model.Velocity.y < 0)
        {
            _model.Velocity.y = -2f;
        }
    }

    private void ProcesarOrbitTerceraPersona()
    {
        if (_state.CurrentPerspective == CameraPerspective.ThirdPerson)
        {
            bool holdingRightClick = _inputService.IsRightClickPressed;
            _view.SetThirdPersonOrbitState(holdingRightClick);
        }
    }

    private void ProcesarCambioCamara()
    {
        if (_inputService.SwitchCameraPressed)
        {
            // Ejecutar regla de dominio (cambia perspectiva y apaga linterna si pasa a 3ra persona)
            _state.SwitchPerspective();

            _view.SetFlashlightState(_state.IsFlashlightOn);
            _view.ActualizarCamaras(_state.CurrentPerspective);
        }
    }

    private Vector3 CalcularMovimientoHorizontal()
    {
        Vector2 input = _inputService.MoveInput;
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        if (_state.CurrentPerspective == CameraPerspective.FirstPerson)
        {
            // --- LÓGICA EN 1ª PERSONA ---
            Vector3 targetRotation = new Vector3(0f, _view.CameraTransform.eulerAngles.y, 0f);
            _view.transform.rotation = Quaternion.Euler(targetRotation);

            if (direction.magnitude >= 0.1f)
            {
                Vector3 moveDirection = _view.transform.right * input.x + _view.transform.forward * input.y;
                return moveDirection.normalized * _state.MoveSpeed;
            }
        }
        else
        {
            // --- LÓGICA EN 3ª PERSONA ---
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _view.CameraTransform.eulerAngles.y;
                float angle = Mathf.LerpAngle(_view.transform.eulerAngles.y, targetAngle, Time.deltaTime * _state.RotationSpeed);
                _view.transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                return moveDirection.normalized * _state.MoveSpeed;
            }
        }

        return Vector3.zero;
    }

    private void CalcularMovimientoVertical()
    {
        if (_inputService.JumpPressed && _state.IsGrounded)
        {
            // Calcular velocidad de salto desde el Dominio
            _model.Velocity.y = _state.CalculateJumpVerticalVelocity();

            // Disparar animación en la Vista
            _view.DispararGatilloAnimacion(_model.JumpHash);
        }

        _model.Velocity.y += _state.Gravity * Time.deltaTime;
    }

    private void ActualizarAnimaciones(float velocidadMovimiento)
    {
        _view.ActualizarVelocidadAnimacion(_model.SpeedHash, velocidadMovimiento);
        _view.ActualizarEstadoSueloAnimacion(_model.IsGroundedHash, _state.IsGrounded);
    }

    private void ProcesarLinterna()
    {
        if (_inputService.FlashlightPressed)
        {
            // Ejecutar regla de dominio
            bool isNowOn = _state.ToggleFlashlight();
            _view.SetFlashlightState(isNowOn);
        }
    }

    private void ProcesarHeadBobbing(float velocidadNormalizada)
    {
        if (_state.CurrentPerspective == CameraPerspective.FirstPerson && _state.IsGrounded && velocidadNormalizada > 0.1f)
        {
            _view.SetHeadBobbingAmplitude(_state.BobbingAmplitude * velocidadNormalizada);
        }
        else
        {
            _view.SetHeadBobbingAmplitude(0f);
        }
    }
}