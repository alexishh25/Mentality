using Cysharp.Threading.Tasks;
using Gameplay.Enemy.Walker.Interfaces;
using UnityEngine;

namespace Gameplay.Enemy.Walker.Presenters
{
    public enum EnemyBehavior { RunLeft, RunRight, ChargePlayer }

    public class EnemyPresenter
    {
        private readonly IEnemyView view;
        private readonly Transform playerTransform;
        private readonly float speed = 12f;
        private bool isActive;

        public EnemyPresenter(IEnemyView view, Transform playerTransform)
        {
            this.view = view;
            this.playerTransform = playerTransform;
        }

        public async UniTask ExecuteBehaviorAsync()
        {
            isActive = true;
            EnemyBehavior choice = (EnemyBehavior)Random.Range(0, 3);

            if (choice == EnemyBehavior.ChargePlayer)
            {
                // 1. FASE DE ESPERA: Quedarse mirando al jugador durante 2 segundos
                float stareDuration = 2f;
                float stareElapsed = 0f;

                while (stareElapsed < stareDuration && isActive)
                {
                    stareElapsed += Time.deltaTime;
                    Vector3 lookDirection = (playerTransform.position - view.Transform.position).normalized;
                    lookDirection.y = 0;

                    // Pasamos velocidad 0 para que no avance, pero actualice su rotación (transform.forward)
                    view.MoveTo(lookDirection, 0f);
                    await UniTask.Yield();
                }

                // 2. FASE DE ATAQUE: Bloquear la dirección actual y correr en línea recta
                Vector3 lockedChargeDirection = (playerTransform.position - view.Transform.position).normalized;
                lockedChargeDirection.y = 0;

                // Desaparecer si toca al jugador
                view.SetProximityListener(playerTransform, 1.5f, Despawn);

                // Tiempo límite por si el jugador lo esquiva y el enemigo sigue de largo
                float maxRunDuration = 5f;
                float runElapsed = 0f;

                while (runElapsed < maxRunDuration && isActive)
                {
                    runElapsed += Time.deltaTime;
                    // Corre en la dirección bloqueada, ignorando si el jugador se mueve
                    view.MoveTo(lockedChargeDirection, speed);
                    await UniTask.Yield();
                }

                // Si falló el ataque y se acabó el tiempo de corrida, desaparece
                if (isActive) Despawn();
            }
            else
            {
                // Comportamiento de huida (Izquierda o Derecha)
                Vector3 moveDir = choice == EnemyBehavior.RunLeft ? -view.Transform.right : view.Transform.right;
                float runDuration = 2.5f;
                float runElapsed = 0f;

                while (runElapsed < runDuration && isActive)
                {
                    runElapsed += Time.deltaTime;
                    view.MoveTo(moveDir, speed);
                    await UniTask.Yield();
                }

                if (isActive) Despawn();
            }
        }

        private void Despawn()
        {
            isActive = false;
            view.DestroySelf();
        }
    }
}