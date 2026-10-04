using System;
using System.Collections.Generic;
using System.IO;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using Gameplay.Rhythm.Scopes;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using VContainer;
using Object = UnityEngine.Object;

// Prueba de integracion en el editor: usa los contenedores, raycast e Input System reales.
[InitializeOnLoad]
public static class RhythmTestPlayModeChecks
{
    private const string RunningKey = "Mentality.RhythmChecks.Running";
    private const string PreviousSceneKey = "Mentality.RhythmChecks.PreviousStartScene";
    private const string ReportPath = "Logs/RhythmTest-playmode.json";
    private const string ScreenshotPath = "Logs/RhythmTest-preview.png";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static int _phase;
    private static double _nextStep;
    private static double _deadline;
    private static int _hits;
    private static int _misses;
    private static long _score;
    private static int _combo;
    private static PlayerView _player;
    private static PlayerModel _playerModel;
    private static RhythmGameModel _model;
    private static RhythmGamePresenter _game;
    private static AudioConductorService _conductor;
    private static TargetFactory _factory;
    private static RhythmGameConfig _config;
    private static Vector3 _position;
    private static Vector3 _look;
    private static Mouse _mouse;
    private static Keyboard _keyboard;
    private static Mouse _previousMouse;
    private static Keyboard _previousKeyboard;

    [Serializable]
    private class Report
    {
        public bool success;
        public List<string> checks;
        public List<string> errors;
        public string screenshot;
    }

