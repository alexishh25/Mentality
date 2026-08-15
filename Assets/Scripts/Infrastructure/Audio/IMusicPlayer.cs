using UnityEngine;

namespace Infrastructure.Audio
{
    public interface IMusicPlayer
    {
        void Play(MusicTrackSO track, float volumeMultiplier = 1f, float pitch = 1f, bool loop = true);
        void DefinirLoop(float start, float end);
        void Stop();
    }
}