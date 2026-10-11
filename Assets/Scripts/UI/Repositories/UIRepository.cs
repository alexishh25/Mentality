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

        private readonly IObjectResolver _resolver;

        public UIRepository(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Initialize()
        {

        // Instanciar el servicio puro de C# y registrar en el localizador global
        // var uiService = new UIService(this);
        // ServiceLocator.Register<IUIService>(uiService);
        }

        // Called by UIController/MenuUIManager in each scene to register available screens
        public void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument, bool showInitialScreen = true)
        {
            registeredScreens = screens;
            uIDocument = uiDocument;
            if (uIDocument != null)
                root = uIDocument.rootVisualElement;

            if (showInitialScreen)
                ChangeScreen(initialScreen);
        }

        public void HideScreen()
        {
            if (currentLogic != null)
            {
                currentLogic.Dispose();
                currentLogic = null;
            }

            root?.Clear();
        }

        public void ChangeScreen(ScreenType type)
        {
            if (root == null) return;

        // Cancel inputs on the old screen to prevent clicking bugs
        //if (currentLogic != null)
        //{
        //    currentLogic.Dispose();
        //}

            // Find screen entry
            ScreenEntry entry = Array.Find(registeredScreens, s => s.type == type);
            if (entry.visualAsset == null)
            {
                Debug.LogError($"Screen not found for type: {type}");
                return;
            }

            // Clear old visual elements
            root.Clear();

            // Instantiate and add new screen
            VisualElement screenInstance = entry.visualAsset.Instantiate();
            screenInstance.style.flexGrow = 1f;

            screenInstance.style.position = Position.Absolute;
            screenInstance.style.left = 0;
            screenInstance.style.right = 0;
            screenInstance.style.top = 0;
            screenInstance.style.bottom = 0;

            root.Add(screenInstance);

            currentLogic = entry.logicComponent;

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

        public void DisableUI()
        {
            if (uIDocument == null) return;
            uIDocument.gameObject.SetActive(false);
        }

        public void EnableUI()
        {
            if (uIDocument == null) return;
            uIDocument.gameObject.SetActive(true);
        }
    }
}
