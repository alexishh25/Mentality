using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;
using VContainer;

[InitializeOnLoad]
public static class NivelGameplayPerformanceChecks
{
    private const string Running = "Nivel.Performance.Running";
    private static RhythmLifetimeScope scope;
    private static RhythmGameModel model;
    private static RhythmGamePresenter game;
    private static AudioConductorService clock;
    private static RhythmGameConfig config;
    private static Mouse mouse, previousMouse;
    private static bool restoreMouse;
    private static readonly HashSet<int> words = new HashSet<int>(), creatures = new HashSet<int>();
    private static readonly List<Sample>[] samples = Enumerable.Range(0, 5).Select(_ => new List<Sample>()).ToArray();
    private static readonly int[] chartCounts = new int[5], creatureWordCounts = new int[5], appearanceCounts = new int[5];
    private static readonly List<string> errors = new List<string>();
    private static int lastFrame, initialSanity;
    private static double deadline, nextProgress;
    private static bool initialized, firstStress, secondStress;
    private static Func<int> drawCalls, triangles;

    static NivelGameplayPerformanceChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (File.Exists("Temp/NivelPerformance.request")) EditorApplication.update += StartWhenReady;
    }
    private static void StartWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= StartWhenReady;
        File.Delete("Temp/NivelPerformance.request");
        SessionState.SetString("Nivel.Performance.PreviousStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(NivelPlayerSetup.ScenePath);
        SessionState.SetBool(Running, true); EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            initialized = false; firstStress = secondStress = false; lastFrame = -1;
            words.Clear(); creatures.Clear(); errors.Clear(); foreach (var segment in samples) segment.Clear();
            Array.Clear(chartCounts, 0, 5); Array.Clear(creatureWordCounts, 0, 5); Array.Clear(appearanceCounts, 0, 5);
            deadline = EditorApplication.timeSinceStartup + 240; nextProgress = 0;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        if (state == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            string previous = SessionState.GetString("Nivel.Performance.PreviousStart", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void OnLog(string message, string stack, LogType type)
    { if ((type == LogType.Error || type == LogType.Exception) && errors.Count < 30) errors.Add(message + "\n" + stack); }
    private static Func<int> Counter(string name)
    {
        var property = typeof(UnityStats).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        var field = typeof(UnityStats).GetField(name, BindingFlags.Public | BindingFlags.Static);
        return () => property != null ? Convert.ToInt32(property.GetValue(null)) : field != null ? Convert.ToInt32(field.GetValue(null)) : -1;
    }
    private static int Segment(double t) => t < 71.9 ? 0 : t < 102 ? 1 : t < 132 ? 2 : t < 162.1 ? 3 : 4;
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) { errors.Add("Performance run timed out or remained paused"); Finish(); return; }
            if (!initialized)
            {
                scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                if (scope?.Container == null) return;
                model = scope.Container.Resolve<RhythmGameModel>(); game = scope.Container.Resolve<RhythmGamePresenter>();
                clock = scope.Container.Resolve<AudioConductorService>(); config = scope.Container.Resolve<RhythmGameConfig>();
                previousMouse = Mouse.current; restoreMouse = previousMouse != null && previousMouse.enabled;
                if (restoreMouse) InputSystem.DisableDevice(previousMouse);
                mouse = InputSystem.AddDevice<Mouse>();
                if (model.IsPaused) InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                initialSanity = model.Sanity; initialized = true;
                drawCalls = Counter("drawCalls"); triangles = Counter("triangles");
                Inventory(); return;
            }
            if (mouse.leftButton.isPressed) InputSystem.QueueStateEvent(mouse, new MouseState());
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (model.IsGameOver || model.HasWon) { Finish(); return; }
            if (model.IsPaused || !clock.IsPlaying) return;
            double time = clock.CurrentSongTime; int segment = Segment(time);
            var activeWords = Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
            var activeCreatures = Object.FindObjectsByType<CreatureEmergenceView>(FindObjectsSortMode.None);
            foreach (var word in activeWords)
            {
                if (word.IsResolved) continue;
                if (words.Add(word.GetInstanceID()))
                {
                    if (word.Timing != null && word.Timing.LateralArcDegrees != 0f) creatureWordCounts[segment]++;
                    else chartCounts[segment]++;
                }
                // Automated interception isolates sustained load from player skill; no immunity or time scaling.
                if (word.Timing != null && word.Timing.ProgressAt(time) >= 0.8d) word.Touch(scope.transform.position);
            }
            foreach (var creature in activeCreatures)
                if (creatures.Add(creature.GetInstanceID())) appearanceCounts[segment]++;
            if (time >= 0d)
                samples[segment].Add(new Sample { ms = Time.unscaledDeltaTime * 1000f, draws = drawCalls(), tris = triangles(),
                    words = activeWords.Count(w => !w.IsResolved), creatures = activeCreatures.Length,
                    effectStress = (time >= 85 && time < 91) || (time >= 145 && time < 151) });
            if ((!firstStress && time >= 85d) || (!secondStress && time >= 145d))
            {
                var effects = scope.Container.Resolve<FalseWordEffectService>();
                effects.Request(WordTargetKind.Screamer); effects.Request(WordTargetKind.Glitch); effects.Request(WordTargetKind.Bullicio);
                if (!firstStress) firstStress = true; else secondStress = true;
            }
            if (time >= nextProgress)
            {
                File.WriteAllText("Logs/Nivel-1-performance-progress.json", JsonUtility.ToJson(new Progress {
                    songTime = time, section = segment, chartWords = chartCounts.Sum(), creatureWords = creatureWordCounts.Sum(),
                    creatures = appearanceCounts.Sum(), sanity = model.Sanity, latestFrameMs = Time.unscaledDeltaTime * 1000f
                }, true));
                nextProgress = time + 10d;
            }
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private static void Inventory()
    {
        var meshes = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(m => m.sharedMesh != null && m.GetComponent<Renderer>() != null && m.GetComponent<Renderer>().enabled
                && m.transform.root.name != "Rhythm Player")
            .Select(m => new { mesh = m.sharedMesh, path = AssetDatabase.GetAssetPath(m.sharedMesh),
                triangles = Enumerable.Range(0, m.sharedMesh.subMeshCount).Sum(i => (long)m.sharedMesh.GetIndexCount(i)) / 3 }).ToArray();
        File.WriteAllLines("Logs/Nivel-1-render-inventory.txt", new[] {
            "Enabled scenery mesh instances: " + meshes.Length, "Total assigned triangles (including off-screen): " + meshes.Sum(m => m.triangles),
            "Largest shared mesh groups:" }.Concat(meshes.GroupBy(m => m.mesh).OrderByDescending(g => g.Sum(m => m.triangles)).Take(12)
                .Select(g => g.First().path + " | instances=" + g.Count() + " | triangles each=" + g.First().triangles)));
    }
    private static Summary Summarize(IEnumerable<Sample> values, string name)
    {
        var s = values.ToArray(); if (s.Length == 0) return new Summary { name = name };
        var ms = s.Select(v => v.ms).OrderBy(v => v).ToArray();
        return new Summary { name = name, frames = s.Length, meanFps = 1000f / ms.Average(), meanMs = ms.Average(),
            p95Ms = ms[(int)((ms.Length - 1) * 0.95)], p99Ms = ms[(int)((ms.Length - 1) * 0.99)], maxMs = ms.Last(),
            framesOver33Ms = s.Count(v => v.ms > 33.34f), maxWords = s.Max(v => v.words), maxCreatures = s.Max(v => v.creatures),
            meanDrawCalls = (float)s.Average(v => v.draws), maxTriangles = s.Max(v => v.tris) };
    }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (restoreMouse && previousMouse != null && previousMouse.added) InputSystem.EnableDevice(previousMouse);
        var names = new[] { "Intro", "Intense 1", "Bridge", "Intense 2", "Ending" };
        var report = new Result {
            success = model != null && model.HasWon && errors.Count == 0, won = model != null && model.HasWon,
            sanity = model?.Sanity ?? 0, initialSanity = initialSanity, songDuration = game?.MusicalMap?.Metadata.DurationSeconds ?? 0,
            plannedChartWords = initialized ? new MusicalSpawnSchedule(game.MusicalMap, config.MinimumSpawnInterval, config.TargetLeadTimeSeconds,
                config.MusicalActivity, config.Patterns, 0d, config.Placement, config.Cadence, config.AnalyzedSong.ManualIntensity).Events.Count : 0,
            chartWords = chartCounts.Sum(), creatureWords = creatureWordCounts.Sum(), creatureAppearances = appearanceCounts.Sum(),
            summaries = Enumerable.Range(0, 5).Select(i => Summarize(samples[i], names[i])).Concat(new[] {
                Summarize(samples.SelectMany(s => s).Where(s => s.effectStress), "Effects stress") }).ToArray(),
            errors = errors.ToArray(), note = "Unity Editor Game View, real-time full song, fixed camera, automated word interception at 80% travel, effects requested in both intense sections. Measurements include Editor and probe overhead; not standalone FPS or a human difficulty test."
        };
        File.WriteAllText("Logs/Nivel-1-performance.json", JsonUtility.ToJson(report, true));
        EditorApplication.ExitPlaymode();
    }
    private class Sample { public float ms; public int draws, tris, words, creatures; public bool effectStress; }
    [Serializable] private class Summary {
        public string name; public int frames, framesOver33Ms, maxWords, maxCreatures, maxTriangles;
        public float meanFps, meanMs, p95Ms, p99Ms, maxMs, meanDrawCalls;
    }
    [Serializable] private class Progress { public double songTime; public int section, chartWords, creatureWords, creatures, sanity; public float latestFrameMs; }
    [Serializable] private class Result {
        public bool success, won; public int sanity, initialSanity, plannedChartWords, chartWords, creatureWords, creatureAppearances;
        public double songDuration; public Summary[] summaries; public string[] errors; public string note;
    }
}
