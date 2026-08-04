using System;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public enum ScreenType
{
    MainMenu,
    Options,
    LoadData,
    Loading
}

[Serializable]
public struct ScreenEntry
{
    public ScreenType type;
    public VisualTreeAsset visualAsset;
    public UnityUI.Views.BaseView logicComponent;
}
public class UIDoc : MonoBehaviour
{
    [Header("Screens available in this scene")]
    [SerializeField] private ScreenType initialScreen;
    [SerializeField] private UIDocument uIDocument;
    [SerializeField] private ScreenEntry[] screens;

    private UIService _uiService;

    [Inject]
    public void Construct(UIService uiService)
    {
        _uiService =  uiService;
    }
    private void Start()
    {
        _uiService.RegisterScreens(screens, initialScreen, uIDocument);
    }
}
