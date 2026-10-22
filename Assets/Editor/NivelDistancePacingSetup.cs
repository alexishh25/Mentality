using System;
using System.IO;
using System.Linq;
using System.Text;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelDistancePacingSetup
{
    private const string OriginalSong = "Assets/Content/Music/Midnight_at_the_Gallows.asset";
    private const string LevelSong = "Assets/Content/Music/Midnight_at_the_Gallows_Nivel1.asset";
    static NivelDistancePacingSetup() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/NivelDistancePacing.request")) return;
        File.Delete("Temp/NivelDistancePacing.request");
        try { Prepare(); }
        catch (Exception e) { File.WriteAllText("Logs/NivelDistancePacing.md", "FAIL\n" + e); Debug.LogException(e); }
    }
    private static MusicalSpawnSchedule Chart(RhythmGameConfig config) => new MusicalSpawnSchedule(config.AnalyzedSong.LoadValidatedMap(),
        config.MinimumSpawnInterval, config.TargetLeadTimeSeconds, config.MusicalActivity, config.Patterns, 0d,
        config.Placement, config.Cadence, config.AnalyzedSong.ManualIntensity, config.CreatureReservedSlots);

    [MenuItem("Tools/Mentality/Integration/Calibrate Nivel Distance Pacing")]
    public static void Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm") throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var scope = all.Select(t => t.GetComponent<RhythmLifetimeScope>()).Single(s => s != null);
        var settings = new SerializedObject(scope);
        var config = (RhythmGameConfig)settings.FindProperty("config").boxedValue;
        int previousCount = Chart(config).Events.Count;
        config.WordsPassThroughScenery = true;
        var player = (Transform)settings.FindProperty("playerTarget").objectReferenceValue;
        var routes = all.Select(t => t.GetComponent<AudienceWordRouteLayout>()).Single(s => s != null);
        var registry = routes.GetComponent<AudienceWordEmitterRegistry>();
        var members = registry.Emitters.Where(e => e.CanEmit && (config.WordsPassThroughScenery || routes.IsClear(e, player.position, config))
            && Mathf.Abs(Mathf.Atan2(e.WordOrigin.position.x - player.position.x, e.WordOrigin.position.z - player.position.z) * Mathf.Rad2Deg) <= config.Placement.MaximumSideAngle).ToArray();
        if (members.Length < 20) throw new InvalidOperationException("Validate usable audience routes first.");
        var original = AssetDatabase.LoadAssetAtPath<MusicalSongAsset>(OriginalSong);
        string originalJson = File.ReadAllText(OriginalSong);
        var before = JsonUtility.FromJson<RhythmGameConfig>(JsonUtility.ToJson(config));
        before.AnalyzedSong = original; before.SpawnDistance = 12f; before.TravelSpeed = 3f; before.UseDistanceAwareApproach = false;
        before.WordsPassThroughScenery = false; before.CreatureReservedSlots = 0;
        before.Cadence.BeatsPerTarget = 2; before.Cadence.PressureExtraBeatEvery = 2;
        before.Cadence.AttackVariationsEnabled = true;
        var previousChart = Chart(before);
        var log = new StringBuilder("# Ritmo adaptado a las distancias de Nivel-1\n\n");
        AppendMetrics(log, "Antes: referencia RhythmTest", before, previousChart, members, routes, player.position);
        var song = AssetDatabase.LoadAssetAtPath<MusicalSongAsset>(LevelSong);
        if (song == null) { song = ScriptableObject.CreateInstance<MusicalSongAsset>(); AssetDatabase.CreateAsset(song, LevelSong); }
        Undo.RecordObject(song, "Configure level difficulty"); EditorUtility.CopySerialized(original, song); song.name = "Midnight_at_the_Gallows_Nivel1";
        float[] speeds = { 1.67f, 1.40f, 2.05f }, frenzy = { 0.60f, 0.38f, 0.78f };
        song.ManualIntensity.TransitionSeconds = 1.8f;
        for (int i = 0; i < 3; i++)
        {
            song.ManualIntensity.Sections[i].SpeedMultiplier = speeds[i];
            song.ManualIntensity.Sections[i].Frenzy = frenzy[i];
            song.ManualIntensity.Sections[i].MinimumInterval = 0.3f;
            song.ManualIntensity.Sections[i].AttackEveryBeats = 8;
        }
        config.AnalyzedSong = song; config.UseDistanceAwareApproach = true; config.ReadableApproachFraction = 0.65f;
        config.Cadence.BeatsPerTarget = 2; config.Cadence.PressureExtraBeatEvery = 4; config.Cadence.AttackEveryBeats = 8;
        config.Cadence.AttackVariationsEnabled = true;
        config.CreatureReservedSlots = 2;
        config.MusicalActivity.MaximumSpeedMultiplier = 1.90f; config.MusicalActivity.MaximumFrenzy = 0.78f;
        config.ImmediateWordGrowth = true; config.Creatures.ContinuousAttack = true;
        config.MusicalActivity.FrenzyIntervalMultiplier = 0.95f;
        config.Evasion.OscillationHz = 1.8f; config.Evasion.ReleaseSeconds = 0.22f;
        config.Placement.FrenzyTurnAngleMultiplier = 1.12f; config.Placement.FrenzyTurnSpeedMultiplier = 1.4f;
        config.Creatures.FirstEveryBeats = 8; config.Creatures.BridgeEveryBeats = 10; config.Creatures.SecondEveryBeats = 4;
        float maximumDistance = members.Max(e => Vector3.Distance(e.WordOrigin.position, player.position));
        float requiredLead = 4.75f;
        foreach (var member in members)
        {
            float distance = Vector3.Distance(member.WordOrigin.position, player.position) - config.ArrivalDistance;
            float clearance = Clearance(config, routes, member, player.position);
            float q = AudienceWordFlight.ReadableTravel(clearance) / distance;
            float slope = q > 1f - config.ReadableApproachFraction ? (1f - q) / config.ReadableApproachFraction : 1f;
            requiredLead = Mathf.Max(requiredLead, distance * slope * speeds.Max() / 3.2f);
            if (AudienceWordFlight.ScaleAt(0.01f, clearance, config.ImmediateWordGrowth) <= AudienceWordFlight.InitialScale)
                throw new InvalidOperationException("Word growth still has a stationary opening plateau.");
            float previous = 0f;
            for (int step = 0; step <= 1000; step++)
            {
                float p = AudienceWordFlight.TravelProgressAt(step / 1000f, distance, clearance, config.ReadableApproachFraction);
                if (p < previous - 0.00001f || p < 0f || p > 1.00001f) throw new InvalidOperationException("Non-monotonic distance pacing.");
                previous = p;
            }
            if (Mathf.Abs(previous - 1f) > 0.00001f) throw new InvalidOperationException("Pacing misses arrival endpoint.");
        }
        requiredLead = Mathf.Ceil(requiredLead * 4f) / 4f;
        config.SpawnDistance = Mathf.Ceil(maximumDistance * 4f) / 4f;
        config.TravelSpeed = (config.SpawnDistance - config.ArrivalDistance) / requiredLead;
        var chart = Chart(config);
        log.AppendLine("Palabras del plan anterior: " + previousCount + ". Nuevo plan: " + chart.Events.Count + ".\n");
        var formerlyBlocked = members.FirstOrDefault(e => !routes.IsClear(e, player.position, config));
        if (formerlyBlocked == null) throw new InvalidOperationException("No formerly blocked emitter available for passthrough check.");
        var probe = new GameObject("Passthrough emitter verification");
        try
        {
            var single = probe.AddComponent<AudienceWordEmitterRegistry>(); single.Configure(new[] { formerlyBlocked });
            float angle = Mathf.Atan2(formerlyBlocked.WordOrigin.position.x - player.position.x, formerlyBlocked.WordOrigin.position.z - player.position.z) * Mathf.Rad2Deg;
            config.WordsPassThroughScenery = false;
            if (new AudienceWordSelector(single, config, routes).TrySelect(angle, player.position, 0f, null, out _))
                throw new InvalidOperationException("Blocked control route unexpectedly accepted.");
            config.WordsPassThroughScenery = true;
            if (!new AudienceWordSelector(single, config, routes).TrySelect(angle, player.position, 0f, null, out var chosen) || chosen != formerlyBlocked)
                throw new InvalidOperationException("Scenery still prevents emission.");
            log.AppendLine("PASS: un emisor bloqueado por la escenografia se rechaza con el filtro antiguo y se selecciona con el nuevo modo de paso libre.\n");
        }
        finally { config.WordsPassThroughScenery = true; UnityEngine.Object.DestroyImmediate(probe); }
        AppendMetrics(log, "Ahora: distancias reales y acercamiento legible", config, chart, members, routes, player.position);
        if (song.MusicClip != original.MusicClip || song.AnalysisJson != original.AnalysisJson || File.ReadAllText(OriginalSong) != originalJson)
            throw new InvalidOperationException("Original audio/map configuration changed.");
        for (int i = 0; i < 3; i++)
            if (song.ManualIntensity.Sections[i].Start != original.ManualIntensity.Sections[i].Start || song.ManualIntensity.Sections[i].End != original.ManualIntensity.Sections[i].End)
                throw new InvalidOperationException("Musical section boundaries changed.");
        var creatures = new CreatureSpawnSchedule(song.LoadValidatedMap(), song.ManualIntensity, config.Creatures,
            config.Creatures.Prefab.GetComponent<CreatureEmergenceView>().EmergenceSeconds);
        var grounds = all.Select(t => t.GetComponent<CreaturePlacementLayout>()).Single(t => t != null);
        var budget = new CreatureShotBudget(creatures, chart, Math.Max(config.Creatures.Distance + 2d,
            grounds.MaximumMouthDistance(player.position) + 0.1d) / config.TravelSpeed);
        if (creatures.Events.Any(e => Math.Abs(e.Shots[0] - e.Time - config.Creatures.Prefab.GetComponent<CreatureEmergenceView>().EmergenceSeconds) > 0.000001d))
            throw new InvalidOperationException("Creature still waits after emerging before the first shot.");
        log.AppendLine("PASS: primer disparo al completar la emergencia, sin preparacion ni espera adicional hasta el siguiente beat. Crecimiento y abanico de palabras desde la salida.\n");
        for (int i = 0; i < 3; i++)
        {
            int admitted = creatures.Events.Where(e => e.Section == i).Sum(e => e.Shots.Count(budget.Allows));
            if (admitted == 0) throw new InvalidOperationException("No creature shots admitted in section " + i);
            log.AppendLine("Disparos de criatura admitidos en tramo " + (i + 1) + ": " + admitted + ".\n");
        }
        log.AppendLine("Distancias reales revisadas: " + members.Min(e => Vector3.Distance(e.WordOrigin.position, player.position)).ToString("F2") + "–" + maximumDistance.ToString("F2") + " m. Anticipacion base: " + config.TargetLeadTimeSeconds.ToString("F2") + " s.\n");
        log.AppendLine("Los primeros metros de filas lejanas despejan el publico; el 65% del tiempo de movimiento se reserva para el acercamiento legible. La velocidad enlaza sin saltos. Las filas cercanas conservan movimiento uniforme.\n");
        log.AppendLine("PASS: recorrido monotono, llegada en el beat, espacio de lectura, disparos de criaturas por tramo y limites musicales originales. RhythmTest conserva su asset.\n");
        Undo.RecordObject(scope, "Apply level distance pacing"); settings.FindProperty("config").boxedValue = config;
        settings.ApplyModifiedProperties(); EditorUtility.SetDirty(song); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/NivelDistancePacing.md", log.ToString());
        Debug.Log("Nivel distance pacing calibrated: lead=" + config.TargetLeadTimeSeconds + "; chart=" + chart.Events.Count);
    }

    private static void AppendMetrics(StringBuilder log, string title, RhythmGameConfig config, MusicalSpawnSchedule chart,
        AudienceWordEmitter[] members, AudienceWordRouteLayout routes, Vector3 player)
    {
        string[] names = { "Inicio", "Primer tramo intenso", "Puente", "Tramo final intenso", "Cierre" };
        double[] starts = { 0, 71.9, 102, 132, 162.1 }, ends = { 71.9, 102, 132, 162.1, 999 };
        log.AppendLine("## " + title + "\n\n| Tramo | Palabras programadas | Menor tiempo legible | Mayor velocidad legible |\n|---|---:|---:|---:|");
        for (int section = 0; section < 5; section++)
        {
            var events = chart.Events.Where(e => e.HitTime >= starts[section] && e.HitTime < ends[section]).ToArray();
            double minimum = double.PositiveInfinity, maximum = 0;
            foreach (var item in events)
            {
                var timing = new TargetTiming(item.SpawnTime.Value, item.HitTime, chart.ActivityProfile);
                foreach (var member in members)
                {
                    double distance = Vector3.Distance(member.WordOrigin.position, player) - config.ArrivalDistance;
                    double q = AudienceWordFlight.ReadableTravel(Clearance(config, routes, member, player)) / distance;
                    double fraction = config.UseDistanceAwareApproach ? config.ReadableApproachFraction : 0d;
                    bool shaped = fraction > 0 && q > 1d - fraction && q < 1d;
                    double motionFraction = shaped ? 1d - fraction : q;
                    double low = timing.SpawnTime, high = timing.HitTime;
                    for (int n = 0; n < 36; n++) { double mid = (low + high) * 0.5d; if (timing.ProgressAt(mid) < motionFraction) low = mid; else high = mid; }
                    double readableStart = (low + high) * 0.5d;
                    minimum = Math.Min(minimum, item.HitTime - readableStart);
                    double slope = shaped ? (1d - q) / fraction : 1d;
                    for (int n = 0; n <= 24; n++)
                        maximum = Math.Max(maximum, distance * slope / config.TargetLeadTimeSeconds * chart.ActivityProfile.SpeedMultiplierAt(readableStart + (item.HitTime - readableStart) * n / 24d));
                    if (config.UseDistanceAwareApproach && (item.HitTime - readableStart < 1.5d || maximum > 3.21d))
                        throw new InvalidOperationException("Readable flight is still too fast.");
                }
            }
            log.AppendLine("| " + names[section] + " | " + events.Length + " | " + (events.Length == 0 ? "—" : minimum.ToString("F2") + " s") + " | " + maximum.ToString("F2") + " m/s |");
        }
        log.AppendLine("\nTiempo legible medido desde aproximadamente 68% del tamano final hasta la llegada, incluyendo crecimiento aplazado. Cotas conservadoras: todos los emisores aptos contra todos los eventos del tramo, sin contar intercepciones tempranas.\n");
    }
    private static float Clearance(RhythmGameConfig config, AudienceWordRouteLayout routes, AudienceWordEmitter member, Vector3 player)
        => config.WordsPassThroughScenery ? AudienceWordFlight.ClearanceForDistance(Vector3.Distance(member.WordOrigin.position, player)) : routes.ClearanceFor(member);
}
