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
        EditorApplication.update += Process;
    }
    private static void Process()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/NivelCreatureLayout.request")) return;
        File.Delete("Temp/NivelCreatureLayout.request");
        try { Prepare(); }
        catch (Exception e) { File.AppendAllText("Logs/Nivel-1-creature-grounds.txt", e.ToString()); Debug.LogException(e); }
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
                    && r.transform.root.name != "Rhythm Player"
                    && r.sharedMaterials.Any(m => m != null && m.renderQueue < 3000)).ToArray();
            var audience = transforms.Select(t => t.GetComponent<AudienceWordEmitter>()).Where(e => e != null).ToArray();
            var floors = renderers.Where(r => r.name == "BaseT" || (r.name.StartsWith("New Game Object", StringComparison.Ordinal) && r.bounds.size.x > 5f && r.bounds.size.z > 1f)).ToArray();
            if (floors.Length < 2) throw new InvalidOperationException("Expected arena floor and audience steps");
            NivelWordLayoutSetup.OpenGeometry(renderers);
            var positions = new List<Vector3>(); var clearances = new List<float>(); var log = new List<string>();
            float rayHeight = floors.Max(f => f.bounds.max.y) + 1f;
            foreach (float nominal in new[] { -60f, -30f, 0f, 30f, 60f })
            {
                bool found = false;
                foreach (float offset in new[] { 0f, -5f, 5f, -10f, 10f, -15f, 15f })
                {
                    float yaw = nominal + offset;
                    if (Mathf.Abs(yaw) > 75f) continue;
                    for (float distance = 10f; distance <= 22f; distance += 0.5f)
                    {
                        var direction = Quaternion.Euler(0f, origin.eulerAngles.y + yaw, 0f) * Vector3.forward;
                        var point = origin.position + direction * distance;
                        var ray = new Ray(new Vector3(point.x, rayHeight, point.z), Vector3.down);
                        bool supported = false; float highest = float.NegativeInfinity;
                        foreach (var floor in floors)
                            if (NivelWordLayoutSetup.Ground(floor, ray, out var hit) && hit.normal.y > 0.65f && hit.point.y > highest)
                            { point.y = hit.point.y + 0.02f; highest = hit.point.y; supported = true; }
                        if (!supported || positions.Any(p => Vector3.Distance(p, point) < 2.4f)) continue;
                        if (!audience.Any(e => Vector2.Distance(new Vector2(e.transform.position.x, e.transform.position.z), new Vector2(point.x, point.z)) < 3f)) continue;
                        var facing = Quaternion.LookRotation(new Vector3(origin.position.x - point.x, 0f, origin.position.z - point.z));
                        if (!NivelWordLayoutSetup.BoxClear(point + Vector3.up * 1.23f, new Vector3(1.2f, 1.17f, 0.08f), facing, renderers, out _)) continue;
                        var head = point + Vector3.up * 1.9f;
                        bool visible = NivelWordLayoutSetup.RayClear(origin.position, head, renderers, out _);
                        if (!visible) continue;
                        var mouth = point + Vector3.up * 1.392f;
                        float maximum = Vector3.Distance(mouth, origin.position) - config.ArrivalDistance - Gameplay.Rhythm.Services.AudienceWordFlight.GrowthDistance - 0.5f;
                        for (float clearance = Mathf.Max(0f, distance - 6f); clearance <= maximum; clearance += 0.5f)
                            if (NivelAudienceRouteSetup.CheckVolume(mouth, origin.position, config, renderers, true, out _, clearance,
                                Mathf.Clamp(config.Placement.MaximumSideAngle - Mathf.Abs(yaw), 0f, 18f)))
                            {
                                positions.Add(point); clearances.Add(clearance);
                                log.Add("slot=" + nominal + "; actual yaw=" + yaw + "; distance=" + distance + "; ground=" + point + "; launch clearance=" + clearance);
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
            layout.Configure(points, clearances.ToArray());
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
}
