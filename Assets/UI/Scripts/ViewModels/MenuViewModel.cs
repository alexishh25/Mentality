using System.Collections.Generic;
using UnityEngine;
using UnityUI.Models;
using UnityUI.Services;

namespace UnityUI.ViewModels
{
	public class MenuViewModel : UnityBaseViewModel
	{
		private readonly MenuService _menuService;

		public MenuViewModel(MenuService menuService)
		{
			_menuService = menuService;
		}

		public void NewGame()
		{
			Debug.Log("[MenuViewModel] Confirmación: Click recibido en 'Nueva Partida'. Todo funciona correctamente.");
		}

		public List<MenuModel> GetSaveData()
		{
			return _menuService.ObtenerDatosGuardado();
		}

		public void OpenSettings()
		{
			Debug.Log("[MenuViewModel] Confirmación: Click recibido en 'Configuración'. Todo funciona correctamente.");
		}

		public void ExitGame()
		{
			Debug.Log("[MenuViewModel] Saliendo del juego...");
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			#else
			Application.Quit();
			#endif
		}
	}

}