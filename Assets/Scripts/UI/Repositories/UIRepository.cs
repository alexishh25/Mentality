using System;
using UnityEngine;
using UnityEngine.UIElements;
using Mentality.UI.Views;
using VContainer;
using VContainer.Unity;

namespace Mentality.UI.Repositories
{
    public class UIRepository : IInitializable
    {
        [SerializeField] private UIDocument uIDocument;

        private ScreenEntry[] registeredScreens;
        private BaseView currentLogic;
        private VisualElement root;
        private ScreenType? currentScreen;
        private ScreenType? previousScreen;

        public ScreenType? CurrentScreen => currentScreen;

        private readonly IObjectResolver _resolver;

        public UIRepository(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Initialize()
        {
        }

        // Called by UIDoc in each scene to register available screens
        public void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument)
        {
            registeredScreens = screens;
            uIDocument = uiDocument;
            if (uIDocument != null)
                root = uIDocument.rootVisualElement;

            ChangeScreen(initialScreen, pushHistory: false);
        }

        public void ChangeScreen(ScreenType type, bool pushHistory = true)
        {
            if (uIDocument != null && !uIDocument.gameObject.activeInHierarchy)
                uIDocument.gameObject.SetActive(true);

            if (uIDocument != null)
                root = uIDocument.rootVisualElement;

            if (root == null) return;

            if (currentLogic != null)
                currentLogic.Dispose();

            if (pushHistory && currentScreen.HasValue)
                previousScreen = currentScreen;

            ScreenEntry entry = Array.Find(registeredScreens, s => s.type == type);
            if (entry.visualAsset == null)
            {
                Debug.LogError($"Screen not found for type: {type}");
                return;
            }

            root.Clear();

            VisualElement screenInstance = entry.visualAsset.Instantiate();
            screenInstance.style.flexGrow = 1f;

            screenInstance.style.position = Position.Absolute;
            screenInstance.style.left = 0;
            screenInstance.style.right = 0;
            screenInstance.style.top = 0;
            screenInstance.style.bottom = 0;

            root.Add(screenInstance);

            currentLogic = entry.logicComponent;
            currentScreen = type;

            if (currentLogic != null)
            {
                _resolver.Inject(currentLogic);
                currentLogic.Initialize(screenInstance);
            }
            else
            {
                Debug.LogError($"No script assigned for {type}");
            }
        }

        public void GoBack(ScreenType fallback)
        {
            ScreenType target = previousScreen ?? fallback;
            previousScreen = null;
            ChangeScreen(target, pushHistory: false);
        }

        public void DisableUI()
        {
            if (uIDocument == null) return;
            uIDocument.gameObject.SetActive(false);
        }

        public void EnableUI()
        {
            if (uIDocument == null) return;
            uIDocument.gameObject.SetActive(true);
            if (uIDocument != null)
                root = uIDocument.rootVisualElement;
        }
    }
}
