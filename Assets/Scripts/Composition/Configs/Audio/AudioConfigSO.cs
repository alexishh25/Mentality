using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfigSO", menuName = "Global Config/AudioConfigSO")]
public class AudioConfigSO : ScriptableObject
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    public AudioMixer Mixer => mixer;
    public AudioMixerGroup MusicGroup => musicGroup;
    public AudioMixerGroup SFXGroup => sfxGroup;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;
}
