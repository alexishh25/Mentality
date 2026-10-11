using Mentality.Gameplay.MG01;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mentality.UI.Views
{
    // Overlay persistente del HUD en su propio UIDocument (no pasa por UIRepository.ChangeScreen).
    // Solo enlaza y pinta; el estado lo decide HudViewModel y lo empuja HudPresenter.
    public class HudView : MonoBehaviour
    {
        private static readonly string[] BandClasses = { "sanity--high", "sanity--mid", "sanity--low", "sanity--critical" };
        private const string HitClass = "sanity--hit";
        private const string AbilityUnlockedClass = "hud-ability--unlocked";

        [SerializeField] private UIDocument document;

        [Tooltip("Opcional: retratos por banda (Alta, Media, Baja, Critica). Vacio = placeholder de USS.")]
        [SerializeField] private Texture2D[] portraitsByBand = new Texture2D[4];

        private VisualElement _root;
        private VisualElement _sanityPanel;
        private VisualElement _sanityGlow;
        private VisualElement _portraitArt;
        private VisualElement _progressFill;
        private VisualElement _abilityChip;
        private SanityWedgeBar _sanityBar;
        private Label _sanityValue;
        private Label _sanityMax;
        private Label _sanityDamage;
        private Label _sanityStatus;
        private Label _abilityState;

        public bool TryBind()
        {
            if (_root != null) return true;
            if (document == null || document.rootVisualElement == null) return false;

            VisualElement root = document.rootVisualElement;
            _sanityPanel = root.Q<VisualElement>("sanityPanel");
            _sanityGlow = root.Q<VisualElement>("sanityGlow");
            _portraitArt = root.Q<VisualElement>("sanityPortraitArt");
            _sanityBar = root.Q<SanityWedgeBar>("sanityBar");
            _sanityValue = root.Q<Label>("sanityValue");
            _sanityMax = root.Q<Label>("sanityMax");
            _sanityDamage = root.Q<Label>("sanityDamage");
            _sanityStatus = root.Q<Label>("sanityStatus");
            _progressFill = root.Q<VisualElement>("progressFill");
            _abilityChip = root.Q<VisualElement>("abilityChip");
            _abilityState = root.Q<Label>("abilityState");

            SetText(root.Q<Label>("sanityLabel"), MG01Texts.SanityLabel);
            SetText(root.Q<Label>("abilityName"), MG01Texts.AbilityName);
            SetText(root.Q<Label>("levelCaption"), MG01Texts.LevelCaption);
            SetText(root.Q<Label>("aimHint"), MG01Texts.AimHint);
            SetText(root.Q<Label>("songLabel"), MG01Texts.SongLabel);

            _root = root;
            return true;
        }

        public void SetSanityBar(float displayedNormalized, float ghostStart, float ghostWidth)
        {
            if (_sanityBar == null) return;
            _sanityBar.value = displayedNormalized;
            _sanityBar.SetGhostTrail(ghostStart, ghostWidth);
        }

        public void SetSanityValue(int value) => SetText(_sanityValue, value.ToString());

        public void SetSanityMax(int max) => SetText(_sanityMax, "/" + max);

        public void SetDamageText(string text) => SetText(_sanityDamage, text);

        public void SetStatus(string text) => SetText(_sanityStatus, text);

        public void SetBand(SanityBand band)
        {
            if (_sanityPanel == null) return;

            int index = (int)band;
            for (int i = 0; i < BandClasses.Length; i++)
                _sanityPanel.EnableInClassList(BandClasses[i], i == index);

            if (_portraitArt != null && portraitsByBand != null && index < portraitsByBand.Length && portraitsByBand[index] != null)
                _portraitArt.style.backgroundImage = new StyleBackground(portraitsByBand[index]);
        }

        public void SetHitActive(bool active) => _sanityPanel?.EnableInClassList(HitClass, active);

        public void SetShakeOffset(float pixels)
        {
            if (_sanityPanel == null) return;
            _sanityPanel.style.translate = new StyleTranslate(new Translate(pixels, 0f));
        }

        public void SetCriticalPulse(float pulse)
        {
            if (_sanityGlow == null) return;
            _sanityGlow.style.opacity = pulse;
        }

        public void SetProgress(float normalized)
        {
            if (_progressFill == null) return;
            _progressFill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        public void SetAbilityUnlocked(bool unlocked)
        {
            _abilityChip?.EnableInClassList(AbilityUnlockedClass, unlocked);
            SetText(_abilityState, unlocked ? MG01Texts.AbilityUnlocked : MG01Texts.AbilityLocked);
        }

        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
