// IPlayerInputService.cs
using UnityEngine;

public interface IPlayerInputService
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool SwitchCameraPressed { get; }
    bool IsRightClickPressed { get; }
    bool FlashlightPressed { get; }
    /// <summary>Escape / Start — permanece activo aunque se llame a DisableInput().</summary>
    bool PausePressed { get; }
    void EnableInput();
    void DisableInput();
}