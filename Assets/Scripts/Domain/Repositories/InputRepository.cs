using UnityEngine;
using UnityEngine.InputSystem;

public class InputRepository : MonoBehaviour
{
    [SerializeField] public InputActionAsset inputActions;
    // [SerializeField] private string initialActionMap = "";

    // Explicit interface implementation to avoid renaming the serialized field
    // InputActionAsset IInputService.inputActions => inputActions;
    public void DisableCurrentMap()
    {
        inputActions.Disable();
    }

    public void SwitchActionMap(string mapName)
    {
        foreach (var map in inputActions.actionMaps)
            map.Disable();

        var target = inputActions.FindActionMap(mapName);

        if (target == null)
        {
            Debug.LogWarning($"InputManager: Action Map '{mapName}' not found.");
            return;
        }

        target.Enable();
        Debug.Log($"InputManager: Switched to Action Map '{mapName}'.");
    }

    // Acceso a acciones individuales para suscribirse desde otros scripts
    public InputAction GetAction(string mapName, string actionName)
    {
        var action = inputActions.FindActionMap(mapName)?.FindAction(actionName);

        if (action == null)
            Debug.LogWarning($"InputManager: Action '{actionName}' not found in map '{mapName}'.");

        return action;
    }
}
