using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Views;
using UnityEngine;

namespace Gameplay.Rhythm.Services
{
    // Chooses an emitter; optional baked geometry validation filters unsafe routes.
    public sealed class AudienceWordSelector
    {
        private readonly AudienceWordEmitterRegistry registry;
        private readonly RhythmPlacementSettings placement;
        private readonly float arrivalDistance;
        private readonly RhythmGameConfig config;
        private readonly AudienceWordRouteLayout routes;
        private readonly List<AudienceWordEmitter> available = new List<AudienceWordEmitter>();
        private readonly Dictionary<AudienceWordEmitter, long> lastUsed = new Dictionary<AudienceWordEmitter, long>();
        private long sequence;
        private float previousYaw;
        private double previousSpawn = double.NegativeInfinity;
        private readonly int? randomSeed;
        private System.Random random;
        private AudienceWordEmitter previousMember;
        private string previousRow;
        private readonly Dictionary<string, int> rowUses = new Dictionary<string, int>();
        private readonly Dictionary<string, double> rowWeights = new Dictionary<string, double>();
        private readonly Dictionary<string, int> rowSizes = new Dictionary<string, int>();
        private struct Candidate
        {
            public AudienceWordEmitter Member;
            public string Row;
            public float Yaw, Difference;
            public double Weight;
        }
        private readonly List<Candidate> candidates = new List<Candidate>();
        public bool LastSelectionRedirected { get; private set; }
        public float LastSelectionClearance { get; private set; }
        public string LastSelectionRow { get; private set; }
        public string RouteStatus => routes == null ? "No route filter" : routes.LastRejection;

        public AudienceWordSelector(AudienceWordEmitterRegistry registry, RhythmGameConfig config, AudienceWordRouteLayout routes = null,
            int? randomSeed = null)
        {
            this.registry = registry != null ? registry : throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new ArgumentNullException(nameof(config));
            RhythmPlacementPlanner.ValidateSettings(config.Placement);
            placement = config.Placement;
            arrivalDistance = config.ArrivalDistance;
            this.config = config;
            this.routes = routes;
            this.randomSeed = randomSeed;
            Reset();
        }

        public void Reset()
        {
            lastUsed.Clear(); rowUses.Clear(); sequence = 0; previousSpawn = double.NegativeInfinity;
            previousMember = null; previousRow = null; LastSelectionRow = null;
            LastSelectionRedirected = false; LastSelectionClearance = 0;
            random = new System.Random(randomSeed ?? Guid.NewGuid().GetHashCode());
        }

