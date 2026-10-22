using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class NivelTheaterEyesSetup
{
    private const string Folder = "Assets/Content/TheaterEyes/";
    private const string Running = "Nivel.TheaterEyes.Play";
    private static double readyAt;
    private static readonly Dictionary<TheaterWatchingEye, HashSet<Sprite>> observedFrames = new Dictionary<TheaterWatchingEye, HashSet<Sprite>>();
    static NivelTheaterEyesSetup()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += Changed;
    }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("Temp/NivelTheaterEyes.request"))
        {
            File.Delete("Temp/NivelTheaterEyes.request");
            try { Prepare(); }
            catch (Exception e) { File.WriteAllText("Logs/NivelTheaterEyes.txt", e.ToString()); Debug.LogException(e); }
        }
        if (File.Exists("Temp/NivelTheaterEyesPlay.request"))
        {
            File.Delete("Temp/NivelTheaterEyesPlay.request");
            SessionState.SetString(Running + ".Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(NivelPlayerSetup.ScenePath);
            SessionState.SetBool(Running, true);
            EditorApplication.EnterPlaymode();
        }
        if (File.Exists("Temp/NivelTheaterEyesAnimate.request"))
        {
            File.Delete("Temp/NivelTheaterEyesAnimate.request");
            try { AnimateExisting(); }
            catch (Exception e) { File.WriteAllText("Logs/NivelTheaterEyesAnimation.txt", e.ToString()); Debug.LogException(e); }
        }
    }

    [MenuItem("Tools/Mentality/Integration/Animate Existing Theater Eyes")]
    public static void AnimateExisting()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play.");
        var frames = AssetDatabase.LoadAllAssetsAtPath(Folder + "ojo.jpg").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        var eyes = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TheaterWatchingEye>(true)).OrderBy(e => e.name).ToArray();
        if (eyes.Length != 24 || frames.Length != 8) throw new InvalidOperationException("Expected 24 eyes and eight sheet frames.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Animate theater eyes");
        for (int i = 0; i < eyes.Length; i++)
        {
            var eye = eyes[i]; var position = eye.transform.position; var scale = eye.transform.localScale;
            Undo.RecordObject(eye, "Configure eye animation");
            eye.ConfigureAnimation(frames, 9f + i % 3, 2.2f + i % 5 * 0.27f, i * 0.173f);
            EditorUtility.SetDirty(eye);
            if (eye.transform.position != position || eye.transform.localScale != scale) throw new InvalidOperationException("Eye placement changed.");
        }
        Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Logs/NivelTheaterEyesAnimation.txt", "PASS: All 24 eyes configured with frames 0 through 7 in sheet order, individual phase offsets and open-eye holds. Positions and sizes preserved. Uses musical clock to freeze on pause and reset on restart.");
    }

    [MenuItem("Tools/Mentality/Integration/Prepare Theater Watching Eyes")]
    public static void Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Nivel-1_Rhythm" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play before placing eyes.");
        string sheet = Folder + "ojo.jpg";
        AssetDatabase.ImportAsset(sheet);
        var importer = (TextureImporter)AssetImporter.GetAtPath(sheet);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 768f;
        importer.mipmapEnabled = true; importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
        var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings);
#pragma warning disable CS0618
        importer.spritesheet = Enumerable.Range(0, 8).Select(i => new SpriteMetaData {
            name = "ojo_" + i, rect = new Rect(i % 4 * 688, (1 - i / 4) * 768, 688, 768),
            alignment = (int)SpriteAlignment.Center, pivot = Vector2.one * 0.5f
        }).ToArray();
