using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using Mentality.UI.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using VContainer;
using Object = UnityEngine.Object;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;
using Cursor = UnityEngine.Cursor;

[InitializeOnLoad]
public static class NivelHudPlayChecks
{
    private const string Running = "Nivel.Hud.Checks";
    private static readonly List<string> checks = new List<string>(), errors = new List<string>();
    private static int phase, hits;
    private static double next, deadline;
    private static Mouse mouse, oldMouse;
    private static Keyboard keyboard, oldKeyboard;
    private static bool restoreMouse, restoreKeyboard;
    private static RhythmLifetimeScope scope;
    private static RhythmGameModel model;
    private static RhythmGamePresenter game;
    private static AudioConductorService clock;
    private static TargetFactory factory;
    private static RhythmGameConfig config;
    private static FalseWordEffectService effects;
    private static RhythmHudView hud;
    private static PlayerView player;
    private static VisualElement root;
    private static TargetView pausedWord;
    private static CreatureEmergenceView pausedCreature;
    private static double pausedTime;
    private static Vector3 pausedPosition, pausedLook;
    private static int pausedSanity;
    private static Vector2 buttonPoint;
    static NivelHudPlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (File.Exists("Temp/NivelHudChecks.request") || File.Exists("Temp/NivelLifecycleChecks.request")) EditorApplication.update += StartWhenReady;
    }
    private static void StartWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || File.Exists("Temp/NivelHud.request")) return;
        EditorApplication.update -= StartWhenReady;
        SessionState.SetBool("Nivel.Hud.FullCycle", File.Exists("Temp/NivelLifecycleChecks.request"));
        if (File.Exists("Temp/NivelLifecycleChecks.request")) File.Delete("Temp/NivelLifecycleChecks.request");
        File.Delete("Temp/NivelHudChecks.request");
        SessionState.SetString("Nivel.Hud.PreviousStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(NivelPlayerSetup.ScenePath);
        SessionState.SetBool(Running, true); EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 0; checks.Clear(); errors.Clear(); next = EditorApplication.timeSinceStartup + 1;
            deadline = next + (SessionState.GetBool("Nivel.Hud.FullCycle", false) ? 90 : 35);
            Application.logMessageReceived += Log;
            EditorApplication.update += Step;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        if (state == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Step; Application.logMessageReceived -= Log; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            string previous = SessionState.GetString("Nivel.Hud.PreviousStart", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void Log(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack); }
    private static void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); checks.Add(label); }
    private static GameObject Find(string name) => Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(g => g.name == name);
    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("HUD test timeout");
            switch (phase++)
            {
                case 0:
                    scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    model = scope.Container.Resolve<RhythmGameModel>(); game = scope.Container.Resolve<RhythmGamePresenter>();
                    clock = scope.Container.Resolve<AudioConductorService>(); factory = scope.Container.Resolve<TargetFactory>();
                    config = scope.Container.Resolve<RhythmGameConfig>(); effects = scope.Container.Resolve<FalseWordEffectService>();
                    hud = Object.FindFirstObjectByType<RhythmHudView>(); player = Object.FindFirstObjectByType<PlayerView>();
                    Check(hud != null && hud.TryBind(), "Current UI Toolkit HUD binds in Nivel");
                    root = hud.GetComponent<UIDocument>().rootVisualElement;
                    oldMouse = Mouse.current; oldKeyboard = Keyboard.current;
                    restoreMouse = oldMouse != null && oldMouse.enabled; restoreKeyboard = oldKeyboard != null && oldKeyboard.enabled;
                    if (restoreMouse) InputSystem.DisableDevice(oldMouse);
                    if (restoreKeyboard) InputSystem.DisableDevice(oldKeyboard);
                    mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
                    if (model.IsPaused) InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    break;
                case 1:
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    break;
                case 2:
                    if (!clock.HasStarted) { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                    Check(root.Q<Label>("sanityValue").text == "100" && Mathf.Approximately(root.Q<SanityWedgeBar>("sanityTrack").value, 1f), "HUD shows full initial sanity");
                    Check(root.Q<VisualElement>("Portrait").ClassListContains("brain--stable"), "Portrait shows the initial sanity state");
                    Check(!Find("Rhythm Aim").activeSelf && root.Q<VisualElement>("reticle").resolvedStyle.display != DisplayStyle.None, "One active HUD reticle replaces the provisional crosshair");
                    Check(root.pickingMode == PickingMode.Ignore && root.Q<VisualElement>("reticle").pickingMode == PickingMode.Ignore, "HUD and reticle do not intercept pointer picking");
                    factory.ClearTargets();
                    // Isolate UI checks from chart collisions; restart below restores the complete map.
                    scope.Container.Resolve<RhythmSpawnerPresenter>().ResetBeatTracking();
                    hits = model.Hits;
                    factory.Create(config.WordPrefabs[0], player.CameraTransform.position + player.CameraTransform.forward * 2f, null, WordTargetKind.Normal, false);
                    Physics.SyncTransforms();
                    Check(player.PerformFirstPersonRaycast(out var hit, false) && hit.collider.GetComponent<TargetView>() != null, "Aim reaches a gameplay word with HUD visible");
                    InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    break;
                case 3:
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    Check(model.Hits == hits + 1, "Real click scores one hit through the HUD");
                    Check(root.Q<Label>("comboLabel").text.Contains("COMBO") && root.Q<Label>("comboLabel").resolvedStyle.display != DisplayStyle.None, "Successful click displays the combo pop-up");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Nivel-1-hud-combo.png"));
                    break;
                case 4:
                    factory.ClearTargets();
                    factory.Create(config.WordPrefabs[0], player.CameraTransform.position, null, WordTargetKind.Normal, false);
                    break;
                case 5:
                    int expected = 100 - RhythmGameModel.WordCollisionDamage;
                    Check(model.Sanity == expected && root.Q<Label>("sanityValue").text == expected.ToString()
                        && Mathf.Abs(root.Q<SanityWedgeBar>("sanityTrack").value - expected / 100f) < 0.001f, "Real collision updates sanity text and bar with current damage");
                    Check(root.Q<Label>("comboLabel").text.Contains("RACHA PERDIDA"), "Collision displays broken-combo feedback");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Nivel-1-hud-sanity.png"));
                    break;
                case 6:
                    effects.Request(WordTargetKind.Screamer); effects.Request(WordTargetKind.Bullicio); effects.Request(WordTargetKind.Glitch);
                    Check(Find("Screamer overlay").activeSelf && Find("Screamer audio").GetComponent<AudioSource>().isPlaying, "Screamer image and sound activate over the new scene");
                    Check(Find("Glitch overlay").activeSelf && Find("Bullicio audio").GetComponent<AudioSource>().isPlaying, "Glitch and bullicio activate in the new scene");
                    foreach (string name in new[] { "Screamer overlay", "Glitch overlay" })
                        Check(Find(name).GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget), name + " does not block gameplay raycasts");
                    break;
                case 7:
                    Check(!Find("Screamer overlay").activeSelf && Find("Screamer audio").GetComponent<AudioSource>().isPlaying, "Screamer sound continues after the brief image");
                    Check(scope.GetComponent<AudioSource>().isPlaying, "Music continues independently of effects");
                    game.RestartSession();
                    break;
                case 8:
                    Check(root.Q<Label>("sanityValue").text == "100" && root.Q<Label>("comboLabel").resolvedStyle.display == DisplayStyle.None, "Restart resets sanity and clears combo feedback");
                    Check(!Find("Screamer overlay").activeSelf && !Find("Glitch overlay").activeSelf
                        && !Find("Screamer audio").GetComponent<AudioSource>().isPlaying
                        && !Find("Bullicio audio").GetComponent<AudioSource>().isPlaying, "Restart cleans all effect overlays and sounds");
                    Check(Object.FindObjectsByType<RhythmHudView>(FindObjectsSortMode.None).Length == 1, "One HUD instance after restart");
                    Check(File.Exists("Logs/Nivel-1-hud-combo.png") && File.Exists("Logs/Nivel-1-hud-sanity.png"), "Gameplay HUD captures saved");
                    if (SessionState.GetBool("Nivel.Hud.FullCycle", false)) break;
                    Finish(); return;
                case 9:
                    if (!clock.HasStarted) { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                    SpawnPauseFixture();
                    effects.Request(WordTargetKind.Screamer); effects.Request(WordTargetKind.Glitch); effects.Request(WordTargetKind.Bullicio);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
                    break;
                case 10:
                    Check(model.IsPaused && clock.IsPaused && !scope.GetComponent<AudioSource>().isPlaying, "P pauses the session and music");
                    Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Pause releases the cursor");
                    Check(!Find("Screamer audio").GetComponent<AudioSource>().isPlaying && !Find("Bullicio audio").GetComponent<AudioSource>().isPlaying
                        && !pausedCreature.GetComponent<AudioSource>().isPlaying, "Pause silences screamer, bullicio and creature whistle");
                    pausedTime = clock.CurrentSongTime; pausedPosition = pausedWord.Position; pausedLook = player.CameraTransform.forward; pausedSanity = model.Sanity;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(80, 0) });
                    break;
                case 11:
                    Check(Math.Abs(clock.CurrentSongTime - pausedTime) < 0.001 && Vector3.Distance(pausedWord.Position, pausedPosition) < 0.001f
                        && model.Sanity == pausedSanity, "Paused time, word position and sanity remain frozen");
                    Check(Vector3.Angle(pausedLook, player.CameraTransform.forward) < 0.1f, "Mouse movement does not rotate the paused camera");
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
                    break;
                case 12:
                    Check(!model.IsPaused && !clock.IsPaused && clock.CurrentSongTime > pausedTime, "P resumes the same musical timeline");
                    Check(Cursor.lockState == CursorLockMode.Locked && scope.GetComponent<AudioSource>().isPlaying, "Resume restores cursor capture and music");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    break;
                case 13:
                    int collisions = Mathf.CeilToInt(model.Sanity / (float)RhythmGameModel.WordCollisionDamage);
                    for (int i = 0; i < collisions; i++)
                        factory.Create(config.WordPrefabs[0], player.CameraTransform.position, null, WordTargetKind.Normal, false);
                    break;
                case 14:
                    Check(model.IsGameOver && model.Sanity == 0, "Actual word collisions exhaust sanity");
                    Check(!model.IsPlaying && !clock.IsPlaying && !scope.GetComponent<AudioSource>().isPlaying, "Defeat stops the song and session");
                    Check(ScreenRoot("gameOverScreen") != null && ScreenRoot("victoryScreen") == null, "Only Game Over is shown after defeat");
                    Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Defeat releases the pointer for the restart button");
                    CheckWorldCleared();
                    Check(Find("Game Over audio")?.GetComponent<AudioSource>().isPlaying == true, "Defeat plays the configured Game Over music");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Nivel-1-game-over.png"));
                    PrepareButton("gameOverScreen");
                    break;
                case 15:
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPoint }.WithButton(MouseButton.Left));
                    break;
                case 16:
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPoint });
                    break;
                case 17:
                    Check(model.IsPlaying && !model.IsGameOver && model.Sanity == 100 && ScreenRoot("gameOverScreen") == null,
                        "Real click on Game Over restart restores a fresh level");
                    Check(!Find("Game Over audio").GetComponent<AudioSource>().isPlaying, "Restart stops Game Over music");
                    break;
                case 18:
                    if (!clock.HasStarted) { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                    PrepareSongEnding(false);
                    next = EditorApplication.timeSinceStartup + 0.5; return;
                case 19:
                    Check(model.HasWon && !model.IsGameOver && !model.IsPlaying && model.Sanity > 0, "Actual song ending awards victory with sanity remaining");
                    Check(clock.HasCompletedSong && !clock.IsPlaying && !scope.GetComponent<AudioSource>().isPlaying, "Victory stops the completed song");
                    Check(ScreenRoot("victoryScreen") != null && ScreenRoot("gameOverScreen") == null, "Only WIN GAME is shown after victory");
                    CheckWorldCleared();
                    Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Victory exposes the restart pointer");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Nivel-1-victory.png"));
                    PrepareButton("victoryScreen");
                    break;
                case 20:
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPoint }.WithButton(MouseButton.Left));
                    break;
                case 21:
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPoint });
                    break;
                case 22:
                    Check(model.IsPlaying && !model.HasWon && !model.IsGameOver && model.Sanity == 100 && ScreenRoot("victoryScreen") == null,
                        "Real click on victory restart restores the level");
                    break;
                case 23:
                    if (!clock.HasStarted) { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                    SpawnPauseFixture();
                    effects.Request(WordTargetKind.Screamer); effects.Request(WordTargetKind.Glitch); effects.Request(WordTargetKind.Bullicio);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
                    break;
                case 24:
                    Check(model.IsPaused, "Restart scenario begins paused with a creature and effects pending");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                    break;
                case 25:
                    Check(model.IsPlaying && !model.IsPaused && model.Sanity == 100 && model.Hits == 0 && model.Combo == 0,
                        "R from pause resets session counters and sanity");
                    CheckWorldCleared();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    break;
                case 26:
                    if (!clock.HasStarted) { phase--; next = EditorApplication.timeSinceStartup + 0.1; return; }
                    PrepareSongEnding(true);
                    next = EditorApplication.timeSinceStartup + 0.5; return;
                case 27:
                    Check(model.IsGameOver && !model.HasWon && model.Sanity == 0 && ScreenRoot("gameOverScreen") != null,
                        "Fatal arrival at the song ending takes priority over victory");
                    CheckWorldCleared();
                    Check(Object.FindObjectsByType<RhythmHudView>(FindObjectsSortMode.None).Length == 1
                        && Object.FindObjectsByType<RhythmLifetimeScope>(FindObjectsSortMode.None).Length == 1, "Repeated restarts preserve one HUD and one rhythm scope");
                    Finish(); return;
            }
            next = EditorApplication.timeSinceStartup + 0.3;
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Step; Application.logMessageReceived -= Log;
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (restoreMouse && oldMouse != null && oldMouse.added) InputSystem.EnableDevice(oldMouse);
        if (restoreKeyboard && oldKeyboard != null && oldKeyboard.added) InputSystem.EnableDevice(oldKeyboard);
        string report = SessionState.GetBool("Nivel.Hud.FullCycle", false) ? "Logs/Nivel-1-lifecycle-playchecks.json" : "Logs/Nivel-1-hud-playchecks.json";
        File.WriteAllText(report, JsonUtility.ToJson(new Result { success = errors.Count == 0, checks = checks.ToArray(), errors = errors.ToArray() }, true));
        EditorApplication.ExitPlaymode();
    }
    [Serializable] private class Result { public bool success; public string[] checks, errors; }

    private static VisualElement ScreenRoot(string name) => Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
        .Select(d => d.rootVisualElement.Q<VisualElement>(name)).FirstOrDefault(e => e != null && e.resolvedStyle.display != DisplayStyle.None);
    private static void PrepareButton(string screen)
    {
        var button = ScreenRoot(screen).Q<Button>("restartBtn");
        Check(button != null && button.enabledInHierarchy && button.worldBound.width > 0, screen + " has a live restart button");
        var panelSize = button.panel.visualTree.worldBound.size;
        buttonPoint = new Vector2(button.worldBound.center.x * Screen.width / panelSize.x,
            Screen.height - button.worldBound.center.y * Screen.height / panelSize.y);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPoint });
    }
    private static void Seek(double time)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(AudioConductorService).GetField("_dspSongTime", flags).SetValue(clock, AudioSettings.dspTime - time - 0.005d);
        typeof(AudioConductorService).GetField("_dspTimelineTime", flags).SetValue(clock, AudioSettings.dspTime - 1d);
        clock.UpdateTime();
    }
    private static void SpawnPauseFixture()
    {
        factory.ClearTargets();
        var spawner = scope.Container.Resolve<RhythmSpawnerPresenter>();
        spawner.ResetBeatTracking(game.MusicalMap, config.MinimumSpawnInterval, config.TargetLeadTimeSeconds,
            config.MusicalActivity, config.Patterns, 0d, config.Placement, config.Cadence, config.AnalyzedSong.ManualIntensity);
        var item = spawner.CreatureSchedule.Events[0];
        Seek(item.Time); spawner.CheckAndSpawnTargets();
        pausedCreature = Object.FindFirstObjectByType<CreatureEmergenceView>();
        Check(pausedCreature != null, "Real creature is active before lifecycle transition");
        foreach (var word in Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None)) word.Touch(player.transform.position);
        pausedWord = factory.Create(config.WordPrefabs[0], player.CameraTransform.position + player.CameraTransform.forward * 6f,
            new TargetTiming(clock.CurrentSongTime, clock.CurrentSongTime + 3d), WordTargetKind.Normal, true);
    }
    private static void CheckWorldCleared()
    {
        Check(factory.ActiveTargetCount == 0 && Object.FindObjectsByType<CreatureEmergenceView>(FindObjectsSortMode.None).Length == 0
            && Object.FindObjectsByType<SpawnRingView>(FindObjectsSortMode.None).Length == 0, "Transition clears words, creatures and launch rings");
        Check(!Find("Screamer overlay").activeSelf && !Find("Glitch overlay").activeSelf
            && !Find("Screamer audio").GetComponent<AudioSource>().isPlaying
            && !Find("Bullicio audio").GetComponent<AudioSource>().isPlaying, "Transition clears effect visuals and sounds");
        Check(scope.Container.Resolve<BullicioSessionService>().ProbabilityBonus == 0, "Transition clears bullicio spawn bonus");
    }
    private static void PrepareSongEnding(bool fatal)
    {
        factory.ClearTargets();
        scope.Container.Resolve<RhythmSpawnerPresenter>().ResetBeatTracking();
        if (fatal) while (model.Sanity > RhythmGameModel.WordCollisionDamage) model.RegisterWordCollision();
        double duration = (double)config.SessionMusicClip.samples / config.SessionMusicClip.frequency;
        scope.GetComponent<AudioSource>().timeSamples = config.SessionMusicClip.samples - (int)(0.25d * config.SessionMusicClip.frequency);
        Seek(duration - 0.25d);
        effects.Request(WordTargetKind.Screamer); effects.Request(WordTargetKind.Glitch); effects.Request(WordTargetKind.Bullicio);
        factory.Create(config.WordPrefabs[0], player.CameraTransform.position + player.CameraTransform.forward * 6f,
            new TargetTiming(clock.CurrentSongTime, fatal ? duration : duration + 2d), WordTargetKind.Normal, true);
    }
}
