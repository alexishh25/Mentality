namespace Gameplay.Ghosts.Models
{
    public enum GhostSpawnCueType
    {
        Offbeat,
        Silence
    }

    public readonly struct GhostSpawnCue
    {
        public double Time { get; }
        public GhostSpawnCueType Type { get; }

        public GhostSpawnCue(double time, GhostSpawnCueType type)
        {
            Time = time;
            Type = type;
        }
    }
}
