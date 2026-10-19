using System;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

[InitializeOnLoad]
public static class NivelRhythmSystemsSetup
{
    static NivelRhythmSystemsSetup()
    {
        if (File.Exists("Temp/NivelRhythmSystems.request")) EditorApplication.update += Process;
    }
    private static void Process()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Process;
        Build();
        File.Delete("Temp/NivelRhythmSystems.request");
    }
    [MenuItem("Tools/Mentality/Integration/Prepare Nivel Rhythm Systems")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var previous = SceneManager.GetActiveScene();
        var destination = SceneManager.GetSceneByPath(NivelPlayerSetup.ScenePath);
        bool opened = !destination.isLoaded;
        if (opened) destination = EditorSceneManager.OpenScene(NivelPlayerSetup.ScenePath, OpenSceneMode.Additive);
        var source = EditorSceneManager.OpenPreviewScene(RhythmTestSceneBuilder.ScenePath);
        try
        {
            if (destination.isDirty) throw new InvalidOperationException("Save pending Nivel edits before setup");
            var objects = destination.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            if (objects.Any(t => t.GetComponent<RhythmLifetimeScope>() != null))
                throw new InvalidOperationException("Rhythm systems already exist; current settings preserved");
            var player = objects.Select(t => t.GetComponent<PlayerView>()).First(p => p != null);
            var original = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RhythmLifetimeScope>(true)).Single();
            var originalSerialized = new SerializedObject(original);
            var originalConfig = (RhythmGameConfig)originalSerialized.FindProperty("config").boxedValue;
            var clone = UnityEngine.Object.Instantiate(original.gameObject);
            SceneManager.MoveGameObjectToScene(clone, destination);
            clone.name = "Rhythm Systems";
            var scope = clone.GetComponent<RhythmLifetimeScope>();
            scope.parentReference = ParentReference.Create<PlayerLifetimeScope>();
            scope.enabled = true;
            scope.autoRun = true;
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("playerTarget").objectReferenceValue = player.CameraTransform;
            serialized.FindProperty("musicSource").objectReferenceValue = clone.GetComponent<AudioSource>();
            // HUD and result screens are connected in their dedicated integration step.
            serialized.FindProperty("hudView").objectReferenceValue = null;
            serialized.FindProperty("screensDoc").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var config = (RhythmGameConfig)new SerializedObject(scope).FindProperty("config").boxedValue;
            if (JsonUtility.ToJson(config) != JsonUtility.ToJson(originalConfig))
                throw new InvalidOperationException("Configuration changed during transfer");
            var audio = clone.GetComponent<AudioSource>();
            if (audio == null) throw new InvalidOperationException("Source rhythm systems have no AudioSource");
            audio.clip = config.AnalyzedSong.MusicClip;
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0f;
            clone.SetActive(true);
            EditorSceneManager.MarkSceneDirty(destination);
            EditorSceneManager.SaveScene(destination);
            File.WriteAllText("Logs/Nivel-1-rhythm-config.json", JsonUtility.ToJson(config, true));
            File.WriteAllText("Logs/Nivel-1-rhythm-setup.txt", "PASS: Config copied exactly from current RhythmTest; dedicated music source and player parent container connected. HUD and screen references intentionally await their integration step.\n");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(source);
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(destination, true);
        }
    }
}
