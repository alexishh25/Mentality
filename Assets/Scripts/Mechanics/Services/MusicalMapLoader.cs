using System;
using System.IO;
using Gameplay.Rhythm.Models;
using Newtonsoft.Json;

namespace Gameplay.Rhythm.Services
{
    public static class MusicalMapLoader
    {
        public static MusicalMapData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("El mapa musical esta vacio.", nameof(json));
            var map = JsonConvert.DeserializeObject<MusicalMapData>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
            Validate(map);
            return map;
        }

        public static void Validate(MusicalMapData map)
        {
            Require(map != null && map.Metadata != null && map.Events != null && map.Curves != null,
                "Faltan secciones del mapa musical.");
            var metadata = map.Metadata;
            Require(!string.IsNullOrWhiteSpace(metadata.AudioFile)
                && !string.IsNullOrWhiteSpace(metadata.AnalysisContext), "Falta la identificacion del audio.");
            Require(IsFinite(metadata.DurationSeconds) && metadata.DurationSeconds > 0,
                "La duracion debe ser positiva y finita.");
            Require(metadata.AnalysisSampleRate > 0 && IsFinite(metadata.TempoBpm) && metadata.TempoBpm >= 1,
                "La frecuencia de analisis o el tempo no son validos.");
            ValidateTimes(map.Events.Beats, metadata.DurationSeconds, "beats");
            ValidateTimes(map.Events.Attacks, metadata.DurationSeconds, "ataques");
            Require(map.ActiveIntervals != null, "Faltan los intervalos activos.");
            double previousEnd = 0;
            foreach (var interval in map.ActiveIntervals)
            {
                Require(interval != null && IsFinite(interval.Start) && IsFinite(interval.End)
                    && interval.Start >= previousEnd && interval.Start < interval.End
                    && interval.End <= metadata.DurationSeconds, "Intervalo activo invalido o solapado.");
                previousEnd = interval.End;
            }
            ValidateCurve(map.Curves.Intensity, metadata.DurationSeconds, "intensidad");
            ValidateCurve(map.Curves.Transitions, metadata.DurationSeconds, "transiciones");
        }

        // La frecuencia del analisis puede diferir de la del clip importado por Unity.
        public static void ValidateAudio(MusicalMapData map, string analyzedFile, double clipDuration)
        {
            Require(string.Equals(Path.GetFileName(map.Metadata.AudioFile), Path.GetFileName(analyzedFile),
                StringComparison.OrdinalIgnoreCase), "El mapa no corresponde al archivo de audio asociado.");
            Require(IsFinite(clipDuration) && clipDuration > 0
                && Math.Abs(clipDuration - map.Metadata.DurationSeconds) <= 0.1d,
                "La duracion del clip no coincide con el mapa musical (tolerancia: 0.1 s).");
        }

        private static void ValidateCurve(MusicalCurve curve, double duration, string label)
        {
            Require(curve != null && curve.Values != null, "Falta la curva de " + label + ".");
            ValidateTimes(curve.Times, duration, label);
            Require(curve.Times.Length > 0 && curve.Times.Length == curve.Values.Length,
                "Los tiempos y valores de " + label + " no coinciden.");
            foreach (double value in curve.Values)
                Require(IsFinite(value) && value >= 0 && value <= 1,
                    "Valor no normalizado en " + label + ".");
        }

        private static void ValidateTimes(double[] times, double duration, string label)
        {
            Require(times != null, "Faltan los tiempos de " + label + ".");
            double previous = -1;
            foreach (double time in times)
            {
                Require(IsFinite(time) && time >= 0 && time <= duration && time > previous,
                    "Tiempo fuera de rango, repetido o desordenado en " + label + ".");
                previous = time;
            }
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
