using System;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    public static class RhythmScoreRules
    {
        public const int PerfectPoints = 100;
        public const int HitPoints = 100;
        public const int GoodPoints = 60;
        public const int ComboStep = 10;
        public const int MaximumMultiplier = 4;

        public static int MultiplierFor(int combo)
        {
            if (combo < 0) throw new ArgumentOutOfRangeException(nameof(combo));
            return Math.Min(MaximumMultiplier, 1 + combo / ComboStep);
        }

        public static int PointsFor(RhythmJudgment judgment, int combo)
        {
            int multiplier = MultiplierFor(combo);
            switch (judgment)
            {
                case RhythmJudgment.Perfect: return PerfectPoints * multiplier;
                case RhythmJudgment.Hit: return HitPoints * multiplier;
                case RhythmJudgment.Good: return GoodPoints * multiplier;
                case RhythmJudgment.OutOfTime: return 0;
                case RhythmJudgment.Miss: return 0;
                default: throw new ArgumentOutOfRangeException(nameof(judgment));
            }
        }
    }
}
