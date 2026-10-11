using Mentality.Gameplay.MG01;
using NUnit.Framework;
using UnityEngine;

namespace Mentality.Tests
{
    public class SanityServiceTests
    {
        private MG01ConfigSO _config;
        private SanityService _sanity;

        [SetUp]
        public void SetUp()
        {
            _config = MG01ConfigSO.CreateDefault();
            _sanity = new SanityService(_config);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void StartsFullAtHundred()
        {
            Assert.AreEqual(100f, _sanity.Max);
            Assert.AreEqual(100f, _sanity.Current);
            Assert.AreEqual(1f, _sanity.Normalized);
        }

        [Test]
        public void EachHitRemovesTen()
        {
            _sanity.ApplyHit();
            Assert.AreEqual(90f, _sanity.Current);
            _sanity.ApplyHit();
            Assert.AreEqual(80f, _sanity.Current);
        }

        [Test]
        public void ClampsAtZero()
        {
            for (int i = 0; i < 15; i++) _sanity.ApplyHit();
            Assert.AreEqual(0f, _sanity.Current);
            Assert.IsTrue(_sanity.IsDepleted);
        }

        [Test]
        public void DepletedFiresExactlyOnce()
        {
            int depleted = 0;
            _sanity.Depleted += () => depleted++;

            for (int i = 0; i < 15; i++) _sanity.ApplyHit();

            Assert.AreEqual(1, depleted);
        }

        [Test]
        public void DoesNotRegenerateOverTime()
        {
            _sanity.ApplyHit();
            float afterHit = _sanity.Current;

            // No hay Tick ni regeneracion: el valor solo cambia con ApplyHit o Reset.
            Assert.AreEqual(afterHit, _sanity.Current);
            Assert.AreEqual(90f, afterHit);
        }

        [Test]
        public void ChangedReportsCurrentAndMax()
        {
            float reportedCurrent = -1f, reportedMax = -1f;
            _sanity.Changed += (current, max) => { reportedCurrent = current; reportedMax = max; };

            _sanity.ApplyHit();

            Assert.AreEqual(90f, reportedCurrent);
            Assert.AreEqual(100f, reportedMax);
        }

        [Test]
        public void ResetRestoresAndAllowsNewDepletion()
        {
            int depleted = 0;
            _sanity.Depleted += () => depleted++;
            for (int i = 0; i < 10; i++) _sanity.ApplyHit();

            _sanity.Reset();
            Assert.AreEqual(100f, _sanity.Current);
            Assert.IsFalse(_sanity.IsDepleted);

            for (int i = 0; i < 10; i++) _sanity.ApplyHit();
            Assert.AreEqual(2, depleted);
        }
    }
}
