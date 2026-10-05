using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;
using VContainer;

namespace Gameplay.Ghosts.Views
{
    // Test-only green glow driven by the analyzed musical intensity curve.
    [RequireComponent(typeof(TargetView), typeof(Renderer))]
    public sealed class FrequencyGlowPreview : MonoBehaviour
    {
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        [SerializeField, Min(0f)] private float baseGlow = 0.18f;
        [SerializeField, Min(0f)] private float intensityGlow = 0.72f;
        [SerializeField] private Color green = new Color(0.08f, 1f, 0.32f, 1f);

        private TargetView _target;
        private Renderer _renderer;
        private AudioConductorService _conductor;
        private MaterialPropertyBlock _properties;

        private void Awake()
        {
            _target = GetComponent<TargetView>();
            _renderer = GetComponent<Renderer>();
            _properties = new MaterialPropertyBlock();
        }

        [Inject]
        public void Construct(AudioConductorService conductor) => _conductor = conductor;

        private void Update()
        {
            if (_renderer == null) return;
            double intensity = 0.25d;
            TargetTiming timing = _target != null ? _target.Timing : null;
            if (timing?.MotionProfile != null && _conductor != null)
                intensity = timing.MotionProfile.IntensityAt(_conductor.CurrentSongTime);

            _renderer.GetPropertyBlock(_properties);
            _properties.SetFloat(GlowStrengthId, baseGlow + intensityGlow * (float)intensity);
            _properties.SetColor(GlowColorId, green);
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
