// PlayerInputService.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

public class PlayerInputService : IPlayerInputService, IInitializable, IDisposable
{
    private readonly InputSystem_Actions _inputActions;

    public Vector2 MoveInput => _inputActions.Player.Move.ReadValue<Vector2>();
    public bool JumpPressed => _inputActions.Player.Jump.WasPressedThisFrame();
    public bool IsRightClickPressed => Mouse.current != null && Mouse.current.rightButton.isPressed;
    public bool FlashlightPressed => _inputActions.Player.Flashlight.WasPressedThisFrame();
    public bool SwitchCameraPressed => _inputActions.Player.SwitchCamera.WasPressedThisFrame();

    public PlayerInputService()
    {
        _inputActions = new InputSystem_Actions();
    }

    public void Initialize() => EnableInput();
    public void Dispose() => DisableInput();

    public void EnableInput() => _inputActions.Player.Enable();
    public void DisableInput() => _inputActions.Player.Disable();
}