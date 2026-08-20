using UnityEngine;

namespace Composition.Interfaces
{
    public interface IInputService
    {
        InputConfigSO Config { get; }
        float MouseSensitivity { get; set; }
        bool InvertY { get; set; }
        float AnalogDeadzone { get; set; }

        Vector2 ProcessLookDelta(Vector2 rawLookDelta);
        Vector2 ProcessMoveInput(Vector2 rawMoveInput);
    }
}