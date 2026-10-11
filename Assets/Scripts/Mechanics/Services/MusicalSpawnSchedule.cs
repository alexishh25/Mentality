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
        public MusicalPhraseProfile PhraseProfile { get; }
        private readonly RhythmDifficultyProfile _difficulty;
        private readonly MusicalSpawnEvent[] _events;
        public IReadOnlyList<MusicalSpawnEvent> Events { get; }
        public int MaximumActiveTargets => _difficulty == null ? int.MaxValue
            : Math.Max(_difficulty.MaximumActiveTargets + (ActivityProfile == null ? 0 : ActivityProfile.ExtraTargets),
                ActivityProfile?.ManualIntensity?.MaximumTargets ?? 0);

        public int MaximumTargetsAt(double time) => _difficulty == null ? int.MaxValue
            : ActivityProfile?.ManualIntensity?.SectionAt(time)?.MaximumTargets ?? (_difficulty.MaximumActiveTargets + (ActivityProfile == null ? 0
                : (int)Math.Floor(ActivityProfile.ExtraTargets * ActivityProfile.FrenzyAt(time) + 1e-9d)));

        public MusicalSpawnSchedule(MusicalMapData map, double minimumInterval, double leadTime = 0d,
            MusicalActivitySettings activitySettings = null, RhythmPatternSettings patternSettings = null,
            double lateHitWindow = 0d, RhythmPlacementSettings placementSettings = null,
            BeatCadenceSettings cadenceSettings = null, ManualIntensitySettings manualSettings = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (double.IsNaN(minimumInterval) || double.IsInfinity(minimumInterval) || minimumInterval <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumInterval));
            if (double.IsNaN(leadTime) || double.IsInfinity(leadTime) || leadTime < 0)
                throw new ArgumentOutOfRangeException(nameof(leadTime));
            _leadTime = leadTime;
            bool beatCadence = cadenceSettings != null && cadenceSettings.Enabled;
            if (beatCadence && (cadenceSettings.BeatsPerTarget < 1 || cadenceSettings.BeatsPerTarget > 4))
                throw new ArgumentOutOfRangeException(nameof(cadenceSettings), "La cadencia necesita entre 1 y 4 beats por objetivo.");
            if (beatCadence && cadenceSettings.AttackVariationsEnabled
                && (cadenceSettings.AttackEveryBeats < 4 || cadenceSettings.AttackEveryBeats > 16
                    || float.IsNaN(cadenceSettings.MinimumAttackIntensity) || float.IsInfinity(cadenceSettings.MinimumAttackIntensity)
                    || cadenceSettings.MinimumAttackIntensity < 0f || cadenceSettings.MinimumAttackIntensity > 1f))
                throw new ArgumentOutOfRangeException(nameof(cadenceSettings), "Las variaciones necesitan una frase e intensidad validas.");
            MusicalMapLoader.Validate(map);
            if (patternSettings != null && patternSettings.Enabled)
            {
                RhythmPatternPlanner.ValidateSettings(patternSettings);
                if (double.IsNaN(lateHitWindow) || double.IsInfinity(lateHitWindow) || lateHitWindow < 0d)
                    throw new ArgumentOutOfRangeException(nameof(lateHitWindow));
                _difficulty = RhythmDifficultyProfile.For(patternSettings.Difficulty);
            }
            if (activitySettings != null && activitySettings.Enabled)
                ActivityProfile = new MusicalActivityProfile(map, minimumInterval, activitySettings, manualSettings);
            if (beatCadence && cadenceSettings.MusicalPhrasesEnabled && ActivityProfile != null)
                PhraseProfile = new MusicalPhraseProfile(map, ActivityProfile, cadenceSettings);
            var selected = new List<double>();
            int cadenceStep = 0;
            double previousActiveBeat = double.NegativeInfinity;
            foreach (double beat in map.Events.Beats)
            {
                if (ActivityProfile != null && !ActivityProfile.IsActive(beat))
                {
                    cadenceStep = 0;
                    previousActiveBeat = double.NegativeInfinity;
                    continue;
                }
                if (beatCadence)
                {
                    // Una interrupcion real reinicia la frase, sin inventar beats dentro del silencio.
                    if (beat - previousActiveBeat > 2d) cadenceStep = 0;
                    previousActiveBeat = beat;
                    if (cadenceStep++ % cadenceSettings.BeatsPerTarget != 0) continue;
                }
                if (selected.Count == 0 || beat - selected[selected.Count - 1]
                    >= (beatCadence ? minimumInterval : RequiredGap(beat, selected[selected.Count - 1], minimumInterval)))
                    selected.Add(beat);
            }

            if (!beatCadence) foreach (double attack in map.Events.Attacks)
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
            if (_difficulty != null) _events = RhythmPatternPlanner.Build(selected, ActivityProfile, patternSettings, PhraseProfile);
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
            if (PhraseProfile != null)
            {
                var pressureEvents = new List<MusicalSpawnEvent>(_events);
                for (int i = 0; i < map.Events.Beats.Length; i++)
                    if (i % cadenceSettings.PressureExtraBeatEvery == 1 && ActivityProfile.IsActive(map.Events.Beats[i])
                        && PhraseProfile.At(map.Events.Beats[i]) == MusicalPhrasePressure.Pressure)
                        TryAddVariation(pressureEvents, map.Events.Beats[i], minimumInterval, leadTime, lateHitWindow, placementSettings);
                _events = pressureEvents.ToArray();
            }
            if (beatCadence && cadenceSettings.AttackVariationsEnabled)
                _events = AddAttackVariations(map, cadenceSettings, minimumInterval, leadTime, lateHitWindow, placementSettings);
            _times = new double[_events.Length];
            for (int i = 0; i < _events.Length; i++) _times[i] = _events[i].HitTime;
            Times = Array.AsReadOnly(_times);
            Events = Array.AsReadOnly(_events);
        }

        public void Reset() => _next = 0;

        private MusicalSpawnEvent[] AddAttackVariations(MusicalMapData map, BeatCadenceSettings settings,
            double minimumInterval, double leadTime, double lateWindow, RhythmPlacementSettings placement)
        {
            var result = new List<MusicalSpawnEvent>(_events);
            var usedPhrases = new HashSet<int>();
            foreach (double attack in map.Events.Attacks)
            {
                if (PhraseProfile != null && PhraseProfile.At(attack) == MusicalPhrasePressure.Recovery) continue;
                if (ActivityProfile != null && (!ActivityProfile.IsActive(attack)
                    || ActivityProfile.IntensityAt(attack) < settings.MinimumAttackIntensity)) continue;
                int beat = Array.BinarySearch(map.Events.Beats, attack);
                if (beat >= 0) continue;
                int nextBeat = ~beat;
                if (nextBeat == 0 || nextBeat == map.Events.Beats.Length) continue;
                // Evitar duplicar un beat, incluso si ese beat no genera un objetivo base.
                double beatMargin = Math.Min(0.1d, minimumInterval * 0.5d);
                if (attack - map.Events.Beats[nextBeat - 1] < beatMargin
                    || map.Events.Beats[nextBeat] - attack < beatMargin) continue;
                int attackEvery = ActivityProfile?.ManualIntensity?.SectionAt(attack)?.AttackEveryBeats ?? settings.AttackEveryBeats;
                int phrase = (nextBeat - 1) / attackEvery;
                if (usedPhrases.Contains(phrase)) continue;
                if (!TryAddVariation(result, attack, minimumInterval, leadTime, lateWindow, placement)) continue;
                usedPhrases.Add(phrase);
            }
            return result.ToArray();
        }

        private bool TryAddVariation(List<MusicalSpawnEvent> result, double time, double minimumInterval,
            double leadTime, double lateWindow, RhythmPlacementSettings placement)
        {
            int index = result.FindIndex(item => item.HitTime >= time);
            if (index <= 0) return false;
            double previousMinimum = Math.Max(MinimumAt(time,minimumInterval), MinimumAt(result[index-1].HitTime,minimumInterval));
            double nextMinimum = Math.Max(MinimumAt(time,minimumInterval), MinimumAt(result[index].HitTime,minimumInterval));
            if (time-result[index-1].HitTime<previousMinimum || result[index].HitTime-time<nextMinimum) return false;
            double spawnTime = ActivityProfile == null ? time - leadTime : ActivityProfile.SpawnTimeFor(time, leadTime);
            var previous = result[index - 1];
            var trial = new List<MusicalSpawnEvent>(result);
            trial.Insert(index, new MusicalSpawnEvent(time, previous.AngleDegrees, previous.Pattern, spawnTime));
            var deadlines = new Queue<double>();
            foreach (var item in trial)
            {
                double spawn = item.SpawnTime ?? (item.HitTime - leadTime);
                while (deadlines.Count > 0 && deadlines.Peek() < spawn - 1e-9d) deadlines.Dequeue();
                if (deadlines.Count >= MaximumTargetsAt(item.HitTime)) return false;
                deadlines.Enqueue(item.HitTime + lateWindow);
            }
            MusicalSpawnEvent[] placed = placement != null && placement.Enabled
                ? RhythmPlacementPlanner.Build(trial, leadTime, lateWindow, placement, _difficulty, ActivityProfile)
                : trial.ToArray();
            // Conservar todos los eventos existentes; rechazar la variacion si obliga a quitar alguno.
            if (placed.Length != trial.Count) return false;
            result.Clear();
            result.AddRange(placed);
            return true;
        }

        private double RequiredGap(double a, double b, double minimumInterval)
        {
            double gap = ActivityProfile == null ? minimumInterval
                : Math.Max(ActivityProfile.RequiredIntervalAt(a), ActivityProfile.RequiredIntervalAt(b));
            double density = ActivityProfile == null ? 1d
                : Math.Max(ActivityProfile.DensityMultiplierAt(a), ActivityProfile.DensityMultiplierAt(b));
            double floor = minimumInterval * density;
            return Math.Max(floor, gap * density * (_difficulty == null ? 1d : _difficulty.IntervalMultiplier));
        }
        private double MinimumAt(double time,double fallback) => ActivityProfile?.ManualIntensity?.SectionAt(time)?.MinimumInterval ?? fallback;

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
