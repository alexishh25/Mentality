// PlayerModel.cs
using UnityEngine;

public enum CameraPerspective { ThirdPerson, FirstPerson }
public enum PlayerGameplayMode { Exploration, Rhythm }

[System.Serializable]
public class PlayerModel
{
    [Header("Gameplay Mode")]
    public PlayerGameplayMode GameplayMode = PlayerGameplayMode.Exploration;

    [Header("Settings")]
    public float MoveSpeed = 5.0f;
    public float RotationSpeed = 10.0f;
    public float Gravity = -9.81f;
    public float JumpHeight = 1.5f;

    [Header("Camera Settings")]
    public CameraPerspective CurrentPerspective = CameraPerspective.ThirdPerson;

    [Header("Flashlight Settings")]
    public bool IsFlashlightOn = false;

    [Header("Head Bobbing Settings")]
    public float BobbingAmplitude = 0.5f;

    // Estado en tiempo de ejecución
    public Vector3 Velocity;
    public bool IsGrounded;

    //Hash de Animacion
    public readonly int SpeedHash = Animator.StringToHash("Speed");
    public readonly int IsGroundedHash = Animator.StringToHash("IsGround");
    public readonly int JumpHash = Animator.StringToHash("Jump");
}
