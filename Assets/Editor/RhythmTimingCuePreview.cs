using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Vista previa aislada: no abre ni modifica la escena de juego.
public static class RhythmTimingCuePreview
{
    [MenuItem("Tools/Mentality/Rhythm/Preview Projectile Launch")]
    public static void Render()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Sal de Play Mode para generar la vista previa.");
        var preview = new PreviewRenderUtility();
        Texture2D image = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/Prefabs/Rhythm/FRACASO.prefab");
            if (prefab == null) throw new InvalidOperationException("Falta el prefab FRACASO.");
            for (int i = 0; i < 3; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.gameObject.scene);
                instance.transform.position = new Vector3(0f, 2.5f - i * 2.5f, 0f);
                var view = instance.GetComponent<TargetView>();
                view.FacePlayer(instance.transform.position + Vector3.back * 8f);
                var rings = instance.GetComponentsInChildren<LineRenderer>();
                if (rings.Length != 0 || instance.GetComponentsInChildren<Collider>().Length != 1)
                    throw new InvalidOperationException("El proyectil debe conservar un collider y no llevar aros.");
            }
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 4.2f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 30f;
            preview.camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -8f), Quaternion.identity);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            preview.BeginStaticPreview(new Rect(0f, 0f, 1000f, 900f));
            preview.Render(true);
            image = preview.EndStaticPreview();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/RhythmProjectile-preview.png", image.EncodeToPNG());
            Debug.Log("Vista previa de palabras sin aros guardada en Logs/RhythmProjectile-preview.png.");
        }
        finally
        {
            if (image != null) Object.DestroyImmediate(image);
            preview.Cleanup();
        }
    }
}
