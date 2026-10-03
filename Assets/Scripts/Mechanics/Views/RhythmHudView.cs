using UnityEngine;
using UnityEngine.UI;

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

        public void Render(int hits, int misses, string status, string rhythm, string cursorHint, float pulse)
        {
            if (hitsText != null) hitsText.text = hits.ToString("00");
            if (missesText != null) missesText.text = misses.ToString("00");
            if (statusText != null) statusText.text = status;
            if (rhythmText != null) rhythmText.text = rhythm;
            if (cursorText != null) cursorText.text = cursorHint;
            if (beatIndicator != null)
                beatIndicator.color = Color.Lerp(new Color(0.13f, 0.22f, 0.25f), new Color(0.3f, 0.95f, 0.8f), pulse);
        }
    }
}
