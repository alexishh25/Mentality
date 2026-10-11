using System;
using VContainer.Unity;

namespace Mentality.Gameplay.MG01
{
    // Adaptador delgado: el sistema de estados global (CUS-001) podra conducirlo mas adelante.
    public sealed class MG01ResultService : IMG01ResultService, ILateTickable, IDisposable
    {
        private readonly ISanityService _sanity;
        private readonly ISongProgressSource _song;
        private readonly IPauseService _pause;

        private int _wordsDestroyed;
        private int _hitsTaken;
        private int _ghostClicks;
        private float _lastSanity;
        private bool _victoryPending;

        public bool HasEnded { get; private set; }
        public MG01Outcome Outcome { get; private set; }
        public MG01RunStats Stats => new MG01RunStats
        {
            wordsDestroyed = _wordsDestroyed,
            hitsTaken = _hitsTaken,
            ghostClicks = _ghostClicks,
            sanityRemaining = _sanity.Current
        };

        public event Action<MG01Outcome, MG01RunStats> RunEnded;

        public MG01ResultService(ISanityService sanity, ISongProgressSource song, IPauseService pause)
        {
            _sanity = sanity ?? throw new ArgumentNullException(nameof(sanity));
            _song = song ?? throw new ArgumentNullException(nameof(song));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));

            _lastSanity = _sanity.Current;
            _sanity.Changed += OnSanityChanged;
            _sanity.Depleted += OnSanityDepleted;
            _song.Finished += OnSongFinished;
        }

        public void ReportWordDestroyed()
        {
            if (!HasEnded) _wordsDestroyed++;
        }

        public void ReportGhostClicked()
        {
            if (!HasEnded) _ghostClicks++;
        }

        // GDD: "si el ultimo impacto coincide con el cierre, primero se resuelve el dano".
        // La victoria se confirma al final del frame, despues de cualquier impacto del mismo frame.
        public void LateTick() => ResolvePendingVictory();

        public void ResolvePendingVictory()
        {
            if (!_victoryPending || HasEnded) return;
            _victoryPending = false;
            EndRun(_sanity.Current > 0f ? MG01Outcome.Victory : MG01Outcome.Defeat);
        }

        private void OnSanityChanged(float current, float max)
        {
            if (!HasEnded && current < _lastSanity) _hitsTaken++;
            _lastSanity = current;
        }

        private void OnSanityDepleted() => EndRun(MG01Outcome.Defeat);

        private void OnSongFinished()
        {
            if (!HasEnded) _victoryPending = true;
        }

        private void EndRun(MG01Outcome outcome)
        {
            if (HasEnded) return;

            HasEnded = true;
            _victoryPending = false;
            Outcome = outcome;
            _pause.Halt();
            RunEnded?.Invoke(outcome, Stats);
        }

        public void Dispose()
        {
            _sanity.Changed -= OnSanityChanged;
            _sanity.Depleted -= OnSanityDepleted;
            _song.Finished -= OnSongFinished;
        }
    }
}
