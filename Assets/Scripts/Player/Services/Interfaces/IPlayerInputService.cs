// IPlayerInputService.cs
using UnityEngine;

public interface IPlayerInputService
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool SwitchCameraPressed { get; }
    bool IsRightClickPressed { get; }
    bool FlashlightPressed { get; }
    bool ActionPressed { get; }
    void EnableInput();
    void DisableInput();
}