using Mentality.UI.Commands;
using Mentality.UI.Services;

namespace Mentality.UI.ViewModels
{
    public class PauseViewModel : UnityBaseViewModel
    {
        private readonly PauseService _pauseService;

        public UnityHandler ResumeCommand { get; }
        public UnityHandler OpenSettingsCommand { get; }
        public UnityHandler ExitToMenuCommand { get; }

        public PauseViewModel(PauseService pauseService)
        {
            _pauseService = pauseService;
            ResumeCommand = new UnityHandler(Resume);
            OpenSettingsCommand = new UnityHandler(OpenSettings);
            ExitToMenuCommand = new UnityHandler(ExitToMenu);
        }

        public void Resume() => _pauseService.Resume();
        public void OpenSettings() => _pauseService.OpenSettings();
        public void ExitToMenu() => _pauseService.ExitToMenu();
    }
}
