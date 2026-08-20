using UnityEngine;

namespace Composition.Interfaces
{
    public interface IAudioService
    {
        void PlayMusic(MusicTrackSO track, float volumeMultiplier = 1f, float pitch = 1f, bool loop = true);
        void PlaySFX(SFXTrackSO track, float volumeMultiplier = 1f, float pitch = 1f);
        void StopMusic();
    }
}