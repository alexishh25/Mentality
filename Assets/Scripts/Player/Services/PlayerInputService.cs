// PlayerInputService.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

public class PlayerInputService : IPlayerInputService, IInitializable, IDisposable
{
    private readonly InputSystem_Actions _inputActions;
    private readonly InputAction _pauseAction;

    public Vector2 MoveInput => _inputActions.Player.Move.ReadValue<Vector2>();
    public bool JumpPressed => _inputActions.Player.Jump.WasPressedThisFrame();
    public bool IsRightClickPressed => Mouse.current != null && Mouse.current.rightButton.isPressed;
    public bool FlashlightPressed => _inputActions.Player.Flashlight.WasPressedThisFrame();
    public bool SwitchCameraPressed => _inputActions.Player.SwitchCamera.WasPressedThisFrame();
    public bool PausePressed => _pauseAction != null && _pauseAction.WasPressedThisFrame();

    public PlayerInputService()
    {
        _inputActions = new InputSystem_Actions();
        // Acción aparte del mapa Player para poder pausar/reanudar con el gameplay deshabilitado.
        _pauseAction = new InputAction("Pause", InputActionType.Button);
        _pauseAction.AddBinding("<Keyboard>/escape");
        _pauseAction.AddBinding("<Gamepad>/start");
    }

    public void Initialize()
    {
        _pauseAction.Enable();
        EnableInput();
    }

    public void Dispose()
    {
        DisableInput();
        _pauseAction.Disable();
        _pauseAction.Dispose();
        _inputActions.Dispose();
    }

    public void EnableInput() => _inputActions.Player.Enable();
    public void DisableInput() => _inputActions.Player.Disable();
}