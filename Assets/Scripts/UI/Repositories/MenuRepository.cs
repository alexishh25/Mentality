using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityUI.Models;

namespace UnityUI.Repositories
{
	public class MenuRepository
	{
		public List<MenuModel> ObtenerGuardado()
        {
            // Aquí iría la lógica para obtener los datos del guardado desde PlayerPrefs, JSON, etc.
            // Por ahora, devolvemos una lista simulada
            return new List<MenuModel>
            {
                new MenuModel { HasSavedGame = true, LatestSavedGameName = "Partida1" },
                new MenuModel { HasSavedGame = false, LatestSavedGameName = "Partida2" },
                new MenuModel { HasSavedGame = true, LatestSavedGameName = "Partida3" }
            };
        }
    }

}