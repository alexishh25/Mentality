using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mentality.UI.Models;
using Mentality.UI.Services;
using Mentality.UI.Commands;

namespace Mentality.UI.ViewModels
{
	public class MenuViewModel : UnityBaseViewModel
	{
		private readonly MenuService _menuService;

		public UnityHandler NewGameCommand { get; }
		public UnityHandler OpenSettingsCommand { get; }
		public UnityHandler ExitGameCommand { get; }

		public MenuViewModel(MenuService menuService)
		{
			_menuService = menuService;
			NewGameCommand = new UnityHandler(NewGame);
			OpenSettingsCommand = new UnityHandler(OpenSettings);
			ExitGameCommand = new UnityHandler(ExitGame);
		}

		public void NewGame()
		{
			_menuService.IniciarNuevaPartida();
		}

		public List<MenuModel> GetSaveData()
		{
			return _menuService.ObtenerDatosGuardado();
		}

		public void OpenSettings()
		{
			_menuService.AbrirOptions();
		}

		public void ExitGame()
		{
			Debug.Log("[MenuViewModel] Saliendo del juego...");
			_menuService.Salir();
		}
	}
}