    static RhythmTestPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (File.Exists("Temp/RhythmPlayModeChecks.request")) EditorApplication.update += ProcessRequest;
    }

    private static void ProcessRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= ProcessRequest;
        if (!File.Exists("Temp/RhythmPlayModeChecks.request")) return;
        File.Delete("Temp/RhythmPlayModeChecks.request");
        Run();
    }

    [MenuItem("Tools/Mentality/Rhythm/Run Play Mode Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("La prueba debe iniciarse fuera de Play Mode.");
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RhythmTestSceneBuilder.ScenePath);
        if (scene == null) throw new InvalidOperationException("Primero crea RhythmTest.");
        SessionState.SetString(PreviousSceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear();
            Errors.Clear();
            _phase = 0;
            _nextStep = EditorApplication.timeSinceStartup + 1.5;
            _deadline = EditorApplication.timeSinceStartup + 40;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Step;
            var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.update -= Step;
            Application.logMessageReceived -= OnLog;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(RunningKey, false);
            string previous = SessionState.GetString(PreviousSceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            bool dirty = false;
            for (int i = 0; i < SceneManager.sceneCount; i++) dirty |= SceneManager.GetSceneAt(i).isDirty;
            if (!dirty) EditorSceneManager.OpenScene(RhythmTestSceneBuilder.ScenePath, OpenSceneMode.Single);
        }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Errors.Add(message + "\n" + stack);
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Checks.Add(label);
    }

    private static void Step()
    {
        if (!EditorApplication.isPlaying) return;
        if (EditorApplication.timeSinceStartup > _deadline) { Errors.Add("Tiempo limite de la prueba."); Finish(); return; }
        if (EditorApplication.timeSinceStartup < _nextStep) return;
        try
        {
            switch (_phase++)
            {
                case 0:
                    var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    Check(scope != null && scope.Container != null, "RhythmLifetimeScope construido");
                    _model = scope.Container.Resolve<RhythmGameModel>();
                    _game = scope.Container.Resolve<RhythmGamePresenter>();
                    _conductor = scope.Container.Resolve<AudioConductorService>();
                    _factory = scope.Container.Resolve<TargetFactory>();
                    _config = scope.Container.Resolve<RhythmGameConfig>();
                    _player = scope.Container.Resolve<PlayerView>();
                    _playerModel = scope.Container.Resolve<PlayerModel>();
                    var hud = scope.Container.Resolve<RhythmHudView>();
                    var serializedHud = new SerializedObject(hud);
                    var scoreText = serializedHud.FindProperty("scoreText").objectReferenceValue as UnityEngine.UI.Text;
                    var resultText = serializedHud.FindProperty("resultText").objectReferenceValue as UnityEngine.UI.Text;
                    Check(scoreText != null && scoreText.text.Contains("PUNTOS") && scoreText.text.Contains("COMBO"),
                        "HUD presenta puntuacion y combo");
                    Check(resultText != null && !resultText.raycastTarget,
                        "Feedback de precision asignado sin bloquear clics");
                    Check(_model.IsPlaying, "Sesion ritmica iniciada");
                    if (_config.AnalyzedSong != null)
                    {
                        Check(_game.MusicalMap != null, "Mapa musical cargado y validado");
                        Check(_config.SessionMusicClip == _config.AnalyzedSong.MusicClip,
                            "Audio de la sesion asociado al mapa musical");
                        var musicalTargets = Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
                        Check(musicalTargets.Length > 0, "Objetivos musicales aparecen con anticipacion");
                        foreach (var target in musicalTargets)
                        {
                            Check(target.Timing != null && target.Timing.HitTime >= 0
                                && target.Timing.LeadTime > 0
                                && target.Timing.LeadTime <= _config.TargetLeadTimeSeconds + 0.000001,
                                "Objetivo conserva llegada musical y anticipacion adaptada a velocidad");
                            Check(target.GetComponentsInChildren<LineRenderer>().Length == 0,
                                "El proyectil musical no lleva aros de sincronizacion");
                            Check(target.GetComponentsInChildren<Collider>().Length == 1,
                                "Los aros visuales no interceptan el raycast");
                            if (_config.Placement != null && _config.Placement.Enabled)
                            {
                                Check(target.transform.localScale.x > 0 && target.transform.localScale.x <= 1,
                                    "El objetivo musical ajusta su tamano al lanzarse");
                                var box = target.GetComponent<BoxCollider>();
                                Check(box != null && box.enabled && target.GetComponent<Renderer>() != null,
                                    "La palabra y su collider se conservan con la escala adaptada");
                            }
                        }
                    }
                    Check(!scope.Container.TryResolve<IApiClient>(out _), "Contenedor sin cliente backend: Python/Ollama no se inicia");
                    Check(_playerModel.GameplayMode == PlayerGameplayMode.Rhythm && _playerModel.CurrentPerspective == CameraPerspective.FirstPerson, "Player en modo ritmico y primera persona");
                    Check(Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None).Length > 0,
                        "Spawn real de palabras durante la sesion");
                    _previousMouse = Mouse.current;
                    _previousKeyboard = Keyboard.current;
                    _mouse = InputSystem.AddDevice<Mouse>("RhythmCheckMouse");
                    _keyboard = InputSystem.AddDevice<Keyboard>("RhythmCheckKeyboard");
                    _position = _player.transform.position;
                    _look = _player.CameraTransform.forward;
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W, Key.Space, Key.J));
                    InputSystem.QueueStateEvent(_mouse, new MouseState { delta = new Vector2(80, 0) });
                    break;
                case 1:
                    Check(Vector3.Distance(_position, _player.transform.position) < 0.001f, "W y salto no desplazan al jugador");
                    Check(_playerModel.CurrentPerspective == CameraPerspective.FirstPerson, "J no cambia la camara en modo ritmico");
                    Check(Vector3.Angle(_look, _player.CameraTransform.forward) > 0.5f, "Cinemachine gira con la entrada de raton");
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                    InputSystem.QueueStateEvent(_mouse, new MouseState());
                    break;
                case 2:
                    _factory.ClearTargets();
                    _factory.Create(_config.WordPrefabs[0], _player.CameraTransform.position + _player.CameraTransform.forward * 6f);
                    if (_config.ShowSpawnRing)
                    {
                        var rings = Object.FindObjectsByType<SpawnRingView>(FindObjectsSortMode.None);
                        Check(rings.Length > 0, "El lanzamiento crea un aro de aparicion independiente");
                        foreach (var ring in rings)
                            Check(ring.transform.parent == null && ring.GetComponentsInChildren<Collider>().Length == 0,
                                "El aro queda en el origen y no intercepta disparos");
                    }
                    _hits = _model.Hits;
                    _score = _model.Score;
                    _combo = _model.Combo;
                    break;
                case 3:
                    Check(_player.PerformFirstPersonRaycast(out var hit) && hit.collider.GetComponent<TargetView>() != null, "Raycast del Player alcanza una palabra");
                    InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
                    break;
                case 4:
                    InputSystem.QueueStateEvent(_mouse, new MouseState());
                    Check(_model.Hits == _hits + 1, "Clic real del Input System registra un acierto");
                    Check(_model.Combo == _combo + 1 && _model.Score == _score
                        + RhythmScoreRules.PointsFor(RhythmJudgment.Perfect, _combo + 1),
                        "El acierto por raycast suma puntos y mantiene el combo");
                    _factory.ClearTargets();
                    _misses = _model.Misses;
                    _factory.Create(_config.WordPrefabs[1], _player.CameraTransform.position + _player.CameraTransform.forward * (_config.ArrivalDistance + 0.1f));
                    break;
                case 5:
                    Check(_model.Misses == _misses + 1, "Llegada de palabra registra un fallo");
                    Check(_model.Combo == 0 && _model.ScoreMultiplier == 1,
                        "El fallo reinicia combo y multiplicador");
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
                    break;
                case 6:
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                    Check(Cursor.lockState == CursorLockMode.None && !_player.FirstPersonCamera.GetComponent<CinemachineInputAxisController>().enabled, "Escape libera cursor y detiene giro de camara");
                    Check(_model.IsPaused && _conductor.IsPaused && _model.IsPlaying,
                        "Escape pausa la sesion y el reloj musical sin finalizarla");
                    _hits = _model.Hits;
                    InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
                    break;
                case 7:
                    InputSystem.QueueStateEvent(_mouse, new MouseState());
                    Check(Cursor.lockState == CursorLockMode.Locked, "Clic recupera cursor");
                    Check(!_model.IsPaused && !_conductor.IsPaused,
                        "Clic recupera la sesion musical pausada");
                    Check(_model.Hits == _hits, "Clic de recuperacion no dispara");
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.R));
                    break;
                case 8:
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                    Check(_model.Hits == 0 && _model.Misses == 0 && _model.IsPlaying, "R reinicia sesion y contadores");
                    Check(_model.Score == 0 && _model.Combo == 0 && _model.BestCombo == 0,
                        "R reinicia puntos y mejor racha");
                    Check(!_model.IsPaused && !_conductor.IsPaused, "R limpia el estado de pausa");
                    _factory.ClearTargets();
                    Vector3 forward = _player.CameraTransform.forward;
                    forward.y = 0;
                    forward.Normalize();
                    for (int i = 0; i < 3; i++)
                        _factory.Create(_config.WordPrefabs[i], _player.CameraTransform.position + Quaternion.Euler(0, (i - 1) * 24f, 0) * forward * 10f);
                    break;
                case 9:
                    Directory.CreateDirectory("Logs");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(ScreenshotPath));
                    break;
                case 10:
                    Check(File.Exists(ScreenshotPath), "Captura real de Game View guardada");
                    Finish();
                    return;
            }
            _nextStep = EditorApplication.timeSinceStartup + 0.3;
        }
        catch (Exception exception)
        {
            Errors.Add(exception.ToString());
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Step;
        Application.logMessageReceived -= OnLog;
        if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        if (_previousMouse != null && _previousMouse.added) _previousMouse.MakeCurrent();
        if (_previousKeyboard != null && _previousKeyboard.added) _previousKeyboard.MakeCurrent();
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
        {
            success = Errors.Count == 0,
            checks = Checks,
            errors = Errors,
            screenshot = File.Exists(ScreenshotPath) ? Path.GetFullPath(ScreenshotPath) : ""
        }, true));
        EditorApplication.isPlaying = false;
    }
}
