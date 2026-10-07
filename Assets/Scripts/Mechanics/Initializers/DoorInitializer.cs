using Gameplay.Doors.Models;
using Gameplay.Doors.Services;
using Gameplay.Doors.Views;
using UnityEngine;
using VContainer.Unity;

namespace Gameplay.Doors.Services
{
    public class DoorInitializer : IStartable
    {
        private readonly DoorManagerService doorManagerService;

        // Inyectamos el servicio gestionado por VContainer
        public DoorInitializer(DoorManagerService doorManagerService)
        {
            this.doorManagerService = doorManagerService;
        }

        public void Start()
        {
            // Encuentra todas las puertas activas e inactivas en la escena
            var doorsInScene = Object.FindObjectsByType<DoorView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (doorsInScene.Length == 0)
            {
                Debug.LogWarning("[DoorInitializer] No se encontraron componentes DoorView en la escena.");
                return;
            }

            // Inicializamos el servicio que VContainer mantendrá vivo en memoria
            doorManagerService.Initialize(doorsInScene);
        }
    }
}