using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelWordOriginSetup
{
    private const string Request = "Temp/NivelWordOrigin.request";
    private const string Report = "Logs/NivelWordOrigin.txt";
    static NivelWordOriginSetup() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        var action = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try { if (action == "apply") Prepare(); else Inspect(); }
        catch (Exception e) { File.WriteAllText(Report, e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Mentality/Prepare Audience Word Origins")]
    public static void Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play Mode.");
        var audience = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "pbl" || t.name.StartsWith("pbl_PublicS_", StringComparison.Ordinal)).ToArray();
        var source = audience.Single(t => t.name == "pbl");
        if (audience.Any(t => t.Cast<Transform>().Count(c => c.name == "WordOrigin") > 1))
            throw new InvalidOperationException("Duplicate WordOrigin children found.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Prepare audience word origins");
        int created = 0;
        var existing = source.Find("WordOrigin");
        // Eyes are at local y=0.619. The lower face reaches z=0.035 at mouth height.
        // Leave about 7 cm of world-space clearance in front of the face at scale 0.9.
        Vector3 localPosition = existing != null ? existing.localPosition : new Vector3(0f, 0.52f, 0.115f);
        Quaternion localRotation = existing != null ? existing.localRotation : Quaternion.identity;
        foreach (var member in audience)
        {
            var origin = member.Find("WordOrigin");
            if (origin == null)
            {
                origin = new GameObject("WordOrigin").transform;
                Undo.RegisterCreatedObjectUndo(origin.gameObject, "Create WordOrigin");
                origin.SetParent(member, false);
                created++;
            }
            Undo.RecordObject(origin, "Place WordOrigin");
            origin.localPosition = localPosition;
            origin.localRotation = localRotation;
            origin.localScale = Vector3.one;
        }
        Undo.CollapseUndoOperations(group);
        foreach (var member in audience)
        {
            var origins = member.Cast<Transform>().Where(t => t.name == "WordOrigin").ToArray();
            if (origins.Length != 1 || Vector3.Distance(origins[0].localPosition, localPosition) > 0.0001f ||
                Quaternion.Angle(origins[0].localRotation, localRotation) > 0.01f || origins[0].GetComponents<Component>().Length != 1)
                throw new InvalidOperationException("WordOrigin validation failed: " + member.name);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save scene.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, "PASS: audience=" + audience.Length + "; created=" + created +
            "; exactly one empty WordOrigin per character; local position=" + localPosition.ToString("F5") +
            "; forward=character +Z; scene saved.\n");
        Selection.activeGameObject = source.Find("WordOrigin").gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.drawGizmos = true;
            SceneView.lastActiveSceneView.LookAt(source.TransformPoint(new Vector3(0f, 0.56f, 0.04f)),
                Quaternion.LookRotation(-source.forward, source.up), 0.28f, false, true);
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawOrigin(Transform origin, GizmoType type)
    {
        if (origin.name != "WordOrigin" || origin.parent == null ||
            !(origin.parent.name == "pbl" || origin.parent.name.StartsWith("pbl_PublicS_", StringComparison.Ordinal))) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin.position, 0.015f);
        Gizmos.DrawRay(origin.position, origin.forward * 0.10f);
    }

    private static void Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play Mode.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var source = all.Single(t => t.name == "pbl");
        var log = new StringBuilder();
        foreach (var t in source.GetComponentsInChildren<Transform>(true))
        {
            log.AppendLine(t.name + "; local=" + t.localPosition.ToString("F5") + "; source local=" + source.InverseTransformPoint(t.position).ToString("F5"));
            var mesh = t.GetComponent<MeshFilter>();
            if (mesh != null) log.AppendLine("mesh=" + mesh.sharedMesh.name + "; bounds=" + mesh.sharedMesh.bounds.ToString("F5"));
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, log.ToString());
        Selection.activeGameObject = source.gameObject;
        SceneView.lastActiveSceneView?.LookAt(source.TransformPoint(new Vector3(0f, 0.59f, 0.04f)),
            Quaternion.LookRotation(-source.forward, source.up), 0.38f, false, true);
    }
}
