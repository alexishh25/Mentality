using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NivelIntegrationDependencyChecks
{
    private const string Report = "Logs/Nivel-1-native-dependencies.json";
    static NivelIntegrationDependencyChecks()
    {
        if (!File.Exists(Report)) EditorApplication.update += WhenReady;
    }
    private static void WhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= WhenReady;
        Verify();
    }
    [MenuItem("Tools/Mentality/Integration/Verify Nivel Dependencies")]
    public static void Verify()
    {
        const string path = "Assets/All-ObjE/Scenes/Nivel-1_Rhythm.unity";
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            int missingScripts = objects.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            int missingMeshes = objects.SelectMany(t => t.GetComponents<MeshFilter>()).Count(m => m.sharedMesh == null);
            int missingMaterials = 0, unsupportedShaders = 0, missingPrefabs = 0;
            foreach (var t in objects)
            {
                if (PrefabUtility.IsPrefabAssetMissing(t.gameObject)) missingPrefabs++;
                foreach (var renderer in t.GetComponents<Renderer>())
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null) missingMaterials++;
                        else if (material.shader == null || !material.shader.isSupported) unsupportedShaders++;
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Report, JsonUtility.ToJson(new Result {
                success = missingScripts + missingPrefabs + missingMaterials + missingMeshes + unsupportedShaders == 0,
                objects = objects.Length, missingScripts = missingScripts, missingPrefabObjects = missingPrefabs,
                missingMeshes = missingMeshes, missingMaterialSlots = missingMaterials, unsupportedShaders = unsupportedShaders
            }, true));
            Debug.Log("Nivel integration dependency inspection complete: " + Report);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    [Serializable] private class Result
    {
        public bool success;
        public int objects, missingScripts, missingPrefabObjects, missingMeshes, missingMaterialSlots, unsupportedShaders;
    }
}
