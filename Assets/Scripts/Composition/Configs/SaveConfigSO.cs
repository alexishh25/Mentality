using UnityEngine;

[CreateAssetMenu(fileName = "SaveConfigSO", menuName = "Global Config/SaveConfigSO")]
public class SaveConfigSO : ScriptableObject
{
    [Header("File Settings")]
    [SerializeField] private string defaultSaveFileName = "savegame.json";
    [SerializeField] private string saveFolderName = "Saves";

    [Header("Serialization")]
    [SerializeField] private bool prettyPrint = true;
    [SerializeField] private bool useEncryption = false;
    [SerializeField] private string encryptionKey = "MentalitySecretKey2026";

    public string DefaultSaveFileName => defaultSaveFileName;
    public string SaveFolderName => saveFolderName;
    public bool PrettyPrint => prettyPrint;
    public bool UseEncryption => useEncryption;
    public string EncryptionKey => encryptionKey;
}
