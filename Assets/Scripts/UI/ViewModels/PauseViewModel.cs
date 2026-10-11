using Mentality.Gameplay.MG01;
using Mentality.UI.Commands;

namespace Mentality.UI.ViewModels
{
    public class PauseViewModel : UnityBaseViewModel
    {
        private readonly IPauseService _pauseService;
        private readonly IMG01SceneNavigator _navigator;

        public UnityHandler ResumeCommand { get; }
        public UnityHandler ExitCommand { get; }

        public PauseViewModel(IPauseService pauseService, IMG01SceneNavigator navigator)
        {
            _pauseService = pauseService;
            _navigator = navigator;
            ResumeCommand = new UnityHandler(Resume);
            ExitCommand = new UnityHandler(_navigator.GoToMenu);
        }

        private void Resume() => _pauseService.SetPaused(false);
    }
}
