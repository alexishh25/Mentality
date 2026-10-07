using UnityEngine;
using UnityEngine.UI;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;

namespace Gameplay.Rhythm.Views
{
    public class RhythmHudView : MonoBehaviour
    {
        [SerializeField] private Text hitsText;
        [SerializeField] private Text missesText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text rhythmText;
        [SerializeField] private Text cursorText;
        [SerializeField] private Image beatIndicator;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text resultText;

        public void RenderScore(long score, int combo, int bestCombo, int multiplier, string result,
            RhythmJudgment judgment, double resultAge)
        {
            if (scoreText != null)
                scoreText.text = $"PUNTOS {score:N0}\nCOMBO {combo}  /  x{multiplier}\nMEJOR RACHA {bestCombo}";
            if (resultText == null) return;
            resultText.text = result;
            bool visible = !string.IsNullOrEmpty(result) && resultAge >= 0
                && resultAge <= RhythmFeedbackFormatter.DurationSeconds;
            resultText.enabled = visible;
            if (!visible) return;
            Color color = judgment == RhythmJudgment.Perfect ? new Color(1f, 0.84f, 0.28f)
                : judgment == RhythmJudgment.Good || judgment == RhythmJudgment.Hit ? new Color(0.25f, 0.95f, 0.7f) : new Color(1f, 0.48f, 0.48f);
            color.a = Mathf.Clamp01((float)((RhythmFeedbackFormatter.DurationSeconds - resultAge) / 0.3d));
            resultText.color = color;
            float emphasis = 1f + 0.08f * Mathf.Clamp01(1f - (float)resultAge / 0.18f);
            resultText.rectTransform.localScale = Vector3.one * emphasis;
        }

        public void Render(int hits, int misses, string status, string rhythm, string cursorHint, float pulse,
            RhythmJudgment judgment = RhythmJudgment.None)
        {
            if (hitsText != null) hitsText.text = hits.ToString("00");
            if (missesText != null) missesText.text = misses.ToString("00");
            if (statusText != null)
            {
                statusText.text = status;
                statusText.color = judgment == RhythmJudgment.Perfect ? new Color(1f, 0.84f, 0.28f)
                    : judgment == RhythmJudgment.Good || judgment == RhythmJudgment.Hit ? new Color(0.25f, 0.95f, 0.7f)
                    : judgment == RhythmJudgment.OutOfTime || judgment == RhythmJudgment.Miss ? new Color(1f, 0.48f, 0.48f) : Color.white;
            }
            if (rhythmText != null) rhythmText.text = rhythm;
            if (cursorText != null) cursorText.text = cursorHint;
            if (beatIndicator != null)
                beatIndicator.color = Color.Lerp(new Color(0.13f, 0.22f, 0.25f), new Color(0.3f, 0.95f, 0.8f), pulse);
        }
    }
}
