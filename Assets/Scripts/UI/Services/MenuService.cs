using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mentality.UI.Models;
using Mentality.UI.Repositories;
using VContainer.Unity;

namespace Mentality.UI.Services
{
	public class MenuService : IInitializable, IDisposable
	{
		private readonly MenuRepository _menuRepository;
		private readonly UIService _uiService;
		public void IniciarNuevaPartida() => _menuRepository.IniciarNuevaPartida();
		public List<MenuModel> ObtenerDatosGuardado() => _menuRepository.ObtenerGuardado();
		public void AbrirOptions() => _menuRepository.AbrirOptions(_uiService);
		public void Salir() => _menuRepository.Salir();

        public MenuService(MenuRepository menuRepository, UIService uiService)
        {
            _menuRepository = menuRepository;
            _uiService = uiService;
        }
        public void Initialize()
        {
            // Aquí puedes inicializar cualquier cosa que necesites al iniciar el servicio
        }

		public void Dispose() { }
    }
}