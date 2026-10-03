using System;
using System.Collections.Generic;
using System.IO;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Views;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer.Unity;
using Object = UnityEngine.Object;

public static class RhythmTestSceneBuilder
{
    public const string ScenePath = "Assets/Content/Scenes/RhythmTest.unity";
    private const string ResourcesPath = "Assets/Content/Prefabs/Rhythm";

    [InitializeOnLoadMethod]
    private static void CheckRequest()
    {
        if (File.Exists("Temp/RhythmTestScene.request")) EditorApplication.update += ProcessRequest;
    }

    private static void ProcessRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= ProcessRequest;
        if (!File.Exists("Temp/RhythmTestScene.request")) return;
        File.Delete("Temp/RhythmTestScene.request");
        Build();
    }

    [MenuItem("Tools/Mentality/Rhythm/Create Test Scene")]
    public static void Build()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene testScene = default;
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sal de Play Mode antes de crear la escena.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("RhythmTest ya existe. Se conserva para no sobrescribir ajustes manuales.");
            if (!AssetDatabase.CopyAsset("Assets/Content/Scenes/SampleScene.unity", ScenePath))
                throw new InvalidOperationException("No se pudo copiar la escena base.");

            testScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);
            PlayerView player = FindInScene<PlayerView>(testScene);
            PlayerLifetimeScope playerScope = FindInScene<PlayerLifetimeScope>(testScene);
            if (player == null || playerScope == null) throw new InvalidOperationException("Falta el jugador de la escena base.");

            var keep = new HashSet<GameObject>
            {
                player.transform.root.gameObject, playerScope.transform.root.gameObject,
                player.CameraTransform.root.gameObject, player.FirstPersonCamera.transform.root.gameObject,
                player.ThirdPersonCamera.transform.root.gameObject
            };
            foreach (GameObject root in testScene.GetRootGameObjects())
            {
                if (!keep.Contains(root)) Object.DestroyImmediate(root);
            }
            // Solo conservar el scope del jugador; la copia puede contener scopes de pruebas anteriores.
            foreach (GameObject root in keep)
            {
                foreach (var component in root.GetComponentsInChildren<LifetimeScope>(true))
                    if (component != playerScope) Object.DestroyImmediate(component);
                root.SetActive(true);
            }
            playerScope.gameObject.SetActive(true);
            playerScope.enabled = true;
            playerScope.autoRun = true;

            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;
            player.FirstPersonCamera.Priority = 20;
            player.ThirdPersonCamera.Priority = 0;
            var camera = player.CameraTransform.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.025f, 0.04f);
            camera.farClipPlane = 100f;
            camera.nearClipPlane = 0.05f;
            var brain = camera.GetComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientLight = Color.gray;

            var playerSettings = new SerializedObject(playerScope);
            var playerModel = playerSettings.FindProperty("playerModel");
            playerModel.FindPropertyRelative("GameplayMode").enumValueIndex = (int)PlayerGameplayMode.Rhythm;
            playerModel.FindPropertyRelative("CurrentPerspective").enumValueIndex = (int)CameraPerspective.FirstPerson;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();

            var systems = new GameObject("Rhythm Systems");
            var backendSettings = systems.AddComponent<BackendSceneSettings>();
            SetReferenceOrBool(backendSettings, "enableBackend", null, false);
            var audioSource = systems.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            RhythmHudView hud = BuildHud(out GameObject crosshair);
            SetReferenceOrBool(player, "pointerUI", crosshair);
            var playerSerialized = new SerializedObject(player);
            playerSerialized.FindProperty("rhythmInteractDistance").floatValue = 100f;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var scope = systems.AddComponent<RhythmLifetimeScope>();
            scope.parentReference = ParentReference.Create<PlayerLifetimeScope>();
            var rhythmSettings = new SerializedObject(scope);
            rhythmSettings.FindProperty("playerTarget").objectReferenceValue = player.CameraTransform;
            rhythmSettings.FindProperty("musicSource").objectReferenceValue = audioSource;
            rhythmSettings.FindProperty("hudView").objectReferenceValue = hud;
            var config = rhythmSettings.FindProperty("config");
            config.FindPropertyRelative("Beatmap").FindPropertyRelative("bpm").floatValue = 120f;
            config.FindPropertyRelative("Beatmap").FindPropertyRelative("offsetSeconds").floatValue = 0f;
            config.FindPropertyRelative("SpawnDistance").floatValue = 12f;
            config.FindPropertyRelative("TravelSpeed").floatValue = 3f;
            config.FindPropertyRelative("ArrivalDistance").floatValue = 0.75f;
            var prefabs = config.FindPropertyRelative("WordPrefabs");
            string[] words = { "INUTIL", "FRACASO", "COBARDE" };
            prefabs.arraySize = words.Length;
            for (int i = 0; i < words.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ResourcesPath}/{words[i]}.prefab");
                if (prefab == null) throw new InvalidOperationException("Falta el prefab " + words[i]);
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }
            rhythmSettings.ApplyModifiedPropertiesWithoutUndo();
            BuildArena(player.transform.position);

            EditorSceneManager.SaveScene(testScene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/RhythmTestScene-result.json", "{\"success\":true,\"scene\":\"" + ScenePath + "\"}");
            Debug.Log("[RhythmTestSceneBuilder] Escena creada con Player, Cinemachine, HUD y backend deshabilitado.");
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/RhythmTestScene-error.txt", exception.ToString());
            Debug.LogException(exception);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (testScene.IsValid() && testScene.isLoaded) EditorSceneManager.CloseScene(testScene, true);
        }
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private static void SetReferenceOrBool(Object target, string property, Object reference, bool? boolean = null)
    {
        var serialized = new SerializedObject(target);
        if (boolean.HasValue) serialized.FindProperty(property).boolValue = boolean.Value;
        else serialized.FindProperty(property).objectReferenceValue = reference;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RhythmHudView BuildHud(out GameObject crosshair)
    {
        var root = new GameObject("Rhythm HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var hud = root.AddComponent<RhythmHudView>();
        RectTransform top = Rect("Top panel", root.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 160));
        top.pivot = new Vector2(0.5f, 1f);
        top.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.035f, 0.055f, 0.92f);
        TextElement("Title", top, "MENTALITY / PRUEBA RITMICA", new Vector2(48, -22), new Vector2(750, 40), 30, new Color(0.35f, 0.95f, 0.8f));
        Text status = TextElement("Status", top, "SESION ACTIVA", new Vector2(48, -69), new Vector2(750, 28), 20, Color.white);
        Text rhythm = TextElement("Rhythm", top, "120 BPM / RELOJ SIN MUSICA", new Vector2(48, -105), new Vector2(1100, 30), 20, new Color(0.65f, 0.73f, 0.8f));
        TextElement("Hits label", top, "ACIERTOS", new Vector2(-440, -24), new Vector2(180, 30), 19, new Color(0.65f, 0.73f, 0.8f), true);
        Text hits = TextElement("Hits", top, "00", new Vector2(-440, -59), new Vector2(180, 65), 48, new Color(0.35f, 0.95f, 0.8f), true);
        TextElement("Misses label", top, "FALLOS", new Vector2(-210, -24), new Vector2(180, 30), 19, new Color(0.65f, 0.73f, 0.8f), true);
        Text misses = TextElement("Misses", top, "00", new Vector2(-210, -59), new Vector2(180, 65), 48, new Color(1f, 0.48f, 0.48f), true);
        RectTransform beatRect = Rect("Beat pulse", top, new Vector2(0, 1), new Vector2(0, 1), new Vector2(810, -40), new Vector2(16, 16));
        Image beat = beatRect.gameObject.AddComponent<Image>();
        beat.color = new Color(0.35f, 0.95f, 0.8f);

        RectTransform footer = Rect("Footer", root.transform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 95));
        footer.pivot = new Vector2(0.5f, 0);
        footer.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.035f, 0.055f, 0.9f);
        Text hint = TextElement("Controls", footer, "APUNTA Y HAZ CLIC | ESC libera el cursor | R reinicia", new Vector2(48, -30), new Vector2(1800, 40), 22, new Color(0.8f, 0.85f, 0.9f));

        RectTransform sight = Rect("Crosshair", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
        crosshair = sight.gameObject;
        foreach (var size in new[] { new Vector2(22, 3), new Vector2(3, 22) })
        {
            RectTransform line = Rect("Line", sight, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            line.gameObject.AddComponent<Image>().color = Color.white;
        }
        SetReferenceOrBool(hud, "hitsText", hits);
        SetReferenceOrBool(hud, "missesText", misses);
        SetReferenceOrBool(hud, "statusText", status);
        SetReferenceOrBool(hud, "rhythmText", rhythm);
        SetReferenceOrBool(hud, "cursorText", hint);
        SetReferenceOrBool(hud, "beatIndicator", beat);
        return hud;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text TextElement(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, bool right = false)
    {
        Vector2 anchor = new Vector2(right ? 1 : 0, 1);
        RectTransform rect = Rect(name, parent, anchor, anchor, position, size);
        rect.pivot = new Vector2(0, 1);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.text = value;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static void BuildArena(Vector3 center)
    {
        Material floorMaterial = MaterialAsset("RhythmFloor", new Color(0.035f, 0.055f, 0.08f));
        Material lineMaterial = MaterialAsset("RhythmArenaLines", new Color(0.1f, 0.32f, 0.36f));
        var arena = new GameObject("Test Arena");
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "Floor";
        floor.transform.SetParent(arena.transform);
        floor.transform.position = center + Vector3.down * 0.15f;
        floor.transform.localScale = new Vector3(36f, 0.1f, 36f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        var ring = new GameObject("Orientation ring", typeof(LineRenderer));
        ring.transform.SetParent(arena.transform);
        var line = ring.GetComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.loop = true;
        line.widthMultiplier = 0.045f;
        line.positionCount = 96;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / line.positionCount;
            line.SetPosition(i, center + new Vector3(Mathf.Sin(angle) * 15, -0.035f, Mathf.Cos(angle) * 15));
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>(ResourcesPath + "/RhythmWords.fontsettings");
        string[] labels = { "NORTE", "ESTE", "OESTE" };
        float[] headings = { 0f, 90f, 270f };
        for (int i = 0; i < labels.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0, headings[i], 0) * Vector3.forward;
            var marker = new GameObject(labels[i]);
            marker.transform.SetParent(arena.transform);
            marker.transform.position = center + direction * 16 + Vector3.up * 2.8f;
            marker.transform.rotation = Quaternion.LookRotation(direction);
            var text = marker.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 96;
            text.characterSize = 0.04f;
            text.anchor = TextAnchor.MiddleCenter;
            text.text = labels[i];
            text.color = new Color(0.28f, 0.48f, 0.55f);
            marker.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = ResourcesPath + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
