using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mentality.UI.Models;
using Mentality.UI.Services;

namespace Mentality.UI.Repositories
{
	public class MenuRepository
	{
        public const string GameplaySceneName = "SampleScene";

        public void IniciarNuevaPartida()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }
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

        public void AbrirOptions(UIService uiService)
        {
            uiService.ChangeScreen(ScreenType.Options);
        }
        public void Salir()
        {
            #if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			#else
			Application.Quit();
			#endif
        }
    }

}