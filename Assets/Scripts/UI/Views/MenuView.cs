using UnityEngine;
using UnityEngine.UIElements;
using Mentality.UI.ViewModels;
using VContainer;

namespace Mentality.UI.Views
{
	public class MenuView : BaseView
	{
		private Button newpartyButton;
		private Button loadpartyButton;
		private Button settingsButton;
		private Button exitButton;

		private MenuViewModel _menuViewModel;

		[Inject]
		public void Construct(MenuViewModel menuViewModel)
		{
			_menuViewModel = menuViewModel;
		}

		protected override void Awake()
		{
			base.Awake();
		}

		protected override void OnInitialize(VisualElement root)
		{
			newpartyButton = root.Q<Button>("newBtn");
			loadpartyButton = root.Q<Button>("continueBtn");
			settingsButton = root.Q<Button>("optionsBtn");
			exitButton = root.Q<Button>("exitBtn");
		}

		protected override void OnBind()
		{
			if (_menuViewModel == null) return;

			// Vinculamos acciones al hacer click a los comandos del ViewModel
			if (newpartyButton != null) newpartyButton.clicked += _menuViewModel.NewGameCommand.Execute;
			if (loadpartyButton != null) loadpartyButton.clicked += OnLoadPartyClicked;
			if (settingsButton != null) settingsButton.clicked += _menuViewModel.OpenSettingsCommand.Execute;
			if (exitButton != null) exitButton.clicked += _menuViewModel.ExitGameCommand.Execute;
		}

		protected override void OnUnbind()
		{
			if (_menuViewModel == null) return;

			// Limpiamos eventos al cerrar la pantalla (mismos handlers que OnBind)
			if (newpartyButton != null) newpartyButton.clicked -= _menuViewModel.NewGameCommand.Execute;
			if (loadpartyButton != null) loadpartyButton.clicked -= OnLoadPartyClicked;
			if (settingsButton != null) settingsButton.clicked -= _menuViewModel.OpenSettingsCommand.Execute;
			if (exitButton != null) exitButton.clicked -= _menuViewModel.ExitGameCommand.Execute;
		}

		private void OnNewPartyClicked()
		{
			_menuViewModel?.NewGame();
		}

		private void OnLoadPartyClicked()
		{
			var saves = _menuViewModel?.GetSaveData();
			if (saves != null)
			{
				foreach (var save in saves)
				{
					Debug.Log($"Elija: {save.LatestSavedGameName}");
				}
			}
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