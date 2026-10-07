using Composition.Interfaces;
using UnityEngine;

namespace Composition.Services
{
    public class InputService : IInputService
    {
        private readonly InputConfigSO _config;

        private float? _overrideSensitivity;
        private bool? _overrideInvertY;
        private float? _overrideDeadzone;

        public InputConfigSO Config => _config;

        public float MouseSensitivity
        {
            get => _overrideSensitivity ?? (_config != null ? _config.MouseSensitivity : 1f);
            set => _overrideSensitivity = value;
        }

        public bool InvertY
        {
            get => _overrideInvertY ?? (_config != null ? _config.InvertY : false);
            set => _overrideInvertY = value;
        }

        public float AnalogDeadzone
        {
            get => _overrideDeadzone ?? (_config != null ? _config.AnalogDeadzone : 0.125f);
            set => _overrideDeadzone = value;
        }

        public InputService(InputConfigSO config = null)
        {
            _config = config;
        }

        public Vector2 ProcessLookDelta(Vector2 rawLookDelta)
        {
            float sensitivity = MouseSensitivity;
            float yMultiplier = InvertY ? -1f : 1f;

            return new Vector2(rawLookDelta.x * sensitivity, rawLookDelta.y * sensitivity * yMultiplier);
        }

        public Vector2 ProcessMoveInput(Vector2 rawMoveInput)
        {
            float deadzone = AnalogDeadzone;
            if (rawMoveInput.magnitude < deadzone)
            {
                return Vector2.zero;
            }

            return rawMoveInput;
        }
    }
}