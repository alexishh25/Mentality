using UnityEngine;

[CreateAssetMenu(fileName = "MusicTrackSO", menuName = "Global Config/Audio/MusicTrack")]
public class MusicTrackSO : ScriptableObject
{
    [Header("Music")]
    [SerializeField] private AudioClip bgm;
    
    [Header("Music Loop")]
    [SerializeField] private float loopStartTime;
    [SerializeField] private float loopEndTime;
    
    public AudioClip Bgm => bgm;
    public float LoopStartTime => loopStartTime;
    public float LoopEndTime => loopEndTime;
    
}
