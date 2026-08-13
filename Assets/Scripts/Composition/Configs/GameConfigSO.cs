using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigSO", menuName = "Global Config/GameConfigSO")]
public class GameConfigSO : ScriptableObject
{
    public AudioConfigSO audioConfig;
    public InputConfigSO inputConfig;
    public SaveConfigSO saveConfig;
}
