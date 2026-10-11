using Mentality.Gameplay.MG01;
using Mentality.UI.ViewModels;
using NUnit.Framework;
using UnityEngine;

namespace Mentality.Tests
{
    public class HudViewModelTests
    {
        private const float Frame = 1f / 60f;

        private MG01ConfigSO _config;
        private SanityService _sanity;
        private FakeSongProgressSource _song;
        private HudViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _config = MG01ConfigSO.CreateDefault();
            _sanity = new SanityService(_config);
            _song = new FakeSongProgressSource();
            _viewModel = new HudViewModel(_sanity, _song, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _viewModel.Dispose();
            Object.DestroyImmediate(_config);
        }

        private void Advance(float seconds, float step = Frame)
        {
            for (float t = 0f; t < seconds; t += step) _viewModel.Tick(step);
        }

        [Test]
        public void StartsDisplayingFullSanity()
        {
            Assert.AreEqual(1f, _viewModel.DisplayedSanityNormalized);
            Assert.AreEqual(100, _viewModel.DisplayedSanityValue);
            Assert.AreEqual(SanityBand.High, _viewModel.Band);
        }

        [Test]
        public void HitMovesTargetImmediatelyButDisplayAnimates()
        {
            _sanity.ApplyHit();

            Assert.AreEqual(0.9f, _viewModel.TargetSanityNormalized, 1e-5f);
            Assert.AreEqual(1f, _viewModel.DisplayedSanityNormalized, "Sin Tick no debe saltar.");

            _viewModel.Tick(Frame);

            Assert.Less(_viewModel.DisplayedSanityNormalized, 1f);
            Assert.Greater(_viewModel.DisplayedSanityNormalized, 0.9f);
        }

        [Test]
        public void SmoothingApproachesMonotonicallyAndSettlesWithinHalfSecond()
        {
            _sanity.ApplyHit();
            float previous = _viewModel.DisplayedSanityNormalized;

            for (float t = 0f; t < 0.5f; t += Frame)
            {
                _viewModel.Tick(Frame);
                float current = _viewModel.DisplayedSanityNormalized;
                Assert.LessOrEqual(current, previous);
                Assert.GreaterOrEqual(current, _viewModel.TargetSanityNormalized);
                previous = current;
            }

            Assert.AreEqual(90, _viewModel.DisplayedSanityValue);
            Assert.AreEqual(0.9f, _viewModel.DisplayedSanityNormalized, 0.002f);
        }

        [Test]
        public void SmoothingIsFramerateIndependent()
        {
            var otherSanity = new SanityService(_config);
            var other = new HudViewModel(otherSanity, _song, _config);

            _sanity.ApplyHit();
            otherSanity.ApplyHit();

            for (int i = 0; i < 6; i++) _viewModel.Tick(1f / 60f);
            for (int i = 0; i < 3; i++) other.Tick(1f / 30f);

            Assert.AreEqual(_viewModel.DisplayedSanityNormalized, other.DisplayedSanityNormalized, 1e-4f);
            other.Dispose();
        }

        [Test]
        public void DisplayedValueStaysWithinZeroAndOne()
        {
            for (int i = 0; i < 15; i++)
            {
                _sanity.ApplyHit();
                _viewModel.Tick(10f);
                Assert.GreaterOrEqual(_viewModel.DisplayedSanityNormalized, 0f);
                Assert.LessOrEqual(_viewModel.DisplayedSanityNormalized, 1f);
            }

            Assert.AreEqual(0f, _viewModel.DisplayedSanityNormalized);
            Assert.AreEqual(SanityBand.Critical, _viewModel.Band);
        }

        [Test]
        public void HugeDeltaTimeDoesNotOvershoot()
        {
            _sanity.ApplyHit();
            _viewModel.Tick(1000f);

            Assert.AreEqual(0.9f, _viewModel.DisplayedSanityNormalized, 1e-5f);
        }

        [Test]
        public void HitFeedbackIsTemporary()
        {
            _sanity.ApplyHit();
            Assert.IsTrue(_viewModel.IsHitActive);
            Assert.AreEqual("IMPACTO · −10", _viewModel.StatusText);
            Assert.Greater(_viewModel.GhostTrailWidth, 0f);

            Advance(1f);

            Assert.IsFalse(_viewModel.IsHitActive);
            Assert.AreEqual(0f, _viewModel.GhostTrailWidth);
            Assert.AreEqual(MG01Texts.StatusHigh, _viewModel.StatusText);
        }

        [Test]
        public void BandsFollowFigmaThresholds()
        {
            Assert.AreEqual(SanityBand.High, SanityBands.FromNormalized(0.76f));
            Assert.AreEqual(SanityBand.Mid, SanityBands.FromNormalized(0.75f));
            Assert.AreEqual(SanityBand.Mid, SanityBands.FromNormalized(0.51f));
            Assert.AreEqual(SanityBand.Low, SanityBands.FromNormalized(0.50f));
            Assert.AreEqual(SanityBand.Low, SanityBands.FromNormalized(0.26f));
            Assert.AreEqual(SanityBand.Critical, SanityBands.FromNormalized(0.25f));
        }

        [Test]
        public void ProgressIsClamped()
        {
            _song.Normalized = 1.5f;
            Assert.AreEqual(1f, _viewModel.ProgressNormalized);
            _song.Normalized = -1f;
            Assert.AreEqual(0f, _viewModel.ProgressNormalized);
        }
    }
}
