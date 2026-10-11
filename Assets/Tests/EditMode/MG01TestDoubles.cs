using System;
using Mentality.Gameplay.MG01;

namespace Mentality.Tests
{
    internal sealed class FakeSongProgressSource : ISongProgressSource
    {
        public float Normalized { get; set; }
        public bool IsFinished { get; private set; }
        public event Action Finished;

        public void Finish()
        {
            Normalized = 1f;
            IsFinished = true;
            Finished?.Invoke();
        }
    }
}
