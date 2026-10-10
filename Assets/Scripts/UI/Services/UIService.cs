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

        public ScreenType? CurrentScreen => _uiRepository.CurrentScreen;

        public void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument)
        {
            _uiRepository.RegisterScreens(screens, initialScreen, uiDocument);
        }

        public void ChangeScreen(ScreenType type, bool pushHistory = true)
        {
            _uiRepository.ChangeScreen(type, pushHistory);
        }

        /// <summary>
        /// Vuelve a la pantalla anterior (Pause ← Options, MainMenu ← Options, etc.).
        /// </summary>
        public void GoBack(ScreenType fallback = ScreenType.MainMenu)
        {
            _uiRepository.GoBack(fallback);
        }

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
