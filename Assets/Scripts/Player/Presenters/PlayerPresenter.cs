// PlayerPresenter.cs
using UnityEngine;
using VContainer.Unity;

public class PlayerPresenter : ITickable, IStartable
{
    private readonly IPlayerInputService _inputService;
    private readonly PlayerModel _model;
    private readonly PlayerView _view;

    public PlayerPresenter(IPlayerInputService inputService, PlayerModel model, PlayerView view)
    {
        _inputService = inputService;
        _model = model;
        _view = view;
    }

    public void Start()
    {
        _view.ActualizarCamaras(_model.CurrentPerspective);
    }
    public void Tick()
    {
        ProcesarCambioCamara();
        ActualizarEstadoSuelo();
        ProcesarOrbitTerceraPersona();
        ProcesarLinterna();
        ProcesarAccionClic();

        Vector3 velocidadHorizontal = CalcularMovimientoHorizontal();
        CalcularMovimientoVertical();

        // Aplicamos el movimiento físico
        Vector3 velocidadFinal = velocidadHorizontal + Vector3.up * _model.Velocity.y;
        _view.Controller.Move(velocidadFinal * Time.deltaTime);

        // --- ACTUALIZAMOS LAS ANIMACIONES ---
        float velocidadNormalizada = velocidadHorizontal.magnitude / _model.MoveSpeed;
        ProcesarHeadBobbing(velocidadNormalizada);
        ActualizarAnimaciones(velocidadNormalizada);
    }

    private void ActualizarEstadoSuelo()
    {
        _model.IsGrounded = _view.Controller.isGrounded;

        if (_model.IsGrounded && _model.Velocity.y < 0)
        {
            _model.Velocity.y = -2f;
        }
    }
    private void ProcesarOrbitTerceraPersona()
    {
        if (_model.CurrentPerspective == CameraPerspective.ThirdPerson)
        {
            bool holdingRightClick = _inputService.IsRightClickPressed;
            _view.SetThirdPersonOrbitState(holdingRightClick);
        }
    }
    //private void ProcesarCambioCamara()
    //{
    //    if (_inputService.SwitchCameraPressed)
    //    {
    //        _model.CurrentPerspective = _model.CurrentPerspective == CameraPerspective.ThirdPerson
    //            ? CameraPerspective.FirstPerson
    //            : CameraPerspective.ThirdPerson;

    //        _view.ActualizarCamaras(_model.CurrentPerspective);
    //    }
    //}

    private void ProcesarCambioCamara()
    {
        if (_inputService.SwitchCameraPressed)
        {
            _model.CurrentPerspective = _model.CurrentPerspective == CameraPerspective.ThirdPerson
                ? CameraPerspective.FirstPerson
                : CameraPerspective.ThirdPerson;

            // Regla de seguridad: Si pasa a 3ª persona, apaga la linterna
            if (_model.CurrentPerspective == CameraPerspective.ThirdPerson && _model.IsFlashlightOn)
            {
                _model.IsFlashlightOn = false;
                _view.SetFlashlightState(false);
            }

            _view.ActualizarCamaras(_model.CurrentPerspective);
        }
    }

    //private Vector3 CalcularMovimientoHorizontal()
    //{
    //    Vector2 input = _inputService.MoveInput;
    //    Vector3 direccion = new Vector3(input.x, 0f, input.y).normalized;

    //    if (direccion.magnitude >= 0.1f)
    //    {
    //        float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg + _view.CameraTransform.eulerAngles.y;

    //        float anguloSuave = Mathf.LerpAngle(_view.transform.eulerAngles.y, anguloObjetivo, Time.deltaTime * _model.RotationSpeed);
    //        _view.transform.rotation = Quaternion.Euler(0f, anguloSuave, 0f);

    //        Vector3 direccionMovimiento = Quaternion.Euler(0f, anguloObjetivo, 0f) * Vector3.forward;
    //        return direccionMovimiento.normalized * _model.MoveSpeed;
    //    }

    //    return Vector3.zero;
    //}

    private Vector3 CalcularMovimientoHorizontal()
    {
        Vector2 input = _inputService.MoveInput;
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        if (_model.CurrentPerspective == CameraPerspective.FirstPerson)
        {
            // --- LÓGICA EN 1ª PERSONA ---
            // El cuerpo del jugador siempre se alinea con el eje Y de la cámara
            Vector3 targetRotation = new Vector3(0f, _view.CameraTransform.eulerAngles.y, 0f);
            _view.transform.rotation = Quaternion.Euler(targetRotation);

            if (direction.magnitude >= 0.1f)
            {
                // El movimiento es relativo a la orientación del jugador/cámara
                Vector3 moveDirection = _view.transform.right * input.x + _view.transform.forward * input.y;
                return moveDirection.normalized * _model.MoveSpeed;
            }
        }
        else
        {
            // --- LÓGICA EN 3ª PERSONA (La que ya teníamos) ---
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _view.CameraTransform.eulerAngles.y;
                float angle = Mathf.LerpAngle(_view.transform.eulerAngles.y, targetAngle, Time.deltaTime * _model.RotationSpeed);
                _view.transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                return moveDirection.normalized * _model.MoveSpeed;
            }
        }

        return Vector3.zero;
    }

    private void CalcularMovimientoVertical()
    {
        if (_inputService.JumpPressed && _model.IsGrounded)
        {
            _model.Velocity.y = Mathf.Sqrt(_model.JumpHeight * -2f * _model.Gravity);

            // Disparar la animación de salto cuando presiona el botón
            _view.DispararGatilloAnimacion(_model.JumpHash);
        }

        _model.Velocity.y += _model.Gravity * Time.deltaTime;
    }

    private void ActualizarAnimaciones(float velocidadMovimiento)
    {
        // Enviamos los datos actualizados a la View
        _view.ActualizarVelocidadAnimacion(_model.SpeedHash, velocidadMovimiento);
        _view.ActualizarEstadoSueloAnimacion(_model.IsGroundedHash, _model.IsGrounded);
    }

    private void ProcesarLinterna()
    {
        // Solo permitir accionar si estamos en Primera Persona
        if (_model.CurrentPerspective == CameraPerspective.FirstPerson)
        {
            if (_inputService.FlashlightPressed)
            {
                _model.IsFlashlightOn = !_model.IsFlashlightOn;
                _view.SetFlashlightState(_model.IsFlashlightOn);
            }
        }
    }

    private void ProcesarHeadBobbing(float velocidadNormalizada)
    {
        // Solo aplica en 1ª persona, cuando toca el suelo y la velocidad es relevante
        if (_model.CurrentPerspective == CameraPerspective.FirstPerson && _model.IsGrounded && velocidadNormalizada > 0.1f)
        {
            // Puedes escalar el bamboleo según la velocidad si en el futuro agregas sprint/correr
            _view.SetHeadBobbingAmplitude(_model.BobbingAmplitude * velocidadNormalizada);
        }
        else
        {
            _view.SetHeadBobbingAmplitude(0f);
        }
    }

    private void ProcesarAccionClic()
    {
        if (_model.CurrentPerspective != CameraPerspective.FirstPerson || !_inputService.ActionPressed)
            return;

        if (_view.PerformFirstPersonRaycast(out RaycastHit hit))
        {
            // 1. Disparamos el feedback visual/sonoro en el punto del impacto
            _view.PlayActionFeedback(hit.point);

            // 2. Le pasamos la posición del jugador al objeto tocado
            if (hit.collider.TryGetComponent<Gameplay.ITouchable>(out var touchable))
            {
                touchable.Touch(_view.transform.position);
            }
        }
    }
}