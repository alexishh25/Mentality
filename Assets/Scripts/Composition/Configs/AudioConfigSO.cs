using UnityEngine;

[CreateAssetMenu(fileName = "AudioConfigSO", menuName = "Global Config/AudioConfigSO")]
public class AudioConfigSO : ScriptableObject
{
    [SerializeField] public AudioSource musicSource;
    [SerializeField] public AudioSource sfxSource;

    public float loop_start_time, loop_end_time;
}
