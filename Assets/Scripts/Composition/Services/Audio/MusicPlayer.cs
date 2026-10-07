using UnityEngine;
using VContainer.Unity;

namespace Infrastructure.Audio
{
    public class MusicPlayer : IMusicPlayer, ITickable
    {
        private readonly AudioSource _audioSource;
        private readonly AudioConfigSO _config;
        private bool _isLooping;
        private float _startLoop;
        private float _endLoop;

        public MusicPlayer(AudioRoot audioRoot, AudioConfigSO config)
        {
            _audioSource = audioRoot.MusicSource;
            _config = config;

            if (_audioSource != null && _config != null && _config.MusicGroup != null)
            {
                _audioSource.outputAudioMixerGroup = _config.MusicGroup;
            }
        }

        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = true, float startLoop = 0f, float endLoop = 0f)
        {
            if (clip == null || _audioSource == null) return;

            if (!_audioSource.gameObject.activeInHierarchy)
            {
                _audioSource.gameObject.SetActive(true);
            }
            if (!_audioSource.enabled)
            {
                _audioSource.enabled = true;
            }

            _audioSource.clip = clip;
            _audioSource.volume = volume * (_config != null ? _config.MusicVolume * _config.MasterVolume : 1f);
            _audioSource.pitch = pitch;
            _audioSource.loop = false;

            _isLooping = loop;
            _startLoop = Mathf.Max(0f, startLoop);
            _endLoop = (endLoop > _startLoop && endLoop <= clip.length) ? endLoop : clip.length;

            _audioSource.Play();
        }

        public void Play(MusicTrackSO track, float volumeMultiplier = 1f, float pitch = 1f, bool loop = true)
        {
            if (track == null) return;
            Play(track.Bgm, volumeMultiplier, pitch, loop, track.LoopStartTime, track.LoopEndTime);
        }

        public void DefinirLoop(float start, float end)
        {
            _startLoop = Mathf.Max(0f, start);
            if (_audioSource != null && _audioSource.clip != null)
            {
                _endLoop = (end > _startLoop && end <= _audioSource.clip.length) ? end : _audioSource.clip.length;
            }
            else
            {
                _endLoop = end;
            }
        }

        public void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
            _isLooping = false;
        }

        public void Tick()
        {
            if (!_isLooping || _audioSource == null || !_audioSource.isPlaying) return;

            if (_endLoop > _startLoop && _audioSource.time >= _endLoop)
            {
                _audioSource.time = _startLoop;
            }
        }
    }
}