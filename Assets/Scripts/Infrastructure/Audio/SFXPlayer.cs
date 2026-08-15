using UnityEngine;

namespace Infrastructure.Audio
{
    public class SFXPlayer : ISFXPlayer
    {
        private readonly AudioSource _audioSource;
        private readonly AudioConfigSO _config;

        public SFXPlayer(AudioRoot audioRoot, AudioConfigSO config)
        {
            _audioSource = audioRoot.SfxSource;
            _config = config;

            if (_audioSource != null && _config != null && _config.SFXGroup != null)
            {
                _audioSource.outputAudioMixerGroup = _config.SFXGroup;
            }
        }


        public void Play(SFXTrackSO track, float volumeMultiplier = 1f, float pitch = 1f)
        {
            if (track == null) return;
            if (track.Sfx == null || _audioSource == null) return;

            if (!_audioSource.gameObject.activeInHierarchy)
            {
                _audioSource.gameObject.SetActive(true);
            }
            if (!_audioSource.enabled)
            {
                _audioSource.enabled = true;
            }

            float finalVolume = volumeMultiplier * (_config != null ? _config.SFXVolume * _config.MasterVolume : 1f);
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(track.Sfx, finalVolume);
        }
    }
}