#pragma warning restore CS0618
        importer.SaveAndReimport();
        var frames = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != 8) throw new InvalidOperationException("Expected eight eye frames.");
        var shader = Shader.Find("Mentality/Theater Watching Eye");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Eye shader must compile.");
        string materialPath = Folder + "TheaterEye.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
        material.shader = shader; material.SetTexture("_MainTex", frames[0].texture);
        material.SetColor("_Tint", new Color(0.8f, 0.74f, 0.7f, 1f)); EditorUtility.SetDirty(material);
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var scope = all.Select(t => t.GetComponent<RhythmLifetimeScope>()).Single(s => s != null);
        var target = (Transform)new SerializedObject(scope).FindProperty("playerTarget").objectReferenceValue;
        var obstacles = all.SelectMany(t => t.GetComponents<MeshRenderer>()).Where(r => r.enabled && r.gameObject.activeInHierarchy
            && r.GetComponentInParent<PlayerView>() == null && r.transform.root.name != "Rhythm Player"
            && r.sharedMaterials.Any(m => m != null && m.renderQueue < 3000)).Cast<Renderer>().ToArray();
        var locations = new List<Vector3>(); var diameters = new List<float>();
        var log = new List<string>();
        try
        {
            NivelWordLayoutSetup.OpenGeometry(obstacles);
            for (int i = 0; i < 24; i++)
            {
                bool audience = i < 16;
                float yaw = i < 8 ? -67f + i * 19f : i < 16 ? -75f + (i - 8) * 21f : new[] { -92f, 92f, -113f, 113f, -139f, 139f, -100f, 100f }[i - 16];
                float height = i < 8 ? 4.7f + (i % 3) * 0.37f : i < 16 ? 7f + (i % 3) * 0.3f : i >= 22 ? 7.4f : 4.5f + (i % 3) * 0.5f;
                float diameter = 0.95f + (i * 7 % 5) * 0.17f;
                bool found = false; Vector3 candidate = default; string reason = "";
                foreach (float offset in audience ? new[] { 0f, -3f, 3f, -6f, 6f } : new[] { 0f, -6f, 6f, -12f, 12f, -24f, 24f, -36f, 36f })
                {
                  foreach (float heightOffset in audience ? new[] { 0f } : new[] { 0f, -0.8f, -1.6f, 0.8f })
                  {
                    for (float distance = audience ? 23f : 15f; distance >= (audience ? 17f : 4.5f); distance -= 0.5f)
                    {
                        var direction = Quaternion.Euler(0f, yaw + offset, 0f) * Vector3.forward;
                        candidate = target.position + direction * distance; candidate.y = height + heightOffset;
                        if (locations.Any(p => Vector3.Distance(candidate, p) < diameter + 0.5f)) continue;
                        var rotation = Quaternion.LookRotation(candidate - target.position, Vector3.up);
                        float halfSize = diameter * 0.54f;
                        if (!NivelWordLayoutSetup.BoxClear(candidate, new Vector3(halfSize, halfSize, 0.08f), rotation, obstacles, out var blocker)) { reason = "Body: " + blocker.name; continue; }
                        bool visible = true;
                        foreach (var corner in new[] { Vector2.zero, new Vector2(-1,-1), new Vector2(-1,1), new Vector2(1,-1), new Vector2(1,1) })
                            if (!NivelWordLayoutSetup.RayClear(target.position, candidate + rotation * new Vector3(corner.x * halfSize, corner.y * halfSize, 0f), obstacles, out blocker)) { reason = "Sightline: " + blocker.name; visible = false; break; }
                        if (!visible) continue;
                        locations.Add(candidate); diameters.Add(diameter); found = true;
                        log.Add("Eye " + (i + 1) + "; group=" + (audience ? "Audience backdrop" : "Theater sides") + "; position=" + candidate + "; diameter=" + diameter);
                        break;
                    }
                    if (found) break;
                  }
                  if (found) break;
                }
                if (!found) throw new InvalidOperationException("No clear background position for eye " + (i + 1) + "; yaw=" + yaw + "; " + reason);
            }
        }
        finally { NivelWordLayoutSetup.CloseGeometry(); }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Place theater watching eyes");
        var holder = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Theater Watching Eyes");
        if (holder == null) { holder = new GameObject("Theater Watching Eyes"); Undo.RegisterCreatedObjectUndo(holder, "Create eye group"); }
        int[] openFrames = { 0, 1, 5, 7 };
        for (int i = 0; i < locations.Count; i++)
        {
            string name = (i < 16 ? "Audience Eye " : "Theater Eye ") + (i + 1).ToString("00");
            var eye = holder.transform.Find(name)?.gameObject;
            if (eye == null) { eye = new GameObject(name); Undo.RegisterCreatedObjectUndo(eye, "Create watching eye"); eye.transform.SetParent(holder.transform, false); }
            Undo.RecordObject(eye.transform, "Position eye"); eye.transform.position = locations[i];
            eye.transform.localScale = Vector3.one * (diameters[i] / 0.73f);
            var visual = eye.GetComponent<SpriteRenderer>();
            if (visual == null) visual = Undo.AddComponent<SpriteRenderer>(eye);
            Undo.RecordObject(visual, "Assign eye sprite"); visual.sprite = frames[openFrames[i % openFrames.Length]];
            visual.sharedMaterial = material; visual.color = Color.white;
            visual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; visual.receiveShadows = false;
            var watcher = eye.GetComponent<TheaterWatchingEye>();
            if (watcher == null) watcher = Undo.AddComponent<TheaterWatchingEye>(eye);
            Undo.RecordObject(watcher, "Assign gaze target"); watcher.Configure(target); EditorUtility.SetDirty(watcher);
            watcher.ConfigureAnimation(frames, 9f + i % 3, 2.2f + i % 5 * 0.27f, i * 0.173f);
        }
        Undo.CollapseUndoOperations(group); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        log.Add("PASS: 24 eyes; 16 audience backdrop, 8 theater sides. All eight source frames animate with individual phase offsets. Clear center/corner sightlines and sprite space checked. No colliders or lights added.");
        File.WriteAllLines("Logs/NivelTheaterEyes.txt", log); Selection.activeGameObject = holder;
        Debug.Log("Theater watching eyes: 24 placed and saved.");
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { observedFrames.Clear(); readyAt = EditorApplication.timeSinceStartup + 11d; EditorApplication.update += CheckPlay; }
        if (state == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= CheckPlay;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            string previous = SessionState.GetString(Running + ".Previous", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        }
    }
    private static void CheckPlay()
    {
        foreach (var eye in Object.FindObjectsByType<TheaterWatchingEye>(FindObjectsSortMode.None))
        {
            if (!observedFrames.TryGetValue(eye, out var seen)) observedFrames.Add(eye, seen = new HashSet<Sprite>());
            seen.Add(eye.GetComponent<SpriteRenderer>().sprite);
        }
        if (EditorApplication.timeSinceStartup < readyAt) return;
        EditorApplication.update -= CheckPlay;
        try
        {
            var eyes = Object.FindObjectsByType<TheaterWatchingEye>(FindObjectsSortMode.None);
            if (eyes.Length != 24 || eyes.Any(e => e.PlayerTarget == null || e.GetComponent<Collider>() != null)) throw new InvalidOperationException("Missing eyes, targets, or unwanted colliders.");
            if (eyes.Any(e => e.AnimationFrameCount != 8 || observedFrames[e].Count != 8))
                throw new InvalidOperationException("Not all eight animation frames observed during live playback: " + string.Join(", ", eyes.Select(e => e.name + "=" + observedFrames[e].Count)));
            foreach (var eye in eyes)
            {
                var block = new MaterialPropertyBlock(); var visual = eye.GetComponent<SpriteRenderer>(); visual.GetPropertyBlock(block);
                var rect = visual.sprite.textureRect; var texture = visual.sprite.texture;
                if (Vector4.Distance(block.GetVector("_EyeUvRect"), new Vector4(rect.x / texture.width, rect.y / texture.height, rect.width / texture.width, rect.height / texture.height)) > 0.0001f)
                    throw new InvalidOperationException("Shader rectangle is not following animated sprite.");
            }
            foreach (var eye in eyes)
                if (Vector3.Dot(-eye.transform.forward, (eye.PlayerTarget.position - eye.transform.position).normalized) < 0.999f)
                    throw new InvalidOperationException("Eye is not looking at the player: " + eye.name);
            var probe = new GameObject("Gaze verification target");
            var first = eyes[0]; var original = first.PlayerTarget;
            try
            {
                probe.transform.position = original.position + Vector3.right * 3f;
                first.Configure(probe.transform); first.FacePlayer();
                if (Vector3.Dot(-first.transform.forward, (probe.transform.position - first.transform.position).normalized) < 0.999f)
                    throw new InvalidOperationException("Gaze does not follow target position.");
            }
            finally { first.Configure(original); Object.DestroyImmediate(probe); }
            Object.FindFirstObjectByType<RhythmLifetimeScope>().Container.Resolve<RhythmGamePresenter>().PauseSession();
            Capture(0f, "Front"); Capture(-65f, "Left"); Capture(115f, "Theater");
            File.WriteAllText("Logs/NivelTheaterEyesPlay.txt", "PASS: All eight frames observed on each of 24 live eyes; shader cutout tracks each frame; all face player; moved-target gaze verified; no eye colliders; three gameplay captures saved.");
        }
        catch (Exception e) { File.WriteAllText("Logs/NivelTheaterEyesPlay.txt", e.ToString()); Debug.LogException(e); }
        finally { EditorApplication.ExitPlaymode(); }
    }
    private static void Capture(float yaw, string name)
    {
        var go = new GameObject("Watching eyes capture"); var camera = go.AddComponent<Camera>();
        camera.CopyFrom(Camera.main); camera.transform.position = Camera.main.transform.position;
        camera.transform.rotation = Quaternion.Euler(-8f, yaw, 0f); camera.fieldOfView = 65f;
        var target = new RenderTexture(1440, 810, 24); var pixels = new Texture2D(1440, 810, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,1440,810),0,0); pixels.Apply();
            File.WriteAllBytes("Logs/NivelTheaterEyes-" + name + ".png", pixels.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; camera.targetTexture = null; Object.DestroyImmediate(pixels); Object.DestroyImmediate(target); Object.DestroyImmediate(go); }
    }
}
