using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public sealed class RhythmJudgmentWindows
    {
        // Solo compensa el redondeo double en limites inclusivos, no la latencia del jugador.
        private const double BoundaryTolerance = 0.000000001d;
        public double PerfectSeconds { get; }
        public double GoodSeconds { get; }

        public RhythmJudgmentWindows(double perfectSeconds = 0.06d, double goodSeconds = 0.15d)
        {
            if (!IsFinite(perfectSeconds) || perfectSeconds <= 0d)
                throw new ArgumentOutOfRangeException(nameof(perfectSeconds), "La ventana Perfecto debe ser positiva y finita.");
            if (!IsFinite(goodSeconds) || goodSeconds < perfectSeconds)
                throw new ArgumentOutOfRangeException(nameof(goodSeconds), "La ventana Bien debe ser finita y mayor o igual a Perfecto.");
            PerfectSeconds = perfectSeconds;
            GoodSeconds = goodSeconds;
        }

        public RhythmJudgment Evaluate(double signedErrorSeconds)
        {
            if (!IsFinite(signedErrorSeconds)) throw new ArgumentOutOfRangeException(nameof(signedErrorSeconds));
            double error = Math.Abs(signedErrorSeconds);
            if (error <= PerfectSeconds + BoundaryTolerance) return RhythmJudgment.Perfect;
            if (error <= GoodSeconds + BoundaryTolerance) return RhythmJudgment.Good;
            return RhythmJudgment.OutOfTime;
        }

        public bool HasExpired(double signedErrorSeconds)
        {
            if (!IsFinite(signedErrorSeconds)) throw new ArgumentOutOfRangeException(nameof(signedErrorSeconds));
            return signedErrorSeconds > GoodSeconds + BoundaryTolerance;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
