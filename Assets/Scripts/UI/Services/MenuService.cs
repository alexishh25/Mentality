using System.Collections.Generic;
using UnityEngine;
using UnityUI.Models;
using UnityUI.Repositories;

namespace UnityUI.Services
{
	public class MenuService : MonoBehaviour
	{
		private readonly MenuRepository _menuRepository;
		public List<MenuModel> ObtenerDatosGuardado() => _menuRepository.ObtenerGuardado();
	}
}