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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class NivelAudienceLaunchPlayChecks
{
    private const string Running = "Nivel.Audience.LaunchChecks";
    private static int phase;
    private static double next, deadline;
    private static RhythmGamePresenter game;
    private static RhythmGameModel model;
    private static AudioConductorService clock;
    private static TargetFactory factory;
    private static TargetView word;
    private static Vector3 pausedPosition, pausedScale;
    private static double pausedTime;
    private static float initialScale;
    private static TargetTiming timing;
    private static SpawnRingView cue;
    private static float pausedCueWidth;
    private static Transform destination;
    private static float arrivalDistance;
    private static GameObject sceneryProbe;
    private static readonly List<string> checks = new List<string>(), errors = new List<string>();
    private static readonly HashSet<int> observed = new HashSet<int>();
    private static readonly HashSet<string> observedRows = new HashSet<string>(), observedMembers = new HashSet<string>();

    static NivelAudienceLaunchPlayChecks()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += Changed;
    }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
            !File.Exists("Temp/NivelAudienceLaunchPlay.request")) return;
        File.Delete("Temp/NivelAudienceLaunchPlay.request");
        var routes = Object.FindFirstObjectByType<AudienceWordRouteLayout>();
        if (routes == null || routes.ClearCount == 0)
        {
            File.WriteAllText("Logs/NivelAudienceLaunchPlay.txt", "FAIL: validate usable audience routes before running Play checks.");
            return;
        }
        SessionState.SetString(Running + ".StartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/All-ObjE/Scenes/Nivel-1_Rhythm.unity");
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 0; checks.Clear(); errors.Clear(); observed.Clear(); observedRows.Clear(); observedMembers.Clear();
            next = EditorApplication.timeSinceStartup + 1d; deadline = next + 60d;
            Application.logMessageReceived += Log;
            EditorApplication.update += Step;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
        { EditorApplication.update -= Step; Application.logMessageReceived -= Log; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            string previous = SessionState.GetString(Running + ".StartScene", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }

    private static void Log(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack); }
    private static void Check(bool condition, string label)
    { if (!condition) throw new InvalidOperationException(label); checks.Add(label); }

    private static void Step()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Audience Play check timed out.");
            switch (phase)
            {
                case 0:
                    var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    game = scope.Container.Resolve<RhythmGamePresenter>();
                    model = scope.Container.Resolve<RhythmGameModel>();
                    clock = scope.Container.Resolve<AudioConductorService>();
                    factory = scope.Container.Resolve<TargetFactory>();
                    destination = scope.Container.Resolve<Transform>();
                    arrivalDistance = scope.Container.Resolve<RhythmGameConfig>().ArrivalDistance;
                    if (model.IsPaused) game.ResumeSession();
                    phase++; next = EditorApplication.timeSinceStartup + 0.2d; break;
                case 1:
                    if (model.IsPaused) game.ResumeSession();
                    if (!clock.HasStarted) { next = EditorApplication.timeSinceStartup + 0.1d; return; }
                    factory.ClearTargets();
                    timing = new TargetTiming(clock.CurrentSongTime + 0.4d, clock.CurrentSongTime + 3.4d);
                    for (float angle = -75; angle <= 75 && word == null; angle += 15) word = factory.CreateAtAngle(angle, timing);
                    if (word == null || word.SourceEmitter == null)
                        throw new InvalidOperationException("No audience word created. " + scopeStatus());
                    var currentConfig = Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<RhythmGameConfig>();
                    if (currentConfig.WordsPassThroughScenery)
                    {
                        var collider = word.GetComponent<Collider>();
                        Check(collider.isTrigger, "Passthrough words retain a trigger collider for clicks without physical blocking.");
                        Physics.SyncTransforms();
                        var ray = new Ray(collider.bounds.center + word.transform.forward * 0.5f, -word.transform.forward);
                        Check(Physics.Raycast(ray, out var clickHit, 1f, 1 << word.gameObject.layer, QueryTriggerInteraction.Collide)
                            && clickHit.collider == collider, "Passthrough word remains reachable by the interaction raycast.");
                        sceneryProbe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        sceneryProbe.name = "Temporary solid scenery crossing check";
                        sceneryProbe.transform.position = word.Position + (destination.position - word.Position).normalized * 0.8f;
                        sceneryProbe.transform.localScale = Vector3.one * 0.25f;
                    }
                    checks.Add("Factory selects a registered audience emitter.");
                    if (factory.AudienceRedirectedCount > 0) checks.Add("A requested sector is reassigned without moving musical time.");
                    Check(Vector3.Distance(word.Position, word.SourceEmitter.WordOrigin.position) < 0.001f,
                        "Word begins at the actual mouth position, including future musical launch time.");
                    Check(Vector3.Distance(word.LaunchPosition, word.SourceEmitter.WordOrigin.position) < 0.001f,
                        "Launch provenance matches WordOrigin.");
                    Check(ReferenceEquals(word.Timing, timing), "Factory preserves the scheduled SpawnTime and HitTime.");
                    cue = Object.FindObjectsByType<SpawnRingView>(FindObjectsSortMode.None)
                        .Single(c => c.SourceEmitter == word.SourceEmitter);
                    Check(Vector3.Distance(cue.transform.position, word.SourceEmitter.WordOrigin.position) < 0.001f,
                        "One audience cue is anchored to the emitting mouth.");
                    Check(!cue.IsVisible, "Mouth cue remains hidden before the scheduled musical launch.");
                    initialScale = word.transform.localScale.magnitude;
                    game.PauseSession();
                    pausedPosition = word.Position; pausedScale = word.transform.localScale; pausedTime = clock.CurrentSongTime;
                    phase++; next = EditorApplication.timeSinceStartup + 0.5d; break;
                case 2:
                    Check(word != null && Vector3.Distance(word.Position, pausedPosition) < 0.001f &&
                        Vector3.Distance(word.transform.localScale, pausedScale) < 0.001f && Math.Abs(clock.CurrentSongTime - pausedTime) < 0.01d,
                        "Pause freezes musical time, mouth trajectory and growth.");
                    Check(cue != null && !cue.IsVisible, "Pause does not reveal a future mouth cue.");
                    game.ResumeSession(); phase++; next = EditorApplication.timeSinceStartup + 0.48d; break;
                case 3:
                    if (clock.CurrentSongTime < timing.SpawnTime + 0.06d)
                    { next = EditorApplication.timeSinceStartup + 0.02d; return; }
                    Check(cue != null && cue.IsVisible, "Mouth pulse becomes visible at the musical launch.");
                    Check(Vector3.Distance(cue.transform.position, word.SourceEmitter.WordOrigin.position) < 0.001f &&
                        Vector3.Distance(word.Position, cue.transform.position) > 0.01f,
                        "Pulse stays on the character as the word moves away.");
                    if (Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<RhythmGameConfig>().ImmediateWordGrowth)
                        Check(word.transform.localScale.magnitude > initialScale,
                            "Audience word starts moving and growing on launch without an initial size hold.");
                    CaptureCue(cue);
                    game.PauseSession();
                    pausedCueWidth = cue.GetComponent<LineRenderer>().widthMultiplier;
                    phase++; next = EditorApplication.timeSinceStartup + 0.4d; break;
                case 4:
                    Check(cue != null && cue.IsVisible && Mathf.Abs(cue.GetComponent<LineRenderer>().widthMultiplier - pausedCueWidth) < 0.0001f,
                        "An active mouth pulse freezes during pause.");
                    game.ResumeSession(); phase++; next = EditorApplication.timeSinceStartup + 1d; break;
                case 5:
                    float travelDistance = Vector3.Distance(word.LaunchPosition, destination.position) - arrivalDistance;
                    var liveConfig = Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<RhythmGameConfig>();
                    float spatialProgress = AudienceWordFlight.TravelProgressAt((float)timing.ProgressAt(clock.CurrentSongTime),
                        travelDistance, word.LaunchClearance, liveConfig.UseDistanceAwareApproach ? liveConfig.ReadableApproachFraction : 0f);
                    float travelled = spatialProgress * travelDistance;
                    if (travelled < word.LaunchClearance + AudienceWordFlight.GrowthDistance * 0.65f)
                    { next = EditorApplication.timeSinceStartup + 0.02d; return; }
                    Check(word != null && word.transform.localScale.magnitude > initialScale * 2f,
                        "Word grows smoothly after leaving the mouth.");
                    Check(Vector3.Distance(word.Position, pausedPosition) > 0.2f && ReferenceEquals(word.Timing, timing),
                        "Flight advances toward the player without rescheduling arrival.");
                    var arrival = destination.position + (word.LaunchPosition - destination.position).normalized * arrivalDistance;
                    Check(Vector3.Distance(word.Position, Vector3.Lerp(word.LaunchPosition, arrival, spatialProgress)) < 0.15f,
                        "Runtime position follows the calibrated distance-aware approach and keeps the musical arrival.");
                    if (sceneryProbe != null)
                    {
                        Check(travelled > 1f, "Word crossed a solid scenery collider without stopping or changing its scheduled path.");
                        Object.Destroy(sceneryProbe);
                    }
                    Check(cue == null || !cue.gameObject.activeInHierarchy, "Mouth pulse expires after its brief musical duration.");
                    game.RestartSession();
                    Check(factory.AudienceSpawnedCount == 0 && factory.AudienceSkippedCount == 0 && factory.AudienceRedirectedCount == 0,
                        "Restart clears targets and resets audience selection counters.");
                    Check(Object.FindObjectsByType<SpawnRingView>(FindObjectsSortMode.None).Length == 0,
                        "Restart immediately removes active visual cues.");
                    phase++; next = EditorApplication.timeSinceStartup + 0.2d; break;
                case 6:
                    if (model.IsPaused) game.ResumeSession();
                    foreach (var target in Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None))
                    {
                        if (target.IsResolved) continue;
                        Check(target.SourceEmitter != null, "Chart word comes from the audience.");
                        if (Vector3.Distance(target.LaunchPosition, target.SourceEmitter.WordOrigin.position) > 0.001f)
                            throw new InvalidOperationException("Chart word has a floating origin.");
                        observed.Add(target.GetInstanceID());
                        observedMembers.Add(target.SourceEmitter.name);
                        var routeLayout = target.SourceEmitter.gameObject.scene.GetRootGameObjects()
                            .SelectMany(g => g.GetComponentsInChildren<AudienceWordRouteLayout>(true)).Single();
                        observedRows.Add(routeLayout.RowFor(target.SourceEmitter) ?? "Audience");
                        // This pass observes spawn variety, not player skill. Remove observed
                        // targets so misses cannot end the session before ten slower events.
                        target.Cancel();
                    }
                    if (observed.Count < 10) { next = EditorApplication.timeSinceStartup + 0.1d; return; }
                    Check(observedRows.Count >= 2 && observedMembers.Count >= 3,
                        "Live chart uses different audience rows and different characters.");
                    Check(errors.Count == 0, "No runtime exceptions during audience playback.");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/NivelAudienceLaunchPlay.png"));
                    phase++; next = EditorApplication.timeSinceStartup + 0.5d; break;
                case 7:
                    File.WriteAllText("Logs/NivelAudienceLaunchPlay.txt", "PASS\n" + string.Join("\n", checks.Distinct()) +
                        "\nObserved unique chart words=" + observed.Count + "; spawned=" + factory.AudienceSpawnedCount +
                        "; redirected=" + factory.AudienceRedirectedCount + "; skipped unavailable routes=" + factory.AudienceSkippedCount +
                        "; rows=" + string.Join(", ", observedRows) + "; different characters=" + observedMembers.Count + "\n");
                    EditorApplication.ExitPlaymode(); break;
            }
        }
        catch (Exception e)
        {
            File.WriteAllText("Logs/NivelAudienceLaunchPlay.txt", "FAIL\n" + e + "\n" + string.Join("\n", errors));
            EditorApplication.ExitPlaymode();
        }
    }

    private static string scopeStatus()
    {
        var registry = Object.FindFirstObjectByType<AudienceWordEmitterRegistry>();
        return registry.GetComponent<AudienceWordRouteLayout>().LastRejection;
    }

    private static void CaptureCue(SpawnRingView pulse)
    {
        var holder = new GameObject("Temporary audience cue capture camera");
        var texture = new RenderTexture(800, 800, 24);
        var pixels = new Texture2D(800, 800, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            var camera = holder.AddComponent<Camera>();
            Vector3 point = pulse.transform.position + Vector3.up * 0.08f;
            camera.transform.position = point + pulse.SourceEmitter.transform.forward * 1.4f;
            camera.transform.rotation = Quaternion.LookRotation(point - camera.transform.position, Vector3.up);
            camera.fieldOfView = 35f; camera.nearClipPlane = 0.03f; camera.farClipPlane = 35f;
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            pixels.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); pixels.Apply();
            File.WriteAllBytes("Logs/NivelAudienceCueCloseup.png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            Object.Destroy(holder); Object.Destroy(texture); Object.Destroy(pixels);
        }
    }
}
