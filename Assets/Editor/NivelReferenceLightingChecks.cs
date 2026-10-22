using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
public static class NivelReferenceLightingChecks
{
    private const string Running = "Nivel.ReferenceLighting.Checks";
    private const string Output = "Logs/ReferenceLighting/";
    private static double ready, deadline, sampleStartedAt;
    private static readonly List<float> frameTimes = new List<float>();
    private static readonly List<string> errors = new List<string>();
    private static int frames;

    static NivelReferenceLightingChecks()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += Changed;
    }
    private static void Poll()
    {
        const string request = "Temp/ReferenceLightingPlay.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        File.Delete(request);
        SessionState.SetString(Running + ".Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(NivelPlayerSetup.ScenePath);
        SessionState.SetBool(Running, true); EditorApplication.EnterPlaymode();
    }
    [MenuItem("Tools/Mentality/Lighting/Verify In Play Mode")]
    public static void RequestPlayCheck()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/ReferenceLightingPlay.request", "run");
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            ready = EditorApplication.timeSinceStartup + 3;
            deadline = ready + 35; sampleStartedAt = 0; frames = 0; frameTimes.Clear(); errors.Clear();
            Application.logMessageReceived += Log;
            EditorApplication.update += Step;
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Application.logMessageReceived -= Log; EditorApplication.update -= Step;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            var previous = SessionState.GetString(Running + ".Previous", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < ready) return;
        var initialScope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
        if (initialScope != null && initialScope.Container != null)
        {
            var initialModel = initialScope.Container.Resolve<RhythmGameModel>();
            var initialClock = initialScope.Container.Resolve<AudioConductorService>();
            if (initialModel.IsPaused) initialScope.Container.Resolve<RhythmGamePresenter>().ResumeSession();
            if (!initialClock.HasStarted && initialModel.IsPlaying && EditorApplication.timeSinceStartup < deadline) return;
        }
        else if (EditorApplication.timeSinceStartup < deadline) return;
        if (sampleStartedAt == 0) sampleStartedAt = EditorApplication.timeSinceStartup;
        // Wait for the musical count-in; bound the live sample so unattended words
        // cannot end the session before a lighting screenshot is taken.
        if (frames++ < 90 && EditorApplication.timeSinceStartup < sampleStartedAt + 2 && EditorApplication.timeSinceStartup < deadline)
        {
            frameTimes.Add(Time.unscaledDeltaTime); return;
        }
        EditorApplication.update -= Step;
        try
        {
            var camera = Camera.main; Require(camera != null, "Missing gameplay camera.");
            var data = camera.GetUniversalAdditionalCameraData();
            Require(data.renderPostProcessing, "Reference grading must stay enabled in Play.");
            var root = GameObject.Find("Reference Lighting"); Require(root != null,"Missing reference lighting group.");
            var volume = root.GetComponent<Volume>();
            Require(volume != null && volume.isGlobal && volume.sharedProfile != null, "Missing global scene profile.");
            Require((data.volumeLayerMask.value & (1 << root.layer)) != 0, "Camera excludes the reference Volume.");
            Require(volume.sharedProfile.TryGet<Tonemapping>(out var tone) && tone.mode.value == TonemappingMode.ACES, "Missing ACES grading.");
            Require(root.GetComponentsInChildren<Light>().Count(l => l.shadows != LightShadows.None) == 1, "Expected one additional shadow caster.");
            Require(root.GetComponentsInChildren<Collider>().Length == 0, "Atmosphere must not block words or gaze.");
            var eyes = Object.FindObjectsByType<Gameplay.Rhythm.Views.TheaterWatchingEye>(FindObjectsSortMode.None);
            Require(eyes.Length == 24 && eyes.All(e => e.AnimationFrameCount == 8), "Eye animation configuration changed.");
            Require(eyes.All(e => e.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_AtmosphereFog") == 1f), "Eyes missing atmosphere material.");
            Require(!ShaderUtil.ShaderHasError(Shader.Find("Mentality/Theater Reference Haze"))
                && !ShaderUtil.ShaderHasError(Shader.Find("Mentality/Theater Watching Eye")), "Atmosphere shader error.");
            var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
            Require(scope != null && scope.Container != null, "Gameplay did not initialize.");
            var game = scope.Container.Resolve<RhythmGamePresenter>();
            var model = scope.Container.Resolve<RhythmGameModel>();
            var clock = scope.Container.Resolve<AudioConductorService>();
            Require(model.IsPlaying && clock.HasStarted, "Rhythm session did not start.");
            game.PauseSession();
            double withHaze = NivelReferenceLightingSetup.Capture("Play-Front");
            var hazeRenderers = root.GetComponentsInChildren<MeshRenderer>();
            double withoutHaze;
            try
            {
                foreach (var r in hazeRenderers) r.enabled = false;
                withoutHaze = NivelReferenceLightingSetup.Capture("Play-NoHaze");
            }
            finally { foreach (var r in hazeRenderers) r.enabled = true; }
            Require(withHaze - withoutHaze > 0.4, "Haze must visibly contribute to the rendered gameplay camera.");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "Gameplay.png"));
            Require(errors.Count == 0, string.Join("\n",errors));
            File.WriteAllText(Output + "PlayChecks.txt", "PASS: Gameplay started; camera grading and volume mask active; ACES profile; 24 animated eyes with eight frames and atmosphere; shaders compile; one new shadow caster; haze has no colliders; haze visibly contributes (mean RGB increase=" + (withHaze-withoutHaze).ToString("F2") + "); no runtime errors.\n"
                + "Editor frame sample: " + frameTimes.Count + "; mean=" + (frameTimes.Average() * 1000f).ToString("F1") + " ms (editor measurement, not a standalone benchmark).\n");
            // Give CaptureScreenshot a rendered frame before leaving Play.
            ready = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update += Finish;
        }
        catch (Exception e)
        {
            File.WriteAllText(Output + "PlayChecks.txt", "FAIL: " + e);
            Debug.LogException(e); EditorApplication.ExitPlaymode();
        }
    }
    private static void Finish()
    {
        if (EditorApplication.timeSinceStartup < ready) return;
        EditorApplication.update -= Finish; EditorApplication.ExitPlaymode();
    }
}
