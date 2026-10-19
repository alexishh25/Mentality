using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VContainer;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;

public class NivelClickProbe : MonoBehaviour, Gameplay.ITouchable
{
    public int Touches;
    public void Touch(Vector3 position) { Touches++; }
}

[InitializeOnLoad]
public static class NivelPlayerPlayChecks
{
    private const string Running = "Nivel.Player.Checks";
    private static int phase;
    private static double next, deadline;
    private static PlayerView player;
    private static PlayerModel model;
    private static Mouse mouse, previousMouse;
    private static Keyboard keyboard, previousKeyboard;
    private static bool mouseEnabled, keyboardEnabled;
    private static Vector3 position, look;
    private static int musicalHits;
    private static NivelClickProbe probe;
    private static readonly List<string> checks = new List<string>();
    private static readonly List<string> errors = new List<string>();
    static NivelPlayerPlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
        // A script reload during Play Mode must not leave an unfinished check running.
        if (SessionState.GetBool(Running, false) && EditorApplication.isPlaying)
            EditorApplication.delayCall += () => Changed(PlayModeStateChange.EnteredPlayMode);
        if (File.Exists("Temp/NivelPlayerChecks.request")) EditorApplication.update += StartWhenReady;
    }
    private static void StartWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || File.Exists("Temp/NivelPlayerSetup.request") || File.Exists("Temp/NivelRhythmSystems.request")
            || File.Exists("Temp/NivelWordLayout.request") || File.Exists("Temp/NivelCreatureLayout.request")) return;
        EditorApplication.update -= StartWhenReady;
        File.Delete("Temp/NivelPlayerChecks.request");
        SessionState.SetString("Nivel.PreviousStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(NivelPlayerSetup.ScenePath);
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 0; next = EditorApplication.timeSinceStartup + 2; deadline = next + 20;
            checks.Clear(); errors.Clear();
            EditorApplication.update += Step;
            var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            EditorWindow.GetWindow(gameView).Focus();
        }
        if (state == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Step;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            string previous = SessionState.GetString("Nivel.PreviousStartScene", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); checks.Add(label); }
    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException();
            switch (phase++)
            {
                case 0:
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerView>();
                    var scope = UnityEngine.Object.FindFirstObjectByType<PlayerLifetimeScope>();
                    model = scope.Container.Resolve<PlayerModel>();
                    Check(player != null && model.GameplayMode == PlayerGameplayMode.Rhythm, "Player scope and fixed rhythm mode ready");
                    Check(Camera.allCameras.Count(c => c.enabled) == 1, "One active rendering camera");
                    Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled) == 1, "One active audio listener");
                    var rhythm = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    if (rhythm != null)
                    {
                        var config = rhythm.Container.Resolve<RhythmGameConfig>();
                        var game = rhythm.Container.Resolve<RhythmGamePresenter>();
                        Check(game.MusicalMap != null && config.AnalyzedSong.LoadValidatedMap() != null, "Analyzed song loaded and validated in Nivel");
                        Check(rhythm.Container.Resolve<RhythmGameModel>().IsPlaying, "Rhythm session starts through its container");
                        Check(ReferenceEquals(scope.Container.Resolve<IPlayerInputService>(), rhythm.Container.Resolve<IPlayerInputService>()), "Rhythm container shares the existing player input");
                        Check(config.WordPrefabs.Length > 0 && config.WordPrefabs.All(p => p != null), "Word prefabs preserved");
                        Check(config.Screamer.Images.Length == 15 && config.Screamer.Clip != null && config.Bullicio.Clip != null
                            && config.Creatures.Prefab != null && config.Creatures.AppearanceClip != null, "Current effects and creature resources preserved");
                        Check(config.AnalyzedSong.ManualIntensity.Sections.Length == 3, "Three current difficulty sections preserved");
                    }
                    else Check(true, "Player-only integration stage");
                    previousMouse = Mouse.current; previousKeyboard = Keyboard.current;
                    mouseEnabled = previousMouse != null && previousMouse.enabled; keyboardEnabled = previousKeyboard != null && previousKeyboard.enabled;
                    if (mouseEnabled) InputSystem.DisableDevice(previousMouse);
                    if (keyboardEnabled) InputSystem.DisableDevice(previousKeyboard);
                    mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
                    position = player.transform.position; look = player.CameraTransform.forward;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space, Key.J));
                    InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(80, 0) });
                    break;
                case 1:
                    Check(Vector3.Distance(position, player.transform.position) < 0.001f, "W and jump keep the player fixed");
                    Check(model.CurrentPerspective == CameraPerspective.FirstPerson, "Camera switch key keeps first person");
                    Check(Vector3.Angle(look, player.CameraTransform.forward) > 0.5f, "Real mouse input rotates Cinemachine");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    var activeRhythm = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    if (activeRhythm != null) activeRhythm.Container.Resolve<RhythmGamePresenter>().PauseSession();
                    var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    target.name = "Temporary click verification";
                    int mask = new SerializedObject(player).FindProperty("interactableLayer").intValue;
                    target.layer = Enumerable.Range(0, 32).First(i => (mask & (1 << i)) != 0);
                    target.transform.position = player.CameraTransform.position + player.CameraTransform.forward * 1.5f;
                    target.transform.localScale = Vector3.one * 0.5f;
                    probe = target.AddComponent<NivelClickProbe>();
                    Physics.SyncTransforms();
                    Check(player.PerformFirstPersonRaycast(out var hit, false) && hit.collider.gameObject == target, "Aim raycast reaches an interactable target");
                    InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    break;
                case 2:
                    Check(probe.Touches > 0, "Real left click reaches the interaction interface");
                    var pausedRhythm = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    if (pausedRhythm != null) pausedRhythm.Container.Resolve<RhythmGamePresenter>().ResumeSession();
                    UnityEngine.Object.Destroy(probe.gameObject);
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    break;
                case 3:
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Nivel-1-player-preview.png"));
                    break;
                case 4:
                    var rhythmScope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    if (rhythmScope != null)
                    {
                        var clock = rhythmScope.Container.Resolve<AudioConductorService>();
                        if (!clock.HasStarted) { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                        var config = rhythmScope.Container.Resolve<RhythmGameConfig>();
                        var game = rhythmScope.Container.Resolve<RhythmGamePresenter>();
                        Check(clock.CurrentSongTime >= 0d && rhythmScope.GetComponent<AudioSource>().isPlaying
                            && rhythmScope.GetComponent<AudioSource>().clip == config.SessionMusicClip, "Dedicated source plays the analyzed song after count-in");
                        var chart = new MusicalSpawnSchedule(game.MusicalMap, config.MinimumSpawnInterval, config.TargetLeadTimeSeconds,
                            config.MusicalActivity, config.Patterns, 0d, config.Placement, config.Cadence, config.AnalyzedSong.ManualIntensity);
                        var item = chart.Events.First(e => e.SpawnTime.Value >= 10d);
                        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                        typeof(AudioConductorService).GetField("_dspSongTime", flags).SetValue(clock, AudioSettings.dspTime - item.SpawnTime.Value - 0.005d);
                        typeof(AudioConductorService).GetField("_dspTimelineTime", flags).SetValue(clock, AudioSettings.dspTime - 1d);
                        clock.UpdateTime();
                        var factory = rhythmScope.Container.Resolve<TargetFactory>();
                        factory.ClearTargets();
                        if (UnityEngine.Object.FindFirstObjectByType<CreaturePlacementLayout>() != null)
                            CheckCreatureGrounds(rhythmScope, chart);
                        var layout = UnityEngine.Object.FindFirstObjectByType<WordLaunchLayout>();
                        if (layout != null)
                        {
                            Check(layout.Directions.Length == 11, "All eleven scene-specific launch lanes configured");
                            int lane = 0;
                            foreach (var direction in layout.Directions)
                            {
                                float angle = -75f + 15f * lane++;
                                double now = clock.CurrentSongTime;
                                var timing = new TargetTiming(now, now + config.TargetLeadTimeSeconds);
                                var word = factory.CreateAtAngle(angle, timing);
                                var expected = layout.PositionFor(angle, player.CameraTransform.position, config.SpawnDistance);
                                Check(word != null && Vector3.Distance(word.Position, expected) < 0.05f
                                    && Math.Abs(Vector3.Distance(word.Position, player.CameraTransform.position) - config.SpawnDistance) < 0.05f
                                    && ReferenceEquals(word.Timing, timing), "Scene lane " + angle + " preserves travel distance and musical timing");
                                factory.ClearTargets();
                            }
                        }
                        typeof(AudioConductorService).GetField("_dspSongTime", flags).SetValue(clock, AudioSettings.dspTime - item.SpawnTime.Value - 0.005d);
                        typeof(AudioConductorService).GetField("_dspTimelineTime", flags).SetValue(clock, AudioSettings.dspTime - 1d);
                        clock.UpdateTime();
                        rhythmScope.Container.Resolve<RhythmSpawnerPresenter>().CheckAndSpawnTargets();
                        Check(factory.ActiveTargetCount > 0, "Real spawner consumes the musical chart in Nivel");
                        game.StopSession();
                        Check(!clock.IsPlaying && factory.ActiveTargetCount == 0, "Stopping clears music and targets");
                        game.RestartSession();
                        Check(rhythmScope.Container.Resolve<RhythmGameModel>().Sanity == 100 && clock.IsPlaying, "Restart rebuilds a fresh musical session");
                        next = EditorApplication.timeSinceStartup + 0.1;
                        return;
                    }
                    Check(File.Exists("Logs/Nivel-1-player-preview.png"), "Gameplay view capture saved");
                    Finish(); return;
                case 5:
                    var readyScope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    if (!readyScope.Container.Resolve<AudioConductorService>().HasTimelineStarted)
                    { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                    var readyFactory = readyScope.Container.Resolve<TargetFactory>();
                    readyFactory.ClearTargets();
                    musicalHits = readyScope.Container.Resolve<RhythmGameModel>().Hits;
                    readyFactory.Create(readyScope.Container.Resolve<RhythmGameConfig>().WordPrefabs[0],
                        player.CameraTransform.position + player.CameraTransform.forward * 2f, null, WordTargetKind.Normal, false);
                    Physics.SyncTransforms();
                    InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    next = EditorApplication.timeSinceStartup + 0.1;
                    return;
                case 6:
                    var completedScope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    Check(completedScope.Container.Resolve<RhythmGameModel>().Hits == musicalHits + 1,
                        "Real click on a gameplay word registers exactly one hit; actual=" + completedScope.Container.Resolve<RhythmGameModel>().Hits
                            + "; expected=" + (musicalHits + 1) + "; ray=" + (player.PerformFirstPersonRaycast(out var wordHit, false) ? wordHit.collider.name : "none"));
                    completedScope.Container.Resolve<RhythmGamePresenter>().StopSession();
                    Check(File.Exists("Logs/Nivel-1-player-preview.png"), "Gameplay view capture saved");
                    Finish(); return;
            }
            next = EditorApplication.timeSinceStartup + 0.4;
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Step;
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouseEnabled && previousMouse != null && previousMouse.added) InputSystem.EnableDevice(previousMouse);
        if (keyboardEnabled && previousKeyboard != null && previousKeyboard.added) InputSystem.EnableDevice(previousKeyboard);
        File.WriteAllText("Logs/Nivel-1-player-playchecks.json", JsonUtility.ToJson(new Result { success = errors.Count == 0, checks = checks.ToArray(), errors = errors.ToArray() }, true));
        EditorApplication.ExitPlaymode();
    }
    [Serializable] private class Result { public bool success; public string[] checks, errors; }

    private static void CheckCreatureGrounds(RhythmLifetimeScope scope, MusicalSpawnSchedule chart)
    {
        var layout = UnityEngine.Object.FindFirstObjectByType<CreaturePlacementLayout>();
        var config = scope.Container.Resolve<RhythmGameConfig>();
        var game = scope.Container.Resolve<RhythmGamePresenter>();
        var clock = scope.Container.Resolve<AudioConductorService>();
        var factory = scope.Container.Resolve<TargetFactory>();
        var spawner = scope.Container.Resolve<RhythmSpawnerPresenter>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        Check(layout.Points.Length == 5 && layout.Points.Distinct().Count() == 5, "Five distinct creature ground markers configured");
        Check(layout.Points.All(p => Mathf.Abs(p.position.y - 0.27f) < 0.03f), "Creature feet rest on the actual floor surface");
        var budget = new CreatureShotBudget(spawner.CreatureSchedule, chart, (config.Creatures.Distance + 2d) / config.TravelSpeed);
        for (int section = 0; section < 3; section++)
        {
            game.RestartSession();
            var item = spawner.CreatureSchedule.Events.First(e => e.Section == section && e.Shots.Any(budget.Allows));
            Action<double> seek = time => {
                typeof(AudioConductorService).GetField("_dspSongTime", flags).SetValue(clock, AudioSettings.dspTime - time - 0.005d);
                typeof(AudioConductorService).GetField("_dspTimelineTime", flags).SetValue(clock, AudioSettings.dspTime - 1d);
                clock.UpdateTime();
            };
            seek(item.Time);
            spawner.CheckAndSpawnTargets();
            var creatures = UnityEngine.Object.FindObjectsByType<CreatureEmergenceView>(FindObjectsSortMode.None);
            Check(creatures.Length == 1 && Vector3.Distance(creatures[0].GroundPosition, layout.GroundFor(item.Angle)) < 0.01f,
                "Section " + section + " spawns on its scene ground without Floor-name dependency");
            var creature = creatures[0]; var whistle = creature.GetComponent<AudioSource>();
            Check(whistle != null && whistle.isPlaying && whistle.clip == config.Creatures.AppearanceClip,
                "Section " + section + " appearance plays the whistle");
            foreach (var word in UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None)) word.Touch(player.transform.position);
            creature.AdvanceAt(0.9d);
            Check(creature.HasEmerged && !creature.IsAttacking, "Standing emergence precedes the attack");
            creature.AdvanceAt(1.05d);
            Check(creature.IsAttacking, "Open-mouth animation anticipates the volley");
            bool fired = false;
            foreach (double shot in item.Shots)
            {
                seek(shot);
                creature.AdvanceAt(shot - item.Time + 0.005d);
                var words = UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None).Where(w => !w.IsResolved).ToArray();
                if (words.Length > 0)
                {
                    Check(words.All(w => w.Kind != WordTargetKind.Normal && Vector3.Distance(w.Position, creature.MouthPosition) < 0.05f),
                        "Creature volley launches false words from its mouth");
                    fired = true;
                    foreach (var word in words) word.Touch(player.transform.position);
                }
            }
            Check(fired, "Section " + section + " keeps an admitted musical volley");
            game.PauseSession(); creature.SendMessage("Update");
            Check(!whistle.isPlaying, "Pausing also pauses the creature whistle");
            game.RestartSession();
            Check(creature.HasFinished && !creature.gameObject.activeSelf && !whistle.isPlaying,
                "Restart removes the creature and cancels pending shots and whistle");
        }
        game.RestartSession();
    }
}
