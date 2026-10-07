using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public static class RhythmFeedbackFormatter
    {
        public const double DurationSeconds = 0.8d;

        public static string Status(RhythmJudgment judgment, double? error)
        {
            if (judgment == RhythmJudgment.Hit) return "ACIERTO";
            if (judgment == RhythmJudgment.Miss) return "FALLO: EL OBJETO LLEGO AL JUGADOR";
            string result = judgment == RhythmJudgment.Perfect ? "PERFECTO"
                : judgment == RhythmJudgment.Good ? "BIEN" : "FUERA DE TIEMPO";
            if (error.HasValue)
            {
                string direction = Math.Abs(error.Value) < 0.0005d ? "" : error.Value < 0 ? " ANTES" : " DESPUES";
                result += $"  /  {Math.Abs(error.Value) * 1000d:0} ms{direction}";
            }
            else if (judgment == RhythmJudgment.OutOfTime) result += "  /  SIN CLIC";
            return result;
        }

        public static string Result(RhythmGameModel model)
        {
            if (model.LastJudgment == RhythmJudgment.None) return "";
            if (model.LastJudgment == RhythmJudgment.OutOfTime || model.LastJudgment == RhythmJudgment.Miss)
                return model.LastBrokenCombo > 0 ? $"FALLO\nRACHA PERDIDA: {model.LastBrokenCombo}" : "FALLO";
            string label = model.LastJudgment == RhythmJudgment.Hit ? "ACIERTO"
                : model.LastJudgment == RhythmJudgment.Perfect ? "PERFECTO" : "BIEN";
            return $"{label}  +{model.LastScoreGain} pts\nCOMBO {model.Combo}  /  x{model.ScoreMultiplier}";
        }
    }
}
