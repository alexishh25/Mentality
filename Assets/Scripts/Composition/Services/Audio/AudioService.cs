using Composition.Interfaces;
using Infrastructure.Audio;
using UnityEngine;

namespace Composition.Services
{
    public class AudioService : IAudioService
    {
        private readonly IMusicPlayer _musicPlayer;
        private readonly ISFXPlayer _sfxPlayer;

        public AudioService(IMusicPlayer musicPlayer, ISFXPlayer sfxPlayer)
        {
            _musicPlayer = musicPlayer;
            _sfxPlayer = sfxPlayer;
        }

        public void PlayMusic(MusicTrackSO track, float volumeMultiplier = 1f, float pitch = 1f, bool loop = true)
        {
            _musicPlayer.Play(track, volumeMultiplier, pitch, loop);
        }

        public void PlaySFX(SFXTrackSO track, float volumeMultiplier = 1f, float pitch = 1f)
        {
            _sfxPlayer.Play(track, volumeMultiplier, pitch);
        }

        public void StopMusic()
        {
            _musicPlayer.Stop();
        }
    }
}