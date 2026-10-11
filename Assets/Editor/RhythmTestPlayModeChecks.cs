using System;
using System.Collections.Generic;
using System.IO;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using Mentality.UI.Views;
using Gameplay.Rhythm.Scopes;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;
using Cursor = UnityEngine.Cursor;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;
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
    private static readonly List<string> InputDiagnostics = new List<string>();
    private static IPlayerInputService _playerInput;
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
    private static AudioSource _completionAudio;
    private static AudioClip _completionClip;
    private static TargetView[] _restartTargets;
    private static SpawnRingView[] _restartRings;
    private static Vector3 _position;
    private static Vector3 _look;
    private static Mouse _mouse;
    private static Keyboard _keyboard;
    private static Mouse _previousMouse;
    private static Keyboard _previousKeyboard;
    private static bool _restoreMouseEnabled, _restoreKeyboardEnabled;
    private static bool _victoryClickReleased;
    private static int _sanity;
    private static int _screamerPausedSample;
    private static bool _completionScreamerPending;
    private static Vector2 _victoryPointerPosition;
    private static string _victoryPointerDetails;

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
        // Refresh disk edits before testing; never discard an unsaved editor scene.
        bool hasDirtyScene = false;
        for (int i = 0; i < SceneManager.sceneCount; i++) hasDirtyScene |= SceneManager.GetSceneAt(i).isDirty;
        if (!hasDirtyScene) EditorSceneManager.OpenScene(RhythmTestSceneBuilder.ScenePath, OpenSceneMode.Single);
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
            InputDiagnostics.Clear();
            _phase = 0;
            _victoryClickReleased = false;
            _nextStep = EditorApplication.timeSinceStartup + 1.5;
            _deadline = EditorApplication.timeSinceStartup + 40;
            Application.logMessageReceived += OnLog;
            InputSystem.onAfterUpdate += CaptureInput;
            EditorApplication.update += Step;
            var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.update -= Step;
            Application.logMessageReceived -= OnLog;
            InputSystem.onAfterUpdate -= CaptureInput;
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

    private static void CheckScreamerImages(FalseWordEffectService effects)
    {
        var settings = _config.Screamer;
        Check(settings.Images != null && settings.Images.Length == 15 && Array.TrueForAll(settings.Images, t => t != null),
            "Quince imagenes de screamer asignadas en la escena");
        Texture previous = null;
        for (int i = 0; i < 60; i++)
        {
            effects.Request(WordTargetKind.Screamer);
            var current = FindRuntime("Screamer overlay").GetComponentInChildren<UnityEngine.UI.RawImage>(true).texture;
            if (Array.IndexOf(settings.Images, current as Texture2D) < 0 || current == previous)
                throw new InvalidOperationException("Screamer fuera de la lista o repetido consecutivamente");
            previous = current;
        }
        Check(true, "Sesenta activaciones usan la lista sin repetir consecutivamente");
        var audio = FindRuntime("Screamer audio")?.GetComponent<AudioSource>();
        Check(audio != null && audio.isPlaying && audio.clip == settings.Clip && !audio.loop && audio.spatialBlend == 0f,
            "Screamer reproduce susto junto a la imagen con audio independiente");
        Check(Array.FindAll(Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            item => item.name == "Screamer audio").Length == 1 && audio.time < 0.15f,
            "Activaciones consecutivas reutilizan una fuente y reinician el susto");
        Check(Math.Abs(audio.volume - settings.Volume) < 0.001f,
            "Screamer aplica el volumen configurado");
        var original = settings.Images;
        try
        {
            settings.Images = new[] { original[0], null, original[0] };
            effects.Request(WordTargetKind.Screamer);
            Check(FindRuntime("Screamer overlay").GetComponentInChildren<UnityEngine.UI.RawImage>(true).texture == original[0],
                "Lista con una imagen duplicada y referencias vacias funciona");
            settings.Images = Array.Empty<Texture2D>();
            effects.Request(WordTargetKind.Screamer);
            Check(FindRuntime("Screamer overlay").GetComponentInChildren<UnityEngine.UI.RawImage>(true).texture == settings.Image,
                "Lista vacia conserva la imagen de respaldo");
        }
        finally { settings.Images = original; }
    }

    private static void CaptureInput()
    {
        if (_phase != 4 || _playerInput == null || InputDiagnostics.Count >= 12) return;
        if (!_playerInput.ActionPressed && (_mouse == null || !_mouse.leftButton.isPressed)) return;
        string target = _player.PerformFirstPersonRaycast(out var hit, false) ? hit.collider.name : "none";
        InputDiagnostics.Add($"update={InputState.currentUpdateType} attack={_playerInput.ActionPressed} focus={Application.isFocused} paused={_model.IsPaused} waiting={_conductor.IsWaitingForTimeline} ray={target} hits={_model.Hits}");
    }

    private static void Step()
    {
        if (!EditorApplication.isPlaying) return;
        if (_completionScreamerPending && !_conductor.IsWaitingForTimeline)
        {
            _completionScreamerPending = false;
            Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<FalseWordEffectService>().Request(WordTargetKind.Screamer);
            if (FindRuntime("Screamer audio")?.GetComponent<AudioSource>().isPlaying != true)
            { Errors.Add("Susto no se activo antes del final de cancion"); Finish(); return; }
            Checks.Add("Susto activo antes del final de cancion");
        }
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
                    var creaturePlan = scope.Container.Resolve<RhythmSpawnerPresenter>().CreatureSchedule;
                    Check(_config.Creatures.Enabled && _config.Creatures.Prefab != null && creaturePlan != null
                        && creaturePlan.Events.Count > 0, "Progresion de criaturas activa al iniciar la sesion");
                    Check(!creaturePlan.TryTakeDue(71d, out _), "Criaturas esperan el primer tramo intenso");
                    _player = scope.Container.Resolve<PlayerView>();
                    _playerModel = scope.Container.Resolve<PlayerModel>();
                    _playerInput = scope.Container.Resolve<IPlayerInputService>();
                    var hud = scope.Container.Resolve<RhythmHudView>();
                    var hudRoot = hud.GetComponent<UIDocument>().rootVisualElement;
                    Check(hudRoot != null, "HUD de UI Toolkit enlazado");
                    var sanityBar = hudRoot.Q<SanityWedgeBar>("sanityTrack");
                    var combo = hudRoot.Q<Label>("comboLabel");
                    Check(sanityBar != null && sanityBar.pickingMode == PickingMode.Ignore
                        && Mathf.Approximately(sanityBar.value, _model.Sanity / (float)RhythmGameModel.MaximumSanity),
                        "HUD presenta cordura sin bloquear clics");
                    Check(combo != null && combo.pickingMode == PickingMode.Ignore,
                        "Feedback de precision asignado sin bloquear clics");
                    var legacyCanvas = hud.GetComponent<Canvas>();
                    var legacyTop = hud.transform.Find("Top panel");
                    var legacyFooter = hud.transform.Find("Footer");
                    Check((legacyCanvas == null || !legacyCanvas.enabled)
                        && (legacyTop == null || !legacyTop.gameObject.activeSelf)
                        && (legacyFooter == null || !legacyFooter.gameObject.activeSelf),
                        "HUD anterior oculto durante el juego");
                    Check(_model.IsPlaying, "Sesion ritmica iniciada");
                    if (_config.AnalyzedSong != null)
                    {
                        Check(_game.MusicalMap != null, "Mapa musical cargado y validado");
                        if (_config.AnalyzedSong.ManualIntensity != null && _config.AnalyzedSong.ManualIntensity.Enabled)
                        {
                            var manual = _config.AnalyzedSong.ManualIntensity;
                            Check(manual.Intervals != null && manual.Intervals.Length > 0, "Intervalos manuales persisten al entrar en Play Mode");
                            var plan = new MusicalSpawnSchedule(_game.MusicalMap, _config.MinimumSpawnInterval, _config.TargetLeadTimeSeconds,
                                _config.MusicalActivity, _config.Patterns, 0d, _config.Placement, _config.Cadence, manual);
                            var checkedIntervals = manual.Sections.Length > 0
                                ? Array.ConvertAll(manual.Sections, s => new MusicalActiveInterval { Start = s.Start, End = s.End })
                                : manual.Intervals;
                            foreach (var interval in checkedIntervals)
                            {
                                double midpoint = (interval.Start + interval.End) / 2d;
                                Check(plan.PhraseProfile.At(midpoint) == MusicalPhrasePressure.Pressure,
                                    "Tramo manual activa patron intenso en " + midpoint + " segundos");
                                var section = plan.ActivityProfile.ManualIntensity.SectionAt(midpoint);
                                Check(Math.Abs(plan.ActivityProfile.FrenzyAt(midpoint) - (section?.Frenzy ?? _config.MusicalActivity.MaximumFrenzy)) < 1e-6d,
                                    "Tramo manual alcanza el frenesi configurado");
                                double expectedSpeed = section?.SpeedMultiplier ?? (1d + (_config.MusicalActivity.MaximumSpeedMultiplier - 1d) * _config.MusicalActivity.MaximumFrenzy);
                                Check(Math.Abs(plan.ActivityProfile.SpeedMultiplierAt(midpoint) - expectedSpeed) < 1e-6d,
                                    "Velocidad intensa coincide con la configuracion actual");
                                Check(section != null
                                    ? plan.ActivityProfile.ManualIntensity.SectionAt(interval.Start) == section
                                        && plan.ActivityProfile.ManualIntensity.SectionAt(interval.Start - 0.001d) != section
                                        && plan.ActivityProfile.ManualIntensity.SectionAt(interval.End) != section
                                    : !plan.ActivityProfile.ManualIntensity.Contains(interval.Start - 0.001d)
                                        && plan.ActivityProfile.ManualIntensity.Contains(interval.Start)
                                        && !plan.ActivityProfile.ManualIntensity.Contains(interval.End), "Limites manuales respetados en Play Mode");
                            }
                        }
                        Check(_config.SessionMusicClip == _config.AnalyzedSong.MusicClip,
                            "Audio de la sesion asociado al mapa musical");
                        var musicalTargets = Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
                        Check(HasTargetsOrPendingCadenceIntro(), "Objetivos aparecen con anticipacion o esperan el primer beat de la introduccion");
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
                    Check(HasTargetsOrPendingCadenceIntro(), "Spawn respeta el inicio musical de la sesion");
                    _previousMouse = Mouse.current;
                    _previousKeyboard = Keyboard.current;
                    _restoreMouseEnabled = _previousMouse != null && _previousMouse.enabled;
                    _restoreKeyboardEnabled = _previousKeyboard != null && _previousKeyboard.enabled;
                    // Aislar los dispositivos de prueba del movimiento/clic fisico del editor.
                    if (_restoreMouseEnabled) InputSystem.DisableDevice(_previousMouse);
                    if (_restoreKeyboardEnabled) InputSystem.DisableDevice(_previousKeyboard);
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
                    _sanity = _model.Sanity;
                    break;
                case 3:
                    Check(_player.PerformFirstPersonRaycast(out var hit) && hit.collider.GetComponent<TargetView>() != null, "Raycast del Player alcanza una palabra");
                    InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
                    break;
                case 4:
                    InputSystem.QueueStateEvent(_mouse, new MouseState());
                    Check(_model.Hits == _hits + 1, "Clic real del Input System registra un acierto"
                        + (_model.Hits == _hits + 1 ? "" : ". " + string.Join(" | ", InputDiagnostics)));
                    Check(_model.Combo == _combo + 1 && _model.Score == _score
                        + RhythmScoreRules.PointsFor(RhythmJudgment.Hit, _combo + 1) + _model.LastPulseBonus,
                        "El acierto por raycast suma puntos y mantiene el combo");
                    Check(_model.Sanity == _sanity, "Interceptar palabra normal conserva cordura");
                    _factory.ClearTargets();
                    _misses = _model.Misses;
                    _factory.Create(_config.WordPrefabs[1], _player.CameraTransform.position + _player.CameraTransform.forward * (_config.ArrivalDistance + 0.1f));
                    break;
                case 5:
                    Check(_model.Misses == _misses + 1, "Llegada de palabra registra un fallo");
                    Check(_model.Sanity == _sanity - 20, "Llegada normal resta exactamente veinte puntos de cordura");
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
                    _game.RestartSession();
                    _factory.ClearTargets();
                    _conductor.PlayMusic(0d);
                    break;
                case 11:
                    Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<FalseWordEffectService>().Request(WordTargetKind.Screamer);
                    Check(FindRuntime("Screamer audio").GetComponent<AudioSource>().isPlaying, "Susto activo antes de derrota");
                    for (int i = 0; i < 5; i++)
                    {
                        var collision = _factory.Create(_config.WordPrefabs[0], _player.CameraTransform.position);

                    }
                    break;
                case 12:
                    Check(!FindRuntime("Screamer audio").GetComponent<AudioSource>().isPlaying, "Derrota detiene el sonido del screamer");
                    Check(_model.Sanity == 0 && _model.IsGameOver && !_model.IsPlaying, "Cinco colisiones reales agotan cordura y terminan la partida");
                    Check(!_conductor.IsPlaying && _factory.ActiveTargetCount == 0,
                        "Game Over detiene musica y limpia palabras");
                    var gameOverAudio = FindRuntime("Game Over audio")?.GetComponent<AudioSource>();
                    Check(gameOverAudio != null && gameOverAudio.isPlaying && gameOverAudio.loop
                        && gameOverAudio.clip == _config.GameOverClip && gameOverAudio.spatialBlend == 0f,
                        "Derrota reproduce el soundtrack de game over");
                    var gameOverScreen = FindScreen("gameOverScreen");
                    Check(gameOverScreen != null, "Pantalla Game Over visible");
                    Check(Cursor.lockState == CursorLockMode.None && Cursor.visible,
                        "Cursor disponible en Game Over");
                    var restartButton = gameOverScreen.Q<Button>("restartBtn");
                    Check(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null
                        && restartButton != null && restartButton.panel != null,
                        "Interfaz de reinicio recibe eventos del puntero");
                    Check(restartButton.enabledInHierarchy, "Boton de reinicio disponible");
                    Submit(restartButton);
                    break;
                case 13:
                    Check(_model.Sanity == 100 && !_model.IsGameOver && _model.IsPlaying,
                        "Boton reinicia el nivel con cordura completa");
                    Check(FindRuntime("Game Over audio")?.GetComponent<AudioSource>().isPlaying == false,
                        "Reinicio detiene el soundtrack de game over");
                    Check(FindScreen("gameOverScreen") == null,
                        "Reinicio oculta pantalla Game Over");
                    BeginCompletionCheck(false);
                    _nextStep = EditorApplication.timeSinceStartup + 0.7;
                    return;
                case 14:
                    Check(!FindRuntime("Screamer audio").GetComponent<AudioSource>().isPlaying, "Victoria detiene el sonido del screamer");
                    Check(_model.HasWon && !_model.IsGameOver && !_model.IsPlaying && _model.Sanity > 0,
                        "Fin natural del audio concede victoria con cordura restante");
                    Check(_conductor.HasCompletedSong && !_conductor.IsPlaying && _factory.ActiveTargetCount == 0,
                        "Victoria detiene audio y limpia objetivos");
                    var victory = FindScreen("victoryScreen");
                    Check(victory != null, "Pantalla WIN GAME visible");
                    Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Victoria libera el cursor");
                    var victoryButton = victory.Q<Button>("restartBtn");
                    var buttonPosition = PanelToScreen(victoryButton.panel, victoryButton.worldBound.center);
                    _victoryPointerPosition = buttonPosition;
                    _victoryPointerDetails = $"target={buttonPosition}, bound={victoryButton.worldBound}, panel={victoryButton.panel}";
                    InputSystem.QueueStateEvent(_mouse, new MouseState { position = buttonPosition }.WithButton(MouseButton.Left));
                    break;
                case 15:
                    if (!_victoryClickReleased)
                    {
                        _victoryClickReleased = true;
                        InputSystem.QueueStateEvent(_mouse, new MouseState { position = _victoryPointerPosition });
                        _phase = 15;
                        _nextStep = EditorApplication.timeSinceStartup + 0.2;
                        return;
                    }
                    Check(_model.IsPlaying && !_model.HasWon && !_model.IsGameOver && _model.Sanity == 100,
                        "Clic real en boton de victoria reinicia con cordura completa" + VictoryPointerDiagnostics());
                    Check(FindScreen("victoryScreen") == null,
                        "Reinicio oculta victoria");
                    BeginCompletionCheck(true);
                    _nextStep = EditorApplication.timeSinceStartup + 0.7;
                    return;
                case 16:
                    Check(_model.IsGameOver && !_model.HasWon && _model.Sanity == 0,
                        "Llegada en el final del audio concede derrota antes que victoria");
                    Submit(FindScreen("gameOverScreen").Q<Button>("restartBtn"));
                    _nextStep = EditorApplication.timeSinceStartup + _config.TargetLeadTimeSeconds + 0.3;
                    return;
                case 17:
                    var restartScope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    var restartEffects = restartScope.Container.Resolve<FalseWordEffectService>();
                    CheckScreamerImages(restartEffects);
                    restartEffects.Request(WordTargetKind.Screamer);
                    restartEffects.Request(WordTargetKind.Glitch);
                    restartEffects.Request(WordTargetKind.Bullicio);
                    Check(FindRuntime("Screamer overlay")?.activeSelf == true && FindRuntime("Glitch overlay")?.activeSelf == true,
                        "Screamer y glitch activos antes del reinicio");
                    Check(FindRuntime("Bullicio audio")?.GetComponent<AudioSource>().isPlaying == true
                        && restartScope.Container.Resolve<BullicioSessionService>().ProbabilityBonus > 0,
                        "Bullicio y aumento de palabras falsas activos antes del reinicio");
                    _model.RegisterHit();
                    _model.RegisterWordCollision();
                    _factory.Create(_config.WordPrefabs[0], _player.CameraTransform.position + Vector3.forward * 10f);
                    _restartTargets = Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
                    _restartRings = Object.FindObjectsByType<SpawnRingView>(FindObjectsSortMode.None);
                    _game.PauseSession();
                    Check(_model.IsPaused, "Reinicio probado desde pausa con efectos pendientes");
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.R));
                    break;
                case 18:
                    InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                    Check(_model.Sanity == 100 && _model.Hits == 0 && _model.Misses == 0 && _model.Score == 0
                        && _model.Combo == 0 && _model.BestCombo == 0 && _model.LastJudgment == RhythmJudgment.None,
                        "Reinicio limpia cordura, puntuacion, combo y feedback");
                    Check(_model.IsPlaying && !_model.IsPaused && !_model.IsGameOver && !_model.HasWon
                        && !_conductor.IsPaused && !_conductor.HasCompletedSong && _conductor.CurrentSongTime < 0,
                        "Reinicio restaura sesion y cuenta de entrada desde el comienzo");
                    foreach (var oldTarget in _restartTargets)
                        Check(oldTarget == null || !oldTarget.gameObject.activeSelf, "Palabra anterior retirada al reiniciar");
                    foreach (var oldRing in _restartRings)
                        Check(oldRing == null || !oldRing.gameObject.activeSelf, "Aro anterior retirado al reiniciar");
                    CheckEffectsCleared();
                    Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "Reinicio recupera cursor de juego");
                    _nextStep = EditorApplication.timeSinceStartup + _config.TargetLeadTimeSeconds + 0.3;
                    return;
                case 19:
                    CheckEffectsCleared();
                    Check(_conductor.HasStarted && _conductor.IsPlaying, "Cancion vuelve a comenzar tras reiniciar");
                    var repeatedEffects = Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<FalseWordEffectService>();
                    repeatedEffects.Request(WordTargetKind.Screamer);
                    repeatedEffects.Request(WordTargetKind.Glitch);
                    repeatedEffects.Request(WordTargetKind.Bullicio);
                    foreach (var effectName in new[] { "Screamer overlay", "Glitch overlay", "Bullicio audio" })
                        Check(Array.FindAll(Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                            item => item.name == effectName).Length == 1, "Reinicio reutiliza un unico efecto: " + effectName);
                    Check(FindRuntime("Bullicio audio").GetComponent<AudioDistortionFilter>().enabled,
                        "Nuevo bullicio vuelve a habilitar su distorsion");
                    _game.RestartSession();
                    CheckEffectsCleared();
                    break;
                case 20:
                    if (!_conductor.HasStarted) { _phase--; _nextStep = EditorApplication.timeSinceStartup + 0.1d; return; }
                    Check(_model.Sanity == 100 && !_model.IsPaused && _model.IsPlaying,
                        "Reinicios consecutivos mantienen una partida nueva limpia");
                    CheckCreatureAttack();
                    CheckCreatureProgression();
                    _nextStep = EditorApplication.timeSinceStartup + _config.TargetLeadTimeSeconds + 0.3;
                    return;
                case 21:
                    Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<FalseWordEffectService>().Request(WordTargetKind.Screamer);
                    FindRuntime("Screamer audio").GetComponent<AudioSource>().time = 0.5f;
                    _game.PauseSession();
                    _screamerPausedSample = FindRuntime("Screamer audio").GetComponent<AudioSource>().timeSamples;
                    Check(!FindRuntime("Screamer audio").GetComponent<AudioSource>().isPlaying,
                        "Pausa detiene temporalmente el susto");
                    break;
                case 22:
                    var pausedScreamer = FindRuntime("Screamer audio").GetComponent<AudioSource>();
                    Check(Math.Abs(pausedScreamer.timeSamples - _screamerPausedSample) < 2048,
                        "Susto conserva su posicion durante pausa");
                    _game.ResumeSession();
                    break;
                case 23:
                    var resumedScreamer = FindRuntime("Screamer audio").GetComponent<AudioSource>();
                    Check(resumedScreamer.isPlaying && resumedScreamer.time > 0.4f,
                        "Reanudar continua el susto sin reiniciarlo");
                    _game.PauseSession();
                    _game.RestartSession();
                    Check(!resumedScreamer.isPlaying && resumedScreamer.timeSamples == 0,
                        "Reinicio desde pausa cancela el susto y borra su posicion");
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

    private static void CheckCreatureAttack()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/Creatures/Creature.prefab");
        Check(prefab != null, "Prefab de criatura con boca abierta disponible");
        var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
        var player = new SerializedObject(scope).FindProperty("playerTarget").objectReferenceValue as Transform;
        var creature = Object.Instantiate(prefab).GetComponent<CreatureEmergenceView>();
        var origin = player.position + Vector3.forward * 10f;
        origin.y = 0;
        creature.Initialize(origin, player, _conductor);
        _factory.RegisterCreature(creature);
        var profile = new MusicalActivityProfile(_game.MusicalMap, _config.MinimumSpawnInterval,
            _config.MusicalActivity, _config.AnalyzedSong.ManualIntensity);
        var spawned = new List<TargetView>();
        creature.ConfigureAttack(4, 0.22d, (mouth, index, time) =>
            spawned.Add(_factory.CreateCreatureWord(mouth, index, _conductor.CurrentSongTime, profile)), _model);
        creature.AdvanceAt(0.9d);
        Check(spawned.Count == 0 && !creature.IsAttacking, "Criatura aparece de pie antes de atacar");
        creature.AdvanceAt(1.05d);
        Check(creature.IsAttacking && spawned.Count == 0, "Boca abierta anticipa la rafaga");
        int sanity = _model.Sanity;
        int hits = _model.Hits;
        for (int i = 0; i < 4; i++)
        {
            creature.AdvanceAt(1.151d + i * 0.22d);
            creature.AdvanceAt(1.151d + i * 0.22d);
        }
        Check(spawned.Count == 4 && spawned.TrueForAll(t => t != null && t.Kind != WordTargetKind.Normal),
            "Rafaga crea cuatro palabras falsas sin duplicados");
        Check(spawned[0].Timing.LateralArcDegrees > 0f && spawned[1].Timing.LateralArcDegrees < 0f,
            "Rafaga alterna lados del abanico sin cambiar el destino");
        Check(spawned.TrueForAll(t => Vector3.Distance(t.Position, creature.MouthPosition) < 0.05f
            && Math.Abs(t.Timing.ProgressAt(t.Timing.HitTime) - 1d) < 1e-6),
            "Palabras salen de la boca y su recorrido termina en el jugador");
        Check(!_factory.HasClearCreatureRoute(creature.MouthPosition),
            "Ruta ocupada impide superponer otro lanzamiento de criatura");
        foreach (var target in spawned) target.Touch(player.position);
        Check(_factory.HasClearCreatureRoute(creature.MouthPosition),
            "Interceptar palabras libera la ruta de la criatura");
        Check(_model.Sanity == sanity && _model.Hits == hits && spawned.TrueForAll(t => t.IsResolved),
            "Interceptar la rafaga falsa conserva cordura y no suma aciertos normales");
        float screamer = _config.FalseWords.ScreamerWeight, noise = _config.FalseWords.BullicioWeight, glitch = _config.FalseWords.GlitchWeight;
        var effects = scope.Container.Resolve<FalseWordEffectService>();
        WordTargetKind received = WordTargetKind.Normal;
        Action<WordTargetKind, double> listener = (kind, time) => received = kind;
        effects.Requested += listener;
        try
        {
            foreach (var kind in new[] { WordTargetKind.Screamer, WordTargetKind.Bullicio, WordTargetKind.Glitch })
            {
                _config.FalseWords.ScreamerWeight = kind == WordTargetKind.Screamer ? 1 : 0;
                _config.FalseWords.BullicioWeight = kind == WordTargetKind.Bullicio ? 1 : 0;
                _config.FalseWords.GlitchWeight = kind == WordTargetKind.Glitch ? 1 : 0;
                received = WordTargetKind.Normal;
                var word = _factory.CreateCreatureWord(creature.MouthPosition, 0, _conductor.CurrentSongTime, profile);
                int before = _model.Sanity;
                word.ResolveAtSongEnd(word.Timing.HitTime);
                Check(word.IsResolved && _model.Sanity == before - 20 && received == kind,
                    "Palabra de criatura resta veinte de cordura y activa " + kind);
                effects.Clear();
            }
            creature.ConfigureAttack(2, 0.22d, (mouth, index, time) => spawned.Add(null), _model);
            _conductor.PauseMusic();
            _model.PauseSession();
            int beforePause = spawned.Count;
            creature.AdvanceAt(1.151d);
            Check(spawned.Count == beforePause, "Criatura no dispara durante pausa");
            _game.RestartSession();
            Check(!creature.gameObject.activeSelf && creature.HasFinished && _model.Sanity == 100,
                "Reinicio retira criatura y cancela rafaga pendiente");
        }
        finally
        {
            effects.Requested -= listener;
            _config.FalseWords.ScreamerWeight = screamer;
            _config.FalseWords.BullicioWeight = noise;
            _config.FalseWords.GlitchWeight = glitch;
            effects.Clear();
            _factory.ClearTargets();
        }
    }

    private static void CheckCreatureProgression()
    {
        var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
        var spawner = scope.Container.Resolve<RhythmSpawnerPresenter>();
        var clockType = typeof(AudioConductorService);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        for (int section = 0; section < 3; section++)
        {
            _game.RestartSession();
            var item = new List<CreatureSpawnEvent>(spawner.CreatureSchedule.Events).Find(e => e.Section == section);
            Check(item != null && item.Shots.Length == section + 2, "Rafaga musical de " + (section + 2) + " palabras en seccion " + (section + 1));
            // Advance only the test clock, then use the real spawner, floor mesh and prefab.
            clockType.GetField("_dspSongTime", flags).SetValue(_conductor, AudioSettings.dspTime - item.Time - 0.005d);
            clockType.GetField("_dspTimelineTime", flags).SetValue(_conductor, AudioSettings.dspTime - 1d);
            _conductor.UpdateTime();
            spawner.CheckAndSpawnTargets();
            var creatures = Object.FindObjectsByType<CreatureEmergenceView>(FindObjectsSortMode.None);
            var floor = GameObject.Find("Floor").GetComponent<Renderer>();
            Check(creatures.Length == 1 && Math.Abs(creatures[0].GroundPosition.y - floor.bounds.max.y) < 0.02f,
                "Spawner real situa criatura en el suelo de la seccion " + (section + 1)
                    + (creatures.Length == 1 ? "" : "; cantidad=" + creatures.Length
                        + "; time=" + _conductor.SongTime + "; due=" + item.Time
                        + "; playing=" + _conductor.IsPlaying + "; model=" + _model.IsPlaying
                        + "; timeline=" + _conductor.HasTimelineStarted + "; floor=" + floor.bounds
                        + "; player=" + new SerializedObject(scope).FindProperty("playerTarget").objectReferenceValue));
            var whistle = creatures[0].GetComponent<AudioSource>();
            Check(whistle != null && whistle.isPlaying && whistle.clip == _config.Creatures.AppearanceClip
                && Math.Abs(whistle.volume - _config.Creatures.AppearanceVolume) < 0.001f && !whistle.loop,
                "Aparicion reproduce silbido en seccion " + (section + 1));
            _factory.ClearTargets();
            Check(!whistle.isPlaying, "Limpieza detiene silbido en seccion " + (section + 1));
            Check(creatures[0].HasFinished && !creatures[0].gameObject.activeSelf,
                "Limpieza cancela aparicion musical de la seccion " + (section + 1));
        }
        _game.RestartSession();
        Check(!spawner.CreatureSchedule.TryTakeDue(71d, out _), "Reinicio reconstruye progresion desde el primer tramo");
    }

    private static VisualElement FindScreen(string name)
    {
        var documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var document in documents)
        {
            var screen = document.rootVisualElement?.Q<VisualElement>(name);
            if (screen != null) return screen;
        }
        return null;
    }

    private static void Submit(Button button)
    {
        using (var submit = new NavigationSubmitEvent { target = button })
            button.SendEvent(submit);
    }

    private static Vector2 PanelToScreen(IPanel panel, Vector2 panelPosition)
    {
        if (panel == null) return panelPosition;
        float scale = panel.scaledPixelsPerPoint > 0f ? panel.scaledPixelsPerPoint : 1f;
        return new Vector2(panelPosition.x * scale, Screen.height - (panelPosition.y * scale));
    }

    private static GameObject FindRuntime(string name) => Array.Find(
        Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == name);

    private static bool HasTargetsOrPendingCadenceIntro()
    {
        if (Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None).Length > 0) return true;
        if (_config.Cadence == null || !_config.Cadence.Enabled || _game.MusicalMap == null) return false;
        var plan = new MusicalSpawnSchedule(_game.MusicalMap, _config.MinimumSpawnInterval, _config.TargetLeadTimeSeconds,
            _config.MusicalActivity, _config.Patterns, 0d, _config.Placement, _config.Cadence, _config.AnalyzedSong?.ManualIntensity);
        return plan.Events.Count > 0 && _conductor.CurrentSongTime < plan.Events[0].SpawnTime;
    }

    private static string VictoryPointerDiagnostics()
    {
        if (_model.IsPlaying) return "";
        var events = UnityEngine.EventSystems.EventSystem.current;
        var module = events?.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var pointer = new UnityEngine.EventSystems.PointerEventData(events) { position = _mouse.position.ReadValue() };
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        events?.RaycastAll(pointer, hits);
        return $" [mouse={pointer.position}, uiPoint={module?.point?.action.ReadValue<Vector2>()}, "
            + _victoryPointerDetails + ", "
            + $"module={module?.isActiveAndEnabled}, focused={Application.isFocused}, size={Screen.width}x{Screen.height}, "
            + $"hits={string.Join(",", hits.ConvertAll(item => item.gameObject.name))}]";
    }

    private static void CheckEffectsCleared()
    {
        Check(!FindRuntime("Screamer audio").GetComponent<AudioSource>().isPlaying,
            "Reinicio detiene el sonido del screamer");
        Check(FindRuntime("Screamer overlay")?.activeSelf == false && FindRuntime("Glitch overlay")?.activeSelf == false,
            "Reinicio deja screamer y glitch ocultos sin reactivacion anterior");
        var noise = FindRuntime("Bullicio audio");
        Check(noise != null && !noise.GetComponent<AudioSource>().isPlaying && !noise.GetComponent<AudioDistortionFilter>().enabled,
            "Reinicio detiene bullicio y desactiva distorsion");
        Check(Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<BullicioSessionService>().ProbabilityBonus == 0,
            "Reinicio elimina aumento de palabras falsas");
    }

    private static void BeginCompletionCheck(bool finalCollision)
    {
        _factory.ClearTargets();
        _model.StartSession();
        if (_completionAudio == null)
        {
            _completionAudio = new GameObject("Song completion test audio").AddComponent<AudioSource>();
            _completionClip = AudioClip.Create("Completion check", 4410, 1, 11025, false);
            _completionAudio.clip = _completionClip;
        }
        _conductor.Initialize(_completionAudio, 120f);
        _conductor.PlayMusic();
        _completionScreamerPending = true;
        // Aislar el limite del audio del mapa de la cancion de varios minutos.
        Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<RhythmSpawnerPresenter>().ResetBeatTracking();
        if (!finalCollision) return;
        for (int i = 0; i < 4; i++) _model.RegisterWordCollision();
        double duration = (double)_completionClip.samples / _completionClip.frequency;
        _factory.Create(_config.WordPrefabs[0], _player.CameraTransform.position + Vector3.forward * 12f,
            new TargetTiming(0d, duration));
    }

    private static void Finish()
    {
        if (_completionAudio != null) Object.Destroy(_completionAudio.gameObject);
        if (_completionClip != null) Object.Destroy(_completionClip);
        EditorApplication.update -= Step;
        Application.logMessageReceived -= OnLog;
        InputSystem.onAfterUpdate -= CaptureInput;
        if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        if (_restoreMouseEnabled && _previousMouse != null && _previousMouse.added) InputSystem.EnableDevice(_previousMouse);
        if (_restoreKeyboardEnabled && _previousKeyboard != null && _previousKeyboard.added) InputSystem.EnableDevice(_previousKeyboard);
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












