using UnityEngine;

namespace Composition.Interfaces
{
    public interface IAudioService
    {
        void PlayMusic(AudioClip clip, float start, float end);
        void PlaySFX(AudioClip clip, float volume = 1f);
        void StopMusic();
        void CheckLoop();
    }
}