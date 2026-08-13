using Composition.Interfaces;
using UnityEngine;

namespace Composition.Services
{
    public class AudioService : IAudioService
    {
        private readonly AudioConfigSO _config;

        public AudioService(AudioConfigSO config)
        {
            _config = config;
        }
        public void PlayMusic(AudioClip clip, float start, float end)
        {
            
        }

        public void PlaySFX(AudioClip clip, float volume = 1)
        {
            
        }

        public void StopMusic()
        {
            
        }

        public void CheckLoop()
        {
            
        }
    }

}