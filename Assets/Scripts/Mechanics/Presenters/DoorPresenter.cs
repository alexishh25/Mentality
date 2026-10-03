using System;
using Cysharp.Threading.Tasks;
using Gameplay.Doors.Interfaces;
using Gameplay.Doors.Models;
using UnityEngine;

namespace Gameplay.Doors.Presenters
{
    public class DoorPresenter
    {
        private readonly IDoorView view;
        private readonly DoorConfig config;
        private readonly Transform playerTransform;
        private readonly Action<DoorPresenter> onPlayerGotTooClose;
        private bool isProcessingTrap;

        public IDoorView View => view;

        public DoorPresenter(IDoorView view, DoorConfig config, Transform playerTransform, Action<DoorPresenter> onPlayerGotTooClose)
        {
            this.view = view;
            this.config = config;
            this.playerTransform = playerTransform;
            this.onPlayerGotTooClose = onPlayerGotTooClose;

            if (view is Views.DoorView doorView)
            {
                doorView.SetTouchListener(OnDoorTouched);
            }

            ResetProximityDetection();
        }

        public void ResetProximityDetection()
        {
            view.SetProximityListener(playerTransform, config.ProximityDistance, () => onPlayerGotTooClose?.Invoke(this));
        }

        private void OnDoorTouched()
        {
            if (isProcessingTrap) return;
            ExecuteTrapRoutineAsync().Forget();
        }

        private async UniTaskVoid ExecuteTrapRoutineAsync()
        {
            isProcessingTrap = true;

            // 1. Guardar posición original donde tocó la puerta
            Vector3 originalPosition = playerTransform.position;
            Quaternion originalRotation = playerTransform.rotation;

            // 2. Desactivar movimiento del jugador (Asegúrate de desactivar su Controller/Rigidbody)
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 3. Teletransportar a la sala de aislamiento
            playerTransform.position = config.IsolationRoomPoint.position;
            playerTransform.rotation = config.IsolationRoomPoint.rotation;

            // 4. Esperar 4 segundos inmovilizado
            await UniTask.Delay(TimeSpan.FromSeconds(config.ImmobilizeDuration));

            // 5. Regresar al punto de origen
            playerTransform.position = originalPosition;
            playerTransform.rotation = originalRotation;

            if (cc != null) cc.enabled = true;

            isProcessingTrap = false;
        }
    }
}