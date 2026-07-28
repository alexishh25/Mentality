using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Domain.Repositories
{
    public class UIRepository : MonoBehaviour
    {
        [SerializeField] private UIDocument uIDocument;

        private ScreenEntry[] registeredScreens;
        private UnityUI.Views.BaseView currentLogic;
        private VisualElement root;

        private void Awake()
        {
            if (uIDocument == null)
                uIDocument = GetComponent<UIDocument>();

            if (uIDocument != null)
                root = uIDocument.rootVisualElement;

            // Instanciar el servicio puro de C# y registrar en el localizador global
            var uiService = new Unity.Domain.Services.UIService(this);
            // ServiceLocator.Register<IUIService>(uiService);
        }

        private void OnDestroy()
        {
            // ServiceLocator.Unregister<IUIService>();
        }

        // Called by UIController/MenuUIManager in each scene to register available screens
        public void RegisterScreens(ScreenEntry[] screens, ScreenType initialScreen, UIDocument uiDocument)
        {
            registeredScreens = screens;
            uIDocument = uiDocument;
            if (uIDocument != null)
                root = uIDocument.rootVisualElement;

            ChangeScreen(initialScreen);
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
                currentLogic.Initialize(screenInstance);
            else
                Debug.LogError($"No script assigned for {type}");
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