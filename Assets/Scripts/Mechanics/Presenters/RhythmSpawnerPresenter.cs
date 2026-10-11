using System;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEngine;

public class RhythmSpawnerPresenter
{
    private readonly AudioConductorService _conductor;
    private readonly RhythmGameModel _model;
    private readonly TargetFactory _targetFactory;

    private int _lastProcessedBeat = -1;
    private MusicalSpawnSchedule _schedule;
    private double _leadTime;
    private CreatureSpawnSchedule _creatureSchedule;
    private CreatureShotBudget _creatureBudget;
    private readonly RhythmGameConfig _config;
    private readonly Transform _player;
    public CreatureSpawnSchedule CreatureSchedule => _creatureSchedule;

    public RhythmSpawnerPresenter(AudioConductorService conductor, RhythmGameModel model, TargetFactory targetFactory,
        RhythmGameConfig config, Transform player)
    {
        _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _targetFactory = targetFactory ?? throw new ArgumentNullException(nameof(targetFactory));
        _config = config;
        _player = player;
    }

    // Llamar al iniciar/reiniciar, despues de que el reloj vuelva a tiempo cero.
    public void ResetBeatTracking(MusicalMapData map = null, double minimumInterval = 0.3d,
        double leadTime = 0d, MusicalActivitySettings activitySettings = null,
        RhythmPatternSettings patternSettings = null, double lateHitWindow = 0d,
        RhythmPlacementSettings placementSettings = null, BeatCadenceSettings cadenceSettings = null,
        ManualIntensitySettings manualSettings = null)
    {
        _leadTime = leadTime;
        _schedule = map == null ? null : new MusicalSpawnSchedule(map, minimumInterval, leadTime, activitySettings,
            patternSettings, lateHitWindow, placementSettings, cadenceSettings, manualSettings);
        _creatureSchedule = null;
        _creatureBudget = null;
        if (map != null && _config.Creatures != null && _config.Creatures.Enabled)
        {
            var creature = _config.Creatures.Prefab == null ? null : _config.Creatures.Prefab.GetComponent<CreatureEmergenceView>();
            if (creature == null) throw new InvalidOperationException("Asignar el prefab de criatura.");
            _creatureSchedule = new CreatureSpawnSchedule(map, manualSettings, _config.Creatures, creature.EmergenceSeconds);
            double conservativeLead = (_config.Creatures.Distance + 2d) / _config.TravelSpeed;
            _creatureBudget = new CreatureShotBudget(_creatureSchedule, _schedule, conservativeLead);
        }
        // Un desfase negativo puede situar beats antes del inicio del clip.
        // Esos beats se omiten; un beat exactamente en t=0 si puede aparecer.
        _lastProcessedBeat = Math.Max(-1, (int)Math.Ceiling(_conductor.SongPositionInBeats) - 1);
    }

    public void CheckAndSpawnTargets()
    {
        if (!_model.IsPlaying || _model.IsPaused || !_conductor.IsPlaying || _conductor.IsPaused || !_conductor.HasTimelineStarted)
            return;

        CheckCreatures();
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

    private void CheckCreatures()
    {
        if (_creatureSchedule == null || !_creatureSchedule.TryTakeDue(_conductor.SongTime, out var item)) return;
        float angle = item.Angle * Mathf.Deg2Rad;
        Vector3 origin = _player.position + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * _config.Creatures.Distance;
        // The cylinder's default CapsuleCollider does not match its flat rendered surface.
        // Anchor to the visible arena mesh rather than the capsule's rounded volume.
        var floor = GameObject.Find("Floor");
        var renderer = floor == null ? null : floor.GetComponent<Renderer>();
        if (renderer == null) return;
        var bounds = renderer.bounds;
        double x = (origin.x - bounds.center.x) / bounds.extents.x;
        double z = (origin.z - bounds.center.z) / bounds.extents.z;
        if (x * x + z * z > 1d) return;
        var view = UnityEngine.Object.Instantiate(_config.Creatures.Prefab).GetComponent<CreatureEmergenceView>();
        view.Initialize(new Vector3(origin.x, bounds.max.y + 0.01f, origin.z), _player, _conductor);
        view.ConfigureMusicalAttack(item.Time, item.Shots, (mouth, index, time) => {
            if (_creatureBudget.Allows(item.Shots[index])
                && _targetFactory.ActiveTargetCount < _schedule.MaximumTargetsAt(time)
                && _targetFactory.HasClearCreatureRoute(mouth))
                _targetFactory.CreateCreatureWord(mouth, index, time, _schedule.ActivityProfile);
        }, _model);
        _targetFactory.RegisterCreature(view);
        view.PlayAppearanceSound(_config.Creatures.AppearanceClip, _config.Creatures.AppearanceVolume);
    }
}
