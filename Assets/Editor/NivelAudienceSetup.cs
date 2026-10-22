using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelAudienceSetup
{
    private const string Request = "Temp/NivelAudience.request";
    private const string Report = "Logs/NivelAudience.txt";

    static NivelAudienceSetup() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        var action = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try { Run(action == "apply"); }
        catch (Exception e) { File.WriteAllText(Report, e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Mentality/Populate PublicS with pbl")]
    public static void Populate() { Run(true); }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    private static void Run(bool apply)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm") throw new InvalidOperationException("Open Nivel-1_Rhythm first.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var source = all.Single(t => t.name == "pbl");
        // Match the actual chair mesh, including unpacked objects named default.
        var chairs = all.Where(t =>
        {
            var filter = t.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null &&
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(filter.sharedMesh)) == "e81ce360204fdbc48837f2a84862164a";
        }).ToArray();
        if (chairs.Length == 0) throw new InvalidOperationException("No PublicS chairs found.");
        var reference = chairs.OrderBy(t => Vector3.Distance(t.position, source.position)).First();
        var offset = reference.InverseTransformPoint(source.position);
        var rotation = Quaternion.Inverse(reference.rotation) * source.rotation;
        var scale = new Vector3(source.lossyScale.x / reference.lossyScale.x,
            source.lossyScale.y / reference.lossyScale.y, source.lossyScale.z / reference.lossyScale.z);
        var log = new StringBuilder();
        log.AppendLine("scene=" + scene.path + "; dirty=" + scene.isDirty);
        log.AppendLine("source=" + PathOf(source) + "; position=" + source.position.ToString("F5") + "; rotation=" + source.eulerAngles.ToString("F5"));
        log.AppendLine("reference=" + PathOf(reference) + "; position=" + reference.position.ToString("F5") + "; rotation=" + reference.eulerAngles.ToString("F5"));
        log.AppendLine("chairs=" + chairs.Length + "; seat offset=" + offset.ToString("F5") + "; relative rotation=" + rotation.eulerAngles.ToString("F5"));
        foreach (var c in chairs.OrderBy(t => Vector3.Distance(t.position, source.position)).Take(5))
            log.AppendLine("nearest=" + PathOf(c) + "; distance=" + Vector3.Distance(c.position, source.position));
        var occupants = all.Where(t => t == source || t.name.StartsWith("pbl_PublicS_", StringComparison.Ordinal)).ToArray();
        var assignments = new Dictionary<Transform, Transform>();
        foreach (var occupant in occupants)
        {
            var candidates = occupant == source ? new[] { reference } : chairs.Where(c =>
                c == occupant.parent || c.IsChildOf(occupant.parent)).ToArray();
            if (candidates.Length == 0) throw new InvalidOperationException("Cannot associate occupant with a chair: " + PathOf(occupant));
            var chair = candidates.OrderBy(c => Vector3.Distance(occupant.position, c.TransformPoint(offset))).First();
            if (assignments.ContainsKey(chair)) throw new InvalidOperationException("Multiple occupants associated with " + PathOf(chair));
            assignments.Add(chair, occupant);
        }
        var empty = chairs.Where(c => !assignments.ContainsKey(c)).ToArray();
        log.AppendLine("occupied=" + (chairs.Length - empty.Length) + "; empty=" + empty.Length);
        foreach (var c in empty)
        {
            var nearest = occupants.OrderBy(p => Vector3.Distance(p.position, c.TransformPoint(offset))).First();
            log.AppendLine("empty chair=" + PathOf(c) + "; nearest=" + PathOf(nearest) + "; delta=" + Vector3.Distance(nearest.position, c.TransformPoint(offset)));
        }
        if (apply)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Populate PublicS with pbl");
            int created = 0;
            int adjusted = 0;
            foreach (var pair in assignments.Where(p => p.Value != source))
            {
                var chair = pair.Key;
                var occupant = pair.Value;
                if (Vector3.Distance(occupant.position, chair.TransformPoint(offset)) < 0.001f &&
                    Quaternion.Angle(occupant.rotation, chair.rotation * rotation) < 0.01f) continue;
                Undo.SetTransformParent(occupant, chair, "Align pbl to chair mesh");
                Undo.RecordObject(occupant, "Align pbl to chair mesh");
                occupant.localPosition = offset;
                occupant.localRotation = rotation;
                occupant.localScale = scale;
                adjusted++;
            }
            int suffix = occupants.Length;
            foreach (var chair in empty)
            {
                var copy = UnityEngine.Object.Instantiate(source.gameObject, chair, false);
                Undo.RegisterCreatedObjectUndo(copy, "Seat pbl");
                do { copy.name = "pbl_PublicS_" + (++suffix).ToString("D3"); }
                while (all.Any(t => t.name == copy.name));
                created++;
                copy.transform.localPosition = offset;
                copy.transform.localRotation = rotation;
                copy.transform.localScale = scale;
                if (Vector3.Distance(copy.transform.position, chair.TransformPoint(offset)) > 0.001f ||
                    Quaternion.Angle(copy.transform.rotation, chair.rotation * rotation) > 0.01f)
                    throw new InvalidOperationException("Seat placement verification failed: " + PathOf(chair));
            }
            Undo.CollapseUndoOperations(group);
            var finalOccupants = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t == source || t.name.StartsWith("pbl_PublicS_", StringComparison.Ordinal)).ToArray();
            int remaining = chairs.Count(c => !finalOccupants.Any(p => Vector3.Distance(p.position, c.TransformPoint(offset)) < 0.1f));
            if (remaining != 0) throw new InvalidOperationException("Empty chairs remain: " + remaining);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene could not be saved.");
            log.AppendLine("PASS: created=" + created + "; adjusted=" + adjusted + "; total seated=" + finalOccupants.Length + "; empty=" + remaining + "; scene saved.");
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, log.ToString());
    }
}
