using System;
using System.Collections.Generic;
using Gameplay.Ghosts.Models;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;

namespace Gameplay.Ghosts.Services
{
    public sealed class GhostSpawnPlanner
    {
        public GhostSpawnCue[] Build(MusicalMapData map, SongBeatmap beatmap, float clipDuration,
            GhostFeatureSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var candidates = new List<GhostSpawnCue>();
            var random = new Random(settings.RandomSeed);

            if (map != null)
            {
                MusicalMapLoader.Validate(map);
                AddOffbeats(map.Events.Beats, map.Metadata.DurationSeconds, settings, random, candidates);
                AddSilenceCues(map.ActiveIntervals, map.Metadata.DurationSeconds, settings, candidates);
            }
            else if (beatmap != null && IsFinite(clipDuration) && clipDuration > 0f
                && IsFinite(beatmap.bpm) && beatmap.bpm >= 1f)
            {
                // A BPM-only session has enough information for syncopation, but no silence map.
                double beatLength = 60d / beatmap.bpm;
                var beats = new List<double>();
                for (double beat = beatmap.offsetSeconds; beat < clipDuration; beat += beatLength)
                    if (beat >= 0d) beats.Add(beat);
                AddOffbeats(beats, clipDuration, settings, random, candidates);
            }

            candidates.Sort((left, right) => left.Time.CompareTo(right.Time));
            var selected = new List<GhostSpawnCue>(candidates.Count);
            foreach (GhostSpawnCue candidate in candidates)
            {
                if (candidate.Time < 0d || candidate.Time > clipDuration && clipDuration > 0f)
                    continue;
                if (selected.Count == 0 || candidate.Time - selected[selected.Count - 1].Time
                    >= settings.MinimumSpawnSeparationSeconds)
                    selected.Add(candidate);
            }
            return selected.ToArray();
        }

        private static void AddOffbeats(IReadOnlyList<double> beats, double duration,
            GhostFeatureSettings settings, Random random, List<GhostSpawnCue> output)
        {
            if (beats == null || beats.Count < 2) return;
            for (int i = 0; i + 1 < beats.Count; i++)
            {
                if (random.NextDouble() > settings.OffbeatSpawnChance) continue;
                double midpoint = beats[i] + (beats[i + 1] - beats[i]) * 0.5d;
                if (midpoint >= 0d && midpoint < duration)
                    output.Add(new GhostSpawnCue(midpoint, GhostSpawnCueType.Offbeat));
            }
        }

        private static void AddSilenceCues(MusicalActiveInterval[] activeIntervals, double duration,
            GhostFeatureSettings settings, List<GhostSpawnCue> output)
        {
            if (activeIntervals == null) return;
            double cursor = 0d;
            foreach (MusicalActiveInterval interval in activeIntervals)
            {
                if (interval == null) continue;
                AddSilenceCue(cursor, interval.Start, settings, output);
                cursor = Math.Max(cursor, interval.End);
            }
            AddSilenceCue(cursor, duration, settings, output);
        }

        private static void AddSilenceCue(double start, double end, GhostFeatureSettings settings,
            List<GhostSpawnCue> output)
        {
            if (end - start < settings.MinimumSilenceSeconds) return;
            output.Add(new GhostSpawnCue(start + (end - start) * 0.5d, GhostSpawnCueType.Silence));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
