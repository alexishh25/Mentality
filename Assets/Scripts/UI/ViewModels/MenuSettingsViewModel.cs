using Mentality.UI.Services;
using Mentality.UI.Commands;

namespace Mentality.UI.ViewModels
{
    public class MenuSettingsViewModel : UnityBaseViewModel
    {
        private readonly MenuSettingsService _menuSettingsService;

        public UnityHandler ReturnMenuCommand { get; }
        
        public MenuSettingsViewModel(MenuSettingsService menuSettingsService)
        {
            _menuSettingsService = menuSettingsService;
            ReturnMenuCommand = new UnityHandler(ReturnMenu);
        }

        public void ReturnMenu()
        {
            _menuSettingsService.ReturntoMenu();
        }
    }
}