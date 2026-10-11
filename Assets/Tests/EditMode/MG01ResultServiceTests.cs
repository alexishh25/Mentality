using Mentality.Gameplay.MG01;
using NUnit.Framework;
using UnityEngine;

namespace Mentality.Tests
{
    public class MG01ResultServiceTests
    {
        private MG01ConfigSO _config;
        private SanityService _sanity;
        private FakeSongProgressSource _song;
        private PauseService _pause;
        private MG01ResultService _result;
        private int _runEndedCount;
        private MG01Outcome _lastOutcome;
        private MG01RunStats _lastStats;

        [SetUp]
        public void SetUp()
        {
            _config = MG01ConfigSO.CreateDefault();
            _sanity = new SanityService(_config);
            _song = new FakeSongProgressSource();
            _pause = new PauseService();
            _result = new MG01ResultService(_sanity, _song, _pause);
            _runEndedCount = 0;
            _result.RunEnded += (outcome, stats) =>
            {
                _runEndedCount++;
                _lastOutcome = outcome;
                _lastStats = stats;
            };
        }

        [TearDown]
        public void TearDown()
        {
            _result.Dispose();
            Object.DestroyImmediate(_config);
        }

        private void HitTimes(int count)
        {
            for (int i = 0; i < count; i++) _sanity.ApplyHit();
        }

        [Test]
        public void DepletingSanityIsDefeat()
        {
            HitTimes(10);

            Assert.AreEqual(1, _runEndedCount);
            Assert.AreEqual(MG01Outcome.Defeat, _lastOutcome);
            Assert.AreEqual(0f, _lastStats.sanityRemaining);
            Assert.IsTrue(_pause.IsHalted);
        }

        [Test]
        public void SongEndWithSanityIsVictoryAtEndOfFrame()
        {
            HitTimes(3);
            _song.Finish();

            Assert.AreEqual(0, _runEndedCount, "La victoria espera al final del frame para resolver el dano primero.");

            _result.LateTick();

            Assert.AreEqual(1, _runEndedCount);
            Assert.AreEqual(MG01Outcome.Victory, _lastOutcome);
            Assert.AreEqual(70f, _lastStats.sanityRemaining);
        }

        [Test]
        public void LethalHitInSameFrameAsSongEndIsDefeat()
        {
            HitTimes(9);
            _song.Finish();
            _sanity.ApplyHit();
            _result.LateTick();

            Assert.AreEqual(1, _runEndedCount);
            Assert.AreEqual(MG01Outcome.Defeat, _lastOutcome);
        }

        [Test]
        public void NonLethalHitInSameFrameAsSongEndIsVictory()
        {
            HitTimes(8);
            _song.Finish();
            _sanity.ApplyHit();
            _result.LateTick();

            Assert.AreEqual(1, _runEndedCount);
            Assert.AreEqual(MG01Outcome.Victory, _lastOutcome);
            Assert.AreEqual(10f, _lastStats.sanityRemaining);
        }

        [Test]
        public void RunEndedFiresOnce()
        {
            HitTimes(10);
            _song.Finish();
            _result.LateTick();
            HitTimes(3);
            _result.LateTick();

            Assert.AreEqual(1, _runEndedCount);
            Assert.AreEqual(MG01Outcome.Defeat, _result.Outcome);
        }

        [Test]
        public void StatsCountWordsHitsAndGhosts()
        {
            _result.ReportWordDestroyed();
            _result.ReportWordDestroyed();
            _result.ReportGhostClicked();
            HitTimes(2);
            _song.Finish();
            _result.LateTick();

            Assert.AreEqual(2, _lastStats.wordsDestroyed);
            Assert.AreEqual(1, _lastStats.ghostClicks);
            Assert.AreEqual(2, _lastStats.hitsTaken);
            Assert.AreEqual(80f, _lastStats.sanityRemaining);
        }

        [Test]
        public void ReportsAfterRunEndedAreIgnored()
        {
            HitTimes(10);
            _result.ReportWordDestroyed();
            _result.ReportGhostClicked();

            Assert.AreEqual(0, _result.Stats.wordsDestroyed);
            Assert.AreEqual(0, _result.Stats.ghostClicks);
        }
    }
}
