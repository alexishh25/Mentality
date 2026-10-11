using System;
using UnityEngine;
using VContainer.Unity;

namespace Mentality.Gameplay.MG01
{
    // Sustituto TEMPORAL del conductor ritmico: una cancion de duracion fija que respeta la pausa.
    // Reemplazar el registro en MG01LifetimeScope cuando el conductor implemente ISongProgressSource.
    public sealed class DebugSongProgressSource : ISongProgressSource, ITickable
    {
        private readonly IPauseService _pause;
        private float _elapsed;

        public float Duration { get; }
        public float Normalized => Mathf.Clamp01(_elapsed / Duration);
        public bool IsFinished { get; private set; }

        public event Action Finished;

        public DebugSongProgressSource(MG01ConfigSO config, IPauseService pause)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            Duration = Mathf.Max(0.1f, config.DebugSongDuration);
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (IsFinished || _pause.IsGameplayStopped || deltaTime <= 0f) return;

            _elapsed += deltaTime;
            if (_elapsed >= Duration)
                Finish();
        }

        public void JumpToEnd()
        {
            if (IsFinished) return;
            Finish();
        }

        private void Finish()
        {
            _elapsed = Duration;
            IsFinished = true;
            Finished?.Invoke();
        }
    }
}
