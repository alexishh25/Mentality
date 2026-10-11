using System;
using System.Collections.Generic;
using System.IO;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MidnightIntegrationChecks
{
    [MenuItem("Tools/Mentality/Rhythm/Verify Saved Midnight Intensity")]
    public static void VerifySavedIntensity()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Verificar fuera de Play Mode.");
        var scope = UnityEngine.Object.FindFirstObjectByType<RhythmLifetimeScope>();
        if (scope == null) throw new InvalidOperationException("Abrir RhythmTest para verificar la configuracion actual.");
        var config = (RhythmGameConfig)new SerializedObject(scope).FindProperty("config").boxedValue;
        var song = config.AnalyzedSong;
        var map = song.LoadValidatedMap();
        var plan = new MusicalSpawnSchedule(map, config.MinimumSpawnInterval, config.TargetLeadTimeSeconds,
            config.MusicalActivity, config.Patterns, 0d, config.Placement, config.Cadence, song.ManualIntensity);
        int verified = 0;
        for (double time = 0d; time < map.Metadata.DurationSeconds; time += 0.02d)
        {
            var manual = plan.ActivityProfile.ManualIntensity;
            double amount = manual.AmountAt(time);
            double frenzy = plan.ActivityProfile.FrenzyAt(time);
            double speed = plan.ActivityProfile.SpeedMultiplierAt(time);
            double expectedFrenzy = manual.HasSections ? manual.FrenzyAt(time) : config.MusicalActivity.MaximumFrenzy * amount;
            if (Math.Abs(frenzy - expectedFrenzy) > 1e-8d
                || (plan.PhraseProfile.At(time) == MusicalPhrasePressure.Pressure) != manual.Contains(time))
                throw new InvalidOperationException("Presion y frenesi no coinciden con los intervalos.");
            if (!manual.Contains(time) && (frenzy != 0d || Math.Abs(speed - 1d) > 1e-8d))
                throw new InvalidOperationException("Pico fuera de los intervalos.");
            if (!manual.HasSections && amount == 1d && Math.Abs(speed - (1d + (config.MusicalActivity.MaximumSpeedMultiplier - 1d) * config.MusicalActivity.MaximumFrenzy)) > 1e-6d)
                throw new InvalidOperationException("La velocidad no alcanza la configuracion actual.");
            verified++;
        }
        var report = new System.Text.StringBuilder();
        report.AppendLine("PASS: Native Unity imported asset and current scene configuration. Samples verified: " + verified);
        report.AppendLine("Targets: " + plan.Times.Count + "; configured frenzy cap: " + config.MusicalActivity.MaximumFrenzy
            + "; effective peak speed: " + plan.ActivityProfile.SpeedMultiplierAt(90d));
        foreach (var interval in song.ManualIntensity.Intervals)
        {
            int targets = 0;
            foreach (double time in plan.Times) if (time >= interval.Start && time < interval.End) targets++;
            report.AppendLine("Interval [" + interval.Start + "," + interval.End + "): " + targets + " targets.");
        }
        if(song.ManualIntensity.Sections.Length>0)
        {
            var serialized = JsonUtility.FromJson<ManualIntensitySettings>(JsonUtility.ToJson(song.ManualIntensity));
            AssetDatabase.ForceReserializeAssets(new[] {AssetDatabase.GetAssetPath(song)});
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(song),ImportAssetOptions.ForceUpdate);
            if(serialized.Sections.Length!=3)throw new InvalidOperationException("Las tres secciones no persisten.");
            var baselineCadence=JsonUtility.FromJson<BeatCadenceSettings>(JsonUtility.ToJson(config.Cadence));baselineCadence.PressureExtraBeatEvery=4;
            var baselineManual=JsonUtility.FromJson<ManualIntensitySettings>(JsonUtility.ToJson(song.ManualIntensity));baselineManual.Sections=Array.Empty<ManualDifficultySection>();
            var baseline=new MusicalSpawnSchedule(map,config.MinimumSpawnInterval,config.TargetLeadTimeSeconds,config.MusicalActivity,
                config.Patterns,0d,config.Placement,baselineCadence,baselineManual);
            foreach(var section in serialized.Sections)
            {
                int total=0,fakes=0,oldTotal=0,oldFakes=0;
                foreach(var e in plan.Events)if(e.HitTime>=section.Start&&e.HitTime<section.End){total++;if(FalseWordSelector.Select(config.FalseWords,e.HitTime,plan.ActivityProfile.FrenzyAt(e.SpawnTime.Value),0d,section.FalseProbability)!=WordTargetKind.Normal)fakes++;}
                foreach(var e in baseline.Events)if(e.HitTime>=section.Start&&e.HitTime<section.End){oldTotal++;if(FalseWordSelector.Select(config.FalseWords,e.HitTime,baseline.ActivityProfile.FrenzyAt(e.SpawnTime.Value))!=WordTargetKind.Normal)oldFakes++;}
                double midpoint=(section.Start+section.End)/2;
                if(Math.Abs(plan.ActivityProfile.SpeedMultiplierAt(midpoint)-section.SpeedMultiplier)>1e-6 || plan.MaximumTargetsAt(midpoint)!=section.MaximumTargets)
                    throw new InvalidOperationException("Velocidad o capacidad de seccion incorrecta.");
                report.AppendLine("Section ["+section.Start+","+section.End+"): total="+total+", normal="+(total-fakes)+", false="+fakes+"; previous total="+oldTotal+", normal="+(oldTotal-oldFakes)+", false="+oldFakes+"; speed="+section.SpeedMultiplier+", capacity="+section.MaximumTargets);
                if(total<=oldTotal || fakes<=oldFakes || total-fakes<=oldTotal-oldFakes)throw new InvalidOperationException("No aumentaron ambas categorias en la seccion. "+report);
            }
            for(int i=0;i<plan.Events.Count;i++)
            {
                var e=plan.Events[i];int active=0;
                for(int j=0;j<i;j++)if(plan.Events[j].HitTime>=e.SpawnTime.Value-1e-9){active++;if(Math.Abs(plan.Events[j].AngleDegrees.Value-e.AngleDegrees.Value)<config.Placement.LaneSpacingDegrees-1e-6)throw new InvalidOperationException("Rutas superpuestas.");}
                if(active>=plan.MaximumTargetsAt(e.HitTime))throw new InvalidOperationException("Capacidad excedida.");
                if(i>0){var previous=plan.Events[i-1];double gap=Math.Max(plan.ActivityProfile.ManualIntensity.SectionAt(e.HitTime)?.MinimumInterval??config.MinimumSpawnInterval,plan.ActivityProfile.ManualIntensity.SectionAt(previous.HitTime)?.MinimumInterval??config.MinimumSpawnInterval);if(e.HitTime-previous.HitTime<gap-1e-8)throw new InvalidOperationException("Separacion insuficiente.");}
            }
            report.AppendLine("PASS: Serialized sections, increased normal and false categories, speed/capacity per section and full-flight lane/capacity/separation checks.");
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Midnight-current-intensity.txt", report.ToString());
        Debug.Log(report.ToString());
    }
    [MenuItem("Tools/Mentality/Rhythm/Restore And Verify Midnight Intervals")]
    public static void RestoreAndVerifyIntervals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salir de Play Mode antes de restaurar los intervalos.");
        const string path = "Assets/Content/Music/Midnight_at_the_Gallows.asset";
        var song = AssetDatabase.LoadAssetAtPath<MusicalSongAsset>(path);
        if (song == null || song.ManualIntensity == null) throw new InvalidOperationException("Asset Midnight no disponible.");
        float transition = song.ManualIntensity.TransitionSeconds;
        song.ManualIntensity.Intervals = new[] {
            new MusicalActiveInterval { Start = 71.9d, End = 102d },
            new MusicalActiveInterval { Start = 132d, End = 162.1d }
        };
        song.ManualIntensity.Enabled = true;
        EditorUtility.SetDirty(song);
        AssetDatabase.SaveAssetIfDirty(song);
        AssetDatabase.ForceReserializeAssets(new[] { path });
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        song = AssetDatabase.LoadAssetAtPath<MusicalSongAsset>(path);
        var roundTrip = JsonUtility.FromJson<ManualIntensitySettings>(JsonUtility.ToJson(song.ManualIntensity));
        if (roundTrip.Intervals == null || roundTrip.Intervals.Length != 2
            || roundTrip.Intervals[0].Start != 71.9d || roundTrip.Intervals[0].End != 102d
            || roundTrip.Intervals[1].Start != 132d || roundTrip.Intervals[1].End != 162.1d
            || roundTrip.TransitionSeconds != transition)
            throw new InvalidOperationException("Unity no conserva los intervalos o la transicion.");
        string saved = File.ReadAllText(path);
        if (!saved.Contains("Start: 71.9") || !saved.Contains("End: 102")
            || !saved.Contains("Start: 132") || !saved.Contains("End: 162.1"))
            throw new InvalidOperationException("Los intervalos no quedaron persistidos en el asset.");
        var legacy = JsonUtility.FromJson<ManualIntensitySettings>(JsonUtility.ToJson(roundTrip));
        legacy.Sections = Array.Empty<ManualDifficultySection>();
        var profile = new ManualIntensityProfile(song.LoadValidatedMap().Metadata.DurationSeconds, legacy);
        if (!profile.Contains(71.9) || profile.Contains(102) || !profile.Contains(132) || profile.Contains(162.1)
            || profile.AmountAt(90) != 1d || profile.AmountAt(145) != 1d || profile.AmountAt(120) != 0d)
            throw new InvalidOperationException("Activacion de intervalos incorrecta.");
        bool rejectedEmpty = false;
        try { new ManualIntensityProfile(180d, new ManualIntensitySettings { Enabled = true }); }
        catch (ArgumentException) { rejectedEmpty = true; }
        if (!rejectedEmpty) throw new InvalidOperationException("El modo manual vacio debe detectarse.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Midnight-interval-persistence.txt",
            "PASS: Native Unity asset save, force reserialization, forced import, JsonUtility round trip, exact interval boundaries, pressure envelope and empty interval validation.\n"
            + "Intervals: [71.9,102), [132,162.1). Transition preserved: " + transition + " seconds.\n");
        Debug.Log("Midnight interval persistence: PASS. Dos tramos guardados y verificados en Unity.");
    }
    [Serializable] private class Report
    {
        public bool success;
        public List<string> checks = new List<string>();
        public string error;
        public double importedDuration;
        public double analyzedDuration;
        public int beats;
        public int attacks;
    }

    public static void Run()
    {
        var report = new Report();
        try
        {
            var song = AssetDatabase.LoadAssetAtPath<MusicalSongAsset>("Assets/Content/Music/Midnight_at_the_Gallows.asset");
            Require(song != null && song.MusicClip != null && song.AnalysisJson != null, "Audio, JSON y cancion importados", report);
            var map = song.LoadValidatedMap();
            report.importedDuration = (double)song.MusicClip.samples / song.MusicClip.frequency;
            report.analyzedDuration = map.Metadata.DurationSeconds;
            report.beats = map.Events.Beats.Length;
            report.attacks = map.Events.Attacks.Length;
            Require(map.Metadata.AudioFile == "Midnight_at_the_Gallows.mp3", "Mapa asociado al audio Midnight", report);
            Require(Math.Abs(report.importedDuration - report.analyzedDuration) <= 0.1d, "Duracion importada coincide con el analisis", report);
            Require(report.beats == 337 && report.attacks == 548, "Eventos musicales completos y validos", report);
            Require(song.MusicClip.loadType == AudioClipLoadType.Streaming, "Audio largo importado en streaming", report);
            var scene = EditorSceneManager.OpenScene("Assets/Content/Scenes/RhythmTest.unity");
            RhythmLifetimeScope scope = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                scope = root.GetComponentInChildren<RhythmLifetimeScope>(true);
                if (scope != null) break;
            }
            Require(scope != null, "Nivel ritmico disponible", report);
            var config = new SerializedObject(scope).FindProperty("config");
            Require(config.FindPropertyRelative("AnalyzedSong").objectReferenceValue == song
                && config.FindPropertyRelative("MusicClip").objectReferenceValue == song.MusicClip,
                "Nivel usa Midnight en ambas referencias de musica", report);
            Require(config.FindPropertyRelative("Beatmap").FindPropertyRelative("offsetSeconds").floatValue == 0f,
                "Desfase cero para analisis desde el inicio", report);
            var gameConfig = (RhythmGameConfig)config.boxedValue;
            Require(gameConfig.Cadence.Enabled && gameConfig.Cadence.MusicalPhrasesEnabled
                && gameConfig.Cadence.AttackVariationsEnabled && gameConfig.PulseBonus.Enabled,
                "Cadencia, frases, ataques y premio por pulso activos", report);
            var schedule = new MusicalSpawnSchedule(map, gameConfig.MinimumSpawnInterval, gameConfig.TargetLeadTimeSeconds,
                gameConfig.MusicalActivity, gameConfig.Patterns, 0d, gameConfig.Placement, gameConfig.Cadence, song.ManualIntensity);
            Require(gameConfig.Cadence.PressureExtraBeatEvery == 2
                && song.ManualIntensity.Sections.Length == 3 && schedule.Times.Count > 226,
                "Plan ampliado con tres secciones de dificultad", report);
            Require(song.ManualIntensity.Enabled && song.ManualIntensity.Intervals.Length == 2
                && song.ManualIntensity.Intervals[0].Start == 71.9 && song.ManualIntensity.Intervals[0].End == 102d
                && song.ManualIntensity.Intervals[1].Start == 132d && song.ManualIntensity.Intervals[1].End == 162.1,
                "Intervalos manuales coinciden con los tiempos aprobados", report);
            int peakCapacity = 0;
            foreach (double beat in map.Events.Beats) peakCapacity = Math.Max(peakCapacity, schedule.MaximumTargetsAt(beat));
            Require(peakCapacity == 7 && schedule.MaximumTargetsAt(0d) == 5,
                "Capacidad de cinco palabras en calma y hasta siete en picos", report);
            var probe = new RhythmGameModel();
            probe.StartSession();
            probe.ConfigurePulseBonus(map, gameConfig.PulseBonus);
            probe.RegisterJudgment(RhythmJudgment.Hit, map.Events.Beats[0]);
            Require(probe.LastPulseBonus == 50 && probe.Score == 150 && probe.Sanity == 100,
                "Premio por pulso conserva cordura y acierto", report);
            Require(AssetDatabase.LoadAssetAtPath<MusicalSongAsset>("Assets/Content/Music/Waltz_of_the_Abandoned.asset") != null,
                "Cancion anterior conservada", report);
            report.success = true;
        }
        catch (Exception error) { report.error = error.ToString(); }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Midnight-integration.json", JsonUtility.ToJson(report, true));
        if (!report.success) throw new InvalidOperationException(report.error);
        Debug.Log("Midnight integration: " + report.checks.Count + " checks passed.");
    }

    private static void Require(bool valid, string label, Report report)
    {
        if (!valid) throw new InvalidOperationException(label);
        report.checks.Add(label);
    }
}
