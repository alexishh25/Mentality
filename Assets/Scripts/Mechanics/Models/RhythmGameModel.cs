namespace Gameplay.Rhythm.Models
{
    public class RhythmGameModel
    {
        public bool IsPlaying { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }

        public void StartSession()
        {
            Hits = 0;
            Misses = 0;
            IsPlaying = true;
        }

        public void StopSession()
        {
            IsPlaying = false;
        }

        public void RegisterHit()
        {
            if (IsPlaying)
                Hits++;
        }

        public void RegisterMiss()
        {
            if (IsPlaying)
                Misses++;
        }
    }
}
