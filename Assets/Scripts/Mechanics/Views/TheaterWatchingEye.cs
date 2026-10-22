using System;
using Gameplay.Rhythm.Scopes;
using Gameplay.Rhythm.Services;
using UnityEngine;
using VContainer;

namespace Gameplay.Rhythm.Views
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class TheaterWatchingEye : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Sprite[] animationFrames = Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;
        [SerializeField, Min(0f)] private float openHoldSeconds = 2.5f;
        [SerializeField, Min(0f)] private float phaseOffsetSeconds;
        private static readonly int EyeRect = Shader.PropertyToID("_EyeUvRect");
        private MaterialPropertyBlock properties;
        private SpriteRenderer visual;
        private AudioConductorService clock;
        public Transform PlayerTarget => playerTarget;
        public int AnimationFrameCount => animationFrames.Length;
        public float FramesPerSecond => framesPerSecond;
        public double AnimationCycleSeconds => animationFrames.Length / (double)framesPerSecond + openHoldSeconds;

        public void Configure(Transform target)
        {
            playerTarget = target;
            RefreshSprite();
            FacePlayer();
        }

        public void ConfigureAnimation(Sprite[] frames, float fps, float holdSeconds, float phaseSeconds)
        {
            if (frames == null || frames.Length != 8 || Array.Exists(frames, f => f == null)
                || !float.IsFinite(fps) || fps < 1f || !float.IsFinite(holdSeconds) || holdSeconds < 0f
                || !float.IsFinite(phaseSeconds) || phaseSeconds < 0f)
                throw new ArgumentException("Eight eye frames and valid animation timing required.");
            animationFrames = (Sprite[])frames.Clone();
            framesPerSecond = fps; openHoldSeconds = holdSeconds; phaseOffsetSeconds = phaseSeconds;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            var scope = FindFirstObjectByType<RhythmLifetimeScope>();
            if (scope != null && scope.Container != null) clock = scope.Container.Resolve<AudioConductorService>();
        }

        private void OnEnable() { RefreshSprite(); FacePlayer(); }
        private void OnValidate() { RefreshSprite(); FacePlayer(); }
        private void LateUpdate()
        {
            FacePlayer();
            if (Application.isPlaying) SampleAnimationAt(clock == null ? Time.timeAsDouble : Math.Max(0d, clock.CurrentSongTime));
        }

        public void SampleAnimationAt(double seconds)
        {
            if (animationFrames.Length == 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            double cycleTime = (Math.Max(0d, seconds) + phaseOffsetSeconds) % AnimationCycleSeconds;
            int index = Math.Min(animationFrames.Length - 1, (int)(cycleTime * framesPerSecond));
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (visual.sprite == animationFrames[index]) return;
            visual.sprite = animationFrames[index];
            RefreshSprite();
        }

        private void RefreshSprite()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (visual == null || visual.sprite == null) return;
            var sprite = visual.sprite;
            var rect = sprite.textureRect;
            properties ??= new MaterialPropertyBlock();
            visual.GetPropertyBlock(properties);
            properties.SetVector(EyeRect, new Vector4(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
                rect.width / sprite.texture.width, rect.height / sprite.texture.height));
            visual.SetPropertyBlock(properties);
        }

        public void FacePlayer()
        {
            if (playerTarget == null) return;
            Vector3 direction = transform.position - playerTarget.position;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
