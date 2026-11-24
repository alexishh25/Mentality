using System.Collections.Generic;
using Mentality.Infrastructure.AppLifecycle;
using Mentality.UI.Models;
using Mentality.UI.Services;

namespace Mentality.UI.Repositories
{
    public class MenuRepository
    {
        private readonly IAppFlowService _appFlowService;

        public MenuRepository(IAppFlowService appFlowService)
        {
            _appFlowService = appFlowService;
        }

        public void IniciarNuevaPartida()
        {
            _appFlowService.IniciarNuevaPartida("SampleScene");
        }

        public List<MenuModel> ObtenerGuardado()
        {
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
            _appFlowService.SalirJuego();
        }
    }
}