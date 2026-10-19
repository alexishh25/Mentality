using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Mentality.Gameplay.MG01;
using Mentality.UI;
using Mentality.UI.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

// Opt-in integration smoke check: run only with Temp/NivelMenuNavigation.request.
[InitializeOnLoad]
public static class NivelMenuNavigationChecks
{
    const string Running = "Nivel.MenuNavigation.Running";
    const string Menu = "Assets/Content/Scenes/UIScene.unity";
    static readonly List<string> checks = new List<string>();
    static readonly List<string> errors = new List<string>();
    static int phase, previousScope;
    static double next, deadline;

    static NivelMenuNavigationChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (File.Exists("Temp/NivelMenuNavigation.request")) EditorApplication.update += Start;
    }
    static void Start()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Start;
        File.Delete("Temp/NivelMenuNavigation.request");
        SessionState.SetString(Running + ".Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu);
        SessionState.SetBool(Running, true);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = 0; checks.Clear(); errors.Clear();
            next = EditorApplication.timeSinceStartup + 2;
            deadline = next + 90;
            Application.logMessageReceived += Log;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Running + ".Previous", ""));
            SessionState.SetBool(Running, false);
        }
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        checks.Add(message);
    }
    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) errors.Add(message);
    }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Navigation timed out");
            if (phase == 0)
            {
                var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
                Check(enabled.Length == 2 && enabled[0].path == Menu && enabled[1].path == NivelPlayerSetup.ScenePath, "Menu first and integrated level second in enabled build scenes");
                Check(SceneManager.GetActiveScene().path == Menu, "Menu starts correctly");
                var ui = UnityEngine.Object.FindFirstObjectByType<UILifetimeScope>();
                Check(ui != null && ui.Container != null, "Menu container initialized");
                Check(ui.Container.Resolve<MG01ConfigSO>().MG01SceneName == "Nivel-1_Rhythm", "Actual menu config targets integrated scene");
                ui.Container.Resolve<MenuService>().IniciarNuevaPartida();
            }
            else if (phase == 1 || phase == 2)
            {
                Check(SceneManager.GetActiveScene().path == NivelPlayerSetup.ScenePath, "Integrated level loaded, phase " + phase);
                var scopes = UnityEngine.Object.FindObjectsByType<RhythmLifetimeScope>(FindObjectsSortMode.None);
                Check(scopes.Length == 1 && scopes[0].Container != null, "Exactly one initialized rhythm scope, phase " + phase);
                if (phase == 1)
                {
                    previousScope = scopes[0].GetInstanceID();
                    scopes[0].Container.Resolve<RhythmGamePresenter>().RestartSession();
                    Check(scopes[0].Container.Resolve<RhythmGameModel>().Sanity == 100, "Restart restores sanity");
                }
                else
                {
                    Check(previousScope == scopes[0].GetInstanceID(), "Restart retains one level scope");
                    Check(scopes[0].Container.Resolve<RhythmGameModel>().IsPlaying, "Session runs after restart from menu entry");
                    Check(errors.Count == 0, "No runtime errors during menu entry and restart");
                    Finish(true); return;
                }
            }
            phase++; next = EditorApplication.timeSinceStartup + 3;
        }
        catch (Exception ex) { errors.Add(ex.ToString()); Finish(false); }
    }
    static void Finish(bool success)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Nivel-1-menu-navigation.json", JsonUtility.ToJson(new Report { success = success, checks = checks.ToArray(), errors = errors.ToArray() }, true));
        EditorApplication.ExitPlaymode();
    }
    [Serializable] class Report { public bool success; public string[] checks, errors; }
}
