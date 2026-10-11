using Mentality.Gameplay.MG01;
using NUnit.Framework;
using UnityEngine;

namespace Mentality.Tests
{
    public class DebugSongProgressSourceTests
    {
        private MG01ConfigSO _config;
        private PauseService _pause;
        private DebugSongProgressSource _song;

        [SetUp]
        public void SetUp()
        {
            _config = MG01ConfigSO.CreateDefault();
            _pause = new PauseService();
            _song = new DebugSongProgressSource(_config, _pause);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void ProgressGoesFromZeroToOneOverDuration()
        {
            int finished = 0;
            _song.Finished += () => finished++;

            Assert.AreEqual(0f, _song.Normalized);
            _song.Advance(_song.Duration * 0.5f);
            Assert.AreEqual(0.5f, _song.Normalized, 1e-4f);
            _song.Advance(_song.Duration);

            Assert.AreEqual(1f, _song.Normalized);
            Assert.IsTrue(_song.IsFinished);
            Assert.AreEqual(1, finished);
        }

        [Test]
        public void PauseStopsProgress()
        {
            _song.Advance(1f);
            float before = _song.Normalized;

            _pause.SetPaused(true);
            _song.Advance(10f);
            Assert.AreEqual(before, _song.Normalized);

            _pause.SetPaused(false);
            _song.Advance(1f);
            Assert.Greater(_song.Normalized, before);
        }

        [Test]
        public void HaltStopsProgress()
        {
            _pause.Halt();
            _song.Advance(10f);
            Assert.AreEqual(0f, _song.Normalized);
        }
    }
}
