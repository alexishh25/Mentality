using UnityEngine;

[CreateAssetMenu(fileName = "SFXTrackSO", menuName = "Global Config/Audio/SFXTrack")]
public class SFXTrackSO : ScriptableObject
{
    [Header("SFX")]
    [SerializeField] private AudioClip sfx;
    
    public AudioClip Sfx => sfx;
    
}
