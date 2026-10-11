using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using Mentality.UI.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gameplay.Rhythm.Views
{
    // Overlay persistente del ritmo. Las pantallas de resultado las instancia UIDoc.
    public class RhythmHudView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private static readonly string[] BrainClasses = { "brain--stable", "brain--nervous", "brain--uneasy", "brain--collapse" };
        private static readonly string[] StateLabels = { "Mente estable", "Nerviosismo", "Inquietud", "Colaps" };

        private SanityWedgeBar _sanityBar;
        private VisualElement _portrait;
        private Label _sanityValue;
        private Label _labelState;
        private Label _comboLabel;
        private VisualElement _reticle;
        private int _band = -1;
        private bool _bound;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = false;
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }

        public bool TryBind()
        {
            if (_bound) return true;
            if (document == null) document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null) return false;

            VisualElement root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _portrait = root.Q<VisualElement>("Portrait");
            _sanityValue = root.Q<Label>("sanityValue");
            _labelState = root.Q<Label>("labelState");
            _sanityBar = root.Q<SanityWedgeBar>("sanityTrack");
            _comboLabel = root.Q<Label>("comboLabel");
            _reticle = root.Q<VisualElement>("reticle");
            _bound = _sanityValue != null && _sanityBar != null && _comboLabel != null && _reticle != null;
            return _bound;
        }

        public void RenderSanity(int sanity)
        {
            if (!TryBind()) return;
            int value = Mathf.Clamp(sanity, 0, RhythmGameModel.MaximumSanity);
            float fraction = value / (float)RhythmGameModel.MaximumSanity;
            _sanityValue.text = $"{value}";
            _sanityBar.value = fraction;
            _sanityValue.style.color = Color.Lerp(new Color(1f, 0.35f, 0.35f), new Color(0.25f, 0.95f, 0.7f), fraction);
            ApplyBand(value >= 76 ? 0 : value >= 51 ? 1 : value >= 26 ? 2 : 3);
        }

        private void ApplyBand(int band)
        {
            if (band == _band) return;
            _band = band;
            if (_portrait != null)
            {
                for (int i = 0; i < BrainClasses.Length; i++)
                    _portrait.EnableInClassList(BrainClasses[i], i == band);
            }
            if (_labelState != null)
                _labelState.text = StateLabels[band];
        }

        public void RenderCombo(int combo, int multiplier, int brokenCombo, double age, bool show, int pulseBonus = 0)
        {
            if (!TryBind()) return;
            bool visible = show && age >= 0d && age <= RhythmFeedbackFormatter.DurationSeconds
                && (combo > 0 || brokenCombo > 0);
            _comboLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                _comboLabel.style.scale = new Scale(Vector2.one);
                return;
            }

            _comboLabel.text = combo > 0 ? $"COMBO {combo}  /  x{multiplier}" : $"RACHA PERDIDA: {brokenCombo}";
            if (combo > 0 && pulseBonus > 0) _comboLabel.text += $"\nAL RITMO +{pulseBonus}";
            Color color = combo > 0 ? new Color(0.25f, 0.95f, 0.7f) : new Color(1f, 0.48f, 0.48f);
            _comboLabel.style.color = color;
            _comboLabel.style.opacity = Mathf.Clamp01((float)((RhythmFeedbackFormatter.DurationSeconds - age) / 0.3d));
            float pop = 1f + 0.08f * Mathf.Clamp01(1f - (float)age / 0.18f);
            _comboLabel.style.scale = new Scale(new Vector2(pop, pop));
        }

        public void SetReticleVisible(bool visible)
        {
            if (!TryBind()) return;
            _reticle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ResetFeedback()
        {
            if (!TryBind()) return;
            _comboLabel.text = "";
            _comboLabel.style.display = DisplayStyle.None;
            _comboLabel.style.scale = new Scale(Vector2.one);
            _comboLabel.style.opacity = 0f;
        }
    }
}
