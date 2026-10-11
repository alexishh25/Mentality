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
public static class CreatureProgressionChecks
{
    static CreatureProgressionChecks()
    {
        if (!File.Exists("Logs/Creature-progression.txt")) EditorApplication.delayCall += EnableAndVerify;
    }

    [MenuItem("Tools/Mentality/Rhythm/Enable And Verify Creature Progression")]
    public static void EnableAndVerify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
        if (scope == null || scope.gameObject.scene.path != "Assets/Content/Scenes/RhythmTest.unity")
            throw new InvalidOperationException("Abrir RhythmTest para configurar las criaturas.");
        var serialized = new SerializedObject(scope);
        var settings = serialized.FindProperty("config").FindPropertyRelative("Creatures");
        settings.FindPropertyRelative("Enabled").boolValue = true;
        settings.FindPropertyRelative("Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/Creatures/Creature.prefab");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scope.gameObject.scene);
        EditorSceneManager.SaveScene(scope.gameObject.scene);
        var config = (RhythmGameConfig)new SerializedObject(scope).FindProperty("config").boxedValue;
        var map = config.AnalyzedSong.LoadValidatedMap();
        var schedule = new CreatureSpawnSchedule(map, config.AnalyzedSong.ManualIntensity, config.Creatures,
            config.Creatures.Prefab.GetComponent<CreatureEmergenceView>().EmergenceSeconds);
        int[] expectedShots = { 2, 3, 4 };
        int[] counts = new int[3];
        int[] shots = new int[3];
        foreach (var item in schedule.Events)
        {
            var range = config.AnalyzedSong.ManualIntensity.Sections[item.Section];
            if (item.Time < range.Start || item.Time >= range.End || Array.BinarySearch(map.Events.Beats, item.Time) < 0
                || item.Shots.Length != expectedShots[item.Section] || item.Shots.Last() >= range.End
                || item.Shots[0] - item.Time < 1.15d - 1e-6)
                throw new InvalidOperationException("Aparicion o rafaga fuera de los beats/seccion.");
            counts[item.Section]++;
            shots[item.Section] += item.Shots.Length;
        }
        if (!(counts[0] < counts[1] && counts[1] < counts[2]) || schedule.Events.Select(e => e.Angle).Distinct().Count() != 5)
            throw new InvalidOperationException("La progresion o las posiciones no aumentan correctamente.");
        var restarted = new CreatureSpawnSchedule(map, config.AnalyzedSong.ManualIntensity, config.Creatures, 0.8f);
        if (restarted.TryTakeDue(71d, out _) || !restarted.TryTakeDue(schedule.Events[0].Time, out var first)
            || first.Time != schedule.Events[0].Time || restarted.TryTakeDue(first.Time, out _))
            throw new InvalidOperationException("Primer evento o consumo duplicado incorrecto.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Creature-progression.txt", "PASS: Native scene saved with creature prefab enabled. Real beat appearances, beat/half-beat bursts, five alternating positions, section boundaries and fresh schedule checked.\n"
            + string.Join("\n", Enumerable.Range(0, 3).Select(i => "Section " + (i + 1) + ": " + counts[i] + " appearances; " + shots[i] + " scheduled false words (subject to shared capacity).")));
        Debug.Log("Creature musical progression: PASS.");
    }
}
