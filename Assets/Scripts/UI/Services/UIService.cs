using UnityEngine;
using UnityEngine.UIElements;
using Mentality.UI.Repositories;

namespace Mentality.UI.Services
{
    public class UIService
    {
        private readonly UIRepository _uiRepository;

        public UIService(UIRepository uiRepository)
        {
            _uiRepository = uiRepository;
        }

        public void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument, bool showInitialScreen = true)
        {
            _uiRepository.RegisterScreens(screens, initialScreen, uiDocument, showInitialScreen);
        }

        public void ChangeScreen(ScreenType type)
        {
            // Omitimos TransitionScreenData ya que quitamos las transiciones
            _uiRepository.ChangeScreen(type);
        }

        public void HideScreen() => _uiRepository.HideScreen();

        public void EnableUI()
        {
            _uiRepository.EnableUI();
        }

        public void DisableUI()
        {
            _uiRepository.DisableUI();
        }
    }
}
