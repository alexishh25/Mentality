using UnityEngine;
using UnityEngine.UIElements;

public interface IUIService
{
    void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument);
    void ChangeScreen(ScreenType type);
    void EnableUI();
    void DisableUI();
}
