using System;
using UnityEngine;
using UnityEngine.UIElements;
using Mentality.UI.Views;
using Mentality.UI.Services;
using VContainer;

namespace Mentality.UI
{
    public enum ScreenType
    {
        MainMenu,
        Options,
        LoadData,
        Loading,
        Pause
    }

    [Serializable]
    public struct ScreenEntry
    {
        public ScreenType type;
        public VisualTreeAsset visualAsset;
        public BaseView logicComponent;
    }

    public class UIDoc : MonoBehaviour
    {
        [Header("Screens available in this scene")]
        [SerializeField] private ScreenType initialScreen;
        [SerializeField] private UIDocument uIDocument;
        [SerializeField] private ScreenEntry[] screens;

        [Header("Gameplay")]
        [Tooltip("Si está activo, oculta el UIDocument tras registrar pantallas (p. ej. overlay de pausa en SampleScene).")]
        [SerializeField] private bool hideUiAfterRegister;

        private UIService _uiService;

        [Inject]
        public void Construct(UIService uiService)
        {
            _uiService = uiService;
        }

        public void Start()
        {
            _uiService.RegisterScreens(screens, initialScreen, uIDocument);
            if (hideUiAfterRegister)
                _uiService.DisableUI();
        }
    }
}
