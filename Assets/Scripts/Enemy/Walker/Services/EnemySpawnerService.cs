using Cysharp.Threading.Tasks;
using Gameplay.Enemy.Walker.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Enemy.Walker.Services
{
    public class EnemySpawnerService : IDisposable
    {
        private readonly EnemyFactory factory;
        private readonly EnemySpawnerConfig config;
        private readonly Transform playerTransform;
        private bool isSpawning;

        public EnemySpawnerService(EnemyFactory factory, EnemySpawnerConfig config, Transform playerTransform)
        {
            this.factory = factory;
            this.config = config;
            this.playerTransform = playerTransform;
        }

        public void StartSpawning()
        {
            isSpawning = true;
            StartSpawningLoop().Forget();
        }

        private async UniTaskVoid StartSpawningLoop()
        {
            while (isSpawning)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(config.SpawnInterval));
                SpawnRandomEnemy();
            }
        }

        public void SpawnRandomEnemy()
        {
            if (config.SpawnPoints == null || config.SpawnPoints.Length == 0 || config.EnemyPrefab == null) return;

            // 1. Filtrar los puntos que cumplen con la distancia mínima respecto al jugador
            List<Transform> validSpawnPoints = new List<Transform>();

            foreach (var spawnPoint in config.SpawnPoints)
            {
                if (spawnPoint == null) continue;

                float distance = Vector3.Distance(playerTransform.position, spawnPoint.position);

                if (distance >= config.MinSpawnDistance)
                {
                    validSpawnPoints.Add(spawnPoint);
                }
            }

            // 2. Si el jugador está cerca de todos, toma el punto MÁS LEJANO de toda la lista como respaldo
            Transform selectedPoint = null;

            if (validSpawnPoints.Count > 0)
            {
                selectedPoint = validSpawnPoints[UnityEngine.Random.Range(0, validSpawnPoints.Count)];
            }
            else
            {
                selectedPoint = GetFarthestSpawnPoint();
            }

            // 3. Generar el enemigo en el punto seleccionado
            if (selectedPoint != null)
            {
                factory.Create(config.EnemyPrefab, selectedPoint.position, selectedPoint.rotation, playerTransform);
            }
        }

        private Transform GetFarthestSpawnPoint()
        {
            Transform farthestPoint = null;
            float maxDistance = -1f;

            foreach (var point in config.SpawnPoints)
            {
                if (point == null) continue;

                float distance = Vector3.Distance(playerTransform.position, point.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestPoint = point;
                }
            }

            return farthestPoint;
        }

        public void Dispose()
        {
            isSpawning = false;
        }
    }
}