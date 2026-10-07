using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Rhythm.Services
{
    // Solo controles de la sesion; apuntar y disparar siguen usando el subsistema Player.
    public class RhythmSessionInputService
    {
        public bool RestartPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public bool ToggleCursorPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public bool TogglePausePressed => Keyboard.current != null
            && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame);
        public bool CapturePressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public bool HasFocus => Application.isFocused;
    }
}
