using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public static class RhythmPlacementPlanner
    {
        public static void ValidateSettings(RhythmPlacementSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!Finite(settings.MaximumSideAngle) || settings.MaximumSideAngle < 15f || settings.MaximumSideAngle > 90f
                || !Finite(settings.LaneSpacingDegrees) || settings.LaneSpacingDegrees < 5f || settings.LaneSpacingDegrees > 45f
                || !Finite(settings.MaximumTurnDegrees) || settings.MaximumTurnDegrees < settings.LaneSpacingDegrees || settings.MaximumTurnDegrees > 180f
                || !Finite(settings.MaximumTurnSpeedDegreesPerSecond) || settings.MaximumTurnSpeedDegreesPerSecond < 15f
                || !Finite(settings.TargetAngularWidthDegrees) || settings.TargetAngularWidthDegrees < 2f
                || settings.TargetAngularWidthDegrees >= settings.LaneSpacingDegrees)
                throw new ArgumentException("La colocacion necesita un arco y giro validos, y palabras mas estrechas que la separacion de rutas.", nameof(settings));
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static MusicalSpawnEvent[] Build(IReadOnlyList<MusicalSpawnEvent> source, double leadTime,
            double lateWindow, RhythmPlacementSettings settings, RhythmDifficultyProfile difficulty = null,
            MusicalActivityProfile activity = null)
        {
            ValidateSettings(settings);
            if (double.IsNaN(leadTime) || double.IsInfinity(leadTime) || leadTime < 0
                || double.IsNaN(lateWindow) || double.IsInfinity(lateWindow) || lateWindow < 0)
                throw new ArgumentOutOfRangeException(nameof(leadTime));
            var result = new List<MusicalSpawnEvent>();
            var occupied = new List<MusicalSpawnEvent>();
            double side = Math.Min(settings.MaximumSideAngle,
                difficulty == null ? settings.MaximumSideAngle : difficulty.SideAngleDegrees + settings.LaneSpacingDegrees);
            int laneCount = (int)Math.Floor(side / settings.LaneSpacingDegrees);
            foreach (var item in source)
            {
                double spawnTime = item.SpawnTime ?? (item.HitTime - leadTime);
                occupied.RemoveAll(other => other.HitTime + lateWindow < spawnTime - 1e-9d);
                double preferred = item.AngleDegrees ?? (result.Count % 2 == 0 ? -side * 0.5d : side * 0.5d);
                float? chosen = null;
                double best = double.PositiveInfinity;
                for (int lane = -laneCount; lane <= laneCount; lane++)
                {
                    float angle = lane * settings.LaneSpacingDegrees;
                    if (result.Count > 0)
                    {
                        var previous = result[result.Count - 1];
                        double frenzy = activity == null ? 0d : activity.FrenzyAt(item.HitTime);
                        double allowedTurn = Math.Min(Math.Min(180d, settings.MaximumTurnDegrees * (1d + 0.7d * frenzy)),
                            (spawnTime - (previous.SpawnTime ?? (previous.HitTime - leadTime)))
                                * settings.MaximumTurnSpeedDegreesPerSecond * (1d + 4d * frenzy));
                        if (Math.Abs(angle - previous.AngleDegrees.Value) > allowedTurn + 1e-6d) continue;
                    }
                    bool blocked = false;
                    foreach (var other in occupied)
                        if (Math.Abs(angle - other.AngleDegrees.Value) < settings.LaneSpacingDegrees - 1e-6d) { blocked = true; break; }
                    if (blocked) continue;
                    double cost = Math.Abs(angle - preferred);
                    if (cost < best) { best = cost; chosen = angle; }
                }
                // Nunca desplazar el hitTime para esperar una ruta libre.
                if (!chosen.HasValue) continue;
                var placed = new MusicalSpawnEvent(item.HitTime, chosen, item.Pattern, item.SpawnTime);
                result.Add(placed);
                occupied.Add(placed);
            }
            return result.ToArray();
        }

        public static float ReadabilityScale(double distance, double initialDistance, double initialWidth, double angularWidth)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0
                || double.IsNaN(initialDistance) || double.IsInfinity(initialDistance) || initialDistance <= 0
                || double.IsNaN(initialWidth) || double.IsInfinity(initialWidth) || initialWidth <= 0
                || double.IsNaN(angularWidth) || double.IsInfinity(angularWidth) || angularWidth <= 0 || angularWidth >= 180)
                throw new ArgumentOutOfRangeException(nameof(distance));
            double initialScale = Math.Min(1d, 2d * initialDistance * Math.Tan(angularWidth * Math.PI / 360d) / initialWidth);
            return (float)(initialScale * Math.Min(1d, distance / initialDistance));
        }
    }
}
