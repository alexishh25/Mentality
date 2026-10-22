using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Explicit editor commands only; never changes a scene on import.
[InitializeOnLoad]
public static class NivelReferenceLightingSetup
{
    private const string ScenePath = "Assets/All-ObjE/Scenes/Nivel-1_Rhythm.unity";
    private const string Folder = "Assets/Content/ReferenceLighting";
    private const string Output = "Logs/ReferenceLighting";

    static NivelReferenceLightingSetup() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        const string request = "Temp/ReferenceLighting.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        string command = File.ReadAllText(request).Trim(); File.Delete(request);
        try
        {
            if (command == "diagnose") Diagnose();
            else if (command == "apply") Apply();
            else if (command == "capture") Capture("After");
            else throw new InvalidOperationException("Unknown lighting command: " + command);
        }
        catch (Exception e) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/Error.txt", e.ToString()); Debug.LogException(e); }
    }


    [MenuItem("Tools/Mentality/Lighting/Apply Reference Atmosphere")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Nivel-1_Rhythm outside Play Mode.");
        if (scene.isDirty) throw new InvalidOperationException("Save pending scene edits before applying lighting.");
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var hazeShader = Shader.Find("Mentality/Theater Reference Haze");
        var eyeShader = Shader.Find("Mentality/Theater Watching Eye");
        if (hazeShader == null || eyeShader == null || ShaderUtil.ShaderHasError(hazeShader) || ShaderUtil.ShaderHasError(eyeShader))
            throw new InvalidOperationException("Atmosphere shaders must compile before changing the scene.");
        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Reference Lighting");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Reference theater lighting");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var root = new GameObject("Reference Lighting"); Undo.RegisterCreatedObjectUndo(root, "Reference lighting");

        // Replace the old ceiling source with the reference rig.
        foreach (var light in all.Select(t => t.GetComponent<Light>()).Where(l => l != null))
        {
            Undo.RecordObject(light, "Balance theater lights");
            if (light.type == LightType.Point && light.range <= 1.01f)
            {
                light.color = new Color(0.66f, 0.72f, 1f);
                light.intensity = 0.025f; light.shadows = LightShadows.None;
            }
            if (light.name == "Area Light") light.enabled = false;
            if (light.name == "Flashlight") light.intensity = 8f;
            EditorUtility.SetDirty(light);
        }
        AddLight(root, "Overhead violet key", new Vector3(0,10.2f,11), new Vector3(0,1.8f,18),
            new Color(0.68f,0.70f,1f), 400f, 32f, 115f, true);
        AddLight(root, "Audience soft fill", new Vector3(0,5.5f,5), new Vector3(0,2.4f,19),
            new Color(0.67f,0.73f,1f), 150f, 32f, 110f, false);
        AddLight(root, "Left column uplight", new Vector3(-6.1f,2.5f,21), new Vector3(-6.1f,9.5f,23),
            new Color(0.58f,0.55f,1f), 16f, 12f, 35f, false);
        AddLight(root, "Right column uplight", new Vector3(8.6f,2.5f,20), new Vector3(8.6f,9.5f,22),
            new Color(0.58f,0.55f,1f), 16f, 12f, 35f, false);
        AddLight(root, "Muted red floor bounce", new Vector3(0,4f,10), new Vector3(0,0,14),
            new Color(0.65f,0.08f,0.15f), 3f, 14f, 110f, false);

        Undo.RecordObject(Unsupported.GetRenderSettings(), "Reference ambient and fog");
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f,0.17f,0.23f);
        RenderSettings.reflectionIntensity = 0.12f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.14f,0.14f,0.22f); RenderSettings.fogDensity = 0.022f;

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder + "/TheaterReference.asset");
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, Folder + "/TheaterReference.asset"); }
        var tone = Override<Tonemapping>(profile); tone.mode.Override(TonemappingMode.ACES);
        var grade = Override<ColorAdjustments>(profile);
        grade.postExposure.Override(0.15f); grade.contrast.Override(8f); grade.saturation.Override(-22f);
        grade.colorFilter.Override(new Color(0.93f,0.94f,1f));
        var bloom = Override<Bloom>(profile); bloom.threshold.Override(1.05f); bloom.intensity.Override(0.20f); bloom.scatter.Override(0.6f);
        var vignette = Override<Vignette>(profile); vignette.intensity.Override(0.27f); vignette.smoothness.Override(0.48f);
        var volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10f; volume.sharedProfile = profile;
        foreach (var camera in all.Select(t => t.GetComponent<Camera>()).Where(c => c != null))
        {
            var data = camera.GetUniversalAdditionalCameraData(); Undo.RecordObject(data, "Enable reference grading");
            data.renderPostProcessing = true; data.volumeLayerMask |= 1 << root.layer;
            EditorUtility.SetDirty(data);
        }

        // Color properties are authored in sRGB; these map to a subdued linear blue-violet.
        var haze = MaterialAsset("CeilingHaze", hazeShader); haze.SetColor("_HazeColor", new Color(0.55f,0.55f,0.72f,0.4f)); haze.SetFloat("_HazeShape",0);
        var shaft = MaterialAsset("SoftShaft", hazeShader); shaft.SetColor("_HazeColor", new Color(0.48f,0.48f,0.66f,0.18f)); shaft.SetFloat("_HazeShape",1);
        AddHaze(root, "Upper violet halo", new Vector3(0,8.5f,10f), new Vector3(18,9,1), haze);
        AddHaze(root, "Soft central shaft", new Vector3(0,6.5f,11f), new Vector3(16,10,1), shaft);

        var eyeMaterial = MaterialAsset("TheaterReferenceEye", eyeShader);
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Content/TheaterEyes/TheaterEye.mat");
        eyeMaterial.SetTexture("_MainTex", original.GetTexture("_MainTex"));
        eyeMaterial.SetColor("_Tint", new Color(1f,1f,1.1f,1));
        eyeMaterial.SetFloat("_ReferenceLook",1); eyeMaterial.SetFloat("_AtmosphereFog",1);
        foreach (var eye in all.Select(t => t.GetComponent<Gameplay.Rhythm.Views.TheaterWatchingEye>()).Where(e => e != null))
        {
            var visual = eye.GetComponent<SpriteRenderer>(); Undo.RecordObject(visual,"Integrate eye atmosphere");
            visual.sharedMaterial = eyeMaterial; EditorUtility.SetDirty(visual);
        }
        foreach (var asset in new Object[] {profile,haze,shaft,eyeMaterial}) EditorUtility.SetDirty(asset);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save reference lighting.");
        File.WriteAllText(Output + "/Applied.txt", "Reference atmosphere saved. Five spotlights (one shadow caster), two static haze meshes, dedicated scene VolumeProfile and eye material. Existing eye animation, positions, colliders and gameplay unchanged.");
        Capture("After");
    }

    private static T Override<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var component)) return component;
        component = profile.Add<T>(false); AssetDatabase.AddObjectToAsset(component, profile); return component;
    }
    private static Material MaterialAsset(string name, Shader shader)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        return material;
    }
    private static void AddLight(GameObject root, string name, Vector3 position, Vector3 target, Color color, float intensity, float range, float angle, bool shadows)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform, false);
        go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target-position));
        var light = go.AddComponent<Light>(); light.type = LightType.Spot;
        light.color = color; light.intensity = intensity; light.range = range; light.spotAngle = angle; light.innerSpotAngle = angle * 0.5f;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = 0.8f; light.shadowBias = 0.04f; light.shadowNormalBias = 0.3f;
    }
    private static void AddHaze(GameObject root, string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name;
        go.transform.SetParent(root.transform,false); go.transform.position = position; go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    [MenuItem("Tools/Mentality/Lighting/Capture Before")]
    public static void Diagnose()
    {
        Directory.CreateDirectory(Output);
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open Nivel-1_Rhythm first.");
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Output + "/Layout.txt", all.Where(t => t.GetComponent<Camera>() != null
            || t.GetComponent<Light>() != null && t.GetComponent<Light>().type != LightType.Point
            || t.name == "Rhythm Player" || t.name == "Publico" || t.name.StartsWith("column"))
            .Select(t => t.name + " position=" + t.position + " rotation=" + t.eulerAngles + " active=" + t.gameObject.activeInHierarchy)
            .Concat(all.Where(t => t.name.StartsWith("pbl_PublicS_")).Take(1).SelectMany(t => t.GetComponentsInChildren<Renderer>(true))
            .Select(r => r.name + " materials=" + string.Join(",", r.sharedMaterials.Select(m => m == null ? "null" : AssetDatabase.GetAssetPath(m) + " shader=" + m.shader.name)))));
        Capture("Before");
    }

    public static double Capture(string label)
    {
        var source = Camera.main;
        if (source == null) throw new InvalidOperationException("Main camera required.");
        var go = new GameObject("Reference lighting preview camera");
        var camera = go.AddComponent<Camera>();
        camera.CopyFrom(source);
        camera.transform.SetPositionAndRotation(source.transform.position, Quaternion.Euler(-8f, 0f, 0f));
        camera.fieldOfView = 65f;
        var originalData = source.GetUniversalAdditionalCameraData();
        var data = camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = originalData.renderPostProcessing;
        data.volumeLayerMask = originalData.volumeLayerMask;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        // Use an sRGB target for PNG delivery; half-float targets retain linear values
        // and make an otherwise correct dark scene look much darker in an image viewer.
        var target = new RenderTexture(1440, 810, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var pixels = new Texture2D(1440, 810, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1440, 810), 0, 0); pixels.Apply();
            File.WriteAllBytes(Output + "/" + label + ".png", pixels.EncodeToPNG());
            var colors = pixels.GetPixels32();
            long sum = 0; foreach (var color in colors) sum += color.r + color.g + color.b;
            return sum / (colors.Length * 3.0);
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.DestroyImmediate(pixels); Object.DestroyImmediate(target); Object.DestroyImmediate(go);
        }
    }
}
