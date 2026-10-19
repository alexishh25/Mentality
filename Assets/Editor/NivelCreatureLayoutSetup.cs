using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NivelCreatureLayoutSetup
{
    static NivelCreatureLayoutSetup()
    {
        if (File.Exists("Temp/NivelCreatureLayout.request")) EditorApplication.update += Process;
    }
    private static void Process()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Process;
        Prepare();
        File.Delete("Temp/NivelCreatureLayout.request");
    }
    [MenuItem("Tools/Mentality/Integration/Prepare Nivel Creature Grounds")]
    public static void Prepare()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(NivelPlayerSetup.ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(NivelPlayerSetup.ScenePath, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save pending Nivel edits before preparing creature grounds");
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var scope = transforms.Select(t => t.GetComponent<RhythmLifetimeScope>()).First(v => v != null);
            var settings = new SerializedObject(scope);
            var config = (RhythmGameConfig)settings.FindProperty("config").boxedValue;
            var origin = transforms.Single(t => t.name == "Gameplay Origin");
            var renderers = transforms.SelectMany(t => t.GetComponents<Renderer>())
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)
                    && r.transform.root.name != "Rhythm Player").ToArray();
            var floors = renderers.Where(r => r.name == "BaseT" || (r.name == "New Game Object" && r.transform.parent == null)).ToArray();
            if (floors.Length < 2) throw new InvalidOperationException("Expected arena floor and audience steps");
            NivelWordLayoutSetup.OpenGeometry(renderers);
            var positions = new List<Vector3>(); var arcs = new List<float>(); var log = new List<string>();
            foreach (float nominal in new[] { -60f, -30f, 0f, 30f, 60f })
            {
                bool found = false;
                foreach (float offset in new[] { 0f, -5f, 5f, -10f, 10f, -15f, 15f })
                {
                    float yaw = nominal + offset;
                    if (Mathf.Abs(yaw) > 75f) continue;
                    for (float distance = config.Creatures.Distance; distance >= 3f; distance -= 0.5f)
                    {
                        var direction = Quaternion.Euler(0f, origin.eulerAngles.y + yaw, 0f) * Vector3.forward;
                        var point = origin.position + direction * distance;
                        var ray = new Ray(new Vector3(point.x, origin.position.y + 0.1f, point.z), Vector3.down);
                        bool supported = false; float highest = float.NegativeInfinity;
                        foreach (var floor in floors)
                            if (NivelWordLayoutSetup.Ground(floor, ray, out var hit) && hit.normal.y > 0.65f && hit.point.y > highest)
                            { point.y = hit.point.y + 0.02f; highest = hit.point.y; supported = true; }
                        if (!supported || positions.Any(p => Vector3.Distance(p, point) < 2.4f)) continue;
                        var body = new Bounds(point + Vector3.up * 1.23f, new Vector3(2.4f, 2.34f, 2.4f));
                        if (!NivelWordLayoutSetup.VolumeClear(body, renderers)) continue;
                        var head = point + Vector3.up * 1.9f;
                        bool visible = true;
                        for (int i = 1; i < 20; i++)
                            if (!NivelWordLayoutSetup.VolumeClear(new Bounds(Vector3.Lerp(origin.position, head, i / 20f), Vector3.one * 0.12f), renderers))
                            { visible = false; break; }
                        if (!visible) continue;
                        foreach (float arc in new[] { 0f })
                            if (ShotClear(point + Vector3.up * 1.392f, origin.position, arc, config, renderers))
                            {
                                positions.Add(point); arcs.Add(arc);
                                log.Add("slot=" + nominal + "; actual yaw=" + yaw + "; distance=" + distance + "; ground=" + point + "; shot arc=" + arc);
                                found = true; break;
                            }
                        if (found) break;
                    }
                    if (found) break;
                }
                if (!found) { log.Add("NO CLEAR GROUND FOR SLOT " + nominal); break; }
            }
            File.WriteAllLines("Logs/Nivel-1-creature-grounds.txt", log);
            if (positions.Count != 5) throw new InvalidOperationException("Five clear creature grounds required; inspect ground report");
            var holder = origin.Find("Creature Grounds")?.gameObject ?? new GameObject("Creature Grounds");
            holder.transform.SetParent(origin, false);
            var layout = holder.GetComponent<CreaturePlacementLayout>() ?? holder.AddComponent<CreaturePlacementLayout>();
            var points = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                var point = holder.transform.Find("Ground " + (i + 1)) ?? new GameObject("Ground " + (i + 1)).transform;
                point.SetParent(holder.transform, true); point.position = positions[i]; points[i] = point;
            }
            layout.Configure(points);
            settings.FindProperty("creaturePlacementLayout").objectReferenceValue = layout;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.AppendAllText("Logs/Nivel-1-creature-grounds.txt", "PASS: Real floor surfaces, empty body space, player sightlines and both volley sides checked. Musical cadence and burst sizes unchanged.\n");
        }
        finally
        {
            NivelWordLayoutSetup.CloseGeometry();
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
    private static bool ShotClear(Vector3 mouth, Vector3 player, float arc, RhythmGameConfig config, Renderer[] renderers)
    {
        var direction = (mouth - player).normalized;
        float distance = Vector3.Distance(mouth, player);
        float halfWidth = distance * Mathf.Tan(config.Placement.TargetAngularWidthDegrees * Mathf.Deg2Rad * 0.5f);
        foreach (float side in new[] { -1f, 0f, 1f })
            foreach (float evade in new[] { -config.Evasion.MaximumAngleDegrees, 0f, config.Evasion.MaximumAngleDegrees })
                for (int i = 0; i <= 30; i++)
                {
                    float p = i / 30f;
                    var point = Vector3.Lerp(mouth, player + direction * config.ArrivalDistance, p);
                    float fan = side * 18f * Mathf.Sin(Mathf.PI * p) + evade;
                    point = player + Quaternion.AngleAxis(fan, Vector3.up) * (point - player);
                    point += Vector3.up * (arc * Mathf.Sin(Mathf.PI * p));
                    if (!NivelWordLayoutSetup.VolumeClear(new Bounds(point, new Vector3(halfWidth * 2f + 0.1f, halfWidth * 0.6f + 0.1f, halfWidth * 2f + 0.1f)), renderers)) return false;
                }
        return true;
    }
}
