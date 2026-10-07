using System;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;

public class RhythmSpawnerPresenter
{
    private readonly AudioConductorService _conductor;
    private readonly RhythmGameModel _model;
    private readonly TargetFactory _targetFactory;

    private int _lastProcessedBeat = -1;
    private MusicalSpawnSchedule _schedule;
    private double _leadTime;

    public RhythmSpawnerPresenter(AudioConductorService conductor, RhythmGameModel model, TargetFactory targetFactory)
    {
        _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
    }

    // Llamar al iniciar/reiniciar, despues de que el reloj vuelva a tiempo cero.
    public void ResetBeatTracking(MusicalMapData map = null, double minimumInterval = 0.3d,
        double leadTime = 0d, MusicalActivitySettings activitySettings = null,
        RhythmPatternSettings patternSettings = null, double lateHitWindow = 0d,
        RhythmPlacementSettings placementSettings = null)
    {
        _leadTime = leadTime;
        _schedule = map == null ? null : new MusicalSpawnSchedule(map, minimumInterval, leadTime, activitySettings,
            patternSettings, lateHitWindow, placementSettings);
        // Un desfase negativo puede situar beats antes del inicio del clip.
        // Esos beats se omiten; un beat exactamente en t=0 si puede aparecer.
        _lastProcessedBeat = Math.Max(-1, (int)Math.Ceiling(_conductor.SongPositionInBeats) - 1);
    }

    public void CheckAndSpawnTargets()
    {
        if (!_model.IsPlaying || _model.IsPaused || !_conductor.IsPlaying || _conductor.IsPaused || !_conductor.HasTimelineStarted)
            return;

        if (_schedule != null)
        {
            if (_schedule.TryTakeDueEvent(_conductor.SongTime, out MusicalSpawnEvent item))
            {
                if (_schedule.MaximumActiveTargets != int.MaxValue
                    && _targetFactory.ActiveTargetCount >= _schedule.MaximumTargetsAt(item.HitTime)) return;
                var timing = new TargetTiming(item.SpawnTime ?? (item.HitTime - _leadTime), item.HitTime, _schedule.ActivityProfile);
                if (item.AngleDegrees.HasValue) _targetFactory.CreateAtAngle(item.AngleDegrees.Value, timing);
                else _targetFactory.CreateRandomPosition(timing);
            }
            return;
        }

        if (!_conductor.HasStarted) return;

        int currentBeat = _conductor.CurrentBeatIndex;
        if (currentBeat < 0 || currentBeat <= _lastProcessedBeat)
            return;

        // Una sola aparicion aunque varios beats hayan pasado entre frames.
        _lastProcessedBeat = currentBeat;
        _targetFactory.CreateRandomPosition();
    }
}
