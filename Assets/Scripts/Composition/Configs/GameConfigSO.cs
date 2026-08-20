using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigSO", menuName = "Global Config/GameConfigSO")]
public class GameConfigSO : ScriptableObject
{
    [Header("Sub Configurations")]
    [SerializeField] private AudioConfigSO audioConfig;
    [SerializeField] private InputConfigSO inputConfig;
    [SerializeField] private SaveConfigSO saveConfig;

    public AudioConfigSO AudioConfig => audioConfig;
    public InputConfigSO InputConfig => inputConfig;
    public SaveConfigSO SaveConfig => saveConfig;
}
