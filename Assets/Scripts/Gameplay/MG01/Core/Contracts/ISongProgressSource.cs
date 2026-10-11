using System;

namespace Mentality.Gameplay.MG01
{
    // Lo implementara el conductor ritmico; mientras tanto se usa DebugSongProgressSource.
    public interface ISongProgressSource
    {
        // 0..1
        float Normalized { get; }
        bool IsFinished { get; }
        event Action Finished;
    }
}
