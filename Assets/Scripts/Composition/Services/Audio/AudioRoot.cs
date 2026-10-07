using UnityEngine;
using VContainer;

public class AudioRoot : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    public AudioSource MusicSource
    {
        get
        {
            if (musicSource == null)
            {
                var sources = GetComponentsInChildren<AudioSource>(true);
                if (sources.Length > 0) musicSource = sources[0];
            }
            return musicSource;
        }
    }

    public AudioSource SfxSource
    {
        get
        {
            if (sfxSource == null)
            {
                var sources = GetComponentsInChildren<AudioSource>(true);
                if (sources.Length > 1) sfxSource = sources[1];
                else if (sources.Length > 0) sfxSource = sources[0];
            }
            return sfxSource;
        }
    }

    private void Awake()
    {
        // Auto-assign from components if not manually set in Inspector
        var sources = GetComponentsInChildren<AudioSource>(true);
        if (musicSource == null && sources.Length > 0) musicSource = sources[0];
        if (sfxSource == null && sources.Length > 1) sfxSource = sources[1];
    }
}
