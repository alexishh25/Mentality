using UnityEngine;
using UnityEngine.UIElements;

public class UIService
{
    private readonly UIRepository _uiRepository;

    public UIService(UIRepository uiRepository)
    {
        _uiRepository = uiRepository;
    }

    public void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument)
    {
        _uiRepository.RegisterScreens(screens, initialScreen, uiDocument);
    }

    public void ChangeScreen(ScreenType type)
    {
        // Omitimos TransitionScreenData ya que quitamos las transiciones
        _uiRepository.ChangeScreen(type);
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
