using System;
using Unity.Domain.Repositories;
using Unity.Domain.Services;
using UnityEngine;
using UnityEngine.UIElements;

public enum ScreenType
{
    MainMenu,
    SelectCharacter,
    LoadScreen,
    Settings
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

    private UIService uiService;

    private void Start()
    {
        var repository = GetComponent<UIRepository>();
        if (repository == null)
        {
            repository = gameObject.AddComponent<UIRepository>();
        }

        try
        {
            uiService = new UIService(repository);

        }
        catch (Exception ex )
        {
            Debug.Log("Error occurred while initializing UIService.");
            Debug.LogException(ex);
        }
        uiService.RegisterScreens(screens, initialScreen, uIDocument);
    }
}
