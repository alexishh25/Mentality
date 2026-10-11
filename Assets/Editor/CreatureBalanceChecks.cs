using System;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CreatureBalanceChecks
{
    static CreatureBalanceChecks()
    {
        if (!File.Exists("Logs/Creature-balance.txt") || File.Exists("Temp/CreatureBalanceChecks.request")) EditorApplication.update += VerifyWhenReady;
    }
    private static void VerifyWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= VerifyWhenReady;
        Verify();
        if (File.Exists("Temp/CreatureBalanceChecks.request")) File.Delete("Temp/CreatureBalanceChecks.request");
    }
    [MenuItem("Tools/Mentality/Rhythm/Verify Creature Balance")]
    public static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
        if (File.Exists("Temp/CreatureSecondCadence.request"))
        {
            int cadence = int.Parse(File.ReadAllText("Temp/CreatureSecondCadence.request"));
            if (cadence < 1 || cadence > 8) throw new InvalidOperationException("Invalid creature cadence request");
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("config").FindPropertyRelative("Creatures").FindPropertyRelative("SecondEveryBeats").intValue = cadence;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scope.gameObject.scene);
            EditorSceneManager.SaveScene(scope.gameObject.scene);
            File.Delete("Temp/CreatureSecondCadence.request");
        }
        var config = (RhythmGameConfig)new SerializedObject(scope).FindProperty("config").boxedValue;
        var map = config.AnalyzedSong.LoadValidatedMap();
        var chart = new MusicalSpawnSchedule(map, config.MinimumSpawnInterval, config.TargetLeadTimeSeconds,
            config.MusicalActivity, config.Patterns, 0d, config.Placement, config.Cadence, config.AnalyzedSong.ManualIntensity);
        var creatures = new CreatureSpawnSchedule(map, config.AnalyzedSong.ManualIntensity, config.Creatures,
            config.Creatures.Prefab.GetComponent<CreatureEmergenceView>().EmergenceSeconds);
        var budget = new CreatureShotBudget(creatures, chart, (config.Creatures.Distance + 2d) / config.TravelSpeed);
        var times = creatures.Events.SelectMany(e => e.Shots).Distinct().Where(budget.Allows).ToArray();
        var samples = chart.Events.Select(e => e.SpawnTime.Value).Concat(times)
            .Concat(times.Select(budget.EndAt)).Concat(chart.ActivityProfile.ManualIntensity.SectionBoundaries).Distinct();
        foreach (double sample in samples)
        {
            int baseWords = chart.Events.Count(e => e.SpawnTime.Value <= sample + 1e-9d && e.HitTime >= sample - 1e-9d);
            int extras = times.Count(t => t <= sample && budget.EndAt(t) >= sample);
            int capacity = chart.MaximumTargetsAt(sample);
            foreach (var item in chart.Events)
                if (item.SpawnTime.Value <= sample + 1e-9d && item.HitTime >= sample - 1e-9d)
                    capacity = Math.Min(capacity, chart.MaximumTargetsAt(item.HitTime));
            if (extras > 0 && (extras + Math.Max(2, baseWords) > capacity || extras > (capacity >= 7 ? 3 : 2)))
                throw new InvalidOperationException("Rafaga desplaza eventos del mapa o excede su limite.");
        }
        var counts = Enumerable.Range(0, 3).Select(section => creatures.Events.Where(e => e.Section == section)
            .Sum(e => e.Shots.Count(budget.Allows))).ToArray();
        if (counts.Any(n => n == 0) || counts[2] <= counts[0]) throw new InvalidOperationException("Se perdio la progresion de dificultad.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Creature-balance.txt", "PASS: Complete chart reserved throughout all creature flights, including frame-latency margin and section boundaries. At least two chart slots, at most two creature words (three in peak).\n"
            + "Chart events preserved: " + chart.Events.Count + ". Accepted candidate shots per section: " + string.Join(", ", counts)
            + ". Live route clearance can additionally suppress shots; no delayed backlog.\n");
        File.AppendAllText("Logs/Creature-balance.txt", "Chart targets per section: " + string.Join(", ",
            config.AnalyzedSong.ManualIntensity.Sections.Select(s => chart.Events.Count(e => e.HitTime >= s.Start && e.HitTime < s.End)))
            + ". Creature appearances per section: " + string.Join(", ", Enumerable.Range(0, 3).Select(s => creatures.Events.Count(e => e.Section == s))) + ".\n");
        Debug.Log("Creature balance: PASS.");
    }
}
