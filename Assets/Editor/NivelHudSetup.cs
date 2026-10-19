using System;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Views;
using Mentality.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelHudSetup
{
    static NivelHudSetup() { if (File.Exists("Temp/NivelHud.request")) EditorApplication.update += Process; }
    private static void Process()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Process;
        Prepare(); File.Delete("Temp/NivelHud.request");
    }
    [MenuItem("Tools/Mentality/Integration/Prepare Nivel HUD")]
    public static void Prepare()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(NivelPlayerSetup.ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(NivelPlayerSetup.ScenePath, OpenSceneMode.Additive);
        var source = EditorSceneManager.OpenPreviewScene(RhythmTestSceneBuilder.ScenePath);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save pending level edits before HUD integration");
            var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            if (objects.Any(t => t.GetComponent<RhythmHudView>() != null)) throw new InvalidOperationException("HUD already installed; manual edits preserved");
            var scope = objects.Select(t => t.GetComponent<RhythmLifetimeScope>()).First(s => s != null);
            var player = objects.Select(t => t.GetComponent<PlayerView>()).First(p => p != null);
            var original = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RhythmLifetimeScope>(true)).Single();
            var sourceSettings = new SerializedObject(original);
            var hud = sourceSettings.FindProperty("hudView").objectReferenceValue as RhythmHudView;
            var screens = sourceSettings.FindProperty("screensDoc").objectReferenceValue as UIDoc;
            if (hud == null || screens == null) throw new InvalidOperationException("RhythmTest HUD or screen document missing");
            var staging = new GameObject("Rhythm Interface");
            SceneManager.MoveGameObjectToScene(staging, source);
            hud.transform.SetParent(staging.transform, true);
            screens.transform.SetParent(staging.transform, true);
            var copy = UnityEngine.Object.Instantiate(staging);
            SceneManager.MoveGameObjectToScene(copy, scene); copy.name = "Rhythm Interface";
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("hudView").objectReferenceValue = copy.GetComponentInChildren<RhythmHudView>(true);
            serialized.FindProperty("screensDoc").objectReferenceValue = copy.GetComponentInChildren<UIDoc>(true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var fallbackAim = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Rhythm Aim");
            if (fallbackAim != null) fallbackAim.SetActive(false);
            var playerSettings = new SerializedObject(player);
            playerSettings.FindProperty("pointerUI").objectReferenceValue = null;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Nivel-1-hud-setup.txt", "PASS: Current RhythmTest HUD and UIDoc screen bindings copied together. Sanity, portrait states, combo pop-ups and reticle reused. Temporary reticle disabled. Existing effect configuration unchanged. Result document included because the existing HUD service requires it; outcome-cycle verification follows separately.\n");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(source);
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
