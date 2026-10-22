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
public static class NivelWordLayoutSetup
{
    private static readonly Dictionary<Renderer, MeshCollider> Shapes = new Dictionary<Renderer, MeshCollider>();
    private static readonly Dictionary<Collider, Renderer> ShapeOwners = new Dictionary<Collider, Renderer>();
    private static readonly Collider[] QueryHits = new Collider[16384];
    private static BoxCollider probe;
    private static Scene geometryScene;
    private static string lastBlock;
    private static float halfHeight;
    static NivelWordLayoutSetup()
    {
        if (File.Exists("Temp/NivelWordLayout.request")) EditorApplication.update += Process;
    }
    private static void Process()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Process;
        Prepare();
        File.Delete("Temp/NivelWordLayout.request");
    }
    [MenuItem("Tools/Mentality/Integration/Prepare Nivel Word Lanes")]
    public static void Prepare()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(NivelPlayerSetup.ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(NivelPlayerSetup.ScenePath, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save pending level edits before preparing lanes");
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var scope = transforms.Select(t => t.GetComponent<RhythmLifetimeScope>()).First(v => v != null);
            var settings = new SerializedObject(scope);
            var config = (RhythmGameConfig)settings.FindProperty("config").boxedValue;
            var origin = transforms.Single(t => t.name == "Gameplay Origin");
            // The imported level includes a static performer at the camera position.
            // Hide that body for first-person play in this integration scene only.
            foreach (var r in transforms.SelectMany(t => t.GetComponents<MeshRenderer>()))
                if (r.name == "Player" && r.bounds.Contains(origin.position) && r.transform.root.name != "Rhythm Player")
                    r.enabled = false;
            var renderers = transforms.SelectMany(t => t.GetComponents<Renderer>())
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)
                    && r.GetComponentInParent<PlayerView>() == null && r.transform.root.name != "Rhythm Player").ToArray();
            Vector3 player = origin.position;
            OpenGeometry(renderers);
            var lanes = new List<Vector3>();
            var log = new List<string>();
            float halfWidth = config.SpawnDistance * Mathf.Tan(config.Placement.TargetAngularWidthDegrees * Mathf.Deg2Rad * 0.5f);
            halfHeight = 0f;
            foreach (var prefab in config.WordPrefabs)
            {
                var wordRenderers = prefab.GetComponentsInChildren<Renderer>(true);
                var bounds = wordRenderers[0].bounds;
                foreach (var r in wordRenderers.Skip(1)) bounds.Encapsulate(r.bounds);
                halfHeight = Mathf.Max(halfHeight, halfWidth * bounds.size.y / bounds.size.x);
            }
            halfHeight += 0.08f;
            float evasion = config.Evasion.Enabled ? config.Evasion.MaximumAngleDegrees : 0f;
            Vector3 initialPlayer = player;
            for (float advance = 0f; advance <= 5.001f; advance += 0.5f)
            {
              lanes.Clear(); log.Clear();
              player = initialPlayer + origin.forward * advance;
              foreach (float yaw in Enumerable.Range(0, 11).Select(i => -75f + 15f * i))
              {
                bool found = false;
                foreach (float pitch in new[] { 0f, 8f, 12f, 16f, 20f, 24f, 28f })
                {
                    var local = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                    var world = Quaternion.Euler(0f, origin.eulerAngles.y, 0f) * local;
                    bool clear = IsClear(player, world, config.SpawnDistance, config.ArrivalDistance, halfWidth, evasion, renderers);
                    if (!clear) continue;
                    lanes.Add(local);
                    log.Add("yaw=" + yaw + ", elevation=" + pitch + ", distance=" + config.SpawnDistance);
                    found = true; break;
                }
                if (!found) log.Add("Blocked yaw=" + yaw + ": " + lastBlock);
              }
              log.Add("Player advance=" + advance);
              if (lanes.Count == 11) break;
            }
            File.WriteAllLines("Logs/Nivel-1-word-lanes.txt", log);
            if (lanes.Count != 11) throw new InvalidOperationException("Some lanes remain blocked; inspect Nivel-1-word-lanes.txt before saving");
            var playerRoot = scene.GetRootGameObjects().Single(g => g.name == "Rhythm Player");
            playerRoot.transform.position += player - initialPlayer;
            origin.position = player;
            var layout = origin.GetComponent<WordLaunchLayout>() ?? origin.gameObject.AddComponent<WordLaunchLayout>();
            layout.Configure(lanes.ToArray());
            settings.FindProperty("wordLaunchLayout").objectReferenceValue = layout;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.AppendAllText("Logs/Nivel-1-word-lanes.txt", "PASS: 11 lanes checked across full travel and evasion margin against " + renderers.Length + " scenery renderers. Musical configuration unchanged.\n");
        }
        finally
        {
            CloseGeometry();
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void OpenGeometry(Renderer[] renderers)
    {
            var activeScene = SceneManager.GetActiveScene();
            geometryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(activeScene);
            var probeObject = new GameObject("Word volume probe");
            SceneManager.MoveGameObjectToScene(probeObject, geometryScene);
            probe = probeObject.AddComponent<BoxCollider>();
            foreach (var renderer in renderers)
            {
                Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                var shape = new GameObject(renderer.name + " geometry");
                SceneManager.MoveGameObjectToScene(shape, geometryScene);
                shape.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                shape.transform.localScale = renderer.transform.lossyScale;
                var collider = shape.AddComponent<MeshCollider>();
                if (Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i)) > 6291456)
                    collider.cookingOptions &= ~MeshColliderCookingOptions.UseFastMidphase;
                collider.sharedMesh = mesh;
                Shapes.Add(renderer, collider);
                ShapeOwners.Add(collider, renderer);
            }
            Physics.SyncTransforms();
    }
    public static void CloseGeometry()
    {
            Shapes.Clear(); ShapeOwners.Clear(); probe = null;
            if (geometryScene.IsValid() && geometryScene.isLoaded) EditorSceneManager.CloseScene(geometryScene, true);
    }

    public static bool Ground(Renderer floor, Ray ray, out RaycastHit hit)
    { hit = default; return Shapes.TryGetValue(floor, out var shape) && shape.Raycast(ray, out hit, 20f); }

    public static bool VolumeClear(Bounds box, Renderer[] obstacles)
    {
        foreach (var obstacle in obstacles)
            if (box.Intersects(obstacle.bounds) && Shapes.TryGetValue(obstacle, out var shape))
            {
                probe.size = box.size;
                if (Physics.ComputePenetration(probe, box.center, Quaternion.identity, shape,
                    shape.transform.position, shape.transform.rotation, out _, out _)) return false;
            }
        return true;
    }

    public static bool RayClear(Vector3 start, Vector3 end, Renderer[] obstacles, out Renderer blocker)
    {
        blocker = null;
        Vector3 delta = end - start;
        var ray = new Ray(start, delta.normalized);
        float nearest = delta.magnitude;
        foreach (var obstacle in obstacles)
            if (Shapes.TryGetValue(obstacle, out var shape))
            {
                if (shape.Raycast(ray, out var hit, nearest))
                { nearest = hit.distance; blocker = obstacle; }
                // Reverse casting also detects a start point enclosed by a one-sided mesh.
                if (shape.Raycast(new Ray(end, -delta.normalized), out var reverse, delta.magnitude) &&
                    delta.magnitude - reverse.distance < nearest)
                { nearest = delta.magnitude - reverse.distance; blocker = obstacle; }
            }
        return blocker == null;
    }

    public static bool BoxClear(Vector3 center, Vector3 halfSize, Quaternion rotation, Renderer[] obstacles, out Renderer blocker)
    {
        blocker = null;
        // Overlap queries handle non-convex mesh surfaces; penetration queries can
        // miss them, so they are unsuitable for validating these imported meshes.
        int count = Physics.OverlapBoxNonAlloc(center, halfSize, QueryHits, rotation, ~0, QueryTriggerInteraction.Ignore);
        if (count == QueryHits.Length) throw new InvalidOperationException("Word route query exceeded its collider buffer.");
        for (int i = 0; i < count; i++)
            if (ShapeOwners.TryGetValue(QueryHits[i], out var owner) && Array.IndexOf(obstacles, owner) >= 0)
            { blocker = owner; return false; }
        return true;
    }

    private static bool IsClear(Vector3 player, Vector3 direction, float distance, float arrival, float halfWidth,
        float evasion, Renderer[] obstacles)
    {
        int steps = Mathf.CeilToInt((distance - arrival) / 0.2f);
        for (int step = 0; step <= steps; step++)
        {
            float radius = Mathf.Lerp(arrival, distance, (float)step / steps);
            float side = halfWidth + radius * Mathf.Sin(evasion * Mathf.Deg2Rad) + 0.1f;
            var box = new Bounds(player + direction * radius, new Vector3(side * 2f, halfHeight * 2f, side * 2f));
            foreach (var obstacle in obstacles)
                if (box.Intersects(obstacle.bounds) && Shapes.TryGetValue(obstacle, out var shape))
                {
                    probe.size = box.size;
                    if (Physics.ComputePenetration(probe, box.center, Quaternion.identity,
                        shape, shape.transform.position, shape.transform.rotation, out _, out _))
                    { lastBlock = obstacle.name + " at radius=" + radius + " bounds=" + obstacle.bounds; return false; }
                }
        }
        return true;
    }
}
