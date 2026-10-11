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
        LoadMinigame,
        // Agregar valores siempre al final: ScreenEntry se serializa como entero en las escenas.
        GameOver,
        Reward,
        Pause,
        RhythmGameOver,
        RhythmVictory
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
        [Tooltip("Desactivar en escenas de gameplay: las pantallas (Game Over, Recompensa, Pausa) se muestran por eventos.")]
        [SerializeField] private bool showInitialScreen = true;
        [SerializeField] private UIDocument uIDocument;
        [SerializeField] private ScreenEntry[] screens;

        private UIService _uiService;

        [Inject]
        public void Construct(UIService uiService)
        {
            _uiService = uiService;
        }

        public void Start()
        {
            _uiService.RegisterScreens(screens, initialScreen, uIDocument, showInitialScreen);
        }
    }
}
