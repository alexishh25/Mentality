using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelAudienceEmitterSetup
{
    private const string Request = "Temp/NivelAudienceEmitters.request";
    private const string Report = "Logs/NivelAudienceEmitters.txt";
    static NivelAudienceEmitterSetup() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        File.Delete(Request);
        try { Prepare(); }
        catch (Exception e) { File.WriteAllText(Report, e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Mentality/Prepare Audience Word Emitters")]
    public static void Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play Mode.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var audience = all.Where(t => t.name == "pbl" || t.name.StartsWith("pbl_PublicS_", StringComparison.Ordinal))
            .OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
        if (audience.Length != 131 || audience.Any(t => t.Find("WordOrigin") == null))
            throw new InvalidOperationException("Expected 131 audience members with WordOrigin.");
        var scope = all.Select(t => t.GetComponent<RhythmLifetimeScope>()).Single(t => t != null);
        var origin = all.Single(t => t.name == "Gameplay Origin");
        if (all.Count(t => t.GetComponent<AudienceWordEmitterRegistry>() != null) > 1)
            throw new InvalidOperationException("Multiple audience registries found.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Register audience word emitters");
        int created = 0;
        var members = new List<AudienceWordEmitter>();
        foreach (var character in audience)
        {
            var emitter = character.GetComponent<AudienceWordEmitter>();
            if (emitter == null) { emitter = Undo.AddComponent<AudienceWordEmitter>(character.gameObject); created++; }
            Undo.RecordObject(emitter, "Link WordOrigin");
            emitter.Configure(character.Find("WordOrigin"));
            EditorUtility.SetDirty(emitter);
            members.Add(emitter);
        }
        var registry = all.Select(t => t.GetComponent<AudienceWordEmitterRegistry>()).SingleOrDefault(t => t != null);
        if (registry == null)
        {
            var holder = new GameObject("Audience Word Emitters");
            Undo.RegisterCreatedObjectUndo(holder, "Create audience registry");
            holder.transform.SetParent(origin, false);
            registry = Undo.AddComponent<AudienceWordEmitterRegistry>(holder);
        }
        Undo.RecordObject(registry, "Register audience");
        registry.Configure(members.ToArray());
        EditorUtility.SetDirty(registry);
        var settings = new SerializedObject(scope);
        settings.FindProperty("audienceWordEmitters").objectReferenceValue = registry;
        settings.ApplyModifiedProperties();
        var available = new List<AudienceWordEmitter>();
        registry.GetAvailableEmitters(available);
        int availableBefore = available.Count;
        var probe = members.First(t => t.CanEmit);
        try
        {
            probe.EmissionEnabled = false;
            registry.GetAvailableEmitters(available);
            if (available.Contains(probe) || available.Count != availableBefore - 1)
                throw new InvalidOperationException("Per-character emission toggle failed.");
        }
        finally { probe.EmissionEnabled = true; }
        try
        {
            probe.enabled = false;
            registry.GetAvailableEmitters(available);
            if (available.Contains(probe) || available.Count != availableBefore - 1)
                throw new InvalidOperationException("Disabled emitter was still available.");
        }
        finally { probe.enabled = true; }
        bool duplicateRejected = false;
        try { registry.Configure(new[] { probe, probe }); }
        catch (ArgumentException) { duplicateRejected = true; }
        if (!duplicateRejected || registry.Count != 131 || registry.Emitters.Distinct().Count() != 131)
            throw new InvalidOperationException("Registry validation failed.");
        if (members.Any(e => e.WordOrigin != e.transform.Find("WordOrigin")))
            throw new InvalidOperationException("WordOrigin link mismatch.");
        registry.GetAvailableEmitters(available);
        if (available.Count != availableBefore) throw new InvalidOperationException("Toggle restoration failed.");
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save scene.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, "PASS: members=131; created=" + created + "; available=" + available.Count +
            "; unique origins=131; toggle and disabled-component filtering passed; duplicate rejection passed; scope linked; scene saved.\n");
        Selection.activeGameObject = registry.gameObject;
        EditorGUIUtility.PingObject(registry);
    }
}
