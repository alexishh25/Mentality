using System.Collections.Generic;
using Gameplay.Doors.Models;
using Gameplay.Doors.Presenters;
using Gameplay.Doors.Views;
using UnityEngine;

namespace Gameplay.Doors.Services
{
    public class DoorManagerService
    {
        private readonly DoorConfig config;
        private readonly Transform playerTransform;
        private readonly List<DoorPresenter> presenters = new List<DoorPresenter>();

        // VContainer inyecta automáticamente la configuración y el jugador
        public DoorManagerService(DoorConfig config, Transform playerTransform)
        {
            this.config = config;
            this.playerTransform = playerTransform;
        }

        public void Initialize(DoorView[] doorsInScene)
        {
            presenters.Clear();

            foreach (var doorView in doorsInScene)
            {
                var presenter = new DoorPresenter(doorView, config, playerTransform, OnDoorNearTriggered);
                presenters.Add(presenter);
            }

            ActivateRandomDoor();
        }

        private void OnDoorNearTriggered(DoorPresenter triggeredPresenter)
        {
            ActivateRandomDoor(triggeredPresenter);
        }

        private void ActivateRandomDoor(DoorPresenter excludePresenter = null)
        {
            List<DoorPresenter> availablePresenters = new List<DoorPresenter>();

            foreach (var p in presenters)
            {
                p.View.SetActiveState(false);
                if (p != excludePresenter)
                {
                    availablePresenters.Add(p);
                }
            }

            if (availablePresenters.Count > 0)
            {
                int randomIndex = Random.Range(0, availablePresenters.Count);
                DoorPresenter selected = availablePresenters[randomIndex];
                selected.View.SetActiveState(true);
                selected.ResetProximityDetection();
            }
        }
    }
}