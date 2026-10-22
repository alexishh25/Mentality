using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public sealed class CreatureSpawnEvent
    {
        public double Time { get; }
        public double[] Shots { get; }
        public int Section { get; }
        public float Angle { get; }
        public CreatureSpawnEvent(double time, double[] shots, int section, float angle)
        { Time = time; Shots = shots; Section = section; Angle = angle; }
    }

    public sealed class CreatureSpawnSchedule
    {
        private readonly List<CreatureSpawnEvent> _events = new List<CreatureSpawnEvent>();
        private int _next;
        public IReadOnlyList<CreatureSpawnEvent> Events => _events;
        private CreatureSpawnSchedule(List<CreatureSpawnEvent> events) { _events.AddRange(events); }
        public CreatureSpawnSchedule WithAdmittedShots(CreatureShotBudget budget, double emergenceSeconds)
        {
            if (budget == null || emergenceSeconds <= 0d || double.IsNaN(emergenceSeconds) || double.IsInfinity(emergenceSeconds))
                throw new ArgumentException("A valid shot budget and emergence duration are required.");
            var events = new List<CreatureSpawnEvent>();
            foreach (var item in _events)
            {
                var shots = item.Shots.Where(budget.Allows).ToArray();
                // Do not show a creature whose whole volley is unavailable, or let it
                // stand waiting because the budget rejected the opening shot.
                if (shots.Length > 0) events.Add(new CreatureSpawnEvent(shots[0] - emergenceSeconds, shots, item.Section, item.Angle));
            }
            events.Sort((a, b) => a.Time.CompareTo(b.Time));
            return new CreatureSpawnSchedule(events);
        }
        public CreatureSpawnSchedule(MusicalMapData map, ManualIntensitySettings manual, CreatureSettings settings, double emergenceSeconds)
        {
            if (settings == null || !settings.Enabled) return;
            if (manual == null || !manual.Enabled || manual.Sections == null || manual.Sections.Length != 3)
                throw new ArgumentException("Las criaturas necesitan las tres secciones musicales configuradas.");
            if (settings.FirstEveryBeats < 1 || settings.BridgeEveryBeats < 1 || settings.SecondEveryBeats < 1
                || settings.FirstBurst < 1 || settings.FirstBurst > 8 || settings.BridgeBurst < 1 || settings.BridgeBurst > 8
                || settings.SecondBurst < 1 || settings.SecondBurst > 8 || double.IsNaN(emergenceSeconds) || emergenceSeconds <= 0)
                throw new ArgumentException("Cadencia de criaturas invalida.");
            var beats = map.Events.Beats;
            int[] cadence = { settings.FirstEveryBeats, settings.BridgeEveryBeats, settings.SecondEveryBeats };
            int[] burst = { settings.FirstBurst, settings.BridgeBurst, settings.SecondBurst };
            for (int section = 0; section < 3; section++)
            {
                var range = manual.Sections[section];
                int first = Array.BinarySearch(beats, range.Start);
                if (first < 0) first = ~first;
                for (int i = first; i < beats.Length && beats[i] < range.End; i += cadence[section])
                {
                    int attack = Array.BinarySearch(beats, beats[i] + emergenceSeconds + (settings.ContinuousAttack ? 0d : 0.35d));
                    if (attack < 0) attack = ~attack;
                    if (attack + (burst[section] + 1) / 2 >= beats.Length) continue;
                    var shots = new double[burst[section]];
                    for (int j = 0; j < shots.Length; j++)
                    {
                        int beat = attack + j / 2;
                        shots[j] = j % 2 == 0 ? beats[beat] : (beats[beat] + beats[beat + 1]) / 2d;
                    }
                    // Complete the burst within its own section; no queued attack leaks into the ending.
                    if (shots[shots.Length - 1] >= range.End) continue;
                    int slot = (_events.Count * 3) % 5;
                    float angle = (slot / 2f - 1f) * settings.MaximumAngle;
                    // Keep the first shot on its beat and finish emerging at that exact time.
                    double appearance = settings.ContinuousAttack ? shots[0] - emergenceSeconds : beats[i];
                    _events.Add(new CreatureSpawnEvent(appearance, shots, section, angle));
                }
            }
        }

        public bool TryTakeDue(double songTime, out CreatureSpawnEvent item)
        {
            item = null;
            // Discard stale appearances following a long frame, without releasing a backlog.
            while (_next < _events.Count && songTime - _events[_next].Time > 0.12d) _next++;
            if (_next >= _events.Count || _events[_next].Time > songTime) return false;
            item = _events[_next++];
            return true;
        }
    }
}
