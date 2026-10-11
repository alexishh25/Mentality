using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Preparation only: does not create scene objects or change gameplay.
[InitializeOnLoad]
public static class CreatureSpriteImporter
{
    private const string Folder = "Assets/Content/Creatures/";
    private static readonly string[] Sheets = {
        "creature_standing_fornt", "creature_openmouth_standing_fornt"
    };

    static CreatureSpriteImporter()
    {
        if (!File.Exists("Logs/Creature-sprite-import.txt"))
            EditorApplication.delayCall += Prepare;
    }

    [MenuItem("Tools/Mentality/Rhythm/Prepare Creature Sprites")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (string sheet in Sheets)
        {
            string path = Folder + sheet + ".png";
            if (!File.Exists(path)) return;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (importer == null) throw new InvalidOperationException("No se pudo importar " + path);
#pragma warning disable CS0618
            var frames = Enumerable.Range(0, 5).Select(i => new SpriteMetaData {
                name = sheet + "_" + i,
                rect = new Rect(i * 64, 0, 64, 64),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            }).ToArray();
            bool prepared = importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Multiple
                && importer.spritesheet.Length == 5
                && importer.spritesheet.Select((s, i) => s.name == frames[i].name && s.rect == frames[i].rect
                    && s.alignment == frames[i].alignment).All(x => x)
                && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled
                && importer.alphaIsTransparency && importer.spritePixelsPerUnit == 64
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.npotScale == TextureImporterNPOTScale.None;
            if (!prepared)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 64;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.sRGBTexture = true;
                importer.spritesheet = frames;
                importer.SaveAndReimport();
            }
#pragma warning restore CS0618
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != 5 || sprites.Where((s, i) => s.name != frames[i].name
                || s.rect != frames[i].rect || s.pivot != new Vector2(32, 0)).Any())
                throw new InvalidOperationException("Cuadros o pivotes incorrectos: " + path);
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Creature-sprite-import.txt",
            "PASS: Two 320x64 sheets imported as five 64x64 sprites each. Point filtering, no mipmaps, uncompressed alpha, 64 pixels per unit, bottom-center pivots. No gameplay changes.\n");
        Debug.Log("Creature sprite preparation: PASS. Diez cuadros listos para animacion.");
    }
}
