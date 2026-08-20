namespace Mentality.Domain.Player
{
    public enum CameraPerspective
    {
        ThirdPerson,
        FirstPerson
    }

    public class PlayerState
    {
        public CameraPerspective CurrentPerspective { get; private set; } = CameraPerspective.ThirdPerson;
        public bool IsFlashlightOn { get; private set; } = false;

        public float MoveSpeed { get; set; } = 5.0f;
        public float RotationSpeed { get; set; } = 10.0f;
        public float Gravity { get; set; } = -9.81f;
        public float JumpHeight { get; set; } = 1.5f;
        public float BobbingAmplitude { get; set; } = 0.5f;

        // Estado en tiempo de ejecución
        public bool IsGrounded { get; set; }

        public PlayerState() { }

        public PlayerState(float moveSpeed, float rotationSpeed, float gravity, float jumpHeight, float bobbingAmplitude, CameraPerspective initialPerspective)
        {
            MoveSpeed = moveSpeed;
            RotationSpeed = rotationSpeed;
            Gravity = gravity;
            JumpHeight = jumpHeight;
            BobbingAmplitude = bobbingAmplitude;
            CurrentPerspective = initialPerspective;
        }

        /// <summary>
        /// Regla de Dominio: Cambiar perspectiva.
        /// Si pasa a 3ª persona, la linterna se apaga automáticamente.
        /// </summary>
        public void SwitchPerspective()
        {
            CurrentPerspective = CurrentPerspective == CameraPerspective.ThirdPerson
                ? CameraPerspective.FirstPerson
                : CameraPerspective.ThirdPerson;

            if (CurrentPerspective == CameraPerspective.ThirdPerson && IsFlashlightOn)
            {
                IsFlashlightOn = false;
            }
        }

        /// <summary>
        /// Regla de Dominio: Accionar linterna.
        /// Solo está permitido encender la linterna en 1ª persona.
        /// </summary>
        public bool ToggleFlashlight()
        {
            if (CurrentPerspective == CameraPerspective.FirstPerson)
            {
                IsFlashlightOn = !IsFlashlightOn;
            }
            else
            {
                IsFlashlightOn = false;
            }

            return IsFlashlightOn;
        }

        /// <summary>
        /// Regla de Dominio: Calcular velocidad inicial del salto.
        /// </summary>
        public float CalculateJumpVerticalVelocity()
        {
            return (float)System.Math.Sqrt(JumpHeight * -2.0f * Gravity);
        }
    }
}
