using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Scriptable Objects/Dialogue")]
[System.Serializable]
public class Dialogue : ScriptableObject
{
    bool isPlayerSpeaking;
    string characterName;

    string[] dialogueLines;

    private int currentIndex;
    public string GetTextLine()
    {
        if (dialogueLines.Length == 0)
            return "";
        Debug.Log("Índice actual: " + currentIndex);
        string line = dialogueLines[currentIndex];

        currentIndex++;

        if (currentIndex >= dialogueLines.Length)
            currentIndex = 0;

        return line;

        //view func para presenter inicializar en service
        //model helper etc
        //presenter con unity
    }
}
