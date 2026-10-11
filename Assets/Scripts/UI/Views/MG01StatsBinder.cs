using Mentality.Gameplay.MG01;
using Mentality.UI.ViewModels;
using UnityEngine.UIElements;

namespace Mentality.UI.Views
{
    // Rellena el bloque de estadisticas compartido (ResultStatsUXML).
    internal static class MG01StatsBinder
    {
        public static void Fill(VisualElement root, MG01ResultViewModel viewModel)
        {
            SetText(root, "statWordsLabel", MG01Texts.StatWordsDestroyed);
            SetText(root, "statWordsValue", viewModel.WordsDestroyedText);
            SetText(root, "statHitsLabel", MG01Texts.StatHitsTaken);
            SetText(root, "statHitsValue", viewModel.HitsTakenText);
            SetText(root, "statGhostsLabel", MG01Texts.StatGhostClicks);
            SetText(root, "statGhostsValue", viewModel.GhostClicksText);
            SetText(root, "statSanityLabel", MG01Texts.StatSanityRemaining);
            SetText(root, "statSanityValue", viewModel.SanityRemainingText);
        }

        public static void SetText(VisualElement root, string name, string text)
        {
            var element = root.Q<TextElement>(name);
            if (element != null) element.text = text;
        }
    }
}
