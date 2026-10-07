using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay
{
    public class LockerPresenter : IStartable, IDisposable
    {
        private readonly ILockerView view;
        private readonly IObjectResolver resolver;
        private readonly GameObject consumablePrefab;
        private bool isOpened;
        private bool isBusy;

        public LockerPresenter(ILockerView view, IObjectResolver resolver, GameObject consumablePrefab)
        {
            this.view = view;
            this.resolver = resolver;
            this.consumablePrefab = consumablePrefab;
        }

        public void Start()
        {
            view.SetInteractionListener(HandleInteraction);
        }

        public void Dispose()
        {
            view.SetInteractionListener(null);
        }

        private void HandleInteraction()
        {
            if (isOpened) return;
            isOpened = true;

            view.AnimateOpen(OnOpened);
        }

        private async void OnOpened()
        {

            EjectConsumable();
            await UniTask.Delay(1000);

            view.AnimateClose(() =>
            {
                isBusy = false;
            });
        }

        private void EjectConsumable()
        {
            if (view.ConsumablePrefab == null) return;

            // Instancia el objeto exactamente dentro del cajón (utilizando la posición actual del drawer)
            GameObject spawnedItem = resolver.Instantiate(
                view.ConsumablePrefab,
                view.SpawnPoint.position,
                view.SpawnPoint.rotation
            );

            // Le da un impulso físico para salir disparado hacia el frente y caer
            if (spawnedItem.TryGetComponent<Rigidbody>(out var rb))
            {
                // Fuerza: Hacia adelante en base a la orientación del cajón + un toque leve hacia arriba
                Vector3 forwardDirection = view.SpawnPoint.forward;
                Vector3 ejectForce = (forwardDirection * 1.2f) + (Vector3.up * 0.5f);

                rb.AddForce(ejectForce, ForceMode.Impulse);
            }
        }
    }
}