using System;
using Gameplay.Rhythm.Services;

namespace Gameplay.Rhythm.Models
{
    public class RhythmGameModel
    {
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int PerfectHits { get; private set; }
        public int GoodHits { get; private set; }
        public int TimingMisses { get; private set; }
        public RhythmJudgment LastJudgment { get; private set; }
        public double? LastTimingErrorSeconds { get; private set; }
        public double LastJudgmentSongTime { get; private set; }
        public long Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int ScoreMultiplier => RhythmScoreRules.MultiplierFor(Combo);
        public int LastScoreGain { get; private set; }
        public int LastBrokenCombo { get; private set; }

        public void StartSession()
        {
            Hits = 0;
            Misses = 0;
            PerfectHits = 0;
            GoodHits = 0;
            TimingMisses = 0;
            LastJudgment = RhythmJudgment.None;
            LastTimingErrorSeconds = null;
            LastJudgmentSongTime = 0d;
            Score = 0;
            Combo = 0;
            BestCombo = 0;
            LastScoreGain = 0;
            LastBrokenCombo = 0;
            IsPlaying = true;
            IsPaused = false;
        }

        public void StopSession()
        {
            IsPlaying = false;
            IsPaused = false;
        }

        public void PauseSession() { if (IsPlaying) IsPaused = true; }
        public void ResumeSession() { if (IsPlaying) IsPaused = false; }

        public void RegisterHit()
        {
            if (IsPlaying && !IsPaused)
            {
                Hits++;
                RewardHit(RhythmJudgment.Perfect);
            }
        }

        public void RegisterMiss()
        {
            if (IsPlaying && !IsPaused)
            {
                Misses++;
                BreakCombo();
            }
        }

        public void RegisterJudgment(RhythmJudgment judgment, double songTime, double? signedErrorSeconds = null)
        {
            if (!IsPlaying || IsPaused) return;
            if (judgment != RhythmJudgment.Perfect && judgment != RhythmJudgment.Good && judgment != RhythmJudgment.OutOfTime
                && judgment != RhythmJudgment.Hit && judgment != RhythmJudgment.Miss)
                throw new ArgumentOutOfRangeException(nameof(judgment));
            if (double.IsNaN(songTime) || double.IsInfinity(songTime))
                throw new ArgumentOutOfRangeException(nameof(songTime));
            if (signedErrorSeconds.HasValue && (double.IsNaN(signedErrorSeconds.Value) || double.IsInfinity(signedErrorSeconds.Value)))
                throw new ArgumentOutOfRangeException(nameof(signedErrorSeconds));
            LastJudgment = judgment;
            LastJudgmentSongTime = songTime;
            LastTimingErrorSeconds = signedErrorSeconds;
            if (judgment == RhythmJudgment.Perfect) { PerfectHits++; Hits++; RewardHit(judgment); }
            else if (judgment == RhythmJudgment.Good) { GoodHits++; Hits++; RewardHit(judgment); }
            else if (judgment == RhythmJudgment.Hit) { Hits++; RewardHit(judgment); }
            else { if (judgment == RhythmJudgment.OutOfTime) TimingMisses++; Misses++; BreakCombo(); }
        }

        private void RewardHit(RhythmJudgment judgment)
        {
            Combo++;
            BestCombo = Math.Max(BestCombo, Combo);
            LastScoreGain = RhythmScoreRules.PointsFor(judgment, Combo);
            Score += LastScoreGain;
            LastBrokenCombo = 0;
        }

        private void BreakCombo()
        {
            LastBrokenCombo = Combo;
            Combo = 0;
            LastScoreGain = 0;
        }
    }
}
