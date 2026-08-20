using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputConfigSO", menuName = "Global Config/InputConfigSO")]
public class InputConfigSO : ScriptableObject
{
    [Header("Input Actions Asset")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    [Header("Sensitivity & Look")]
    [Range(0.1f, 10f)]
    [SerializeField] private float mouseSensitivity = 1f;

    [SerializeField] private bool invertY = false;

    [Header("Deadzone")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float analogDeadzone = 0.125f;

    public InputActionAsset InputActionsAsset => inputActionsAsset;
    public float MouseSensitivity => mouseSensitivity;
    public bool InvertY => invertY;
    public float AnalogDeadzone => analogDeadzone;
}
