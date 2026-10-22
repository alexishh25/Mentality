using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelAudienceTimingChecks
{
    private sealed class Flight { public double Hit; public Vector3 Origin; }
    private sealed class Result
    {
        public int Spawned, Redirected, Skipped, Peak;
        public int CapacitySkipped, NoEmitterSkipped;
        public double MinSpeed = double.PositiveInfinity, MaxSpeed;
        public readonly List<double> MissedTimes = new List<double>();
        public readonly List<string> Assignments = new List<string>();
        public readonly Dictionary<string, int> Rows = new Dictionary<string, int>();
        public readonly HashSet<string> Members = new HashSet<string>();
        public int RowRepeats, MemberRepeats;
    }
    static NivelAudienceTimingChecks() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
            !File.Exists("Temp/NivelAudienceTiming.request")) return;
        File.Delete("Temp/NivelAudienceTiming.request");
        try { Check(); }
        catch (Exception e) { File.WriteAllText("Logs/NivelAudienceTiming.md", "FAIL\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Mentality/Check Audience Musical Timing")]
    public static void Check()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm") throw new InvalidOperationException("Open Nivel-1_Rhythm.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var scope = all.Select(t => t.GetComponent<RhythmLifetimeScope>()).Single(t => t != null);
        var settings = new SerializedObject(scope);
        var config = (RhythmGameConfig)settings.FindProperty("config").boxedValue;
        var player = (Transform)settings.FindProperty("playerTarget").objectReferenceValue;
        var layout = (WordLaunchLayout)settings.FindProperty("wordLaunchLayout").objectReferenceValue;
        var registry = (AudienceWordEmitterRegistry)settings.FindProperty("audienceWordEmitters").objectReferenceValue;
        var routes = registry.GetComponent<AudienceWordRouteLayout>();
        var map = config.AnalyzedSong.LoadValidatedMap();
        var chart = new MusicalSpawnSchedule(map, config.MinimumSpawnInterval, config.TargetLeadTimeSeconds,
            config.MusicalActivity, config.Patterns, 0d, config.Placement, config.Cadence, config.AnalyzedSong.ManualIntensity, config.CreatureReservedSlots);
        float yaw = layout == null ? 0 : layout.transform.eulerAngles.y;
        var baseline = Simulate(chart, registry, routes, config, player.position, yaw, false);
        var adapted = Simulate(chart, registry, routes, config, player.position, yaw, true);
        var repeat = Simulate(chart, registry, routes, config, player.position, yaw, true);
        if (!adapted.Assignments.SequenceEqual(repeat.Assignments)) throw new InvalidOperationException("Reset produced a different assignment sequence.");
        var anotherSession = Simulate(chart, registry, routes, config, player.position, yaw, true, 2027);
        if (adapted.Assignments.SequenceEqual(anotherSession.Assignments)) throw new InvalidOperationException("Different random seeds produced the same audience sequence.");
        if (adapted.Rows.Count < 4 || adapted.Members.Count < 20)
            throw new InvalidOperationException("Audience selection does not use enough rows and characters.");
        var log = new StringBuilder("# Sincronización musical del público\n\n");
        log.AppendLine("Se conserva el instante de salida y llegada de cada evento. La distancia del personaje cambia la velocidad media, sin desplazar la música.\n");
        log.AppendLine("| Comprobación de canción completa | Selección fija por sector | Selección aleatoria por filas |\n|---|---:|---:|");
        log.AppendLine("| Eventos programados | " + chart.Events.Count + " | " + chart.Events.Count + " |");
        log.AppendLine("| Palabras asignadas | " + baseline.Spawned + " | " + adapted.Spawned + " |");
        log.AppendLine("| Reasignadas a otro sector libre | 0 | " + adapted.Redirected + " |");
        log.AppendLine("| Omitidas sin retrasarlas | " + baseline.Skipped + " | " + adapted.Skipped + " |");
        log.AppendLine("| Por límite musical de simultáneas | " + baseline.CapacitySkipped + " | " + adapted.CapacitySkipped + " |");
        log.AppendLine("| Por falta de emisor compatible | " + baseline.NoEmitterSkipped + " | " + adapted.NoEmitterSkipped + " |");
        log.AppendLine("| Máximo simultáneo asignado | " + baseline.Peak + " | " + adapted.Peak + " |");
        log.AppendLine("\nVelocidad media de los recorridos asignados: " + adapted.MinSpeed.ToString("F2") + "–" + adapted.MaxSpeed.ToString("F2") + " m/s.");
        log.AppendLine("\nPASS: entrada y llegada musicales, separación entre rutas, límites de giro reales, capacidad de palabras normales y reproducibilidad con semilla fija. Cada sesión de juego usa una semilla nueva. Las criaturas y la latencia de frames se comprueban aparte en Play.");
        log.AppendLine("\n## Variedad del público\n\nPersonajes distintos: " + adapted.Members.Count + ". Repeticiones consecutivas del mismo personaje: " + adapted.MemberRepeats + ".\n");
        foreach (var row in adapted.Rows.OrderBy(p => p.Key)) log.AppendLine("- " + row.Key + ": " + row.Value + " palabras.");
        log.AppendLine("\nRepeticiones consecutivas de fila: " + adapted.RowRepeats + " (se permite repetir cuando no hay otra fila compatible).");
        log.AppendLine("\nPASS: diferentes semillas producen secuencias distintas, con varias filas y personajes; la prueba usa semilla fija para poder reproducirse.");
        log.AppendLine("\nEventos omitidos (tiempo de llegada en segundos): " + (adapted.MissedTimes.Count == 0 ? "ninguno" : string.Join(", ", adapted.MissedTimes.Select(t => t.ToString("F3")))));
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/NivelAudienceTiming.md", log.ToString());
        Debug.Log("Audience musical timing PASS: " + adapted.Spawned + "/" + chart.Events.Count + "; redirected=" + adapted.Redirected + "; skipped=" + adapted.Skipped);
    }

    private static Result Simulate(MusicalSpawnSchedule chart, AudienceWordEmitterRegistry registry,
        AudienceWordRouteLayout routes, RhythmGameConfig config, Vector3 player, float yaw, bool adapt, int seed = 2026)
    {
        var result = new Result();
        var selector = new AudienceWordSelector(registry, config, routes, seed);
        var active = new List<Flight>();
        var positions = new List<Vector3>();
        double previousTime = double.NegativeInfinity;
        float previousYaw = 0;
        string previousRow = null, previousMember = null;
        foreach (var item in chart.Events)
        {
            var timing = new TargetTiming(item.SpawnTime ?? item.HitTime - config.TargetLeadTimeSeconds, item.HitTime, chart.ActivityProfile);
            active.RemoveAll(f => f.Hit <= timing.SpawnTime + 1e-9d);
            positions.Clear(); positions.AddRange(active.Select(f => f.Origin));
            AudienceWordEmitter member = null;
            bool capacityAvailable = active.Count < chart.MaximumTargetsAt(item.HitTime);
            bool found = capacityAvailable &&
                selector.TrySelect(item.AngleDegrees ?? 0, player, yaw, positions, out member, adapt ? timing : null);
            if (!found)
            {
                result.Skipped++;
                if (capacityAvailable) result.NoEmitterSkipped++; else result.CapacitySkipped++;
                result.MissedTimes.Add(item.HitTime); result.Assignments.Add("skip"); continue;
            }
            Vector3 direction = member.WordOrigin.position - player;
            float actualYaw = Mathf.DeltaAngle(yaw, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            if (positions.Any(p => Vector3.Angle(direction, p - player) < config.Placement.LaneSpacingDegrees - 0.0001f))
                throw new InvalidOperationException("Overlapping audience routes.");
            if (adapt && !double.IsNegativeInfinity(previousTime))
            {
                double frenzy = chart.ActivityProfile?.FrenzyAt(item.HitTime) ?? 0;
                double limit = Math.Min(config.Placement.MaximumTurnDegrees * (1 + (config.Placement.FrenzyTurnAngleMultiplier - 1) * frenzy),
                    (timing.SpawnTime - previousTime) * config.Placement.MaximumTurnSpeedDegreesPerSecond *
                    (1 + (config.Placement.FrenzyTurnSpeedMultiplier - 1) * frenzy));
                if (Math.Abs(Mathf.DeltaAngle(previousYaw, actualYaw)) > limit + 0.0001d)
                    throw new InvalidOperationException("Actual audience angle exceeds the musical turn budget.");
            }
            if (Math.Abs(timing.ProgressAt(timing.SpawnTime)) > 1e-8d || Math.Abs(timing.ProgressAt(item.HitTime) - 1d) > 1e-8d)
                throw new InvalidOperationException("Audience flight is not aligned to musical time.");
            double speed = (direction.magnitude - config.ArrivalDistance) / timing.LeadTime;
            result.MinSpeed = Math.Min(result.MinSpeed, speed); result.MaxSpeed = Math.Max(result.MaxSpeed, speed);
            previousTime = timing.SpawnTime; previousYaw = actualYaw;
            result.Spawned++; if (adapt && selector.LastSelectionRedirected) result.Redirected++;
            string row = selector.LastSelectionRow ?? "Audience";
            result.Rows[row] = result.Rows.TryGetValue(row, out int rowCount) ? rowCount + 1 : 1;
            result.Members.Add(member.name);
            if (row == previousRow) result.RowRepeats++;
            if (member.name == previousMember) result.MemberRepeats++;
            previousRow = row; previousMember = member.name;
            result.Assignments.Add(member.name + "@" + timing.HitTime.ToString("R"));
            active.Add(new Flight { Hit = item.HitTime, Origin = member.WordOrigin.position });
            result.Peak = Math.Max(result.Peak, active.Count);
        }
        return result;
    }
}
