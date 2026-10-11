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
using UnityEngine.UI;
using VContainer;
using Object = UnityEngine.Object;

// Prueba nativa aislada: reutiliza los recursos, pero no modifica la configuracion de la escena.
[InitializeOnLoad]
public static class FalseWordPlayModeChecks
{
    private const string RunningKey = "Mentality.FalseWordsChecks.Running";
    private const string PreviousKey = "Mentality.FalseWordsChecks.PreviousScene";
    private const string RequestPath = "Temp/FalseWordPlayModeChecks.request";
    private const string ReportPath = "Logs/FalseWords-playmode.json";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static int _phase;
    private static double _next, _deadline, _frozen;
    private static RhythmGameModel _model;
    private static RhythmGameConfig _config;
    private static AudioConductorService _clock;
    private static FalseWordEffectService _effects;
    private static BullicioSessionService _bullicio;
    private static ScreamerPresenter _screamerView;
    private static GlitchPresenter _glitchView;
    private static BullicioPresenter _audioView;
    private static TargetFactory _factory;
    private static Transform _player;
    private static TargetView _target;
    private static string _original, _firstText;
    private static byte[] _frozenGrain;
    private static AudioSource _noise;
    private static bool _arrivalOverlaysVisible;
    private static readonly Dictionary<WordTargetKind, int> Requests = new Dictionary<WordTargetKind, int>();
    [Serializable] private class Report { public bool success; public List<string> checks, errors; public string[] screenshots; }

