using System;
using Composition.Interfaces;
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
        Loading
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

        [Header("Audio de Escena")]
        [SerializeField] private MusicTrackSO sceneMusic;
        
        private UIService _uiService;
        private IAudioService _audioService;

        [Inject]
        public void Construct(UIService uiService, IObjectResolver container)
        {
            _uiService = uiService;
            if (container != null && container.TryResolve<IAudioService>(out var audioService))
            {
                _audioService = audioService;
            }
        }

        public void Start()
        {
            _uiService.RegisterScreens(screens, initialScreen, uIDocument);
            if (sceneMusic != null && _audioService != null)
                _audioService.PlayMusic(sceneMusic);
        }
    }
}
