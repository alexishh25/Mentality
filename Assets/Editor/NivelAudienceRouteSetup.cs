using System;
using System.Collections.Generic;
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
public static class NivelAudienceRouteSetup
{
    private const string Request = "Temp/NivelAudienceRoutes.request";
    private const string Report = "Logs/NivelAudienceRoutes.md";
    private const float Step = 0.05f;
    private const float Margin = 0.01f;
    static NivelAudienceRouteSetup() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        File.Delete(Request);
        try { Prepare(); }
        catch (Exception e) { File.WriteAllText(Report, e.ToString()); Debug.LogException(e); }
    }
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    [MenuItem("Tools/Mentality/Validate Audience Word Routes")]
    public static void Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play Mode.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var registry = all.Select(t => t.GetComponent<AudienceWordEmitterRegistry>()).Single(t => t != null);
        var scope = all.Select(t => t.GetComponent<RhythmLifetimeScope>()).Single(t => t != null);
        var settings = new SerializedObject(scope);
        var config = (RhythmGameConfig)settings.FindProperty("config").boxedValue;
        var player = (Transform)settings.FindProperty("playerTarget").objectReferenceValue;
        var launch = (WordLaunchLayout)settings.FindProperty("wordLaunchLayout").objectReferenceValue;
        if (config.WordPrefabs.Length == 0 || config.WordPrefabs.Any(p => p == null || p.GetComponent<BoxCollider>() == null))
            throw new InvalidOperationException("Word prefabs need box colliders.");
        // Match visible mesh geometry, not imported broad colliders or transparent flame quads.
        var obstacles = all.SelectMany(t => t.GetComponents<MeshRenderer>()).Where(r => r.enabled &&
            r.gameObject.activeInHierarchy && r.transform.root.name != "Rhythm Player" &&
            r.GetComponentInParent<PlayerView>() == null && r.GetComponent<MeshFilter>()?.sharedMesh != null &&
            r.sharedMaterials.Any(m => m != null && m.renderQueue < 3000)).Cast<Renderer>().ToArray();
        var results = new List<AudienceWordRouteLayout.Route>();
        int straightClear = 0;
        var log = new StringBuilder("# Recorridos de palabras desde el público\n\n");
        log.AppendLine("Comprobación conservadora sobre las mallas visibles, incluidos el propio emisor, otros personajes, sillas, columnas y suelo.");
        log.AppendLine("Salida al 8% del tamaño final, crecimiento durante 1,5 m y evasión progresiva a partir de 2,5 m. Barrido radial cada 0,05 m, margen de 1 cm y volumen de todos los prefabs.\n");
        log.AppendLine("| Personaje | Línea visible | Recto sin evasión | Volumen con evasión | Primer obstáculo con evasión |\n|---|---|---|---|---|");
        try
        {
            // Control cases exercise the same mesh query used for the real level.
            CheckGeometryQueries();
            NivelWordLayoutSetup.OpenGeometry(obstacles);
            foreach (var emitter in registry.Emitters)
            {
                Vector3 start = emitter.WordOrigin.position;
                Vector3 direction = (start - player.position).normalized;
                Vector3 end = player.position + direction * config.ArrivalDistance;
                bool visible = NivelWordLayoutSetup.RayClear(start, end, obstacles, out var blocker);
                string reason = blocker == null ? "" : PathOf(blocker.transform);
                bool straight = visible && CheckVolume(start, player.position, config, obstacles, false, out _);
                if (straight) straightClear++;
                bool clear = visible && CheckVolume(start, player.position, config, obstacles, true, out reason);
                float clearance = 0f;
                if (visible && !clear)
                {
                    // Keep rear-row words small until they pass the foreground audience.
                    float candidate = Mathf.Max(0.5f, Vector3.Distance(start, player.position) - 6f);
                    float maximum = Vector3.Distance(start, player.position) - config.ArrivalDistance - AudienceWordFlight.GrowthDistance - 0.5f;
                    for (; candidate <= maximum + 0.001f; candidate += 0.5f)
                        if (CheckVolume(start, player.position, config, obstacles, true, out reason, candidate))
                        { clearance = candidate; clear = true; break; }
                }
                string row = "Fila1";
                for (var parent = emitter.transform.parent; parent != null; parent = parent.parent)
                    if (parent.name.StartsWith("Fila", StringComparison.Ordinal)) { row = parent.name; break; }
                var result = new AudienceWordRouteLayout.Route { Emitter = emitter, Origin = start,
                    Visible = visible, Clear = clear, Obstacle = reason, LaunchClearance = clearance, Row = row };
                results.Add(result);
                log.AppendLine("| " + emitter.name + " | " + (visible ? "Sí" : "No") + " | " + (straight ? "Sí" : "No") + " | " + (clear ? "Sí" : "No") + " | " + reason + " |");
            }
        }
        finally { NivelWordLayoutSetup.CloseGeometry(); }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Validate audience word routes");
        var layout = registry.GetComponent<AudienceWordRouteLayout>();
        if (layout == null) layout = Undo.AddComponent<AudienceWordRouteLayout>(registry.gameObject);
        Undo.RecordObject(layout, "Store audience routes");
        layout.Configure(player.position, config, results.ToArray(), obstacles.Cast<MeshRenderer>().ToArray());
        EditorUtility.SetDirty(layout);
        settings.FindProperty("audienceWordRoutes").objectReferenceValue = layout;
        settings.ApplyModifiedProperties();
        var selector = new AudienceWordSelector(registry, config, layout);
        log.AppendLine("\n## Cobertura por carril\n\n| Carril | Emisor seleccionado con recorrido válido |\n|---|---|");
        int lanes = 0;
        for (float angle = -config.Placement.MaximumSideAngle; angle <= config.Placement.MaximumSideAngle; angle += config.Placement.LaneSpacingDegrees)
        {
            bool found = selector.TrySelect(angle, player.position, launch == null ? 0 : launch.transform.eulerAngles.y, null, out var selected);
            if (found) lanes++;
            log.AppendLine("| " + angle + "° | " + (found ? selected.name : "Sin recorrido válido") + " |");
        }
        foreach (var result in results)
            if (layout.IsClear(result.Emitter, player.position, config) != result.Clear)
                throw new InvalidOperationException("Route filter mismatch.");
        if (results.Any(r => layout.IsClear(r.Emitter, player.position + Vector3.right * 0.1f, config)))
            throw new InvalidOperationException("Moved-player invalidation failed.");
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save scene.");
        log.AppendLine("\n**Resultado:** " + results.Count + " emisores revisados; " + results.Count(r => r.Visible) +
            " con línea visible; " + straightClear + " con volumen recto libre; " + layout.ClearCount + " con volumen y evasión libres; " + lanes + " carriles con candidato válido.");
        foreach (var row in results.GroupBy(r => r.Row))
            log.AppendLine("\n" + row.Key + ": " + row.Count(r => r.Clear) + "/" + row.Count() + " emisores aptos.");
        log.AppendLine("\nLas filas posteriores mantienen el tamaño pequeño hasta despejar el público; cada distancia de crecimiento se valida con las mallas del escenario.");
        log.Insert(log.ToString().IndexOf("| Personaje", StringComparison.Ordinal),
            "**Resumen:** " + results.Count(r => r.Visible) + "/" + results.Count + " líneas visibles; " + straightClear +
            " recorridos rectos libres; " + layout.ClearCount + " recorridos con evasión libres. Son resultados conservadores, no una prueba de que toda trayectoria posible esté obstruida.\n\n");
        log.AppendLine("\nPASS: controles de geometría, filtro del selector e invalidación al mover el jugador. Escena guardada.");
        log.AppendLine("\nLa comprobación corresponde al escenario estático actual y al perfil AudienceWordFlight. Repetir Tools → Mentality → Validate Audience Word Routes tras cambiar geometría, prefabs, orígenes o configuración.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, log.ToString());
        Selection.activeGameObject = registry.gameObject;
        Debug.Log("Audience routes: " + layout.ClearCount + "/" + results.Count + " clear; " + lanes + " lanes. Report: " + Report);
    }

    private static Vector3 HalfSize(GameObject prefab, float distance, RhythmPlacementSettings placement)
    {
        var box = prefab.GetComponent<BoxCollider>();
        Vector3 scale = prefab.transform.lossyScale;
        Vector3 size = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        Vector3 center = Vector3.Scale(box.center, scale);
        var half = new Vector3(Mathf.Abs(center.x), Mathf.Abs(center.y), Mathf.Abs(center.z)) + size * 0.5f;
        float width = size.x;
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var bounds = renderer.bounds;
            Vector3 relative = bounds.center - prefab.transform.position;
            half = Vector3.Max(half, new Vector3(Mathf.Abs(relative.x), Mathf.Abs(relative.y), Mathf.Abs(relative.z)) + bounds.extents);
            width = Mathf.Max(width, bounds.size.x);
        }
        return placement.Enabled ? half * RhythmPlacementPlanner.ReadabilityScale(distance, distance, width, placement.TargetAngularWidthDegrees) : half;
    }

    public static bool CheckVolume(Vector3 origin, Vector3 player, RhythmGameConfig config, Renderer[] obstacles, bool includeEvasion, out string reason,
        float clearance = 0f, float extraAngle = 0f)
    {
        reason = "";
        float distance = Vector3.Distance(origin, player);
        if (distance <= config.ArrivalDistance) { reason = "Dentro del radio de llegada"; return false; }
        var half = Vector3.zero;
        foreach (var prefab in config.WordPrefabs) half = Vector3.Max(half, HalfSize(prefab, distance, config.Placement));
        float maximumEvasion = includeEvasion && config.Evasion != null && config.Evasion.Enabled ? config.Evasion.MaximumAngleDegrees : 0f;
        maximumEvasion += extraAngle;
        int steps = Mathf.CeilToInt((distance - config.ArrivalDistance) / Step);
        Vector3 direction = (origin - player).normalized;
        var corridor = new Bounds(player, Vector3.zero);
        corridor.Encapsulate(origin);
        corridor.Expand((half.magnitude + distance * Mathf.Sin(maximumEvasion * Mathf.Deg2Rad) + Step) * 2f);
        var nearby = obstacles.Where(r => corridor.Intersects(r.bounds)).ToArray();
        for (int step = 0; step <= steps; step++)
        {
            float radius = Mathf.Lerp(distance, config.ArrivalDistance, (float)step / steps);
            float travelled = distance - radius;
            float evasion = maximumEvasion * AudienceWordFlight.EvasionAt(travelled + Step * 0.5f, clearance);
            int angularSteps = Mathf.Max(1, Mathf.CeilToInt(evasion * 4f));
            float angularStep = evasion * 2f / angularSteps;
            Vector3 sampleHalf = half * AudienceWordFlight.ScaleAt(travelled + Step * 0.5f, clearance);
            if (extraAngle > 0f) sampleHalf.y += radius * Mathf.Abs(direction.y) * (1f - Mathf.Cos(evasion * Mathf.Deg2Rad));
            sampleHalf += Vector3.one * (Margin + Step * 0.5f + (radius + half.magnitude) * Mathf.Sin(angularStep * 0.5f * Mathf.Deg2Rad));
            for (int angular = 0; angular <= angularSteps; angular++)
            {
                float angle = Mathf.Lerp(-evasion, evasion, (float)angular / angularSteps);
                // Matches TargetPresenter's evasion plane, including its vertical component.
                float radians = angle * Mathf.Deg2Rad;
                Vector3 tangent = new Vector3(-direction.z, 0, direction.x);
                Vector3 offset = (direction * Mathf.Cos(radians) + tangent * Mathf.Sin(radians)) * radius;
                var center = player + offset;
                var rotation = Quaternion.LookRotation(offset, Vector3.up);
                if (!NivelWordLayoutSetup.BoxClear(center, sampleHalf, rotation, nearby, out var blocker))
                { reason = PathOf(blocker.transform) + " (radio " + radius.ToString("F2") + " m; evasión " + angle.ToString("F1") + "°)"; return false; }
            }
        }
        return true;
    }

    private static void CheckGeometryQueries()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Route check obstacle";
        cube.transform.position = new Vector3(10000, 10000, 10000);
        var renderer = cube.GetComponent<MeshRenderer>();
        try
        {
            NivelWordLayoutSetup.OpenGeometry(new[] { renderer });
            var center = cube.transform.position;
            if (NivelWordLayoutSetup.BoxClear(center + Vector3.forward * 0.49f, Vector3.one * 0.1f, Quaternion.identity, new[] { renderer }, out _))
                throw new InvalidOperationException("Blocked volume control failed.");
            if (!NivelWordLayoutSetup.BoxClear(center + Vector3.up * 2f, Vector3.one * 0.1f, Quaternion.identity, new[] { renderer }, out _))
                throw new InvalidOperationException("Empty route control failed.");
            if (NivelWordLayoutSetup.RayClear(center - Vector3.forward * 2f, center + Vector3.forward * 2f, new[] { renderer }, out _))
                throw new InvalidOperationException("Occluded visibility control failed.");
            if (!NivelWordLayoutSetup.RayClear(center + Vector3.up * 2f, center + Vector3.up * 3f, new[] { renderer }, out _))
                throw new InvalidOperationException("Clear visibility control failed.");
            CheckCacheInvalidation(renderer);
        }
        finally { NivelWordLayoutSetup.CloseGeometry(); UnityEngine.Object.DestroyImmediate(cube); }
    }

    private static void CheckCacheInvalidation(MeshRenderer obstacle)
    {
        var holder = new GameObject("Temporary route cache check");
        try
        {
            var mouth = new GameObject("WordOrigin").transform;
            mouth.SetParent(holder.transform, false);
            mouth.position = Vector3.forward * 10f;
            var emitter = holder.AddComponent<AudienceWordEmitter>();
            emitter.Configure(mouth);
            var layout = holder.AddComponent<AudienceWordRouteLayout>();
            var config = new RhythmGameConfig();
            layout.Configure(Vector3.zero, config, new[] { new AudienceWordRouteLayout.Route {
                Emitter = emitter, Origin = mouth.position, Visible = true, Clear = true } }, new[] { obstacle });
            if (!layout.IsClear(emitter, Vector3.zero, config)) throw new InvalidOperationException("Valid route cache rejected.");
            if (layout.IsClear(emitter, Vector3.right, config)) throw new InvalidOperationException("Moved player cache accepted.");
            mouth.position += Vector3.right;
            if (layout.IsClear(emitter, Vector3.zero, config)) throw new InvalidOperationException("Moved emitter cache accepted.");
            mouth.position -= Vector3.right;
            config.Placement.TargetAngularWidthDegrees += 1f;
            if (layout.IsClear(emitter, Vector3.zero, config)) throw new InvalidOperationException("Changed word width cache accepted.");
            config.Placement.TargetAngularWidthDegrees -= 1f;
            Vector3 oldPosition = obstacle.transform.position;
            try
            {
                obstacle.transform.position += Vector3.up;
                if (layout.IsClear(emitter, Vector3.zero, config)) throw new InvalidOperationException("Moved scenery cache accepted.");
            }
            finally { obstacle.transform.position = oldPosition; }
            if (!layout.IsClear(emitter, Vector3.zero, config)) throw new InvalidOperationException("Restored route cache rejected.");
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
}

