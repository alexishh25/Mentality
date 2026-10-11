using System;
using Gameplay.Rhythm.Services;
using UnityEngine;

namespace Gameplay.Rhythm.Models
{
    [CreateAssetMenu(menuName = "Mentality/Rhythm/Analyzed Song")]
    public sealed class MusicalSongAsset : ScriptableObject
    {
        public AudioClip MusicClip;
        public TextAsset AnalysisJson;
        [Tooltip("Nombre original del archivo analizado, incluida su extension.")]
        public string AnalyzedAudioFile;
        public ManualIntensitySettings ManualIntensity = new ManualIntensitySettings();

        public MusicalMapData LoadValidatedMap()
        {
            if (MusicClip == null || AnalysisJson == null)
                throw new InvalidOperationException("La cancion analizada necesita AudioClip y JSON.");
            var map = MusicalMapLoader.Parse(AnalysisJson.text);
            MusicalMapLoader.ValidateAudio(map, AnalyzedAudioFile,
                (double)MusicClip.samples / MusicClip.frequency);
            return map;
        }
    }
}
