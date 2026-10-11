using System;
using System.IO;
using System.Linq;
using Gameplay.Rhythm.Views;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Factories;
using Gameplay.Rhythm.Services;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CreatureEmergenceSetup
{
    private const string PrefabPath = "Assets/Content/Creatures/Creature.prefab";
    static CreatureEmergenceSetup()
    {
        if (!File.Exists(PrefabPath) || !File.Exists("Logs/Creature-attack-preparation.txt"))
            EditorApplication.delayCall += Prepare;
    }

    [MenuItem("Tools/Mentality/Rhythm/Prepare Creature Emergence")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Content/Creatures/creature_standing_fornt.png")
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (sprites.Length != 5) throw new InvalidOperationException("Importar primero los cinco cuadros de pie.");
        var mouthSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Content/Creatures/creature_openmouth_standing_fornt.png")
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (mouthSprites.Length != 5) throw new InvalidOperationException("Importar los cinco cuadros de boca abierta.");
        var root = new GameObject("Creature");
        GameObject player = null;
        try
        {
            var visual = new GameObject("Standing sprite", typeof(SpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprites[0];
            var view = root.AddComponent<CreatureEmergenceView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("body").objectReferenceValue = renderer;
            var frames = serialized.FindProperty("standingFrames");
            frames.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            var mouthFrames = serialized.FindProperty("openMouthFrames");
            mouthFrames.arraySize = mouthSprites.Length;
            for (int i = 0; i < mouthSprites.Length; i++) mouthFrames.GetArrayElementAtIndex(i).objectReferenceValue = mouthSprites[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            player = new GameObject("Creature verification player");
            player.transform.position = new Vector3(0, 1.6f, 0);
            foreach (var ground in new[] { new Vector3(-6, 0, 9), new Vector3(0, 0, 11), new Vector3(6, 0, 9) })
            {
                view.Initialize(ground, player.transform);
                if (renderer.transform.position.y > -2.39f) throw new InvalidOperationException("Inicio sobre el suelo.");
                view.SampleAt(0.4d);
                if (renderer.transform.position.y <= -2.39f || renderer.transform.position.y >= 0f)
                    throw new InvalidOperationException("La criatura no emerge progresivamente.");
                view.SampleAt(1d);
                if (!view.HasEmerged || !renderer.enabled || Mathf.Abs(renderer.transform.position.y) > 0.001f
                    || Vector3.Dot(root.transform.forward, new Vector3(-ground.x, 0, -ground.z).normalized) < 0.999f)
                    throw new InvalidOperationException("Aparicion u orientacion incorrecta.");
                view.SampleAt(3.5d);
                if (!view.HasFinished || renderer.enabled) throw new InvalidOperationException("La criatura no se retira.");
            }
            if (root.GetComponentInChildren<Collider>() != null) throw new InvalidOperationException("La criatura bloquea clics.");
            view.Initialize(new Vector3(0, 0, 10), player.transform);
            int shots = 0;
            view.ConfigureAttack(4, 0.22d, (position, index, time) => {
                if (index != shots || !view.IsAttacking || position.y <= 0 || (position - view.MouthPosition).sqrMagnitude > 0.001f)
                    throw new InvalidOperationException("Disparo fuera de la boca o de la secuencia.");
                shots++;
            });
            view.AdvanceAt(0.9d);
            if (view.IsAttacking || shots != 0 || !sprites.Contains(renderer.sprite)) throw new InvalidOperationException("Ataque anticipado.");
            view.AdvanceAt(1.05d);
            if (!view.IsAttacking || !mouthSprites.Contains(renderer.sprite) || shots != 0) throw new InvalidOperationException("Falta la anticipacion de boca abierta.");
            for (int i = 0; i < 4; i++) { view.AdvanceAt(1.151d + i * 0.22d); view.AdvanceAt(1.151d + i * 0.22d); }
            if (shots != 4) throw new InvalidOperationException("Rafaga incorrecta o duplicada.");
            view.AdvanceAt(3.5d);
            if (!view.HasFinished || shots != 4) throw new InvalidOperationException("Disparo despues de retirarse.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/Creature-emergence.txt", "PASS: Native Unity prefab saved. Three ground positions verified: hidden start, smooth half-emergence, five-frame standing animation, facing player, visible standing stage, retreat and no click-blocking collider.\n");
            File.WriteAllText("Logs/Creature-attack-preparation.txt", "PASS: Native Unity prefab contains five open-mouth frames. Standing first, mouth opens before firing, four spaced callbacks from the mouth, no duplicate callbacks and no shots after retreat.\n");
            Debug.Log("Creature emergence preparation: PASS.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [MenuItem("Tools/Mentality/Rhythm/Preview Creature Emergence")]
    public static void Preview()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Entrar en Play Mode para previsualizar.");
        var scope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
        if (scope == null) throw new InvalidOperationException("Abrir RhythmTest.");
        var serialized = new SerializedObject(scope);
        var player = serialized.FindProperty("playerTarget").objectReferenceValue as Transform;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var clock = (AudioConductorService)scope.Container.Resolve(typeof(AudioConductorService));
        var factory = (TargetFactory)scope.Container.Resolve(typeof(TargetFactory));
        var model = (RhythmGameModel)scope.Container.Resolve(typeof(RhythmGameModel));
        var config = (RhythmGameConfig)serialized.FindProperty("config").boxedValue;
        var profile = new MusicalActivityProfile(config.AnalyzedSong.LoadValidatedMap(), config.MinimumSpawnInterval,
            config.MusicalActivity, config.AnalyzedSong.ManualIntensity);
        if (player == null || prefab == null || !clock.IsPlaying) throw new InvalidOperationException("Iniciar la sesion antes de previsualizar.");
        foreach (float angle in new[] { -35f, 0f, 35f })
        {
            float radians = angle * Mathf.Deg2Rad;
            var origin = player.position + new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians)) * 10f;
            var floor = GameObject.Find("Floor").GetComponent<Renderer>();
            origin.y = floor.bounds.max.y + 0.01f;
            var view = UnityEngine.Object.Instantiate(prefab).GetComponent<CreatureEmergenceView>();
            view.Initialize(origin, player, clock);
            view.ConfigureAttack(2, 0.25d, (mouth, index, time) => factory.CreateCreatureWord(mouth, index, time, profile), model);
            factory.RegisterCreature(view);
        }
    }
}
