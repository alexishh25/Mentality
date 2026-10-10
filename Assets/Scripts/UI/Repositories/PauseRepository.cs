using Mentality.UI.Services;

namespace Mentality.UI.Repositories
{
    public class PauseRepository
    {
        public void Resume(IGameplayPauseService pauseService) => pauseService.Resume();

        public void OpenSettings(UIService uiService) =>
            uiService.ChangeScreen(ScreenType.Options, pushHistory: true);

        public void ExitToMenu(IGameplayPauseService pauseService) =>
            pauseService.ExitToMainMenu();
    }
}
