using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    // Plan construido una vez por sesion. Los beats tienen prioridad sobre ataques cercanos.
    public sealed class MusicalSpawnSchedule
    {
        private readonly double[] _times;
        private int _next;
        private readonly double _leadTime;
        public IReadOnlyList<double> Times { get; }
        public MusicalActivityProfile ActivityProfile { get; }
        private readonly RhythmDifficultyProfile _difficulty;
        private readonly MusicalSpawnEvent[] _events;
        public IReadOnlyList<MusicalSpawnEvent> Events { get; }
        public int MaximumActiveTargets => _difficulty == null ? int.MaxValue
            : _difficulty.MaximumActiveTargets + (ActivityProfile == null ? 0 : ActivityProfile.ExtraTargets);

        public int MaximumTargetsAt(double time) => _difficulty == null ? int.MaxValue
            : _difficulty.MaximumActiveTargets + (ActivityProfile == null ? 0
                : (int)Math.Floor(ActivityProfile.ExtraTargets * ActivityProfile.FrenzyAt(time) + 1e-9d));

        public MusicalSpawnSchedule(MusicalMapData map, double minimumInterval, double leadTime = 0d,
            MusicalActivitySettings activitySettings = null, RhythmPatternSettings patternSettings = null,
            double lateHitWindow = 0d, RhythmPlacementSettings placementSettings = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (double.IsNaN(minimumInterval) || double.IsInfinity(minimumInterval) || minimumInterval <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumInterval));
            if (double.IsNaN(leadTime) || double.IsInfinity(leadTime) || leadTime < 0)
                throw new ArgumentOutOfRangeException(nameof(leadTime));
            _leadTime = leadTime;
            MusicalMapLoader.Validate(map);
            if (patternSettings != null && patternSettings.Enabled)
            {
                RhythmPatternPlanner.ValidateSettings(patternSettings);
                if (double.IsNaN(lateHitWindow) || double.IsInfinity(lateHitWindow) || lateHitWindow < 0d)
                    throw new ArgumentOutOfRangeException(nameof(lateHitWindow));
                _difficulty = RhythmDifficultyProfile.For(patternSettings.Difficulty);
            }
            if (activitySettings != null && activitySettings.Enabled)
                ActivityProfile = new MusicalActivityProfile(map, minimumInterval, activitySettings);
            var selected = new List<double>();
            foreach (double beat in map.Events.Beats)
                if ((ActivityProfile == null || ActivityProfile.IsActive(beat))
                    && (selected.Count == 0 || beat - selected[selected.Count - 1]
                        >= RequiredGap(beat, selected[selected.Count - 1], minimumInterval)))
                    selected.Add(beat);

            foreach (double attack in map.Events.Attacks)
            {
                if (ActivityProfile != null && !ActivityProfile.IsActive(attack)) continue;
                int index = selected.BinarySearch(attack);
                if (index >= 0) continue;
                index = ~index;
                if (index > 0 && attack - selected[index - 1] < RequiredGap(attack, selected[index - 1], minimumInterval)) continue;
                if (index < selected.Count && selected[index] - attack < RequiredGap(attack, selected[index], minimumInterval)) continue;
                selected.Insert(index, attack);
            }
            if (_difficulty != null)
            {
                // Limite conservador: reservar cada objetivo hasta su llegada al jugador.
                var deadlines = new Queue<double>();
                var limited = new List<double>();
                foreach (double hitTime in selected)
                {
                    double spawnTime = ActivityProfile == null ? hitTime - leadTime : ActivityProfile.SpawnTimeFor(hitTime, leadTime);
                    while (deadlines.Count > 0 && deadlines.Peek() < spawnTime - 0.000000001d) deadlines.Dequeue();
                    if (deadlines.Count >= MaximumTargetsAt(hitTime)) continue;
                    limited.Add(hitTime);
                    deadlines.Enqueue(hitTime + lateHitWindow);
                }
                selected = limited;
            }
            if (_difficulty != null) _events = RhythmPatternPlanner.Build(selected, ActivityProfile, patternSettings);
            else
            {
                _events = new MusicalSpawnEvent[selected.Count];
                for (int i = 0; i < selected.Count; i++) _events[i] = new MusicalSpawnEvent(selected[i]);
            }
            if (ActivityProfile != null)
                for (int i = 0; i < _events.Length; i++)
                    _events[i] = new MusicalSpawnEvent(_events[i].HitTime, _events[i].AngleDegrees, _events[i].Pattern,
                        ActivityProfile.SpawnTimeFor(_events[i].HitTime, leadTime));
            if (placementSettings != null && placementSettings.Enabled)
                _events = RhythmPlacementPlanner.Build(_events, leadTime, lateHitWindow, placementSettings, _difficulty, ActivityProfile);
            _times = new double[_events.Length];
            for (int i = 0; i < _events.Length; i++) _times[i] = _events[i].HitTime;
            Times = Array.AsReadOnly(_times);
            Events = Array.AsReadOnly(_events);
        }

        public void Reset() => _next = 0;

        private double RequiredGap(double a, double b, double minimumInterval)
        {
            double gap = ActivityProfile == null ? minimumInterval
                : Math.Max(ActivityProfile.RequiredIntervalAt(a), ActivityProfile.RequiredIntervalAt(b));
            double density = ActivityProfile == null ? 1d
                : Math.Max(ActivityProfile.DensityMultiplierAt(a), ActivityProfile.DensityMultiplierAt(b));
            double floor = minimumInterval * density;
            return Math.Max(floor, gap * density * (_difficulty == null ? 1d : _difficulty.IntervalMultiplier));
        }

        public bool TryTakeDue(double songTime, out double eventTime)
        {
            bool found = TryTakeDueEvent(songTime, out MusicalSpawnEvent item);
            eventTime = found ? item.HitTime : 0d;
            return found;
        }

        public bool TryTakeDueEvent(double songTime, out MusicalSpawnEvent item)
        {
            item = null;
            if (double.IsNaN(songTime) || double.IsInfinity(songTime) || songTime < -_leadTime) return false;
            bool found = false;
            // Igual que el modo anterior: como maximo un spawn por frame, sin rafaga de recuperacion.
            // Los eventos son momentos de acierto; sus apariciones se adelantan al recorrido.
            while (_next < _times.Length && (_events[_next].SpawnTime ?? (_times[_next] - _leadTime)) <= songTime)
            {
                item = _events[_next++];
                found = true;
            }
            return found;
        }
    }
}
