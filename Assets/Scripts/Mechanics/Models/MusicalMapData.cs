using Newtonsoft.Json;

namespace Gameplay.Rhythm.Models
{
    // Contrato del JSON de analisis; todos los tiempos parten del inicio del clip.
    public sealed class MusicalMapData
    {
        [JsonProperty("metadata", Required = Required.Always)] public MusicalMapMetadata Metadata;
        [JsonProperty("eventos_discretos", Required = Required.Always)] public MusicalMapEvents Events;
        [JsonProperty("intervalos_activos", Required = Required.Always)] public MusicalActiveInterval[] ActiveIntervals;
        [JsonProperty("curvas_continuas", Required = Required.Always)] public MusicalMapCurves Curves;
    }

    public sealed class MusicalMapMetadata
    {
        [JsonProperty("archivo_analizado", Required = Required.Always)] public string AudioFile;
        [JsonProperty("contexto_analisis", Required = Required.Always)] public string AnalysisContext;
        [JsonProperty("duracion_segundos", Required = Required.Always)] public double DurationSeconds;
        [JsonProperty("frecuencia_muestreo", Required = Required.Always)] public int AnalysisSampleRate;
        [JsonProperty("tempo_bpm", Required = Required.Always)] public double TempoBpm;
    }

    public sealed class MusicalMapEvents
    {
        [JsonProperty("beats", Required = Required.Always)] public double[] Beats;
        [JsonProperty("ataques", Required = Required.Always)] public double[] Attacks;
    }

    public sealed class MusicalActiveInterval
    {
        [JsonProperty("inicio", Required = Required.Always)] public double Start;
        [JsonProperty("fin", Required = Required.Always)] public double End;
    }

    public sealed class MusicalMapCurves
    {
        [JsonProperty("intensidad", Required = Required.Always)] public MusicalCurve Intensity;
        [JsonProperty("transiciones", Required = Required.Always)] public MusicalCurve Transitions;
    }

    public sealed class MusicalCurve
    {
        [JsonProperty("tiempos", Required = Required.Always)] public double[] Times;
        [JsonProperty("valores_normalizados", Required = Required.Always)] public double[] Values;
    }
}
