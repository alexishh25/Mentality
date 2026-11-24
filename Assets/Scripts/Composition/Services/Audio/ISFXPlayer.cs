using UnityEngine;

namespace Infrastructure.Audio
{
    public interface ISFXPlayer
    {
        void Play(SFXTrackSO track, float volumeMultiplier = 1f, float pitch = 1f);
    }
}