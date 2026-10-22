using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class NivelImpactBlurPlayChecks
{
    private const string Running = "Nivel.ImpactBlur.Checks";
    private static int phase;
    private static double next, deadline;
    private static RhythmGamePresenter game;
    private static RhythmGameModel model;
    private static AudioConductorService clock;
    private static TargetFactory factory;
    private static WordImpactBlurPresenter blur;
    private static RhythmGameConfig config;
    private static Camera camera;
    private static UniversalAdditionalCameraData cameraData;
    private static bool originalPost;
    private static LayerMask originalMask;
    private static CameraOverrideOption originalDepth;
    private static TargetView word;
    private static int sanity;
    private static float pausedStrength;
    private static readonly List<string> checks = new List<string>(), errors = new List<string>();
    static NivelImpactBlurPlayChecks()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += Changed;
    }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/NivelImpactBlurPlay.request")) return;
        File.Delete("Temp/NivelImpactBlurPlay.request");
        SessionState.SetString(Running + ".Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(NivelPlayerSetup.ScenePath);
        SessionState.SetBool(Running, true); EditorApplication.EnterPlaymode();
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 0; checks.Clear(); errors.Clear(); next = EditorApplication.timeSinceStartup + 1d; deadline = next + 25d;
            Application.logMessageReceived += Log; EditorApplication.update += Step;
        }
        if (state == PlayModeStateChange.ExitingPlayMode) { Application.logMessageReceived -= Log; EditorApplication.update -= Step; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            string previous = SessionState.GetString(Running + ".Previous", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void Log(string message, string stack, LogType type)
    { if (type == LogType.Exception || type == LogType.Error) errors.Add(message + "\n" + stack); }
    private static void Check(bool result, string label)
    { if (!result) throw new InvalidOperationException(label); checks.Add(label); }
    private static TargetView Create(WordTargetKind kind, double delay)
        => factory.Create(config.WordPrefabs[0], camera.transform.position + camera.transform.forward * 4f,
            new TargetTiming(clock.CurrentSongTime, clock.CurrentSongTime + delay), kind, false);
    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Impact blur Play check timed out.");
            switch (phase)
            {
                case 0:
                    var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    game = scope.Container.Resolve<RhythmGamePresenter>(); model = scope.Container.Resolve<RhythmGameModel>();
                    clock = scope.Container.Resolve<AudioConductorService>(); factory = scope.Container.Resolve<TargetFactory>();
                    blur = scope.Container.Resolve<WordImpactBlurPresenter>(); config = scope.Container.Resolve<RhythmGameConfig>();
                    camera = Camera.main; cameraData = camera.GetUniversalAdditionalCameraData();
                    originalPost = cameraData.renderPostProcessing; originalMask = cameraData.volumeLayerMask; originalDepth = cameraData.requiresDepthOption;
                    Check(config.ImpactBlur != null && config.ImpactBlur.Enabled && !blur.IsActive, "Impact blur starts inactive.");
                    if (model.IsPaused) game.ResumeSession(); phase++; break;
                case 1:
                    if (!clock.HasStarted) { next = EditorApplication.timeSinceStartup + 0.1d; return; }
                    factory.ClearTargets(); sanity = model.Sanity;
                    var intercepted = Create(WordTargetKind.Normal, 1d); intercepted.Touch(camera.transform.position);
                    Check(model.Sanity == sanity && !blur.IsActive, "Intercepting a word does not blur the camera.");
                    word = Create(WordTargetKind.Normal, 0.06d); phase++; next = EditorApplication.timeSinceStartup + 0.10d; return;
                case 2:
                    word.RefreshMusicalState();
                    Check(word.IsResolved && model.Sanity == sanity - RhythmGameModel.WordCollisionDamage && blur.IsActive,
                        "A real normal-word impact applies damage and activates camera blur.");
                    Check(cameraData.renderPostProcessing && Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Any(v => v.name == "Word impact blur" && v.enabled),
                        "Player camera enables the runtime URP blur volume.");
                    game.PauseSession(); pausedStrength = blur.Strength;
                    cameraData.renderPostProcessing = false; var before = Capture("Before");
                    cameraData.renderPostProcessing = true; var after = Capture("During");
                    double difference = before.Zip(after, (a, b) => (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)) / 3d).Average();
                    Check(difference > 0.02d, "Rendered camera image changes when impact blur is enabled; mean RGB difference=" + difference.ToString("F3"));
                    phase++; next = EditorApplication.timeSinceStartup + 0.35d; return;
                case 3:
                    blur.PostTick();
                    Check(blur.IsActive && Mathf.Abs(blur.Strength - pausedStrength) < 0.00001f, "Pause freezes the brief blur envelope.");
                    game.ResumeSession(); phase++; next = EditorApplication.timeSinceStartup + 0.45d; return;
                case 4:
                    blur.PostTick();
                    Check(!blur.IsActive && cameraData.renderPostProcessing == originalPost && cameraData.volumeLayerMask == originalMask && cameraData.requiresDepthOption == originalDepth,
                        "Blur expires and restores the camera's original postprocessing, volume mask and depth settings.");
                    factory.ClearTargets(); sanity = model.Sanity;
                    word = Create(WordTargetKind.Glitch, 0.06d); phase++; next = EditorApplication.timeSinceStartup + 0.10d; return;
                case 5:
                    word.RefreshMusicalState();
                    Check(word.IsResolved && model.Sanity == sanity - RhythmGameModel.WordCollisionDamage && blur.IsActive,
                        "A false/creature-word impact also activates blur alongside its existing effect.");
                    model.RegisterWordCollision(); blur.PostTick();
                    Check(blur.Strength <= config.ImpactBlur.Radius + 0.00001f && Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Count(v => v.name == "Word impact blur") == 1,
                        "Repeated impacts refresh a bounded blur without stacking volumes.");
                    game.RestartSession();
                    Check(!blur.IsActive && cameraData.renderPostProcessing == originalPost, "Restart immediately removes blur.");
                    game.PauseSession(); sanity = model.Sanity; model.RegisterWordCollision();
                    Check(model.Sanity == sanity && !blur.IsActive, "Paused collisions cannot trigger damage or blur.");
                    game.ResumeSession();
                    while (model.IsPlaying) model.RegisterWordCollision(); game.HandleGameOver(); blur.PostTick();
                    Check(!blur.IsActive && cameraData.renderPostProcessing == originalPost, "Game over clears blur for the result screen.");
                    Check(errors.Count == 0, "No runtime exceptions or rendering errors.");
                    Finish(); return;
            }
            next = EditorApplication.timeSinceStartup + 0.1d;
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    private static Color32[] Capture(string suffix)
    {
        var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            File.WriteAllBytes("Logs/NivelImpactBlur-" + suffix + ".png", pixels.EncodeToPNG()); return pixels.GetPixels32();
        }
        finally { camera.targetTexture = previousTarget; RenderTexture.active = previousActive; Object.DestroyImmediate(pixels); Object.DestroyImmediate(target); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Step;
        File.WriteAllText("Logs/NivelImpactBlurPlay.txt", (errors.Count == 0 ? "PASS\n" : "FAIL\n") + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
        EditorApplication.ExitPlaymode();
    }
}
