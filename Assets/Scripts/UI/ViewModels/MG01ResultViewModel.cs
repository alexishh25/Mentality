using Mentality.Gameplay.MG01;
using Mentality.UI.Commands;
using UnityEngine;

namespace Mentality.UI.ViewModels
{
    // Base comun de Game Over y Recompensa: estadisticas de la partida + volver al menu.
    public abstract class MG01ResultViewModel : UnityBaseViewModel
    {
        private readonly IMG01ResultService _resultService;
        private readonly ISanityService _sanityService;
        protected readonly IMG01SceneNavigator Navigator;

        public UnityHandler BackToMenuCommand { get; }

        public string WordsDestroyedText => _resultService.Stats.wordsDestroyed.ToString();
        public string HitsTakenText => _resultService.Stats.hitsTaken.ToString();
        public string GhostClicksText => _resultService.Stats.ghostClicks.ToString();
        public string SanityRemainingText =>
            $"{Mathf.RoundToInt(_resultService.Stats.sanityRemaining)} / {Mathf.RoundToInt(_sanityService.Max)}";

        protected MG01ResultViewModel(IMG01ResultService resultService, ISanityService sanityService, IMG01SceneNavigator navigator)
        {
            _resultService = resultService;
            _sanityService = sanityService;
            Navigator = navigator;
            BackToMenuCommand = new UnityHandler(Navigator.GoToMenu);
        }
    }

    public class GameOverViewModel : MG01ResultViewModel
    {
        public string Title => MG01Texts.GameOverTitle;
        public string Subtitle => MG01Texts.GameOverSubtitle;
        public UnityHandler RetryCommand { get; }

        public GameOverViewModel(IMG01ResultService resultService, ISanityService sanityService, IMG01SceneNavigator navigator)
            : base(resultService, sanityService, navigator)
        {
            RetryCommand = new UnityHandler(Navigator.ReloadLevel);
        }
    }

    public class RewardViewModel : MG01ResultViewModel
    {
        public string Title => MG01Texts.RewardTitle;
        public string RewardLine => MG01Texts.RewardLine;
        public string ClosingMessage => MG01Texts.AlphaClosingMessage;
        public UnityHandler ReplayCommand { get; }

        public RewardViewModel(IMG01ResultService resultService, ISanityService sanityService, IMG01SceneNavigator navigator)
            : base(resultService, sanityService, navigator)
        {
            ReplayCommand = new UnityHandler(Navigator.ReloadLevel);
        }
    }
}
