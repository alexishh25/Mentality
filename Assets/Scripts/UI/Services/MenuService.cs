using System;
using System.Collections.Generic;
using UnityEngine;
using UnityUI.Models;
using UnityUI.Repositories;
using VContainer.Unity;

namespace UnityUI.Services
{
	public class MenuService : IInitializable, IDisposable
	{
		private readonly MenuRepository _menuRepository;
		public List<MenuModel> ObtenerDatosGuardado() => _menuRepository.ObtenerGuardado();

        public MenuService(MenuRepository menuRepository)
        {
            _menuRepository = menuRepository;
        }
        public void Initialize()
        {
            // Aquí puedes inicializar cualquier cosa que necesites al iniciar el servicio
        }

		public void Dispose() { }
    }
}