    static FalseWordPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += OnState;
        if (File.Exists(RequestPath)) EditorApplication.update += ProcessRequest;
    }
    private static void ProcessRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= ProcessRequest;
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        Run();
    }
    [MenuItem("Tools/Mentality/Rhythm/Run False Word Play Mode Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Inicia la prueba fuera de Play Mode.");
        SessionState.SetString(PreviousKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RhythmTestSceneBuilder.ScenePath);
        EditorApplication.EnterPlaymode();
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear(); Errors.Clear(); Requests.Clear(); _phase = 0;
            _next = EditorApplication.timeSinceStartup + 1.5;
            _deadline = EditorApplication.timeSinceStartup + 45;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Step;
            var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.update -= Step;
            Application.logMessageReceived -= OnLog;
            Cleanup();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(RunningKey, false);
            string previous = SessionState.GetString(PreviousKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Errors.Add(message + "\n" + stack);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Checks.Add(message);
    }
    private static GameObject Overlay(string name) => Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(c => c.name == name).gameObject;
    private static Texture2D Grain() => (Texture2D)Overlay("Glitch overlay").GetComponentsInChildren<RawImage>(true).Single(i => i.name == "Static").texture;
    private static TargetView Spawn(WordTargetKind kind, double travel = 1.2)
    {
        var settings = _config.FalseWords;
        settings.ScreamerWeight = kind == WordTargetKind.Screamer ? 1 : 0;
        settings.BullicioWeight = kind == WordTargetKind.Bullicio ? 1 : 0;
        settings.GlitchWeight = kind == WordTargetKind.Glitch ? 1 : 0;
        double now = _clock.CurrentSongTime;
        return _factory.Create(_config.WordPrefabs[0], _player.position + _player.forward * 6f,
            new TargetTiming(now, now + travel));
    }
    private static void Step()
    {
        if (EditorApplication.timeSinceStartup > _deadline) { Errors.Add("Timeout de la prueba nativa."); Finish(); return; }
        try
        {
            if (_clock != null) { _clock.UpdateTime(); _screamerView.PostTick(); _glitchView.PostTick(); _audioView.PostTick(); }
            if (EditorApplication.timeSinceStartup < _next) return;
            double delay = 0.2;
            switch (_phase++)
            {
                case 0:
                    var scope = Object.FindFirstObjectByType<RhythmLifetimeScope>();
                    Check(scope != null && scope.Container != null, "Contenedor real de RhythmTest disponible");
                    var actual = scope.Container.Resolve<RhythmGameConfig>();
                    Check(actual.Screamer.Image != null && actual.Bullicio.Clip != null, "Imagen y MP3 importados y asignados en la escena");
                    Check(actual.Bullicio.Clip.length > 600 && actual.Bullicio.Clip.loadType == AudioClipLoadType.Streaming, "Bullicio largo importado en streaming");
                    scope.Container.Resolve<RhythmGamePresenter>().PauseSession();
                    scope.Container.Resolve<TargetFactory>().ClearTargets();
                    _player = scope.Container.Resolve<Transform>();
                    _config = new RhythmGameConfig { WordPrefabs = actual.WordPrefabs, Screamer = actual.Screamer, Glitch = actual.Glitch, Bullicio = actual.Bullicio };
                    _config.FalseWords.Enabled = true;
                    _config.FalseWords.QuietProbability = _config.FalseWords.FrenzyProbability = 1;
                    _model = new RhythmGameModel(); _model.StartSession();
                    _clock = new AudioConductorService(); _clock.Initialize(null, 120); _clock.PlayMusic();
                    _effects = new FalseWordEffectService(_model, _clock);
                    _effects.Requested += (kind, time) => Requests[kind] = Requests.TryGetValue(kind, out int count) ? count + 1 : 1;
                    _bullicio = new BullicioSessionService(_config.Bullicio, _model, _clock, _effects);
                    _screamerView = new ScreamerPresenter(_config, _model, _clock, _effects);
                    _glitchView = new GlitchPresenter(_config, _model, _clock, _effects);
                    _audioView = new BullicioPresenter(_config.Bullicio, _model, _clock, _bullicio, _effects);
                    // Comprobar al activarse: el usuario puede configurar un screamer mas corto que el intervalo del editor.
                    _effects.Requested += (kind, time) =>
                    {
                        if (Requests.Count != 3) return;
                        _arrivalOverlaysVisible = Overlay("Screamer overlay").activeSelf && Overlay("Glitch overlay").activeSelf;
                        _next = EditorApplication.timeSinceStartup;
                    };
                    _factory = new TargetFactory(scope.Container, _config, _model, _player, _clock, _effects, _bullicio);
                    delay = 0.25;
                    break;
                case 1:
                    _original = _config.WordPrefabs[0].GetComponentInChildren<TextMesh>(true).text;
                    _target = Spawn(WordTargetKind.Glitch);
                    _firstText = _target.GetComponentInChildren<TextMesh>(true).text;
                    Check(_target.Kind == WordTargetKind.Glitch && _firstText != _original, "Prefab real aparece con letras distorsionadas y tipo correcto");
                    break;
                case 2:
                    var text = _target.GetComponentInChildren<TextMesh>().text;
                    Check(text != _firstText && text.OrderBy(c => c).SequenceEqual(_original.OrderBy(c => c)), "Letras cambian durante el vuelo conservando el contenido");
                    var box = _target.GetComponent<BoxCollider>();
                    var bounds = _target.GetComponentInChildren<Renderer>().bounds;
                    Check(box.bounds.min.x <= bounds.min.x + 0.01f && box.bounds.max.x >= bounds.max.x - 0.01f
                        && box.bounds.min.y <= bounds.min.y + 0.01f && box.bounds.max.y >= bounds.max.y - 0.01f, "Collider real cubre el texto visible");
                    Vector3 center = box.transform.TransformPoint(box.center);
                    Physics.SyncTransforms();
                    Check(Physics.Raycast(_player.position, (center - _player.position).normalized, out var hit, 20)
                        && hit.collider == box, "Raycast real alcanza la palabra falsa");
                    _target.Touch(_player.position);
                    _target.Touch(_player.position);
                    Check(_target.IsResolved && Requests.Count == 0 && _model.Score == 0 && _model.Misses == 0, "Interceptar la palabra evita el efecto y no altera puntuacion");
                    Check(_model.Sanity == 100, "Interceptar palabra falsa conserva cordura incluso con clic repetido");
                    break;
                case 3:
                    foreach (var kind in new[] { WordTargetKind.Screamer, WordTargetKind.Bullicio, WordTargetKind.Glitch }) Spawn(kind, 0.6);
                    delay = 0.85;
                    break;
                case 4:
                    Check(Requests.Count == 3 && Requests.Values.All(v => v == 1), "Llegadas reales activan cada tipo exactamente una vez");
                    Check(_model.Sanity == 85, "Tres palabras falsas restan quince puntos sin dano duplicado");
                    Check(_arrivalOverlaysVisible, "Screamer y glitch se muestran juntos en Canvas reales");
                    Check(Overlay("Screamer overlay").GetComponent<Canvas>().sortingOrder > Overlay("Glitch overlay").GetComponent<Canvas>().sortingOrder, "Screamer conserva prioridad visual");
                    _noise = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(s => s.name == "Bullicio audio");
                    Check(_noise.isPlaying && _noise.clip == _config.Bullicio.Clip && _noise.spatialBlend == 0, "AudioSource independiente reproduce bullicio real");
                    var audioSamples = new float[1024];
                    _noise.GetOutputData(audioSamples, 0);
                    Check(audioSamples.All(s => !float.IsNaN(s) && !float.IsInfinity(s)), "Muestras de audio nativas sin valores invalidos");
                    Check(Mathf.Abs(_noise.GetComponent<AudioDistortionFilter>().distortionLevel - _config.Bullicio.DistortionLevel) < 0.001f && _bullicio.ProbabilityBonus > 0, "Distorsion nativa y aumento de palabras activos");
                    Check(Overlay("Glitch overlay").GetComponentsInChildren<RawImage>().All(i => !i.raycastTarget), "Glitch no captura clics");
                    Directory.CreateDirectory("Logs");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/FalseWords-screamer.png"));
                    delay = 0.1;
                    break;
                case 5:
                    _model.PauseSession(); _clock.PauseMusic(); _effects.RefreshPresentation();
                    _frozen = _clock.CurrentSongTime; _frozenGrain = Grain().GetRawTextureData<byte>().ToArray();
                    Check(!Overlay("Screamer overlay").activeSelf && !Overlay("Glitch overlay").activeSelf && !_noise.isPlaying, "Pausa inmediata oculta capas y pausa audio nativo");
                    delay = 0.4;
                    break;
                case 6:
                    Check(_clock.CurrentSongTime == _frozen && _frozenGrain.SequenceEqual(Grain().GetRawTextureData<byte>().ToArray()), "Pausa conserva reloj y textura sin regenerar estatica");
                    _model.ResumeSession(); _clock.ResumeMusic(); _effects.RefreshPresentation();
                    Check(!_noise.isPlaying && !Overlay("Glitch overlay").activeSelf, "Buffer de reanudacion mantiene efectos suspendidos");
                    delay = 0.5;
                    break;
                case 7:
                    Check(_noise.isPlaying && Overlay("Glitch overlay").activeSelf && _bullicio.ProbabilityBonus > 0, "Audio y glitch reanudan con su tiempo restante");
                    var resumedSamples = new float[1024];
                    _noise.GetOutputData(resumedSamples, 0);
                    Check(resumedSamples.Any(s => Mathf.Abs(s) > 0.00001f), "Bullicio produce salida de audio real"
                        + $" [mute={EditorUtility.audioMasterMute}, volume={AudioListener.volume}, pause={AudioListener.pause}, listeners={Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length}, clipState={_noise.clip.loadState}, time={_noise.time}]");
                    Check(!Overlay("Screamer overlay").activeSelf, "Glitch sigue visible despues de finalizar el screamer");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/FalseWords-glitch.png"));
                    break;
                case 8:
                    _factory.ClearTargets(); _effects.Clear();
                    Check(!_noise.isPlaying && !Overlay("Glitch overlay").activeSelf && !Overlay("Screamer overlay").activeSelf && _bullicio.ProbabilityBonus == 0, "Limpieza conjunta detiene audio, capas y probabilidad adicional");
                    _model.StopSession(); _clock.StopMusic(); _model.StartSession(); _clock.Initialize(null, 120); _clock.PlayMusic();
                    _effects.RefreshPresentation();
                    Check(!_noise.isPlaying && !Overlay("Glitch overlay").activeSelf, "Reiniciar reloj no reactiva efectos antiguos");
                    Check(_model.Sanity == 100 && !_model.IsGameOver && !_model.HasWon, "Reinicio de efectos restaura cordura y limpia resultados");
                    break;
                case 9:
                    _config.FalseWords.Enabled = false;
                    _target = Spawn(WordTargetKind.Glitch);
                    Check(_target.Kind == WordTargetKind.Normal && _target.GetComponentInChildren<TextMesh>().text == _original, "Palabras normales conservan texto original");
                    _target.Touch(_player.position);
                    Check(_model.Score == 100 && _model.Combo == 1, "Palabra normal conserva puntuacion despues del reinicio");
                    Check(File.Exists("Logs/FalseWords-screamer.png") && File.Exists("Logs/FalseWords-glitch.png"), "Capturas nativas de ambos efectos guardadas");
                    Finish(); return;
            }
            _next = EditorApplication.timeSinceStartup + delay;
        }
        catch (Exception error) { Errors.Add(error.ToString()); Finish(); }
    }
    private static void Cleanup()
    {
        _effects?.Clear(); _factory?.Dispose(); _screamerView?.Dispose(); _glitchView?.Dispose();
        _audioView?.Dispose(); _bullicio?.Dispose(); _effects?.Dispose(); _clock?.StopMusic();
        _factory = null; _screamerView = null; _glitchView = null; _audioView = null; _bullicio = null; _effects = null; _clock = null;
    }
    private static void Finish()
    {
        EditorApplication.update -= Step;
        try { Cleanup(); } catch (Exception error) { Errors.Add(error.ToString()); }
        Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { success = Errors.Count == 0, checks = Checks, errors = Errors,
            screenshots = new[] { Path.GetFullPath("Logs/FalseWords-screamer.png"), Path.GetFullPath("Logs/FalseWords-glitch.png") } }, true));
        EditorApplication.isPlaying = false;
    }
}