        public bool TrySelect(float requestedAngle, Vector3 player, float referenceYaw,
            IReadOnlyList<Vector3> activeWordPositions, out AudienceWordEmitter selected, TargetTiming timing = null)
        {
            selected = null;
            LastSelectionRedirected = false;
            LastSelectionClearance = 0f;
            LastSelectionRow = null;
            candidates.Clear();
            if (!float.IsFinite(requestedAngle) || !float.IsFinite(referenceYaw) || !Finite(player))
                throw new ArgumentException("Selection requires finite angles and player position.");
            if (activeWordPositions != null)
                foreach (var position in activeWordPositions)
                    if (!Finite(position)) throw new ArgumentException("Active word positions must be finite.");
            if (Mathf.Abs(requestedAngle) > placement.MaximumSideAngle) return false;
            registry.GetAvailableEmitters(available);
            long oldest = long.MaxValue;
            float bestAngle = float.PositiveInfinity;
            bool bestInRequestedSector = false;
            float selectedYaw = 0f;
            double allowedTurn = double.PositiveInfinity;
            if (timing != null && !double.IsNegativeInfinity(previousSpawn))
            {
                double frenzy = timing.MotionProfile?.FrenzyAt(timing.HitTime) ?? 0d;
                allowedTurn = Math.Min(placement.MaximumTurnDegrees * (1d + (placement.FrenzyTurnAngleMultiplier - 1d) * frenzy),
                    Math.Max(0d, timing.SpawnTime - previousSpawn) * placement.MaximumTurnSpeedDegreesPerSecond *
                    (1d + (placement.FrenzyTurnSpeedMultiplier - 1d) * frenzy));
            }
            foreach (var candidate in available)
            {
                if (!config.WordsPassThroughScenery && routes != null && !routes.IsClear(candidate, player, config)) continue;
                Vector3 direction = candidate.WordOrigin.position - player;
                if (!Finite(direction) || direction.sqrMagnitude <= arrivalDistance * arrivalDistance ||
                    direction.x * direction.x + direction.z * direction.z < 0.0001f) continue;
                float yaw = Mathf.DeltaAngle(referenceYaw, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
                float difference = Mathf.Abs(Mathf.DeltaAngle(requestedAngle, yaw));
                if (Mathf.Abs(yaw) > placement.MaximumSideAngle ||
                    (timing == null && difference > placement.LaneSpacingDegrees * 0.5f) ||
                    Mathf.Abs(Mathf.DeltaAngle(previousYaw, yaw)) > allowedTurn + 0.0001d) continue;
                bool occupied = false;
                if (activeWordPositions != null)
                    foreach (var position in activeWordPositions)
                        if (Vector3.Angle(direction, position - player) < placement.LaneSpacingDegrees)
                        { occupied = true; break; }
                if (occupied) continue;
                long used = lastUsed.TryGetValue(candidate, out long value) ? value : 0;
                string row = RowFor(candidate);
                double freshness = used == 0 ? 1d : Math.Min(1d, (sequence - used + 1d) / 6d);
                candidates.Add(new Candidate { Member = candidate, Row = row, Yaw = yaw, Difference = difference,
                    Weight = freshness / (1d + difference / 30d) });
                bool inSector = difference <= placement.LaneSpacingDegrees * 0.5f;
                bool better = selected == null || (inSector && !bestInRequestedSector) ||
                    (inSector == bestInRequestedSector && (inSector
                        ? used < oldest || (used == oldest && difference < bestAngle)
                        : difference < bestAngle || (Mathf.Approximately(difference, bestAngle) && used < oldest)));
                if (better)
                { selected = candidate; oldest = used; bestAngle = difference; selectedYaw = yaw; bestInRequestedSector = inSector; }
            }
            if (timing != null && candidates.Count > 0)
            {
                var choice = ChooseRandomRowMember();
                selected = choice.Member; selectedYaw = choice.Yaw;
                bestInRequestedSector = choice.Difference <= placement.LaneSpacingDegrees * 0.5f;
                LastSelectionRow = choice.Row;
            }
            if (selected == null) return false;
            lastUsed[selected] = ++sequence;
            LastSelectionRedirected = !bestInRequestedSector;
            LastSelectionClearance = config.WordsPassThroughScenery
                ? AudienceWordFlight.ClearanceForDistance(Vector3.Distance(selected.WordOrigin.position, player))
                : routes?.ClearanceFor(selected) ?? 0f;
            LastSelectionRow = LastSelectionRow ?? RowFor(selected);
            previousMember = selected; previousRow = LastSelectionRow;
            rowUses[LastSelectionRow] = rowUses.TryGetValue(LastSelectionRow, out int count) ? count + 1 : 1;
            if (timing != null) { previousSpawn = timing.SpawnTime; previousYaw = selectedYaw; }
            return true;
        }

        private string RowFor(AudienceWordEmitter member)
        {
            if (!config.WordsPassThroughScenery) return routes?.RowFor(member) ?? "Audience";
            for (var parent = member.transform.parent; parent != null; parent = parent.parent)
                if (parent.name.StartsWith("Fila", StringComparison.Ordinal)) return parent.name;
            return "Fila1";
        }

        private Candidate ChooseRandomRowMember()
        {
            rowWeights.Clear(); rowSizes.Clear();
            bool canAvoidPrevious = candidates.Exists(c => c.Member != previousMember);
            foreach (var candidate in candidates)
            {
                if (canAvoidPrevious && candidate.Member == previousMember) continue;
                rowWeights[candidate.Row] = rowWeights.TryGetValue(candidate.Row, out double sum) ? sum + candidate.Weight : candidate.Weight;
                rowSizes[candidate.Row] = rowSizes.TryGetValue(candidate.Row, out int size) ? size + 1 : 1;
            }
            double total = 0d;
            bool changeRow = rowWeights.Count > 1 && previousRow != null;
            foreach (var pair in rowWeights)
            {
                if (changeRow && pair.Key == previousRow) continue;
                int uses = rowUses.TryGetValue(pair.Key, out int count) ? count : 0;
                total += pair.Value / rowSizes[pair.Key] / Math.Sqrt(uses + 1d);
            }
            double draw = random.NextDouble() * total;
            string row = null;
            foreach (var pair in rowWeights)
            {
                if (changeRow && pair.Key == previousRow) continue;
                int uses = rowUses.TryGetValue(pair.Key, out int count) ? count : 0;
                draw -= pair.Value / rowSizes[pair.Key] / Math.Sqrt(uses + 1d);
                row = pair.Key;
                if (draw <= 0d) break;
            }
            total = 0d;
            foreach (var candidate in candidates)
                if (candidate.Row == row && (!canAvoidPrevious || candidate.Member != previousMember)) total += candidate.Weight;
            draw = random.NextDouble() * total;
            Candidate selected = default;
            foreach (var candidate in candidates)
            {
                if (candidate.Row != row || (canAvoidPrevious && candidate.Member == previousMember)) continue;
                selected = candidate;
                draw -= candidate.Weight;
                if (draw <= 0d) break;
            }
            return selected;
        }

        private static bool Finite(Vector3 point) => float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
    }
}
