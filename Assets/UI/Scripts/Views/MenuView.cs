using UnityEngine;
using UnityEngine.UIElements;
using UnityUI.ViewModels;
using UnityUI.Services;

namespace UnityUI.Views
{
	public class MenuView : BaseView
	{
		private Button newpartyButton;
		private Button loadpartyButton;
		private Button settingsButton;
		private Button exitButton;

		private MenuViewModel _menuViewModel;

		protected override void Awake()
		{
			base.Awake();
			
			// Buscamos o creamos el componente MenuService en la escena
			var menuService = FindFirstObjectByType<MenuService>();
			if (menuService == null)
			{
				var go = new GameObject("MenuService");
				menuService = go.AddComponent<MenuService>();
			}

			_menuViewModel = new MenuViewModel(menuService);
		}

		protected override void OnInitialize(VisualElement root)
		{
			newpartyButton = root.Q<Button>("newBtn");
			loadpartyButton = root.Q<Button>("loadBtn");
			settingsButton = root.Q<Button>("settingsBtn");
			exitButton = root.Q<Button>("exitBtn");
		}

		protected override void OnBind()
		{
			// Vinculamos acciones al hacer click
			if (newpartyButton != null) newpartyButton.clicked += OnNewPartyClicked;
			if (loadpartyButton != null) loadpartyButton.clicked += OnLoadPartyClicked;
			if (settingsButton != null) settingsButton.clicked += OnSettingsClicked;
			if (exitButton != null) exitButton.clicked += OnExitClicked;
		}

		protected override void OnUnbind()
		{
			// Limpiamos eventos al cerrar la pantalla
			if (newpartyButton != null) newpartyButton.clicked -= OnNewPartyClicked;
			if (loadpartyButton != null) loadpartyButton.clicked -= OnLoadPartyClicked;
			if (settingsButton != null) settingsButton.clicked -= OnSettingsClicked;
			if (exitButton != null) exitButton.clicked -= OnExitClicked;
		}

		private void OnNewPartyClicked()
		{
			_menuViewModel?.NewGame();
		}

		private void OnLoadPartyClicked()
		{
			var saves = _menuViewModel?.GetSaveData();
			Debug.Log($"[MenuView] Cargar partida. Slots de guardado encontrados: {saves?.Count ?? 0}");
			// Aquí abrirías el menú de cargas pasando los datos a otra pantalla
		}

		private void OnSettingsClicked()
		{
			_menuViewModel?.OpenSettings();
		}

		private void OnExitClicked()
		{
			_menuViewModel?.ExitGame();
		}
	}
}