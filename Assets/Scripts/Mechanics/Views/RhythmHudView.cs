using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;

namespace Gameplay.Rhythm.Views
{
    public class RhythmHudView : MonoBehaviour
    {
        // Referencias del HUD anterior para ocultarlo en escenas existentes.
        [SerializeField] private Text hitsText;
        [SerializeField] private Text missesText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text rhythmText;
        [SerializeField] private Text cursorText;
        [SerializeField] private Image beatIndicator;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text resultText;
        [SerializeField] private Image sanityFill;
        [SerializeField] private Text sanityText;
        private GameObject _gameOverPanel;
        private GameObject _victoryPanel;
        private Button _restartButton;
        private Button _victoryRestartButton;
        public event Action RestartRequested;

        private void Awake() => InitializePresentation();

        public void InitializePresentation()
        {
            var top = transform.Find("Top panel");
            var footer = transform.Find("Footer");
            if (top != null) top.gameObject.SetActive(false);
            if (footer != null) footer.gameObject.SetActive(false);
            foreach (var text in new[] { hitsText, missesText, statusText, rhythmText, cursorText, scoreText })
                if (text != null) text.enabled = false;
            if (beatIndicator != null) beatIndicator.enabled = false;
            if (resultText != null)
            {
                resultText.raycastTarget = false;
                resultText.enabled = false;
            }
            if (sanityFill != null && sanityText != null) return;

            var root = CreateRect("Sanity", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -28f);
            root.sizeDelta = new Vector2(360f, 60f);
            var label = CreateRect("Cordura", root, Vector2.zero, Vector2.one);
            label.offsetMin = new Vector2(0f, 30f);
            label.offsetMax = Vector2.zero;
            sanityText = label.gameObject.AddComponent<Text>();
            sanityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sanityText.fontSize = 22;
            sanityText.alignment = TextAnchor.MiddleCenter;
            sanityText.color = Color.white;
            sanityText.raycastTarget = false;
            var background = CreateRect("Bar background", root, Vector2.zero, Vector2.one);
            background.offsetMin = Vector2.zero;
            background.offsetMax = new Vector2(0f, -36f);
            var image = background.gameObject.AddComponent<Image>();
            image.color = new Color(0.025f, 0.035f, 0.05f, 0.85f);
            image.raycastTarget = false;
            var fill = CreateRect("Sanity fill", background, Vector2.zero, Vector2.one);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);
            sanityFill = fill.gameObject.AddComponent<Image>();
            sanityFill.raycastTarget = false;
            RenderSanity(RhythmGameModel.MaximumSanity);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            return rect;
        }

        public void RenderSanity(int sanity)
        {
            int value = Mathf.Clamp(sanity, 0, RhythmGameModel.MaximumSanity);
            float fraction = value / (float)RhythmGameModel.MaximumSanity;
            if (sanityText != null) sanityText.text = $"CORDURA  {value}%";
            if (sanityFill == null) return;
            // Recortar por anclas funciona tambien sin un sprite asignado.
            sanityFill.rectTransform.anchorMax = new Vector2(fraction, 1f);
            sanityFill.enabled = value > 0;
            sanityFill.color = Color.Lerp(new Color(1f, 0.35f, 0.35f), new Color(0.25f, 0.95f, 0.7f), fraction);
        }

        public void RenderCombo(int combo, int multiplier, int brokenCombo, double age, bool show, int pulseBonus = 0)
        {
            if (resultText == null) return;
            bool visible = show && age >= 0d && age <= RhythmFeedbackFormatter.DurationSeconds
                && (combo > 0 || brokenCombo > 0);
            resultText.enabled = visible;
            if (!visible) return;
            resultText.text = combo > 0 ? $"COMBO {combo}  /  x{multiplier}" : $"RACHA PERDIDA: {brokenCombo}";
            if (combo > 0 && pulseBonus > 0) resultText.text += $"\nAL RITMO +{pulseBonus}";
            Color color = combo > 0 ? new Color(0.25f, 0.95f, 0.7f) : new Color(1f, 0.48f, 0.48f);
            color.a = Mathf.Clamp01((float)((RhythmFeedbackFormatter.DurationSeconds - age) / 0.3d));
            resultText.color = color;
            resultText.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Clamp01(1f - (float)age / 0.18f));
        }

        public void ShowGameOver(bool visible)
        {
            if (_gameOverPanel == null && visible) _gameOverPanel = BuildResultPanel(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(visible);
            if (visible && resultText != null) resultText.enabled = false;
        }

        public void ResetSessionPresentation(int sanity)
        {
            ShowGameOver(false);
            ShowVictory(false);
            RenderSanity(sanity);
            if (resultText != null)
            {
                resultText.text = "";
                resultText.enabled = false;
                resultText.rectTransform.localScale = Vector3.one;
            }
            var events = EventSystem.current;
            var selected = events != null ? events.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(transform))
                events.SetSelectedGameObject(null);
        }

        public void ShowVictory(bool visible)
        {
            if (_victoryPanel == null && visible) _victoryPanel = BuildResultPanel(true);
            if (_victoryPanel != null) _victoryPanel.SetActive(visible);
            if (visible && resultText != null) resultText.enabled = false;
        }

        private GameObject BuildResultPanel(bool won)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Rhythm UI events", typeof(EventSystem));
                events.transform.SetParent(transform, false);
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var panel = CreateRect(won ? "Victory" : "Game Over", transform, Vector2.zero, Vector2.one);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            var backdrop = panel.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.015f, 0.02f, 0.035f, 0.95f);
            backdrop.raycastTarget = true;
            var title = CreateRect("Result title", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            title.anchoredPosition = new Vector2(0f, 95f);
            title.sizeDelta = new Vector2(700f, 140f);
            AddLabel(title, won ? "WIN GAME\nNIVEL COMPLETADO" : "GAME OVER\nCORDURA AGOTADA", 40,
                won ? new Color(0.25f, 0.95f, 0.7f) : new Color(1f, 0.4f, 0.4f));
            var button = CreateRect("Restart level", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            button.anchoredPosition = new Vector2(0f, -65f);
            button.sizeDelta = new Vector2(340f, 70f);
            var image = button.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.35f, 0.32f);
            var restart = button.gameObject.AddComponent<Button>();
            restart.targetGraphic = image;
            restart.onClick.AddListener(OnRestartClicked);
            if (won) _victoryRestartButton = restart;
            else _restartButton = restart;
            var label = CreateRect("Restart label", button, Vector2.zero, Vector2.one);
            label.offsetMin = label.offsetMax = Vector2.zero;
            AddLabel(label, "REINICIAR NIVEL", 26, Color.white);
            return panel.gameObject;
        }

        private static void AddLabel(RectTransform rect, string value, int size, Color color)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            text.color = color;
            text.raycastTarget = false;
        }

        private void OnRestartClicked() => RestartRequested?.Invoke();

        private void OnDestroy()
        {
            if (_restartButton != null) _restartButton.onClick.RemoveListener(OnRestartClicked);
            if (_victoryRestartButton != null) _victoryRestartButton.onClick.RemoveListener(OnRestartClicked);
            RestartRequested = null;
        }
    }
}
