using Mentality.UI.Services;

namespace Mentality.UI.Repositories
{
    public class MenuSettingsRepository
    {
        /// <summary>
        /// Regresa a la pantalla previa (MainMenu o Pause). Si no hay historial, MainMenu.
        /// </summary>
        public void ReturntoMenu(UIService uiService) => uiService.GoBack(ScreenType.MainMenu);
    }
}