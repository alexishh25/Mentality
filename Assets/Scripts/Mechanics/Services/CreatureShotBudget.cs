using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    // Reserve the original chart throughout each proposed shot's complete flight.
    // A rejected shot is discarded, never delayed and never replaces a chart event.
    public sealed class CreatureShotBudget
    {
        private readonly Dictionary<double, double> _accepted = new Dictionary<double, double>();
        public int AcceptedCount => _accepted.Count;
        public bool Allows(double time) => _accepted.ContainsKey(time);
        public double EndAt(double time) => _accepted[time];
        private sealed class Flight { public double Start, End; }

        public CreatureShotBudget(CreatureSpawnSchedule creatures, MusicalSpawnSchedule chart, double baseLead)
        {
            var candidates = new List<double>();
            foreach (var creature in creatures.Events) candidates.AddRange(creature.Shots);
            candidates.Sort();
            var accepted = new List<Flight>();
            foreach (double time in candidates)
            {
                if (_accepted.ContainsKey(time)) continue;
                double low = time, high = time + baseLead;
                double destination = chart.ActivityProfile.MotionTimeAt(time) + baseLead;
                for (int i = 0; i < 48; i++)
                {
                    double midpoint = (low + high) * 0.5d;
                    if (chart.ActivityProfile.MotionTimeAt(midpoint) < destination) low = midpoint; else high = midpoint;
                }
                // Room for ordinary frame latency; the real launch is within 120 ms of its beat.
                double end = (low + high) * 0.5d + 0.12d;
                var samples = new List<double> { time, end };
                // Capacity changes at section boundaries, even if no chart word launches there.
                foreach (var boundary in chart.ActivityProfile.ManualIntensity.SectionBoundaries)
                    if (boundary > time && boundary < end) samples.Add(boundary);
                foreach (var item in chart.Events)
                    if (item.SpawnTime.Value > time && item.SpawnTime.Value < end) samples.Add(item.SpawnTime.Value);
                bool fits = true;
                foreach (double sample in samples)
                {
                    int chartCount = 0, creatureCount = 1;
                    foreach (var item in chart.Events)
                        if (item.SpawnTime.Value <= sample + 1e-9d && item.HitTime >= sample - 1e-9d) chartCount++;
                    foreach (var flight in accepted)
                        if (flight.Start <= sample && flight.End >= sample) creatureCount++;
                    int capacity = chart.MaximumTargetsAt(sample);
                    foreach (var item in chart.Events)
                        if (item.SpawnTime.Value <= sample + 1e-9d && item.HitTime >= sample - 1e-9d)
                            capacity = Math.Min(capacity, chart.MaximumTargetsAt(item.HitTime));
                    int creatureLimit = capacity >= 7 ? 3 : 2;
                    if (creatureCount > creatureLimit || creatureCount + Math.Max(2, chartCount) > capacity)
                    { fits = false; break; }
                }
                if (!fits) continue;
                accepted.Add(new Flight { Start = time, End = end });
                _accepted.Add(time, end);
            }
        }
    }
}
