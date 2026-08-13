using Mentality.UI.Services;

namespace Mentality.UI.Repositories
{
    public class MenuSettingsRepository
    {
        public void ReturntoMenu(UIService uiService) => uiService.ChangeScreen(ScreenType.MainMenu);
    }
}