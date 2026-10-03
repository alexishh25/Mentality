using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Herramienta de authoring. No se incluye en el juego ni modifica escenas.
public static class RhythmWordPrefabBuilder
{
    private const string Folder = "Assets/Content/Prefabs/Rhythm";
    private const string ReportPath = "Logs/RhythmWordPrefabs-result.json";
    private const string PreviewPath = "Logs/RhythmWordPrefabs-preview.png";
    private const int FontResolution = 96;
    private const float LetterHeight = 0.65f;
    private static readonly string[] Words = { "INUTIL", "FRACASO", "COBARDE" };

    // Solicitud puntual para el editor abierto; nunca se ejecuta sin este archivo.
    [InitializeOnLoadMethod]
    private static void CheckBuildRequest()
    {
        if (File.Exists("Temp/RhythmWordPrefabs.request"))
            EditorApplication.update += ProcessBuildRequest;
    }

    private static void ProcessBuildRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= ProcessBuildRequest;
        if (!File.Exists("Temp/RhythmWordPrefabs.request")) return;
        File.Delete("Temp/RhythmWordPrefabs.request");
        Build();
    }

    [Serializable]
    private class BuildReport
    {
        public bool success;
        public string error;
        public string preview;
        public List<WordReport> words = new List<WordReport>();
    }

    [Serializable]
    private class WordReport
    {
        public string word;
        public string prefab;
        public Vector3 textSize;
        public Vector3 colliderSize;
    }

    [MenuItem("Tools/Mentality/Rhythm/Create Word Prefabs")]
    public static void Build()
    {
        Directory.CreateDirectory("Logs");
        var report = new BuildReport();
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Genera los prefabs fuera de Play Mode.");

            Directory.CreateDirectory(Folder);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "/RhythmWord.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("El shader URP no esta disponible o tiene errores.");

            Font font = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/RhythmWords.fontsettings");
            if (font == null)
                font = BakeFont(shader);

            float characterSize = MeasureCharacterSize(font);
            foreach (string word in Words)
            {
                string path = Folder + "/" + word + ".prefab";
                // Preserva ajustes manuales al ejecutar de nuevo la herramienta.
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    CreateWordPrefab(word, path, font, characterSize);

                report.words.Add(ValidatePrefab(word, path, font));
            }

            AssetDatabase.SaveAssets();
            RenderPreview();
            report.preview = Path.GetFullPath(PreviewPath);
            report.success = true;
            Debug.Log("[RhythmWordPrefabBuilder] INUTIL, FRACASO y COBARDE creados y validados.");
        }
        catch (Exception exception)
        {
            report.error = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }

    private static Font BakeFont(Shader shader)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        source.RequestCharactersInTexture(alphabet, FontResolution, FontStyle.Bold);

        var characters = new List<CharacterInfo>();
        foreach (char letter in alphabet)
        {
            if (!source.GetCharacterInfo(letter, out CharacterInfo glyph, FontResolution, FontStyle.Bold))
                throw new InvalidOperationException("No se pudo generar el caracter " + letter);

            // Fuente estatica: los glifos horneados ya contienen la negrita.
            glyph.size = 0;
            glyph.style = FontStyle.Normal;
            characters.Add(glyph);
        }

        Texture atlas = source.material.mainTexture;
        RenderTexture temporary = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        Texture2D copy = null;
        try
        {
            Graphics.Blit(atlas, temporary);
            RenderTexture.active = temporary;
            copy = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
            copy.Apply();
            File.WriteAllBytes(Folder + "/RhythmWordsAtlas.png", copy.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            if (copy != null) Object.DestroyImmediate(copy);
        }

        string atlasPath = Folder + "/RhythmWordsAtlas.png";
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        var material = new Material(shader) { name = "RhythmWord" };
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        material.SetColor("_Color", Color.white);
        AssetDatabase.CreateAsset(material, Folder + "/RhythmWord.mat");

        var font = new Font { name = "RhythmWords", material = material, characterInfo = characters.ToArray() };
        var serialized = new SerializedObject(font);
        SetFontMetric(serialized, "m_FontSize", FontResolution);
        SetFontMetric(serialized, "m_LineSpacing", FontResolution * 1.2f);
        SetFontMetric(serialized, "m_Ascent", FontResolution);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(font, Folder + "/RhythmWords.fontsettings");
        return font;
    }

    private static void SetFontMetric(SerializedObject serialized, string name, float value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) return;
        if (property.propertyType == SerializedPropertyType.Integer)
            property.intValue = Mathf.RoundToInt(value);
        else if (property.propertyType == SerializedPropertyType.Float)
            property.floatValue = value;
    }

    private static TextMesh AddText(GameObject root, string word, Font font, float characterSize)
    {
        TextMesh text = root.AddComponent<TextMesh>();
        text.font = font;
        text.fontSize = FontResolution;
        text.fontStyle = FontStyle.Normal;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;
        text.text = word;

        var renderer = root.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return text;
    }

    private static float MeasureCharacterSize(Font font)
    {
        var preview = new PreviewRenderUtility();
        GameObject root = new GameObject("Measure word");
        try
        {
            preview.AddSingleGO(root);
            AddText(root, "COBARDE", font, 1f);
            float height = root.GetComponent<MeshRenderer>().bounds.size.y;
            if (height <= 0f)
                throw new InvalidOperationException("La fuente no genero geometria de texto.");
            return LetterHeight / height;
        }
        finally
        {
            preview.Cleanup();
        }
    }

    private static void CreateWordPrefab(string word, string path, Font font, float characterSize)
    {
        var preview = new PreviewRenderUtility();
        GameObject root = new GameObject(word);
        try
        {
            preview.AddSingleGO(root);
            TextMesh text = AddText(root, word, font, characterSize);
            var view = root.AddComponent<TargetView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("wordText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Bounds bounds = root.GetComponent<MeshRenderer>().localBounds;
            var collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = new Vector3(bounds.size.x + 0.12f, bounds.size.y + 0.10f, 0.20f);
            collider.isTrigger = false;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            preview.Cleanup();
        }
    }

    private static WordReport ValidatePrefab(string word, string path, Font font)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("No se guardo " + path);

        // TextMesh genera su geometria en una instancia, no en el asset del prefab.
        var preview = new PreviewRenderUtility();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.gameObject.scene);
            var text = instance.GetComponent<TextMesh>();
            var view = instance.GetComponent<TargetView>();
            var collider = instance.GetComponent<BoxCollider>();
            var renderer = instance.GetComponent<MeshRenderer>();
            if (text == null || text.text != word || text.font != font || view == null || !view.enabled
                || collider == null || !collider.enabled || renderer == null || renderer.sharedMaterial != font.material)
                throw new InvalidOperationException("Componentes incompletos en " + path);

            if (new SerializedObject(view).FindProperty("wordText").objectReferenceValue != text)
                throw new InvalidOperationException("TargetView no tiene su texto asignado en " + path);

            Bounds bounds = renderer.bounds;
            if (bounds.size.x <= 0f || Mathf.Abs(bounds.size.y - LetterHeight) > 0.03f
                || collider.size.x < bounds.size.x || collider.size.y < bounds.size.y)
                throw new InvalidOperationException("Dimensiones incorrectas en " + path + ": " + bounds.size);

            return new WordReport { word = word, prefab = path, textSize = bounds.size, colliderSize = collider.size };
        }
        finally
        {
            preview.Cleanup();
        }
    }

    private static void RenderPreview()
    {
        var preview = new PreviewRenderUtility();
        Texture2D image = null;
        try
        {
            for (int i = 0; i < Words.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + Words[i] + ".prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.gameObject.scene);
                instance.transform.position = new Vector3(0f, 1.3f - i * 1.3f, 0f);
            }

            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 2.4f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = 30f;
            preview.camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -8f), Quaternion.identity);
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
            preview.BeginStaticPreview(new Rect(0, 0, 1200, 700));
            preview.Render(true);
            image = preview.EndStaticPreview();
            File.WriteAllBytes(PreviewPath, image.EncodeToPNG());
        }
        finally
        {
            if (image != null) Object.DestroyImmediate(image);
            preview.Cleanup();
        }
    }
}
