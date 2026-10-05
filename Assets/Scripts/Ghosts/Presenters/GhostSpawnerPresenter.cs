using Gameplay.Ghosts.Factories;
using Gameplay.Ghosts.Models;
using Gameplay.Ghosts.Services;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using System;
using VContainer.Unity;

namespace Gameplay.Ghosts.Presenters
{
    public sealed class GhostSpawnerPresenter : IStartable, ITickable, IDisposable
    {
        private readonly GhostFeatureSettings _settings;
        private readonly GhostSpawnPlanner _planner;
        private readonly RhythmGameConfig _rhythmConfig;
        private readonly RhythmGameModel _session;
        private readonly AudioConductorService _conductor;
        private readonly GhostDecoyFactory _factory;
        private readonly IGhostConsequences _consequences;
        private GhostSpawnCue[] _cues = Array.Empty<GhostSpawnCue>();
        private int _nextCue;
        private double _lastSongTime = double.NegativeInfinity;

        public GhostSpawnerPresenter(GhostFeatureSettings settings, GhostSpawnPlanner planner,
            RhythmGameConfig rhythmConfig, RhythmGameModel session, AudioConductorService conductor,
            GhostDecoyFactory factory, IGhostConsequences consequences)
        {
            _settings = settings;
            _planner = planner;
            _rhythmConfig = rhythmConfig;
            _session = session;
            _conductor = conductor;
            _factory = factory;
            _consequences = consequences;
        }

        public void Start()
        {
            MusicalMapData map = _rhythmConfig.AnalyzedSong != null
                ? _rhythmConfig.AnalyzedSong.LoadValidatedMap() : null;
            float duration = _rhythmConfig.SessionMusicClip != null ? _rhythmConfig.SessionMusicClip.length : 0f;
            _cues = _planner.Build(map, _rhythmConfig.Beatmap, duration, _settings);
        }

        public void Tick()
        {
            if (!_session.IsPlaying || _session.IsPaused || !_conductor.IsPlaying
                || _conductor.IsPaused || !_conductor.HasTimelineStarted)
                return;

            double now = _conductor.CurrentSongTime;
            if (now + 0.001d < _lastSongTime)
                ResetForRestart();
            _lastSongTime = now;

            // Spawn at most one decoy per frame, even if a frame hitch crosses several cues.
            if (_nextCue < _cues.Length && _cues[_nextCue].Time <= now)
            {
                _factory.Create();
                _nextCue++;
            }
        }

        private void ResetForRestart()
        {
            _factory.Clear();
            _consequences.ResetForSession();
            _nextCue = 0;
        }

        public void Dispose() => _factory.Clear();
    }
}
