using System;
using System.IO;
using System.Linq;
using System.Text;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelAudienceSelectionChecks
{
    private const string Request = "Temp/NivelAudienceSelection.request";
    static NivelAudienceSelectionChecks() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        File.Delete(Request);
        try { Preview(); }
        catch (Exception e) { File.WriteAllText("Logs/NivelAudienceSelection.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Mentality/Preview Audience Word Selection")]
    public static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        CheckSelectionRules();
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm") throw new InvalidOperationException("Open Nivel-1_Rhythm.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var registry = all.Select(t => t.GetComponent<AudienceWordEmitterRegistry>()).Single(t => t != null);
        var scope = all.Select(t => t.GetComponent<RhythmLifetimeScope>()).Single(t => t != null);
        var settings = new SerializedObject(scope);
        var config = (RhythmGameConfig)settings.FindProperty("config").boxedValue;
        var player = (Transform)settings.FindProperty("playerTarget").objectReferenceValue;
        var layout = (WordLaunchLayout)settings.FindProperty("wordLaunchLayout").objectReferenceValue;
        var routes = registry.GetComponent<AudienceWordRouteLayout>();
        var selector = new AudienceWordSelector(registry, config, routes);
        var log = new StringBuilder("Selection checks PASS: alternation, disabled emitters, inactive origins, angular occupancy, rotated reference, empty sector and reset.\n");
        log.AppendLine("Preview only: no words spawned; baked route filter=" + (routes != null));
        log.AppendLine("Registry members=" + registry.Count);
        for (float angle = -config.Placement.MaximumSideAngle; angle <= config.Placement.MaximumSideAngle; angle += config.Placement.LaneSpacingDegrees)
        {
            log.Append("lane=" + angle + ": ");
            for (int i = 0; i < 3; i++)
            {
                if (selector.TrySelect(angle, player.position, layout == null ? 0f : layout.transform.eulerAngles.y, null, out var member))
                {
                    Vector3 direction = member.WordOrigin.position - player.position;
                    log.Append(member.name + " (distance=" + direction.magnitude.ToString("F2") + ")");
                }
                else log.Append("no available emitter in this sector");
                if (i < 2) log.Append(" -> ");
            }
            log.AppendLine();
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/NivelAudienceSelection.txt", log.ToString());
        Selection.activeGameObject = registry.gameObject;
        Debug.Log("Audience word selection checks PASS. Preview: Logs/NivelAudienceSelection.txt");
    }

    private static void Require(bool condition, string rule)
    { if (!condition) throw new InvalidOperationException("Audience selection check failed: " + rule); }

    private static void CheckSelectionRules()
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var holder = new GameObject("Selection check");
            SceneManager.MoveGameObjectToScene(holder, preview);
            var registry = holder.AddComponent<AudienceWordEmitterRegistry>();
            var members = new AudienceWordEmitter[3];
            for (int i = 0; i < members.Length; i++)
            {
                var character = new GameObject("Emitter " + i);
                character.transform.SetParent(holder.transform, false);
                float yaw = i == 2 ? 40f : i * 4f;
                character.transform.position = Quaternion.Euler(0, yaw, 0) * Vector3.forward * 12f;
                var mouth = new GameObject("WordOrigin");
                mouth.transform.SetParent(character.transform, false);
                members[i] = character.AddComponent<AudienceWordEmitter>();
                members[i].Configure(mouth.transform);
            }
            registry.Configure(members);
            var selector = new AudienceWordSelector(registry, new RhythmGameConfig());
            Require(selector.TrySelect(0, Vector3.zero, 0, null, out var first) && first == members[0], "nearest angle");
            Require(selector.TrySelect(0, Vector3.zero, 0, null, out var second) && second == members[1], "alternate characters");
            members[0].EmissionEnabled = false;
            Require(selector.TrySelect(0, Vector3.zero, 0, null, out var enabled) && enabled == members[1], "disabled exclusion");
            members[1].WordOrigin.gameObject.SetActive(false);
            Require(!selector.TrySelect(0, Vector3.zero, 0, null, out _), "inactive origin and no available candidate");
            members[0].EmissionEnabled = true;
            members[1].WordOrigin.gameObject.SetActive(true);
            Require(!selector.TrySelect(0, Vector3.zero, 0, new[] { Vector3.forward * 6f }, out _), "occupied angular sector");
            Require(selector.TrySelect(0, Vector3.zero, 40, null, out var rotated) && rotated == members[2], "reference yaw");
            Require(!selector.TrySelect(-60, Vector3.zero, 0, null, out _), "empty sector");
            Require(!selector.TrySelect(90, Vector3.zero, 0, null, out _), "outside allowed arc");
            selector.Reset();
            Require(selector.TrySelect(0, Vector3.zero, 0, null, out var reset) && reset == first, "deterministic reset");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
