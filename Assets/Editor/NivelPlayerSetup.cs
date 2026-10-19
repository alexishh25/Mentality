using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Cinemachine;
using VContainer.Unity;

[InitializeOnLoad]
public static class NivelPlayerSetup
{
    private const string Request = "Temp/NivelPlayerSetup.request";
    public const string ScenePath = "Assets/All-ObjE/Scenes/Nivel-1_Rhythm.unity";
    static NivelPlayerSetup() { if (File.Exists(Request)) EditorApplication.update += Process; }
    private static void Process()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Process;
        if (File.ReadAllText(Request).Trim() == "build")
        {
            Build();
            File.Delete(Request);
            return;
        }
        var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
        try
        {
            var lines = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t.parent == null || t.GetComponent<Camera>() != null || t.name.Contains("Tarima") || t.name.Contains("Thomas") || t.name.Contains("Microphone") || t.name.Contains("BaseT") || t.name.Contains("Pared"))
                .Select(t => t.name + " position=" + t.position.ToString("F2") + " rotation=" + t.eulerAngles.ToString("F1")
                    + (t.GetComponent<Renderer>() != null ? " bounds=" + t.GetComponent<Renderer>().bounds : ""));
            File.WriteAllLines("Logs/Nivel-1-layout.txt", lines);
            File.Delete(Request);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [MenuItem("Tools/Mentality/Integration/Prepare Nivel Player")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var previous = SceneManager.GetActiveScene();
        var destination = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !destination.isLoaded;
        if (opened) destination = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var source = EditorSceneManager.OpenPreviewScene(RhythmTestSceneBuilder.ScenePath);
        try
        {
            if (destination.isDirty) throw new InvalidOperationException("Save pending Nivel scene edits before setup");
            if (destination.GetRootGameObjects().Any(g => g.name == "Rhythm Player"))
                throw new InvalidOperationException("Player setup already exists; manual settings preserved");
            var sourceObjects = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var player = sourceObjects.Select(t => t.GetComponent<PlayerView>()).First(v => v != null);
            var scope = sourceObjects.Select(t => t.GetComponent<PlayerLifetimeScope>()).First(v => v != null);
            var oldCamera = destination.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).First(c => c.enabled);
            Vector3 eye = oldCamera.transform.position;
            Quaternion facing = Quaternion.Euler(0f, 0f, 0f);
            // Preserve references across the player, cameras and scope by cloning one hierarchy.
            var roots = new[] { player.transform.root, scope.transform.root, player.CameraTransform.root,
                player.FirstPersonCamera.transform.root, player.ThirdPersonCamera.transform.root }.Distinct().ToArray();
            var staging = new GameObject("Rhythm Player");
            SceneManager.MoveGameObjectToScene(staging, source);
            foreach (var root in roots) root.SetParent(staging.transform, true);
            var clone = UnityEngine.Object.Instantiate(staging);
            SceneManager.MoveGameObjectToScene(clone, destination);
            clone.name = "Rhythm Player";
            var copiedPlayer = clone.GetComponentInChildren<PlayerView>(true);
            var copiedScope = clone.GetComponentInChildren<PlayerLifetimeScope>(true);
            foreach (var other in clone.GetComponentsInChildren<LifetimeScope>(true))
                if (other != copiedScope) UnityEngine.Object.DestroyImmediate(other);
            clone.transform.rotation = facing;
            clone.transform.position += eye - copiedPlayer.FirstPersonCamera.transform.position;
            copiedScope.enabled = true;
            copiedScope.autoRun = true;
            foreach (Transform root in clone.transform) root.gameObject.SetActive(true);
            copiedPlayer.gameObject.SetActive(true);
            var settings = new SerializedObject(copiedScope);
            settings.FindProperty("playerModel").FindPropertyRelative("GameplayMode").enumValueIndex = (int)PlayerGameplayMode.Rhythm;
            settings.FindProperty("playerModel").FindPropertyRelative("CurrentPerspective").enumValueIndex = (int)CameraPerspective.FirstPerson;
            settings.ApplyModifiedPropertiesWithoutUndo();
            copiedPlayer.FirstPersonCamera.Priority = 20;
            copiedPlayer.ThirdPersonCamera.Priority = 0;
            var camera = copiedPlayer.CameraTransform.GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(eye, facing);
            camera.clearFlags = oldCamera.clearFlags;
            camera.backgroundColor = oldCamera.backgroundColor;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = Mathf.Max(100f, oldCamera.farClipPlane);
            camera.GetComponent<CinemachineBrain>().DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            foreach (var c in destination.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
                if (c != camera) c.enabled = false;
            foreach (var listener in destination.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AudioListener>(true)))
                listener.enabled = listener.gameObject == camera.gameObject;
            oldCamera.tag = "Untagged";
            camera.tag = "MainCamera";
            var canvasObject = new GameObject("Rhythm Aim", typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, destination);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var crosshair = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            crosshair.transform.SetParent(canvasObject.transform, false);
            var dot = crosshair.GetComponent<Image>();
            dot.raycastTarget = false;
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dot.rectTransform.sizeDelta = new Vector2(4f, 4f);
            var playerSettings = new SerializedObject(copiedPlayer);
            playerSettings.FindProperty("pointerUI").objectReferenceValue = crosshair;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            var origin = new GameObject("Gameplay Origin");
            SceneManager.MoveGameObjectToScene(origin, destination);
            origin.transform.SetPositionAndRotation(eye, facing);
            EditorSceneManager.MarkSceneDirty(destination);
            EditorSceneManager.SaveScene(destination);
            File.WriteAllText("Logs/Nivel-1-player-setup.txt", "PASS: Player, input scope, first-person Cinemachine, crosshair and one enabled camera/listener saved. Eye=" + eye + ". Facing +Z toward audience. No rhythm session or spawn systems added.\n");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(source);
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(destination, true);
        }
    }
}
