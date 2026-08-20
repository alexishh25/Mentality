// PlayerModel.cs
using UnityEngine;
using Mentality.Domain.Player;

[System.Serializable]
public class PlayerModel
{
    [Header("Settings")]
    public float MoveSpeed = 5.0f;
    public float RotationSpeed = 10.0f;
    public float Gravity = -9.81f;
    public float JumpHeight = 1.5f;

    [Header("Camera Settings")]
    public CameraPerspective CurrentPerspective = CameraPerspective.ThirdPerson;

    [Header("Head Bobbing Settings")]
    public float BobbingAmplitude = 0.5f;

    // Estado en tiempo de ejecución de física de Unity
    public Vector3 Velocity;

    // Hashes de Animación
    public readonly int SpeedHash = Animator.StringToHash("Speed");
    public readonly int IsGroundedHash = Animator.StringToHash("IsGround");
    public readonly int JumpHash = Animator.StringToHash("Jump");

    private PlayerState _state;

    public PlayerState GetOrCreateState()
    {
        if (_state == null)
        {
            _state = new PlayerState(MoveSpeed, RotationSpeed, Gravity, JumpHeight, BobbingAmplitude, CurrentPerspective);
        }
        return _state;
    }
}