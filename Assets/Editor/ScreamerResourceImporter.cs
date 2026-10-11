using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Resource preparation only; leaves scene references and gameplay unchanged.
[InitializeOnLoad]
public static class ScreamerResourceImporter
{
    private const string Folder = "Assets/Content/Effects/Screamers/";
    private const string Report = "Logs/Screamer-resource-import.txt";

    static ScreamerResourceImporter()
    {
        if (!File.Exists(Report)) EditorApplication.update += PrepareWhenReady;
    }

    private static void PrepareWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= PrepareWhenReady;
        Prepare();
    }

    [MenuItem("Tools/Mentality/Rhythm/Prepare Screamer Resources")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int i = 1; i <= 15; i++)
        {
            string path = Folder + $"64x64_faces_{i:000}.png";
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing screamer: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width <= 0 || texture.height <= 0)
                throw new InvalidOperationException("Unexpected screamer dimensions: " + path);
        }
        string audioPath = Folder + "susto.mp3";
        AssetDatabase.ImportAsset(audioPath);
        var audioImporter = AssetImporter.GetAtPath(audioPath) as AudioImporter;
        if (audioImporter == null) throw new InvalidOperationException("Missing susto.mp3");
        var settings = audioImporter.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        audioImporter.defaultSampleSettings = settings;
        audioImporter.loadInBackground = false;
        audioImporter.SaveAndReimport();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
        if (clip == null || clip.length <= 0 || clip.samples <= 0)
            throw new InvalidOperationException("Invalid screamer audio");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, $"PASS: 15 textures, original dimensions preserved, point filtering, alpha preserved, no mipmaps or compression. Audio imported and preloaded: {clip.length:F3}s, {clip.frequency}Hz, {clip.channels} channels. Scene and gameplay unchanged.\n");
        Debug.Log("Screamer resource preparation: PASS.");
    }
}
