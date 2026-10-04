namespace Gameplay.Rhythm.Models
{
    public sealed class MusicalSpawnEvent
    {
        public double HitTime { get; }
        public float? AngleDegrees { get; }
        public RhythmPattern Pattern { get; }
        public double? SpawnTime { get; }

        public MusicalSpawnEvent(double hitTime, float? angleDegrees = null, RhythmPattern pattern = RhythmPattern.Random,
            double? spawnTime = null)
        { HitTime = hitTime; AngleDegrees = angleDegrees; Pattern = pattern; SpawnTime = spawnTime; }
    }
}
